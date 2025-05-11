using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Events;
using MediatR;
using System.Text.Json;

namespace HospitalQueueSystem.Application.EventHandlers
{
    public class PatientDeletedEventHandler : INotificationHandler<PatientDeletedEvent>
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<PatientDeletedEventHandler> _logger;
        private const string TopicName = "patient-topic";

        public PatientDeletedEventHandler(ServiceBusClient serviceBusClient, ILogger<PatientDeletedEventHandler> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task Handle(PatientDeletedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender(TopicName);

                var messageBody = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(messageBody)
                {
                    Subject = "PatientDeletedEvent"
                };

                await sender.SendMessageAsync(message, cancellationToken);
                _logger.LogInformation("Published PatientDeletedEvent for PatientId: {PatientId}", notification.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PatientDeletedEvent.");
            }
        }
    }
}
