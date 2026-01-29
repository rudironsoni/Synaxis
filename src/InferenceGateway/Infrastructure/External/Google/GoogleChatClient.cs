using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Infrastructure.External.Google;

public class GoogleChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private readonly ChatClientMetadata _metadata;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    private readonly ILogger<GoogleChatClient>? _logger;

    public GoogleChatClient(string apiKey, string modelId, HttpClient httpClient, ILogger<GoogleChatClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _modelId = modelId ?? "default";
        _logger = logger;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Synaxis/1.0");
        _metadata = new ChatClientMetadata("Google.Gemini", new Uri(Endpoint), _modelId);
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requestObj = CreateRequest(chatMessages, options, stream: false);

        // Debug: print request URI and model field payload for diagnosing 404s and model formatting
        try
        {
            var payloadModel = (requestObj as dynamic)?.model ?? _modelId;
            var requestJson = JsonSerializer.Serialize(requestObj, _jsonOptions);
            if (_logger != null)
            {
                _logger.LogInformation("GoogleChatClient sending request. Endpoint: {Endpoint} Model: {Model} Payload: {Payload}", Endpoint, payloadModel, requestJson);
            }
            else
            {
                Console.WriteLine($"GoogleChatClient sending request. Endpoint: {Endpoint} Model: {payloadModel} Payload: {requestJson}");
            }
        }
        catch (Exception ex)
        {
            // Ensure diagnostic information never throws
            Console.WriteLine($"GoogleChatClient debug logging failed: {ex}");
        }

        var response = await _httpClient.PostAsJsonAsync(Endpoint, requestObj, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Google Gemini API Error {response.StatusCode}: {err}");
        }

        var openAiResp = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(_jsonOptions, cancellationToken: cancellationToken);

        var choice = openAiResp?.Choices?.FirstOrDefault();
        var text = choice?.Message?.Content ?? choice?.Text ?? string.Empty;

        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = _modelId
        };

        return chatResponse;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming responses are not implemented for GoogleChatClient yet.");
    }

    private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool stream)
    {
        var messages = chatMessages.Select(m => new
        {
            role = m.Role == ChatRole.User ? "user" :
                   m.Role == ChatRole.Assistant ? "assistant" :
                   m.Role == ChatRole.System ? "system" : "user",
            content = m.Text
        }).ToList();

        return new
        {
            model = options?.ModelId ?? _modelId,
            messages = messages,
            stream = stream
        };
    }

    public void Dispose() => _httpClient.Dispose();
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private class OpenAiChatResponse
    {
        [JsonPropertyName("choices")] public OpenAiChoice[]? Choices { get; set; }
    }

    private class OpenAiChoice
    {
        [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }
        [JsonPropertyName("text")] public string? Text { get; set; }
        [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
    }

    private class OpenAiMessage
    {
        [JsonPropertyName("role")] public string? Role { get; set; }
        [JsonPropertyName("content")] public string? Content { get; set; }
    }
}
