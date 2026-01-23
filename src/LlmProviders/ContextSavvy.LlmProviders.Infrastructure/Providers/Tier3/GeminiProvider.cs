using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier3
{
    public class GeminiProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<GeminiProvider> _logger;
        private readonly string? _apiKey;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "gemini-2.5-flash", "gemini-2.5-pro", "gemini-1.5-pro", "gemini-1.5-flash"
        };

        public string Id => "gemini-tier3";
        public string Name => "Gemini (Tier 3)";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public GeminiProvider(
            HttpClient httpClient,
            CookieManager cookieManager,
            ILogger<GeminiProvider> logger,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _cookieManager = cookieManager;
            _logger = logger;
            _apiKey = config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "gemini-2.5-flash" : request.Model;
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

            var url = $"/v1beta/models/{apiModel}:generateContent";

            if (!string.IsNullOrEmpty(_apiKey))
            {
                url += $"?key={_apiKey}";
            }
            else
            {
                var cookies = await _cookieManager.GetCookiesAsync("gemini");
                if (cookies.Count > 0)
                {
                    _httpClient.DefaultRequestHeaders.Remove("Cookie");
                    var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
                    _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
                }
            }

            var response = await _httpClient.PostAsync(url, content, ct);
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

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
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
