using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ContextSavvy.LlmProviders.Infrastructure.Providers.Tier3
{
    public class CookieManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CookieManager> _logger;
        private readonly Dictionary<string, CachedCookies> _cookieCache = new();
        private readonly TimeSpan _refreshBeforeExpiry = TimeSpan.FromHours(12);

        public CookieManager(IConfiguration configuration, ILogger<CookieManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> GetCookiesAsync(string provider, bool forceRefresh = false)
        {
            if (_cookieCache.TryGetValue(provider, out var cached) && !forceRefresh)
            {
                if (!IsExpired(cached) && !ShouldRefresh(cached))
                {
                    return cached.Values;
                }
            }

            _logger.LogInformation("Refreshing cookies for {Provider}", provider);

            try
            {
                var cookies = await LoadCookiesFromConfigAsync(provider);
                if (cookies.Count > 0)
                {
                    _cookieCache[provider] = new CachedCookies
                    {
                        Values = cookies,
                        Expires = DateTime.UtcNow.AddDays(7)
                    };
                    return cookies;
                }

                _logger.LogWarning("No cookies found for {Provider}", provider);
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load cookies for {Provider}", provider);
                return new Dictionary<string, string>();
            }
        }

        private async Task<Dictionary<string, string>> LoadCookiesFromConfigAsync(string provider)
        {
            var cookieConfig = provider.ToLowerInvariant() switch
            {
                "gemini" => _configuration["Cookies:Gemini"] ?? _configuration["GhostApi:Cookies:Gemini"],
                "huggingchat" => _configuration["Cookies:HuggingChat"] ?? _configuration["GhostApi:Cookies:HuggingChat"],
                "designer" => _configuration["Cookies:Microsoft"] ?? _configuration["GhostApi:Cookies:Microsoft"],
                "nous" => _configuration["Cookies:Nous"] ?? _configuration["GhostApi:Cookies:Nous"],
                "meta" => _configuration["Cookies:Meta"] ?? _configuration["GhostApi:Cookies:Meta"],
                _ => null
            };

            if (string.IsNullOrEmpty(cookieConfig))
            {
                return new Dictionary<string, string>();
            }

            try
            {
                var cookies = JsonSerializer.Deserialize<Dictionary<string, string>>(cookieConfig);
                return cookies ?? new Dictionary<string, string>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse cookies for {Provider}", provider);
                return new Dictionary<string, string>();
            }
        }

        public void SetCookies(string provider, Dictionary<string, string> cookies)
        {
            _cookieCache[provider] = new CachedCookies
            {
                Values = cookies,
                Expires = DateTime.UtcNow.AddDays(7)
            };
        }

        public bool IsAuthenticated(string provider)
        {
            if (_cookieCache.TryGetValue(provider, out var cached))
            {
                return !IsExpired(cached) && cached.Values.Count > 0;
            }
            return false;
        }

        private bool IsExpired(CachedCookies cached)
        {
            return DateTime.UtcNow >= cached.Expires;
        }

        private bool ShouldRefresh(CachedCookies cached)
        {
            return cached.Expires - DateTime.UtcNow < _refreshBeforeExpiry;
        }

        private class CachedCookies
        {
            public Dictionary<string, string> Values { get; set; } = new();
            public DateTime Expires { get; set; }
        }
    }
}
