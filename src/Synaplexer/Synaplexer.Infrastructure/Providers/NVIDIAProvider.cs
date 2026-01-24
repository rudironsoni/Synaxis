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
    public class NVIDIAProvider : BaseLlmProvider
    {
        private const string BaseUrl = "https://integrate.api.nvidia.com/v1";

        public override string Id => "nvidia";
        public override string Name => "NVIDIA";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "meta/llama-3.3-70b-instruct", "meta/llama-3.1-405b-instruct", "meta/llama-3.1-70b-instruct", "meta/llama-3.1-8b-instruct",
            "mistralai/mixtral-8x7b-instruct-v0.1", "mistralai/mixtral-8x22b-instruct-v0.1", "mistralai/mistral-large-latest",
            "nvidia/nemotron-4-340b-instruct", "microsoft/phi-3-mini-128k-instruct", "microsoft/phi-3-medium-128k-instruct",
            "meta/codellama-70b-instruct", "deepseek-ai/deepseek-coder-33b-instruct",
            "llama-3.3-70b", "llama-3.1-405b", "llama-3.1-70b", "nemotron-70b", "phi-3", "codellama"
        };

        public NVIDIAProvider(
            HttpClient http,
            ILogger<NVIDIAProvider> logger,
            IConfiguration config)
            : base(http, logger, config, "NVIDIA")
        {
        }

        public override bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            var nvRequest = BuildRequest(request, stream: false);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = JsonContent.Create(nvRequest);

            if (!string.IsNullOrEmpty(apiKey))
            {
                httpRequest.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
            }

            var response = await Http.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<NVIDIAChatResponse>(cancellationToken: ct);
            var choice = result?.Choices?.FirstOrDefault();
            var content = choice?.Message?.Content ?? "";

            return new ChatCompletionResult(
                result?.Id ?? Guid.NewGuid().ToString(),
                content,
                choice?.FinishReason ?? "stop",
                new Usage(result?.Usage?.PromptTokens ?? 0, result?.Usage?.CompletionTokens ?? 0, result?.Usage?.TotalTokens ?? 0)
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
                "llama-3.3-70b" or "llama3-70b" => "meta/llama-3.3-70b-instruct",
                "llama-3.1-405b" or "llama-405b" => "meta/llama-3.1-405b-instruct",
                "llama-3.1-70b" => "meta/llama-3.1-70b-instruct",
                "llama-3.1-8b" => "meta/llama-3.1-8b-instruct",
                "mixtral-8x7b" => "mistralai/mixtral-8x7b-instruct-v0.1",
                "mixtral-8x22b" => "mistralai/mixtral-8x22b-instruct-v0.1",
                "mistral-large" => "mistralai/mistral-large-latest",
                "nemotron-70b" => "nvidia/nemotron-4-340b-instruct",
                "phi-3" or "phi-3-mini" => "microsoft/phi-3-mini-128k-instruct",
                "phi-3-medium" => "microsoft/phi-3-medium-128k-instruct",
                "codellama" or "codellama-70b" => "meta/codellama-70b-instruct",
                "deepseek-coder" => "deepseek-ai/deepseek-coder-33b-instruct",
                _ => model
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class NVIDIAChatResponse
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
