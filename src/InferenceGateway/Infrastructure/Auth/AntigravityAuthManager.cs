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

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

public class AntigravityAccount
{
    public string Email { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public TokenResponse Token { get; set; } = new();
}

/// <summary>
/// Manages authentication for Antigravity, supporting multiple accounts, load balancing, and headless flows.
/// </summary>
public class AntigravityAuthManager : IAntigravityAuthManager
{
    private readonly ILogger<AntigravityAuthManager> _logger;
    private readonly ITokenStore _tokenStore;
    private readonly string _projectId;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AntigravitySettings _settings;

    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string UserInfoEndpoint = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json";
    private const string DefaultProjectId = "rising-fact-p41fc";
    private static readonly string[] Scopes = {
        "https://www.googleapis.com/auth/cloud-platform",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/cclog",
        "https://www.googleapis.com/auth/experimentsandconfigs"
    };
    private static readonly string[] LoadEndpoints =
    {
        "https://cloudcode-pa.googleapis.com",
        "https://daily-cloudcode-pa.sandbox.googleapis.com",
        "https://autopush-cloudcode-pa.sandbox.googleapis.com"
    };

    private static readonly string[] FallbackEndpoints =
    {
        "https://daily-cloudcode-pa.sandbox.googleapis.com",
        "https://autopush-cloudcode-pa.sandbox.googleapis.com",
        "https://cloudcode-pa.googleapis.com"
    };

    private List<AntigravityAccount> _accounts = new();
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private int _requestCount = 0;

