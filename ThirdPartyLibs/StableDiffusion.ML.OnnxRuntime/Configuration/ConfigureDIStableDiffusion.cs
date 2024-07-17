using Microsoft.Extensions.DependencyInjection;
using StableDiffusion.ML.OnnxRuntime.Interfaces;
using StableDiffusion.ML.OnnxRuntime;

namespace StableDiffusion.ML.OnnxRuntime.Configuration
{
    public static class ConfigureDIStableDiffusion
    {
        public static IServiceCollection AddStableDiffusion(this IServiceCollection services)
        {
            services.AddSingleton<ITextProcessing, TextProcessing>();
            services.AddSingleton<IUNet, UNet>();
            services.AddSingleton<IVaeDecoder, VaeDecoder>();

            return services;
        }
    }
}
