// <copyright file="MessageBusIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Common.Tests.Attributes;
using Synaxis.Providers.OnPrem;
using Xunit;

/// <summary>
/// Integration tests for Message Bus using RabbitMQ with TestContainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Infrastructure")]
public sealed class MessageBusIntegrationTests : IClassFixture<RabbitMqTestFixture>, IAsyncLifetime
{
    private readonly RabbitMqTestFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBusIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The RabbitMQ fixture.</param>
    public MessageBusIntegrationTests(RabbitMqTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _fixture.PurgeQueuesAsync();
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_PublishesMessageToRabbitMQ()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var message = new TestMessage { Id = Guid.NewGuid(), Content = "Test message" };
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            tcs.TrySetResult(true);
        });

        await bus.PublishAsync(message);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(tcs.Task, "Message should be received within 3 seconds");
        receivedMessages.Should().ContainSingle();
        receivedMessages.First().Content.Should().Be("Test message");
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task SubscribeAsync_ReceivesMessages()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var messages = new List<TestMessage>();
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();
        var count = 0;

        for (int i = 0; i < 3; i++)
        {
            messages.Add(new TestMessage { Id = Guid.NewGuid(), Content = $"Message {i}" });
        }

        // Act
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            if (Interlocked.Increment(ref count) == 3)
            {
                tcs.TrySetResult(true);
            }
        });

        foreach (var msg in messages)
        {
            await bus.PublishAsync(msg);
        }

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        completed.Should().Be(tcs.Task, "All messages should be received within 5 seconds");
        receivedMessages.Should().HaveCount(3);
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_WithTopic_RoutesToCorrectTopic()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var topicAReceived = new ConcurrentBag<string>();
        var topicBReceived = new ConcurrentBag<string>();
        var tcsA = new TaskCompletionSource<bool>();
        var tcsB = new TaskCompletionSource<bool>();

        // Act
        await bus.SubscribeAsync<TestMessage>("TopicA", async msg =>
        {
            topicAReceived.Add(msg.Content);
            tcsA.TrySetResult(true);
        });

        await bus.SubscribeAsync<TestMessage>("TopicB", async msg =>
        {
            topicBReceived.Add(msg.Content);
            tcsB.TrySetResult(true);
        });

        await bus.PublishAsync("TopicA", new TestMessage { Content = "To Topic A" });
        await bus.PublishAsync("TopicB", new TestMessage { Content = "To Topic B" });

        // Assert
        await Task.WhenAny(tcsA.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        await Task.WhenAny(tcsB.Task, Task.Delay(TimeSpan.FromSeconds(3)));

        topicAReceived.Should().ContainSingle().Which.Should().Be("To Topic A");
        topicBReceived.Should().ContainSingle().Which.Should().Be("To Topic B");
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task SubscribeAsync_HandlesMultipleSubscribers()
    {
        // Arrange
        await using var bus1 = await _fixture.CreateMessageBusAsync();
        await using var bus2 = await _fixture.CreateMessageBusAsync();
        var subscriber1Received = new ConcurrentBag<TestMessage>();
        var subscriber2Received = new ConcurrentBag<TestMessage>();
        var tcs1 = new TaskCompletionSource<bool>();
        var tcs2 = new TaskCompletionSource<bool>();

        // Act
        await bus1.SubscribeAsync<TestMessage>(async msg =>
        {
            subscriber1Received.Add(msg);
            tcs1.TrySetResult(true);
        });

        await bus2.SubscribeAsync<TestMessage>(async msg =>
        {
            subscriber2Received.Add(msg);
            tcs2.TrySetResult(true);
        });

        await bus1.PublishAsync(new TestMessage { Content = "Broadcast" });

        // Assert
        await Task.WhenAll(
            Task.WhenAny(tcs1.Task, Task.Delay(TimeSpan.FromSeconds(3))),
            Task.WhenAny(tcs2.Task, Task.Delay(TimeSpan.FromSeconds(3))));

        subscriber1Received.Should().ContainSingle();
        subscriber2Received.Should().ContainSingle();
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task SubscribeAsync_HandlesMessageProcessingErrors()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var messageProcessed = false;
        var tcs = new TaskCompletionSource<bool>();
        var attemptCount = 0;

        // Act - First handler throws, second succeeds after retry
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            if (Interlocked.Increment(ref attemptCount) == 1)
            {
                throw new InvalidOperationException("Simulated processing error");
            }

            messageProcessed = true;
            tcs.TrySetResult(true);
        });

        await bus.PublishAsync(new TestMessage { Content = "Error test" });

        // Wait for message to be processed (with retry)
        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // Assert - Message should eventually be processed due to retry
        messageProcessed.Should().BeTrue("Message should be requeued and processed after error");
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_WithLargeMessage()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var largeContent = new string('X', 100000); // 100KB message
        var message = new TestMessage { Id = Guid.NewGuid(), Content = largeContent };
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            tcs.TrySetResult(true);
        });

        await bus.PublishAsync(message);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(tcs.Task, "Large message should be received within 3 seconds");
        receivedMessages.Should().ContainSingle();
        receivedMessages.First().Content.Length.Should().Be(100000);
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_HandlesUnicodeContent()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var unicodeContent = "Hello 世界 🌍 Émojis: 🎉 🚀 Ñoño café";
        var message = new TestMessage { Id = Guid.NewGuid(), Content = unicodeContent };
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            tcs.TrySetResult(true);
        });

        await bus.PublishAsync(message);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(tcs.Task, "Unicode message should be received within 3 seconds");
        receivedMessages.Should().ContainSingle();
        receivedMessages.First().Content.Should().Be(unicodeContent);
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_HandlesComplexMessageTypes()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        var message = new ComplexTestMessage
        {
            Id = Guid.NewGuid(),
            Name = "Complex Test",
            Nested = new NestedObject { Value = 42, Data = "Nested data" },
            Tags = new[] { "tag1", "tag2", "tag3" }
        };
        var receivedMessages = new ConcurrentBag<ComplexTestMessage>();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        await bus.SubscribeAsync<ComplexTestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            tcs.TrySetResult(true);
        });

        await bus.PublishAsync(message);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(tcs.Task, "Complex message should be received within 3 seconds");
        receivedMessages.Should().ContainSingle();
        var received = receivedMessages.First();
        received.Name.Should().Be("Complex Test");
        received.Nested.Should().NotBeNull();
        received.Nested!.Value.Should().Be(42);
        received.Nested.Data.Should().Be("Nested data");
        received.Tags.Should().HaveCount(3);
    }

    [Fact]
    [SlopwatchSuppress("SW004", "Task.WhenAny with Task.Delay used as defensive timeout for async message bus operations - prevents test hangs")]
    public async Task PublishAsync_HandlesHighVolume()
    {
        // Arrange
        await using var bus = await _fixture.CreateMessageBusAsync();
        const int messageCount = 100;
        var receivedMessages = new ConcurrentBag<TestMessage>();
        var tcs = new TaskCompletionSource<bool>();
        var receivedCount = 0;

        // Act
        await bus.SubscribeAsync<TestMessage>(async msg =>
        {
            receivedMessages.Add(msg);
            if (Interlocked.Increment(ref receivedCount) == messageCount)
            {
                tcs.TrySetResult(true);
            }
        });

        var publishTasks = new List<Task>();
        for (int i = 0; i < messageCount; i++)
        {
            publishTasks.Add(bus.PublishAsync(new TestMessage
            {
                Id = Guid.NewGuid(),
                Content = $"Message {i}"
            }));
        }

        await Task.WhenAll(publishTasks);

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        completed.Should().Be(tcs.Task, "All messages should be received within 10 seconds");
        receivedMessages.Should().HaveCount(messageCount);
    }

    /// <summary>
    /// Verifies RabbitMQ container is running.
    /// </summary>
    [Fact]
    public async Task Container_IsRunning()
    {
        // Arrange & Act
        var connectionString = _fixture.ConnectionString;

        // Assert
        connectionString.Should().NotBeNullOrEmpty();
        connectionString.Should().Contain("amqp://");
    }

    /// <summary>
    /// Test message for messaging tests.
    /// </summary>
    public sealed class TestMessage
    {
        /// <summary>
        /// Gets or sets the message ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Complex test message with nested objects.
    /// </summary>
    public sealed class ComplexTestMessage
    {
        /// <summary>
        /// Gets or sets the message ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the nested object.
        /// </summary>
        public NestedObject? Nested { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Nested object for complex messages.
    /// </summary>
    public sealed class NestedObject
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public string Data { get; set; } = string.Empty;
    }
}
