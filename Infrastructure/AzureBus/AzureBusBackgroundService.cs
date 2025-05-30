using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class AzureBusBackgroundService : BackgroundService
{
    private readonly List<ServiceBusProcessor> _processors = new();
    private readonly ILogger<AzureBusBackgroundService> _logger;
    private readonly ServiceBusClient _client;
    private readonly IHubContext<QueueHub> _hubContext;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly List<TopicSubscriptionPair> _topicSubscriptionPairs;

    public AzureBusBackgroundService(
        ServiceBusClient client,
        IServiceScopeFactory serviceScopeFactory,
        IHubContext<QueueHub> hubContext,
        List<TopicSubscriptionPair> topicSubscriptionPairs,
        ILogger<AzureBusBackgroundService> logger)
    {
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _topicSubscriptionPairs = topicSubscriptionPairs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var pair in _topicSubscriptionPairs)
        {
            var processor = _client.CreateProcessor(pair.TopicName, pair.SubscriptionName, new ServiceBusProcessorOptions());
            processor.ProcessMessageAsync += OnMessageReceived;
            processor.ProcessErrorAsync += ErrorHandler;
            _processors.Add(processor);
            await processor.StartProcessingAsync(stoppingToken);
        }
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var subject = args.Message.Subject;

        _logger.LogInformation($"Received message with subject: {subject}");
        _logger.LogDebug($"Message body: {body}");

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            switch (subject)
            {
                case nameof(PatientRegisteredEvent):
                    await HandleMessage<PatientRegisteredEvent>(body, args, "NewPatientRegistered", subject);
                    break;

                case nameof(PatientUpdatedEvent):
                    await HandleMessage<PatientUpdatedEvent>(body, args, "PatientUpdated", subject);
                    break;

                case nameof(PatientDeletedEvent):
                    await HandleMessage<PatientDeletedEvent>(body, args, "PatientDeleted", subject);
                    break;

                default:
                    _logger.LogWarning($"Unknown message subject: {subject}");
                    await args.DeadLetterMessageAsync(args.Message, "Unknown subject", subject);
                    break;
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unhandled error while processing message with subject: {subject}");
            await args.DeadLetterMessageAsync(args.Message, "Unhandled processing error", ex.Message);
        }
    }

    private async Task HandleMessage<T>(string body, ProcessMessageEventArgs args, string signalRMethod,string subject)
    {
        try
        {
            var message = JsonSerializer.Deserialize<T>(body);

            if (message == null)
            {
                _logger.LogWarning($"Deserialization of {typeof(T).Name} resulted in null.");
                await args.DeadLetterMessageAsync(args.Message, "Deserialization failure", $"Failed to deserialize {typeof(T).Name}");
                return;
            }

            //await _hubContext.Clients.All.SendAsync(signalRMethod, message);
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", subject, message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"Deserialization failed for type {typeof(T).Name}");
            await args.DeadLetterMessageAsync(args.Message, "JSON deserialization error", ex.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, $"Service Bus Error. Entity: {args.EntityPath}, Operation: {args.ErrorSource}");
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            try
            {
                if (processor != null)
                {
                    await processor.StopProcessingAsync(cancellationToken);
                    await processor.DisposeAsync();
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "ServiceBusProcessor already disposed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping ServiceBusProcessor.");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        foreach (var processor in _processors)
        {
            processor?.DisposeAsync().AsTask().Wait();
        }

        base.Dispose();
    }
}
