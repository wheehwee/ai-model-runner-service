using Microsoft.ML.OnnxRuntime.Tensors;
using StableDiffusion.ML.OnnxRuntime.Configs;
using System;
using System.Collections.Generic;
using System.Text;

namespace StableDiffusion.ML.OnnxRuntime.Interfaces
{
    public interface ITextProcessing
    {
        public Task<DenseTensor<float>> PreprocessText(string refId, string prompt, StableDiffusionConfig config, string negativePrompt = null);
    }
}
