// <copyright file="EventStoreOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="EventStoreOptions"/> configuration.
/// </summary>
public class EventStoreOptionsValidator : IValidateOptions<EventStoreOptions>
{
    private static readonly string[] ValidProviders = ["InMemory", "CosmosDB", "EventStoreDB", "PostgreSQL", "MongoDB"];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, EventStoreOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("EventStore Provider is required.");
        }
        else if (!ValidProviders.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"EventStore Provider '{options.Provider}' is not valid. Valid values are: {string.Join(", ", ValidProviders)}.");
        }

        // Connection string required for non-InMemory providers
        if (!string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add($"EventStore ConnectionString is required for provider '{options.Provider}'.");
        }

        // Validate snapshot interval
        if (options.SnapshotInterval < 1 || options.SnapshotInterval > 10000)
        {
            failures.Add("EventStore SnapshotInterval must be between 1 and 10000.");
        }

        // Validate batch size
        if (options.MaxBatchSize < 1 || options.MaxBatchSize > 1000)
        {
            failures.Add("EventStore MaxBatchSize must be between 1 and 1000.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
