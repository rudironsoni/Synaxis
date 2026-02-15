// <copyright file="GcpMessageBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.GCP;

using System;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future GCP MessageBus integration using Pub/Sub.
/// </summary>
public class GcpMessageBus : IMessageBus
{
    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("GCP MessageBus integration is not yet implemented. This stub will use Pub/Sub for publish operations.");
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("GCP MessageBus integration is not yet implemented. This stub will use Pub/Sub topics for publish operations.");
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("GCP MessageBus integration is not yet implemented. This stub will use Pub/Sub subscriptions for subscribe operations.");
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("GCP MessageBus integration is not yet implemented. This stub will use Pub/Sub subscriptions for subscribe operations.");
    }
}
