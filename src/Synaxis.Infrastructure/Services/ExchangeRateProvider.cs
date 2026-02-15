// <copyright file="ExchangeRateProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Exchange rate provider with caching and fallback support.
    /// </summary>
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        // Cache key for exchange rates
        private const string RatesCacheKey = "exchange_rates:all";

        private readonly ICacheService _cacheService;
        private readonly ILogger<ExchangeRateProvider> _logger;

        // Supported currencies
        private static readonly string[] SupportedCurrencies = { "USD", "EUR", "BRL", "GBP" };

        // Cache duration: 1 hour
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

        // Fallback rates (used if external API fails)
        private static readonly Dictionary<string, decimal> FallbackRates = new(StringComparer.Ordinal)
        {
            { "USD", 1.00m },
            { "EUR", 0.92m },
            { "BRL", 5.75m },
            { "GBP", 0.79m },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExchangeRateProvider"/> class.
        /// </summary>
        /// <param name="cacheService"></param>
        /// <param name="logger"></param>
        public ExchangeRateProvider(ICacheService cacheService, ILogger<ExchangeRateProvider> logger)
        {
            this._cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<decimal> GetRateAsync(string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
            {
                throw new ArgumentException("Target currency is required", nameof(targetCurrency));
            }

            targetCurrency = targetCurrency.ToUpperInvariant();

            if (!this.IsSupported(targetCurrency))
            {
                throw new ArgumentException($"Currency '{targetCurrency}' is not supported", nameof(targetCurrency));
            }

            // USD is base currency
            if (string.Equals(targetCurrency, "USD", StringComparison.Ordinal))
            {
                return 1.00m;
            }

            try
            {
                // Try to get from cache
                var cachedRates = await this._cacheService.GetAsync<Dictionary<string, decimal>>(RatesCacheKey).ConfigureAwait(false);

                if (cachedRates != null && cachedRates.ContainsKey(targetCurrency))
                {
                    this._logger.LogDebug("Retrieved exchange rate for {Currency} from cache: {Rate}", targetCurrency, cachedRates[targetCurrency]);
                    return cachedRates[targetCurrency];
                }

                // Fetch fresh rates
                var rates = await this.FetchRatesFromApiAsync().ConfigureAwait(false);

                // Cache the rates
                await this._cacheService.SetAsync(RatesCacheKey, rates, CacheDuration).ConfigureAwait(false);

                if (rates.ContainsKey(targetCurrency))
                {
                    this._logger.LogInformation("Fetched exchange rate for {Currency}: {Rate}", targetCurrency, rates[targetCurrency]);
                    return rates[targetCurrency];
                }

                // Fallback to default rate
                this._logger.LogWarning("Could not fetch rate for {Currency}, using fallback", targetCurrency);
                return FallbackRates[targetCurrency];
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error fetching exchange rate for {Currency}, using fallback", targetCurrency);
                return FallbackRates[targetCurrency];
            }
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, decimal>> GetRatesAsync(params string[] targetCurrencies)
        {
            if (targetCurrencies == null || targetCurrencies.Length == 0)
            {
                return new Dictionary<string, decimal>(StringComparer.Ordinal);
            }

            var result = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var currency in targetCurrencies)
            {
                try
                {
                    var rate = await this.GetRateAsync(currency).ConfigureAwait(false);
                    result[currency.ToUpperInvariant()] = rate;
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error fetching rate for {Currency}", currency);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public bool IsSupported(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                return false;
            }

            return SupportedCurrencies.Contains(currency.ToUpperInvariant());
        }

        /// <inheritdoc/>
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
            this._logger.LogDebug("Fetching exchange rates from API (mock)");

            // Mock implementation - in production, call real API like:
            // - https://api.exchangerate-api.com
            // - https://openexchangerates.org
            // - https://currencyapi.com

            // Simulate API delay
            Task.Delay(50).Wait();

            // Return mock rates with slight randomization to simulate real data
            var random = new Random();
            var variance = 0.02m; // ±2% variance

            var rates = new Dictionary<string, decimal>(
StringComparer.Ordinal)
            {
                { "USD", 1.00m },
                { "EUR", 0.92m * (1 + ((decimal)((random.NextDouble() * 2) - 1) * variance)) },
                { "BRL", 5.75m * (1 + ((decimal)((random.NextDouble() * 2) - 1) * variance)) },
                { "GBP", 0.79m * (1 + ((decimal)((random.NextDouble() * 2) - 1) * variance)) },
            };

            return Task.FromResult(rates);
        }
    }
}
