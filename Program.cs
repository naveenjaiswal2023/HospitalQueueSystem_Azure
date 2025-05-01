using AspNetCoreRateLimit;
using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Services;
using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.AzureBus;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.Infrastructure.Repositories;
using HospitalQueueSystem.WebAPI.Controllers;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HospitalQueueSystem.Domain.Events;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.AzureBlobStorage(
        connectionString: configuration["Logging:BlobStorage:ConnectionString"],
        storageContainerName: configuration["Logging:BlobStorage:ContainerName"],
        storageFileName: "log-{Date}.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information,
        useUtcTimeZone: true,
        bypassBlobCreationValidation: true 
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Add environment variables
builder.Configuration.AddEnvironmentVariables();

// Azure Key Vault integration
var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new Azure.Identity.VisualStudioCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// Retrieve the database password from the configuration
var qmsDbPassword = builder.Configuration["QmsDbPassword"];
var connTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
var actualConnectionString = connTemplate?.Replace("_QmsDbPassword_", qmsDbPassword);

// Register DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Use the dynamically generated connection string
    options.UseSqlServer(actualConnectionString,
        sqlOptions =>
        {
            // Replace the incorrect usage of EnableRetryOnFailure with the correct overload
            options.UseSqlServer(actualConnectionString,
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3, // Maximum retry attempts
                        maxRetryDelay: TimeSpan.FromSeconds(30), // Retry delay
                        errorNumbersToAdd: null); // Optional: specify additional error numbers to retry
                });
        });
});

// Azure Service Bus connection
var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING");
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Configuration["AzureServiceBus:ConnectionString"] = serviceBusConnectionString;
}

// Register ServiceBusClient (Singleton)
builder.Services.AddSingleton(serviceProvider =>
{
    var connectionString = builder.Configuration["AzureServiceBus:ConnectionString"];
    return new ServiceBusClient(connectionString);
});

// Register ServiceBusSender for patient-topic (Singleton)
builder.Services.AddSingleton(serviceProvider =>
{
    var serviceBusClient = serviceProvider.GetRequiredService<ServiceBusClient>();
    return serviceBusClient.CreateSender("patient-topic"); // Specify the topic name here
});

// Register ServiceBusSender for doctor-queue topic (Singleton)
builder.Services.AddSingleton(serviceProvider =>
{
    var serviceBusClient = serviceProvider.GetRequiredService<ServiceBusClient>();
    return serviceBusClient.CreateSender("doctor-queue"); // Specify the topic name here
});

// Register topics/subscriptions as List<TopicSubscriptionPair>
builder.Services.AddSingleton(new List<TopicSubscriptionPair>
{
    new TopicSubscriptionPair { TopicName = "patient-topic", SubscriptionName = "qms-subscription" },
    new TopicSubscriptionPair { TopicName = "doctor-queue", SubscriptionName = "doctor-queue-subscription" }
});

// Register AzurePublisher as Singleton
//builder.Services.AddSingleton<IPublisher, AzurePublisher>();

// Register other services
//builder.Services.AddScoped<PatientService>();  // Register PatientService
builder.Services.AddScoped<PatientController>();  // Register PatientController
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// Event Handlers
builder.Services.AddScoped<DoctorQueueCreatedEventHandler>();
//builder.Services.AddScoped<PatientRegisteredEventHandler>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RegisterPatientCommandHandler>();
});

// Azure Service Bus Publisher & Subscriber
builder.Services.AddSingleton<IPublisher, AzurePublisher>();
builder.Services.AddHostedService<AzureBusBackgroundService>();
builder.Services.AddSingleton<AzureServiceBusSubscriber>();

// SignalR
builder.Services.AddSignalR();

// 🧠 In-Memory Caching
builder.Services.AddMemoryCache(); // Register IMemoryCache
builder.Services.AddScoped<IPatientCacheService, PatientQueueCacheService>();

// Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true); // Allow all origins (for dev)
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable Routing before other middlewares
app.UseRouting();

// Exception handling middleware (ensure it's implemented)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Apply Rate Limiting after routing
app.UseIpRateLimiting();

app.UseCors();

app.UseAuthentication(); // Only if you're using JWT or similar
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<QueueHub>("/queuehub");

app.MapGet("/", () => Results.Ok("🏥 Hospital Queue System API is running"));

app.Run();
