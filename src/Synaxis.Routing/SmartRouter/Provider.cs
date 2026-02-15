namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents an AI provider that can be selected for routing.
/// </summary>
public class Provider
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the provider (e.g., "OpenAI", "Anthropic", "Azure OpenAI").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name (e.g., "gpt-4", "claude-3-opus").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the provider's API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of this provider (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether this provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cost per 1K input tokens.
    /// </summary>
    public decimal CostPer1KInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the cost per 1K output tokens.
    /// </summary>
    public decimal CostPer1KOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens supported by this provider.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the rate limit in requests per minute.
    /// </summary>
    public int RateLimitRpm { get; set; } = 60;
}
