using AspNetCoreRateLimit;
using Azure.Identity;
using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.AzureBus;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.Infrastructure.Repositories;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables
builder.Configuration.AddEnvironmentVariables();

// Azure Key Vault integration
var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new VisualStudioCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// Replace database password placeholder dynamically
var dbPassword = builder.Configuration["QmsDbPassword"];
var connTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
var actualConnectionString = connTemplate.Replace("QmsDbPassword", dbPassword);

// Register DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(actualConnectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()
    )
);

// Azure Service Bus connection from environment variable
var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING");
if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Configuration["AzureServiceBus:ConnectionString"] = serviceBusConnectionString;
}

// Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDoctorQueueRepository, DoctorQueueRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<DoctorQueueCreatedEventHandler>();
builder.Services.AddScoped<PatientRegisteredEventHandler>();

builder.Services.AddSingleton<IQueuePublisher, AzureServiceBusPublisher>();
builder.Services.AddSingleton<IQueueSubscriber, AzureServiceBusSubscriber>();
builder.Services.AddHostedService<AzureServiceBusBackgroundService>();

// SignalR
builder.Services.AddSignalR();

// Rate Limiting
builder.Services.AddMemoryCache(); // REQUIRED for AspNetCoreRateLimit
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
            .SetIsOriginAllowed(_ => true); // Allow all origins (dev only)
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
app.UseRouting();
app.UseCors();
app.UseIpRateLimiting();   // ➔ Must be after CORS, before Authorization
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<QueueHub>("/queuehub");

app.MapGet("/", () => Results.Ok("🏥 Hospital Queue System API is running"));

app.Run();
