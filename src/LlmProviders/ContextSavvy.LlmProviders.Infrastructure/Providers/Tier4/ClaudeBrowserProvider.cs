using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using ***REMOVED***.Extensions.Inference.Claude;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier4
{
    public class ClaudeBrowserProvider : ILlmProvider
    {
        private readonly ClaudeProvider _innerProvider;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "claude-3-opus", "claude-3-sonnet"
        };

        public string Id => "claude-browser";
        public string Name => "Claude Browser";
        public ProviderTier Tier => ProviderTier.Tier4_Experimental;

        public ClaudeBrowserProvider(ClaudeProvider innerProvider)
        {
            _innerProvider = innerProvider;
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var prompt = string.Join("\n", request.Messages.Select(m => $"{m.Role}: {m.Content}"));
            var response = await _innerProvider.GenerateAsync(prompt, null, ct);

            var promptTokens = EstimateTokens(prompt.Length);
            var completionTokens = EstimateTokens(response.Length);

            return new ChatCompletionResult(
                $"claudebrowser-{Guid.NewGuid():N}",
                response,
                "stop",
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var prompt = string.Join("\n", request.Messages.Select(m => $"{m.Role}: {m.Content}"));
            var id = $"claudebrowser-{Guid.NewGuid():N}";

            await foreach (var chunk in _innerProvider.StreamGenerateAsync(prompt, null, ct))
            {
                yield return new ChatCompletionChunk(id, chunk, string.Empty);
            }

            yield return new ChatCompletionChunk(id, string.Empty, "stop");
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);
    }
}
