// <copyright file="IBillingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for billing operations with multi-currency support.
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Charge organization for usage (deducts from credit balance).
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="amountUsd">The amount to charge in USD.</param>
        /// <param name="description">The description of the charge.</param>
        /// <param name="referenceId">The optional reference identifier for the charge.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the credit transaction.</returns>
        Task<CreditTransaction> ChargeUsageAsync(Guid organizationId, decimal amountUsd, string description, Guid? referenceId = null);

        /// <summary>
        /// Add credits to organization balance.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="amountUsd">The amount of credits to add in USD.</param>
        /// <param name="initiatedBy">The user who initiated the top-up.</param>
        /// <param name="description">The optional description of the top-up.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the credit transaction.</returns>
        Task<CreditTransaction> TopUpCreditsAsync(Guid organizationId, decimal amountUsd, Guid initiatedBy, string description = null);

        /// <summary>
        /// Get current credit balance in USD.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the credit balance in USD.</returns>
        Task<decimal> GetCreditBalanceAsync(Guid organizationId);

        /// <summary>
        /// Get current credit balance in organization's billing currency.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the credit balance in billing currency.</returns>
        Task<decimal> GetCreditBalanceInBillingCurrencyAsync(Guid organizationId);

        /// <summary>
        /// Convert amount from USD to target currency.
        /// </summary>
        /// <param name="amountUsd">The amount in USD to convert.</param>
        /// <param name="targetCurrency">The target currency code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the converted amount.</returns>
        Task<decimal> ConvertCurrencyAsync(decimal amountUsd, string targetCurrency);

        /// <summary>
        /// Get exchange rate from USD to target currency.
        /// </summary>
        /// <param name="targetCurrency">The target currency code.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exchange rate.</returns>
        Task<decimal> GetExchangeRateAsync(string targetCurrency);

        /// <summary>
        /// Generate invoice for billing period.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="periodStart">The start date of the billing period.</param>
        /// <param name="periodEnd">The end date of the billing period.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated invoice.</returns>
        Task<Invoice> GenerateInvoiceAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd);

        /// <summary>
        /// Get transaction history.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="startDate">The optional start date for filtering transactions.</param>
        /// <param name="endDate">The optional end date for filtering transactions.</param>
        /// <param name="limit">The maximum number of transactions to return.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of credit transactions.</returns>
        Task<IList<CreditTransaction>> GetTransactionHistoryAsync(Guid organizationId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100);

        /// <summary>
        /// Log spending for analytics.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="amountUsd">The amount spent in USD.</param>
        /// <param name="model">The model used.</param>
        /// <param name="provider">The provider used.</param>
        /// <param name="tokens">The number of tokens consumed.</param>
        /// <param name="teamId">The optional team identifier.</param>
        /// <param name="virtualKeyId">The optional virtual key identifier.</param>
        /// <param name="requestId">The optional request identifier.</param>
        /// <param name="region">The optional region.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the spend log entry.</returns>
        Task<SpendLog> LogSpendAsync(Guid organizationId, decimal amountUsd, string model, string provider, int tokens, Guid? teamId = null, Guid? virtualKeyId = null, Guid? requestId = null, string region = null);

        /// <summary>
        /// Get spending summary for period.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="periodStart">The start date of the period.</param>
        /// <param name="periodEnd">The end date of the period.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the spending summary grouped by category.</returns>
        Task<IDictionary<string, decimal>> GetSpendingSummaryAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd);
    }
}
