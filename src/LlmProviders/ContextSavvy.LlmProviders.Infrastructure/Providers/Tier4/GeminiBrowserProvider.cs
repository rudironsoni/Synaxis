using Ghostwright;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Runtime.CompilerServices;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier4
{
    public class GeminiBrowserProvider : ILlmProvider
    {
        private readonly IGhostDriver _driver;
        private readonly ILogger<GeminiBrowserProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "gemini-2.0-flash", "gemini-2.0-flash-thinking"
        };

        public string Id => "gemini-browser";
        public string Name => "Gemini Browser";
        public ProviderTier Tier => ProviderTier.Tier4_Experimental;

        public GeminiBrowserProvider(
            IGhostDriver driver,
            ILogger<GeminiBrowserProvider> logger)
        {
            _driver = driver;
            _logger = logger;
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var prompt = string.Join("\n", request.Messages.Select(m => $"{m.Role}: {m.Content}"));
            var page = await _driver.Context.NewPageAsync();

            try
            {
                await page.GotoAsync("https://gemini.google.com");
                await Task.Delay(2000, ct);

                var inputSelector = await FindInputSelector(page);
                if (string.IsNullOrEmpty(inputSelector))
                {
                    throw new Exception("Could not find Gemini input field");
                }

                await page.FillAsync(inputSelector, prompt);
                await page.Keyboard.PressAsync("Enter");
                await Task.Delay(3000, ct);

                var response = await ExtractResponse(page);

                var promptTokens = EstimateTokens(prompt.Length);
                var completionTokens = EstimateTokens(response.Length);

                return new ChatCompletionResult(
                    $"geminibrowser-{Guid.NewGuid():N}",
                    response,
                    "stop",
                    new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
                );
            }
            finally
            {
                await page.CloseAsync();
            }
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private async Task<string> FindInputSelector(IPage page)
        {
            var selectors = new[] { "[data-test-id='gemini-input']", "textarea[aria-label*='Enter your prompt']", ".gemini-input textarea" };
            foreach (var selector in selectors)
            {
                if (await page.QuerySelectorAsync(selector) != null) return selector;
            }
            return "";
        }

        private async Task<string> ExtractResponse(IPage page)
        {
            var selectors = new[] { "[data-test-id='gemini-response']", ".gemini-response", ".model-response-text" };
            foreach (var selector in selectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                if (element != null) return await element.InnerTextAsync();
            }
            return "";
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);
    }
}
