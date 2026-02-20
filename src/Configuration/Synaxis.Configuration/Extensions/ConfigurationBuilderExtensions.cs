// <copyright file="ConfigurationBuilderExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Extensions;

using Microsoft.Extensions.Configuration;
using Synaxis.Configuration.Options;

/// <summary>
/// Extension methods for <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// The default configuration section name for cloud provider options.
    /// </summary>
    public const string CloudProviderSectionName = "CloudProvider";

    /// <summary>
    /// Adds cloud provider configuration to the configuration builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The configuration builder for method chaining.</returns>
    public static IConfigurationBuilder AddCloudProviderConfiguration(this IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    /// Gets the cloud provider options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The cloud provider options.</returns>
    public static CloudProviderOptions GetCloudProviderOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new CloudProviderOptions();
        configuration.GetSection(CloudProviderSectionName).Bind(options);
        return options;
    }

    /// <summary>
    /// Gets the Azure options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The Azure options, or null if not configured.</returns>
    public static AzureOptions? GetAzureOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new AzureOptions();
        configuration.GetSection($"{CloudProviderSectionName}:Azure").Bind(options);
        return string.IsNullOrEmpty(options.SubscriptionId) ? null : options;
    }

    /// <summary>
    /// Gets the AWS options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The AWS options, or null if not configured.</returns>
    public static AwsOptions? GetAwsOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new AwsOptions();
        configuration.GetSection($"{CloudProviderSectionName}:Aws").Bind(options);
        return string.IsNullOrEmpty(options.Region) ? null : options;
    }

    /// <summary>
    /// Gets the GCP options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The GCP options, or null if not configured.</returns>
    public static GcpOptions? GetGcpOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new GcpOptions();
        configuration.GetSection($"{CloudProviderSectionName}:Gcp").Bind(options);
        return string.IsNullOrEmpty(options.ProjectId) ? null : options;
    }

    /// <summary>
    /// Gets the on-premise options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The on-premise options, or null if not configured.</returns>
    public static OnPremiseOptions? GetOnPremiseOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new OnPremiseOptions();
        configuration.GetSection($"{CloudProviderSectionName}:OnPremise").Bind(options);
        return string.IsNullOrEmpty(options.ServerAddress) ? null : options;
    }

    /// <summary>
    /// Gets the event store options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The event store options, or null if not configured.</returns>
    public static EventStoreOptions? GetEventStoreOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new EventStoreOptions();
        configuration.GetSection($"{CloudProviderSectionName}:EventStore").Bind(options);
        return string.IsNullOrEmpty(options.Provider) ? null : options;
    }

    /// <summary>
    /// Gets the key vault options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The key vault options, or null if not configured.</returns>
    public static KeyVaultOptions? GetKeyVaultOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new KeyVaultOptions();
        configuration.GetSection($"{CloudProviderSectionName}:KeyVault").Bind(options);
        return string.IsNullOrEmpty(options.Provider) ? null : options;
    }

    /// <summary>
    /// Gets the message bus options from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The message bus options, or null if not configured.</returns>
    public static MessageBusOptions? GetMessageBusOptions(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var options = new MessageBusOptions();
        configuration.GetSection($"{CloudProviderSectionName}:MessageBus").Bind(options);
        return string.IsNullOrEmpty(options.Provider) ? null : options;
    }
}
