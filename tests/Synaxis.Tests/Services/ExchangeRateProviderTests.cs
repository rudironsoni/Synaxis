using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Services;
using Xunit;

namespace Synaxis.Tests.Services
{
    public class ExchangeRateProviderTests
    {
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<ExchangeRateProvider>> _mockLogger;
        private readonly ExchangeRateProvider _exchangeRateProvider;
        
        public ExchangeRateProviderTests()
        {
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<ExchangeRateProvider>>();
            _exchangeRateProvider = new ExchangeRateProvider(_mockCacheService.Object, _mockLogger.Object);
        }
        
        [Fact]
        public async Task GetRateAsync_UsdCurrency_ReturnsOne()
        {
            // Act
            var rate = await _exchangeRateProvider.GetRateAsync("USD");
            
            // Assert
            Assert.Equal(1.00m, rate);
        }
        
        [Theory]
        [InlineData("EUR")]
        [InlineData("BRL")]
        [InlineData("GBP")]
        public async Task GetRateAsync_SupportedCurrency_ReturnsRate(string currency)
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var rate = await _exchangeRateProvider.GetRateAsync(currency);
            
            // Assert
            Assert.True(rate > 0);
            Assert.NotEqual(1.00m, rate); // Non-USD should not be 1.00
        }
        
        [Fact]
        public async Task GetRateAsync_CachedRate_ReturnsCachedValue()
        {
            // Arrange
            var cachedRates = new Dictionary<string, decimal>
            {
                { "USD", 1.00m },
                { "EUR", 0.92m }
            };
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>("exchange_rates:all"))
                .ReturnsAsync(cachedRates);
            
            // Act
            var rate = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Assert
            Assert.Equal(0.92m, rate);
            _mockCacheService.Verify(x => x.GetAsync<Dictionary<string, decimal>>("exchange_rates:all"), Times.Once);
        }
        
        [Fact]
        public async Task GetRateAsync_FetchNewRates_CachesResult()
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var rate = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Assert
            Assert.True(rate > 0);
            _mockCacheService.Verify(
                x => x.SetAsync("exchange_rates:all", It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()),
                Times.Once
            );
        }
        
        [Fact]
        public async Task GetRateAsync_UnsupportedCurrency_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _exchangeRateProvider.GetRateAsync("JPY")
            );
        }
        
        [Fact]
        public async Task GetRatesAsync_MultipleCurrencies_ReturnsAllRates()
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var rates = await _exchangeRateProvider.GetRatesAsync("USD", "EUR", "BRL");
            
            // Assert
            Assert.Equal(3, rates.Count);
            Assert.Contains("USD", rates.Keys);
            Assert.Contains("EUR", rates.Keys);
            Assert.Contains("BRL", rates.Keys);
            Assert.Equal(1.00m, rates["USD"]);
        }
        
        [Theory]
        [InlineData("USD", true)]
        [InlineData("EUR", true)]
        [InlineData("BRL", true)]
        [InlineData("GBP", true)]
        [InlineData("JPY", false)]
        [InlineData("CAD", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsSupported_VariousCurrencies_ReturnsExpected(string currency, bool expected)
        {
            // Act
            var result = _exchangeRateProvider.IsSupported(currency);
            
            // Assert
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public void GetSupportedCurrencies_ReturnsAllSupported()
        {
            // Act
            var currencies = _exchangeRateProvider.GetSupportedCurrencies();
            
            // Assert
            Assert.Equal(4, currencies.Length);
            Assert.Contains("USD", currencies);
            Assert.Contains("EUR", currencies);
            Assert.Contains("BRL", currencies);
            Assert.Contains("GBP", currencies);
        }
        
        [Fact]
        public async Task GetRateAsync_CaseInsensitive_ReturnsRate()
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            
            // Act
            var rate1 = await _exchangeRateProvider.GetRateAsync("eur");
            var rate2 = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Assert
            Assert.True(rate1 > 0);
            Assert.True(rate2 > 0);
        }
        
        [Fact]
        public async Task GetRateAsync_RatesVaryReasonably_WithinExpectedRange()
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, decimal>>(), It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
            
            // Act - fetch multiple times
            var rate1 = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Reset cache
            _mockCacheService.Setup(x => x.GetAsync<Dictionary<string, decimal>>(It.IsAny<string>()))
                .ReturnsAsync((Dictionary<string, decimal>)null);
            
            var rate2 = await _exchangeRateProvider.GetRateAsync("EUR");
            
            // Assert - should be in reasonable range (0.90 - 0.95 for EUR)
            Assert.InRange(rate1, 0.85m, 0.99m);
            Assert.InRange(rate2, 0.85m, 0.99m);
            
            // Variance should be small (within ±2%)
            var variance = Math.Abs(rate1 - rate2);
            Assert.True(variance <= 0.02m);
        }
    }
}
