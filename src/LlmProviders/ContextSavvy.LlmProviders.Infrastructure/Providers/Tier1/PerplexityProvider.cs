using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1
{
    public class PerplexityProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PerplexityProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "turbo", "sonar", "sonar-pro", "sonar-reasoning", "sonar-reasoning-pro"
        };

        public string Id => "perplexity";
        public string Name => "Perplexity";
        public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public PerplexityProvider(HttpClient httpClient, ILogger<PerplexityProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.perplexity.ai");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "sonar" : request.Model;

            var payload = new
            {
                model = model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PerplexityResponse>(cancellationToken: ct);

            if (result?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new Exception("Invalid Perplexity response");
            }

            var text = result.Choices.First().Message.Content;

            return new ChatCompletionResult(
                result.Id,
                text,
                result.Choices.First().FinishReason ?? "stop",
                new Usage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0, result.Usage?.TotalTokens ?? 0)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private class PerplexityResponse
        {
            public string Id { get; set; } = "";
            public List<Choice> Choices { get; set; } = new();
            public UsageInfo? Usage { get; set; }

            public class Choice
            {
                public MessageInfo Message { get; set; } = new();
                public string FinishReason { get; set; } = "";
            }

            public class MessageInfo
            {
                public string Role { get; set; } = "";
                public string Content { get; set; } = "";
            }

            public class UsageInfo
            {
                [JsonPropertyName("prompt_tokens")]
                public int PromptTokens { get; set; }
                [JsonPropertyName("completion_tokens")]
                public int CompletionTokens { get; set; }
                [JsonPropertyName("total_tokens")]
                public int TotalTokens { get; set; }
            }
        }
    }
}
