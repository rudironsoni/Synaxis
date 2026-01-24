using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Synaplexer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Synaplexer.Infrastructure.Providers
{
    public class GeminiProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "gemini-2.0-flash",
            "gemini-2.0-flash-lite",
            "gemini-1.5-flash"
        };

        public override string Id => "gemini";
        public override string Name => "Gemini";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public GeminiProvider(
            HttpClient httpClient,
            ILogger<GeminiProvider> logger,
            IOptionsSnapshot<ProvidersOptions> options)
            : base(httpClient, logger, options, "Gemini")
        {
        }

        public override bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API Key is missing.");
            }

            var model = string.IsNullOrEmpty(request.Model) ? "gemini-2.0-flash" : request.Model;
            var apiModel = model.Replace("gemini/", "");

            var contents = request.Messages.Select(m => new
            {
                role = m.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = m.Content } }
            }).ToList();

            var payload = new
            {
                contents = contents,
                generationConfig = new
                {
                    maxOutputTokens = request.MaxTokens,
                    temperature = request.Temperature
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{apiModel}:generateContent?key={apiKey}";

            var response = await Http.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid Gemini response");
            }

            var promptTokens = EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = EstimateTokens(text.Length);

            return new ChatCompletionResult(
                $"gemini-{Guid.NewGuid():N}",
                text,
                "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public override async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class GeminiResponse
        {
            public List<CandidateInfo>? Candidates { get; set; }

            public class CandidateInfo
            {
                public ContentInfo? Content { get; set; }
            }

            public class ContentInfo
            {
                public List<PartInfo>? Parts { get; set; }
            }

            public class PartInfo
            {
                public string? Text { get; set; }
            }
        }
    }
}
