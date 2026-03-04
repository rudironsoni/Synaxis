// <copyright file="ModelConfigUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a model configuration is updated.
/// </summary>
public class ModelConfigUpdated : INotification
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
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the configuration is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who updated the configuration.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
