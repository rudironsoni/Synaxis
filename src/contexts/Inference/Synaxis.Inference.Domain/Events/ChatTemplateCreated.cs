// <copyright file="ChatTemplateCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Mediator;

/// <summary>
/// Event raised when a chat template is created.
/// </summary>
public class ChatTemplateCreated : INotification
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
    /// Gets or sets a value indicating whether this is a system template.
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the user identifier who created the template.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
