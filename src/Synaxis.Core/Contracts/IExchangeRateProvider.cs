using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for fetching and caching exchange rates
    /// </summary>
    public interface IExchangeRateProvider
    {
        /// <summary>
        /// Get exchange rate from USD to target currency
        /// </summary>
        Task<decimal> GetRateAsync(string targetCurrency);
        
        /// <summary>
        /// Get multiple exchange rates from USD
        /// </summary>
        Task<Dictionary<string, decimal>> GetRatesAsync(params string[] targetCurrencies);
        
        /// <summary>
        /// Check if currency is supported
        /// </summary>
        bool IsSupported(string currency);
        
        /// <summary>
        /// Get list of supported currencies
        /// </summary>
        string[] GetSupportedCurrencies();
    }
}
