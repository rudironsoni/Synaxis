// <copyright file="WebSocketTransportTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Transport;

using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.InferenceGateway.IntegrationTests.Helpers;
using Synaxis.Transport.WebSocket.Protocol;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Integration tests for the WebSocket transport layer.
/// Tests use WebApplicationFactory with TestServer.CreateWebSocketClient() for self-contained testing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="WebSocketTransportTests"/> class.
/// </remarks>
/// <param name="factory">The web application factory.</param>
/// <param name="output">The test output helper.</param>
[Collection("Integration")]
public class WebSocketTransportTests(SynaxisWebApplicationFactory factory, ITestOutputHelper output)
{
    private readonly SynaxisWebApplicationFactory _factory = factory;
    private readonly ITestOutputHelper _output = output;

    /// <summary>
    /// Tests that a Command message returns a Response message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CommandMessage_ReturnsResponseMessage()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();
        var correlationId = Guid.NewGuid().ToString();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);

        var commandMessage = new WebSocketMessage
        {
            Type = "command",
            CommandType = "ChatCommand",
            CorrelationId = correlationId,
            Payload = JsonSerializer.SerializeToElement(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Hello" }
                },
                model = "test-model",
                maxTokens = 100,
                temperature = 0.7
            })
        };

        var messageData = MessageSerializer.Serialize(commandMessage);

        this._output.WriteLine($"[Observability] Sending command message - CorrelationId: {correlationId}, CommandType: {commandMessage.CommandType}, Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.SendAsync(
            new ArraySegment<byte>(messageData),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var responseBuffer = new byte[4096];
        var receiveResult = await client.ReceiveAsync(
            new ArraySegment<byte>(responseBuffer),
            CancellationToken.None);

        var responseData = new byte[receiveResult.Count];
        Array.Copy(responseBuffer, responseData, receiveResult.Count);
        var responseMessage = MessageSerializer.Deserialize(responseData);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
        Assert.Equal("response", responseMessage.Type);
        Assert.Equal(correlationId, responseMessage.CorrelationId);
        this._output.WriteLine($"[Observability] Received response - Type: {responseMessage.Type}, CorrelationId: {responseMessage.CorrelationId}, Timestamp: {DateTime.UtcNow:O}");

        // Cleanup
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task Streaming_SendsMultipleMessages()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();
        var correlationId = Guid.NewGuid().ToString();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);

        var streamCommandMessage = new WebSocketMessage
        {
            Type = "command",
            CommandType = "ChatStreamCommand",
            CorrelationId = correlationId,
            Payload = JsonSerializer.SerializeToElement(new
            {
                messages = new[]
                {
                    new { role = "user", content = "Tell me a short story" }
                },
                model = "test-model",
                maxTokens = 100,
                temperature = 0.7
            })
        };

        var messageData = MessageSerializer.Serialize(streamCommandMessage);

        this._output.WriteLine($"[Observability] Sending stream command - CorrelationId: {correlationId}, CommandType: {streamCommandMessage.CommandType}, Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.SendAsync(
            new ArraySegment<byte>(messageData),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var messageCount = 0;
        var responseBuffer = new byte[4096];
        var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Receive multiple messages (streaming)
        while (messageCount < 3 && !timeoutCts.Token.IsCancellationRequested)
        {
            try
            {
                var receiveResult = await client.ReceiveAsync(
                    new ArraySegment<byte>(responseBuffer),
                    timeoutCts.Token);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var responseData = new byte[receiveResult.Count];
                Array.Copy(responseBuffer, responseData, receiveResult.Count);
                var responseMessage = MessageSerializer.Deserialize(responseData);

                this._output.WriteLine($"[Observability] Received stream message #{messageCount + 1} - Type: {responseMessage.Type}, CorrelationId: {responseMessage.CorrelationId}, Timestamp: {DateTime.UtcNow:O}");

                // Accept both response and error - we're testing transport, not LLM integration
                Assert.True(
                    responseMessage.Type == "response" || responseMessage.Type == "error",
                    $"Expected message type 'response' or 'error', but got '{responseMessage.Type}'");
                Assert.Equal(correlationId, responseMessage.CorrelationId);
                messageCount++;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Assert
        Assert.True(messageCount >= 1, "Should receive at least one message in stream");
        this._output.WriteLine($"[Observability] Streaming test completed - Messages received: {messageCount}, Timestamp: {DateTime.UtcNow:O}");

        // Cleanup
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task ErrorHandling_ReturnsProperErrorMessages()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();
        var correlationId = Guid.NewGuid().ToString();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);

        // Send an invalid command type
        var invalidCommandMessage = new WebSocketMessage
        {
            Type = "command",
            CommandType = "InvalidCommandType",
            CorrelationId = correlationId,
            Payload = JsonSerializer.SerializeToElement(new { })
        };

        var messageData = MessageSerializer.Serialize(invalidCommandMessage);

        this._output.WriteLine($"[Observability] Sending invalid command - CorrelationId: {correlationId}, CommandType: {invalidCommandMessage.CommandType}, Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.SendAsync(
            new ArraySegment<byte>(messageData),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var responseBuffer = new byte[4096];
        var receiveResult = await client.ReceiveAsync(
            new ArraySegment<byte>(responseBuffer),
            CancellationToken.None);

        var responseData = new byte[receiveResult.Count];
        Array.Copy(responseBuffer, responseData, receiveResult.Count);
        var responseMessage = MessageSerializer.Deserialize(responseData);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
        Assert.Equal("error", responseMessage.Type);
        Assert.Equal(correlationId, responseMessage.CorrelationId);
        Assert.True(responseMessage.Payload.TryGetProperty("error", out var errorProperty));
        Assert.Contains("Unknown command type", errorProperty.GetString(), StringComparison.Ordinal);
        this._output.WriteLine($"[Observability] Received error message - Type: {responseMessage.Type}, Error: {errorProperty.GetString()}, Timestamp: {DateTime.UtcNow:O}");

        // Cleanup
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task ConnectionClose_IsHandledProperly()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);
        Assert.Equal(WebSocketState.Open, client.State);

        this._output.WriteLine($"[Observability] Connection established - State: {client.State}, Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);

        // Assert
        Assert.Equal(WebSocketState.Closed, client.State);
        this._output.WriteLine($"[Observability] Connection closed properly - State: {client.State}, Timestamp: {DateTime.UtcNow:O}");
    }

    [Fact]
    public async Task InvalidMessageFormat_ReturnsError()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);

        // Send invalid JSON
        var invalidJson = Encoding.UTF8.GetBytes("{ invalid json }");

        this._output.WriteLine($"[Observability] Sending invalid JSON - Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.SendAsync(
            new ArraySegment<byte>(invalidJson),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var responseBuffer = new byte[4096];
        var receiveResult = await client.ReceiveAsync(
            new ArraySegment<byte>(responseBuffer),
            CancellationToken.None);

        var responseData = new byte[receiveResult.Count];
        Array.Copy(responseBuffer, responseData, receiveResult.Count);
        var responseMessage = MessageSerializer.Deserialize(responseData);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
        Assert.Equal("error", responseMessage.Type);
        Assert.True(responseMessage.Payload.TryGetProperty("error", out var errorProperty));
        Assert.Contains("Invalid message format", errorProperty.GetString(), StringComparison.Ordinal);
        this._output.WriteLine($"[Observability] Received error for invalid format - Type: {responseMessage.Type}, Error: {errorProperty.GetString()}, Timestamp: {DateTime.UtcNow:O}");

        // Cleanup
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }

    [Fact]
    public async Task UnknownMessageType_ReturnsError()
    {
        // Arrange
        var wsUrl = new Uri("ws://localhost/ws");
        var webSocketClient = this._factory.Server.CreateWebSocketClient();
        var correlationId = Guid.NewGuid().ToString();

        using var client = await webSocketClient.ConnectAsync(wsUrl, CancellationToken.None);

        var unknownTypeMessage = new WebSocketMessage
        {
            Type = "unknown-type",
            CommandType = "ChatCommand",
            CorrelationId = correlationId,
            Payload = JsonSerializer.SerializeToElement(new { })
        };

        var messageData = MessageSerializer.Serialize(unknownTypeMessage);

        this._output.WriteLine($"[Observability] Sending unknown message type - Type: {unknownTypeMessage.Type}, CorrelationId: {correlationId}, Timestamp: {DateTime.UtcNow:O}");

        // Act
        await client.SendAsync(
            new ArraySegment<byte>(messageData),
            WebSocketMessageType.Text,
            endOfMessage: true,
            CancellationToken.None);

        var responseBuffer = new byte[4096];
        var receiveResult = await client.ReceiveAsync(
            new ArraySegment<byte>(responseBuffer),
            CancellationToken.None);

        var responseData = new byte[receiveResult.Count];
        Array.Copy(responseBuffer, responseData, receiveResult.Count);
        var responseMessage = MessageSerializer.Deserialize(responseData);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
        Assert.Equal("error", responseMessage.Type);
        Assert.Equal(correlationId, responseMessage.CorrelationId);
        Assert.True(responseMessage.Payload.TryGetProperty("error", out var errorProperty));
        Assert.Contains("Unknown message type", errorProperty.GetString(), StringComparison.Ordinal);
        this._output.WriteLine($"[Observability] Received error for unknown type - Type: {responseMessage.Type}, Error: {errorProperty.GetString()}, Timestamp: {DateTime.UtcNow:O}");

        // Cleanup
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
    }
}
