using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using StableDiffusion.ML.OnnxRuntime.Helpers;
using StableDiffusion.ML.OnnxRuntime.Interfaces;
using Microsoft.Extensions.Logging;
using StableDiffusion.ML.OnnxRuntime.Configs;

namespace StableDiffusion.ML.OnnxRuntime
{
    public class VaeDecoder : IVaeDecoder
    {
        private readonly SemaphoreSlim lockSession = new SemaphoreSlim(1, 1);

        private InferenceSession vaeDecodeSession;

        private ILogger<VaeDecoder> _logger;
        
        public VaeDecoder(ILogger<VaeDecoder> logger)
        {
            _logger = logger;
        }

        public async Task<Tensor<float>> Decoder(string refId, List<NamedOnnxValue> input, string VaeDecoderOnnxPath)
        {
            try
            {
                await lockSession.WaitAsync();
                if (vaeDecodeSession == null)
                {
                    // Create an InferenceSession from the Model Path.
                    vaeDecodeSession = new InferenceSession(VaeDecoderOnnxPath);
                }
            }
            finally
            {
                lockSession.Release();
            }

            // Run session and send the input data in to get inference output. 
            var output = vaeDecodeSession.Run(input);
            var result = (output.ToList().First().Value as DenseTensor<Float16>);
            var resultFloat = TensorHelper.ConvertFloat16ToFloat(result);

            return resultFloat;
        }

        // create method to convert float array to an image with imagesharp
        public Image<Rgba32> ConvertToImage(string refId, Tensor<float> output, StableDiffusionConfig config, int width = 512, int height = 512)
        {
            var result = new Image<Rgba32>(width, height);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    result[x, y] = new Rgba32(
                        (byte)(Math.Round(Math.Clamp((output[0, 0, y, x] / 2 + 0.5), 0, 1) * 255)),
                        (byte)(Math.Round(Math.Clamp((output[0, 1, y, x] / 2 + 0.5), 0, 1) * 255)),
                        (byte)(Math.Round(Math.Clamp((output[0, 2, y, x] / 2 + 0.5), 0, 1) * 255))
                    );
                }
            }

            var imageName = $"sd_image_{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.png";
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), config.ImageOutputPath, imageName);

            result.Save(imagePath);

            _logger.LogInformation($"Image for run with refId \"{refId}\" saved to: {imagePath}");

            return result;
        }
    }
}
