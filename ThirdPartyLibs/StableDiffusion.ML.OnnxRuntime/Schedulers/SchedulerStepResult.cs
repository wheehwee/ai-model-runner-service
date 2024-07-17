using Microsoft.ML.OnnxRuntime.Tensors;

namespace StableDiffusion.ML.OnnxRuntime.Schedulers
{
    public class SchedulerStepResult
    {
        public SchedulerStepResult(DenseTensor<float> result)
        {
            Result = result;
        }

        public SchedulerStepResult(DenseTensor<float> previousSample, DenseTensor<float> sampleData)
        {
            Result = previousSample;
            SampleData = sampleData;
        }

        public DenseTensor<float> Result { get; set; }
        public DenseTensor<float> SampleData { get; set; }
    }
}
