// <copyright file="EmbeddingTool.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.Tools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;

    /// <summary>
    /// MCP tool for generating text embeddings using AI models.
    /// </summary>
    public sealed class EmbeddingTool : IMcpTool
    {
        private readonly ICommandExecutor<EmbeddingCommand, EmbeddingResponse> _executor;
        private readonly JsonDocument _inputSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmbeddingTool"/> class.
        /// </summary>
        /// <param name="executor">The embedding command executor.</param>
        public EmbeddingTool(ICommandExecutor<EmbeddingCommand, EmbeddingResponse> executor)
        {
            this._executor = executor!;

            // Define JSON schema for the tool's input parameters
            var schemaJson = """
            {
                "type": "object",
                "properties": {
                    "model": {
                        "type": "string",
                        "description": "The model ID to use for generating embeddings"
                    },
                    "input": {
                        "oneOf": [
                            {
                                "type": "string",
                                "description": "Single text input to embed"
                            },
                            {
                                "type": "array",
                                "description": "Array of text inputs to embed",
                                "items": {
                                    "type": "string"
                                }
                            }
                        ]
                    },
                    "provider": {
                        "type": "string",
                        "description": "Optional provider name override"
                    }
                },
                "required": ["model", "input"]
            }
            """;

            this._inputSchema = JsonDocument.Parse(schemaJson);
        }

        /// <inheritdoc/>
        public string Name => "embedding";

        /// <inheritdoc/>
        public string Description => "Generate text embeddings using AI models. Supports single or batch text inputs for vector representation.";

        /// <inheritdoc/>
        public JsonDocument InputSchema => this._inputSchema;

        /// <inheritdoc/>
        public async Task<object> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken)
        {
            var model = arguments.GetProperty("model").GetString()
                ?? throw new ArgumentException("Model is required", nameof(arguments));

            var inputElement = arguments.GetProperty("input");
            string[] inputs;

            if (inputElement.ValueKind == JsonValueKind.String)
            {
                // Single string input
                var stringValue = inputElement.GetString();
                inputs = new[] { stringValue! };
            }
            else if (inputElement.ValueKind == JsonValueKind.Array)
            {
                // Array of strings - use foreach to avoid IDisposable warnings
                var inputList = new List<string>();
                using var enumerator = inputElement.EnumerateArray();
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    var value = current.GetString();
                    if (value is null)
                    {
                        throw new ArgumentException("All input array elements must be strings", nameof(arguments));
                    }

                    inputList.Add(value);
                }

                inputs = inputList.ToArray();
            }
            else
            {
                throw new ArgumentException("Input must be a string or array of strings", nameof(arguments));
            }

            var provider = arguments.TryGetProperty("provider", out var providerElement)
                ? providerElement.GetString()
                : null;

            var command = new EmbeddingCommand(
                Input: inputs,
                Model: model,
                Provider: provider);

            var result = await this._executor.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);

            return this.FormatEmbeddingResponse(result);
        }

        private object FormatEmbeddingResponse(EmbeddingResponse result)
        {
            return new
            {
                embeddings = result.Data.Select(e => new
                {
                    index = e.Index,
                    embedding = e.Embedding,
                }).ToArray(),
                usage = result.Usage is not null
                    ? new
                    {
                        promptTokens = result.Usage.PromptTokens,
                        totalTokens = result.Usage.TotalTokens,
                    }
                    : null,
            };
        }
    }
}
