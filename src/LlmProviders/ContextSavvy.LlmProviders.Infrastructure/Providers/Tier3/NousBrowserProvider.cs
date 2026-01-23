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
    public class NousBrowserProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<NousBrowserProvider> _logger;

        private const string BaseUrl = "https://nous.hermes.dev";
        private const string ApiUrl = "https://api.nous.hermes.dev";

        public string Id => "nousbrowser";
        public string Name => "NousBrowser";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "hermes-3-llama-3.1-405b", "hermes-3-llama-3.1-70b", "hermes-3-llama-3.1-8b", "hermes-2-pro-llama-3-8b",
            "hermes-405b", "hermes-70b", "hermes-8b", "nous/hermes-3", "nous/hermes-405b"
        };

        public NousBrowserProvider(
            HttpClient http,
            CookieManager cookieManager,
            ILogger<NousBrowserProvider> logger)
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
            await ApplyCookiesAsync();

            var nousRequest = new
            {
                model = NormalizeModel(request.Model),
                messages = request.Messages.Select(m => new { role = m.Role.ToLowerInvariant(), content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            };

            var response = await _http.PostAsJsonAsync($"{ApiUrl}/v1/chat/completions", nousRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NousResponse>(cancellationToken: ct);
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
            var cookies = await _cookieManager.GetCookiesAsync("nous");
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
                "hermes" or "nous/hermes-3" or "hermes-3" => "hermes-3-llama-3.1-70b",
                "hermes-405b" or "nous/hermes-405b" => "hermes-3-llama-3.1-405b",
                _ => model.Replace("nous/", "")
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class NousResponse
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
