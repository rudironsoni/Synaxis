using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSavvy.LlmProviders.Domain.Interfaces;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier2
{
    public class AnthropicProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<AnthropicProvider> _logger;
        private readonly string? _apiKey;

        private const string BaseUrl = "https://api.anthropic.com/v1";
        private const string ApiVersion = "2024-01-01";

        public string Id => "anthropic";
        public string Name => "Anthropic";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "claude-3-5-sonnet-20241022",
            "claude-3-5-sonnet-latest",
            "claude-3-opus-20240229",
            "claude-3-sonnet-20240229",
            "claude-3-haiku-20240307",
            "claude-3-5-haiku-20241022",
            "claude-2.1",
            "claude-2.0",
            "claude-3.5-sonnet",
            "claude-3-opus",
            "claude-3-haiku",
            "claude-sonnet",
            "claude-opus",
            "claude-haiku"
        };

        public AnthropicProvider(
            HttpClient http,
            ILogger<AnthropicProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["Anthropic:ApiKey"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = BuildPayload(request, modelId, stream: false);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            AddAuthHeaders(httpRequest);

            var response = await _http.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct);
            if (result?.Content == null || result.Content.Count == 0)
            {
                throw new InvalidOperationException("Empty response from Anthropic");
            }

            var textContent = result.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text)
                .FirstOrDefault() ?? "";

            return new ChatCompletionResult(
                result.Id ?? Guid.NewGuid().ToString(),
                textContent,
                result.StopReason ?? "end_turn",
                new Usage(result.Usage?.InputTokens ?? 0, result.Usage?.OutputTokens ?? 0, (result.Usage?.InputTokens ?? 0) + (result.Usage?.OutputTokens ?? 0))
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(
            ChatRequest request,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var modelId = ResolveModel(request.Model);
            var payload = BuildPayload(request, modelId, stream: true);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/messages");
            httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            AddAuthHeaders(httpRequest);

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

                AnthropicStreamEvent? evt;
                try { evt = JsonSerializer.Deserialize<AnthropicStreamEvent>(data); }
                catch { continue; }

                if (evt == null) continue;

                if (evt.Type == "content_block_delta" && evt.Delta?.Type == "text_delta")
                {
                    yield return new ChatCompletionChunk(responseId, evt.Delta.Text ?? "", null);
                }
                else if (evt.Type == "message_delta" && evt.Delta?.StopReason != null)
                {
                    yield return new ChatCompletionChunk(responseId, "", evt.Delta.StopReason);
                }
            }
        }

        private string ResolveModel(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "claude-3.5-sonnet" or "claude-sonnet" => "claude-3-5-sonnet-latest",
                "claude-3-opus" or "claude-opus" => "claude-3-opus-20240229",
                "claude-3-haiku" or "claude-haiku" => "claude-3-haiku-20240307",
                _ => model
            };
        }

        private object BuildPayload(ChatRequest request, string modelId, bool stream)
        {
            var systemMessage = request.Messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            var otherMessages = request.Messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));

            var payload = new Dictionary<string, object>
            {
                ["model"] = modelId,
                ["messages"] = otherMessages.Select(m => new
                {
                    role = m.Role.ToLowerInvariant() == "user" ? "user" : "assistant",
                    content = m.Content
                }).ToList(),
                ["max_tokens"] = request.MaxTokens,
                ["stream"] = stream,
                ["temperature"] = request.Temperature
            };

            if (systemMessage != null)
            {
                payload["system"] = systemMessage.Content;
            }

            return payload;
        }

        private void AddAuthHeaders(HttpRequestMessage request)
        {
            if (!string.IsNullOrEmpty(_apiKey))
            {
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", ApiVersion);
            }
        }

        private class AnthropicResponse
        {
            public string? Id { get; set; }
            public List<AnthropicContent>? Content { get; set; }
            [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
            public AnthropicUsage? Usage { get; set; }
        }

        private class AnthropicContent
        {
            public string? Type { get; set; }
            public string? Text { get; set; }
        }

        private class AnthropicUsage
        {
            [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
            [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
        }

        private class AnthropicStreamEvent
        {
            public string? Type { get; set; }
            public AnthropicDelta? Delta { get; set; }
        }

        private class AnthropicDelta
        {
            public string? Type { get; set; }
            public string? Text { get; set; }
            [JsonPropertyName("stop_reason")] public string? StopReason { get; set; }
        }
    }
}
