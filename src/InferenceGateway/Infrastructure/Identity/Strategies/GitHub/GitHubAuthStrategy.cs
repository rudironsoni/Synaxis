// <copyright file="GitHubAuthStrategy.cs" company="PlaceholderCompany">
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

    public class GitHubAuthStrategy : IAuthStrategy
    {
        public event EventHandler<AccountAuthenticatedEventArgs>? AccountAuthenticated;
        public const string ClientId = "178c6fc778ccc68e1d6a";

        private readonly HttpClient _http;
        private readonly DeviceFlowService _deviceFlowService;
        // IdentityManager removed to avoid circular dependency. Will raise event when account authenticated.
        private readonly ILogger<GitHubAuthStrategy> _logger;

        public GitHubAuthStrategy(HttpClient http, DeviceFlowService deviceFlowService, ILogger<GitHubAuthStrategy> logger)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _deviceFlowService = deviceFlowService ?? throw new ArgumentNullException(nameof(deviceFlowService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthResult> InitiateFlowAsync(CancellationToken ct)
        {
            var url = "https://github.com/login/device/code";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var body = new System.Collections.Generic.Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["scope"] = "repo read:org copilot"
            };
            req.Content = new FormUrlEncodedContent(body);

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var txt = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            try
            {
                using var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;
                var userCode = root.GetProperty("user_code").GetString();
                var verificationUri = root.GetProperty("verification_uri").GetString();
                var deviceCode = root.GetProperty("device_code").GetString();
                var interval = 5;
                if (root.TryGetProperty("interval", out var iv)) interval = iv.GetInt32();

                // start background polling
                _ = _deviceFlowService.StartPollingAsync(deviceCode ?? string.Empty, interval, OnTokenReceived, ct);

                return new AuthResult
                {
                    Status = "Pending",
                    UserCode = userCode,
                    VerificationUri = verificationUri
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initiate GitHub device flow");
                return new AuthResult { Status = "Error", Message = ex.Message };
            }
        }

        public Task<AuthResult> CompleteFlowAsync(string code, string state, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task<TokenResponse> RefreshTokenAsync(IdentityAccount account, CancellationToken ct)
        {
            var url = "https://github.com/login/oauth/access_token";
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var body = new System.Collections.Generic.Dictionary<string, string>
            {
                ["client_id"] = ClientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = account.RefreshToken ?? string.Empty
            };
            req.Content = new FormUrlEncodedContent(body);

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var txt = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            try
            {
                using var doc = JsonDocument.Parse(txt);
                var root = doc.RootElement;
                var tr = new TokenResponse();
                if (root.TryGetProperty("access_token", out var at)) tr.AccessToken = at.GetString() ?? string.Empty;
                if (root.TryGetProperty("refresh_token", out var rt)) tr.RefreshToken = rt.GetString();
                if (root.TryGetProperty("expires_in", out var ex)) tr.ExpiresInSeconds = ex.GetInt32();
                return tr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh GitHub token: {Text}", txt);
                throw;
            }
        }

        private async Task OnTokenReceived(TokenResponse token)
        {
            try
            {
                // Create a basic account; we don't currently have user id/email from device flow response
                var acc = new IdentityAccount
                {
                    Provider = "github",
                    Id = "github-user",
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken
                };

                if (token.ExpiresInSeconds.HasValue)
                    acc.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds.Value);

                // Notify subscribers that an account was authenticated
                try
                {
                    AccountAuthenticated?.Invoke(this, new AccountAuthenticatedEventArgs(acc));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking AccountAuthenticated event");
                }

                // Write to gh config
                await GhConfigWriter.WriteTokenAsync(token.AccessToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling received GitHub token");
            }
        }
    }
}
