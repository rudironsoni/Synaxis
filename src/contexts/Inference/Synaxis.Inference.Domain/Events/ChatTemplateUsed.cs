// <copyright file="ChatTemplateUsed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a chat template is used.
/// </summary>
public class ChatTemplateUsed : INotification
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who used the template.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the usage timestamp.
    /// </summary>
    public DateTime UsedAt { get; set; }
}
