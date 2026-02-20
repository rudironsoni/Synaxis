// <copyright file="MessageBusOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="MessageBusOptions"/> configuration.
/// </summary>
public class MessageBusOptionsValidator : IValidateOptions<MessageBusOptions>
{
    private static readonly string[] ValidProviders = ["RabbitMQ", "AzureServiceBus", "Kafka", "AWS_SQS", "AWS_SNS", "InMemory",];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MessageBusOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("MessageBus Provider is required.");
        }
        else if (!ValidProviders.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"MessageBus Provider '{options.Provider}' is not valid. Valid values are: {string.Join(", ", ValidProviders)}.");
        }

        // Connection string is not required for InMemory provider
        if (!string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add($"MessageBus ConnectionString is required for provider '{options.Provider}'.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
