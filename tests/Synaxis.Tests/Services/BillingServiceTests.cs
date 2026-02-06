using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Services
{
    public class BillingServiceTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly Mock<IExchangeRateProvider> _mockExchangeRateProvider;
        private readonly Mock<ILogger<BillingService>> _mockLogger;
        private readonly BillingService _billingService;
        
        public BillingServiceTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
                
            _context = new SynaxisDbContext(options);
            _mockExchangeRateProvider = new Mock<IExchangeRateProvider>();
            _mockLogger = new Mock<ILogger<BillingService>>();
            
            _billingService = new BillingService(_context, _mockExchangeRateProvider.Object, _mockLogger.Object);
            
            // Setup default exchange rates
            _mockExchangeRateProvider.Setup(x => x.GetRateAsync("USD")).ReturnsAsync(1.00m);
            _mockExchangeRateProvider.Setup(x => x.GetRateAsync("EUR")).ReturnsAsync(0.92m);
            _mockExchangeRateProvider.Setup(x => x.GetRateAsync("BRL")).ReturnsAsync(5.75m);
            _mockExchangeRateProvider.Setup(x => x.GetRateAsync("GBP")).ReturnsAsync(0.79m);
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        
        [Fact]
        public async Task ChargeUsageAsync_ValidCharge_DeductsFromBalance()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 100.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Act
            var transaction = await _billingService.ChargeUsageAsync(org.Id, 25.50m, "Test charge");
            
            // Assert
            Assert.NotNull(transaction);
            Assert.Equal("charge", transaction.TransactionType);
            Assert.Equal(-25.50m, transaction.AmountUsd);
            Assert.Equal(100.00m, transaction.BalanceBeforeUsd);
            Assert.Equal(74.50m, transaction.BalanceAfterUsd);
            
            var updatedOrg = await _context.Organizations.FindAsync(org.Id);
            Assert.Equal(74.50m, updatedOrg.CreditBalance);
        }
        
        [Fact]
        public async Task ChargeUsageAsync_InsufficientBalance_ThrowsException()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 10.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _billingService.ChargeUsageAsync(org.Id, 25.00m, "Test charge")
            );
        }
        
        [Fact]
        public async Task TopUpCreditsAsync_ValidTopUp_AddsToBalance()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 50.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            var userId = Guid.NewGuid();
            
            // Act
            var transaction = await _billingService.TopUpCreditsAsync(org.Id, 100.00m, userId, "Manual top-up");
            
            // Assert
            Assert.NotNull(transaction);
            Assert.Equal("topup", transaction.TransactionType);
            Assert.Equal(100.00m, transaction.AmountUsd);
            Assert.Equal(50.00m, transaction.BalanceBeforeUsd);
            Assert.Equal(150.00m, transaction.BalanceAfterUsd);
            Assert.Equal(userId, transaction.InitiatedBy);
            
            var updatedOrg = await _context.Organizations.FindAsync(org.Id);
            Assert.Equal(150.00m, updatedOrg.CreditBalance);
        }
        
        [Fact]
        public async Task ConvertCurrencyAsync_UsdToEur_ReturnsCorrectAmount()
        {
            // Arrange
            var amountUsd = 100.00m;
            
            // Act
            var amountEur = await _billingService.ConvertCurrencyAsync(amountUsd, "EUR");
            
            // Assert
            Assert.Equal(92.00m, amountEur); // 100 * 0.92
        }
        
        [Fact]
        public async Task ConvertCurrencyAsync_UsdToBrl_ReturnsCorrectAmount()
        {
            // Arrange
            var amountUsd = 50.00m;
            
            // Act
            var amountBrl = await _billingService.ConvertCurrencyAsync(amountUsd, "BRL");
            
            // Assert
            Assert.Equal(287.50m, amountBrl); // 50 * 5.75
        }
        
        [Fact]
        public async Task ConvertCurrencyAsync_UsdToUsd_ReturnsOriginalAmount()
        {
            // Arrange
            var amountUsd = 75.00m;
            
            // Act
            var result = await _billingService.ConvertCurrencyAsync(amountUsd, "USD");
            
            // Assert
            Assert.Equal(75.00m, result);
        }
        
        [Fact]
        public async Task GetCreditBalanceInBillingCurrencyAsync_EurCurrency_ReturnsConvertedBalance()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "eu-west-1",
                CreditBalance = 100.00m, // USD
                BillingCurrency = "EUR"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Act
            var balanceEur = await _billingService.GetCreditBalanceInBillingCurrencyAsync(org.Id);
            
            // Assert
            Assert.Equal(92.00m, balanceEur); // 100 * 0.92
        }
        
        [Fact]
        public async Task GenerateInvoiceAsync_ValidPeriod_CreatesInvoice()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 100.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            
            var periodStart = new DateTime(2026, 2, 1);
            var periodEnd = new DateTime(2026, 3, 1);
            
            // Add some spend logs
            var spendLog1 = new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                AmountUsd = 10.50m,
                Model = "gpt-4",
                Provider = "openai",
                Tokens = 1000,
                CreatedAt = periodStart.AddDays(5)
            };
            var spendLog2 = new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                AmountUsd = 15.75m,
                Model = "gpt-4",
                Provider = "openai",
                Tokens = 1500,
                CreatedAt = periodStart.AddDays(10)
            };
            var spendLog3 = new SpendLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = org.Id,
                AmountUsd = 8.25m,
                Model = "claude-3",
                Provider = "anthropic",
                Tokens = 800,
                CreatedAt = periodStart.AddDays(15)
            };
            _context.Add(spendLog1);
            _context.Add(spendLog2);
            _context.Add(spendLog3);
            await _context.SaveChangesAsync();
            
            // Act
            var invoice = await _billingService.GenerateInvoiceAsync(org.Id, periodStart, periodEnd);
            
            // Assert
            Assert.NotNull(invoice);
            Assert.Equal(org.Id, invoice.OrganizationId);
            Assert.Equal("issued", invoice.Status);
            Assert.Equal(34.50m, invoice.TotalAmountUsd); // 10.50 + 15.75 + 8.25
            Assert.Equal("USD", invoice.BillingCurrency);
            Assert.Contains("gpt-4", invoice.LineItems.Keys);
            Assert.Equal(26.25m, invoice.LineItems["gpt-4"]); // 10.50 + 15.75
            Assert.Contains("claude-3", invoice.LineItems.Keys);
            Assert.Equal(8.25m, invoice.LineItems["claude-3"]);
        }
        
        [Fact]
        public async Task LogSpendAsync_ValidSpend_CreatesSpendLog()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 100.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Act
            var spendLog = await _billingService.LogSpendAsync(
                org.Id,
                12.50m,
                "gpt-4-turbo",
                "openai",
                1250,
                region: "us-east-1"
            );
            
            // Assert
            Assert.NotNull(spendLog);
            Assert.Equal(org.Id, spendLog.OrganizationId);
            Assert.Equal(12.50m, spendLog.AmountUsd);
            Assert.Equal("gpt-4-turbo", spendLog.Model);
            Assert.Equal("openai", spendLog.Provider);
            Assert.Equal(1250, spendLog.Tokens);
            Assert.Equal("us-east-1", spendLog.Region);
        }
        
        [Fact]
        public async Task GetTransactionHistoryAsync_HasTransactions_ReturnsOrderedList()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 100.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Create transactions
            await _billingService.TopUpCreditsAsync(org.Id, 100.00m, Guid.NewGuid());
            await Task.Delay(10); // Ensure different timestamps
            await _billingService.ChargeUsageAsync(org.Id, 25.00m, "Test charge 1");
            await Task.Delay(10);
            await _billingService.ChargeUsageAsync(org.Id, 15.00m, "Test charge 2");
            
            // Act
            var history = await _billingService.GetTransactionHistoryAsync(org.Id);
            
            // Assert
            Assert.Equal(3, history.Count);
            // Should be ordered by CreatedAt descending
            Assert.Equal("charge", history[0].TransactionType);
            Assert.Equal("charge", history[1].TransactionType);
            Assert.Equal("topup", history[2].TransactionType);
        }
        
        [Fact]
        public async Task GetSpendingSummaryAsync_HasSpendLogs_ReturnsSummary()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-org",
                Name = "Test Org",
                PrimaryRegion = "us-east-1",
                CreditBalance = 100.00m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            var periodStart = DateTime.UtcNow.AddDays(-30);
            
            await _billingService.LogSpendAsync(org.Id, 10.00m, "gpt-4", "openai", 1000);
            await _billingService.LogSpendAsync(org.Id, 20.00m, "gpt-4", "openai", 2000);
            await _billingService.LogSpendAsync(org.Id, 15.00m, "claude-3", "anthropic", 1500);
            
            var periodEnd = DateTime.UtcNow;
            
            // Act
            var summary = await _billingService.GetSpendingSummaryAsync(org.Id, periodStart, periodEnd);
            
            // Assert
            Assert.Equal(45.00m, summary["total"]);
            Assert.Equal(30.00m, summary["model_gpt-4"]);
            Assert.Equal(15.00m, summary["model_claude-3"]);
        }
    }
}
