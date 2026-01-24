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
    public class GroqProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "llama-3.1-8b-instant",
            "llama-3.3-70b-versatile",
            "meta-llama/llama-guard-4-12b",
            "openai/gpt-oss-120b",
            "openai/gpt-oss-20b",
            "groq/compound",
            "groq/compound-mini",
            "meta-llama/llama-4-maverick-17b-128e-instruct",
            "meta-llama/llama-4-scout-17b-16e-instruct",
            "meta-llama/llama-prompt-guard-2-22m",
            "meta-llama/llama-prompt-guard-2-86m",
            "moonshotai/kimi-k2-instruct-0905",
            "openai/gpt-oss-safeguard-20b",
            "qwen/qwen3-32b",
            "allam-2-7b"
        };

        public override string Id => "groq";
        public override string Name => "Groq";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public GroqProvider(HttpClient httpClient, ILogger<GroqProvider> logger, IOptionsSnapshot<ProvidersOptions> options)
            : base(httpClient, logger, options, "Groq")
        {
        }

        public override bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiKey = GetApiKey();
            var model = string.IsNullOrEmpty(request.Model) ? "llama-3.3-70b-versatile" : request.Model;

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

            var requestMsg = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            requestMsg.Content = content;
            if (!string.IsNullOrEmpty(apiKey))
            {
                requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await Http.SendAsync(requestMsg, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GroqResponse>(cancellationToken: ct);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid Groq response");
            }

            return new ChatCompletionResult(
                result?.Id ?? $"groq-{Guid.NewGuid():N}",
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

        private class GroqResponse
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
