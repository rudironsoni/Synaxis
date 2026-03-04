// <copyright file="ChatTemplateDeleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a chat template is deleted.
/// </summary>
public class ChatTemplateDeleted : INotification
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
    /// Gets or sets the user identifier who deleted the template.
    /// </summary>
    public string DeletedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime DeletedAt { get; set; }
}
