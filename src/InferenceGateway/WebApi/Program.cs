using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using DotNetEnv;
using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Extensions;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Extensions;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Microsoft.Extensions.Options;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.AspNetCore.OpenApi;
using Synaxis.InferenceGateway.WebApi.Agents;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity;
using Synaxis.InferenceGateway.WebApi.Endpoints.OpenAI;
using Synaxis.InferenceGateway.WebApi.Endpoints.Identity;

using Scalar.AspNetCore;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.WebApi.Health;
using StackExchange.Redis;
using Serilog;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics;



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
builder.Services.AddControlPlane(builder.Configuration);
builder.Services.AddSynaxisInfrastructure(builder.Configuration);
builder.Services.AddSynaxisApplication(builder.Configuration);
builder.Services.AddOpenApi();

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

builder.Services.AddScoped<RoutingService>();
builder.Services.AddScoped<RoutingAgent>();
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.AddOpenAIChatCompletions();
builder.AddOpenAIResponses();
builder.AddOpenAIConversations();

// Auth
var jwtSecret = builder.Configuration["Synaxis:InferenceGateway:JwtSecret"] ?? "SynaxisDefaultSecretKeyDoNotUseInProd1234567890";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

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
    await app.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    Log.Warning(ex, "Database initialization failed, continuing without migrations. Some features may be unavailable.");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseMiddleware<OpenAIErrorHandlerMiddleware>();
app.UseMiddleware<OpenAIMetadataMiddleware>();

    app.MapOpenAIEndpoints();


    app.MapAntigravityEndpoints();
    app.MapIdentityEndpoints();
    app.MapControllers();

// NOTE: Removed temporary debug endpoint that created AggregateException for testing.

app.MapHealthChecks("/health/liveness", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("liveness")
});

app.MapHealthChecks("/health/readiness", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("readiness")
});

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
