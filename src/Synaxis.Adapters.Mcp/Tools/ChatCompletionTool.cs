// <copyright file="ChatCompletionTool.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Tools
{
    using System;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Commands.Chat;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// MCP tool for generating chat completions using AI models.
    /// </summary>
    public sealed class ChatCompletionTool : IMcpTool
    {
        private readonly ICommandExecutor<ChatCommand, ChatResponse> _executor;
        private readonly JsonDocument _inputSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatCompletionTool"/> class.
        /// </summary>
        /// <param name="executor">The chat command executor.</param>
        public ChatCompletionTool(ICommandExecutor<ChatCommand, ChatResponse> executor)
        {
            this._executor = executor!;

            // Define JSON schema for the tool's input parameters
            var schemaJson = """
            {
                "type": "object",
                "properties": {
                    "model": {
                        "type": "string",
                        "description": "The model ID to use for chat completion"
                    },
                    "messages": {
                        "type": "array",
                        "description": "Array of conversation messages",
                        "items": {
                            "type": "object",
                            "properties": {
                                "role": {
                                    "type": "string",
                                    "enum": ["user", "assistant", "system"]
                                },
                                "content": {
                                    "type": "string",
                                    "description": "The message content"
                                }
                            },
                            "required": ["role", "content"]
                        }
                    },
                    "temperature": {
                        "type": "number",
                        "description": "Sampling temperature (0.0-2.0)",
                        "minimum": 0.0,
                        "maximum": 2.0
                    },
                    "maxTokens": {
                        "type": "integer",
                        "description": "Maximum number of tokens to generate"
                    },
                    "provider": {
                        "type": "string",
                        "description": "Optional provider name override"
                    }
                },
                "required": ["model", "messages"]
            }
            """;

            this._inputSchema = JsonDocument.Parse(schemaJson);
        }

        /// <inheritdoc/>
        public string Name => "chat_completion";

        /// <inheritdoc/>
        public string Description => "Generate chat completions using AI models. Supports multi-turn conversations with configurable temperature and token limits.";

        /// <inheritdoc/>
        public JsonDocument InputSchema => this._inputSchema;

        /// <inheritdoc/>
        public async Task<object> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            var model = arguments.GetProperty("model").GetString()
                ?? throw new ArgumentException("Model is required", nameof(arguments));

            var messages = this.ParseMessages(arguments.GetProperty("messages"));
            var temperature = GetOptionalDouble(arguments, "temperature");
            var maxTokens = GetOptionalInt(arguments, "maxTokens");
            var provider = GetOptionalString(arguments, "provider");

            var command = new ChatCommand(
                Messages: messages,
                Model: model,
                Temperature: temperature,
                MaxTokens: maxTokens,
                Provider: provider);

            var result = await this._executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            return this.FormatChatResponse(result);
        }

        private ChatMessage[] ParseMessages(JsonElement messagesArray)
        {
            var messages = new ChatMessage[messagesArray.GetArrayLength()];

            for (int i = 0; i < messages.Length; i++)
            {
                var msg = messagesArray[i];
                var role = msg.GetProperty("role").GetString()
                    ?? throw new ArgumentException($"Message {i} role is required", nameof(messagesArray));
                var content = msg.GetProperty("content").GetString()
                    ?? throw new ArgumentException($"Message {i} content is required", nameof(messagesArray));

                messages[i] = new ChatMessage
                {
                    Role = role,
                    Content = content,
                };
            }

            return messages;
        }

        private static double? GetOptionalDouble(JsonElement arguments, string propertyName)
        {
            return arguments.TryGetProperty(propertyName, out var element) ? element.GetDouble() : null;
        }

        private static int? GetOptionalInt(JsonElement arguments, string propertyName)
        {
            return arguments.TryGetProperty(propertyName, out var element) ? element.GetInt32() : null;
        }

        private static string? GetOptionalString(JsonElement arguments, string propertyName)
        {
            return arguments.TryGetProperty(propertyName, out var element) ? element.GetString() : null;
        }

        private object FormatChatResponse(ChatResponse result)
        {
            var firstChoice = result.Choices.Length > 0 ? result.Choices[0] : null;
            return new
            {
                content = firstChoice?.Message.Content ?? string.Empty,
                role = firstChoice?.Message.Role ?? string.Empty,
                model = result.Model,
                finishReason = firstChoice?.FinishReason,
                usage = result.Usage is not null
                    ? new
                    {
                        promptTokens = result.Usage.PromptTokens,
                        completionTokens = result.Usage.CompletionTokens,
                        totalTokens = result.Usage.TotalTokens,
                    }
                    : null,
            };
        }
    }
}
