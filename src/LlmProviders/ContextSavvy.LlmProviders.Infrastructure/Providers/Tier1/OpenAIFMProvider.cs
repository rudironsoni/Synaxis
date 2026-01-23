using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1
{
    public class OpenAIFMProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIFMProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "nova", "alloy", "echo", "fable", "shimmer", "onyx", "cowboy", "ballad",
            "sage", "friendly", "scientific_style", "calm", "coral", "ash", "noir_detective",
            "patient_teacher", "verse"
        };

        public string Id => "openaifm";
        public string Name => "openAIFM";
        public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public OpenAIFMProvider(HttpClient httpClient, ILogger<OpenAIFMProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var model = string.IsNullOrEmpty(request.Model) ? "nova" : request.Model;
            var url = $"https://api.openai.fm/v1/{model}";
            
            var payload = new
            {
                model = model,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                temperature = request.Temperature,
                max_tokens = request.MaxTokens
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAIFMResponse>(cancellationToken: ct);
            var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Invalid OpenAIFM response");
            }

            var promptTokens = EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = EstimateTokens(text.Length);

            return new ChatCompletionResult(
                $"openAIFM-{Guid.NewGuid():N}",
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

        private class OpenAIFMResponse
        {
            public List<ChoiceInfo>? Choices { get; set; }

            public class ChoiceInfo
            {
                public MessageInfo? Message { get; set; }
            }

            public class MessageInfo
            {
                public string Content { get; set; } = "";
            }
        }
    }
}
