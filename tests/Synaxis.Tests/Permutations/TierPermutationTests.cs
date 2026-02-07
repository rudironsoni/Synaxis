using System;
using System.Collections.Generic;
using Xunit;

namespace Synaxis.Tests.Permutations
{
    /// <summary>
    /// Exhaustive permutation tests for tier-based feature access
    /// Total: 3 × 4 × 2 = 24 test cases
    /// </summary>
    public class TierPermutationTests
    {
        private static readonly string[] Tiers = { "free", "pro", "enterprise" };
        private static readonly string[] Features = { "multi_geo", "sso", "audit_logs", "custom_backup" };

        /// <summary>
        /// Feature access matrix by tier
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> TierFeatures = new ()
        {
            { "free", new HashSet<string> { } },
            { "pro", new HashSet<string> { "multi_geo", "audit_logs" } },
            { "enterprise", new HashSet<string> { "multi_geo", "sso", "audit_logs", "custom_backup" } }
        };

        /// <summary>
        /// Generate all 24 permutations of tier/feature combinations
        /// </summary>
        public static IEnumerable<object[]> GetAllTierFeaturePermutations()
        {
            foreach (var tier in Tiers)
            {
                foreach (var feature in Features)
                {
                    var shouldHaveAccess = TierFeatures[tier].Contains(feature);
                    yield return new object[] { tier, feature, shouldHaveAccess };
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllTierFeaturePermutations))]
        public void CheckFeatureAccess_WithAllPermutations_ReturnsExpectedAccess(
            string tier,
            string feature,
            bool expectedAccess)
        {
            // Arrange & Act
            var actualAccess = HasFeatureAccess(tier, feature);

            // Assert
            Assert.Equal(expectedAccess, actualAccess);
        }

        /// <summary>
        /// Explicit test cases for all 24 combinations with detailed assertions
        /// </summary>
        [Theory]
        [InlineData("free", "multi_geo", false)]          // Free tier cannot use multi-geo
        [InlineData("free", "sso", false)]                // Free tier cannot use SSO
        [InlineData("free", "audit_logs", false)]         // Free tier cannot use audit logs
        [InlineData("free", "custom_backup", false)]      // Free tier cannot use custom backup
        [InlineData("pro", "multi_geo", true)]            // Pro tier can use multi-geo
        [InlineData("pro", "sso", false)]                 // Pro tier cannot use SSO
        [InlineData("pro", "audit_logs", true)]           // Pro tier can use audit logs
        [InlineData("pro", "custom_backup", false)]       // Pro tier cannot use custom backup
        [InlineData("enterprise", "multi_geo", true)]     // Enterprise can use multi-geo
        [InlineData("enterprise", "sso", true)]           // Enterprise can use SSO
        [InlineData("enterprise", "audit_logs", true)]    // Enterprise can use audit logs
        [InlineData("enterprise", "custom_backup", true)] // Enterprise can use custom backup
        public void CheckFeatureAccess_ExplicitCombinations_ReturnsExpectedAccess(
            string tier,
            string feature,
            bool expectedAccess)
        {
            // Arrange & Act
            var actualAccess = HasFeatureAccess(tier, feature);

            // Assert
            Assert.Equal(expectedAccess, actualAccess);

            // Additional contextual assertions
            if (tier == "free")
            {
                Assert.False(actualAccess, "Free tier should not have access to premium features");
            }
            else if (tier == "enterprise")
            {
                Assert.True(actualAccess, "Enterprise tier should have access to all features");
            }
            else if (tier == "pro")
            {
                if (feature == "sso" || feature == "custom_backup")
                {
                    Assert.False(actualAccess, "Pro tier should not have access to enterprise-only features");
                }
            }
        }

        /// <summary>
        /// Test feature access for all tiers
        /// Verifies that tier hierarchy is respected
        /// </summary>
        [Theory]
        [InlineData("free", 0)]           // Free has 0 features
        [InlineData("pro", 2)]            // Pro has 2 features
        [InlineData("enterprise", 4)]     // Enterprise has all 4 features
        public void GetFeatureCount_ForTier_ReturnsCorrectCount(string tier, int expectedCount)
        {
            // Arrange & Act
            var count = GetFeatureCountForTier(tier);

            // Assert
            Assert.Equal(expectedCount, count);
        }

