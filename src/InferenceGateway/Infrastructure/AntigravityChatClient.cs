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

namespace Synaxis.InferenceGateway.Infrastructure;

/// <summary>
/// A robust, upstream-compatible client for Google's Antigravity Gateway.
/// Implements strict protocol compliance including the wrapper object and custom headers.
/// </summary>
public class AntigravityChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private readonly string _projectId;
    private readonly ITokenProvider _tokenProvider;
    private readonly ChatClientMetadata _metadata;

    // Endpoints are now relative to the configured HttpClient.BaseAddress
    private const string EndpointRelative = "/v1/chat/completions";
    private const string StreamEndpointRelative = "/v1/chat/completions?alt=sse";

    public AntigravityChatClient(
        HttpClient httpClient,
        string modelId,
        string projectId,
        ITokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _modelId = modelId;
        _projectId = projectId;
        _tokenProvider = tokenProvider;
        // Prefer the configured BaseAddress on the provided HttpClient when available
        _metadata = new ChatClientMetadata("Antigravity", _httpClient.BaseAddress ?? new Uri("https://cloudcode-pa.googleapis.com"), modelId);
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var request = BuildRequest(messagesList, options);
        var json = JsonSerializer.Serialize(request, AntigravityJsonContext.Default.AntigravityRequest);
        
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointRelative);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        await PrepareRequestAsync(httpRequest, cancellationToken);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response);

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var agResponse = JsonSerializer.Deserialize(responseJson, AntigravityJsonContext.Default.AntigravityResponseWrapper);

        return MapResponse(agResponse?.Response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var request = BuildRequest(messagesList, options);
        var json = JsonSerializer.Serialize(request, AntigravityJsonContext.Default.AntigravityRequest);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, StreamEndpointRelative);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        await PrepareRequestAsync(httpRequest, cancellationToken);

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessAsync(response);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break; // End of stream
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                AntigravityResponseWrapper? wrapper = null;
                try
                {
                    wrapper = JsonSerializer.Deserialize(data, AntigravityJsonContext.Default.AntigravityResponseWrapper);
                }
                catch (JsonException) { /* Ignore malformed lines */ }

                if (wrapper?.Response?.Candidates != null)
                {
                    foreach (var candidate in wrapper.Response.Candidates)
                    {
                        if (candidate.Content?.Parts != null)
                        {
                            foreach (var part in candidate.Content.Parts)
                            {
                                if (!string.IsNullOrEmpty(part.Text))
                                {
                                    yield return new ChatResponseUpdate
                                    {
                                        Role = new ChatRole(candidate.Content.Role ?? "model"),
                                        Contents = { new TextContent(part.Text) }
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private async Task PrepareRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // Strict Headers required by Antigravity
        request.Headers.TryAddWithoutValidation("User-Agent", "antigravity/1.11.5 windows/amd64");
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Client", "google-cloud-sdk vscode_cloudshelleditor/0.1");
        request.Headers.TryAddWithoutValidation("Client-Metadata", "{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"}");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
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
                    Parts = new List<Part> { new Part { Text = msg.Text } }
                });
            }
        }

        var config = new GenerationConfig
        {
            MaxOutputTokens = options?.MaxOutputTokens ?? 4000,
            Temperature = options?.Temperature ?? 0.7f,
            TopP = options?.TopP ?? 0.95f,
            StopSequences = options?.StopSequences
        };

        // Handle Thinking Config
        if (options?.AdditionalProperties != null && options.AdditionalProperties.TryGetValue("thinking", out var thinkingObj))
        {
             // Assuming thinkingObj is a dictionary or object that can be serialized to ThinkingConfig
             // For now, we'll try to extract budget if present, or default
             if (thinkingObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
             {
                 // Simple extraction logic or default
                 config.ThinkingConfig = new ThinkingConfig { IncludeThoughts = true, ThinkingBudget = 2000 };
             }
        }

        return new AntigravityRequest
        {
            Project = _projectId,
            Model = _modelId,
            RequestPayload = new RequestPayload
            {
                Contents = contentList,
                SystemInstruction = systemInstruction,
                GenerationConfig = config
            }
        };
    }

    private ChatResponse MapResponse(AntigravityResponse? response)
    {
        if (response?.Candidates == null || response.Candidates.Count == 0)
            return new ChatResponse(new List<ChatMessage>());

        var candidate = response.Candidates[0];
        var text = string.Join("", candidate.Content?.Parts?.Select(p => p.Text) ?? Array.Empty<string>());

        return new ChatResponse(new List<ChatMessage>
        {
            new ChatMessage(new ChatRole(candidate.Content?.Role ?? "model"), text)
        })
        {
            ResponseId = response.ResponseId,
            ModelId = response.ModelVersion
        };
    }

    // Do not dispose HttpClient instances provided by IHttpClientFactory - let the factory manage lifetime.
    public void Dispose() { }
    public object? GetService(Type serviceType, object? serviceKey = null) => 
        serviceType == typeof(IChatClient) ? this : null;
}

internal class AntigravityRequest
{
    [JsonPropertyName("project")] public string Project { get; set; } = "";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("request")] public RequestPayload RequestPayload { get; set; } = new();
}

internal class RequestPayload
{
    [JsonPropertyName("contents")] public List<Content> Contents { get; set; } = new();
    [JsonPropertyName("systemInstruction")] public SystemInstruction? SystemInstruction { get; set; }
    [JsonPropertyName("generationConfig")] public GenerationConfig GenerationConfig { get; set; } = new();
}

internal class Content
{
    [JsonPropertyName("role")] public string Role { get; set; } = "user";
    [JsonPropertyName("parts")] public List<Part> Parts { get; set; } = new();
}

internal class SystemInstruction
{
    [JsonPropertyName("parts")] public List<Part> Parts { get; set; } = new();
}

internal class Part
{
    [JsonPropertyName("text")] public string? Text { get; set; }
}

internal class GenerationConfig
{
    [JsonPropertyName("maxOutputTokens")] public int MaxOutputTokens { get; set; }
    [JsonPropertyName("temperature")] public float Temperature { get; set; }
    [JsonPropertyName("topP")] public float TopP { get; set; }
    [JsonPropertyName("stopSequences")] public IList<string>? StopSequences { get; set; }
    [JsonPropertyName("thinkingConfig")] public ThinkingConfig? ThinkingConfig { get; set; }
}

internal class ThinkingConfig
{
    [JsonPropertyName("thinkingBudget")] public int ThinkingBudget { get; set; }
    [JsonPropertyName("includeThoughts")] public bool IncludeThoughts { get; set; }
}

internal class AntigravityResponseWrapper
{
    [JsonPropertyName("response")] public AntigravityResponse? Response { get; set; }
}

internal class AntigravityResponse
{
    [JsonPropertyName("candidates")] public List<Candidate> Candidates { get; set; } = new();
    [JsonPropertyName("modelVersion")] public string ModelVersion { get; set; } = "";
    [JsonPropertyName("responseId")] public string ResponseId { get; set; } = "";
}

internal class Candidate
{
    [JsonPropertyName("content")] public Content? Content { get; set; }
    [JsonPropertyName("finishReason")] public string? FinishReason { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AntigravityRequest))]
[JsonSerializable(typeof(AntigravityResponseWrapper))]
internal partial class AntigravityJsonContext : JsonSerializerContext
{
}
