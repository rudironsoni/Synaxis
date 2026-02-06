using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Unit
{
    public class BillingServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly IBillingService _service;
        private readonly Guid _testOrgId;
        
        public BillingServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
            
            // Setup default exchange rates
            _exchangeRateProvider.GetRateAsync("USD").Returns(1.0m);
            _exchangeRateProvider.GetRateAsync("EUR").Returns(0.85m);
            _exchangeRateProvider.GetRateAsync("BRL").Returns(5.0m);
            
            _service = new BillingService(_context, _exchangeRateProvider, NullLogger<BillingService>.Instance);
            
            _testOrgId = SeedOrganization();
        }
        
        private Guid SeedOrganization()
        {
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Org",
                Slug = "test-org",
                PrimaryRegion = "us-east-1",
                BillingCurrency = "USD",
                CreditBalance = 100.00m,
                Tier = "free",
                IsActive = true
            };
            
            _context.Organizations.Add(org);
            _context.SaveChanges();
            return org.Id;
        }
        
        [Fact]
        public async Task ChargeUsageAsync_ValidRequest_DeductsFromBalance()
        {
            // Arrange
            var amount = 10.50m;
            var description = "API usage charge";
            var initiatorId = Guid.NewGuid();
            
            // Act
            var result = await _service.ChargeUsageAsync(_testOrgId, amount, description, initiatorId);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(-amount, result.AmountUsd);
            Assert.Equal(100.00m, result.BalanceBeforeUsd);
            Assert.Equal(89.50m, result.BalanceAfterUsd);
            Assert.Equal(description, result.Description);
            
            var org = await _context.Organizations.FindAsync(_testOrgId);
            Assert.Equal(89.50m, org.CreditBalance);
        }
        
        [Fact]
        public async Task ChargeUsageAsync_InsufficientBalance_ThrowsInvalidOperationException()
        {
            // Arrange
            var amount = 150.00m; // More than balance
            var description = "API usage charge";
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.ChargeUsageAsync(_testOrgId, amount, description));
            Assert.Contains("Insufficient credit balance", exception.Message);
        }
        
        [Fact]
        public async Task ChargeUsageAsync_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var amount = -10.00m;
            var description = "API usage charge";
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChargeUsageAsync(_testOrgId, amount, description));
        }
        
        [Fact]
        public async Task ChargeUsageAsync_EmptyDescription_ThrowsArgumentException()
        {
            // Arrange
            var amount = 10.00m;
            var description = "";
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.ChargeUsageAsync(_testOrgId, amount, description));
        }
        
        [Fact]
        public async Task TopUpCreditsAsync_ValidRequest_AddsToBalance()
        {
            // Arrange
            var amount = 50.00m;
            var initiatorId = Guid.NewGuid();
            var description = "Credit top-up";
            
            // Act
            var result = await _service.TopUpCreditsAsync(_testOrgId, amount, initiatorId, description);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(amount, result.AmountUsd);
            Assert.Equal(100.00m, result.BalanceBeforeUsd);
            Assert.Equal(150.00m, result.BalanceAfterUsd);
            Assert.Equal(description, result.Description);
            
            var org = await _context.Organizations.FindAsync(_testOrgId);
            Assert.Equal(150.00m, org.CreditBalance);
        }
        
        [Fact]
        public async Task TopUpCreditsAsync_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var amount = -50.00m;
            var initiatorId = Guid.NewGuid();
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.TopUpCreditsAsync(_testOrgId, amount, initiatorId));
        }
        
        [Fact]
        public async Task GetCreditBalanceAsync_ValidOrganization_ReturnsBalance()
        {
            // Act
            var result = await _service.GetCreditBalanceAsync(_testOrgId);
            
            // Assert
            Assert.Equal(100.00m, result);
        }
        
        [Fact]
        public async Task GetCreditBalanceAsync_NonExistingOrganization_ThrowsInvalidOperationException()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetCreditBalanceAsync(nonExistingId));
            Assert.Contains("not found", exception.Message);
        }
        
        [Fact]
        public async Task GetCreditBalanceInBillingCurrencyAsync_UsdCurrency_ReturnsSameAmount()
        {
            // Act
            var result = await _service.GetCreditBalanceInBillingCurrencyAsync(_testOrgId);
            
            // Assert
            Assert.Equal(100.00m, result);
        }
        
        [Fact]
        public async Task GetCreditBalanceInBillingCurrencyAsync_EurCurrency_ReturnsConvertedAmount()
        {
            // Arrange
            var org = await _context.Organizations.FindAsync(_testOrgId);
            org.BillingCurrency = "EUR";
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.GetCreditBalanceInBillingCurrencyAsync(_testOrgId);
            
            // Assert
            Assert.Equal(85.00m, result); // 100 * 0.85
        }
        
        [Fact]
        public async Task ConvertCurrencyAsync_UsdToEur_ReturnsConvertedAmount()
        {
            // Arrange
            var amountUsd = 100.00m;
            
            // Act
            var result = await _service.ConvertCurrencyAsync(amountUsd, "EUR");
            
            // Assert
            Assert.Equal(85.00m, result);
        }
        
        [Fact]
        public async Task ConvertCurrencyAsync_UsdToUsd_ReturnsSameAmount()
        {
            // Arrange
            var amountUsd = 100.00m;
            
            // Act
            var result = await _service.ConvertCurrencyAsync(amountUsd, "USD");
            
            // Assert
            Assert.Equal(100.00m, result);
        }
        
        [Fact]
        public async Task GetExchangeRateAsync_ValidCurrency_ReturnsRate()
        {
            // Act
            var result = await _service.GetExchangeRateAsync("EUR");
            
            // Assert
            Assert.Equal(0.85m, result);
        }
        
        [Fact]
        public async Task GenerateInvoiceAsync_ValidPeriod_CreatesInvoice()
        {
            // Arrange
            var periodStart = DateTime.UtcNow.AddMonths(-1);
            var periodEnd = DateTime.UtcNow;
            
            // Add spend logs
            _context.Set<SpendLog>().Add(new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                AmountUsd = 10.00m,
                Model = "gpt-4",
                Provider = "openai",
                Tokens = 1000,
                CreatedAt = periodStart.AddDays(5)
            });
            
            _context.Set<SpendLog>().Add(new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                AmountUsd = 5.00m,
                Model = "gpt-3.5-turbo",
                Provider = "openai",
                Tokens = 500,
                CreatedAt = periodStart.AddDays(10)
            });
            
            await _context.SaveChangesAsync();
            
            // Act
            var result = await _service.GenerateInvoiceAsync(_testOrgId, periodStart, periodEnd);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testOrgId, result.OrganizationId);
            Assert.Equal(15.00m, result.TotalAmountUsd);
            Assert.Equal("issued", result.Status);
            Assert.NotNull(result.InvoiceNumber);
            Assert.Contains("INV-", result.InvoiceNumber);
        }
        
        [Fact]
        public async Task GenerateInvoiceAsync_InvalidPeriod_ThrowsArgumentException()
        {
            // Arrange
            var periodStart = DateTime.UtcNow;
            var periodEnd = DateTime.UtcNow.AddMonths(-1); // End before start
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GenerateInvoiceAsync(_testOrgId, periodStart, periodEnd));
        }
        
        [Fact]
        public async Task GetTransactionHistoryAsync_ValidOrganization_ReturnsTransactions()
        {
            // Arrange
            await _service.TopUpCreditsAsync(_testOrgId, 50.00m, Guid.NewGuid());
            await _service.ChargeUsageAsync(_testOrgId, 10.00m, "Test charge");
            
            // Act
            var result = await _service.GetTransactionHistoryAsync(_testOrgId);
            
            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.TransactionType == "topup");
            Assert.Contains(result, t => t.TransactionType == "charge");
        }
        
        [Fact]
        public async Task GetTransactionHistoryAsync_WithDateFilters_ReturnsFilteredTransactions()
        {
            // Arrange
            await _service.TopUpCreditsAsync(_testOrgId, 50.00m, Guid.NewGuid());
            await Task.Delay(100); // Small delay to ensure different timestamps
            var cutoffDate = DateTime.UtcNow;
            await Task.Delay(100);
            await _service.ChargeUsageAsync(_testOrgId, 10.00m, "Test charge");
            
            // Act
            var result = await _service.GetTransactionHistoryAsync(_testOrgId, startDate: cutoffDate);
            
            // Assert
            Assert.Single(result);
            Assert.Equal("charge", result.First().TransactionType);
        }
        
        [Fact]
        public async Task LogSpendAsync_ValidRequest_CreatesSpendLog()
        {
            // Arrange
            var amount = 5.50m;
            var model = "gpt-4";
            var provider = "openai";
            var tokens = 1000;
            
            // Act
            var result = await _service.LogSpendAsync(_testOrgId, amount, model, provider, tokens);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(_testOrgId, result.OrganizationId);
            Assert.Equal(amount, result.AmountUsd);
            Assert.Equal(model, result.Model);
            Assert.Equal(provider, result.Provider);
            Assert.Equal(tokens, result.Tokens);
        }
        
        [Fact]
        public async Task LogSpendAsync_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var amount = -5.00m;
            var model = "gpt-4";
            var provider = "openai";
            var tokens = 1000;
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.LogSpendAsync(_testOrgId, amount, model, provider, tokens));
        }
        
        [Fact]
        public async Task GetSpendingSummaryAsync_ValidPeriod_ReturnsSummary()
        {
            // Arrange
            var periodStart = DateTime.UtcNow.AddDays(-7);
            
            await _service.LogSpendAsync(_testOrgId, 10.00m, "gpt-4", "openai", 1000);
            await _service.LogSpendAsync(_testOrgId, 5.00m, "gpt-3.5-turbo", "openai", 500);
            
            var periodEnd = DateTime.UtcNow;
            
            // Act
            var result = await _service.GetSpendingSummaryAsync(_testOrgId, periodStart, periodEnd);
            
            // Assert
            Assert.NotEmpty(result);
            Assert.Contains("total", result.Keys);
            Assert.Equal(15.00m, result["total"]);
            Assert.Contains("model_gpt-4", result.Keys);
            Assert.Contains("model_gpt-3.5-turbo", result.Keys);
        }
        
        [Fact]
        public async Task GetSpendingSummaryAsync_InvalidPeriod_ThrowsArgumentException()
        {
            // Arrange
            var periodStart = DateTime.UtcNow;
            var periodEnd = DateTime.UtcNow.AddDays(-7); // End before start
            
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetSpendingSummaryAsync(_testOrgId, periodStart, periodEnd));
        }
        
        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
