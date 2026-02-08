// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Synaxis.DependencyInjection;
using Synaxis.Providers.OpenAI.DependencyInjection;
using Synaxis.Transport.Grpc.DependencyInjection;
using Synaxis.Transport.Http.DependencyInjection;
using Synaxis.Transport.WebSocket.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Synaxis core services
builder.Services.AddSynaxis(options =>
{
    options.DefaultRoutingStrategy = "RoundRobin";
    options.EnableMetrics = true;
    options.EnableValidation = true;
});

// Add AI providers
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
if (!string.IsNullOrWhiteSpace(openAiApiKey))
{
    builder.Services.AddOpenAIProvider(openAiApiKey);
}

// Note: Add Anthropic provider when available
// var anthropicApiKey = builder.Configuration["Anthropic:ApiKey"];
// if (!string.IsNullOrWhiteSpace(anthropicApiKey))
// {
//     builder.Services.AddAnthropicProvider(anthropicApiKey);
// }

// Add all transport protocols
builder.Services.AddSynaxisTransportHttp();
builder.Services.AddSynaxisTransportGrpc();
builder.Services.AddSynaxisTransportWebSocket();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });

// Add Redis health check if configured
var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddHealthChecks()
        .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });

    // Add Redis caching
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}

// Add PostgreSQL health check if configured
var postgresConnection = builder.Configuration["PostgreSQL:ConnectionString"];
if (!string.IsNullOrWhiteSpace(postgresConnection))
{
    builder.Services.AddHealthChecks()
        .AddNpgSql(postgresConnection, name: "postgresql", tags: new[] { "ready" });
}

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Synaxis.Server"))
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Synaxis.*");

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new System.Uri(otlpEndpoint);
            });
        }
    });

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors();

// Add HTTP transport middleware
app.UseSynaxisTransportHttp();

// Add WebSocket support
app.UseSynaxisTransportWebSocket();

// Map health check endpoints
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    AllowCachingResponses = false
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    AllowCachingResponses = false
});

// Map Prometheus metrics endpoint
app.MapPrometheusScrapingEndpoint();

// Map HTTP endpoints
app.MapSynaxisTransportHttp();

// Map gRPC endpoints
app.MapSynaxisTransportGrpc();

// Map root endpoint
app.MapGet("/", () => Results.Ok(new
{
    Name = "Synaxis Self-Hosted Gateway",
    Version = "1.0.0",
    Status = "Running",
    Transports = new object[]
    {
        new { Type = "HTTP", Endpoints = new[] { "/v1/chat/completions", "/v1/embeddings" } },
        new { Type = "gRPC", Services = new[] { "synaxis.v1.ChatService", "synaxis.v1.EmbeddingsService" } },
        new { Type = "WebSocket", Path = "/ws" },
    },
    Health = new object[]
    {
        new { Path = "/health/ready", Description = "Readiness probe" },
        new { Path = "/health/live", Description = "Liveness probe" },
    },
    Metrics = new { Path = "/metrics", Format = "Prometheus" },
}));

await app.RunAsync();
