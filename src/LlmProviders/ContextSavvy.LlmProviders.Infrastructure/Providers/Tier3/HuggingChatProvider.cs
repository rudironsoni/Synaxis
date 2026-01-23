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
    public class HuggingChatProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<HuggingChatProvider> _logger;
        private readonly string? _apiKey;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "llama-3.3-70b", "mixtral-8x7b", "qwen-72b", "mistral-large"
        };

        public string Id => "huggingchat";
        public string Name => "HuggingChat";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public HuggingChatProvider(
            HttpClient httpClient,
            CookieManager cookieManager,
            ILogger<HuggingChatProvider> logger,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _cookieManager = cookieManager;
            _logger = logger;
            _apiKey = config["HuggingFace:ApiKey"] ?? Environment.GetEnvironmentVariable("HUGGINGFACE_TOKEN");
            _httpClient.BaseAddress = new Uri("https://huggingface.co");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "llama-3.3-70b" : request.Model;
            var modelId = model switch
            {
                "llama-3.3-70b" => "meta-llama/Llama-3.3-70B-Instruct",
                "mixtral-8x7b" => "mistralai/Mixtral-8x7B-Instruct-v0.1",
                "qwen-72b" => "Qwen/Qwen-2.5-72B-Instruct",
                _ => "meta-llama/Llama-3.3-70B-Instruct"
            };

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }
            else
            {
                var cookies = await _cookieManager.GetCookiesAsync("huggingchat");
                if (cookies.Count > 0)
                {
                    _httpClient.DefaultRequestHeaders.Remove("Cookie");
                    var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Key}={c.Value}"));
                    _httpClient.DefaultRequestHeaders.Add("Cookie", cookieHeader);
                }
            }

            var payload = new
            {
                model = modelId,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/api/chat/{modelId}", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HuggingChatResponse>(cancellationToken: ct);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid HuggingChat response");
            }

            var promptTokens = result?.Usage?.PromptTokens ?? EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = result?.Usage?.CompletionTokens ?? EstimateTokens(text.Length);

            return new ChatCompletionResult(
                $"huggingchat-{Guid.NewGuid():N}",
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

        private class HuggingChatResponse
        {
            public List<ChoiceInfo>? Choices { get; set; }
            public UsageInfo? Usage { get; set; }

            public class ChoiceInfo
            {
                public MessageInfo? Message { get; set; }
            }

            public class MessageInfo
            {
                public string Content { get; set; } = "";
            }

            public class UsageInfo
            {
                [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
                [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
            }
        }
    }
}
