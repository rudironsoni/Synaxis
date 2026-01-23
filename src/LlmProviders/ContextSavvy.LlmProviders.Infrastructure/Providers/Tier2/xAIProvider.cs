using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier2
{
    public class xAIProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<xAIProvider> _logger;
        private readonly string? _apiKey;

        private const string BaseUrl = "https://api.x.ai/v1";

        public string Id => "xai";
        public string Name => "xAI";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "grok-3", "grok-3-beta", "grok-3-fast", "grok-2", "grok-2-1212", "grok-2-vision-1212", "grok-beta",
            "grok", "grok3", "grok2"
        };

        public xAIProvider(
            HttpClient http,
            ILogger<xAIProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["xAI:ApiKey"] ?? Environment.GetEnvironmentVariable("XAI_API_KEY");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = new
            {
                model = modelId,
                messages = request.Messages.Select(m => new { role = m.Role.ToLowerInvariant(), content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<XAIResponse>(cancellationToken: ct);
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("Empty response from xAI");
            }

            return new ChatCompletionResult(
                result.Id ?? Guid.NewGuid().ToString(),
                result.Choices[0].Message?.Content ?? "",
                result.Choices[0].FinishReason ?? "stop",
                new Usage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0, result.Usage?.TotalTokens ?? 0)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private string ResolveModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "grok" or "grok3" => "grok-3",
                "grok2" => "grok-2",
                _ => model
            };
        }

        private class XAIResponse
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
                [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
            }
        }
    }
}
