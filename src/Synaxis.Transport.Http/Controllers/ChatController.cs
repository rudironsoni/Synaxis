// <copyright file="ChatController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Contracts.V1.Commands;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Transport.Http.Mapping;

    /// <summary>
    /// Controller for chat completion endpoints.
    /// </summary>
    [ApiController]
    [Route("v1/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public ChatController(ILogger<ChatController> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a non-streaming chat completion.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A chat completion response.</returns>
        [HttpPost("completions")]
        [ProducesResponseType(typeof(ChatResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateChatCompletionAsync(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(this.Request.Body, Encoding.UTF8);
            var requestJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            this.logger.LogInformation("Received chat completion request");

            // Parse request to determine if streaming is requested
            var jsonDoc = JsonDocument.Parse(requestJson);
            var isStream = jsonDoc.RootElement.TryGetProperty("stream", out var streamProp) && streamProp.GetBoolean();

            if (isStream)
            {
                // Redirect to streaming endpoint
                return this.BadRequest(new { error = "Use streaming endpoint for stream=true requests" });
            }

            // Placeholder: RequestMapper will be implemented to map JSON to proper command types
            // and ICommandExecutor will execute the command
            // Placeholder implementation
            var response = new ChatResponse
            {
                Id = "chatcmpl-" + Guid.NewGuid().ToString("N"),
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = "placeholder-model",
                Choices = Array.Empty<ChatChoice>(),
                Usage = null,
            };

            return this.Ok(response);
        }

        /// <summary>
        /// Creates a streaming chat completion.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A stream of chat completion chunks.</returns>
        [HttpPost("completions/stream")]
        [Produces("text/event-stream")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async IAsyncEnumerable<string> CreateChatCompletionStreamAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(this.Request.Body, Encoding.UTF8);
            var requestJson = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            this.logger.LogInformation("Received streaming chat completion request");

            // Placeholder: RequestMapper will be implemented for streaming commands
            // and IStreamExecutor will handle the streaming execution
            // Use the requestJson variable to suppress unused variable warning
            _ = requestJson;

            // Placeholder implementation
            var chunk = new ChatStreamChunk
            {
                Id = "chatcmpl-" + Guid.NewGuid().ToString("N"),
                Object = "chat.completion.chunk",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = "placeholder-model",
                Choices = Array.Empty<ChatChoice>(),
            };

            yield return this.FormatSseEvent(chunk);
            yield return "data: [DONE]\n\n";
        }

        private string FormatSseEvent(ChatStreamChunk chunk)
        {
            var json = JsonSerializer.Serialize(chunk, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            return $"data: {json}\n\n";
        }
    }
}
