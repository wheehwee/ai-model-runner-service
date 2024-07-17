using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Tokenizers;
using StableDiffusion.ML.OnnxRuntime.Helpers;
using StableDiffusion.ML.OnnxRuntime.Interfaces;
using Microsoft.Extensions.Logging;
using StableDiffusion.ML.OnnxRuntime.Configs;

namespace StableDiffusion.ML.OnnxRuntime
{
    public class TextProcessing : ITextProcessing
    {
        private readonly ILogger<TextProcessing> _logger;


        public TextProcessing(ILogger<TextProcessing> logger)
        {
            _logger = logger;
        }

        private readonly SemaphoreSlim lockTokenizeSession = new SemaphoreSlim(1, 1);

        private InferenceSession tokenizeSession;
        private Tokenizer tokenizer;

        private readonly SemaphoreSlim lockEncodeSession = new SemaphoreSlim(1, 1);

        private InferenceSession encodeSession;

        public async Task<DenseTensor<float>> PreprocessText(string refId, string prompt, StableDiffusionConfig config, string negativePrompt = null)
        {
            // Load the tokenizer and text encoder to tokenize and encode the text.
            var textTokenized = await TokenizeText(refId, prompt, config);
            
            // Create uncond_input of blank tokens
            var uncondInputTokens = negativePrompt == null || negativePrompt == string.Empty ? CreateUncondInput() : await TokenizeText(refId, negativePrompt, config);

            //Pad other array
            var maxTokens = Math.Max(textTokenized.Length, uncondInputTokens.Length);
            //maxTokens = maxTokens % 77 != 0 ? ((int) Math.Ceiling((decimal)maxTokens / 77))*77 : maxTokens;

            if (maxTokens > textTokenized.Length)
            {
                var textPad = Enumerable.Repeat(49407, maxTokens - textTokenized.Length).Select(x => (long)x).ToArray();
                textTokenized = textTokenized.Concat(textPad).ToArray();
            }

            if (maxTokens > uncondInputTokens.Length)
            {
                var uncondPad = Enumerable.Repeat(49407, maxTokens - uncondInputTokens.Length).Select(x => (long)x).ToArray();
                uncondInputTokens = uncondInputTokens.Concat(uncondPad).ToArray();
            }

            // Concant textEmeddings and uncondEmbedding
            var textPromptEmbeddings = (await TextEncoder(textTokenized, config, maxTokens));
            var uncondEmbedding = (await TextEncoder(uncondInputTokens, config, maxTokens));

            var dimensions = uncondEmbedding.Dimensions.ToArray();
            dimensions[0] += textPromptEmbeddings.Dimensions[0];

            var buffer = new DenseTensor<float>(dimensions);
            uncondEmbedding.Buffer.CopyTo(buffer.Buffer[..(int)uncondEmbedding.Length]);
            textPromptEmbeddings.Buffer.CopyTo(buffer.Buffer[(int)uncondEmbedding.Length..]);

            return buffer;

            /*for (var i = 0; i < maxTokens; i++)
            {
                textEmbeddings[0, i / 768, i % 768] = uncondEmbedding[i % 77];
                textEmbeddings[1, i / 768, i % 768] = textPromptEmbeddings[i % 77];
            }
            return textEmbeddings;*/
        }
        private async Task<long[]> TokenizeText(string refId, string text, StableDiffusionConfig config)
        {
            // Create session options for custom op of extensions
            var sessionOptions = new SessionOptions();

            var directory = Directory.GetCurrentDirectory().ToString();

            sessionOptions.RegisterCustomOpLibraryV2(config.OrtExtensionsPath, out var libraryHandle);
            
            try
            {
                await lockTokenizeSession.WaitAsync();
                if (config.CustomTokenizerPath == string.Empty && tokenizeSession == null)
                {
                    tokenizeSession = new InferenceSession(config.TokenizerPath, sessionOptions);
                }
                if (config.CustomTokenizerPath != string.Empty && tokenizer == null)
                {
                    tokenizer = new Tokenizer(new Bpe($"{config.CustomTokenizerPath}\\vocab.json", $"{config.CustomTokenizerPath}\\merges.txt")
                    {
                        UnknownToken = "<|endoftext|>"
                    });
                }
            }
            finally
            {
                lockTokenizeSession.Release();
            }

            long[]? inputIds;

            if (config.CustomTokenizerPath != string.Empty)
            {
                var tokens = tokenizer.Model.Tokenize(text);
                inputIds = tokens.Select(t => (long)t.Id).ToList().ToArray();
            }
            else 
            {
                var inputTensor = new DenseTensor<string>(new string[] { text }, new int[] { 1 });
                var inputString = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor<string>("string_input", inputTensor) };
                // Run session and send the input data in to get inference output. 
                var tokens = tokenizeSession.Run(inputString);
                inputIds = (tokens.ToList().First().Value as IEnumerable<long>).ToArray();
            }

