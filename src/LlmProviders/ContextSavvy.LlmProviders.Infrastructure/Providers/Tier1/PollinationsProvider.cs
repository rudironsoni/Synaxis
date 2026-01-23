using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1
{
    public class PollinationsProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PollinationsProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "openai", "deepseek", "qwen", "flux", "turbo", "kontext", "openai-vision", "gpt-4o-mini-audio"
        };

        public string Id => "pollinations";
        public string Name => "Pollinations";
        public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public PollinationsProvider(HttpClient httpClient, ILogger<PollinationsProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var prompt = BuildPrompt(request.Messages);
            var url = $"https://text.pollinations.ai/{HttpUtility.UrlEncode(prompt)}";

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var inputTokens = EstimateTokens(prompt.Length);
            var outputTokens = EstimateTokens(content.Length);

            return new ChatCompletionResult(
                $"pollinations-{Guid.NewGuid():N}",
                content,
                "stop",
                new Usage(inputTokens, outputTokens, inputTokens + outputTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private string BuildPrompt(List<Message> messages)
        {
            return string.Join("\n", messages.Select(m => $"{m.Role}: {m.Content}"));
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);
    }
}
