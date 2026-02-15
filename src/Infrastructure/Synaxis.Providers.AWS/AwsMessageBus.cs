// <copyright file="AwsMessageBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.AWS;

using System;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future AWS MessageBus integration using SQS and SNS.
/// </summary>
public class AwsMessageBus : IMessageBus
{
    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("AWS MessageBus integration is not yet implemented. This stub will use SNS for publish operations.");
    }

    /// <inheritdoc />
    public Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("AWS MessageBus integration is not yet implemented. This stub will use SNS topics for publish operations.");
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("AWS MessageBus integration is not yet implemented. This stub will use SQS queues for subscribe operations.");
    }

    /// <inheritdoc />
    public Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        throw new NotSupportedException("AWS MessageBus integration is not yet implemented. This stub will use SNS subscriptions and SQS queues for subscribe operations.");
    }
}
