using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier2
{
    public class QwenProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<QwenProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "qwen-3-235b-a22b", "qwen-3-72b", "qwen-3-coder-32b", "qwen-3-30b-a3b",
            "qwen-3-32b", "qwen-3-14b", "qwen-3-8b", "qwen-3-4b", "qwen-3-1.7b", "qwen-3-0.6b"
        };

        public string Id => "qwen";
        public string Name => "Qwen";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public QwenProvider(HttpClient httpClient, ILogger<QwenProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://qwen.ai");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "qwen-3-72b" : request.Model;

            var payload = new
            {
                model = model,
                input = new
                {
                    messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList()
                },
                parameters = new
                {
                    max_output_tokens = request.MaxTokens,
                    temperature = request.Temperature
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/chat", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<QwenResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var text = result?.Output?.Text ?? result?.Output?.Messages?.FirstOrDefault()?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid Qwen response");
            }

            var promptTokens = result?.Usage?.InputTokens ?? EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = result?.Usage?.OutputTokens ?? EstimateTokens(text.Length);

            return new ChatCompletionResult(
                $"qwen-{Guid.NewGuid():N}",
                text,
                result?.Output?.FinishReason ?? "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class QwenResponse
        {
            public OutputInfo? Output { get; set; }
            public UsageInfo? Usage { get; set; }

            public class OutputInfo
            {
                public string Text { get; set; } = "";
                public List<MessageInfo>? Messages { get; set; }
                public string FinishReason { get; set; } = "";
            }

            public class MessageInfo
            {
                public string Role { get; set; } = "";
                public string Content { get; set; } = "";
            }

            public class UsageInfo
            {
                public int InputTokens { get; set; }
                public int OutputTokens { get; set; }
            }
        }
    }
}
