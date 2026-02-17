// <copyright file="ChatTemplateCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when a chat template is created.
/// </summary>
public class ChatTemplateCreated : DomainEvent
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string TemplateContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template parameters.
    /// </summary>
    public IList<TemplateParameter> Parameters { get; set; } = new List<TemplateParameter>();

    /// <summary>
    /// Gets or sets the template category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system template.
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.TemplateId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ChatTemplateCreated);
}
