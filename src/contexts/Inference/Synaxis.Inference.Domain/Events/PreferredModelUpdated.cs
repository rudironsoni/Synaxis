// <copyright file="PreferredModelUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a user's preferred model is updated.
/// </summary>
public class PreferredModelUpdated : INotification
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

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
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
