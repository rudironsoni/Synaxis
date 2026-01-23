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
    public class PuterJSProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<PuterJSProvider> _logger;

        private const string BaseUrl = "https://api.puter.com";

        public string Id => "puter";
        public string Name => "Puter";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "gpt-4o-mini", "gpt-4o", "gpt-4-turbo", "o1-mini", "claude-3-5-sonnet",
            "claude-3-haiku", "llama-3.1-70b", "mixtral-8x7b", "gemma-7b",
            "puter/gpt-4o", "puter/claude"
        };

        public PuterJSProvider(
            HttpClient http,
            ILogger<PuterJSProvider> logger)
        {
            _http = http;
            _logger = logger;

            _http.BaseAddress = new Uri(BaseUrl);
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var puterRequest = new
            {
                model = NormalizeModel(request.Model),
                messages = request.Messages.Select(m => new { role = m.Role.ToLowerInvariant(), content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            };

            var response = await _http.PostAsJsonAsync("/v2/ai/chat", puterRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PuterResponse>(cancellationToken: ct);
            var content = result?.Message?.Content ?? result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            var promptTokens = result?.Usage?.PromptTokens ?? EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = result?.Usage?.CompletionTokens ?? EstimateTokens(content.Length);

            return new ChatCompletionResult(
                result?.Id ?? Guid.NewGuid().ToString(),
                content,
                result?.Choices?.FirstOrDefault()?.FinishReason ?? "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private string NormalizeModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "puter" or "default" => "gpt-4o-mini",
                "puter/gpt-4o" => "gpt-4o",
                "puter/claude" => "claude-3-5-sonnet",
                _ => model.Replace("puter/", "")
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class PuterResponse
        {
            public string? Id { get; set; }
            public MessageInfo? Message { get; set; }
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
