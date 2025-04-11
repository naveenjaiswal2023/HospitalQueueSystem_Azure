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
        private readonly ServiceBusProcessor _doctorProcessor;
        private readonly ServiceBusProcessor _patientProcessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public AzureServiceBusSubscriber(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory)
        {
            var client = new ServiceBusClient(configuration["AzureServiceBus:ConnectionString"]);
            _doctorProcessor = client.CreateProcessor(configuration["AzureServiceBus:DoctorQueue"]);
            _patientProcessor = client.CreateProcessor(configuration["AzureServiceBus:PatientQueue"]);
            _scopeFactory = scopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _doctorProcessor.ProcessMessageAsync += async args =>
            {
                var body = args.Message.Body.ToString();
                var doctorEvent = JsonSerializer.Deserialize<DoctorQueueCreatedEvent>(body);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var handler = scope.ServiceProvider.GetRequiredService<DoctorQueueCreatedEventHandler>();
                    if (doctorEvent != null)
                        await handler.HandleAsync(doctorEvent);
                }

                await args.CompleteMessageAsync(args.Message);
            };

            _doctorProcessor.ProcessErrorAsync += args => Task.CompletedTask;

            _patientProcessor.ProcessMessageAsync += async args =>
            {
                var body = args.Message.Body.ToString();
                var patientEvent = JsonSerializer.Deserialize<PatientRegisteredEvent>(body);

                using (var scope = _scopeFactory.CreateScope())
                {
                    var handler = scope.ServiceProvider.GetRequiredService<PatientRegisteredEventHandler>();
                    if (patientEvent != null)
                        await handler.HandleAsync(patientEvent);
                }

                await args.CompleteMessageAsync(args.Message);
            };

            _patientProcessor.ProcessErrorAsync += args => Task.CompletedTask;

            await _doctorProcessor.StartProcessingAsync(cancellationToken);
            await _patientProcessor.StartProcessingAsync(cancellationToken);
        }
    }
}