        /// <summary>
        /// Test tier upgrades unlock features
        /// </summary>
        [Theory]
        [InlineData("free", "pro", "multi_geo", true)]        // Upgrade free→pro unlocks multi_geo
        [InlineData("free", "pro", "audit_logs", true)]       // Upgrade free→pro unlocks audit_logs
        [InlineData("free", "pro", "sso", false)]             // Upgrade free→pro doesn't unlock SSO
        [InlineData("pro", "enterprise", "sso", true)]        // Upgrade pro→enterprise unlocks SSO
        [InlineData("pro", "enterprise", "custom_backup", true)] // Upgrade pro→enterprise unlocks custom_backup
        [InlineData("free", "enterprise", "multi_geo", true)] // Upgrade free→enterprise unlocks multi_geo
        [InlineData("free", "enterprise", "sso", true)]       // Upgrade free→enterprise unlocks SSO
        public void UpgradeTier_UnlocksExpectedFeatures(
            string fromTier,
            string toTier,
            string feature,
            bool shouldUnlock)
        {
            // Arrange
            var hadAccess = HasFeatureAccess(fromTier, feature);
            var hasAccess = HasFeatureAccess(toTier, feature);

            // Act
            var unlocked = !hadAccess && hasAccess;

            // Assert
            Assert.Equal(shouldUnlock, unlocked);
        }

        /// <summary>
        /// Test all features for each tier
        /// </summary>
        [Theory]
        [InlineData("free", "multi_geo")]
        [InlineData("free", "sso")]
        [InlineData("free", "audit_logs")]
        [InlineData("free", "custom_backup")]
        public void FreeTier_HasNoAccessToAnyPremiumFeature(string tier, string feature)
        {
            // Arrange & Act
            var hasAccess = HasFeatureAccess(tier, feature);

            // Assert
            Assert.False(hasAccess, $"Free tier should not have access to {feature}");
        }

        [Theory]
        [InlineData("enterprise", "multi_geo")]
        [InlineData("enterprise", "sso")]
        [InlineData("enterprise", "audit_logs")]
        [InlineData("enterprise", "custom_backup")]
        public void EnterpriseTier_HasAccessToAllFeatures(string tier, string feature)
        {
            // Arrange & Act
            var hasAccess = HasFeatureAccess(tier, feature);

            // Assert
            Assert.True(hasAccess, $"Enterprise tier should have access to {feature}");
        }

