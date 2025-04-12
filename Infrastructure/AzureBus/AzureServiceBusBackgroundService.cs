using HospitalQueueSystem.Domain.Interfaces;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzureServiceBusBackgroundService : BackgroundService
    {
        private readonly IQueueSubscriber _subscriber;
        private readonly ILogger<AzureServiceBusBackgroundService> _logger;

        public AzureServiceBusBackgroundService(
            IQueueSubscriber subscriber,
            ILogger<AzureServiceBusBackgroundService> logger)
        {
            _subscriber = subscriber;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting Azure Service Bus Subscriber...");
                await _subscriber.StartAsync(stoppingToken);
                _logger.LogInformation("Azure Service Bus Subscriber started.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while starting Azure Service Bus Subscriber.");
            }
        }

    }
}
