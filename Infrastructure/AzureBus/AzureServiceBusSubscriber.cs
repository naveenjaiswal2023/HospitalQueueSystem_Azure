using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzureServiceBusSubscriber : IQueueSubscriber
    {
        private readonly ServiceBusProcessor _doctorQueueProcessor;
        private readonly ServiceBusProcessor _patientQueueProcessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public AzureServiceBusSubscriber(IConfiguration configuration, IServiceScopeFactory scopeFactory)
        {
            var connectionString = configuration["AzureServiceBus:ConnectionString"];
            var doctorSubscription = configuration["AzureServiceBus:DoctorQueueSubscription"];
            var patientQueue = configuration["AzureServiceBus:PatientQueue"];

            var client = new ServiceBusClient(connectionString);
            _doctorQueueProcessor = client.CreateProcessor(doctorSubscription);
            _patientQueueProcessor = client.CreateProcessor(patientQueue);

            _serviceScopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _doctorQueueProcessor.ProcessMessageAsync += HandleDoctorMessageAsync;
            _doctorQueueProcessor.ProcessErrorAsync += HandleErrorAsync;

            _patientQueueProcessor.ProcessMessageAsync += HandlePatientMessageAsync;
            _patientQueueProcessor.ProcessErrorAsync += HandleErrorAsync;

            await _doctorQueueProcessor.StartProcessingAsync(cancellationToken);
            await _patientQueueProcessor.StartProcessingAsync(cancellationToken);
        }

        private async Task HandleDoctorMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var doctorEvent = JsonSerializer.Deserialize<DoctorQueueCreatedEvent>(body);

            if (doctorEvent != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<DoctorQueueCreatedEventHandler>();
                await handler.HandleAsync(doctorEvent);
            }

            await args.CompleteMessageAsync(args.Message);
        }

        private async Task HandlePatientMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var patientEvent = JsonSerializer.Deserialize<PatientRegisteredEvent>(body);

            if (patientEvent != null)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<PatientRegisteredEventHandler>();
                await handler.HandleAsync(patientEvent);
            }

            await args.CompleteMessageAsync(args.Message);
        }

        private Task HandleErrorAsync(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Message handler encountered an error: {args.Exception.Message}");
            return Task.CompletedTask;
        }
    }
}
