// <copyright file="ModelConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Inference.Domain.Events;
using Synaxis.Infrastructure.EventSourcing;

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
    /// Gets a value indicating whether the model is active.
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
    /// <param name="id">The unique identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="description">The description.</param>
    /// <param name="settings">The model settings.</param>
    /// <param name="pricing">The pricing configuration.</param>
    /// <param name="capabilities">The model capabilities.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>A new model configuration instance.</returns>
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
    /// <param name="settings">The model settings.</param>
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
    /// <param name="pricing">The pricing configuration.</param>
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
