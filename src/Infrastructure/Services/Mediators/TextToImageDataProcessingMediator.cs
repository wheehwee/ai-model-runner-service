using Application.Events;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Mediators
{
    public class TextToImageDataProcessingMediator : IConsumer<Batch<TextToImageRunDataEvent>>
    {
        private readonly IServiceProvider _serviceProvider;

        public TextToImageDataProcessingMediator(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Consume(ConsumeContext<Batch<TextToImageRunDataEvent>> context)
        {
            var batch = context.Message;

            foreach (var item in batch)
            {
                var message = item.Message;
                await message.Task(_serviceProvider);
            }
        }
    }
}
