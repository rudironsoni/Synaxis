// <copyright file="OpenAiController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Synaxis.Api.DTOs.OpenAi;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers;
    using Synaxis.Providers.Models;

    /// <summary>
    /// OpenAI-compatible REST API controller.
    /// </summary>
    [ApiController]
    [Route("v1")]
    [AllowAnonymous] // Will be replaced with API key authentication
    public class OpenAiController : ControllerBase
    {
        private readonly ILogger<OpenAiController> _logger;
        private readonly ProviderFactory _providerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAiController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="providerFactory">The provider factory.</param>
        public OpenAiController(
            ILogger<OpenAiController> logger,
            ProviderFactory providerFactory)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        }

        /// <summary>
        /// Creates a model response for the given chat conversation.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The chat completion response.</returns>
        [HttpPost("chat/completions")]
        [ProducesResponseType(typeof(ChatCompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ChatCompletionResponse>> CreateChatCompletion(
            [FromBody] ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            // Check if streaming is requested
            if (request.Stream == true)
            {
                await this.CreateChatCompletionStream(request, cancellationToken);
                return new EmptyResult();
            }

            try
            {
                this._logger.LogInformation("Processing chat completion request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Messages == null || request.Messages.Count == 0)
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Messages are required"));
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Model is required"));
                }

                // Determine provider type from model name
                var providerType = GetProviderTypeFromModel(request.Model);
                var adapter = this._providerFactory.CreateAdapter(providerType);

                // Convert OpenAI request to internal request
                var internalRequest = this.ConvertToInternalChatRequest(request);

                // Call the provider
                var response = await adapter.ChatAsync(internalRequest, cancellationToken);

                // Convert internal response to OpenAI response
                var openAiResponse = this.ConvertToOpenAiChatResponse(response);

                return this.Ok(openAiResponse);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing chat completion request");
                return this.StatusCode(500, this.CreateErrorResponse("server_error", "An error occurred while processing the request"));
            }
        }

        /// <summary>
        /// Creates a model response for the given chat conversation with streaming.
        /// </summary>
        /// <param name="request">The chat completion request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A stream of chat completion chunks.</returns>
        [HttpPost("chat/completions/stream")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task CreateChatCompletionStream(
            [FromBody] ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger.LogInformation("Processing chat completion request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Messages == null || request.Messages.Count == 0)
                {
                    await this.SendSseErrorAsync("invalid_request_error", "Messages are required");
                    return;
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    await this.SendSseErrorAsync("invalid_request_error", "Model is required");
                    return;
                }

                // Determine provider type from model name
                var providerType = GetProviderTypeFromModel(request.Model);
                var adapter = this._providerFactory.CreateAdapter(providerType);

                // Convert OpenAI request to internal request
                var internalRequest = this.ConvertToInternalChatRequest(request);

                // Call the provider
                var response = await adapter.ChatAsync(internalRequest, cancellationToken);

                // Convert internal response to OpenAI response
                var openAiResponse = this.ConvertToOpenAiChatResponse(response);

                await this.SendSseEventAsync(JsonSerializer.Serialize(openAiResponse));
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing chat completion request");
                await this.SendSseErrorAsync("server_error", "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Creates a completion for the provided prompt (legacy endpoint).
        /// </summary>
        /// <param name="request">The completion request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The completion response.</returns>
        [HttpPost("completions")]
        [ProducesResponseType(typeof(CompletionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CompletionResponse>> CreateCompletion(
            [FromBody] CompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger.LogInformation("Processing completion request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Prompt == null)
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Prompt is required"));
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Model is required"));
                }

                // Convert completion request to chat request (legacy endpoint)
                var chatRequest = this.ConvertCompletionToChatRequest(request);

                // Determine provider type from model name
                var providerType = GetProviderTypeFromModel(request.Model);
                var adapter = this._providerFactory.CreateAdapter(providerType);

                // Call the provider
                var response = await adapter.ChatAsync(chatRequest, cancellationToken);

                // Convert internal response to completion response
                var completionResponse = this.ConvertToCompletionResponse(response);

                return this.Ok(completionResponse);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing completion request");
                return this.StatusCode(500, this.CreateErrorResponse("server_error", "An error occurred while processing the request"));
            }
        }

        /// <summary>
        /// Creates an embedding vector representing the input text.
        /// </summary>
        /// <param name="request">The embedding request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The embedding response.</returns>
        [HttpPost("embeddings")]
        [ProducesResponseType(typeof(Synaxis.Api.DTOs.OpenAi.EmbeddingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<Synaxis.Api.DTOs.OpenAi.EmbeddingResponse>> CreateEmbedding(
            [FromBody] Synaxis.Api.DTOs.OpenAi.EmbeddingRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                this._logger.LogInformation("Processing embedding request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Input == null)
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Input is required"));
                }

                if (string.IsNullOrWhiteSpace(request.Model))
                {
                    return this.BadRequest(this.CreateErrorResponse("invalid_request_error", "Model is required"));
                }

                // Convert OpenAI request to internal request
                var internalRequest = this.ConvertToInternalEmbedRequest(request);

                // Determine provider type from model name
                var providerType = GetProviderTypeFromModel(request.Model);
                var adapter = this._providerFactory.CreateAdapter(providerType);

                // Call the provider
                var response = await adapter.EmbedAsync(internalRequest, cancellationToken);

                // Convert internal response to OpenAI response
                var openAiResponse = this.ConvertToOpenAiEmbeddingResponse(response, request.Model);

                return this.Ok(openAiResponse);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing embedding request");
                return this.StatusCode(500, this.CreateErrorResponse("server_error", "An error occurred while processing the request"));
            }
        }

        /// <summary>
        /// Lists the currently available models.
        /// </summary>
        /// <returns>The list of available models.</returns>
        [HttpGet("models")]
        [ProducesResponseType(typeof(ModelList), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public ActionResult<ModelList> ListModels()
        {
            try
            {
                this._logger.LogInformation("Listing available models");

                var models = new List<ModelInfo>
                {
                    new ModelInfo
                    {
                        Id = "gpt-4",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "gpt-4-turbo",
                        Created = DateTimeOffset.UtcNow.AddMonths(-3).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "gpt-3.5-turbo",
                        Created = DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-ada-002",
                        Created = DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-3-small",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-3-large",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                };

                return this.Ok(new ModelList { Data = models });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error listing models");
                return this.StatusCode(500, this.CreateErrorResponse("server_error", "An error occurred while processing the request"));
            }
        }

        /// <summary>
        /// Retrieves a model instance, providing basic information about the model.
        /// </summary>
        /// <param name="model">The model identifier.</param>
        /// <returns>The model information.</returns>
        [HttpGet("models/{model}")]
        [ProducesResponseType(typeof(ModelInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(OpenAiErrorResponse), StatusCodes.Status500InternalServerError)]
        public ActionResult<ModelInfo> GetModel(string model)
        {
            try
            {
                this._logger.LogInformation("Getting model info for: {Model}", model);

                var models = new List<ModelInfo>
                {
                    new ModelInfo
                    {
                        Id = "gpt-4",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "gpt-4-turbo",
                        Created = DateTimeOffset.UtcNow.AddMonths(-3).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "gpt-3.5-turbo",
                        Created = DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-ada-002",
                        Created = DateTimeOffset.UtcNow.AddYears(-1).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-3-small",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                    new ModelInfo
                    {
                        Id = "text-embedding-3-large",
                        Created = DateTimeOffset.UtcNow.AddMonths(-6).ToUnixTimeSeconds(),
                        OwnedBy = "openai",
                    },
                };

                var modelInfo = models.FirstOrDefault(m => string.Equals(m.Id, model, StringComparison.OrdinalIgnoreCase));
                if (modelInfo == null)
                {
                    return this.NotFound(this.CreateErrorResponse("model_not_found", $"Model '{model}' not found"));
                }

                return this.Ok(modelInfo);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting model info");
                return this.StatusCode(500, this.CreateErrorResponse("server_error", "An error occurred while processing the request"));
            }
        }

        private static ProviderType GetProviderTypeFromModel(string model)
        {
            // Simple heuristic to determine provider type from model name
            // In production, this would be more sophisticated and potentially configurable
            if (model.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase))
            {
                return ProviderType.OpenAI;
            }

            // Default to OpenAI
            return ProviderType.OpenAI;
        }

        private ChatRequest ConvertToInternalChatRequest(ChatCompletionRequest request)
        {
            return new ChatRequest
            {
                Model = request.Model,
                Messages = request.Messages.Select(m => new Synaxis.Contracts.V1.Messages.ChatMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Name = m.Name,
                }).ToArray(),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stop = request.Stop as string[],
            };
        }

        private ChatCompletionResponse ConvertToOpenAiChatResponse(ChatResponse response)
        {
            return new ChatCompletionResponse
            {
                Id = response.Id,
                Object = response.Object,
                Created = response.Created,
                Model = response.Model,
                Choices = response.Choices.Select(c => new Synaxis.Api.DTOs.OpenAi.ChatChoice
                {
                    Index = c.Index,
                    Message = new Synaxis.Api.DTOs.OpenAi.ChatMessage
                    {
                        Role = c.Message.Role,
                        Content = c.Message.Content,
                        Name = c.Message.Name,
                    },
                    FinishReason = c.FinishReason,
                }).ToList(),
                Usage = response.Usage != null ? new Synaxis.Api.DTOs.OpenAi.Usage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens,
                }
                : null!,
            };
        }

        private object ConvertToOpenAiStreamChunk(StreamingResponse chunk)
        {
            return new
            {
                id = chunk.Id,
                @object = chunk.Object,
                created = chunk.Created,
                model = chunk.Model,
                choices = chunk.Choices.Select(c => new
                {
                    index = c.Index,
                    delta = new
                    {
                        role = c.Message.Role,
                        content = c.Message.Content,
                    },
                    finish_reason = chunk.IsFinished ? c.FinishReason : (object)null,
                }),
            };
        }

        private ChatRequest ConvertCompletionToChatRequest(CompletionRequest request)
        {
            // Convert legacy completion request to chat request
            var promptText = request.Prompt?.ToString() ?? string.Empty;

            return new ChatRequest
            {
                Model = request.Model,
                Messages = new[]
                {
                    new Synaxis.Contracts.V1.Messages.ChatMessage
                    {
                        Role = "user",
                        Content = promptText,
                    },
                },
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stop = request.Stop as string[],
            };
        }

        private CompletionResponse ConvertToCompletionResponse(ChatResponse response)
        {
            var text = response.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;

            return new CompletionResponse
            {
                Id = response.Id,
                Object = "text_completion",
                Created = response.Created,
                Model = response.Model,
                Choices = new List<CompletionChoice>
                {
                    new CompletionChoice
                    {
                        Index = 0,
                        Text = text,
                        FinishReason = response.Choices.FirstOrDefault()?.FinishReason,
                    },
                },
                Usage = response.Usage != null ? new Usage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens,
                }
                : null,
            };
        }

        private EmbedRequest ConvertToInternalEmbedRequest(EmbeddingRequest request)
        {
            var inputs = new List<string>();

            if (request.Input is string singleInput)
            {
                inputs.Add(singleInput);
            }
            else if (request.Input is List<string> multipleInputs)
            {
                inputs.AddRange(multipleInputs);
            }
            else if (request.Input is string[] arrayInputs)
            {
                inputs.AddRange(arrayInputs);
            }

            return new EmbedRequest
            {
                Model = request.Model,
                Input = inputs.ToArray(),
                EncodingFormat = request.EncodingFormat,
                Dimensions = request.Dimensions,
            };
        }

        private Synaxis.Api.DTOs.OpenAi.EmbeddingResponse ConvertToOpenAiEmbeddingResponse(Synaxis.Contracts.V1.Messages.EmbeddingResponse response, string model)
        {
            return new Synaxis.Api.DTOs.OpenAi.EmbeddingResponse
            {
                Object = response.Object,
                Model = model,
                Data = response.Data.Select(d => new Synaxis.Api.DTOs.OpenAi.EmbeddingData
                {
                    Index = d.Index,
                    Object = d.Object,
                    Embedding = d.Embedding.ToList(),
                }).ToList(),
                Usage = response.Usage != null ? new Synaxis.Api.DTOs.OpenAi.Usage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    TotalTokens = response.Usage.TotalTokens,
                }
                : null!,
            };
        }

        private OpenAiErrorResponse CreateErrorResponse(string type, string message)
        {
            return new OpenAiErrorResponse
            {
                Error = new OpenAiErrorDetail
                {
                    Message = message,
                    Type = type,
                    Code = type,
                },
            };
        }

        private async Task SendSseEventAsync(string data)
        {
            await this.Response.WriteAsync($"data: {data}\n\n");
            await this.Response.Body.FlushAsync();
        }

        private Task SendSseErrorAsync(string type, string message)
        {
            var error = this.CreateErrorResponse(type, message);
            return this.SendSseEventAsync(JsonSerializer.Serialize(error));
        }
    }
}
