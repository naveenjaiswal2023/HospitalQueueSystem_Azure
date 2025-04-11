using HospitalQueueSystem.Domain.Interfaces;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzureServiceBusBackgroundService : BackgroundService
    {
        private readonly IQueueSubscriber _subscriber;

        public AzureServiceBusBackgroundService(IQueueSubscriber subscriber)
        {
            _subscriber = subscriber;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _subscriber.StartAsync(stoppingToken);
        }
    }
}
