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
using Azure.Identity;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HospitalQueueSystem.Infrastructure.Seed;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Bind appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// Add Azure Key Vault secrets if VaultUrl is configured
var keyVaultUrl = builder.Configuration["AzureKeyVault:VaultUrl"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
    var credential = new VisualStudioCredential(); // Or AzureCliCredential / DefaultAzureCredential
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
    .WriteTo.AzureBlobStorage(
        connectionString: blobStorageConnectionString,
        storageContainerName: blobContainerName,
        storageFileName: "log-{Date}.txt",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information,
        useUtcTimeZone: true
    )
    .CreateLogger();

// Use Serilog as the app's logger
builder.Host.UseSerilog();

// Add environment variables
builder.Configuration.AddEnvironmentVariables();


// Retrieve the database password from the configuration
//var qmsDbPassword = builder.Configuration["QmsDbPassword"];
//var connTemplate = builder.Configuration.GetConnectionString("DefaultConnection");
//var actualConnectionString = connTemplate?.Replace("_QmsDbPassword_", qmsDbPassword);

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

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtDTO>();
builder.Services.Configure<JwtDTO>(builder.Configuration.GetSection("JwtSettings"));

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

// Register AzurePublisher as Singleton
//builder.Services.AddSingleton<IPublisher, AzurePublisher>();

// Register other services
builder.Services.AddScoped<ITokenService, TokenService>();
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

app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseIpRateLimiting();
app.UseCors();
app.UseAuthentication(); // Only if you're using JWT or similar
app.UseAuthorization();
// Endpoints
app.MapControllers();
app.MapHub<QueueHub>("/queuehub");
app.MapGet("/", () => Results.Ok("🏥 Hospital Queue System API is running"));
app.Run();
