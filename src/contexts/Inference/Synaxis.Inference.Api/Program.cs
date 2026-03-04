// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.Inference.Api.Endpoints;
using Synaxis.Inference.Api.Middleware;

namespace Synaxis.Inference.Api;

/// <summary>
/// Entry point for the Synaxis Inference API.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await app.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add controllers
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
            });

        // Add API explorer and Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new()
            {
                Title = "Synaxis Inference API",
                Version = "v1",
                Description = "OpenAI-compatible inference API with streaming support",
            });
        });

        // Add MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Synaxis.Inference.Application.Commands.ExecuteInferenceCommand>();
        });

        // Add HTTP client
        services.AddHttpClient();

        // Add health checks
        services.AddHealthChecks();
    }

    /// <summary>
    /// Configures the middleware pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    private static void ConfigureMiddleware(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Custom middleware
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseAuthorization();

        // Map endpoints
        app.MapControllers();
        app.MapInferenceEndpoints();

        // Health checks
        app.MapHealthChecks("/health");
    }
}
