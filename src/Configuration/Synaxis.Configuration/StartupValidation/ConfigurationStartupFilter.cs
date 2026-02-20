// <copyright file="ConfigurationStartupFilter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.StartupValidation;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Startup filter that validates all required configuration at application startup.
/// Fails fast with clear error messages if configuration is invalid.
/// </summary>
public class ConfigurationStartupFilter : IStartupFilter
{
    private readonly ILogger<ConfigurationStartupFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationStartupFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ConfigurationStartupFilter(ILogger<ConfigurationStartupFilter> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            this.ValidateConfiguration(builder);
            next(builder);
        };
    }

    private void ValidateConfiguration(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        this._logger.ConfigurationValidationStarted();

        var validationErrors = new List<string>();

        this.ValidateOption<CloudProviderOptions>(serviceProvider, validationErrors, "CloudProvider");
        this.ValidateOption<AzureOptions>(serviceProvider, validationErrors, "Azure");
        this.ValidateOption<AwsOptions>(serviceProvider, validationErrors, "AWS");
        this.ValidateOption<GcpOptions>(serviceProvider, validationErrors, "GCP");
        this.ValidateOption<OnPremiseOptions>(serviceProvider, validationErrors, "OnPremise");
        this.ValidateOption<EventStoreOptions>(serviceProvider, validationErrors, "EventStore");
        this.ValidateOption<KeyVaultOptions>(serviceProvider, validationErrors, "KeyVault");
        this.ValidateOption<MessageBusOptions>(serviceProvider, validationErrors, "MessageBus");

        if (validationErrors.Count > 0)
        {
            this._logger.ConfigurationValidationFailed(validationErrors.Count);
            throw new InvalidOperationException(
                $"Configuration validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, validationErrors.Select(e => $"  - {e}"))}");
        }

        this._logger.ConfigurationValidationSucceeded();
    }

    private void ValidateOption<TOptions>(
        IServiceProvider serviceProvider,
        List<string> validationErrors,
        string optionName)
        where TOptions : class
    {
        try
        {
            _ = serviceProvider.GetService<IOptions<TOptions>>()?.Value;
        }
        catch (OptionsValidationException ex)
        {
            validationErrors.AddRange(ex.Failures.Select(f => $"[{optionName}] {f}"));
        }
        catch (InvalidOperationException)
        {
            this._logger.ConfigurationOptionNotRegistered(optionName);
        }
    }
}
