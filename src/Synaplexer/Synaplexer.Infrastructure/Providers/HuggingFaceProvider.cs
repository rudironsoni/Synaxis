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
    public class HuggingFaceProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "meta-llama/Llama-3.3-70B-Instruct", "Qwen/Qwen-2.5-72B-Instruct",
            "deepseek-ai/DeepSeek-R1", "mistralai/Mixtral-8x7B-Instruct-v0.1",
            "meta-llama/Llama-3.1-8B-Instruct", "google/gemma-2-27b-it"
        };

        public override string Id => "huggingface";
        public override string Name => "HuggingFace";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HuggingFaceProvider(HttpClient httpClient, ILogger<HuggingFaceProvider> logger, IConfiguration config)
            : base(httpClient, logger, config, "HuggingFace")
        {
        }

        public override bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            var model = string.IsNullOrEmpty(request.Model) ? "meta-llama/Llama-3.3-70B-Instruct" : request.Model;

            var payload = new
            {
                model = model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = request.Temperature,
                max_completion_tokens = request.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, $"https://router.huggingface.co/{model}");
            requestMsg.Content = content;
            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await Http.SendAsync(requestMsg, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<HFResponse>(cancellationToken: ct);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid HuggingFace response");
            }

            return new ChatCompletionResult(
                result?.Id ?? $"hf-{Guid.NewGuid():N}",
                text,
                result?.Choices?.FirstOrDefault()?.FinishReason ?? "stop",
                new Usage(result?.Usage?.PromptTokens ?? 0, result?.Usage?.CompletionTokens ?? 0, result?.Usage?.TotalTokens ?? 0)
            );
        }

        public override async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private class HFResponse
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
                public string Content { get; set; } = "";
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
