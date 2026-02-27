// <copyright file="DeviceFlowService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    /// <summary>
    /// Service for handling GitHub device flow authentication polling.
    /// </summary>
    public class DeviceFlowService
    {
        private readonly HttpClient _http;
        private readonly ILogger<DeviceFlowService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceFlowService"/> class.
        /// </summary>
        /// <param name="http">The HTTP client to use.</param>
        /// <param name="logger">The logger instance.</param>
        public DeviceFlowService(HttpClient http, ILogger<DeviceFlowService> logger)
        {
            this._http = http ?? throw new ArgumentNullException(nameof(http));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start polling GitHub token endpoint for the device flow. Calls onSuccess when token received.
        /// </summary>
        /// <param name="deviceCode">The device code received from GitHub device flow.</param>
        /// <param name="intervalSeconds">The polling interval in seconds.</param>
        /// <param name="onSuccess">Callback to invoke when token is successfully received.</param>
        /// <param name="ct">Cancellation token to stop polling.</param>
        /// <param name="testCallbackTcs">Optional TaskCompletionSource for testing. Signals when callback completes.</param>
        /// <returns>A task that represents the asynchronous polling operation.</returns>
        public virtual Task StartPollingAsync(string deviceCode, int intervalSeconds, Func<TokenResponse, Task> onSuccess, CancellationToken ct, TaskCompletionSource? testCallbackTcs = null)
        {
            if (string.IsNullOrEmpty(deviceCode))
            {
                throw new ArgumentNullException(nameof(deviceCode));
            }

            if (onSuccess == null)
            {
                throw new ArgumentNullException(nameof(onSuccess));
            }

            // Run background polling
            return Task.Run(
                async () => await this.PollDeviceCodeAsync(deviceCode, intervalSeconds, onSuccess, ct, testCallbackTcs).ConfigureAwait(false), ct);
        }

        private async Task PollDeviceCodeAsync(
            string deviceCode,
            int intervalSeconds,
            Func<TokenResponse, Task> onSuccess,
            CancellationToken ct,
            TaskCompletionSource? testCallbackTcs = null)
        {
            var interval = Math.Max(1, intervalSeconds);
            var url = "https://github.com/login/oauth/access_token";

            while (!ct.IsCancellationRequested)
            {
                var (shouldContinue, newInterval, _) = await this.PollOnceAsync(url, deviceCode, onSuccess, interval, ct, testCallbackTcs).ConfigureAwait(false);
                interval = newInterval;
                if (!shouldContinue)
                {
                    break;
                }

                if (!await DelayPollingAsync(interval, ct).ConfigureAwait(false))
                {
                    break;
                }
            }
        }

        private async Task<(bool ShouldContinue, int Interval, bool CallbackCompleted)> PollOnceAsync(
            string url,
            string deviceCode,
            Func<TokenResponse, Task> onSuccess,
            int interval,
            CancellationToken ct,
            TaskCompletionSource? testCallbackTcs = null)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var body = new System.Collections.Generic.Dictionary<string, string>
                {
                    ["client_id"] = GitHubAuthStrategy.ClientId,
                    ["device_code"] = deviceCode,
                    ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                };
                req.Content = new FormUrlEncodedContent(body);

                using var resp = await this._http.SendAsync(req, ct).ConfigureAwait(false);
                var txt = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (string.IsNullOrEmpty(txt))
                {
                    this._logger.LogWarning("Empty response when polling GitHub device token endpoint");
                }

                var (shouldContinue, newInterval, callbackCompleted) = await this.ProcessPollingResponseAsync(txt, onSuccess, interval, testCallbackTcs).ConfigureAwait(false);
                return (shouldContinue, newInterval, callbackCompleted);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return (false, interval, false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error while polling GitHub device token endpoint");
                return (false, interval, false);
            }
        }

        private async Task<(bool ShouldContinue, int Interval, bool CallbackCompleted)> ProcessPollingResponseAsync(
            string responseText,
            Func<TokenResponse, Task> onSuccess,
            int interval,
            TaskCompletionSource? testCallbackTcs = null)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                var root = doc.RootElement;
                if (root.TryGetProperty("access_token", out var at))
                {
                    var tokenResponse = this.CreateTokenResponse(root, at);
                    await onSuccess(tokenResponse).ConfigureAwait(false);
                    testCallbackTcs?.TrySetResult(); // Signal test that callback completed
                    return (false, interval, true); // Stop polling, callback completed
                }

                if (root.TryGetProperty("error", out var err))
                {
                    var (shouldContinue, newInterval) = this.HandlePollingError(err.GetString(), interval);
                    return (shouldContinue, newInterval, false);
                }
            }
            catch (JsonException jex)
            {
                this._logger.LogError(jex, "Failed to parse GitHub device token response: {Text}", responseText);
            }

            return (true, interval, false); // Continue polling
        }

        private TokenResponse CreateTokenResponse(JsonElement root, JsonElement accessToken)
        {
            var tr = new TokenResponse
            {
                AccessToken = accessToken.GetString() ?? string.Empty,
            };
            if (root.TryGetProperty("refresh_token", out var rt))
            {
                tr.RefreshToken = rt.GetString();
            }

            if (root.TryGetProperty("expires_in", out var ex))
            {
                tr.ExpiresInSeconds = ex.GetInt32();
            }

            return tr;
        }

        private (bool ShouldContinue, int Interval) HandlePollingError(string? errorValue, int currentInterval)
        {
            switch (errorValue)
            {
                case "authorization_pending":
                    return (true, currentInterval); // Continue waiting
                case "slow_down":
                    return (true, currentInterval + 5);
                case "expired_token":
                case "access_denied":
                    // Token expired or access denied - stop polling
                    this._logger.LogWarning("Device flow polling stopped due to error: {Error}", errorValue);
                    return (false, currentInterval);
                default:
                    this._logger.LogWarning("Device flow polling stopped due to error: {Error}", errorValue);
                    return (false, currentInterval); // Stop polling
            }
        }

        private static async Task<bool> DelayPollingAsync(int intervalSeconds, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
