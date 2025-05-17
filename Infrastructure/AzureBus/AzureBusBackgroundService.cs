using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Services;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
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

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            switch (subject)
            {
                case nameof(PatientRegisteredEvent):
                    var registeredEvent = JsonSerializer.Deserialize<PatientRegisteredEvent>(body);
                    if (registeredEvent != null)
                    {
                        var cacheService = scope.ServiceProvider.GetRequiredService<IPatientCacheService>();
                        await cacheService.AddPatientToCacheAsync(registeredEvent);
                        await _hubContext.Clients.All.SendAsync("NewPatientRegistered", registeredEvent);
                    }
                    break;

                case nameof(PatientUpdatedEvent):
                    var updatedEvent = JsonSerializer.Deserialize<PatientUpdatedEvent>(body);
                    if (updatedEvent != null)
                    {
                        var cacheService = scope.ServiceProvider.GetRequiredService<IPatientCacheService>();
                        //await cacheService.UpdatePatientInCacheAsync(updatedEvent);
                        await _hubContext.Clients.All.SendAsync("PatientUpdated", updatedEvent);
                    }
                    break;

                case nameof(PatientDeletedEvent):
                    var deletedEvent = JsonSerializer.Deserialize<PatientDeletedEvent>(body);
                    if (deletedEvent != null)
                    {
                        //var cacheService = scope.ServiceProvider.GetRequiredService<IPatientCacheService>();
                        //await cacheService.RemovePatientFromCacheAsync(deletedEvent.PatientId);
                        await _hubContext.Clients.All.SendAsync("PatientDeleted", deletedEvent);
                    }
                    break;

                //case nameof(DoctorQueueCreatedEvent):
                //    var doctorQueueEvent = JsonSerializer.Deserialize<DoctorQueueCreatedEvent>(body);
                //    if (doctorQueueEvent != null)
                //    {
                //        var queueService = scope.ServiceProvider.GetRequiredService<IPatientQueueCacheService>();
                //        await queueService.CallPatientAsync(doctorQueueEvent);
                //        await _hubContext.Clients.All.SendAsync("PatientCalled", doctorQueueEvent);
                //    }
                //    break;

                default:
                    _logger.LogWarning($"Unknown message subject: {subject}");
                    break;
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message with subject: {subject}");
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, $"Service Bus Error: EntityPath={args.EntityPath}");
        return Task.CompletedTask;
    }

    //public override async Task StopAsync(CancellationToken cancellationToken)
    //{
    //    foreach (var processor in _processors)
    //    {
    //        await processor.StopProcessingAsync(cancellationToken);
    //        await processor.DisposeAsync();
    //    }
    //}
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            try
            {
                if (processor is not null)
                {
                    // Avoid calling StopProcessing on an already disposed processor
                    await processor.StopProcessingAsync(cancellationToken);
                    await processor.DisposeAsync();
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "ServiceBusProcessor was already disposed during shutdown.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while stopping ServiceBusProcessor.");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    private bool _disposed = false;

    public override void Dispose()
    {
        _disposed = true;
        base.Dispose();
    }

}
