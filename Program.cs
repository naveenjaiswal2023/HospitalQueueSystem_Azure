using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.AzureBus;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.Infrastructure.Repositories;
using HospitalQueueSystem.WebAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ Register ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// SignalR
builder.Services.AddSignalR();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDoctorQueueRepository, DoctorQueueRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
// Event Handlers
builder.Services.AddScoped<DoctorQueueCreatedEventHandler>();
builder.Services.AddScoped<PatientRegisteredEventHandler>();

// Azure Service Bus Publisher & Subscriber
builder.Services.AddSingleton<IQueuePublisher, AzureServiceBusPublisher>();
builder.Services.AddSingleton<IQueueSubscriber, AzureServiceBusSubscriber>();

// Hosted Service for Azure Subscriber
builder.Services.AddHostedService<AzureServiceBusBackgroundService>();

// CORS Policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(origin => true); // Allow all origins for development
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
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

app.Run();
