// <copyright file="ChatTemplateUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a chat template is updated.
/// </summary>
public class ChatTemplateUpdated : INotification
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
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user identifier who updated the template.
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
