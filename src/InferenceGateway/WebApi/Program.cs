// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using DotNetEnv;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Quartz;
using Scalar.AspNetCore;
using Serilog;
using StackExchange.Redis;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.Extensions;
using Synaxis.InferenceGateway.Application.RealTime;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.Extensions;
using Synaxis.InferenceGateway.Infrastructure.Jobs;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Endpoints.Admin;
using Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity;
using Synaxis.InferenceGateway.WebApi.Endpoints.Dashboard;
using Synaxis.InferenceGateway.WebApi.Endpoints.Identity;
using Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;
using Synaxis.InferenceGateway.WebApi.Health;
using Synaxis.InferenceGateway.WebApi.Hubs;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Synaxis.Transport.Grpc.DependencyInjection;
using Synaxis.Transport.Http.DependencyInjection;
using Synaxis.Transport.WebSocket.DependencyInjection;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

// Load .env files as early as possible so environment variables are available
DotNetEnv.Env.TraversePath().Load();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    // Map .env / Docker-style environment variables to configuration keys
    var envMapping = new Dictionary<string, string?>
    {
        { "Synaxis:InferenceGateway:Providers:Groq:Key", Environment.GetEnvironmentVariable("GROQ_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:Cohere:Key", Environment.GetEnvironmentVariable("COHERE_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:Cloudflare:Key", Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:Cloudflare:AccountId", Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID") },
        { "Synaxis:InferenceGateway:Providers:Gemini:Key", Environment.GetEnvironmentVariable("GEMINI_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:OpenRouter:Key", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:DeepSeek:Key", Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:DeepSeek:Endpoint", Environment.GetEnvironmentVariable("DEEPSEEK_API_ENDPOINT") },
        { "Synaxis:InferenceGateway:Providers:OpenAI:Key", Environment.GetEnvironmentVariable("OPENAI_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:OpenAI:Endpoint", Environment.GetEnvironmentVariable("OPENAI_API_ENDPOINT") },
        { "Synaxis:InferenceGateway:Providers:Antigravity:ProjectId", Environment.GetEnvironmentVariable("ANTIGRAVITY_PROJECT_ID") },
        { "Synaxis:InferenceGateway:Providers:Antigravity:Endpoint", Environment.GetEnvironmentVariable("ANTIGRAVITY_API_ENDPOINT") },
        { "Synaxis:InferenceGateway:Providers:Antigravity:FallbackEndpoint", Environment.GetEnvironmentVariable("ANTIGRAVITY_API_ENDPOINT_FALLBACK") },
        { "Synaxis:InferenceGateway:Providers:KiloCode:Key", Environment.GetEnvironmentVariable("KILOCODE_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:NVIDIA:Key", Environment.GetEnvironmentVariable("NVIDIA_API_KEY") },
        { "Synaxis:InferenceGateway:Providers:HuggingFace:Key", Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") },
    };

    // Filter out null or empty values so we don't overwrite other config values with nulls
    var filteredMapping = envMapping.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                    .ToDictionary(kv => kv.Key, kv => kv.Value);

    builder.Configuration.AddInMemoryCollection(filteredMapping);

    builder.Host.UseSerilog();

    // Configure faster shutdown for development
    builder.Services.Configure<HostOptions>(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(3);
        }
    });

    // 1. Add Services
    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Application.Interfaces.IOrganizationUserContext, Synaxis.InferenceGateway.Infrastructure.Auth.OrganizationUserContext>();
    builder.Services.AddControlPlane(builder.Configuration);
    builder.Services.AddSynaxisInfrastructure(builder.Configuration);
    builder.Services.AddSynaxisApplication(builder.Configuration);
    builder.Services.AddOpenApi();

    // Register transport services
    builder.Services.AddSynaxisTransportHttp();
    builder.Services.AddSynaxisTransportGrpc();
    builder.Services.AddSynaxisTransportWebSocket();

    // Register SynaxisDbContext for multi-tenant features
    builder.Services.AddDbContext<Synaxis.Infrastructure.Data.SynaxisDbContext>(options =>
    {
        var connectionString = builder.Configuration["Synaxis:ControlPlane:ConnectionString"];
        options.UseNpgsql(
            connectionString,
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Synaxis.Infrastructure");
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            });
    });

    // Register Synaxis services
    builder.Services.AddScoped<Synaxis.Core.Contracts.IUserService, Synaxis.Infrastructure.Services.UserService>();
    builder.Services.AddScoped<Synaxis.Core.Contracts.IPasswordService, Synaxis.Infrastructure.Services.PasswordService>();

    // Add ASP.NET Core Identity
    builder.Services.AddIdentity<SynaxisUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ControlPlaneDbContext>()
    .AddDefaultTokenProviders();

    // Quartz.NET - schedule periodic jobs (can be disabled for testing)
    var enableQuartz = builder.Configuration.GetValue<bool>("Synaxis:InferenceGateway:EnableQuartz", true);
    if (enableQuartz)
    {
        builder.Services.AddQuartz(q =>
        {
            // Create a job key for ModelsDevSyncJob
            var modelsDevSyncJobKey = new JobKey("ModelsDevSyncJob");

            // Register the job with the scheduler
            q.AddJob<ModelsDevSyncJob>(opts => opts.WithIdentity(modelsDevSyncJobKey));

            // Trigger: start now, repeat every 24 hours
            q.AddTrigger(opts => opts
                .ForJob(modelsDevSyncJobKey)
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromHours(24)).RepeatForever()));

            // Create a job key for ProviderDiscoveryJob
            var providerDiscoveryJobKey = new JobKey("ProviderDiscoveryJob");

            // Register the ProviderDiscoveryJob with the scheduler
            q.AddJob<ProviderDiscoveryJob>(opts => opts.WithIdentity(providerDiscoveryJobKey));

            // Trigger: start now, repeat every 1 hour
            q.AddTrigger(opts => opts
                .ForJob(providerDiscoveryJobKey)
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromHours(1)).RepeatForever()));

            // Health Monitoring - every 2 minutes
            var healthJobKey = new JobKey("HealthMonitoringJob");
            q.AddJob<HealthMonitoringAgent>(opts => opts.WithIdentity(healthJobKey));
            q.AddTrigger(opts => opts
                .ForJob(healthJobKey)
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMinutes(2)).RepeatForever()));

            // Cost Optimization - every 15 minutes
            var costJobKey = new JobKey("CostOptimizationJob");
            q.AddJob<CostOptimizationAgent>(opts => opts.WithIdentity(costJobKey));
            q.AddTrigger(opts => opts
                .ForJob(costJobKey)
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMinutes(15)).RepeatForever()));

            // Model Discovery - daily at 2 AM
            var discoveryJobKey = new JobKey("ModelDiscoveryJob");
            q.AddJob<ModelDiscoveryAgent>(opts => opts.WithIdentity(discoveryJobKey));
            q.AddTrigger(opts => opts
                .ForJob(discoveryJobKey)
                .StartNow()
                .WithCronSchedule("0 0 2 * * ?")); // 2 AM daily

            // Security Audit - every 6 hours
            var securityJobKey = new JobKey("SecurityAuditJob");
            q.AddJob<SecurityAuditAgent>(opts => opts.WithIdentity(securityJobKey));
            q.AddTrigger(opts => opts
                .ForJob(securityJobKey)
                .StartNow()
                .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromHours(6)).RepeatForever()));

            // Audit Log Partitioning - daily at 3 AM
            var auditPartitionJobKey = new JobKey("AuditLogPartitionJob");
            q.AddJob<AuditLogPartitionJob>(opts => opts.WithIdentity(auditPartitionJobKey));
            q.AddTrigger(opts => opts
                .ForJob(auditPartitionJobKey)
                .WithIdentity("AuditLogPartitionTrigger")
                .StartNow()
                .WithCronSchedule("0 0 3 * * ?")); // 3 AM daily
        });

        // Add hosted service to run Quartz and wait for jobs to complete on shutdown
        builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);
    }

    // OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddSource("Synaxis.InferenceGateway")
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("synaxis-gateway"))
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter();
        });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "liveness" })
        .AddDbContextCheck<ControlPlaneDbContext>(tags: new[] { "readiness" })
        .AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>(), tags: new[] { "readiness" })
        .AddCheck<ConfigHealthCheck>("config", tags: new[] { "readiness" })
        .AddCheck<ProviderConnectivityHealthCheck>("providers", tags: new[] { "readiness" });

    // Register Agent Tools
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IProviderTool, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.ProviderTool>();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IAlertTool, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.AlertTool>();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IRoutingTool, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.RoutingTool>();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IHealthTool, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.HealthTool>();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IAuditTool, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.AuditTool>();
    builder.Services.AddScoped<Synaxis.InferenceGateway.Infrastructure.Agents.Tools.IAgentTools, Synaxis.InferenceGateway.Infrastructure.Agents.Tools.AgentTools>();

    // Register Notification Service
    builder.Services.AddScoped<Synaxis.InferenceGateway.Application.ControlPlane.INotificationService, Synaxis.InferenceGateway.Infrastructure.ControlPlane.NotificationService>();

    builder.Services.AddScoped<RoutingService>();
    builder.Services.AddScoped<RoutingAgent>();
    builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
    builder.AddOpenAIChatCompletions();
    builder.AddOpenAIResponses();
    builder.AddOpenAIConversations();

    // Auth
    const string defaultJwtSecret = "SynaxisDefaultSecretKeyDoNotUseInProd1234567890";
    var jwtSecret = builder.Configuration["Synaxis:InferenceGateway:JwtSecret"];

    // Security: Fail fast in production if default JWT secret is used
    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        throw new InvalidOperationException(
            "Synaxis:InferenceGateway:JwtSecret must be configured. " +
            "Set a strong JWT secret in configuration or environment variable.");
    }

    if (string.Equals(jwtSecret, defaultJwtSecret) && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Default JWT secret detected. This is insecure for production. " +
            "Set a strong JWT secret in configuration or environment variable.");
    }

    if (string.Equals(jwtSecret, defaultJwtSecret) && builder.Environment.IsDevelopment())
    {
        Log.Warning("Using default JWT secret. This is insecure and should only be used in development.");
    }

    var key = Encoding.ASCII.GetBytes(jwtSecret);

    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        // Require HTTPS in production
        x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        x.SaveToken = true;
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            // Enable issuer/audience validation in production
            ValidateIssuer = !builder.Environment.IsDevelopment(),
            ValidateAudience = !builder.Environment.IsDevelopment(),
            ValidIssuer = builder.Configuration["Synaxis:InferenceGateway:JwtIssuer"] ?? "Synaxis",
            ValidAudience = builder.Configuration["Synaxis:InferenceGateway:JwtAudience"] ?? "Synaxis",
        };

        // Enable JWT authentication for WebSocket connections (SignalR)
        x.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // If the request is for SignalR hub and token is present
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs", StringComparison.Ordinal))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();

    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IRealTimeNotifier, RealTimeNotifier>();

    builder.Services.AddScoped<IRoutingScoreCalculator, RoutingScoreCalculator>();
    builder.Services.AddScoped<IFallbackOrchestrator, FallbackOrchestrator>();
    builder.Services.AddScoped<IProviderHealthCheckService, ProviderHealthCheckService>();
    builder.Services.AddScoped<IQuotaWarningService, QuotaWarningService>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WebApp", policy =>
        {
            var allowedOrigins = builder.Configuration["Synaxis:InferenceGateway:Cors:WebAppOrigins"]?.Split(',') ?? new[] { "http://localhost:8080" };
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });

        options.AddPolicy("PublicAPI", policy =>
        {
            var allowedOrigins = builder.Configuration["Synaxis:InferenceGateway:Cors:PublicOrigins"]?.Split(',') ?? Array.Empty<string>();
            if (allowedOrigins.Length > 0 && !string.Equals(allowedOrigins[0], "*"))
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else if (builder.Environment.IsDevelopment())
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
        });

        options.AddPolicy("Development", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins("http://localhost:5173", "http://localhost:8080")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

    var app = builder.Build();

    // Validate security configuration at startup
    using (var scope = app.Services.CreateScope())
    {
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecurityConfigurationValidator>>();
        var securityValidator = new SecurityConfigurationValidator(configuration, logger, app.Environment.EnvironmentName);
        var validationResult = securityValidator.Validate();

        if (!validationResult.IsValid)
        {
            Log.Fatal("Security configuration validation failed. Application cannot start.");
            foreach (var error in validationResult.Errors)
            {
                Log.Fatal("Security Error: {Error}", error);
            }

            throw new InvalidOperationException(
                $"Security configuration validation failed with {validationResult.Errors.Count} error(s). " +
                "Please fix the security issues before starting the application.");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    // Initialize the database on startup. Wrap in try/catch so the
    // application can still start in environments where the database
    // is not available (CI/local test runs). Migration failures are
    // logged but do not crash the host.
    try
    {
        await app.InitializeDatabaseAsync().ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database initialization failed, continuing without migrations. Some features may be unavailable.");
    }

    app.UseHttpsRedirection();

    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Enable CORS middleware - policies are applied per endpoint
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    app.Use(async (context, next) =>
    {
        context.Request.EnableBuffering();
        await next().ConfigureAwait(false);
    });

    app.UseMiddleware<RequestIdMiddleware>();
    app.UseMiddleware<OpenAIErrorHandlerMiddleware>();
    app.UseMiddleware<OpenAIMetadataMiddleware>();

    // Add transport middleware
    app.UseSynaxisTransportHttp();
    app.UseSynaxisTransportWebSocket();

    app.MapOpenAIEndpoints();
    app.MapAntigravityEndpoints();
    app.MapIdentityEndpoints();
    app.MapAdminEndpoints();
    app.MapConfigurationEndpoints();
    app.MapProvidersEndpoints();
    app.MapAnalyticsEndpoints();
    app.MapControllers();

    // Map transport endpoints
    app.MapSynaxisTransportGrpc();

    app.MapHub<ConfigurationHub>("/hubs/configuration");
    app.MapHub<SynaxisHub>("/hubs/synaxis");

    // NOTE: Removed temporary debug endpoint that created AggregateException for testing.
    app.MapHealthChecks("/health/liveness", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("liveness"),
    });

    app.MapHealthChecks("/health/readiness", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("readiness"),
    });

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

/// <summary>
/// The main program class for the Synaxis Inference Gateway web application.
/// This partial class enables programmatic access to the application for testing and hosting scenarios.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// Protected constructor to prevent direct instantiation.
    /// </summary>
    protected Program()
    {
    }
}
