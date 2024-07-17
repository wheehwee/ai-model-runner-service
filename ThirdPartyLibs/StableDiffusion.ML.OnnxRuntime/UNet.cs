using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using StableDiffusion.ML.OnnxRuntime.Helpers;
using StableDiffusion.ML.OnnxRuntime.Schedulers;
using StableDiffusion.ML.OnnxRuntime.Configs;
using StableDiffusion.ML.OnnxRuntime.Interfaces;

namespace StableDiffusion.ML.OnnxRuntime
{
    public class UNet : IUNet
    {
        private readonly SemaphoreSlim lockSession = new SemaphoreSlim(1, 1);

        private InferenceSession unetSession;

        private ITextProcessing _textProcessing;
        private IVaeDecoder _vaeDecoder;
        private ILogger<UNet> _logger;

        public UNet(ITextProcessing textProcessing, ILogger<UNet> logger, IVaeDecoder vaeDecoder)
        {
            _textProcessing = textProcessing;
            _logger = logger;
            _vaeDecoder = vaeDecoder;
        }

        private List<NamedOnnxValue> CreateUnetModelInput(
DenseTensor<float> encoderHiddenStates, DenseTensor<float> sample, int timeStep)
        {
            // convert encoderHiddenStates to float16 tensors
            var encoderHiddenStatesFloat16 = TensorHelper.ConvertFloatToFloat16(encoderHiddenStates);
            var sampleFloat16 = TensorHelper.ConvertFloatToFloat16(sample);
            var timeStepFloat = new DenseTensor<float>(new float[] { timeStep }, new int[] { 1 });

            var input = new List<NamedOnnxValue> {
                NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHiddenStatesFloat16),
                NamedOnnxValue.CreateFromTensor("sample", sampleFloat16),
                NamedOnnxValue.CreateFromTensor("timestep", TensorHelper.ConvertFloatToFloat16(timeStepFloat))
            };

            return input;

        }

        private DenseTensor<float> GenerateLatentSample(StableDiffusionConfig config, int seed, float initNoiseSigma)
        {
            return GenerateLatentSample(config.Height, config.Width, seed, initNoiseSigma);
        }
        private DenseTensor<float> GenerateLatentSample(int height, int width, int seed, float initNoiseSigma)
        {
            var random = new Random(seed);
            var batchSize = 1;
            var channels = 4;
            var latents = new DenseTensor<float>(new[] { batchSize, channels, height/8, width/8 });
            var latentsArray = latents.ToArray();

            for (int i = 0; i < latentsArray.Length; i++)
            {
                // Generate a random number from a normal distribution with mean 0 and variance 1
                var u1 = random.NextDouble(); // Uniform(0,1) random number
                var u2 = random.NextDouble(); // Uniform(0,1) random number
                var radius = Math.Sqrt(-2.0 * Math.Log(u1)); // Radius of polar coordinates
                var theta = 2.0 * Math.PI * u2; // Angle of polar coordinates
                var standardNormalRand = radius * Math.Cos(theta); // Standard normal random number

                // add noise to latents with * scheduler.init_noise_sigma
                // generate randoms that are negative and positive
                latentsArray[i] = (float)(standardNormalRand * initNoiseSigma);
            }

            latents = TensorHelper.CreateTensor(latentsArray, latents.Dimensions.ToArray());

            return latents;

        }

        private static Tensor<float> performGuidance(Tensor<float> noisePred, Tensor<float> noisePredText, double guidanceScale)
        {
            for (int i = 0; i < noisePred.Dimensions[0]; i++)
            {
                for (int j = 0; j < noisePred.Dimensions[1]; j++)
                {
                    for (int k = 0; k < noisePred.Dimensions[2]; k++)
                    {
                        for (int l = 0; l < noisePred.Dimensions[3]; l++)
                        {
                            noisePred[i, j, k, l] = (float)(noisePred[i, j, k, l] + (float)guidanceScale * (noisePredText[i, j, k, l] - noisePred[i, j, k, l]));
                        }
                    }
                }
            }
            return noisePred;
        }

