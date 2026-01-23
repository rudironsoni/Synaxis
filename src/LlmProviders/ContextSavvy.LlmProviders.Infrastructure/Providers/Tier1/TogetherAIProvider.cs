using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier1
{
    public class TogetherAIProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<TogetherAIProvider> _logger;
        private readonly string? _apiKey;

        private const string BaseUrl = "https://api.together.xyz/v1";

        public string Id => "togetherai";
        public string Name => "TogetherAI";
        public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "Qwen/Qwen3-235B-A22B",
            "Qwen/Qwen2.5-72B-Instruct-Turbo",
            "Qwen/Qwen2.5-Coder-32B-Instruct",
            "Qwen/QwQ-32B-Preview",
            "deepseek-ai/DeepSeek-V3",
            "deepseek-ai/DeepSeek-R1-Distill-Llama-70B",
            "deepseek-ai/DeepSeek-R1-Distill-Qwen-32B",
            "meta-llama/Llama-3.3-70B-Instruct-Turbo",
            "meta-llama/Meta-Llama-3.1-405B-Instruct-Turbo",
            "meta-llama/Meta-Llama-3.1-70B-Instruct-Turbo",
            "meta-llama/Meta-Llama-3.1-8B-Instruct-Turbo",
            "meta-llama/Llama-4-Scout-17B-16E-Instruct",
            "mistralai/Mixtral-8x22B-Instruct-v0.1",
            "mistralai/Mixtral-8x7B-Instruct-v0.1",
            "mistralai/Mistral-7B-Instruct-v0.3",
            "google/gemma-2-27b-it",
            "google/gemma-2-9b-it",
            "qwen-3-235b",
            "llama-4-scout",
            "deepseek-v3",
            "mixtral-8x22b"
        };

        public TogetherAIProvider(
            HttpClient http,
            ILogger<TogetherAIProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["TogetherAI:ApiKey"] ?? Environment.GetEnvironmentVariable("TOGETHER_AI_API_KEY");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = BuildPayload(request, modelId, stream: false);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            }

            var response = await _http.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Together AI API error: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<TogetherResponse>(cancellationToken: ct);
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("Empty response from Together AI");
            }

            return new ChatCompletionResult(
                result.Id ?? Guid.NewGuid().ToString(),
                result.Choices[0].Message?.Content ?? "",
                result.Choices[0].FinishReason ?? "stop",
                new Usage(result.Usage?.PromptTokens ?? 0, result.Usage?.CompletionTokens ?? 0, result.Usage?.TotalTokens ?? 0)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = BuildPayload(request, modelId, stream: true);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            }

            var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Together AI API error: {response.StatusCode} - {error}");
            }

            var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            var responseId = Guid.NewGuid().ToString();

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrEmpty(line)) continue;

                if (!line.StartsWith("data: ")) continue;

                var data = line[6..];
                if (data == "[DONE]") break;

                TogetherStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<TogetherStreamChunk>(data);
                }
                catch
                {
                    continue;
                }

                if (chunk?.Choices == null || chunk.Choices.Count == 0) continue;

                yield return new ChatCompletionChunk(
                    chunk.Id ?? responseId,
                    chunk.Choices[0].Delta?.Content ?? "",
                    chunk.Choices[0].FinishReason
                );
            }
        }

        private string ResolveModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "qwen-3-235b" => "Qwen/Qwen3-235B-A22B",
                "llama-4-scout" => "meta-llama/Llama-4-Scout-17B-16E-Instruct",
                "deepseek-v3" => "deepseek-ai/DeepSeek-V3",
                "mixtral-8x22b" => "mistralai/Mixtral-8x22B-Instruct-v0.1",
                _ => model
            };
        }

        private object BuildPayload(ChatRequest request, string modelId, bool stream)
        {
            return new
            {
                model = modelId,
                messages = request.Messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content
                }),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = stream
            };
        }
    }

    #region Together AI Response Models

    internal class TogetherResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public List<TogetherChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public TogetherUsage? Usage { get; set; }
    }

    internal class TogetherChoice
    {
        [JsonPropertyName("message")]
        public TogetherMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    internal class TogetherMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    internal class TogetherUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    internal class TogetherStreamChunk
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public List<TogetherStreamChoice>? Choices { get; set; }
    }

    internal class TogetherStreamChoice
    {
        [JsonPropertyName("delta")]
        public TogetherDelta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    internal class TogetherDelta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    #endregion
}
