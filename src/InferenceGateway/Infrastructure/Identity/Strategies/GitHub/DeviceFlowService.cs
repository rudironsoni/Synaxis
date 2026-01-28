using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub
{
    public class DeviceFlowService
    {
        private readonly HttpClient _http;
        private readonly ILogger<DeviceFlowService> _logger;

        public DeviceFlowService(HttpClient http, ILogger<DeviceFlowService> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Start polling GitHub token endpoint for the device flow. Calls onSuccess when token received.
        /// </summary>
        public Task StartPollingAsync(string deviceCode, int intervalSeconds, Func<TokenResponse, Task> onSuccess, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(deviceCode)) throw new ArgumentNullException(nameof(deviceCode));
            if (onSuccess == null) throw new ArgumentNullException(nameof(onSuccess));

            // Run background polling
            return Task.Run(async () =>
            {
                var interval = Math.Max(1, intervalSeconds);
                var url = "https://github.com/login/oauth/access_token";

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Post, url);
                        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        var body = new System.Collections.Generic.Dictionary<string, string>
                        {
                            ["client_id"] = GitHubAuthStrategy.ClientId,
                            ["device_code"] = deviceCode,
                            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                        };
                        req.Content = new FormUrlEncodedContent(body);

                        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
                        var txt = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                        if (string.IsNullOrEmpty(txt))
                        {
                            _logger.LogWarning("Empty response when polling GitHub device token endpoint");
                        }

                        try
                        {
                            using var doc = JsonDocument.Parse(txt);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("access_token", out var at))
                            {
                                var tr = new TokenResponse
                                {
                                    AccessToken = at.GetString() ?? string.Empty
                                };
                                if (root.TryGetProperty("refresh_token", out var rt)) tr.RefreshToken = rt.GetString();
                                if (root.TryGetProperty("expires_in", out var ex)) tr.ExpiresInSeconds = ex.GetInt32();

                                await onSuccess(tr).ConfigureAwait(false);
                                break;
                            }

                            if (root.TryGetProperty("error", out var err))
                            {
                                var errVal = err.GetString();
                                switch (errVal)
                                {
                                    case "authorization_pending":
                                        // continue waiting
                                        break;
                                    case "slow_down":
                                        interval += 5;
                                        break;
                                    case "expired_token":
                                    case "access_denied":
                                    default:
                                        _logger.LogWarning("Device flow polling stopped due to error: {Error}", errVal);
                                        return;
                                }
                            }
                        }
                        catch (JsonException jex)
                        {
                            _logger.LogError(jex, "Failed to parse GitHub device token response: {Text}", txt);
                        }
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error while polling GitHub device token endpoint");
                        // break on unexpected errors
                        break;
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }, ct);
        }
    }
}
