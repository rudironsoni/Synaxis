// <copyright file="ModelConfigActivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a model configuration is activated.
/// </summary>
public class ModelConfigActivated : INotification
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
    /// Gets or sets the user identifier who activated the configuration.
    /// </summary>
    public string ActivatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the activation timestamp.
    /// </summary>
    public DateTime ActivatedAt { get; set; }
}
