using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Integration
{
    /// <summary>
    /// Integration tests for billing and caching services working together
    /// </summary>
    public class BillingCacheIntegrationTests : IDisposable
    {
        private readonly SynaxisDbContext _context;
        private readonly ICacheService _cacheService;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly IBillingService _billingService;
        
        public BillingCacheIntegrationTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SynaxisDbContext(options);
            
            // Setup cache service
            var mockDistributedCache = new Mock<IDistributedCache>();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var mockCacheLogger = new Mock<ILogger<CacheService>>();
            _cacheService = new CacheService(mockDistributedCache.Object, memoryCache, mockCacheLogger.Object);
            
            // Setup exchange rate provider with cache
            var mockRateLogger = new Mock<ILogger<ExchangeRateProvider>>();
            _exchangeRateProvider = new ExchangeRateProvider(_cacheService, mockRateLogger.Object);
            
            // Setup billing service
            var mockBillingLogger = new Mock<ILogger<BillingService>>();
            _billingService = new BillingService(_context, _exchangeRateProvider, mockBillingLogger.Object);
        }
        
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
        
        [Fact]
        public async Task EndToEnd_ChargeUsageAndConvertCurrency_WorksCorrectly()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "acme-corp",
                Name = "ACME Corporation",
                PrimaryRegion = "us-east-1",
                CreditBalance = 1000.00m,
                BillingCurrency = "EUR"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            // Act 1: Log spending
            var spendLog = await _billingService.LogSpendAsync(
                org.Id,
                50.00m,
                "gpt-4-turbo",
                "openai",
                5000,
                region: "us-east-1"
            );
            
            // Act 2: Charge usage
            var transaction = await _billingService.ChargeUsageAsync(
                org.Id,
                50.00m,
                "API usage charge"
            );
            
            // Act 3: Get balance in billing currency (should use cached exchange rate)
            var balanceEur = await _billingService.GetCreditBalanceInBillingCurrencyAsync(org.Id);
            
            // Assert
            Assert.NotNull(spendLog);
            Assert.Equal(50.00m, spendLog.AmountUsd);
            
            Assert.NotNull(transaction);
            Assert.Equal(950.00m, transaction.BalanceAfterUsd);
            
            // Balance should be converted to EUR (950 USD * ~0.92 = ~874 EUR)
            Assert.InRange(balanceEur, 850m, 900m);
        }
        
        [Fact]
        public async Task ExchangeRateCaching_MultipleRequests_UseCachedRate()
        {
            // Arrange & Act
            var rate1 = await _exchangeRateProvider.GetRateAsync("EUR");
            var rate2 = await _exchangeRateProvider.GetRateAsync("EUR");
            var rate3 = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Assert - all should return same rate from cache
            Assert.Equal(rate1, rate2);
            Assert.Equal(rate2, rate3);
            Assert.InRange(rate1, 0.85m, 0.99m);
        }
        
        [Fact]
        public async Task BillingWorkflow_TopUpChargeAndInvoice_WorksEndToEnd()
        {
            // Arrange
            var org = new Organization
            {
                Id = Guid.NewGuid(),
                Slug = "test-startup",
                Name = "Test Startup",
                PrimaryRegion = "us-east-1",
                CreditBalance = 0m,
                BillingCurrency = "USD"
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();
            
            var userId = Guid.NewGuid();
            
            // Act 1: Top up credits
            await _billingService.TopUpCreditsAsync(org.Id, 500.00m, userId, "Initial credit purchase");
            
            // Act 2: Log and charge some usage
            await _billingService.LogSpendAsync(org.Id, 25.00m, "gpt-4", "openai", 2500);
            await _billingService.ChargeUsageAsync(org.Id, 25.00m, "GPT-4 usage");
            
            await _billingService.LogSpendAsync(org.Id, 15.00m, "claude-3", "anthropic", 1500);
            await _billingService.ChargeUsageAsync(org.Id, 15.00m, "Claude-3 usage");
            
            // Act 3: Generate invoice
            var periodStart = DateTime.UtcNow.AddDays(-30);
            var periodEnd = DateTime.UtcNow;
            var invoice = await _billingService.GenerateInvoiceAsync(org.Id, periodStart, periodEnd);
            
            // Act 4: Get transaction history
            var history = await _billingService.GetTransactionHistoryAsync(org.Id);
            
            // Assert
            var finalBalance = await _billingService.GetCreditBalanceAsync(org.Id);
            Assert.Equal(460.00m, finalBalance); // 500 - 25 - 15
            
            Assert.NotNull(invoice);
            Assert.Equal(40.00m, invoice.TotalAmountUsd); // 25 + 15
            Assert.Equal("USD", invoice.BillingCurrency);
            
            Assert.Equal(3, history.Count); // 1 topup + 2 charges
            Assert.Equal("topup", history[2].TransactionType);
        }
        
        [Fact]
        public async Task MultiCurrency_ConversionAccuracy_MaintainsConsistency()
        {
            // Arrange
            var amount = 100.00m;
            
            // Act - Convert to all supported currencies
            var amountEur = await _billingService.ConvertCurrencyAsync(amount, "EUR");
            var amountBrl = await _billingService.ConvertCurrencyAsync(amount, "BRL");
            var amountGbp = await _billingService.ConvertCurrencyAsync(amount, "GBP");
            var amountUsd = await _billingService.ConvertCurrencyAsync(amount, "USD");
            
            // Assert - USD should remain unchanged
            Assert.Equal(100.00m, amountUsd);
            
            // Other currencies should be different but positive
            Assert.NotEqual(amount, amountEur);
            Assert.NotEqual(amount, amountBrl);
            Assert.NotEqual(amount, amountGbp);
            
            Assert.True(amountEur > 0);
            Assert.True(amountBrl > 0);
            Assert.True(amountGbp > 0);
            
            // EUR and GBP should be less than USD (stronger currencies)
            Assert.True(amountEur < amount);
            Assert.True(amountGbp < amount);
            
            // BRL should be more than USD (weaker currency)
            Assert.True(amountBrl > amount);
        }
        
        [Fact]
        public async Task TenantScopedCaching_DifferentTenants_IsolatedData()
        {
            // Arrange
            var tenant1 = Guid.NewGuid();
            var tenant2 = Guid.NewGuid();
            
            var data1 = "Tenant 1 Data";
            var data2 = "Tenant 2 Data";
            
            // Act
            var key1 = _cacheService.GetTenantKey(tenant1, "config");
            var key2 = _cacheService.GetTenantKey(tenant2, "config");
            
            await _cacheService.SetAsync(key1, data1);
            await _cacheService.SetAsync(key2, data2);
            
            var retrieved1 = await _cacheService.GetAsync<string>(key1);
            var retrieved2 = await _cacheService.GetAsync<string>(key2);
            
            // Assert
            Assert.Equal(data1, retrieved1);
            Assert.Equal(data2, retrieved2);
            Assert.NotEqual(key1, key2);
        }
    }
}
