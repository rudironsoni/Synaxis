using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for billing operations with multi-currency support
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Charge organization for usage (deducts from credit balance)
        /// </summary>
        Task<CreditTransaction> ChargeUsageAsync(Guid organizationId, decimal amountUsd, string description, Guid? referenceId = null);
        
        /// <summary>
        /// Add credits to organization balance
        /// </summary>
        Task<CreditTransaction> TopUpCreditsAsync(Guid organizationId, decimal amountUsd, Guid initiatedBy, string description = null);
        
        /// <summary>
        /// Get current credit balance in USD
        /// </summary>
        Task<decimal> GetCreditBalanceAsync(Guid organizationId);
        
        /// <summary>
        /// Get current credit balance in organization's billing currency
        /// </summary>
        Task<decimal> GetCreditBalanceInBillingCurrencyAsync(Guid organizationId);
        
        /// <summary>
        /// Convert amount from USD to target currency
        /// </summary>
        Task<decimal> ConvertCurrencyAsync(decimal amountUsd, string targetCurrency);
        
        /// <summary>
        /// Get exchange rate from USD to target currency
        /// </summary>
        Task<decimal> GetExchangeRateAsync(string targetCurrency);
        
        /// <summary>
        /// Generate invoice for billing period
        /// </summary>
        Task<Invoice> GenerateInvoiceAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd);
        
        /// <summary>
        /// Get transaction history
        /// </summary>
        Task<List<CreditTransaction>> GetTransactionHistoryAsync(Guid organizationId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);
        
        /// <summary>
        /// Log spending for analytics
        /// </summary>
        Task<SpendLog> LogSpendAsync(Guid organizationId, decimal amountUsd, string model, string provider, int tokens, Guid? teamId = null, Guid? virtualKeyId = null, Guid? requestId = null, string region = null);
        
        /// <summary>
        /// Get spending summary for period
        /// </summary>
        Task<Dictionary<string, decimal>> GetSpendingSummaryAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd);
    }
}
