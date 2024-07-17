using Infrastructure.Services.Mediators;
using MassTransit;

namespace WebApi.Configuration
{
    public static partial class HostingExtensions
    {
        public static IServiceCollection AddInfrastructureServiceBus(this WebApplicationBuilder builder)
        {
            builder.Services.AddMediator(cfg =>
            {
                cfg.AddConsumer<TextToImageDataProcessingMediator>(configure =>
                {
                    configure.Options<BatchOptions>(options => options.SetConcurrencyLimit(1000).SetMessageLimit(100));
                });
            });

            return builder.Services;
        }
    }
}
