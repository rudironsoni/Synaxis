using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Infrastructure.Auth;

public class AntigravityAccount
{
    public string Email { get; set; } = string.Empty;
    public TokenResponse Token { get; set; } = new();
}

/// <summary>
/// Manages authentication for Antigravity, supporting multiple accounts, load balancing, and headless flows.
/// </summary>
public class AntigravityAuthManager : IAntigravityAuthManager
{
    private readonly ILogger<AntigravityAuthManager> _logger;
    private readonly string _authStoragePath;
    private readonly string _projectId;

    // Antigravity Plugin Client ID (from opencode-antigravity-auth reference)
    private const string ClientId = "1071006060591-tmhssin2h21lcre235vtolojh4g403ep.apps.googleusercontent.com";
    private const string ClientSecret = "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";
    private static readonly string[] Scopes = {
        "https://www.googleapis.com/auth/cloud-platform",
        "https://www.googleapis.com/auth/userinfo.email",
        "https://www.googleapis.com/auth/userinfo.profile",
        "https://www.googleapis.com/auth/cclog",
        "https://www.googleapis.com/auth/experimentsandconfigs"
    };

    private List<AntigravityAccount> _accounts = new();
    private readonly SemaphoreSlim _authLock = new(1, 1);
    private int _requestCount = 0;

    public AntigravityAuthManager(string projectId, string authStoragePath, ILogger<AntigravityAuthManager> logger)
    {
        _projectId = projectId;
        _authStoragePath = authStoragePath;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (_accounts.Count == 0)
            {
                await LoadAccountsAsync();
            }

            // Check for Env Var Refresh Token (Transient Account)
            var envRefreshToken = Environment.GetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN");
            if (!string.IsNullOrWhiteSpace(envRefreshToken) && !_accounts.Any(a => a.Token.RefreshToken == envRefreshToken))
            {
                _logger.LogInformation("Injecting transient account from environment variable.");
                _accounts.Add(new AntigravityAccount
                {
                    Email = "env-var-user@system",
                    Token = new TokenResponse { RefreshToken = envRefreshToken, ExpiresInSeconds = 0, IssuedUtc = DateTime.UtcNow.AddHours(-1) }
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
        var flow = CreateFlow();
        return flow.CreateAuthorizationCodeRequest(redirectUrl).Build().ToString();
    }

    public async Task CompleteAuthFlowAsync(string code, string redirectUrl)
    {
        await _authLock.WaitAsync();
        try
        {
            var flow = CreateFlow();
            var token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUrl, CancellationToken.None);

            // Fetch User Info
            var credential = new UserCredential(flow, "user", token);
            var oauthService = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer { HttpClientInitializer = credential });
            var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();

            var email = userInfo.Email;

            // Update or Add
            var existing = _accounts.FirstOrDefault(a => a.Email == email);
            if (existing != null)
            {
                existing.Token = token;
                _logger.LogInformation("Updated token for {Email}", email);
            }
            else
            {
                _accounts.Add(new AntigravityAccount { Email = email, Token = token });
                _logger.LogInformation("Added new account: {Email}", email);
            }

            await SaveAccountsAsync();
        }
        finally
        {
            _authLock.Release();
        }
    }

    private GoogleAuthorizationCodeFlow CreateFlow()
    {
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret
            },
            Scopes = Scopes
        });
    }

    private async Task RefreshAccountTokenAsync(AntigravityAccount account, CancellationToken cancellationToken)
    {
        var flow = CreateFlow();
        var newToken = await flow.RefreshTokenAsync("user", account.Token.RefreshToken, cancellationToken);

        account.Token = newToken;
        // We don't necessarily need to save on every refresh (performance), but it's safer to do so
        // to persist the new access token/expiry.
        await SaveAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        if (!File.Exists(_authStoragePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(_authStoragePath);
            var accounts = JsonSerializer.Deserialize<List<AntigravityAccount>>(json);
            if (accounts != null)
            {
                _accounts = accounts;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load accounts from {Path}", _authStoragePath);
        }
    }

    private async Task SaveAccountsAsync()
    {
        try
        {
            var dir = Path.GetDirectoryName(_authStoragePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_accounts);
            await File.WriteAllTextAsync(_authStoragePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save accounts.");
        }
    }

    // Legacy Interactive Login (kept for CLI convenience)
    private async Task InteractiveLoginAsync(CancellationToken cancellationToken)
    {
        var redirectUri = "http://localhost:51121/oauth-callback";
        var url = StartAuthFlow(redirectUri);

        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("ANTIGRAVITY AUTHENTICATION REQUIRED");
        Console.WriteLine("----------------------------------------------------------------");
        Console.WriteLine("1. Visit this URL in your browser:");
        Console.WriteLine(url);
        Console.WriteLine("");
        Console.WriteLine("2. Log in with your Google Account.");
        Console.WriteLine("3. Copy the authorization code (or the full redirect URL if it fails).");
        Console.WriteLine("----------------------------------------------------------------");
        Console.Write("Paste Code/URL here: ");

        var codeOrUrl = await Task.Run(() => Console.ReadLine(), cancellationToken);

        if (string.IsNullOrWhiteSpace(codeOrUrl))
        {
            throw new InvalidOperationException("No code provided.");
        }

        string code = codeOrUrl;
        if (codeOrUrl.Contains("code="))
        {
            var uri = new Uri(codeOrUrl);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            code = query["code"] ?? code;
        }

        await CompleteAuthFlowAsync(code, redirectUri);
    }
}
