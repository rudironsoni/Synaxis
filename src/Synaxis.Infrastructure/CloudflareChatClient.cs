using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Synaxis.Infrastructure;

public class CloudflareChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly string _modelId;
    private readonly ChatClientMetadata _metadata;

    public CloudflareChatClient(HttpClient httpClient, string accountId, string modelId, string apiKey)
    {
        _httpClient = httpClient;
        _accountId = accountId;
        _modelId = modelId;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _metadata = new ChatClientMetadata("Cloudflare", new Uri($"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{modelId}"), modelId);
    }

    public ChatClientMetadata Metadata => _metadata;

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(chatMessages, options, stream: false);
        var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{_modelId}";

        var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var cloudflareResponse = await response.Content.ReadFromJsonAsync<CloudflareResponse>(cancellationToken: cancellationToken);

        var text = cloudflareResponse?.Result?.Response ?? "";
        var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
        chatResponse.ModelId = _modelId;
        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(chatMessages, options, stream: true);
        var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{_modelId}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request)
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var json = line.Substring(6).Trim();
            if (json == "[DONE]") break;

            CloudflareStreamResponse? streamEvent = null;
            try
            {
                streamEvent = JsonSerializer.Deserialize<CloudflareStreamResponse>(json);
            }
            catch { continue; }

            if (streamEvent?.Response != null)
            {
                var update = new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    ModelId = _modelId
                };
                update.Contents.Add(new TextContent(streamEvent.Response));
                yield return update;
            }
        }
    }

    private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool stream)
    {
        var messages = new List<object>();
        foreach (var msg in chatMessages)
        {
            messages.Add(new
            {
                role = msg.Role.Value,
                content = msg.Text
            });
        }

        return new
        {
            messages = messages,
            stream = stream
        };
    }

    public void Dispose() => _httpClient.Dispose();
    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    private class CloudflareResponse
    {
        [JsonPropertyName("result")] public CloudflareResult? Result { get; set; }
    }

    private class CloudflareResult
    {
        [JsonPropertyName("response")] public string? Response { get; set; }
    }

    private class CloudflareStreamResponse
    {
        [JsonPropertyName("response")] public string? Response { get; set; }
    }
}
