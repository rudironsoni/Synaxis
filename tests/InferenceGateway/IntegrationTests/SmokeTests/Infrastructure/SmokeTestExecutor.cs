using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    public class SmokeTestExecutor
    {
        private readonly HttpClient _httpClient;
        private readonly SmokeTestOptions _options;
        private readonly ITestOutputHelper _output;

        public SmokeTestExecutor(HttpClient httpClient, SmokeTestOptions options, ITestOutputHelper output)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public async Task<SmokeTestResult> ExecuteAsync(SmokeTestCase testCase)
        {
            var timeoutMs = testCase.TimeoutMs != 0 ? testCase.TimeoutMs : _options.DefaultTimeoutMs;
            var maxRetries = testCase.MaxRetries != 0 ? testCase.MaxRetries : _options.MaxRetries;

            using var cts = new CancellationTokenSource(timeoutMs);

            var retryPolicy = new RetryPolicy(maxRetries, _options.InitialRetryDelayMs, _options.RetryBackoffMultiplier);

            int attempt = 0;
            var sw = new Stopwatch();

            try
            {
                var result = await retryPolicy.ExecuteAsync(async () =>
                {
                    attempt++;
                    sw.Start();
                    var res = await ExecuteSingleAttemptAsync(testCase, cts.Token).ConfigureAwait(false);
                    sw.Stop();
                    // If not success and retryable, throw to trigger retry
                    if (!res.Success && IsRetryableStatus(res.Error))
                    {
                        throw new HttpRequestException(res.Error);
                    }

                    return res;
                }, ex => IsRetryableException(ex)).ConfigureAwait(false);

                // Ensure response time is captured
                return result with { AttemptCount = attempt, ResponseTime = TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds) };
            }
            catch (Exception ex)
            {
                sw.Stop();
                _output.WriteLine($"Smoke test failed for {testCase.Provider}/{testCase.Model}: {ex.Message}");
                return new SmokeTestResult(testCase, false, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds), ex.Message, null, attempt);
            }
        }

        private static bool IsRetryableStatus(string? error)
        {
            if (string.IsNullOrEmpty(error)) return false;
            // check for status codes in message
            if (error.Contains("429") || error.Contains("502") || error.Contains("503")) return true;
            return false;
        }

        private async Task<SmokeTestResult> ExecuteSingleAttemptAsync(SmokeTestCase testCase, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response;
                object payload;

                if (testCase.Endpoint == EndpointType.ChatCompletions)
                {
                    payload = new
                    {
                        model = testCase.Model,
                        messages = new[] { new { role = "user", content = "Reply with exactly one word: OK" } },
                        max_tokens = 5
                    };

                    response = await _httpClient.PostAsJsonAsync("/openai/v1/chat/completions", payload, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    payload = new
                    {
                        model = testCase.Model,
                        prompt = "Reply with exactly one word: OK",
                        max_tokens = 5
                    };

                    response = await _httpClient.PostAsJsonAsync("/openai/v1/completions", payload, cancellationToken).ConfigureAwait(false);
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    // Try to extract a snippet
                    string? snippet = ExtractSnippet(content);
                    return new SmokeTestResult(testCase, true, TimeSpan.Zero, null, snippet, 1);
                }

                // Non-success
                return new SmokeTestResult(testCase, false, TimeSpan.Zero, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {content}", null, 1);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // treat as timeout
                return new SmokeTestResult(testCase, false, TimeSpan.Zero, "Timeout", null, 1);
            }
            catch (Exception ex)
            {
                return new SmokeTestResult(testCase, false, TimeSpan.Zero, ex.Message, null, 1);
            }
        }

        private static string? ExtractSnippet(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("text", out var text)) return text.GetString();
                    if (first.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentEl)) return contentEl.GetString();
                }
            }
            catch
            {
                // ignore parse errors
            }

            return null;
        }

        private static bool IsRetryableException(Exception ex)
        {
            if (ex is HttpRequestException hre)
            {
                // examine message for status codes
                var msg = hre.Message;
                if (msg.Contains("429") || msg.Contains("502") || msg.Contains("503")) return true;
                return true; // treat network errors as retryable
            }

            if (ex is TaskCanceledException) return true; // timeout

            return false;
        }
    }
}
