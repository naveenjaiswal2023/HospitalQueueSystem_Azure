using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzureServiceBusPublisher : IQueuePublisher, IAsyncDisposable
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusSender _doctorSender;
        private readonly ServiceBusSender _patientSender;
        private readonly ILogger<AzureServiceBusPublisher> _logger;

        public AzureServiceBusPublisher(IConfiguration configuration, ILogger<AzureServiceBusPublisher> logger)
        {
            _client = new ServiceBusClient(configuration["AzureServiceBus:ConnectionString"]);
            _doctorSender = _client.CreateSender(configuration["AzureServiceBus:DoctorQueue"]);
            _patientSender = _client.CreateSender(configuration["AzureServiceBus:PatientQueue"]);
            _logger = logger;
        }

        public async Task PublishDoctorQueueAsync(DoctorQueueCreatedEvent @event)
        {
            try
            {
                var message = new ServiceBusMessage(JsonSerializer.Serialize(@event));
                await _doctorSender.SendMessageAsync(message);
                _logger.LogInformation("DoctorQueueCreatedEvent published successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing DoctorQueueCreatedEvent.");
                throw;
            }
        }

        public async Task PublishPatientRegisteredAsync(PatientRegisteredEvent @event)
        {
            try
            {
                var message = new ServiceBusMessage(JsonSerializer.Serialize(@event));
                await _patientSender.SendMessageAsync(message);
                _logger.LogInformation("PatientRegisteredEvent published successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing PatientRegisteredEvent.");
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _doctorSender.DisposeAsync();
            await _patientSender.DisposeAsync();
            await _client.DisposeAsync();
        }
    }
}
