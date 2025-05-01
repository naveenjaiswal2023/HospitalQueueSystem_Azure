using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System;
using System.Text.Json;

namespace HospitalQueueSystem.Infrastructure.AzureBus
{
    public class AzureServiceBusSubscriber
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly List<ServiceBusProcessor> _processors = new();

        public AzureServiceBusSubscriber(IConfiguration config, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            var client = new ServiceBusClient(config["AzureServiceBus:ConnectionString"]);

            _processors.Add(CreateProcessor<DoctorQueueCreatedEvent>(
                client,
                config["AzureServiceBus:DoctorQueueTopic"],
                config["AzureServiceBus:DoctorQueueSubscription"]
            ));

            _processors.Add(CreateProcessor<PatientRegisteredEvent>(
                client,
                config["AzureServiceBus:PatientTopic"],
                config["AzureServiceBus:PatientSubscription"]
            ));
        }

        private ServiceBusProcessor CreateProcessor<TEvent>(ServiceBusClient client, string topic, string subscription)
        {
            var processor = client.CreateProcessor(topic, subscription, new ServiceBusProcessorOptions());

            processor.ProcessMessageAsync += async args =>
            {
                var body = args.Message.Body.ToString();
                var @event = JsonSerializer.Deserialize<TEvent>(body);

                if (@event != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<ISubscriber<TEvent>>();
                    await handler.HandleAsync(@event);
                }

                await args.CompleteMessageAsync(args.Message);
            };

            processor.ProcessErrorAsync += args =>
            {
                Console.WriteLine($"Error processing message: {args.Exception}");
                return Task.CompletedTask;
            };

            return processor;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var processor in _processors)
            {
                await processor.StartProcessingAsync(cancellationToken);
            }
        }

        public async Task StopAsync()
        {
            foreach (var processor in _processors)
            {
                await processor.StopProcessingAsync();
                await processor.DisposeAsync();
            }
        }
    }
}
