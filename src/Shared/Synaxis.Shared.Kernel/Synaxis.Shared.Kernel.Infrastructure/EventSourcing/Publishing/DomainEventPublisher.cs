// <copyright file="DomainEventPublisher.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Publishing;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

/// <summary>
/// Publishes domain events to the MediatR notification pipeline.
/// </summary>
public sealed class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IPublisher _publisher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventPublisher"/> class.
    /// </summary>
    /// <param name="publisher">The MediatR publisher.</param>
    public DomainEventPublisher(IPublisher publisher)
    {
        this._publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : Synaxis.Shared.Kernel.Application.Commands.INotification
    {
        if (@event is null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        await _publisher.Publish(@event, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task PublishAllAsync(
        IEnumerable<Synaxis.Shared.Kernel.Application.Commands.INotification> events,
        CancellationToken cancellationToken = default)
    {
        if (events is null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        foreach (var @event in events)
        {
            await _publisher.Publish(@event, cancellationToken);
        }
    }
}

/// <summary>
/// Interface for publishing domain events.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes a single domain event.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PublishAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : Synaxis.Shared.Kernel.Application.Commands.INotification;

    /// <summary>
    /// Publishes multiple domain events.
    /// </summary>
    /// <param name="events">The events to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PublishAllAsync(
        IEnumerable<Synaxis.Shared.Kernel.Application.Commands.INotification> events,
        CancellationToken cancellationToken = default);
}
