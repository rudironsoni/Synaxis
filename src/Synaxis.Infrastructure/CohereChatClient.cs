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

namespace Synaxis.Infrastructure;

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
        // For now, throw NotImplementedException for V2 streaming as per priority on GetResponseAsync
        throw new NotImplementedException("Cohere V2 streaming is not yet implemented.");
        
        #pragma warning disable CS0162
        yield break;
        #pragma warning restore CS0162
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
}
