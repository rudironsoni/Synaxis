using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier3
{
    public class MetaAIProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<MetaAIProvider> _logger;

        private const string BaseUrl = "https://www.meta.ai";
        private const string ApiUrl = "https://graph.meta.ai";

        public string Id => "metaai";
        public string Name => "MetaAI";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "meta-ai", "llama-3.3", "llama-3.3-70b", "meta/llama-3.3-70b"
        };

        public MetaAIProvider(
            HttpClient http,
            CookieManager cookieManager,
            ILogger<MetaAIProvider> logger)
        {
            _http = http;
            _cookieManager = cookieManager;
            _logger = logger;

            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            _http.DefaultRequestHeaders.Add("Origin", BaseUrl);
            _http.DefaultRequestHeaders.Add("Referer", $"{BaseUrl}/");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var prompt = string.Join("\n", request.Messages.Select(m => $"{m.Role}: {m.Content}"));
            
            var cookies = await _cookieManager.GetCookiesAsync("meta");
            if (cookies.TryGetValue("access_token", out var accessToken))
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);
            }

            var metaRequest = new
            {
                query = @"
                    mutation sendMessage($message: MessageInput!, $conversationId: String) {
                        sendMessage(message: $message, externalConversationId: $conversationId) {
                            message {
                                content
                                messageId
                            }
                        }
                    }",
                variables = new
                {
                    message = new
                    {
                        content = prompt,
                        contentType = "text"
                    },
                    conversationId = Guid.NewGuid().ToString()
                }
            };

            var response = await _http.PostAsJsonAsync($"{ApiUrl}/graphql", metaRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MetaAIResponse>(cancellationToken: ct);
            var content = result?.Data?.SendMessage?.Message?.Content ?? "";

            var promptTokens = EstimateTokens(prompt.Length);
            var completionTokens = EstimateTokens(content.Length);

            return new ChatCompletionResult(
                result?.Data?.SendMessage?.Message?.MessageId ?? Guid.NewGuid().ToString(),
                content,
                "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class MetaAIResponse
        {
            public DataInfo? Data { get; set; }

            public class DataInfo
            {
                public SendMessageInfo? SendMessage { get; set; }
            }

            public class SendMessageInfo
            {
                public MessageInfo? Message { get; set; }
            }

            public class MessageInfo
            {
                public string? Content { get; set; }
                public string? MessageId { get; set; }
            }
        }
    }
}
