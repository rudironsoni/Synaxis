// <copyright file="Program.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Reflection;
using k8s;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.StampController.CRDs;
using Synaxis.StampController.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

// Configure Kubernetes client
builder.Services.AddSingleton<IKubernetes>(_ =>
{
    var config = KubernetesClientConfiguration.InClusterConfig();
    return new Kubernetes(config);
});

// Register services
builder.Services.AddSingleton<ICrossStampMessaging, ServiceBusMessaging>();
builder.Services.AddSingleton<IStampProvisioner, AzureStampProvisioner>();
builder.Services.AddSingleton<IStateStore, CosmosStateStore>();

// Register hosted service
builder.Services.AddHostedService<StampController>();

// Configure K8s operator
builder.Services.AddKubernetesOperator(config =>
{
    config.Name = "stamp-controller";
    config.Namespace = builder.Configuration["WATCH_NAMESPACE"] ?? "synaxis";
    config.LabelSelector = "app.kubernetes.io/managed-by=stamp-controller";
});

var host = builder.Build();
host.Run();

/// <summary>
/// Extension methods for Kubernetes operator configuration.
/// </summary>
public static class KubernetesOperatorExtensions
{
    /// <summary>
    /// Adds Kubernetes operator services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddKubernetesOperator(
        this IServiceCollection services,
        Action<OperatorConfig> configure)
    {
        var config = new OperatorConfig();
        configure(config);
        services.AddSingleton(config);
        return services;
    }
}

/// <summary>
/// Configuration for the Kubernetes operator.
/// </summary>
public class OperatorConfig
{
    /// <summary>
    /// Gets or sets the operator name.
    /// </summary>
    public string Name { get; set; } = "stamp-controller";

    /// <summary>
    /// Gets or sets the namespace to watch.
    /// </summary>
    public string Namespace { get; set; } = "synaxis";

    /// <summary>
    /// Gets or sets the label selector for filtering resources.
    /// </summary>
    public string LabelSelector { get; set; } = string.Empty;
}
