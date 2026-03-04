// <copyright file="UserChatPreferencesCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when user chat preferences are created.
/// </summary>
public class UserChatPreferencesCreated : INotification
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the preferred model identifier.
    /// </summary>
    public string? PreferredModelId { get; set; }

    /// <summary>
    /// Gets or sets the preferred provider identifier.
    /// </summary>
    public string? PreferredProviderId { get; set; }

    /// <summary>
    /// Gets or sets the default temperature.
    /// </summary>
    public double? DefaultTemperature { get; set; }

    /// <summary>
    /// Gets or sets the default maximum tokens.
    /// </summary>
    public int? DefaultMaxTokens { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
