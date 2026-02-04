namespace Synaxis.InferenceGateway.Application.Configuration.Models;

/// <summary>
/// Represents a configuration setting value with its source.
/// </summary>
/// <typeparam name="T">The type of the setting value.</typeparam>
public class ConfigurationSetting<T>
{
    /// <summary>
    /// Gets or sets the setting value.
    /// </summary>
    public T? Value { get; set; }

    /// <summary>
    /// Gets or sets the source of the setting (User, Group, Organization, Global).
    /// </summary>
    public required string Source { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the setting was found.
    /// </summary>
    public bool Found { get; set; }
}

/// <summary>
/// Represents rate limit configuration.
/// </summary>
public class RateLimitConfiguration
{
    /// <summary>
    /// Gets or sets the requests per minute limit.
    /// </summary>
    public int? RequestsPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the tokens per minute limit.
    /// </summary>
    public int? TokensPerMinute { get; set; }

    /// <summary>
    /// Gets or sets the source of the configuration.
    /// </summary>
    public required string Source { get; set; }
}

/// <summary>
/// Represents cost configuration for a model.
/// </summary>
public class CostConfiguration
{
    /// <summary>
    /// Gets or sets the input cost per 1M tokens.
    /// </summary>
    public decimal InputCostPer1MTokens { get; set; }

    /// <summary>
    /// Gets or sets the output cost per 1M tokens.
    /// </summary>
    public decimal OutputCostPer1MTokens { get; set; }

    /// <summary>
    /// Gets or sets the source of the configuration.
    /// </summary>
    public required string Source { get; set; }
}
