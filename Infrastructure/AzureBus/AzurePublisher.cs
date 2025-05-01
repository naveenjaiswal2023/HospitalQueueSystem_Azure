using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzurePublisher : IPublisher
    {
        private readonly ServiceBusSender _sender;
        private readonly ILogger<AzurePublisher> _logger;

        public AzurePublisher(ServiceBusSender sender, ILogger<AzurePublisher> logger)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
            _logger = logger;
        }

        public async Task PublishAsync<T>(T @event, string subject)
        {
            try
            {
                var message = new ServiceBusMessage(JsonSerializer.Serialize(@event))
                {
                    Subject = subject
                };

                await _sender.SendMessageAsync(message);
                _logger.LogInformation("{Subject} published successfully.", subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing {Subject}.", subject);
                throw;
            }
        }
    }
}
