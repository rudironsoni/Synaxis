using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Billing service with multi-currency support and credit management
    /// All internal tracking is in USD, conversion happens at billing time.
    /// </summary>
    public class BillingService : IBillingService
    {
        private readonly SynaxisDbContext _context;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly ILogger<BillingService> _logger;

        public BillingService(
            SynaxisDbContext context,
            IExchangeRateProvider exchangeRateProvider,
            ILogger<BillingService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _exchangeRateProvider = exchangeRateProvider ?? throw new ArgumentNullException(nameof(exchangeRateProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CreditTransaction> ChargeUsageAsync(Guid organizationId, decimal amountUsd, string description, Guid? referenceId = null)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (amountUsd <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amountUsd));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Description is required", nameof(description));

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            // Check if sufficient balance
            if (organization.CreditBalance < amountUsd)
            {
                _logger.LogWarning("Insufficient credits for organization {OrgId}. Balance: {Balance}, Required: {Required}",
                    organizationId, organization.CreditBalance, amountUsd);
                throw new InvalidOperationException($"Insufficient credit balance. Current: ${organization.CreditBalance:F2}, Required: ${amountUsd:F2}");
            }

            var balanceBefore = organization.CreditBalance;
            organization.CreditBalance -= amountUsd;
            organization.UpdatedAt = DateTime.UtcNow;

            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TransactionType = "charge",
                AmountUsd = -amountUsd, // Negative for charges
                BalanceBeforeUsd = balanceBefore,
                BalanceAfterUsd = organization.CreditBalance,
                Description = description,
                ReferenceId = referenceId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Charged ${Amount} to organization {OrgId}. New balance: ${Balance}",
                amountUsd, organizationId, organization.CreditBalance);

            return transaction;
        }

        public async Task<CreditTransaction> TopUpCreditsAsync(Guid organizationId, decimal amountUsd, Guid initiatedBy, string? description = null)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (amountUsd <= 0)
                throw new ArgumentException("Amount must be positive", nameof(amountUsd));

            if (initiatedBy == Guid.Empty)
                throw new ArgumentException("Initiated by user ID is required", nameof(initiatedBy));

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            var balanceBefore = organization.CreditBalance;
            organization.CreditBalance += amountUsd;
            organization.UpdatedAt = DateTime.UtcNow;

            var transaction = new CreditTransaction
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TransactionType = "topup",
                AmountUsd = amountUsd, // Positive for top-ups
                BalanceBeforeUsd = balanceBefore,
                BalanceAfterUsd = organization.CreditBalance,
                Description = description ?? $"Credit top-up: ${amountUsd:F2}",
                InitiatedBy = initiatedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added ${Amount} credits to organization {OrgId}. New balance: ${Balance}",
                amountUsd, organizationId, organization.CreditBalance);

            return transaction;
        }

        public async Task<decimal> GetCreditBalanceAsync(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            return organization.CreditBalance;
        }

        public async Task<decimal> GetCreditBalanceInBillingCurrencyAsync(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            var balanceUsd = organization.CreditBalance;

            if (organization.BillingCurrency == "USD")
                return balanceUsd;

            return await ConvertCurrencyAsync(balanceUsd, organization.BillingCurrency);
        }

        public async Task<decimal> ConvertCurrencyAsync(decimal amountUsd, string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
                throw new ArgumentException("Target currency is required", nameof(targetCurrency));

            targetCurrency = targetCurrency.ToUpperInvariant();

            if (targetCurrency == "USD")
                return amountUsd;

            var rate = await _exchangeRateProvider.GetRateAsync(targetCurrency);
            return Math.Round(amountUsd * rate, 2);
        }

        public async Task<decimal> GetExchangeRateAsync(string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
                throw new ArgumentException("Target currency is required", nameof(targetCurrency));

            return await _exchangeRateProvider.GetRateAsync(targetCurrency);
        }

        public async Task<Invoice> GenerateInvoiceAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (periodEnd <= periodStart)
                throw new ArgumentException("Period end must be after period start");

            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            // Get all spend logs for the period
            var spendLogs = await _context.Set<SpendLog>()
                .Where(s => s.OrganizationId == organizationId
                    && s.CreatedAt >= periodStart
                    && s.CreatedAt < periodEnd)
                .ToListAsync();

            var totalAmountUsd = spendLogs.Sum(s => s.AmountUsd);

            // Get exchange rate for billing currency
            var exchangeRate = await GetExchangeRateAsync(organization.BillingCurrency);
            var totalAmountBillingCurrency = await ConvertCurrencyAsync(totalAmountUsd, organization.BillingCurrency);

            // Generate line items by model
            var lineItems = spendLogs
                .GroupBy(s => s.Model)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.AmountUsd));

            // Generate invoice number
            var invoiceNumber = $"INV-{periodStart:yyyy-MM}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                InvoiceNumber = invoiceNumber,
                Status = "issued",
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                TotalAmountUsd = totalAmountUsd,
                TotalAmountBillingCurrency = totalAmountBillingCurrency,
                BillingCurrency = organization.BillingCurrency,
                ExchangeRate = exchangeRate,
                LineItems = lineItems,
                DueDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated invoice {InvoiceNumber} for organization {OrgId}. Total: ${Total} USD (${TotalBilling} {Currency})",
                invoiceNumber, organizationId, totalAmountUsd, totalAmountBillingCurrency, organization.BillingCurrency);

            return invoice;
        }

        public async Task<IList<CreditTransaction>> GetTransactionHistoryAsync(Guid organizationId, DateTime? startDate = null, DateTime? endDate = null, int limit = 100)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (limit <= 0 || limit > 1000)
                throw new ArgumentException("Limit must be between 1 and 1000", nameof(limit));

            var query = _context.Set<CreditTransaction>()
                .Where(t => t.OrganizationId == organizationId);

            if (startDate.HasValue)
                query = query.Where(t => t.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.CreatedAt < endDate.Value);

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<SpendLog> LogSpendAsync(Guid organizationId, decimal amountUsd, string model, string provider, int tokens, Guid? teamId = null, Guid? virtualKeyId = null, Guid? requestId = null, string? region = null)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (amountUsd < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amountUsd));

            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required", nameof(model));

            var spendLog = new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                TeamId = teamId,
                VirtualKeyId = virtualKeyId,
                RequestId = requestId,
                AmountUsd = amountUsd,
                Model = model,
                Provider = provider ?? string.Empty,
                Tokens = tokens,
                Region = region ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Add(spendLog);
            await _context.SaveChangesAsync();

            _logger.LogTrace("Logged spend of ${Amount} for organization {OrgId}, model {Model}",
                amountUsd, organizationId, model);

            return spendLog;
        }

        public async Task<IDictionary<string, decimal>> GetSpendingSummaryAsync(Guid organizationId, DateTime periodStart, DateTime periodEnd)
        {
            if (organizationId == Guid.Empty)
                throw new ArgumentException("Organization ID is required", nameof(organizationId));

            if (periodEnd <= periodStart)
                throw new ArgumentException("Period end must be after period start");

            var spendLogs = await _context.Set<SpendLog>()
                .Where(s => s.OrganizationId == organizationId
                    && s.CreatedAt >= periodStart
                    && s.CreatedAt <= periodEnd)
                .ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                { "total", spendLogs.Sum(s => s.AmountUsd) }
            };

            // Add breakdown by model
            var byModel = spendLogs
                .GroupBy(s => s.Model)
                .ToDictionary(g => $"model_{g.Key}", g => g.Sum(s => s.AmountUsd));

            foreach (var kvp in byModel)
            {
                summary[kvp.Key] = kvp.Value;
            }

            // Add breakdown by team if available
            var byTeam = spendLogs
                .Where(s => s.TeamId.HasValue)
                .GroupBy(s => s.TeamId!.Value)
                .ToDictionary(g => $"team_{g.Key}", g => g.Sum(s => s.AmountUsd));

            foreach (var kvp in byTeam)
            {
                summary[kvp.Key] = kvp.Value;
            }

            return summary;
        }
    }
}
