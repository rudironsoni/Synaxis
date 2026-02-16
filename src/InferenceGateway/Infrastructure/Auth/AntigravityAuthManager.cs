// <copyright file="AntigravityAuthManager.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Apis.Auth.OAuth2.Responses;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Manages Antigravity authentication with multi-account support and automatic token refresh.
    /// </summary>
    public class AntigravityAuthManager : IAntigravityAuthManager
    {
#pragma warning disable S1075 // URIs should not be hardcoded - OAuth and API endpoints
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";
        private const string DefaultProjectId = "rising-fact-p41fc";
#pragma warning restore S1075 // URIs should not be hardcoded

        private static readonly string[] Scopes =
        {
            "https://www.googleapis.com/auth/cloud-platform",
            "https://www.googleapis.com/auth/userinfo.email",
            "https://www.googleapis.com/auth/userinfo.profile",
            "https://www.googleapis.com/auth/cclog",
            "https://www.googleapis.com/auth/experimentsandconfigs",
        };

        private static readonly string[] LoadEndpoints =
        {
            "https://cloudcode-pa.googleapis.com",
            "https://daily-cloudcode-pa.sandbox.googleapis.com",
            "https://autopush-cloudcode-pa.sandbox.googleapis.com",
        };

        private static readonly string[] FallbackEndpoints =
        {
            "https://daily-cloudcode-pa.sandbox.googleapis.com",
            "https://autopush-cloudcode-pa.sandbox.googleapis.com",
            "https://cloudcode-pa.googleapis.com",
        };

        private readonly ILogger<AntigravityAuthManager> _logger;
        private readonly ITokenStore _tokenStore;
        private readonly string _projectId;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AntigravitySettings _settings;
        private readonly SemaphoreSlim _authLock = new(1, 1);

        private IList<AntigravityAccount> _accounts = new List<AntigravityAccount>();
        private int _requestCount = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityAuthManager"/> class.
        /// </summary>
        /// <param name="projectId">The default project identifier.</param>
        /// <param name="settings">The Antigravity settings.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="tokenStore">The token store.</param>
        public AntigravityAuthManager(
            string projectId,
            AntigravitySettings settings,
            ILogger<AntigravityAuthManager> logger,
            IHttpClientFactory httpClientFactory,
            ITokenStore tokenStore)
        {
            this._projectId = projectId;
            this._settings = settings;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;
            this._tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));

            if (string.IsNullOrWhiteSpace(this._settings.ClientId) || string.IsNullOrWhiteSpace(this._settings.ClientSecret))
            {
                throw new InvalidOperationException("Antigravity ClientId and ClientSecret must be configured.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AntigravityAuthManager"/> class.
        /// Backwards-compatible constructor that creates a FileTokenStore.
        /// </summary>
        /// <param name="projectId">The default project identifier.</param>
        /// <param name="authStoragePath">The file path for storing tokens.</param>
        /// <param name="settings">The Antigravity settings.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        public AntigravityAuthManager(
            string projectId,
            string authStoragePath,
            AntigravitySettings settings,
            ILogger<AntigravityAuthManager> logger,
            IHttpClientFactory httpClientFactory)
            : this(projectId, settings, logger, httpClientFactory, new FileTokenStore(authStoragePath, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileTokenStore>.Instance))
        {
        }

        /// <inheritdoc/>
        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            await this._authLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await this.EnsureAccountsLoadedAsync().ConfigureAwait(false);
                await this.InjectTransientAccountIfNeededAsync().ConfigureAwait(false);
                await this.EnsureAccountsExistAsync(cancellationToken).ConfigureAwait(false);

                var account = this.SelectAccountRoundRobin();
                await this.RefreshTokenIfStaleAsync(account, cancellationToken).ConfigureAwait(false);

                return account.Token.AccessToken;
            }
            finally
            {
                this._authLock.Release();
            }
        }

        private async Task EnsureAccountsLoadedAsync()
        {
            if (this._accounts.Count == 0)
            {
                var loaded = await this._tokenStore.LoadAsync().ConfigureAwait(false);
                if (loaded?.Count > 0)
                {
                    this._accounts = loaded;
                }
            }
        }

        private async Task InjectTransientAccountIfNeededAsync()
        {
            var envRefreshToken = Environment.GetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN");
            if (!string.IsNullOrWhiteSpace(envRefreshToken) && !this._accounts.Any(a => string.Equals(a.Token.RefreshToken, envRefreshToken, StringComparison.Ordinal)))
            {
                var parsed = ParseRefreshToken(envRefreshToken);
                this._logger.LogInformation("Injecting transient account from environment variable.");
                this._accounts.Add(new AntigravityAccount
                {
                    Email = "env-var-user@system",
                    ProjectId = parsed.ProjectId,
                    Token = new TokenResponse { RefreshToken = parsed.RefreshToken, ExpiresInSeconds = 0, IssuedUtc = DateTime.UtcNow.AddHours(-1) },
                });
            }
        }

        private async Task EnsureAccountsExistAsync(CancellationToken cancellationToken)
        {
            if (this._accounts.Count == 0)
            {
                this._logger.LogInformation("No accounts found. Starting interactive login.");

                try
                {
                    await this.InteractiveLoginAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Interactive login failed or was cancelled. Please add an account via API.");
                    throw new InvalidOperationException("No authenticated accounts available. Please add an account via POST /antigravity/auth/start");
                }
            }

            if (this._accounts.Count == 0)
            {
                throw new InvalidOperationException("Authentication failed.");
            }
        }

        private AntigravityAccount SelectAccountRoundRobin()
        {
            var index = Interlocked.Increment(ref this._requestCount) % this._accounts.Count;
            if (index < 0)
            {
                index = -index; // Handle overflow
            }

            return this._accounts[index];
        }

        private async Task RefreshTokenIfStaleAsync(AntigravityAccount account, CancellationToken cancellationToken)
        {
            if (account.Token.IsStale)
            {
                this._logger.LogInformation("Refreshing token for {Email}...", account.Email);
                await this.RefreshAccountTokenAsync(account, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<AccountInfo> ListAccounts()
        {
            // Thread-safe read (copy list reference)
            var accounts = this._accounts.ToList();
            return accounts.Select(a => new AccountInfo(a.Email, !a.Token.IsStale));
        }

        /// <inheritdoc/>
        public Task<string> StartAuthFlowAsync(string redirectUrl)
        {
            var verifier = GenerateCodeVerifier();
            var challenge = GenerateCodeChallenge(verifier);
            var state = EncodeState(new PkceState { Verifier = verifier, ProjectId = this._projectId });
            return Task.FromResult(this.BuildAuthorizationUrl(redirectUrl, challenge, state));
        }

        /// <inheritdoc/>
        public async Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null)
        {
            await this._authLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    throw new InvalidOperationException("Authorization code is required.");
                }

                if (string.IsNullOrWhiteSpace(state))
                {
                    throw new InvalidOperationException("Missing OAuth state. Start with /antigravity/auth/start and use the provided URL.");
                }

                var pkceState = DecodeState(state);
                var token = await this.ExchangeCodeForTokenAsync(code, redirectUrl, pkceState.Verifier, CancellationToken.None).ConfigureAwait(false);
                var email = await this.FetchUserEmailAsync(token.AccessToken, CancellationToken.None).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = "unknown@user";
                }

                var projectId = pkceState.ProjectId;
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    projectId = await this.FetchProjectIdAsync(token.AccessToken, CancellationToken.None).ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(projectId))
                {
                    projectId = DefaultProjectId;
                }

                // Update or Add
                var existing = this._accounts.FirstOrDefault(a => string.Equals(a.Email, email, StringComparison.Ordinal));
                if (existing != null)
                {
                    existing.Token = token;
                    existing.ProjectId = projectId;
                    this._logger.LogInformation("Updated token for {Email}", email);
                }
                else
                {
                    this._accounts.Add(new AntigravityAccount { Email = email, ProjectId = projectId, Token = token });
                    this._logger.LogInformation("Added new account: {Email}", email);
                }

                await this._tokenStore.SaveAsync(this._accounts).ConfigureAwait(false);
            }
            finally
            {
                this._authLock.Release();
            }
        }

        private async Task RefreshAccountTokenAsync(AntigravityAccount account, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(account.Token.RefreshToken))
            {
                throw new InvalidOperationException("Missing refresh token for account.");
            }

            var newToken = await this.RefreshTokenAsync(account.Token.RefreshToken, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(newToken.RefreshToken))
            {
                newToken.RefreshToken = account.Token.RefreshToken;
            }

            account.Token = newToken;

            // We don't necessarily need to save on every refresh (performance), but it's safer to do so
            // to persist the new access token/expiry.
            await this._tokenStore.SaveAsync(this._accounts).ConfigureAwait(false);
        }

        // Legacy Interactive Login (kept for CLI convenience)
        private async Task InteractiveLoginAsync(CancellationToken cancellationToken)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - OAuth redirect URI
            var redirectUri = "http://localhost:51121/oauth/antigravity/callback";
