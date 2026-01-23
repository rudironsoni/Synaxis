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
    public class LambdaChatProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<LambdaChatProvider> _logger;

        private const string BaseUrl = "https://api.lambdachat.com/v1";

        public string Id => "lambdachat";
        public string Name => "LambdaChat";
        public ProviderTier Tier => ProviderTier.Tier1_FreeFast;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "meta-llama/Llama-3.3-70B-Instruct",
            "meta-llama/Meta-Llama-3.1-70B-Instruct",
            "meta-llama/Meta-Llama-3.1-8B-Instruct",
            "Qwen/Qwen2.5-72B-Instruct",
            "Qwen/Qwen2.5-32B-Instruct",
            "mistralai/Mixtral-8x7B-Instruct-v0.1",
            "llama-3.3-70b-lambda",
            "qwen-2.5-72b-lambda"
        };

        public LambdaChatProvider(
            HttpClient http,
            ILogger<LambdaChatProvider> logger)
        {
            _http = http;
            _logger = logger;
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = new
            {
                model = modelId,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LambdaChatResponse>(cancellationToken: ct);
            if (result?.Choices == null || result.Choices.Count == 0)
            {
                throw new InvalidOperationException("Empty response from LambdaChat");
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
            var payload = new
            {
                model = modelId,
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToList(),
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = true
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

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

                LambdaChatStreamChunk? chunk;
                try { chunk = JsonSerializer.Deserialize<LambdaChatStreamChunk>(data); }
                catch { continue; }

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
                "llama-3.3-70b-lambda" => "meta-llama/Llama-3.3-70B-Instruct",
                "qwen-2.5-72b-lambda" => "Qwen/Qwen2.5-72B-Instruct",
                _ => model
            };
        }

        private class LambdaChatResponse
        {
            public string? Id { get; set; }
            public List<ChoiceInfo>? Choices { get; set; }
            public UsageInfo? Usage { get; set; }

            public class ChoiceInfo
            {
                public MessageInfo? Message { get; set; }
                public string? FinishReason { get; set; }
            }

            public class MessageInfo
            {
                public string? Content { get; set; }
            }

            public class UsageInfo
            {
                [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }
                [JsonPropertyName("completion_tokens")] public int CompletionTokens { get; set; }
                [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
            }
        }

        private class LambdaChatStreamChunk
        {
            public string? Id { get; set; }
            public List<StreamChoiceInfo>? Choices { get; set; }

            public class StreamChoiceInfo
            {
                public DeltaInfo? Delta { get; set; }
                public string? FinishReason { get; set; }
            }

            public class DeltaInfo
            {
                public string? Content { get; set; }
            }
        }
    }
}
