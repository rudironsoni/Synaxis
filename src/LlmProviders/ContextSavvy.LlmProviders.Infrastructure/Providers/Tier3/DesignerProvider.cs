using System.Runtime.CompilerServices;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier3
{
    public class DesignerProvider : ILlmProvider
    {
        private readonly HttpClient _httpClient;
        private readonly CookieManager _cookieManager;
        private readonly ILogger<DesignerProvider> _logger;

        private static readonly HashSet<string> AvailableModels = new(StringComparer.OrdinalIgnoreCase)
        {
            "image"
        };

        public string Id => "designer";
        public string Name => "Microsoft Designer";
        public ProviderTier Tier => ProviderTier.Tier3_Ghost;

        public DesignerProvider(
            HttpClient httpClient,
            CookieManager cookieManager,
            ILogger<DesignerProvider> logger)
        {
            _httpClient = httpClient;
            _cookieManager = cookieManager;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://designer.microsoft.com");
        }

        public bool SupportsModel(string modelId) => AvailableModels.Contains(modelId);

        public Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            throw new NotSupportedException("Designer is for image generation only");
        }

        public IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            throw new NotSupportedException("Designer is for image generation only");
        }
    }
}
