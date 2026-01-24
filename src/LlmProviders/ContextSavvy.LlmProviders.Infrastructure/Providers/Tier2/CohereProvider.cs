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
    public class CohereProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CohereProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "command-r", "command-r-plus", "command-a"
        };

        public string Id => "cohere";
        public string Name => "Cohere";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public CohereProvider(HttpClient httpClient, ILogger<CohereProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.cohere.ai");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "command-r" : request.Model;
            var systemPrompt = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content ?? "";
            var userMessages = request.Messages.Where(m => m.Role != "system").ToList();

            var payload = new
            {
                model = model,
                message = string.Join("\n", userMessages.Select(m => $"<{m.Role.ToUpper()}>{m.Content}</{m.Role.ToUpper()}>")),
                chat_history = userMessages
                    .Where(m => m.Role != "user" || m != userMessages.Last())
                    .Select(m => new
                    {
                        role = m.Role == "assistant" ? "CHATBOT" : m.Role.ToUpper(),
                        message = m.Content
                    })
                    .ToList(),
                preamble = systemPrompt,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat", content, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CohereResponse>(cancellationToken: ct);
            var text = result?.Text ?? result?.ChatHistory?.Message ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid Cohere response");
            }

            var promptTokens = result?.Usage?.InputTokens ?? EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = result?.Usage?.OutputTokens ?? EstimateTokens(text.Length);

            return new ChatCompletionResult(
                $"cohere-{Guid.NewGuid():N}",
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

        private class CohereResponse
        {
            public string Text { get; set; } = "";
            public ChatHistoryItem? ChatHistory { get; set; }
            public UsageInfo? Usage { get; set; }

            public class ChatHistoryItem
            {
                public string Message { get; set; } = "";
            }

            public class UsageInfo
            {
                [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
                [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
            }
        }
    }
}
