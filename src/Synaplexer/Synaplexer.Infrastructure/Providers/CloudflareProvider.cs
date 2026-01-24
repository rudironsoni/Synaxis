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
    /// <summary>
    /// Cloudflare Workers AI provider for accessing Llama models.
    /// Completely free with generous rate limits.
    /// </summary>
    public class CloudflareProvider : BaseLlmProvider
    {
        private readonly string? _accountId;

        public override string Id => "cloudflare";
        public override string Name => "Cloudflare";
        public override ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "@cf/meta/llama-3-8b-instruct",
            "@cf/meta/llama-3-70b-instruct",
            "@cf/meta/llama-3.1-8b-instruct",
            "@cf/meta/llama-3.1-70b-instruct-fp8",
            "@cf/meta/llama-3.2-1b-instruct",
            "@cf/meta/llama-3.2-3b-instruct",
            "@cf/meta/llama-2-7b-chat-int8",
            "@cf/mistral/mistral-7b-instruct-v0.1",
            "@cf/qwen/qwen1.5-14b-chat-awq",
            "@cf/deepseek-ai/deepseek-math-7b-instruct",

            // Aliases
            "llama-3-8b",
            "llama-3-70b",
            "llama-3.1-8b",
            "llama-3.1-70b",
            "mistral-7b"
        };

        public CloudflareProvider(
            HttpClient http,
            ILogger<CloudflareProvider> logger,
            IConfiguration config)
            : base(http, logger, config, "Cloudflare")
        {
            _accountId = config.GetSection("Providers:Cloudflare")["AccountId"] ?? Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID");
        }

        public override bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public override async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var apiToken = GetApiKey();
            var modelId = ResolveModel(request.Model);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{modelId}";

            var payload = new
            {
                messages = request.Messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }),
                max_tokens = request.MaxTokens,
                stream = false
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrEmpty(apiToken))
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {apiToken}");
            }

            var response = await Http.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Cloudflare API error: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<CloudflareResponse>(cancellationToken: ct);
            if (result?.Result == null)
            {
                throw new InvalidOperationException("Empty response from Cloudflare");
            }

            // Cloudflare doesn't provide token counts, estimate
            var inputTokens = EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var outputTokens = EstimateTokens(result.Result.Response?.Length ?? 0);

            return new ChatCompletionResult(
                Guid.NewGuid().ToString(),
                result.Result.Response ?? "",
                "stop",
                new Usage(inputTokens, outputTokens, inputTokens + outputTokens)
            );
        }

        public override async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var apiToken = GetApiKey();
            var modelId = ResolveModel(request.Model);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{modelId}";

            var payload = new
            {
                messages = request.Messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }),
                max_tokens = request.MaxTokens,
                stream = true
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrEmpty(apiToken))
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {apiToken}");
            }

            var response = await Http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Cloudflare API error: {response.StatusCode} - {error}");
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            var responseId = Guid.NewGuid().ToString();

            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrEmpty(line)) continue;

                if (!line.StartsWith("data: ")) continue;

                var data = line[6..];
                if (data == "[DONE]") break;

                CloudflareStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<CloudflareStreamChunk>(data);
                }
                catch
                {
                    continue;
                }

                yield return new ChatCompletionChunk(
                    responseId,
                    chunk?.Response ?? "",
                    ""
                );
            }
        }

        private string ResolveModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "llama-3-8b" => "@cf/meta/llama-3-8b-instruct",
                "llama-3-70b" => "@cf/meta/llama-3-70b-instruct",
                "llama-3.1-8b" => "@cf/meta/llama-3.1-8b-instruct",
                "llama-3.1-70b" => "@cf/meta/llama-3.1-70b-instruct-fp8",
                "mistral-7b" => "@cf/mistral/mistral-7b-instruct-v0.1",
                _ => model
            };
        }

        private static int EstimateTokens(int charCount)
        {
            return Math.Max(1, charCount / 4);
        }
    }

    #region Cloudflare Response Models

    internal class CloudflareResponse
    {
        [JsonPropertyName("result")]
        public CloudflareResult? Result { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }

    internal class CloudflareResult
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    internal class CloudflareStreamChunk
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    #endregion
}