            _logger.LogInformation($"Model run {refId} tokenized its prompt: {string.Join(" ", inputIds)}");

            // Cast inputIds to Int32
            var InputIdsInt = inputIds;

            var modelMaxLength = 77;
            // Pad array with 49407 until length is modelMaxLength
            if (InputIdsInt.Length < modelMaxLength)
            {
                var pad = Enumerable.Repeat(49407, 77 - InputIdsInt.Length).Select(x => (long)x).ToArray();
                InputIdsInt = InputIdsInt.Concat(pad).ToArray();
            }

            return InputIdsInt;

        }

        private long[] CreateUncondInput()
        {
            // Create an array of empty tokens for the unconditional input.
            var blankTokenValue = 49407;
            var modelMaxLength = 77;
            var inputIds = new List<Int32>();
            inputIds.Add(49406);
            var pad = Enumerable.Repeat(blankTokenValue, modelMaxLength - inputIds.Count()).ToArray();
            inputIds.AddRange(pad);

            return inputIds.Select(x => (long)x).ToArray();
        }

        private IEnumerable<long> PadWithBlankTokens(IEnumerable<long> inputs, int requiredLength, int padTokenId)
        {
            var count = inputs.Count();
            if (requiredLength > count)
                return inputs.Concat(Enumerable.Repeat((long)padTokenId, requiredLength - count));
            return inputs;
        }

        private async Task<DenseTensor<float>> TextEncoder(long[] tokenizedInput, StableDiffusionConfig config, int minimumLength)
        {
            var tokenBatches = new List<long[]>();
            foreach (var tokenBatch in tokenizedInput.Chunk(77))
                tokenBatches.Add(PadWithBlankTokens(tokenBatch, 77, 49407).ToArray());

            var promptEmbeddings = new List<float>();

            for (int i = 0; i < tokenBatches.Count; i++)
            {
                // Create input tensor.
                var input_ids = TensorHelper.CreateTensor(tokenBatches[i].Select(x => (int)x).ToArray(), new[] { 1, tokenBatches[i].Length });

                var input = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input_ids", input_ids) };

                // Set CUDA EP
                var sessionOptions = config.GetSessionOptionsForEp();
                try
                {
                    await lockEncodeSession.WaitAsync();
                    if (encodeSession == null)
                    {
                        encodeSession = new InferenceSession(config.TextEncoderOnnxPath, sessionOptions);
                    }
                }
                finally
                {
                    lockEncodeSession.Release();
                }
                // Run inference.
                var encoded = encodeSession.Run(input);
                var lastHiddenStateD = (encoded.ToList().First().Value as IEnumerable<Float16>).ToArray();
                // cast to float
                var lastHiddenState = lastHiddenStateD.Select(x => (float)BitConverter.UInt16BitsToHalf(x)).ToArray();

                promptEmbeddings.AddRange(lastHiddenState);
            }

            var lastHiddenStateTensor = TensorHelper.CreateTensor(promptEmbeddings.ToArray(), new[] { 1, promptEmbeddings.Count()/768 , 768 });

            return lastHiddenStateTensor;

        }
    }
}