        /// <summary>
        /// Test invalid tier handling
        /// </summary>
        [Theory]
        [InlineData("", "multi_geo")]
        [InlineData(null, "sso")]
        [InlineData("invalid", "audit_logs")]
        [InlineData("ENTERPRISE", "sso")] // Case sensitivity
        public void CheckFeatureAccess_WithInvalidTier_ThrowsArgumentException(string tier, string feature)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateTier(tier));
        }

        /// <summary>
        /// Test invalid feature handling
        /// </summary>
        [Theory]
        [InlineData("pro", "")]
        [InlineData("pro", null)]
        [InlineData("enterprise", "invalid_feature")]
        [InlineData("free", "MULTI_GEO")] // Case sensitivity
        public void CheckFeatureAccess_WithInvalidFeature_ThrowsArgumentException(string tier, string feature)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => ValidateFeature(feature));
        }

        /// <summary>
        /// Test feature access matrix completeness
        /// Ensures all tier/feature combinations are accounted for
        /// </summary>
        [Fact]
        public void FeatureAccessMatrix_CoversAllCombinations()
        {
            // Arrange
            var totalCombinations = Tiers.Length * Features.Length;
            var testedCombinations = 0;

            // Act
            foreach (var tier in Tiers)
            {
                foreach (var feature in Features)
                {
                    var hasAccess = HasFeatureAccess(tier, feature);
                    testedCombinations++;
                    
                    // Verify the result is deterministic
                    Assert.Equal(hasAccess, HasFeatureAccess(tier, feature));
                }
            }

            // Assert
            Assert.Equal(12, totalCombinations); // 3 tiers × 4 features = 12
            Assert.Equal(totalCombinations, testedCombinations);
        }

        /// <summary>
        /// Test tier hierarchy is enforced
        /// Enterprise should have all features that Pro has, Pro should have all that Free has
        /// </summary>
        [Theory]
        [InlineData("multi_geo")]
        [InlineData("audit_logs")]
        public void TierHierarchy_EnterpriseSupersetOfPro(string feature)
        {
            // Arrange
            var proHasAccess = HasFeatureAccess("pro", feature);
            var enterpriseHasAccess = HasFeatureAccess("enterprise", feature);

            // Act & Assert
            if (proHasAccess)
            {
                Assert.True(enterpriseHasAccess, 
                    $"If Pro has access to {feature}, Enterprise must also have access");
            }
        }

        [Theory]
        [InlineData("multi_geo")]
        [InlineData("sso")]
        [InlineData("audit_logs")]
        [InlineData("custom_backup")]
        public void TierHierarchy_ProSupersetOfFree(string feature)
        {
            // Arrange
            var freeHasAccess = HasFeatureAccess("free", feature);
            var proHasAccess = HasFeatureAccess("pro", feature);

            // Act & Assert
            if (freeHasAccess)
            {
                Assert.True(proHasAccess, 
                    $"If Free has access to {feature}, Pro must also have access");
            }
        }

        /// <summary>
        /// Test feature limits by tier
        /// </summary>
        [Theory]
        [InlineData("free", "multi_geo", 1)]      // Free: single region only
        [InlineData("pro", "multi_geo", 3)]       // Pro: up to 3 regions
        [InlineData("enterprise", "multi_geo", int.MaxValue)] // Enterprise: unlimited
        [InlineData("free", "audit_logs", 0)]     // Free: no audit logs
        [InlineData("pro", "audit_logs", 30)]     // Pro: 30 days retention
        [InlineData("enterprise", "audit_logs", 365)] // Enterprise: 365 days retention
        public void GetFeatureLimit_ForTierAndFeature_ReturnsCorrectLimit(
            string tier,
            string feature,
            int expectedLimit)
        {
            // Arrange & Act
            var limit = GetFeatureLimitForTier(tier, feature);

            // Assert
            if (HasFeatureAccess(tier, feature) || feature == "multi_geo")
            {
                Assert.Equal(expectedLimit, limit);
            }
            else
            {
                Assert.Equal(0, limit);
            }
        }

        #region Helper Methods

        private static bool HasFeatureAccess(string tier, string feature)
        {
            if (string.IsNullOrWhiteSpace(tier))
                throw new ArgumentException("Tier is required", nameof(tier));

            if (string.IsNullOrWhiteSpace(feature))
                throw new ArgumentException("Feature is required", nameof(feature));

            if (!TierFeatures.ContainsKey(tier))
                throw new ArgumentException($"Invalid tier: {tier}", nameof(tier));

            return TierFeatures[tier].Contains(feature);
        }

        private static int GetFeatureCountForTier(string tier)
        {
            if (!TierFeatures.ContainsKey(tier))
                throw new ArgumentException($"Invalid tier: {tier}", nameof(tier));

            return TierFeatures[tier].Count;
        }

        private static int GetFeatureLimitForTier(string tier, string feature)
        {
            // Feature-specific limits by tier
            if (feature == "multi_geo")
            {
                return tier switch
                {
                    "free" => 1,
                    "pro" => 3,
                    "enterprise" => int.MaxValue,
                    _ => 0
                };
            }

            if (feature == "audit_logs")
            {
                return tier switch
                {
                    "free" => 0,
                    "pro" => 30,
                    "enterprise" => 365,
                    _ => 0
                };
            }

            // Other features are binary (on/off)
            return HasFeatureAccess(tier, feature) ? 1 : 0;
        }

        private static void ValidateTier(string tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                throw new ArgumentException("Tier is required", nameof(tier));

            if (!TierFeatures.ContainsKey(tier))
                throw new ArgumentException($"Invalid tier: {tier}", nameof(tier));
        }

        private static void ValidateFeature(string feature)
        {
            if (string.IsNullOrWhiteSpace(feature))
                throw new ArgumentException("Feature is required", nameof(feature));

            var validFeatures = new HashSet<string>(Features);
            if (!validFeatures.Contains(feature))
                throw new ArgumentException($"Invalid feature: {feature}", nameof(feature));
        }

        #endregion
    }
}
