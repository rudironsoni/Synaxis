// <copyright file="MockMessageBus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Doubles;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// In-memory mock implementation of <see cref="IMessageBus"/> for testing.
/// Provides thread-safe message publishing and subscription capabilities.
/// </summary>
public sealed class MockMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _typeSubscribers = new();
    private readonly ConcurrentDictionary<string, List<Delegate>> _topicSubscribers = new();
    private readonly ConcurrentBag<PublishedMessage> _publishedMessages = new();

    /// <summary>
    /// Publishes a message to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PublishAsync<TMessage>(
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(message);

        cancellationToken.ThrowIfCancellationRequested();

        var publishedMessage = new PublishedMessage
        {
            Topic = null,
            MessageType = typeof(TMessage),
            Message = message,
            Timestamp = DateTime.UtcNow,
        };

        _publishedMessages.Add(publishedMessage);

        // Notify type-based subscribers
        if (_typeSubscribers.TryGetValue(typeof(TMessage), out var handlers))
        {
            foreach (var handler in handlers.Cast<Func<TMessage, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(message).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore handler exceptions in tests
                    }
                }, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Publishes a message to a specific topic or channel.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to publish.</typeparam>
    /// <param name="topic">The topic or channel to publish to.</param>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task PublishAsync<TMessage>(
        string topic,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        ArgumentNullException.ThrowIfNull(message);

        cancellationToken.ThrowIfCancellationRequested();

        var publishedMessage = new PublishedMessage
        {
            Topic = topic,
            MessageType = typeof(TMessage),
            Message = message,
            Timestamp = DateTime.UtcNow,
        };

        _publishedMessages.Add(publishedMessage);

        // Notify topic-based subscribers
        if (_topicSubscribers.TryGetValue(topic, out var handlers))
        {
            foreach (var handler in handlers.Cast<Func<TMessage, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(message).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore handler exceptions in tests
                    }
                }, cancellationToken);
            }
        }

        // Also notify type-based subscribers
        if (_typeSubscribers.TryGetValue(typeof(TMessage), out var typeHandlers))
        {
            foreach (var handler in typeHandlers.Cast<Func<TMessage, Task>>())
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await handler(message).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Ignore handler exceptions in tests
                    }
                }, cancellationToken);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    /// <param name="handler">The handler to process received messages.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SubscribeAsync<TMessage>(
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        cancellationToken.ThrowIfCancellationRequested();

        var handlers = _typeSubscribers.GetOrAdd(typeof(TMessage), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribes to messages from a specific topic or channel.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to subscribe to.</typeparam>
    /// <param name="topic">The topic or channel to subscribe to.</param>
    /// <param name="handler">The handler to process received messages.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SubscribeAsync<TMessage>(
        string topic,
        Func<TMessage, Task> handler,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);
        ArgumentNullException.ThrowIfNull(handler);

        cancellationToken.ThrowIfCancellationRequested();

        var handlers = _topicSubscribers.GetOrAdd(topic, _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all published messages and subscribers.
    /// </summary>
    public void Clear()
    {
        _typeSubscribers.Clear();
        _topicSubscribers.Clear();

        while (_publishedMessages.TryTake(out _))
        {
        }
    }

    /// <summary>
    /// Gets all published messages.
    /// </summary>
    /// <returns>A read-only list of published messages.</returns>
    public IReadOnlyList<PublishedMessage> GetPublishedMessages()
    {
        return _publishedMessages.ToList();
    }

    /// <summary>
    /// Gets published messages of a specific type.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to filter by.</typeparam>
    /// <returns>A read-only list of matching messages.</returns>
    public IReadOnlyList<PublishedMessage> GetPublishedMessages<TMessage>()
        where TMessage : class
    {
        return _publishedMessages
            .Where(m => m.MessageType == typeof(TMessage))
            .ToList();
    }

    /// <summary>
    /// Gets published messages for a specific topic.
    /// </summary>
    /// <param name="topic">The topic to filter by.</param>
    /// <returns>A read-only list of matching messages.</returns>
    public IReadOnlyList<PublishedMessage> GetPublishedMessages(string topic)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);

        return _publishedMessages
            .Where(m => m.Topic == topic)
            .ToList();
    }

    /// <summary>
    /// Gets the count of published messages.
    /// </summary>
    /// <returns>The number of published messages.</returns>
    public int PublishedMessageCount => _publishedMessages.Count;

    /// <summary>
    /// Gets the count of type-based subscribers for a message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The number of subscribers.</returns>
    public int GetSubscriberCount<TMessage>()
        where TMessage : class
    {
        return _typeSubscribers.TryGetValue(typeof(TMessage), out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Gets the count of topic-based subscribers.
    /// </summary>
    /// <param name="topic">The topic.</param>
    /// <returns>The number of subscribers.</returns>
    public int GetSubscriberCount(string topic)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);

        return _topicSubscribers.TryGetValue(topic, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Represents a published message.
    /// </summary>
    public sealed class PublishedMessage
    {
        /// <summary>
        /// Gets or sets the topic the message was published to.
        /// </summary>
        public string? Topic { get; init; }

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public Type MessageType { get; init; } = typeof(object);

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public object Message { get; init; } = new();

        /// <summary>
        /// Gets or sets the timestamp when the message was published.
        /// </summary>
        public DateTime Timestamp { get; init; }
    }
}
