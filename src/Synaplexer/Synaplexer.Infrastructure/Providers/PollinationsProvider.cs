using System.Runtime.CompilerServices;
using System.Text;
using System.Web;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using Synaplexer.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Synaplexer.Infrastructure.Providers
{
    public class PollinationsProvider : BaseLlmProvider
    {
        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "openai", "deepseek", "qwen", "flux", "turbo", "kontext", "openai-vision", "gpt-4o-mini-audio"
        };

        public override string Id => "pollinations";
        public override string Name => "Pollinations";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public PollinationsProvider(HttpClient httpClient, ILogger<PollinationsProvider> logger, IOptionsSnapshot<ProvidersOptions> options)
            : base(httpClient, logger, options, "Pollinations")
        {
        }

        public override bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var prompt = BuildPrompt(request.Messages);
            var url = $"https://text.pollinations.ai/{HttpUtility.UrlEncode(prompt)}";

            var response = await Http.GetAsync(url, ct);
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

        public override async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
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
