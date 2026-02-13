// <copyright file="WebSocketHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket.Handlers
{
    using System;
    using System.Buffers;
    using System.Net.WebSockets;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Transport.WebSocket.Protocol;

    /// <summary>
    /// Handles WebSocket connections for command execution.
    /// </summary>
    public class WebSocketHandler
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<WebSocketHandler> logger;
        private readonly WebSocketTransportOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandler"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The WebSocket transport options.</param>
        public WebSocketHandler(
            IServiceScopeFactory scopeFactory,
            ILogger<WebSocketHandler> logger,
            IOptions<WebSocketTransportOptions> options)
        {
            this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Handles a WebSocket connection.
        /// </summary>
        /// <param name="webSocket">The WebSocket connection.</param>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task HandleAsync(System.Net.WebSockets.WebSocket webSocket, HttpContext context)
        {
            if (webSocket is null)
            {
                throw new ArgumentNullException(nameof(webSocket));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cancellationToken = context.RequestAborted;
            var buffer = ArrayPool<byte>.Shared.Rent(this.options.ReceiveBufferSize);

            try
            {
                this.logger.LogInformation("WebSocket connection established");

                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        cancellationToken).ConfigureAwait(false);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            cancellationToken).ConfigureAwait(false);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageData = new byte[result.Count];
                        Array.Copy(buffer, messageData, result.Count);

                        await this.ProcessMessageAsync(webSocket, messageData, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                this.logger.LogInformation(ex, "WebSocket connection cancelled");
            }
            catch (WebSocketException ex)
            {
                this.logger.LogWarning(ex, "WebSocket error occurred");
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Unexpected error handling WebSocket connection");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                this.logger.LogInformation("WebSocket connection closed");
            }
        }

        private async Task ProcessMessageAsync(
            System.Net.WebSockets.WebSocket webSocket,
            byte[] data,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = await MessageSerializer.DeserializeAsync(data, cancellationToken).ConfigureAwait(false);
                this.logger.LogDebug("Received message: {Type} {CommandType}", message.Type, message.CommandType);

                if (string.Equals(message.Type, "command", StringComparison.Ordinal))
                {
                    await this.ExecuteCommandAsync(webSocket, message, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await this.SendErrorAsync(
                        webSocket,
                        $"Unknown message type: {message.Type}",
                        message.CorrelationId,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (JsonException ex)
            {
                this.logger.LogError(ex, "Failed to deserialize message");
                await this.SendErrorAsync(webSocket, "Invalid message format", null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to process message");
                await this.SendErrorAsync(webSocket, "Internal server error", null, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExecuteCommandAsync(
            System.Net.WebSockets.WebSocket webSocket,
            WebSocketMessage message,
            CancellationToken cancellationToken)
        {
            using var scope = this.scopeFactory.CreateScope();

            try
            {
                if (string.Equals(message.CommandType, "ChatCommand", StringComparison.Ordinal))
                {
                    await this.ExecuteChatCommandAsync(webSocket, message, scope, cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(message.CommandType, "ChatStreamCommand", StringComparison.Ordinal))
                {
                    await this.ExecuteChatStreamCommandAsync(webSocket, message, scope, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await this.SendErrorAsync(
                        webSocket,
                        $"Unknown command type: {message.CommandType}",
                        message.CorrelationId,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to execute command {CommandType}", message.CommandType);
                await this.SendErrorAsync(
                    webSocket,
                    $"Command execution failed: {ex.Message}",
                    message.CorrelationId,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExecuteChatCommandAsync(
            System.Net.WebSockets.WebSocket webSocket,
            WebSocketMessage message,
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(message.Payload.GetRawText()));
            var command = await JsonSerializer.DeserializeAsync<ChatCommand>(
                stream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken).ConfigureAwait(false);

            if (command is null)
            {
                await this.SendErrorAsync(
                    webSocket,
                    "Failed to deserialize chat command",
                    message.CorrelationId,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            var executor = scope.ServiceProvider.GetRequiredService<ICommandExecutor<ChatCommand, ChatResponse>>();
            var response = await executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            await this.SendResponseAsync(webSocket, response, message.CorrelationId, cancellationToken).ConfigureAwait(false);
        }

        private async Task ExecuteChatStreamCommandAsync(
            System.Net.WebSockets.WebSocket webSocket,
            WebSocketMessage message,
            IServiceScope scope,
            CancellationToken cancellationToken)
        {
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(message.Payload.GetRawText()));
            var command = await JsonSerializer.DeserializeAsync<ChatStreamCommand>(
                stream,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                cancellationToken).ConfigureAwait(false);

            if (command is null)
            {
                await this.SendErrorAsync(
                    webSocket,
                    "Failed to deserialize chat stream command",
                    message.CorrelationId,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            var executor = scope.ServiceProvider.GetRequiredService<IStreamExecutor<ChatStreamCommand, ChatStreamChunk>>();

            await foreach (var chunk in executor.ExecuteStreamAsync(command, cancellationToken).ConfigureAwait(false))
            {
                await this.SendResponseAsync(webSocket, chunk, message.CorrelationId, cancellationToken).ConfigureAwait(false);
            }
        }

        private Task SendResponseAsync<T>(
            System.Net.WebSockets.WebSocket webSocket,
            T response,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            var responseMessage = new WebSocketMessage
            {
                Type = "response",
                Payload = JsonSerializer.SerializeToElement(response),
                CorrelationId = correlationId,
            };

            var data = MessageSerializer.Serialize(responseMessage);
            return webSocket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
        }

        private Task SendErrorAsync(
            System.Net.WebSockets.WebSocket webSocket,
            string error,
            string? correlationId,
            CancellationToken cancellationToken)
        {
            var errorMessage = new WebSocketMessage
            {
                Type = "error",
                Payload = JsonSerializer.SerializeToElement(new { error }),
                CorrelationId = correlationId,
            };

            var data = MessageSerializer.Serialize(errorMessage);
            return webSocket.SendAsync(
                new ArraySegment<byte>(data),
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken);
        }
    }
}
