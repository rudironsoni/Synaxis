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
    public class NousResearchProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<NousResearchProvider> _logger;
        private readonly string? _nousApiKey;

        private const string NousBaseUrl = "https://api.nousresearch.com/v1";

        public string Id => "nousresearch";
        public string Name => "NousResearch";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "nousresearch/hermes-4-405b", "nousresearch/hermes-4-405b-instruct",
            "nousresearch/hermes-3-405b", "nousresearch/hermes-3-llama-3.1-405b", "nousresearch/hermes-3-llama-3.1-70b",
            "nousresearch/hermes-2-pro-mistral-7b", "nousresearch/nous-hermes-llama2-13b",
            "hermes-4", "hermes-4-405b", "hermes-3", "hermes-3-405b"
        };

        public NousResearchProvider(
            HttpClient http,
            ILogger<NousResearchProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _nousApiKey = config["NousResearch:ApiKey"] ?? Environment.GetEnvironmentVariable("NOUSRESEARCH_API_KEY");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var (endpoint, apiKey) = GetApiEndpoint();

            var payload = new
            {
                model = modelId,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}/chat/completions");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _http.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NousResponse>(cancellationToken: ct);
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("Empty response from Nous Research");
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
                "hermes-4" or "hermes-4-405b" => "nousresearch/hermes-4-405b",
                "hermes-3" or "hermes-3-405b" => "nousresearch/hermes-3-llama-3.1-405b",
                _ => model
            };
        }

        private (string endpoint, string apiKey) GetApiEndpoint()
        {
            if (!string.IsNullOrEmpty(_nousApiKey)) return (NousBaseUrl, _nousApiKey);
            throw new InvalidOperationException("No API key configured for Nous Research");
        }

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
                [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
            }
        }
    }
}
