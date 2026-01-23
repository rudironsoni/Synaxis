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
    public class ReplicateProvider : ILlmProvider
    {
        private readonly HttpClient _http;
        private readonly ILogger<ReplicateProvider> _logger;
        private readonly string? _apiKey;

        private const string BaseUrl = "https://api.replicate.com/v1";

        public string Id => "replicate";
        public string Name => "Replicate";
        public ProviderTier Tier => ProviderTier.Tier2_Standard;

        public HashSet<string> SupportedModels { get; } = new(StringComparer.OrdinalIgnoreCase)
        {
            "meta/llama-3.3-70b-instruct", "meta/llama-3.1-405b-instruct", "meta/llama-3.1-70b-instruct", "meta/llama-3.1-8b-instruct",
            "mistralai/mixtral-8x7b-instruct-v0.1", "mistralai/mistral-7b-instruct-v0.3",
            "stability-ai/sdxl", "black-forest-labs/flux-1.1-pro", "black-forest-labs/flux-schnell",
            "llama-3.3-70b", "llama-3.1-405b", "llama-3.1-70b", "sdxl", "flux"
        };

        public ReplicateProvider(
            HttpClient http,
            ILogger<ReplicateProvider> logger,
            IConfiguration config)
        {
            _http = http;
            _logger = logger;
            _apiKey = config["Replicate:ApiKey"] ?? Environment.GetEnvironmentVariable("REPLICATE_API_KEY");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
            }
        }

        public bool SupportsModel(string modelId) => SupportedModels.Contains(modelId);

        public async Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default)
        {
            var modelVersion = GetModelVersion(request.Model);
            var predictionRequest = new
            {
                version = modelVersion,
                input = BuildInput(request)
            };

            var response = await _http.PostAsJsonAsync($"{BaseUrl}/predictions", predictionRequest, ct);
            response.EnsureSuccessStatusCode();

            var prediction = await response.Content.ReadFromJsonAsync<ReplicatePrediction>(cancellationToken: ct);
            if (prediction == null) throw new InvalidOperationException("Failed to create prediction");

            // Poll for completion
            var result = await PollPredictionAsync(prediction.Id, ct);
            var content = ExtractOutput(result.Output);

            var promptTokens = EstimateTokens(request.Messages.Sum(m => m.Content.Length));
            var completionTokens = EstimateTokens(content.Length);

            return new ChatCompletionResult(
                result.Id,
                content,
                result.Status == "succeeded" ? "stop" : result.Status,
                new Usage(promptTokens, completionTokens, promptTokens + completionTokens)
            );
        }

        public async IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var result = await ChatAsync(request, ct);
            yield return new ChatCompletionChunk(result.Id, result.Content, result.FinishReason);
        }

        private async Task<ReplicatePrediction> PollPredictionAsync(string predictionId, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var response = await _http.GetFromJsonAsync<ReplicatePrediction>($"{BaseUrl}/predictions/{predictionId}", ct);
                if (response == null) throw new InvalidOperationException("Failed to get prediction status");

                if (response.Status == "succeeded" || response.Status == "failed" || response.Status == "canceled")
                    return response;

                await Task.Delay(1000, ct);
            }
            throw new OperationCanceledException();
        }

        private Dictionary<string, object> BuildInput(ChatRequest request)
        {
            var input = new Dictionary<string, object>();
            var systemMessage = request.Messages.FirstOrDefault(m => m.Role.Equals("system", StringComparison.OrdinalIgnoreCase));
            var userMessages = request.Messages.Where(m => !m.Role.Equals("system", StringComparison.OrdinalIgnoreCase)).ToList();

            if (systemMessage != null) input["system_prompt"] = systemMessage.Content;
            input["prompt"] = string.Join("\n\n", userMessages.Select(m => $"{m.Role}: {m.Content}"));
            input["max_tokens"] = request.MaxTokens;
            input["temperature"] = request.Temperature;

            return input;
        }

        private static string ExtractOutput(object? output)
        {
            if (output == null) return "";
            if (output is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String) return element.GetString() ?? "";
                if (element.ValueKind == JsonValueKind.Array) return string.Join("", element.EnumerateArray().Select(e => e.GetString() ?? ""));
            }
            return output.ToString() ?? "";
        }

        private static string GetModelVersion(string model)
        {
            return model.ToLowerInvariant() switch
            {
                "llama-3.3-70b" or "meta/llama-3.3-70b" => "meta/meta-llama-3.3-70b-instruct",
                "llama-3.1-405b" or "meta/llama-3.1-405b" => "meta/meta-llama-3.1-405b-instruct",
                "llama-3.1-70b" or "meta/llama-3.1-70b" => "meta/meta-llama-3.1-70b-instruct",
                "llama-3.1-8b" or "meta/llama-3.1-8b" => "meta/meta-llama-3.1-8b-instruct",
                "mixtral-8x7b" or "mistralai/mixtral-8x7b" => "mistralai/mixtral-8x7b-instruct-v0.1",
                _ => model
            };
        }

        private static int EstimateTokens(int charCount) => Math.Max(1, charCount / 4);

        private class ReplicatePrediction
        {
            public string Id { get; set; } = "";
            public string Status { get; set; } = "";
            public object? Output { get; set; }
        }
    }
}
