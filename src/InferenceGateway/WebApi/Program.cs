using System.Text.Json;
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

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<RoutingAgent>();
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

var agent = app.Services.GetRequiredService<RoutingAgent>();
app.MapOpenAIEndpoints(agent);

app.MapAntigravityEndpoints();
app.MapControllers();

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
