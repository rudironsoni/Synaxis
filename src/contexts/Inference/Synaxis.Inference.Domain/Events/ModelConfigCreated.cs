// <copyright file="ModelConfigCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a model configuration is created.
/// </summary>
public class ModelConfigCreated : INotification
{
    /// <summary>
    /// Gets or sets the configuration identifier.
    /// </summary>
    public Guid ConfigId { get; set; }

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
    /// Gets or sets the pricing information.
    /// </summary>
    public ModelPricing? Pricing { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents model pricing information.
/// </summary>
public class ModelPricing
{
    /// <summary>
    /// Gets or sets the input token price.
    /// </summary>
    public decimal InputPrice { get; set; }

    /// <summary>
    /// Gets or sets the output token price.
    /// </summary>
    public decimal OutputPrice { get; set; }

    /// <summary>
    /// Gets or sets the currency.
    /// </summary>
    public string Currency { get; set; } = "USD";
}
