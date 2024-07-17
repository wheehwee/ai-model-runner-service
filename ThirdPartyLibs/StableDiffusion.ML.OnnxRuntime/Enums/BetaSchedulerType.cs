using System;
using System.Collections.Generic;
using System.Text;

namespace StableDiffusion.ML.OnnxRuntime.Enums
{
    public enum BetaScheduleType
    {
        Linear = 0,
        ScaledLinear = 1,
        SquaredCosCapV2 = 2,
        Sigmoid = 3
    }
}
