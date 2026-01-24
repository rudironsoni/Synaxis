using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Synaplexer.Infrastructure.Providers
{
    public class CohereProvider : BaseLlmProvider
    {
        private const string BaseUrl = "https://api.cohere.com/v2";

        public override string Id => "cohere-api";
        public override string Name => "Cohere";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "command-r-plus-08-2024", "command-r-08-2024", "command-r-plus", "command-r", "command", "command-light",
            "cohere/command-r", "cohere/command-r-plus"
        };

        public CohereProvider(
            HttpClient http,
            ILogger<CohereProvider> logger,
            IConfiguration config)
            : base(http, logger, config, "Cohere")
        {
        }

        public override bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            var cohereRequest = BuildRequest(request, stream: false);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat");
            httpRequest.Content = JsonContent.Create(cohereRequest);

            if (!string.IsNullOrEmpty(apiKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            }

            var response = await Http.SendAsync(httpRequest, ct);
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

        public override async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
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
