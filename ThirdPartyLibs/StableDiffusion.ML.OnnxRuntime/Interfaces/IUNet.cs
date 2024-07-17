using StableDiffusion.ML.OnnxRuntime.Configs;

namespace StableDiffusion.ML.OnnxRuntime.Interfaces
{
    public interface IUNet
    {
        public Task<SixLabors.ImageSharp.Image> Inference(string refId, string prompt, StableDiffusionConfig config, string negativePrompt = null, Action<int> newStepCallback = null);
    }
}
