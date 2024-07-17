using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StableDiffusion.ML.OnnxRuntime.Configs;

namespace StableDiffusion.ML.OnnxRuntime.Interfaces
{
    public interface IVaeDecoder
    {
        public Task<Tensor<float>> Decoder(string refId, List<NamedOnnxValue> input, string VaeDecoderOnnxPath);

        public Image<Rgba32> ConvertToImage(string refId, Tensor<float> output, StableDiffusionConfig config, int width = 512, int height = 512);
    }
}
