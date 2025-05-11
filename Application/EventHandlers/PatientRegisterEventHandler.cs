using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Events;
using MediatR;
using System.Text.Json;

namespace HospitalQueueSystem.Application.EventHandlers
{
    public class PatientRegisterEventHandler : INotificationHandler<PatientRegisteredEvent>
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<PatientRegisterEventHandler> _logger;
        private const string TopicName = "patient-topic";

        public PatientRegisterEventHandler(ServiceBusClient serviceBusClient, ILogger<PatientRegisterEventHandler> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task Handle(PatientRegisteredEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender(TopicName);

                var messageBody = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(messageBody)
                {
                    Subject = "PatientRegisteredEvent"
                };

                await sender.SendMessageAsync(message, cancellationToken);
                _logger.LogInformation("Published PatientRegisteredEvent for PatientId: {PatientId}", notification.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PatientRegisteredEvent.");
            }
        }
    }
}
