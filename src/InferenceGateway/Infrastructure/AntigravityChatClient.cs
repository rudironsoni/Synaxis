// <copyright file="AntigravityChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Infrastructure.Auth;

    /// <summary>
    /// A robust, upstream-compatible client for Google's Antigravity Gateway.
    /// Implements strict protocol compliance including the wrapper object and custom headers.
    /// </summary>
    /// <summary>
    /// AntigravityChatClient class.
    /// </summary>
    public sealed class AntigravityChatClient : IChatClient
    {
        // Endpoints are now relative to the configured HttpClient.BaseAddress
        private const string EndpointRelative = "/v1/chat/completions";
        private const string StreamEndpointRelative = "/v1/chat/completions?alt=sse";

        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly string _projectId;
        private readonly ITokenProvider _tokenProvider;
        private readonly ChatClientMetadata _metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityChatClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for requests.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="tokenProvider">The token provider for authentication.</param>
        public AntigravityChatClient(
            HttpClient httpClient,
            string modelId,
            string projectId,
            ITokenProvider tokenProvider)
        {
            this._httpClient = httpClient;
            this._modelId = modelId;
            this._projectId = projectId;
            this._tokenProvider = tokenProvider;

            // Prefer the configured BaseAddress on the provided HttpClient when available
#pragma warning disable S1075 // URIs should not be hardcoded - Default API endpoint
            this._metadata = new ChatClientMetadata("Antigravity", this._httpClient.BaseAddress ?? new Uri("https://cloudcode-pa.googleapis.com"), modelId);
#pragma warning restore S1075 // URIs should not be hardcoded
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var messagesList = messages.ToList();
            var request = this.BuildRequest(messagesList, options);
            var json = JsonSerializer.Serialize(request, AntigravityJsonContext.Default.AntigravityRequest);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointRelative);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            await this.PrepareRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            using var response = await this._httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(response).ConfigureAwait(false);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var agResponse = JsonSerializer.Deserialize(responseJson, AntigravityJsonContext.Default.AntigravityResponseWrapper);

            return this.MapResponse(agResponse?.Response);
        }

        /// <summary>
        /// Gets streaming chat responses asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages to send.</param>
        /// <param name="options">Optional chat options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of chat response updates.</returns>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var messagesList = messages.ToList();
            var request = this.BuildRequest(messagesList, options);
            var json = JsonSerializer.Serialize(request, AntigravityJsonContext.Default.AntigravityRequest);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, StreamEndpointRelative);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            await this.PrepareRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);

            using var response = await this._httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(response).ConfigureAwait(false);

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            await foreach (var update in this.ProcessStreamAsync(reader, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }

        /// <summary>
        /// Processes the stream of server-sent events and yields chat response updates.
        /// </summary>
        /// <param name="reader">The stream reader.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of chat response updates.</returns>
        private async IAsyncEnumerable<ChatResponseUpdate> ProcessStreamAsync(
            StreamReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    break; // End of stream
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    var data = line.Substring(6).Trim();
                    if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                    {
                        break;
                    }

                    AntigravityResponseWrapper? wrapper = null;
                    try
                    {
                        wrapper = JsonSerializer.Deserialize(data, AntigravityJsonContext.Default.AntigravityResponseWrapper);
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed lines
                    }

                    if (wrapper?.Response?.Candidates != null)
                    {
                        var updates = wrapper.Response.Candidates
                            .Where(candidate => candidate.Content?.Parts != null)
                            .SelectMany(candidate => candidate.Content!.Parts
                                .Where(part => !string.IsNullOrEmpty(part.Text))
                                .Select(part => new ChatResponseUpdate
                                {
                                    Role = new ChatRole(candidate.Content.Role ?? "model"),
                                    Contents = { new TextContent(part.Text!) },
                                }));

                        foreach (var update in updates)
                        {
                            yield return update;
                        }
                    }
                }
            }
        }

        private async Task PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await this._tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Strict Headers required by Antigravity
            request.Headers.TryAddWithoutValidation("User-Agent", "antigravity/1.11.5 windows/amd64");
            request.Headers.TryAddWithoutValidation("X-Goog-Api-Client", "google-cloud-sdk vscode_cloudshelleditor/0.1");
            request.Headers.TryAddWithoutValidation("Client-Metadata", "{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"}");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"Antigravity API Error ({response.StatusCode}): {error}");
            }
        }

        private AntigravityRequest BuildRequest(IList<ChatMessage> messages, ChatOptions? options)
        {
            var contentList = new List<Content>();
            SystemInstruction? systemInstruction = null;

            foreach (var msg in messages)
            {
                if (msg.Role == ChatRole.System)
                {
                    // Antigravity requires system instructions in a separate field, not in contents
                    if (systemInstruction == null)
                    {
                        systemInstruction = new SystemInstruction { Parts = new List<Part>() };
                    }

                    systemInstruction.Parts.Add(new Part { Text = msg.Text });
                }
                else
                {
                    var role = msg.Role == ChatRole.User ? "user" : "model";
                    contentList.Add(new Content
                    {
                        Role = role,
                        Parts = new List<Part> { new Part { Text = msg.Text } },
                    });
                }
            }

            var config = new GenerationConfig
            {
                MaxOutputTokens = options?.MaxOutputTokens ?? 4000,
                Temperature = options?.Temperature ?? 0.7f,
                TopP = options?.TopP ?? 0.95f,
                StopSequences = options?.StopSequences,
            };

            // Handle Thinking Config
            if (options?.AdditionalProperties != null
                && options.AdditionalProperties.TryGetValue("thinking", out var thinkingObj)
                && thinkingObj is JsonElement je
                && je.ValueKind == JsonValueKind.Object)
            {
                // Simple extraction logic or default
                config.ThinkingConfig = new ThinkingConfig { IncludeThoughts = true, ThinkingBudget = 2000 };
            }

            return new AntigravityRequest
            {
                Project = this._projectId,
                Model = this._modelId,
                RequestPayload = new RequestPayload
                {
                    Contents = contentList,
                    SystemInstruction = systemInstruction,
                    GenerationConfig = config,
                },
            };
        }

        private ChatResponse MapResponse(AntigravityResponse? response)
        {
            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                return new ChatResponse(new List<ChatMessage>());
            }

            var candidate = response.Candidates[0];
            var text = string.Join(string.Empty, candidate.Content?.Parts?.Select(p => p.Text) ?? Array.Empty<string>());

            return new ChatResponse(new List<ChatMessage>
            {
                new ChatMessage(new ChatRole(candidate.Content?.Role ?? "model"), text),
            })
            {
                ResponseId = response.ResponseId,
                ModelId = response.ModelVersion,
            };
        }

        // Do not dispose HttpClient instances provided by IHttpClientFactory - let the factory manage lifetime.

        /// <summary>
        /// Disposes the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to get.</param>
        /// <param name="serviceKey">An optional key for the service.</param>
        /// <returns>The service instance, or null if not found.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(IChatClient) ? this : null;
    }

    /// <summary>
    /// Represents a request to the Antigravity API.
    /// </summary>
    internal class AntigravityRequest
    {
        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        [JsonPropertyName("project")]
        public string Project { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the RequestPayload.
        /// </summary>
        [JsonPropertyName("request")]
        public RequestPayload RequestPayload { get; set; } = new();
    }

    /// <summary>
    /// Represents the request payload for the Antigravity API.
    /// </summary>
    internal class RequestPayload
    {
        /// <summary>
        /// Gets or sets the contents of the request.
        /// </summary>
        [JsonPropertyName("contents")]
        public List<Content> Contents { get; set; } = new();

        /// <summary>
        /// Gets or sets the SystemInstruction.
        /// </summary>
        [JsonPropertyName("systemInstruction")]
        public SystemInstruction? SystemInstruction { get; set; }

        /// <summary>
        /// Gets or sets the GenerationConfig.
        /// </summary>
        [JsonPropertyName("generationConfig")]
        public GenerationConfig GenerationConfig { get; set; } = new();
    }

    /// <summary>
    /// Represents content in the Antigravity API format.
    /// </summary>
    internal class Content
    {
        /// <summary>
        /// Gets or sets the role of the content.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        /// <summary>
        /// Gets or sets the parts of the content.
        /// </summary>
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    /// <summary>
    /// Represents a system instruction in the Antigravity API format.
    /// </summary>
    internal class SystemInstruction
    {
        /// <summary>
        /// Gets or sets the parts of the system instruction.
        /// </summary>
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }

    /// <summary>
    /// Represents a part of content in the Antigravity API format.
    /// </summary>
    internal class Part
    {
        /// <summary>
        /// Gets or sets the text content.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// Configuration for text generation in the Antigravity API.
    /// </summary>
    internal class GenerationConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of output tokens.
        /// </summary>
        [JsonPropertyName("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        /// <summary>
        /// Gets or sets the temperature for generation.
        /// </summary>
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        /// <summary>
        /// Gets or sets the top-p value for nucleus sampling.
        /// </summary>
        [JsonPropertyName("topP")]
        public float TopP { get; set; }

        /// <summary>
        /// Gets or sets the top-k value for sampling.
        /// </summary>
        [JsonPropertyName("topK")]
        public int TopK { get; set; }

        /// <summary>
        /// Gets or sets the stop sequences.
        /// </summary>
        [JsonPropertyName("stopSequences")]
        public IList<string>? StopSequences { get; set; }

        /// <summary>
        /// Gets or sets the ThinkingConfig.
        /// </summary>
        [JsonPropertyName("thinkingConfig")]
        public ThinkingConfig? ThinkingConfig { get; set; }
    }

    /// <summary>
    /// Configuration for extended thinking capabilities in the Antigravity API.
    /// </summary>
    internal class ThinkingConfig
    {
        /// <summary>
        /// Gets or sets the thinking budget.
        /// </summary>
        [JsonPropertyName("thinkingBudget")]
        public int ThinkingBudget { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include thoughts.
        /// </summary>
        [JsonPropertyName("includeThoughts")]
        public bool IncludeThoughts { get; set; }
    }

    /// <summary>
    /// Wrapper for the Antigravity API response format.
    /// </summary>
    internal class AntigravityResponseWrapper
    {
        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        [JsonPropertyName("response")]
        public AntigravityResponse? Response { get; set; }
    }

    /// <summary>
    /// Represents a response from the Antigravity API.
    /// </summary>
    internal class AntigravityResponse
    {
        /// <summary>
        /// Gets or sets the candidates.
        /// </summary>
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; } = new();

        /// <summary>
        /// Gets or sets the model version.
        /// </summary>
        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the response ID.
        /// </summary>
        [JsonPropertyName("responseId")]
        public string ResponseId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a candidate response from the Antigravity API.
    /// </summary>
    internal class Candidate
    {
        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [JsonPropertyName("content")]
        public Content? Content { get; set; }

        /// <summary>
        /// Gets or sets the finish reason.
        /// </summary>
        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    /// <summary>
    /// JSON serialization context for Antigravity.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(AntigravityRequest))]
    [JsonSerializable(typeof(AntigravityResponseWrapper))]
    internal partial class AntigravityJsonContext : JsonSerializerContext
    {
    }
}