    // New constructor that accepts a token store
    public AntigravityAuthManager(
        string projectId,
        AntigravitySettings settings,
        ILogger<AntigravityAuthManager> logger,
        IHttpClientFactory httpClientFactory,
        ITokenStore tokenStore)
    {
        _projectId = projectId;
        _settings = settings;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));

        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret))
        {
            throw new InvalidOperationException("Antigravity ClientId and ClientSecret must be configured.");
        }
    }

    // Backwards-compatible constructor that accepts a storage path and creates a FileTokenStore
    public AntigravityAuthManager(
        string projectId,
        string authStoragePath,
        AntigravitySettings settings,
        ILogger<AntigravityAuthManager> logger,
        IHttpClientFactory httpClientFactory)
        : this(projectId, settings, logger, httpClientFactory, new FileTokenStore(authStoragePath, Microsoft.Extensions.Logging.Abstractions.NullLogger<FileTokenStore>.Instance))
    {
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_accounts.Count == 0)
            {
                var loaded = await _tokenStore.LoadAsync();
                if (loaded?.Count > 0)
                {
                    _accounts = loaded;
                }
            }

            // Check for Env Var Refresh Token (Transient Account)
            var envRefreshToken = Environment.GetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN");
            if (!string.IsNullOrWhiteSpace(envRefreshToken) && !_accounts.Any(a => a.Token.RefreshToken == envRefreshToken))
            {
                var parsed = ParseRefreshToken(envRefreshToken);
                _logger.LogInformation("Injecting transient account from environment variable.");
                _accounts.Add(new AntigravityAccount
                {
                    Email = "env-var-user@system",
                    ProjectId = parsed.ProjectId,
                    Token = new TokenResponse { RefreshToken = parsed.RefreshToken, ExpiresInSeconds = 0, IssuedUtc = DateTime.UtcNow.AddHours(-1) }
                });
            }

            if (_accounts.Count == 0)
            {
                _logger.LogInformation("No accounts found. Starting interactive login.");
                // For CLI usage, we can still trigger interactive login if no accounts exist
                // But for API usage, this might just fail until an account is added via API
                // We'll try interactive as a fallback for local dev convenience
                try
                {
                    // This will block if run in a non-interactive console without input redirection
                    // In a pure API scenario, this might timeout or throw
                    await InteractiveLoginAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Interactive login failed or was cancelled. Please add an account via API.");
                    throw new InvalidOperationException("No authenticated accounts available. Please add an account via POST /antigravity/auth/start");
                }
            }

            if (_accounts.Count == 0) throw new InvalidOperationException("Authentication failed.");

            // Round-Robin Selection
            var index = Interlocked.Increment(ref _requestCount) % _accounts.Count;
            if (index < 0) index = -index; // Handle overflow
            var account = _accounts[index];

            // Check Expiry & Refresh
            if (account.Token.IsStale)
            {
                _logger.LogInformation("Refreshing token for {Email}...", account.Email);
                await RefreshAccountTokenAsync(account, cancellationToken);
            }

            return account.Token.AccessToken;
        }
        finally
        {
            _authLock.Release();
        }
    }

    public IEnumerable<AccountInfo> ListAccounts()
    {
        // Thread-safe read (copy list reference)
        var accounts = _accounts.ToList();
        return accounts.Select(a => new AccountInfo(a.Email, !a.Token.IsStale));
    }

    public string StartAuthFlow(string redirectUrl)
    {
        var verifier = GenerateCodeVerifier();
        var challenge = GenerateCodeChallenge(verifier);
        var state = EncodeState(new PkceState { Verifier = verifier, ProjectId = _projectId });
        return BuildAuthorizationUrl(redirectUrl, challenge, state);
    }

    public async Task CompleteAuthFlowAsync(string code, string redirectUrl, string? state = null)
    {
        await _authLock.WaitAsync();
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
            var token = await ExchangeCodeForTokenAsync(code, redirectUrl, pkceState.Verifier, CancellationToken.None);
            var email = await FetchUserEmailAsync(token.AccessToken, CancellationToken.None);
            if (string.IsNullOrWhiteSpace(email))
            {
                email = "unknown@user";
            }

            var projectId = pkceState.ProjectId;
            if (string.IsNullOrWhiteSpace(projectId))
            {
                projectId = await FetchProjectIdAsync(token.AccessToken, CancellationToken.None);
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                projectId = DefaultProjectId;
            }

            // Update or Add
            var existing = _accounts.FirstOrDefault(a => a.Email == email);
            if (existing != null)
            {
                existing.Token = token;
                existing.ProjectId = projectId;
                _logger.LogInformation("Updated token for {Email}", email);
            }
            else
            {
                _accounts.Add(new AntigravityAccount { Email = email, ProjectId = projectId, Token = token });
                _logger.LogInformation("Added new account: {Email}", email);
            }

                await _tokenStore.SaveAsync(_accounts);
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task RefreshAccountTokenAsync(AntigravityAccount account, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account.Token.RefreshToken))
        {
            throw new InvalidOperationException("Missing refresh token for account.");
        }

        var newToken = await RefreshTokenAsync(account.Token.RefreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(newToken.RefreshToken))
        {
            newToken.RefreshToken = account.Token.RefreshToken;
        }

        account.Token = newToken;
        // We don't necessarily need to save on every refresh (performance), but it's safer to do so
        // to persist the new access token/expiry.
        await _tokenStore.SaveAsync(_accounts);
    }

    // Legacy Interactive Login (kept for CLI convenience)
    private async Task InteractiveLoginAsync(CancellationToken cancellationToken)
    {
        var redirectUri = "http://localhost:51121/oauth/antigravity/callback";
        var url = StartAuthFlow(redirectUri);

        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("ANTIGRAVITY AUTHENTICATION REQUIRED");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("1. Visit this URL in your browser:");
        Console.WriteLine(url);
        Console.WriteLine("");
        Console.WriteLine("2. Log in with your Google Account.");
        Console.WriteLine("3. Copy the full redirect URL after login (it contains code and state).");
        Console.WriteLine("----------------------------------------------------------------");
        Console.Write("Paste redirect URL here: ");

        var codeOrUrl = await Task.Run(() => Console.ReadLine(), cancellationToken);

        if (string.IsNullOrWhiteSpace(codeOrUrl))
        {
            throw new InvalidOperationException("No code provided.");
        }

        var (code, state) = ParseCallbackUrl(codeOrUrl);
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            throw new InvalidOperationException("The redirect URL must include both code and state parameters.");
        }

        await CompleteAuthFlowAsync(code, redirectUri, state);
    }

    private string BuildAuthorizationUrl(string redirectUrl, string codeChallenge, string state)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUrl,
            ["scope"] = string.Join(" ", Scopes),
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
            ["access_type"] = "offline",
            ["prompt"] = "consent"
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
        var padded = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }

    private async Task<TokenResponse> ExchangeCodeForTokenAsync(
        string code,
        string redirectUrl,
        string verifier,
        CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUrl,
            ["code_verifier"] = verifier
        });

        using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
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
            IssuedUtc = DateTime.UtcNow
        };
    }

    private async Task<TokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token"
        });

        using var response = await httpClient.PostAsync(TokenEndpoint, content, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
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
            IssuedUtc = DateTime.UtcNow
        };
    }

    private async Task<string> FetchUserEmailAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
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
            ["Client-Metadata"] = "{\"ideType\":\"IDE_UNSPECIFIED\",\"platform\":\"PLATFORM_UNSPECIFIED\",\"pluginType\":\"GEMINI\"}"
        };

        var endpoints = LoadEndpoints.Concat(FallbackEndpoints).Distinct().ToArray();
        foreach (var endpoint in endpoints)
        {
            var url = $"{endpoint}/v1internal:loadCodeAssist";
            try
            {
                using var httpClient = CreateHttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                foreach (var header in loadHeaders)
                {
                    if (header.Key == "Authorization")
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

                using var response = await httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                var projectId = ExtractProjectId(payload);
                if (!string.IsNullOrWhiteSpace(projectId))
                {
                    return projectId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve project ID from {Endpoint}", endpoint);
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
        return _httpClientFactory.CreateClient("Antigravity");
    }

    private sealed class PkceState
    {
        [JsonPropertyName("verifier")] public string Verifier { get; set; } = string.Empty;
        [JsonPropertyName("projectId")] public string? ProjectId { get; set; }
    }

    private sealed class TokenPayload
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
    }

    private sealed class UserInfoPayload
    {
        [JsonPropertyName("email")] public string? Email { get; set; }
    }
}
