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
    public class LMArenaProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<LMArenaProvider> _logger;

        private const string BaseUrl = "https://lmarena.ai";

        public string Id => "lmarena";
        public string Name => "LMArena";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "gpt-4o", "claude-3.5-sonnet", "claude-3-opus", "gemini-1.5-pro",
            "llama-3.3-70b", "llama-3.1-405b", "qwen2.5-72b", "deepseek-v3", "mistral-large",
            "arena-random", "arena-side-by-side"
        };

        public LMArenaProvider(
            HttpClient http,
            CookieManager cookieManager,
            ILogger<LMArenaProvider> logger)
        {
            _http = http;
            _cookieManager = cookieManager;
            _logger = logger;

            _http.BaseAddress = new Uri(BaseUrl);
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            await ApplyCookiesAsync();

            var arenaRequest = new
            {
                model = NormalizeModel(request.Model),
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false
            };

            var response = await _http.PostAsJsonAsync("/api/chat", arenaRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ArenaResponse>(cancellationToken: ct);
            var choice = result?.Choices?.FirstOrDefault();
            var content = choice?.Message?.Content ?? "";

            var promptTokens = result?.Usage?.PromptTokens ?? EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = result?.Usage?.CompletionTokens ?? EstimateTokens(content.Length);

            return new ChatCompletionResult(
                result?.Id ?? Guid.NewGuid().ToString(),
                content,
                choice?.FinishReason ?? "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private async Task ApplyCookiesAsync()
        {
            var cookies = await _cookieManager.GetCookiesAsync("lmarena");
            if (cookies.Count > 0)
            {
                var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
                _http.DefaultRequestHeaders.Remove("Cookie");
                _http.DefaultRequestHeaders.Add("Cookie", cookieHeader);
            }
        }

        private string NormalizeModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "arena" or "random" => "arena-random",
                "battle" or "compare" => "arena-side-by-side",
                _ => model
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class ArenaResponse
        {
            public string? Id { get; set; }
            public List<ChoiceInfo>? Choices { get; set; }
            public UsageInfo? Usage { get; set; }

            public class ChoiceInfo
            {
                public MessageInfo? Message { get; set; }
                public string? FinishReason { get; set; }
            }

            public class MessageInfo
            {
                public string? Content { get; set; }
            }

            public class UsageInfo
            {
                [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
                [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
            }
        }
    }
}
