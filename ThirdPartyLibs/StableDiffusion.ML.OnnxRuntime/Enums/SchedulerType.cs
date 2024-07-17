﻿using System.ComponentModel.DataAnnotations;

namespace StableDiffusion.ML.OnnxRuntime.Enums
{
    public enum SchedulerType
    {
        [Display(Name = "LMS")]
        LMS = 0,

        [Display(Name = "Euler")]
        Euler = 1,

        [Display(Name = "Euler Ancestral")]
        EulerAncestral = 2,

        [Display(Name = "DDPM")]
        DDPM = 3,

        [Display(Name = "DDIM")]
        DDIM = 4,

        [Display(Name = "KDPM2")]
        KDPM2 = 5,

        [Display(Name = "DDPMWuerstchen")]
        DDPMWuerstchen = 6,

        [Display(Name = "LCM")]
        LCM = 20,

        [Display(Name = "InstaFlow")]
        InstaFlow = 21
    }
}