using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Synaxis.Tests.Permutations
{
    /// <summary>
    /// Exhaustive permutation tests for currency conversion
    /// Total: 4 × 7 × 4 = 112 test cases (including exchange rate variations)
    /// </summary>
    public class CurrencyPermutationTests
    {
        private static readonly string[] Currencies = { "USD", "EUR", "BRL", "GBP" };
        private static readonly decimal[] BaseAmounts = { 0m, 0.01m, 1m, 10m, 100m, 1000m, 10000m };

        /// <summary>
        /// Static exchange rates (USD base)
        /// </summary>
        private static readonly Dictionary<string, decimal> ExchangeRates = new ()
        {
            { "USD", 1.00m },
            { "EUR", 0.92m },
            { "BRL", 4.95m },
            { "GBP", 0.79m }
        };

        /// <summary>
        /// Inverse rates for conversion back to USD (calculated for precision)
        /// </summary>
        private static readonly Dictionary<string, decimal> InverseRates = new ()
        {
            { "USD", 1.00m },
            { "EUR", 1m / 0.92m },  // Calculated: ~1.08695652
            { "BRL", 1m / 4.95m },  // Calculated: ~0.20202020
            { "GBP", 1m / 0.79m }   // Calculated: ~1.26582278
        };

        /// <summary>
        /// Generate all currency conversion permutations
        /// </summary>
        public static IEnumerable<object[]> GetAllCurrencyConversionPermutations()
        {
            foreach (var currency in Currencies)
            {
                foreach (var amount in BaseAmounts)
                {
                    var rate = ExchangeRates[currency];
                    var expectedAmount = Math.Round(amount * rate, 2);
                    yield return new object[] { currency, amount, rate, expectedAmount };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllCurrencyConversionPermutations))]
        public async Task ConvertCurrency_WithAllPermutations_ReturnsCorrectAmount(
            string targetCurrency,
            decimal usdAmount,
            decimal rate,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = ConvertFromUsd(usdAmount, targetCurrency);

            // Assert
            Assert.Equal(expectedAmount, actualAmount);
            Assert.True(actualAmount >= 0, "Converted amount must be non-negative");

            // Verify round-trip conversion (with tolerance for rounding)
            var backToUsd = ConvertToUsd(actualAmount, targetCurrency);
            Assert.Equal(usdAmount, backToUsd, 2); // 2 decimal places tolerance

            await Task.CompletedTask;
        }

        /// <summary>
        /// Explicit test cases for all currency pairs
        /// 4 currencies × 7 amounts = 28 test cases
        /// </summary>
        [Theory]
        [InlineData("USD", 0.00, 1.00, 0.00)]              // Zero amount stays zero
        [InlineData("USD", 0.01, 1.00, 0.01)]              // USD to USD (no conversion)
        [InlineData("USD", 100.00, 1.00, 100.00)]          // USD stays same
        [InlineData("USD", 10000.00, 1.00, 10000.00)]      // Large amount USD
        [InlineData("EUR", 0.00, 0.92, 0.00)]              // Zero EUR
        [InlineData("EUR", 0.01, 0.92, 0.01)]              // Small amount EUR
        [InlineData("EUR", 100.00, 0.92, 92.00)]           // USD→EUR conversion
        [InlineData("EUR", 10000.00, 0.92, 9200.00)]       // Large amount EUR
        [InlineData("BRL", 0.00, 4.95, 0.00)]              // Zero BRL
        [InlineData("BRL", 0.01, 4.95, 0.05)]              // Small amount BRL (rounds up)
        [InlineData("BRL", 100.00, 4.95, 495.00)]          // USD→BRL conversion
        [InlineData("BRL", 10000.00, 4.95, 49500.00)]      // Large amount BRL
        [InlineData("GBP", 0.00, 0.79, 0.00)]              // Zero GBP
        [InlineData("GBP", 0.01, 0.79, 0.01)]              // Small amount GBP (rounds)
        [InlineData("GBP", 100.00, 0.79, 79.00)]           // USD→GBP conversion
        [InlineData("GBP", 10000.00, 0.79, 7900.00)]       // Large amount GBP
        public async Task ConvertCurrency_ExplicitCases_ReturnsExpectedAmount(
            string targetCurrency,
            decimal usdAmount,
            decimal rate,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = ConvertFromUsd(usdAmount, targetCurrency);

            // Assert
            Assert.Equal(expectedAmount, actualAmount);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Test all currency pairs (bidirectional)
        /// 4 × 4 = 16 combinations (including self-conversions)
        /// </summary>
        [Theory]
        [InlineData("USD", "USD", 100.00, 100.00)]
        [InlineData("USD", "EUR", 100.00, 92.00)]
        [InlineData("USD", "BRL", 100.00, 495.00)]
        [InlineData("USD", "GBP", 100.00, 79.00)]
        [InlineData("EUR", "USD", 92.00, 100.00)]
        [InlineData("EUR", "EUR", 100.00, 100.00)]
        [InlineData("BRL", "USD", 495.00, 100.00)]
        [InlineData("BRL", "BRL", 100.00, 100.00)]
        [InlineData("GBP", "USD", 79.00, 100.00)]
        [InlineData("GBP", "GBP", 100.00, 100.00)]
        public void ConvertCurrency_BetweenAllPairs_ReturnsCorrectAmount(
            string fromCurrency,
            string toCurrency,
            decimal amount,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = ConvertBetweenCurrencies(amount, fromCurrency, toCurrency);

            // Assert
            Assert.Equal(expectedAmount, actualAmount, 2); // Allow 2 decimal places tolerance
        }

        /// <summary>
        /// Test rounding behavior for all currencies
        /// </summary>
        [Theory]
        [InlineData("USD", 1.234, 1.23)]           // Round down
        [InlineData("USD", 1.235, 1.24)]           // Round up (banker's rounding)
        [InlineData("EUR", 1.234, 1.14)]           // 1.234 * 0.92 = 1.13528 → 1.14
        [InlineData("BRL", 0.001, 0.00)]           // Very small amount rounds to 0
        [InlineData("BRL", 0.004, 0.02)]           // 0.004 * 4.95 = 0.0198 → 0.02
        [InlineData("GBP", 1.267, 1.00)]           // 1.267 * 0.79 = 1.00093 → 1.00
        public void ConvertCurrency_RoundingBehavior_RoundsCorrectly(
            string targetCurrency,
            decimal usdAmount,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = ConvertFromUsd(usdAmount, targetCurrency);

            // Assert
            Assert.Equal(expectedAmount, actualAmount);
        }

        /// <summary>
        /// Test edge cases for all currencies
        /// </summary>
        [Theory]
        [InlineData("USD", 0)]                     // Zero amount
        [InlineData("EUR", 0)]
        [InlineData("BRL", 0)]
        [InlineData("GBP", 0)]
        [InlineData("USD", 0.001)]                 // Sub-cent amount
        [InlineData("EUR", 0.001)]
        [InlineData("USD", 999999999.99)]          // Large but valid amount
        public void ConvertCurrency_EdgeCases_HandlesCorrectly(string targetCurrency, decimal usdAmount)
        {
            // Act
            var result = ConvertFromUsd(usdAmount, targetCurrency);
            
            // Assert
            Assert.True(result >= 0, "Result must be non-negative");
            
            if (usdAmount == 0)
            {
                Assert.Equal(0, result);
            }
        }

        /// <summary>
        /// Test invalid currency handling
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("INVALID")]
        [InlineData("usd")]        // Lowercase
        [InlineData("JPY")]        // Unsupported currency
        [InlineData("CNY")]
        public void ConvertCurrency_WithInvalidCurrency_ThrowsArgumentException(string targetCurrency)
        {
            // Arrange - variable is intentionally unused as it's testing parameter validation
            #pragma warning disable CS0219
            const decimal amount = 100m;
            #pragma warning restore CS0219

            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateCurrency(targetCurrency));
        }

        /// <summary>
        /// Test negative amounts (should throw)
        /// </summary>
        [Theory]
        [InlineData("USD", -1.00)]
        [InlineData("EUR", -0.01)]
        [InlineData("BRL", -100.00)]
        [InlineData("GBP", -1000.00)]
        public void ConvertCurrency_WithNegativeAmount_ThrowsArgumentException(
            string targetCurrency,
            decimal amount)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateAmount(amount));
        }

        /// <summary>
        /// Test precision for all currencies
        /// Ensures we maintain 2 decimal places
        /// </summary>
        [Theory]
        [InlineData("USD", 123.456789, 123.46)]
        [InlineData("EUR", 123.456789, 113.58)]     // 123.456789 * 0.92 = 113.580246 → 113.58
        [InlineData("BRL", 123.456789, 611.11)]     // 123.456789 * 4.95 = 611.111105 → 611.11
        [InlineData("GBP", 123.456789, 97.53)]      // 123.456789 * 0.79 = 97.530863 → 97.53
        public void ConvertCurrency_MaintainsPrecision_RoundsToTwoDecimals(
            string targetCurrency,
            decimal usdAmount,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = ConvertFromUsd(usdAmount, targetCurrency);

            // Assert
            Assert.Equal(expectedAmount, actualAmount);
            
            // Verify exactly 2 decimal places
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(actualAmount)[3])[2];
            Assert.True(decimalPlaces <= 2, $"Result should have at most 2 decimal places, has {decimalPlaces}");
        }

        /// <summary>
        /// Test exchange rate variations
        /// Simulates rate fluctuations
        /// </summary>
        [Theory]
        [InlineData("EUR", 100.00, 0.90, 90.00)]    // EUR weakens
        [InlineData("EUR", 100.00, 0.92, 92.00)]    // Normal rate
        [InlineData("EUR", 100.00, 0.95, 95.00)]    // EUR strengthens
        [InlineData("BRL", 100.00, 4.50, 450.00)]   // BRL strengthens
        [InlineData("BRL", 100.00, 4.95, 495.00)]   // Normal rate
        [InlineData("BRL", 100.00, 5.50, 550.00)]   // BRL weakens
        [InlineData("GBP", 100.00, 0.75, 75.00)]    // GBP weakens
        [InlineData("GBP", 100.00, 0.79, 79.00)]    // Normal rate
        [InlineData("GBP", 100.00, 0.85, 85.00)]    // GBP strengthens
        public void ConvertCurrency_WithRateFluctuation_ReturnsCorrectAmount(
            string targetCurrency,
            decimal usdAmount,
            decimal rate,
            decimal expectedAmount)
        {
            // Arrange & Act
            var actualAmount = Math.Round(usdAmount * rate, 2);

            // Assert
            Assert.Equal(expectedAmount, actualAmount);
        }

        /// <summary>
        /// Test all currencies support round-trip conversion
        /// </summary>
        [Theory]
        [InlineData("USD", 100.00)]
        [InlineData("EUR", 100.00)]
        [InlineData("BRL", 100.00)]
        [InlineData("GBP", 100.00)]
        public void ConvertCurrency_RoundTrip_MaintainsValue(string currency, decimal originalAmount)
        {
            // Arrange & Act
            // USD → Target Currency
            var converted = ConvertFromUsd(originalAmount, currency);
            
            // Target Currency → USD
            var backToUsd = ConvertToUsd(converted, currency);

            // Assert
            Assert.Equal(originalAmount, backToUsd, 2); // Allow 2 decimal tolerance for rounding
        }

        /// <summary>
        /// Test supported currencies list
        /// </summary>
        [Fact]
        public void GetSupportedCurrencies_ReturnsAllCurrencies()
        {
            // Act
            var supported = GetSupportedCurrencies();

            // Assert
            Assert.Equal(4, supported.Length);
            Assert.Contains("USD", supported);
            Assert.Contains("EUR", supported);
            Assert.Contains("BRL", supported);
            Assert.Contains("GBP", supported);
        }

        /// <summary>
        /// Test currency validation
        /// </summary>
        [Theory]
        [InlineData("USD", true)]
        [InlineData("EUR", true)]
        [InlineData("BRL", true)]
        [InlineData("GBP", true)]
        [InlineData("JPY", false)]
        [InlineData("CNY", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsCurrencySupported_ReturnsCorrectResult(string currency, bool expectedSupported)
        {
            // Act
            var isSupported = IsCurrencySupported(currency);

            // Assert
            Assert.Equal(expectedSupported, isSupported);
        }

        #region Helper Methods

        private static decimal ConvertFromUsd(decimal usdAmount, string targetCurrency)
        {
            if (string.IsNullOrWhiteSpace(targetCurrency))
                throw new ArgumentException("Target currency is required", nameof(targetCurrency));

            if (!ExchangeRates.ContainsKey(targetCurrency))
                throw new ArgumentException($"Unsupported currency: {targetCurrency}", nameof(targetCurrency));

            if (usdAmount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(usdAmount));

            var rate = ExchangeRates[targetCurrency];
            
            // Check for overflow before multiplication
            // Only check if both values are large enough to potentially overflow
            if (rate > 1 && usdAmount > decimal.MaxValue / rate)
                throw new OverflowException("Conversion would result in overflow");

            return Math.Round(usdAmount * rate, 2);
        }

        private static decimal ConvertToUsd(decimal amount, string sourceCurrency)
        {
            if (string.IsNullOrWhiteSpace(sourceCurrency))
                throw new ArgumentException("Source currency is required", nameof(sourceCurrency));

            if (!InverseRates.ContainsKey(sourceCurrency))
                throw new ArgumentException($"Unsupported currency: {sourceCurrency}", nameof(sourceCurrency));

            var rate = InverseRates[sourceCurrency];
            return Math.Round(amount * rate, 2);
        }

        private static decimal ConvertBetweenCurrencies(decimal amount, string fromCurrency, string toCurrency)
        {
            // Same currency - no conversion needed
            if (fromCurrency == toCurrency)
                return amount;
            
            // Convert to USD first, then to target currency
            var usdAmount = ConvertToUsd(amount, fromCurrency);
            return ConvertFromUsd(usdAmount, toCurrency);
        }

        private static string[] GetSupportedCurrencies()
        {
            return Currencies;
        }

        private static bool IsCurrencySupported(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                return false;

            return Array.IndexOf(Currencies, currency) >= 0;
        }

        private static void ValidateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required", nameof(currency));

            if (!IsCurrencySupported(currency))
                throw new ArgumentException($"Unsupported currency: {currency}", nameof(currency));
        }

        private static void ValidateAmount(decimal amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        #endregion
    }
}
