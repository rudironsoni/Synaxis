using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Exchange rate provider with caching and fallback support.
    /// </summary>
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<ExchangeRateProvider> _logger;

        // Supported currencies
        private static readonly string[] SupportedCurrencies = { "USD", "EUR", "BRL", "GBP" };

        // Cache key for exchange rates
        private const string RatesCacheKey = "exchange_rates:all";

        // Cache duration: 1 hour
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        // Fallback rates (used if external API fails)
        private static readonly Dictionary<string, decimal> FallbackRates = new()
        {
            { "USD", 1.00m },
            { "EUR", 0.92m },
            { "BRL", 5.75m },
            { "GBP", 0.79m }
        };

        public ExchangeRateProvider(ICacheService cacheService, ILogger<ExchangeRateProvider> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<decimal> GetRateAsync(string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
                throw new ArgumentException("Target currency is required", nameof(targetCurrency));

            targetCurrency = targetCurrency.ToUpperInvariant();

            if (!IsSupported(targetCurrency))
                throw new ArgumentException($"Currency '{targetCurrency}' is not supported", nameof(targetCurrency));

            // USD is base currency
            if (targetCurrency == "USD")
                return 1.00m;

            try
            {
                // Try to get from cache
                var cachedRates = await _cacheService.GetAsync<Dictionary<string, decimal>>(RatesCacheKey);

                if (cachedRates != null && cachedRates.ContainsKey(targetCurrency))
                {
                    _logger.LogDebug("Retrieved exchange rate for {Currency} from cache: {Rate}", targetCurrency, cachedRates[targetCurrency]);
                    return cachedRates[targetCurrency];
                }

                // Fetch fresh rates
                var rates = await FetchRatesFromApiAsync();

                // Cache the rates
                await _cacheService.SetAsync(RatesCacheKey, rates, CacheDuration);

                if (rates.ContainsKey(targetCurrency))
                {
                    _logger.LogInformation("Fetched exchange rate for {Currency}: {Rate}", targetCurrency, rates[targetCurrency]);
                    return rates[targetCurrency];
                }

                // Fallback to default rate
                _logger.LogWarning("Could not fetch rate for {Currency}, using fallback", targetCurrency);
                return FallbackRates[targetCurrency];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate for {Currency}, using fallback", targetCurrency);
                return FallbackRates[targetCurrency];
            }
        }

        public async Task<IDictionary<string, decimal>> GetRatesAsync(params string[] targetCurrencies)
        {
            if (targetCurrencies == null || targetCurrencies.Length == 0)
                return new Dictionary<string, decimal>();

            var result = new Dictionary<string, decimal>();

            foreach (var currency in targetCurrencies)
            {
                try
                {
                    var rate = await GetRateAsync(currency);
                    result[currency.ToUpperInvariant()] = rate;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching rate for {Currency}", currency);
                }
            }

            return result;
        }

        public bool IsSupported(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return false;

            return SupportedCurrencies.Contains(currency.ToUpperInvariant());
        }

        public string[] GetSupportedCurrencies()
        {
            return SupportedCurrencies.ToArray();
        }

        /// <summary>
        /// Fetch exchange rates from external API (mocked for now)
        /// In production, this would call a real exchange rate API.
        /// </summary>
        private Task<Dictionary<string, decimal>> FetchRatesFromApiAsync()
        {
            _logger.LogDebug("Fetching exchange rates from API (mock)");

            // Mock implementation - in production, call real API like:
            // - https://api.exchangerate-api.com
            // - https://openexchangerates.org
            // - https://currencyapi.com

            // Simulate API delay
            Task.Delay(50).Wait();

            // Return mock rates with slight randomization to simulate real data
            var random = new Random();
            var variance = 0.02m; // ±2% variance

            var rates = new Dictionary<string, decimal>
            {
                { "USD", 1.00m },
                { "EUR", 0.92m * (1 + (decimal)(random.NextDouble() * 2 - 1) * variance) },
                { "BRL", 5.75m * (1 + (decimal)(random.NextDouble() * 2 - 1) * variance) },
                { "GBP", 0.79m * (1 + (decimal)(random.NextDouble() * 2 - 1) * variance) }
            };

            return Task.FromResult(rates);
        }
    }
}
