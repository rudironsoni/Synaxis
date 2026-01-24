using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Synaplexer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Synaplexer.Infrastructure.Providers
{
    public class OpenRouterProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "liquid/lfm-2.5-1.2b-instruct:free"
        };

        public override string Id => "openrouter";
        public override string Name => "OpenRouter";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public OpenRouterProvider(HttpClient httpClient, ILogger<OpenRouterProvider> logger, IOptionsSnapshot<ProvidersOptions> options)
            : base(httpClient, logger, options, "OpenRouter")
        {
        }

        public override bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            var model = string.IsNullOrEmpty(request.Model) ? "liquid/lfm-2.5-1.2b-instruct:free" : request.Model;

            var payload = new
            {
                model = model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            requestMsg.Content = content;
            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            requestMsg.Headers.Add("HTTP-Referer", "https://synaplexer.ai");
            requestMsg.Headers.Add("X-Title", "Synaplexer");

            var response = await Http.SendAsync(requestMsg, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenRouterResponse>(cancellationToken: ct);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid OpenRouter response");
            }

            return new ChatCompletionResult(
                result?.Id ?? $"openrouter-{Guid.NewGuid():N}",
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

        private class OpenRouterResponse
        {
            public string Id { get; set; } = "";
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
