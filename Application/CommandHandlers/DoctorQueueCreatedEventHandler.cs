using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.EventHandlers;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.WebAPI.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Text.Json;
using System.Threading;

namespace HospitalQueueSystem.Application.Handlers
{
    public class DoctorQueueCreatedEventHandler : INotificationHandler<DoctorQueueCreatedEvent>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<PatientRegisterEventHandler> _logger;
        private const string TopicName = "patient-topic";

        public DoctorQueueCreatedEventHandler(ServiceBusClient serviceBusClient, ILogger<PatientRegisterEventHandler> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task Handle(DoctorQueueCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var sender = _serviceBusClient.CreateSender(TopicName);

                var messageBody = JsonSerializer.Serialize(notification);
                var message = new ServiceBusMessage(messageBody)
                {
                    Subject = "PatientRegisterEventHandler"
                };

                await sender.SendMessageAsync(message, cancellationToken);
                _logger.LogInformation("Published PatientRegisterEventHandler for PatientId: {PatientId}", notification.PatientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PatientRegisterEventHandler.");
            }
        }
    }

}