#pragma warning restore S1075 // URIs should not be hardcoded
            var url = await this.StartAuthFlowAsync(redirectUri).ConfigureAwait(false);

            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("ANTIGRAVITY AUTHENTICATION REQUIRED");
            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine("1. Visit this URL in your browser:");
            Console.WriteLine(url);
            Console.WriteLine(string.Empty);
            Console.WriteLine("2. Log in with your Google Account.");
            Console.WriteLine("3. Copy the full redirect URL after login (it contains code and state).");
            Console.WriteLine("----------------------------------------------------------------");
            Console.Write("Paste redirect URL here: ");

            var codeOrUrl = await Task.Run(() => Console.ReadLine(), cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(codeOrUrl))
            {
                throw new InvalidOperationException("No code provided.");
            }

            var (code, state) = ParseCallbackUrl(codeOrUrl);
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                throw new InvalidOperationException("The redirect URL must include both code and state parameters.");
            }

            await this.CompleteAuthFlowAsync(code, redirectUri, state).ConfigureAwait(false);
        }

        private string BuildAuthorizationUrl(string redirectUrl, string codeChallenge, string state)
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = this._settings.ClientId,
                ["response_type"] = "code",
                ["redirect_uri"] = redirectUrl,
                ["scope"] = string.Join(" ", Scopes),
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["state"] = state,
                ["access_type"] = "offline",
                ["prompt"] = "consent",
            };

            return $"{AuthorizationEndpoint}?{BuildQueryString(parameters)}";
        }

        private static string BuildQueryString(IDictionary<string, string> parameters)
        {
            return string.Join("&", parameters.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            var bytes = Encoding.ASCII.GetBytes(verifier);
            var hash = SHA256.HashData(bytes);
            return Base64UrlEncode(hash);
        }

        private static string EncodeState(PkceState state)
        {
            var json = JsonSerializer.Serialize(state);
            return Base64UrlEncode(Encoding.UTF8.GetBytes(json));
        }

        private static PkceState DecodeState(string state)
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(state));
            var parsed = JsonSerializer.Deserialize<PkceState>(json);
            if (parsed == null || string.IsNullOrWhiteSpace(parsed.Verifier))
            {
                throw new InvalidOperationException("Missing PKCE verifier in state.");
            }

            parsed.ProjectId ??= string.Empty;
            return parsed;
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var normalized = input.Replace('-', '+').Replace('_', '/');
            var padded = normalized.PadRight(normalized.Length + ((4 - (normalized.Length % 4)) % 4), '=');
            return Convert.FromBase64String(padded);
        }

        private async Task<TokenResponse> ExchangeCodeForTokenAsync(
            string code,
            string redirectUrl,
            string verifier,
            CancellationToken cancellationToken)
        {
            using var httpClient = this.CreateHttpClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = this._settings.ClientId,
                ["client_secret"] = this._settings.ClientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUrl,
                ["code_verifier"] = verifier,
            });

            using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token exchange failed: {payload}");
            }

            var tokenPayload = JsonSerializer.Deserialize<TokenPayload>(payload);
            if (tokenPayload == null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
            {
                throw new InvalidOperationException("Token exchange failed: missing access token.");
            }

            if (string.IsNullOrWhiteSpace(tokenPayload.RefreshToken))
            {
                throw new InvalidOperationException("Token exchange failed: missing refresh token.");
            }

            return new TokenResponse
            {
                AccessToken = tokenPayload.AccessToken,
                RefreshToken = tokenPayload.RefreshToken,
                ExpiresInSeconds = tokenPayload.ExpiresIn,
                IssuedUtc = DateTime.UtcNow,
            };
        }

        private async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            using var httpClient = this.CreateHttpClient();
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = this._settings.ClientId,
                ["client_secret"] = this._settings.ClientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token",
            });

            using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {payload}");
            }

            var tokenPayload = JsonSerializer.Deserialize<TokenPayload>(payload);
            if (tokenPayload == null || string.IsNullOrWhiteSpace(tokenPayload.AccessToken))
            {
                throw new InvalidOperationException("Token refresh failed: missing access token.");
            }

            return new TokenResponse
            {
                AccessToken = tokenPayload.AccessToken,
                RefreshToken = tokenPayload.RefreshToken ?? refreshToken,
                ExpiresInSeconds = tokenPayload.ExpiresIn,
                IssuedUtc = DateTime.UtcNow,
            };
        }

        private async Task<string> FetchUserEmailAsync(string accessToken, CancellationToken cancellationToken)
        {
            using var httpClient = this.CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var userInfo = JsonSerializer.Deserialize<UserInfoPayload>(payload);
            return userInfo?.Email ?? string.Empty;
        }

        private async Task<string> FetchProjectIdAsync(string accessToken, CancellationToken cancellationToken)
        {
            var loadHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}",
                ["Content-Type"] = "application/json",
                ["User-Agent"] = "google-api-nodejs-client/9.15.1",
                ["X-Goog-Api-Client"] = "google-cloud-sdk vscode_cloudshelleditor/0.1",
                ["Client-Metadata"] = "{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"}",
            };

            var endpoints = LoadEndpoints.Concat(FallbackEndpoints).Distinct().ToArray();
            foreach (var endpoint in endpoints)
            {
                var url = $"{endpoint}/v1internal:loadCodeAssist";
                try
                {
                    using var httpClient = this.CreateHttpClient();
                    using var request = new HttpRequestMessage(HttpMethod.Post, url);
                    foreach (var header in loadHeaders)
                    {
                        if (string.Equals(header.Key, "Authorization", StringComparison.Ordinal))
                        {
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                        }
                        else
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }

                    request.Content = new StringContent(
                        "{\"metadata\":{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"}}",
                        Encoding.UTF8,
                        "application/json");

                    using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    var projectId = ExtractProjectId(payload);
                    if (!string.IsNullOrWhiteSpace(projectId))
                    {
                        return projectId;
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to resolve project ID from {Endpoint}", endpoint);
                }
            }

            return string.Empty;
        }

        private static string ExtractProjectId(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("cloudaicompanionProject", out var projectElement))
                {
                    if (projectElement.ValueKind == JsonValueKind.String)
                    {
                        return projectElement.GetString() ?? string.Empty;
                    }

                    if (projectElement.ValueKind == JsonValueKind.Object && projectElement.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetString() ?? string.Empty;
                    }
                }
            }
            catch (JsonException)
            {
                // Silently ignore JSON parsing errors for project ID extraction
            }

            return string.Empty;
        }

        private static (string RefreshToken, string ProjectId) ParseRefreshToken(string refreshToken)
        {
            var separatorIndex = refreshToken.IndexOf('|');
            if (separatorIndex <= 0)
            {
                return (refreshToken, string.Empty);
            }

            var token = refreshToken.Substring(0, separatorIndex);
            var projectId = refreshToken.Substring(separatorIndex + 1);
            return (token, projectId);
        }

        private static (string Code, string State) ParseCallbackUrl(string callbackUrl)
        {
            var uri = new Uri(callbackUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return (query["code"] ?? string.Empty, query["state"] ?? string.Empty);
        }

        private HttpClient CreateHttpClient()
        {
            return this._httpClientFactory.CreateClient("Antigravity");
        }

        private sealed class PkceState
        {
            /// <summary>
            /// Gets or sets the Verifier.
            /// </summary>
            [JsonPropertyName("verifier")]
            public string Verifier { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the ProjectId.
            /// </summary>
            [JsonPropertyName("projectId")]
            public string? ProjectId { get; set; }
        }

        private sealed class TokenPayload
        {
            /// <summary>
            /// Gets or sets the AccessToken.
            /// </summary>
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the ExpiresIn.
            /// </summary>
            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            /// <summary>
            /// Gets or sets the RefreshToken.
            /// </summary>
            [JsonPropertyName("refresh_token")]
            public string? RefreshToken { get; set; }
        }

        private sealed class UserInfoPayload
        {
            /// <summary>
            /// Gets or sets the Email.
            /// </summary>
            [JsonPropertyName("email")]
            public string? Email { get; set; }
        }
    }

    /// <summary>
    /// Represents an Antigravity account with token and project information.
    /// </summary>
    public class AntigravityAccount
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Token.
        /// </summary>
        public TokenResponse Token { get; set; } = new();
    }
}
