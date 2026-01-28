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

namespace Synaxis.InferenceGateway.Infrastructure;

public class CohereChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _modelId;
    private readonly ChatClientMetadata _metadata;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public CohereChatClient(HttpClient httpClient, string modelId, string apiKey)
    {
        _httpClient = httpClient;
        _modelId = modelId;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Synaxis/1.0");
        _metadata = new ChatClientMetadata("Cohere", new Uri("https://api.cohere.com/v2/chat"), modelId);
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var requestObj = CreateRequest(chatMessages, options, stream: false);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat")
        {
            Content = JsonContent.Create(requestObj)
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Cohere API Error {response.StatusCode}: {error}");
        }
        
        response.EnsureSuccessStatusCode();

        var cohereResponse = await response.Content.ReadFromJsonAsync<CohereResponseV2>(_jsonOptions, cancellationToken: cancellationToken);
        
        var text = cohereResponse?.Message?.Content?.FirstOrDefault(c => c.Type == "text")?.Text ?? "";
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = _modelId
        };
        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestObj = CreateRequest(chatMessages, options, stream: true);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat")
        {
            Content = JsonContent.Create(requestObj)
        };

        // Ask for SSE
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Cohere API Error {response.StatusCode}: {err}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        string? currentEvent = null;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Track event name if provided
            if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
            {
                currentEvent = line.Substring(7).Trim();
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase)) continue;

            var json = line.Substring(6).Trim();
            if (json == "[DONE]") break;

            CohereStreamEvent? ev = null;
            try
            {
                ev = JsonSerializer.Deserialize<CohereStreamEvent>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // ignore malformed json
                continue;
            }

            // Prefer explicit SSE event name when available, otherwise infer from payload
            var evName = currentEvent ?? ev?.Type;

            // Handle content delta events
            if (string.Equals(evName, "content-delta", StringComparison.OrdinalIgnoreCase) || ev?.Delta?.Message?.Content != null)
            {
                var contents = ev?.Delta?.Message?.Content;
                if (contents != null)
                {
                    // Concatenate text parts if multiple are present in this delta
                    var textParts = contents.Where(c => !string.IsNullOrEmpty(c.Text)).Select(c => c.Text).ToList();
                    if (textParts.Count > 0)
                    {
                        var text = string.Join("", textParts);
                        var update = new ChatResponseUpdate
                        {
                            Role = ChatRole.Assistant,
                            ModelId = _modelId
                        };
                        update.Contents.Add(new TextContent(text));
                        yield return update;
                    }
                }
            }

            // Handle message end events which may include finish reason and usage
            if (string.Equals(evName, "message-end", StringComparison.OrdinalIgnoreCase) || ev?.Delta?.FinishReason != null)
            {
                var finish = ev?.Delta?.FinishReason ?? ev?.Delta?.Message?.Content?.FirstOrDefault()?.Type; // fallback
                var update = new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    ModelId = _modelId
                };

                // If Cohere provides an explicit finish reason, set it on the update if supported
                try
                {
                    // Many consumers expect FinishReason as string; set via reflection-safe approach
                    var prop = typeof(ChatResponseUpdate).GetProperty("FinishReason");
                    if (prop != null && prop.PropertyType == typeof(string))
                    {
                        prop.SetValue(update, finish);
                    }
                }
                catch { /* ignore any reflection issues */ }

                yield return update;
            }
        }
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

    // V2 Response Classes
    private class CohereResponseV2 
    { 
        public CohereMessageV2? Message { get; set; } 
    }
    
    private class CohereMessageV2 
    { 
        public List<CohereContentV2>? Content { get; set; } 
    }
    
    private class CohereContentV2 
    { 
        public string? Type { get; set; } 
        public string? Text { get; set; } 
    }

    // Streaming event DTOs
    private class CohereStreamEvent
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("delta")] public CohereDelta? Delta { get; set; }
        [JsonPropertyName("usage")] public CohereUsage? Usage { get; set; }
    }

    private class CohereDelta
    {
        [JsonPropertyName("message")] public CohereDeltaMessage? Message { get; set; }
        [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
    }

    private class CohereDeltaMessage
    {
        [JsonPropertyName("content")] public List<CohereContentV2>? Content { get; set; }
    }

    private class CohereUsage
    {
        [JsonPropertyName("prompt_tokens")] public int? PromptTokens { get; set; }
        [JsonPropertyName("completion_tokens")] public int? CompletionTokens { get; set; }
        [JsonPropertyName("total_tokens")] public int? TotalTokens { get; set; }
    }
}
