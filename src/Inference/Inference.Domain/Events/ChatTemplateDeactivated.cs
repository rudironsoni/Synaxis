// <copyright file="ChatTemplateDeactivated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a chat template is deactivated.
/// </summary>
public class ChatTemplateDeactivated : DomainEvent
{
    /// <summary>
    /// Gets or sets the template identifier.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.TemplateId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(ChatTemplateDeactivated);
}
