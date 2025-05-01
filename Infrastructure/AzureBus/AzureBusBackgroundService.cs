using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
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
        var subject = args.Message.Subject; // Identifies the event type

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            switch (subject)
            {
                case nameof(PatientRegisteredEvent):
                    var patientEvent = JsonSerializer.Deserialize<PatientRegisteredEvent>(body);
                    if (patientEvent != null)
                    {
                        var queueService = scope.ServiceProvider.GetRequiredService<IPatientCacheService>();
                        await queueService.AddPatientToCacheAsync(patientEvent);
                        await _hubContext.Clients.All.SendAsync("NewPatientRegistered", patientEvent);
                    }
                    break;

                case nameof(DoctorQueueCreatedEvent):
                    //var calledEvent = JsonSerializer.Deserialize<DoctorQueueCreatedEvent>(body);
                    //if (calledEvent != null)
                    //{
                    //    var callService = scope.ServiceProvider.GetRequiredService<IPatientQueueCacheService>();
                    //    await callService.CallPatientAsync(calledEvent);
                    //    await _hubContext.Clients.All.SendAsync("PatientCalled", calledEvent);
                    //}
                    //break;
                    var callService = JsonSerializer.Deserialize<PatientRegisteredEvent>(body);
                    if (callService != null)
                    {
                        var queueService = scope.ServiceProvider.GetRequiredService<IPatientCacheService>();
                        await queueService.GetQueueAsync();
                        await _hubContext.Clients.All.SendAsync("NewPatientRegistered", callService);
                    }
                    break;

                //case nameof(DoctorAvailableEvent):
                //    var doctorEvent = JsonSerializer.Deserialize<DoctorAvailableEvent>(body);
                //    if (doctorEvent != null)
                //    {
                //        var docService = scope.ServiceProvider.GetRequiredService<IDoctorQueueService>();
                //        await docService.NotifyDoctorAvailableAsync(doctorEvent);
                //        await _hubContext.Clients.All.SendAsync("DoctorAvailable", doctorEvent);
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

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }
    }
}
