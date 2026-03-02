// <copyright file="ModelConfigEntity.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Persistence;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Entity representation of a model configuration for persistence.
/// </summary>
public class ModelConfigEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the model settings.
    /// </summary>
    public ModelSettings Settings { get; set; } = new();

    /// <summary>
    /// Gets or sets the model pricing.
    /// </summary>
    public ModelPricing Pricing { get; set; } = new();

    /// <summary>
    /// Gets or sets the model capabilities.
    /// </summary>
    public ModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the model is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Converts the entity to a domain aggregate.
    /// </summary>
    /// <returns>The domain aggregate.</returns>
    public ModelConfig ToDomain()
    {
        var config = ModelConfig.Create(
            this.Id,
            this.ModelId,
            this.ProviderId,
            this.DisplayName,
            this.Description,
            this.Settings,
            this.Pricing,
            this.Capabilities,
            this.TenantId);

        if (this.IsActive)
        {
            config.Activate();
        }
        else
        {
            config.Deactivate();
        }

        return config;
    }

    /// <summary>
    /// Creates an entity from a domain aggregate.
    /// </summary>
    /// <param name="config">The domain aggregate.</param>
    /// <returns>The entity.</returns>
    public static ModelConfigEntity FromDomain(ModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ModelConfigEntity
        {
            Id = config.Id,
            TenantId = config.TenantId,
            ModelId = config.ModelId,
            ProviderId = config.ProviderId,
            DisplayName = config.DisplayName,
            Description = config.Description,
            Settings = config.Settings,
            Pricing = config.Pricing,
            Capabilities = config.Capabilities,
            IsActive = config.IsActive,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt,
        };
    }
}
