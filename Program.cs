using AspNetCoreRateLimit;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using HospitalQueueSystem.Application.Common;
using HospitalQueueSystem.Application.DTO;
using HospitalQueueSystem.Application.Handlers;
using HospitalQueueSystem.Application.Services;
using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using HospitalQueueSystem.Infrastructure.Events;
using HospitalQueueSystem.Infrastructure.Repositories;
using HospitalQueueSystem.Infrastructure.Seed;
using HospitalQueueSystem.Infrastructure.SignalR;
using HospitalQueueSystem.Shared.Utilities;
using HospitalQueueSystem.WebAPI.Controllers;
using HospitalQueueSystem.WebAPI.Hubs;
using HospitalQueueSystem.WebAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Bind appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

builder.Services.Configure<MaintenanceModeOptionsDto>(
    builder.Configuration.GetSection("MaintenanceMode"));


// Add Azure Key Vault secrets if VaultUrl is configured
var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new DefaultAzureCredential();

    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUrl), credential);
}

// Final configuration object (after Key Vault is loaded)
var configuration = builder.Configuration;

// Extract secrets
var azureServiceBusConnectionString = configuration["AzureServiceBusConnectionString"];
var blobStorageConnectionString = configuration["BlobStorageConnectionString"];
var blobContainerName = configuration["Logging:BlobStorage:ContainerName"];
var dbPassword = configuration["QmsDbPassword"];
var connTemplate = configuration.GetConnectionString("DefaultConnection");
var actualConnectionString = connTemplate?.Replace("_QmsDbPassword_", dbPassword);

// Configure Serilog with Azure Blob Storage
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.AzureBlobStorage(
        connectionString: blobStorageConnectionString,
        storageContainerName: blobContainerName,
        storageFileName: "log-{yyyyMMdd}.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information)
    .CreateLogger();

// Use Serilog as the app's logger
builder.Host.UseSerilog();

// Add environment variables
builder.Configuration.AddEnvironmentVariables();

// Register DB Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Use the dynamically generated connection string with retry logic
    options.UseSqlServer(actualConnectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,                         // Maximum retry attempts
            maxRetryDelay: TimeSpan.FromSeconds(30), // Retry delay
            errorNumbersToAdd: null);                // Optional: additional error codes to retry
    });
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtDto>();
builder.Services.Configure<JwtDto>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(jwtSettings.Key);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Azure Service Bus connection
//var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTIONSTRING");

if (!string.IsNullOrEmpty(azureServiceBusConnectionString))
{
    builder.Configuration["AzureServiceBus:ConnectionString"] = azureServiceBusConnectionString;
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

// Register other services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<PatientController>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();

// Event Handlers
builder.Services.AddScoped<DoctorQueueCreatedEventHandler>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RegisterPatientCommandHandler>();
});

// Azure Service Bus Publisher & Subscriber
builder.Services.AddHostedService<AzureBusBackgroundService>();
builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

// Register IHttpContextAccessor (for accessing user info in services)
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<IUserContextService, UserContextService>();

// SignalR
builder.Services.AddSignalR();

// In-Memory Caching
builder.Services.AddMemoryCache(); // Register IMemoryCache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

// Rate Limiting
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// CORS
//builder.Services.AddCors(options =>
//{
//    options.AddDefaultPolicy(policyBuilder =>
//    {
//        policyBuilder
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials()
//            .SetIsOriginAllowed(_ => true); // Allow all origins (for dev)
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .WithOrigins(
                "https://localhost:7026"      // React dev server
                //"https://localhost:4200",      // Angular dev server
                //"https://yourdomain.com",      // Production frontend
                //"https://www.yourdomain.com",  // Production frontend with www
                //"https://staging.yourdomain.com" // Staging environment
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hospital Queue System API",
        Version = "v1"
    });

    //Define the Bearer scheme
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your JWT token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsIn..."
    });

    //Apply the scheme globally
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Controllers
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await IdentitySeed.SeedRolesAndAdminAsync(services);
}
// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 🛡️ Global Exception Handling (should be FIRST to catch all exceptions)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 🌐 HTTPS Redirection (early in pipeline for security)
app.UseHttpsRedirection();

// 🛠️ Maintenance Mode Check (early check before processing requests)
app.UseMaintenanceMode();

// 🚦 Rate Limiting (protect your API early, before expensive operations)
app.UseIpRateLimiting();

// 🌐 Routing (establishes route context)
app.UseRouting();

// 🌍 CORS (after routing, before auth - needs route context)
app.UseCors();

// 🔐 Authentication (must come before authorization)
app.UseAuthentication();

// 🔐 Custom Unauthorized Middleware (after auth, before authorization)
// app.UseUnauthorizedMiddleware();

// 🔐 Authorization (must come after authentication)
app.UseAuthorization();

// 🚀 Response Caching (after auth/authz, before controllers)
// app.UseCachedResponse();

// 🧭 Endpoints (final step - actual request processing)
app.MapControllers();
app.MapHub<NotificationHub>("/NotificationHub");
app.MapGet("/", () => Results.Ok("🏥 Hospital Queue System API is running"));

app.Run();

public partial class Program { }  // This partial class is needed for WebApplicationFactory<T>