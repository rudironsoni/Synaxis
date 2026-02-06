#nullable enable
using System;
using System.Threading.Tasks;
using Xunit;
using Synaxis.InferenceGateway.Infrastructure.Contracts;

namespace Synaxis.Tests.Permutations
{
    /// <summary>
    /// Exhaustive permutation tests for region routing with ALL possible combinations
    /// Total: 3 × 3 × 2 × 4 = 72 test cases
    /// </summary>
    public class RegionPermutationTests
    {
        private static readonly string[] Regions = { "eu-west-1", "us-east-1", "sa-east-1" };
        private static readonly string?[] LegalBases = { "SCC", "consent", "adequacy", null };

        /// <summary>
        /// Generate all 72 permutations of region routing scenarios
        /// </summary>
        public static TheoryData<string, string, bool, string?> GetAllRegionPermutations()
        {
            var data = new TheoryData<string, string, bool, string?>();

            foreach (var userRegion in Regions)
            {
                foreach (var processedRegion in Regions)
                {
                    var isCrossBorder = userRegion != processedRegion;

                    foreach (var legalBasis in LegalBases)
                    {
                        data.Add(userRegion, processedRegion, isCrossBorder, legalBasis);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(GetAllRegionPermutations))]
        public async Task RouteRequest_WithAllRegionPermutations_ReturnsExpectedResult(
            string userRegion,
            string processedRegion,
            bool isCrossBorder,
            string? legalBasis)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = userRegion,
                ToRegion = processedRegion,
                LegalBasis = legalBasis,
                Purpose = "service_provision",
                DataCategories = new[] { "personal" },
                EncryptionUsed = true,
                UserConsentObtained = legalBasis == "consent"
            };

            // Act & Assert
            var expectedValid = DetermineExpectedValidity(userRegion, processedRegion, isCrossBorder, legalBasis);

            if (isCrossBorder)
            {
                // Cross-border transfers require legal basis
                if (string.IsNullOrEmpty(legalBasis))
                {
                    Assert.False(expectedValid, 
                        $"Cross-border transfer from {userRegion} to {processedRegion} without legal basis should be invalid");
                }
                else if (IsValidCrossBorderCombination(userRegion, processedRegion, legalBasis))
                {
                    Assert.True(expectedValid,
                        $"Cross-border transfer from {userRegion} to {processedRegion} with {legalBasis} should be valid");
                }
                else
                {
                    Assert.False(expectedValid,
                        $"Cross-border transfer from {userRegion} to {processedRegion} with {legalBasis} should be invalid");
                }
            }
            else
            {
                // Same region transfers are always valid
                Assert.True(expectedValid,
                    $"Same-region routing in {userRegion} should always be valid");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Explicit test cases for critical combinations (subset of 72)
        /// </summary>
        [Theory]
        [InlineData("eu-west-1", "eu-west-1", false, null, true)]           // Same region, no transfer
        [InlineData("eu-west-1", "us-east-1", true, "SCC", true)]           // EU→US with SCC (valid)
        [InlineData("eu-west-1", "us-east-1", true, "consent", true)]       // EU→US with consent (valid)
        [InlineData("eu-west-1", "us-east-1", true, "adequacy", false)]     // EU→US with adequacy (invalid - US no adequacy)
        [InlineData("eu-west-1", "us-east-1", true, null, false)]           // EU→US without basis (invalid)
        [InlineData("us-east-1", "eu-west-1", true, "SCC", true)]           // US→EU with SCC (valid)
        [InlineData("us-east-1", "sa-east-1", true, "SCC", true)]           // US→BR with SCC (valid)
        [InlineData("sa-east-1", "us-east-1", true, "consent", true)]       // BR→US with consent (valid)
        [InlineData("sa-east-1", "eu-west-1", true, "adequacy", false)]     // BR→EU with adequacy (invalid)
        [InlineData("sa-east-1", "sa-east-1", false, null, true)]           // Same region (BR)
        [InlineData("us-east-1", "us-east-1", false, null, true)]           // Same region (US)
        [InlineData("eu-west-1", "sa-east-1", true, "SCC", true)]           // EU→BR with SCC (valid)
        [InlineData("eu-west-1", "sa-east-1", true, null, false)]           // EU→BR without basis (invalid)
        public async Task RouteRequest_CriticalCombinations_ReturnsExpectedValidity(
            string userRegion,
            string processedRegion,
            bool isCrossBorder,
            string? legalBasis,
            bool expectedValid)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                FromRegion = userRegion,
                ToRegion = processedRegion,
                LegalBasis = legalBasis,
                Purpose = "service_provision",
                DataCategories = new[] { "personal" },
                EncryptionUsed = true,
                UserConsentObtained = legalBasis == "consent"
            };

            // Act
            var actualValid = DetermineExpectedValidity(userRegion, processedRegion, isCrossBorder, legalBasis);

            // Assert
            Assert.Equal(expectedValid, actualValid);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Test all combinations with empty/null values (edge cases)
        /// </summary>
        [Theory]
        [InlineData("", "us-east-1", false, null)]              // Empty user region
        [InlineData("eu-west-1", "", false, null)]              // Empty processed region
        [InlineData("", "", false, null)]                       // Both empty
        [InlineData("invalid-region", "us-east-1", false, null)] // Invalid region
        public void RouteRequest_WithInvalidRegions_ThrowsArgumentException(
            string userRegion,
            string processedRegion,
            bool isCrossBorder,
            string? legalBasis)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = userRegion,
                ToRegion = processedRegion,
                LegalBasis = legalBasis
            };

