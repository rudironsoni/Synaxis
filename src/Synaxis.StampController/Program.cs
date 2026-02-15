// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.StampController.Controllers;
using Synaxis.StampController.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add Kubernetes client wrapper
builder.Services.AddSingleton<KubernetesClientWrapper>();

// Add stamp lifecycle service
builder.Services.AddSingleton<StampLifecycleService>();

// Add stamp controller as a background service
builder.Services.AddHostedService<StampController>();

// Configure options
builder.Services.Configure<KubernetesClientOptions>(builder.Configuration.GetSection("Kubernetes"));
builder.Services.Configure<StampLifecycleOptions>(builder.Configuration.GetSection("StampLifecycle"));
builder.Services.Configure<StampControllerOptions>(builder.Configuration.GetSection("StampController"));

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Stamp Controller starting...");

try
{
    await host.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    logger.LogError(ex, "Stamp Controller terminated unexpectedly");
    throw new InvalidOperationException("Stamp Controller terminated unexpectedly", ex);
}
finally
{
    logger.LogInformation("Stamp Controller stopped");
}
