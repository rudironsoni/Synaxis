// <copyright file="IMessageBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for publishing messages to a message bus.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes a message to a specific topic or channel.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="topic">The topic or channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The handler to process received messages.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Subscribes to messages from a specific topic or channel.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    /// <param name="topic">The topic or channel to subscribe to.</param>
    /// <param name="handler">The handler to process received messages.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class;
}
