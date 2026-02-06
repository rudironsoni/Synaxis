// <copyright file="IExchangeRateProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Service for fetching and caching exchange rates.
    /// </summary>
    public interface IExchangeRateProvider
    {
        /// <summary>
        /// Get exchange rate from USD to target currency.
        /// </summary>
        /// <param name="targetCurrency">The target currency code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exchange rate.</returns>
        Task<decimal> GetRateAsync(string targetCurrency);

        /// <summary>
        /// Get multiple exchange rates from USD.
        /// </summary>
        /// <param name="targetCurrencies">The target currency codes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the dictionary of exchange rates.</returns>
        Task<IDictionary<string, decimal>> GetRatesAsync(params string[] targetCurrencies);

        /// <summary>
        /// Check if currency is supported.
        /// </summary>
        /// <param name="currency">The currency code to check.</param>
        /// <returns>True if the currency is supported; otherwise, false.</returns>
        bool IsSupported(string currency);

        /// <summary>
        /// Get list of supported currencies.
        /// </summary>
        /// <returns>An array of supported currency codes.</returns>
        string[] GetSupportedCurrencies();
    }
}