        private static DenseTensor<float> PerformGuidance(DenseTensor<float> noisePrediction, float guidanceScale)
        {
            // Split Prompt and Negative Prompt predictions
            var dimensions = noisePrediction.Dimensions.ToArray();
            dimensions[0] /= 2;

            var length = (int)noisePrediction.Length / 2;
            var noisePredCond = new DenseTensor<float>(noisePrediction.Buffer[length..], dimensions);
            var noisePredUncond = new DenseTensor<float>(noisePrediction.Buffer[..length], dimensions);
            return noisePredUncond
                .Add(noisePredCond
                .Subtract(noisePredUncond)
                .MultiplyBy(guidanceScale));
        }

        public async Task<SixLabors.ImageSharp.Image> Inference(string refId, string prompt, StableDiffusionConfig config, string negativePrompt = null, Action<int> newStepCallback = null)
        {
            // Preprocess text
            var textEmbeddings = await _textProcessing.PreprocessText(refId, prompt, config, negativePrompt);

            var scheduler = new LMSScheduler(new SchedulerOptions()
            {
                Width = config.Width,
                Height = config.Height,
                InferenceSteps = config.NumInferenceSteps,
                GuidanceScale = config.GuidanceScale
            });

            var timesteps = scheduler.Timesteps;
            //  If you use the same seed, you will get the same image result.
            var seed = new Random().Next();
            //var seed = 329922609;
            _logger.LogInformation($"Seed generated for run with refId {refId}: {seed}");
            // create latent tensor

            var latents = GenerateLatentSample(config, seed, scheduler.InitNoiseSigma);

            var sessionOptions = config.GetSessionOptionsForEp();

            try
            {
                await lockSession.WaitAsync();
                // Create Inference Session
                unetSession = new InferenceSession(config.UnetOnnxPath, sessionOptions);
            }
            finally
            {
                lockSession.Release();
            }

            int step = 0;
            foreach (var timestep in timesteps)
            {
                ++step;

                if(newStepCallback != null)
                {
                    newStepCallback(step);
                };

                // torch.cat([latents] * 2)
                var latentModelInput = TensorHelper.Duplicate(latents, 2);

                // latent_model_input = scheduler.scale_model_input(latent_model_input, timestep = t)
                var latentTensor = scheduler.ScaleInput(latentModelInput, timestep);

                _logger.LogInformation($"Run with refId \"{refId}\" logs: scaled model input {latentTensor[0]} at step {step}.");
                var input = CreateUnetModelInput(textEmbeddings, latentTensor, timestep);

                // Run Inference
                DenseTensor<Float16> outputTensor = null;
                await Task.Run(() =>
                {
                    RunInference(input, out outputTensor);
                });

                // Perform guidance
                var noisePred = PerformGuidance(TensorHelper.ConvertFloat16ToFloat(outputTensor), config.GuidanceScale);

                // LMS Scheduler Step
                latents = scheduler.Step(noisePred, timestep, latents).Result;
                _logger.LogInformation($"Run with refId \"{refId}\" logs: latents result after step {step} min {latents.Min()} max {latents.Max()}");

            }

            // Scale and decode the image latents with vae.
            // latents = 1 / 0.18215 * latents
            latents = latents.MultiplyTensorByFloat((float)(1.0f / 0.18215f));
            var latentsFloat16 = TensorHelper.ConvertFloatToFloat16(latents);
            var decoderInput = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("latent_sample", latentsFloat16) };

            // Decode image
            var imageResultTensor = await _vaeDecoder.Decoder(refId, decoderInput, config.VaeDecoderOnnxPath);

            var image = _vaeDecoder.ConvertToImage(refId, imageResultTensor, config);
            return image;
        }


        private void RunInference(List<NamedOnnxValue> input, out DenseTensor<Float16> outputTensor)
        {
            var output = unetSession.Run(input);
            outputTensor = output.ToList().First().AsTensor<Float16>().ToDenseTensor();
        }
    }
}
