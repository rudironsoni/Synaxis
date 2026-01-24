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
    public class GroqProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "llama-3.3-70b-versatile", "llama-3.3-70b-instruct", "llama3-8b-8192",
            "llama-3.1-8b-instruct", "llama-3.1-70b-versatile", "mixtral-8x7b-32768",
            "gemma-7b-it"
        };

        public override string Id => "groq";
        public override string Name => "Groq";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public GroqProvider(HttpClient httpClient, ILogger<GroqProvider> logger, IConfiguration config)
            : base(httpClient, logger, config, "Groq")
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
