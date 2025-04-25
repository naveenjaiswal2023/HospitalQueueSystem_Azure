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

// 🌐 Add environment variables
builder.Configuration.AddEnvironmentVariables();

var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    // Use Visual Studio Credential explicitly
    var credential = new VisualStudioCredential();
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// Read database password and replace
var dbPassword = builder.Configuration["QmsDbPassword"];
var connTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
var actualConnectionString = connTemplate.Replace("QmsDbPassword", dbPassword);
// Register DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(actualConnectionString));

// 🔌 Register ApplicationDbContext with the actual connection string
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(actualConnectionString));

// SignalR
builder.Services.AddSignalR();

// Service Bus connection string from environment
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

// Azure Service Bus
builder.Services.AddSingleton<IQueuePublisher, AzureServiceBusPublisher>();
builder.Services.AddSingleton<IQueueSubscriber, AzureServiceBusSubscriber>();
builder.Services.AddHostedService<AzureServiceBusBackgroundService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true); // dev only
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapHub<QueueHub>("/queuehub");

app.MapGet("/", () => Results.Ok("🏥 Hospital Queue System API is running"));

app.Run();
