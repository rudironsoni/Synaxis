// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;
using Synaxis.Configuration.StartupValidation;
using Synaxis.Configuration.Validators;

/// <summary>
/// Extension methods for registering Synaxis configuration services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Synaxis configuration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register all options validators
        RegisterValidators(services);

        // Register the startup filter for eager configuration validation
        services.AddTransient<IStartupFilter, ConfigurationStartupFilter>();

        return services;
    }

    /// <summary>
    /// Adds Synaxis configuration services with configuration binding.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure and validate options
        ConfigureOptions(services, configuration);

        // Register validators
        RegisterValidators(services);

        // Register the startup filter for eager configuration validation
        services.AddTransient<IStartupFilter, ConfigurationStartupFilter>();

        return services;
    }

    private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
    {
        // Configure CloudProviderOptions with validation
        ConfigureOption<CloudProviderOptions>(services, configuration, "CloudProvider");

        // Configure AzureOptions with validation
        ConfigureOption<AzureOptions>(services, configuration, "CloudProvider:Azure");

        // Configure AwsOptions with validation
        ConfigureOption<AwsOptions>(services, configuration, "CloudProvider:Aws");

        // Configure GcpOptions with validation
        ConfigureOption<GcpOptions>(services, configuration, "CloudProvider:Gcp");

        // Configure OnPremiseOptions with validation
        ConfigureOption<OnPremiseOptions>(services, configuration, "CloudProvider:OnPremise");

        // Configure EventStoreOptions with validation
        ConfigureOption<EventStoreOptions>(services, configuration, "EventStore");

        // Configure KeyVaultOptions with validation
        ConfigureOption<KeyVaultOptions>(services, configuration, "KeyVault");

        // Configure MessageBusOptions with validation
        ConfigureOption<MessageBusOptions>(services, configuration, "MessageBus");
    }

    private static void ConfigureOption<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services.Configure<TOptions>(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
        });
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        // Register all IValidateOptions<T> validators as singletons
        services.AddSingleton<IValidateOptions<CloudProviderOptions>, CloudProviderOptionsValidator>();
        services.AddSingleton<IValidateOptions<AzureOptions>, AzureOptionsValidator>();
        services.AddSingleton<IValidateOptions<AwsOptions>, AwsOptionsValidator>();
        services.AddSingleton<IValidateOptions<GcpOptions>, GcpOptionsValidator>();
        services.AddSingleton<IValidateOptions<OnPremiseOptions>, OnPremiseOptionsValidator>();
        services.AddSingleton<IValidateOptions<EventStoreOptions>, EventStoreOptionsValidator>();
        services.AddSingleton<IValidateOptions<KeyVaultOptions>, KeyVaultOptionsValidator>();
        services.AddSingleton<IValidateOptions<MessageBusOptions>, MessageBusOptionsValidator>();
    }
}
