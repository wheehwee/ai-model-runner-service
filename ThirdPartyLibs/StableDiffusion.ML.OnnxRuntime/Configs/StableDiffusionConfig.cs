using Microsoft.ML.OnnxRuntime;

namespace StableDiffusion.ML.OnnxRuntime.Configs
{
    public class StableDiffusionConfig
    {
        
        public enum ExecutionProvider
        {
            DirectML = 0,
            Cuda = 1,
            Cpu = 2
        }
        // default props
        public int NumInferenceSteps = 15;
        public ExecutionProvider ExecutionProviderTarget = ExecutionProvider.Cuda;
        public float GuidanceScale = 7.5f;
        public int Height = 512;
        public int Width = 512;
        public int DeviceId = 0;


        public string OrtExtensionsPath = @"..\..\ThirdPartyLibs\StableDiffusion.ML.OnnxRuntime\ortextensions.dll";
        public string TokenizerPath = @"..\\..\ThirdPartyLibs\StableDiffusion.ML.OnnxRuntime\cliptokenizer.onnx";
        public string TextEncoderOnnxPath = "";
        public string UnetOnnxPath = "";
        public string VaeDecoderOnnxPath = "";
        public string SafetyModelPath = "";
        public string CustomTokenizerPath = "";

        // default directory for images
        public string ImageOutputPath = "";

        public SessionOptions GetSessionOptionsForEp()
        {
            var sessionOptions = new SessionOptions();

            switch (this.ExecutionProviderTarget)
            {
                case ExecutionProvider.DirectML:
                    sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    sessionOptions.EnableMemoryPattern = false;
                    sessionOptions.AppendExecutionProvider_DML(this.DeviceId);
                    sessionOptions.AppendExecutionProvider_CPU();
                    return sessionOptions;
                case ExecutionProvider.Cpu:
                    sessionOptions.AppendExecutionProvider_CPU();
                    return sessionOptions;
                default:
                case ExecutionProvider.Cuda:
                    sessionOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    //default to CUDA, fall back on CPU if CUDA is not available.
                    sessionOptions.AppendExecutionProvider_CUDA(this.DeviceId);
                    sessionOptions.AppendExecutionProvider_CPU();
                    //sessionOptions = SessionOptions.MakeSessionOptionWithCudaProvider(cudaProviderOptions);
                    return sessionOptions;

            }

        }



    }


}
