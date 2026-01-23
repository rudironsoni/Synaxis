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
    public class CohereAPIProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<CohereAPIProvider> _logger;
        private readonly string? _apiKey;

        private const string BaseUrl = "https://api.cohere.com/v2";

        public string Id => "cohere-api";
        public string Name => "Cohere";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "command-r-plus-08-2024", "command-r-08-2024", "command-r-plus", "command-r", "command", "command-light",
            "cohere/command-r", "cohere/command-r-plus"
        };

        public CohereAPIProvider(
            HttpClient http,
            ILogger<CohereAPIProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["Cohere:ApiKey"] ?? Environment.GetEnvironmentVariable("COHERE_API_KEY");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
            }
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var cohereRequest = BuildRequest(request, stream: false);
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/chat", cohereRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CohereChatResponse>(cancellationToken: ct);
            var content = result?.Message?.Content?.FirstOrDefault()?.Text ?? "";

            return new ChatCompletionResult(
                result?.Id ?? Guid.NewGuid().ToString(),
                content,
                result?.FinishReason ?? "complete",
                new Usage(result?.Usage?.InputTokens ?? 0, result?.Usage?.OutputTokens ?? 0, (result?.Usage?.InputTokens ?? 0) + (result?.Usage?.OutputTokens ?? 0))
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private object BuildRequest(ChatRequest request, bool stream)
        {
            return new
            {
                model = NormalizeModel(request.Model),
                messages = request.Messages.Select(m => new { role = m.Role.ToLowerInvariant(), content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = stream
            };
        }

        private static string NormalizeModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "command-r" or "cohere/command-r" => "command-r-08-2024",
                "command-r-plus" or "cohere/command-r-plus" => "command-r-plus-08-2024",
                _ => model
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class CohereChatResponse
        {
            public string? Id { get; set; }
            public MessageInfo? Message { get; set; }
            public string? FinishReason { get; set; }
            public UsageInfo? Usage { get; set; }

            public class MessageInfo
            {
                public List<ContentInfo>? Content { get; set; }
            }

            public class ContentInfo
            {
                public string? Text { get; set; }
            }

            public class UsageInfo
            {
                [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
                [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
            }
        }
    }
}
