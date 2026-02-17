// <copyright file="ModelConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Inference.Domain.Events;

/// <summary>
/// Aggregate root representing a model configuration.
/// </summary>
public class ModelConfig : AggregateRoot
{
    /// <summary>
    /// Gets the model configuration identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the model identifier (e.g., "gpt-4", "claude-3").
    /// </summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the model settings.
    /// </summary>
    public ModelSettings Settings { get; private set; } = new();

    /// <summary>
    /// Gets the pricing configuration.
    /// </summary>
    public ModelPricing Pricing { get; private set; } = new();

    /// <summary>
    /// Gets the capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; private set; } = new();

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets whether the model is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new model configuration.
    /// </summary>
    public static ModelConfig Create(
        Guid id,
        string modelId,
        string providerId,
        string displayName,
        string? description,
        ModelSettings settings,
        ModelPricing pricing,
        ModelCapabilities capabilities,
        Guid tenantId)
    {
        var config = new ModelConfig();
        var @event = new ModelConfigCreated
        {
            ConfigId = id,
            ModelId = modelId,
            ProviderId = providerId,
            DisplayName = displayName,
            Description = description,
            Settings = settings,
            Pricing = pricing,
            Capabilities = capabilities,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
        };

        config.ApplyEvent(@event);
        return config;
    }

    /// <summary>
    /// Updates the model settings.
    /// </summary>
    public void UpdateSettings(ModelSettings settings)
    {
        var @event = new ModelConfigUpdated
        {
            ConfigId = this.Id,
            DisplayName = this.DisplayName,
            Description = this.Description,
            Settings = settings,
            Pricing = this.Pricing,
            Capabilities = this.Capabilities,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the pricing.
    /// </summary>
    public void UpdatePricing(ModelPricing pricing)
    {
        var @event = new ModelPricingUpdated
        {
            ConfigId = this.Id,
            Pricing = pricing,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Activates the model.
    /// </summary>
    public void Activate()
    {
        if (this.IsActive)
        {
            return;
        }

        var @event = new ModelConfigActivated
        {
            ConfigId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deactivates the model.
    /// </summary>
    public void Deactivate()
    {
        if (!this.IsActive)
        {
            return;
        }

        var @event = new ModelConfigDeactivated
        {
            ConfigId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ModelConfigCreated created:
                this.ApplyCreated(created);
                break;
            case ModelConfigUpdated updated:
                this.ApplyUpdated(updated);
                break;
            case ModelPricingUpdated pricingUpdated:
                this.ApplyPricingUpdated(pricingUpdated);
                break;
            case ModelConfigActivated:
                this.ApplyActivated();
                break;
            case ModelConfigDeactivated:
                this.ApplyDeactivated();
                break;
        }
    }

    private void ApplyCreated(ModelConfigCreated @event)
    {
        this.Id = @event.ConfigId;
        this.ModelId = @event.ModelId;
        this.ProviderId = @event.ProviderId;
        this.DisplayName = @event.DisplayName;
        this.Description = @event.Description;
        this.Settings = @event.Settings;
        this.Pricing = @event.Pricing;
        this.Capabilities = @event.Capabilities;
        this.TenantId = @event.TenantId;
        this.IsActive = true;
        this.CreatedAt = @event.Timestamp;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyUpdated(ModelConfigUpdated @event)
    {
        this.DisplayName = @event.DisplayName;
        this.Description = @event.Description;
        this.Settings = @event.Settings;
        this.Pricing = @event.Pricing;
        this.Capabilities = @event.Capabilities;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyPricingUpdated(ModelPricingUpdated @event)
    {
        this.Pricing = @event.Pricing;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyActivated()
    {
        this.IsActive = true;
        this.UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDeactivated()
    {
        this.IsActive = false;
        this.UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents model settings.
/// </summary>
public class ModelSettings
{
    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the temperature.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the top P.
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the frequency penalty.
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the presence penalty.
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int ContextWindow { get; set; } = 8192;

    /// <summary>
    /// Gets or sets the stop sequences.
    /// </summary>
    public List<string> StopSequences { get; set; } = new();
}

/// <summary>
/// Represents model pricing.
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// Gets or sets the input token price per 1K tokens.
    /// </summary>
    public decimal InputPricePer1K { get; set; }

    /// <summary>
    /// Gets or sets the output token price per 1K tokens.
    /// </summary>
    public decimal OutputPricePer1K { get; set; }

    /// <summary>
    /// Gets or sets whether this is a free tier model.
    /// </summary>
    public bool IsFreeTier { get; set; }

    /// <summary>
    /// Gets or sets the free tier quota.
    /// </summary>
    public int? FreeTierQuota { get; set; }

    /// <summary>
    /// Calculates the cost for token usage.
    /// </summary>
    public decimal CalculateCost(int inputTokens, int outputTokens)
    {
        if (this.IsFreeTier)
        {
            return 0m;
        }

        var inputCost = (inputTokens / 1000m) * this.InputPricePer1K;
        var outputCost = (outputTokens / 1000m) * this.OutputPricePer1K;
        return inputCost + outputCost;
    }
}

/// <summary>
/// Represents model capabilities.
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// Gets or sets whether the model supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the model supports vision.
    /// </summary>
    public bool SupportsVision { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the model supports JSON mode.
    /// </summary>
    public bool SupportsJsonMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the supported languages.
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = new() { "en" };
}