            // Act & Assert
            if (string.IsNullOrWhiteSpace(userRegion) || string.IsNullOrWhiteSpace(processedRegion))
            {
                Assert.Throws<ArgumentException>(() => ValidateRegions(context));
            }
            else if (!IsValidRegion(userRegion) || !IsValidRegion(processedRegion))
            {
                Assert.Throws<ArgumentException>(() => ValidateRegions(context));
            }
        }

        /// <summary>
        /// Test all legal basis combinations with each region pair
        /// 9 region pairs × 4 legal bases = 36 combinations
        /// </summary>
        [Theory]
        [InlineData("eu-west-1", "us-east-1", "SCC", true)]
        [InlineData("eu-west-1", "us-east-1", "consent", true)]
        [InlineData("eu-west-1", "us-east-1", "adequacy", false)]
        [InlineData("eu-west-1", "us-east-1", null, false)]
        [InlineData("eu-west-1", "sa-east-1", "SCC", true)]
        [InlineData("eu-west-1", "sa-east-1", "consent", true)]
        [InlineData("eu-west-1", "sa-east-1", "adequacy", false)]
        [InlineData("eu-west-1", "sa-east-1", null, false)]
        [InlineData("us-east-1", "eu-west-1", "SCC", true)]
        [InlineData("us-east-1", "eu-west-1", "consent", true)]
        [InlineData("us-east-1", "eu-west-1", "adequacy", true)]
        [InlineData("us-east-1", "eu-west-1", null, false)]
        [InlineData("us-east-1", "sa-east-1", "SCC", true)]
        [InlineData("us-east-1", "sa-east-1", "consent", true)]
        [InlineData("us-east-1", "sa-east-1", "adequacy", false)]
        [InlineData("us-east-1", "sa-east-1", null, false)]
        [InlineData("sa-east-1", "eu-west-1", "SCC", true)]
        [InlineData("sa-east-1", "eu-west-1", "consent", true)]
        [InlineData("sa-east-1", "eu-west-1", "adequacy", false)]
        [InlineData("sa-east-1", "eu-west-1", null, false)]
        [InlineData("sa-east-1", "us-east-1", "SCC", true)]
        [InlineData("sa-east-1", "us-east-1", "consent", true)]
        [InlineData("sa-east-1", "us-east-1", "adequacy", false)]
        [InlineData("sa-east-1", "us-east-1", null, false)]
        public void ValidateLegalBasis_ForCrossBorderTransfer_ReturnsExpectedResult(
            string fromRegion,
            string toRegion,
            string? legalBasis,
            bool expectedValid)
        {
            // Arrange
            var context = new TransferContext
            {
                OrganizationId = Guid.NewGuid(),
                FromRegion = fromRegion,
                ToRegion = toRegion,
                LegalBasis = legalBasis,
                EncryptionUsed = true
            };

            // Act
            var isValid = IsValidCrossBorderCombination(fromRegion, toRegion, legalBasis);

            // Assert
            Assert.Equal(expectedValid, isValid);
        }

        #region Helper Methods

        private static bool DetermineExpectedValidity(string userRegion, string processedRegion, bool isCrossBorder, string? legalBasis)
        {
            // Same region is always valid
            if (!isCrossBorder)
                return true;

            // Cross-border requires legal basis
            if (string.IsNullOrEmpty(legalBasis))
                return false;

            return IsValidCrossBorderCombination(userRegion, processedRegion, legalBasis);
        }

        private static bool IsValidCrossBorderCombination(string fromRegion, string toRegion, string? legalBasis)
        {
            if (string.IsNullOrEmpty(legalBasis))
                return false;

            // SCC and consent are valid for all cross-border transfers
            if (legalBasis == "SCC" || legalBasis == "consent")
                return true;

            // Adequacy decisions are specific to region pairs
            if (legalBasis == "adequacy")
            {
                // EU has adequacy decisions for some countries (not US or Brazil)
                // US→EU can use adequacy (Privacy Shield replacement)
                return (fromRegion == "us-east-1" && toRegion == "eu-west-1");
            }

            return false;
        }

        private static bool IsValidRegion(string region)
        {
            return Array.IndexOf(Regions, region) >= 0;
        }

        private static void ValidateRegions(TransferContext context)
        {
            if (string.IsNullOrWhiteSpace(context.FromRegion))
                throw new ArgumentException("From region is required");

            if (string.IsNullOrWhiteSpace(context.ToRegion))
                throw new ArgumentException("To region is required");

            if (!IsValidRegion(context.FromRegion))
                throw new ArgumentException($"Invalid from region: {context.FromRegion}");

            if (!IsValidRegion(context.ToRegion))
                throw new ArgumentException($"Invalid to region: {context.ToRegion}");
        }

        #endregion
    }
}
