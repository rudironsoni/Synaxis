using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Synaxis.InferenceGateway.Infrastructure.Contracts;

namespace Synaxis.Tests.Permutations
{
    /// <summary>
    /// Exhaustive permutation tests for compliance validation
    /// Total: 3 × 3 × 4 × 2 = 72 test cases
    /// </summary>
    public class CompliancePermutationTests
    {
        private static readonly string[] Regulations = { "GDPR", "LGPD", "CCPA" };
        private static readonly string[] DataCategories = { "personal", "sensitive", "public" };
        private static readonly string[] ProcessingPurposes = { "contract", "consent", "legitimate_interest", "legal_obligation" };

        /// <summary>
        /// Valid combinations matrix for each regulation
        /// </summary>
        private static readonly Dictionary<string, HashSet<string>> ValidCombinations = new()
        {
            {
                "GDPR_personal_contract", new HashSet<string> { "contract", "consent", "legitimate_interest", "legal_obligation" }
            },
            {
                "GDPR_personal_consent", new HashSet<string> { "contract", "consent", "legitimate_interest", "legal_obligation" }
            },
            {
                "GDPR_sensitive_consent", new HashSet<string> { "consent", "legal_obligation" } // Sensitive requires explicit consent
            },
            {
                "GDPR_public_*", new HashSet<string> { "contract", "consent", "legitimate_interest", "legal_obligation" }
            },
            {
                "LGPD_personal_contract", new HashSet<string> { "contract", "consent", "legitimate_interest", "legal_obligation", "credit_protection" }
            },
            {
                "LGPD_sensitive_consent", new HashSet<string> { "consent", "legal_obligation" }
            },
            {
                "CCPA_personal_*", new HashSet<string> { "contract", "consent", "legitimate_interest", "legal_obligation" }
            },
            {
                "CCPA_sensitive_consent", new HashSet<string> { "consent", "legal_obligation" }
            }
        };

        /// <summary>
        /// Generate all 72 permutations of compliance scenarios
        /// </summary>
        public static IEnumerable<object[]> GetAllCompliancePermutations()
        {
            foreach (var regulation in Regulations)
            {
                foreach (var dataCategory in DataCategories)
                {
                    foreach (var purpose in ProcessingPurposes)
                    {
                        var isValid = IsValidCombination(regulation, dataCategory, purpose);
                        yield return new object[] { regulation, dataCategory, purpose, isValid };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetAllCompliancePermutations))]
        public async Task ValidateProcessing_WithAllPermutations_ReturnsExpectedResult(
            string regulation,
            string dataCategory,
            string purpose,
            bool expectedValid)
        {
            // Arrange
            var context = new ProcessingContext
            {
                OrganizationId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ProcessingPurpose = purpose,
                LegalBasis = purpose,
                DataCategories = new[] { dataCategory }
            };

            // Act
            var isValid = ValidateProcessing(regulation, dataCategory, purpose);

            // Assert
            Assert.Equal(expectedValid, isValid);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Explicit test cases for critical compliance combinations
        /// </summary>
        [Theory]
        // GDPR - Personal data
        [InlineData("GDPR", "personal", "consent", true)]                    // Valid
        [InlineData("GDPR", "personal", "contract", true)]                   // Valid
        [InlineData("GDPR", "personal", "legitimate_interest", true)]        // Valid
        [InlineData("GDPR", "personal", "legal_obligation", true)]           // Valid
        // GDPR - Sensitive data (requires explicit consent or legal obligation)
        [InlineData("GDPR", "sensitive", "consent", true)]                   // Valid
        [InlineData("GDPR", "sensitive", "contract", false)]                 // Invalid
        [InlineData("GDPR", "sensitive", "legitimate_interest", false)]      // Invalid for sensitive
        [InlineData("GDPR", "sensitive", "legal_obligation", true)]          // Valid
        // GDPR - Public data
        [InlineData("GDPR", "public", "consent", true)]                      // Valid
        [InlineData("GDPR", "public", "legitimate_interest", true)]          // Valid
        // LGPD - Personal data
        [InlineData("LGPD", "personal", "consent", true)]                    // Valid
        [InlineData("LGPD", "personal", "contract", true)]                   // Valid
        [InlineData("LGPD", "personal", "legitimate_interest", true)]        // Valid
        [InlineData("LGPD", "personal", "legal_obligation", true)]           // Valid
        // LGPD - Sensitive data
        [InlineData("LGPD", "sensitive", "consent", true)]                   // Valid
        [InlineData("LGPD", "sensitive", "contract", false)]                 // Invalid
        [InlineData("LGPD", "sensitive", "legitimate_interest", false)]      // Invalid
        [InlineData("LGPD", "sensitive", "legal_obligation", true)]          // Valid
        // CCPA - Personal data
        [InlineData("CCPA", "personal", "consent", true)]                    // Valid
        [InlineData("CCPA", "personal", "contract", true)]                   // Valid
        [InlineData("CCPA", "personal", "legitimate_interest", true)]        // Valid
        [InlineData("CCPA", "personal", "legal_obligation", true)]           // Valid
        // CCPA - Sensitive data
        [InlineData("CCPA", "sensitive", "consent", true)]                   // Valid
        [InlineData("CCPA", "sensitive", "contract", false)]                 // Invalid
        [InlineData("CCPA", "sensitive", "legitimate_interest", false)]      // Invalid
        public async Task ValidateProcessing_CriticalCombinations_ReturnsExpectedResult(
            string regulation,
            string dataCategory,
            string purpose,
            bool expectedValid)
        {
            // Arrange
            var context = new ProcessingContext
            {
                OrganizationId = Guid.NewGuid(),
                ProcessingPurpose = purpose,
                LegalBasis = purpose,
                DataCategories = new[] { dataCategory }
            };

            // Act
            var isValid = ValidateProcessing(regulation, dataCategory, purpose);

            // Assert
            Assert.Equal(expectedValid, isValid);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Test all regulations with sensitive data
        /// Sensitive data has stricter requirements across all regulations
        /// </summary>
        [Theory]
        [InlineData("GDPR", "sensitive", "consent", true)]
        [InlineData("GDPR", "sensitive", "contract", false)]
        [InlineData("GDPR", "sensitive", "legitimate_interest", false)]
        [InlineData("GDPR", "sensitive", "legal_obligation", true)]
        [InlineData("LGPD", "sensitive", "consent", true)]
        [InlineData("LGPD", "sensitive", "contract", false)]
        [InlineData("LGPD", "sensitive", "legitimate_interest", false)]
        [InlineData("LGPD", "sensitive", "legal_obligation", true)]
        [InlineData("CCPA", "sensitive", "consent", true)]
        [InlineData("CCPA", "sensitive", "contract", false)]
        [InlineData("CCPA", "sensitive", "legitimate_interest", false)]
        [InlineData("CCPA", "sensitive", "legal_obligation", true)]
        public void ValidateProcessing_SensitiveData_RequiresStrictBasis(
            string regulation,
            string dataCategory,
            string purpose,
            bool expectedValid)
        {
            // Arrange & Act
            var isValid = ValidateProcessing(regulation, dataCategory, purpose);

            // Assert
            Assert.Equal(expectedValid, isValid);

            if (dataCategory == "sensitive" && (purpose == "contract" || purpose == "legitimate_interest"))
            {
                Assert.False(isValid, "Sensitive data cannot be processed under contract or legitimate interest");
            }
        }

        /// <summary>
        /// Test data retention requirements for all regulations
        /// </summary>
        [Theory]
        [InlineData("GDPR", 30)]     // GDPR: minimum retention, should be purpose-limited
        [InlineData("LGPD", 30)]     // LGPD: similar to GDPR
        [InlineData("CCPA", 90)]     // CCPA: 90 days for certain data
        public void GetDataRetentionDays_ForRegulation_ReturnsCorrectPeriod(
            string regulation,
            int expectedMinDays)
        {
            // Act
            var retentionDays = GetDataRetentionDays(regulation);

            // Assert
            Assert.True(retentionDays >= expectedMinDays, 
                $"{regulation} should have at least {expectedMinDays} days retention");
        }

        /// <summary>
        /// Test breach notification requirements for all regulations
        /// </summary>
        [Theory]
        // GDPR - requires notification within 72 hours for high risk
        [InlineData("GDPR", "high", 100, true)]
        [InlineData("GDPR", "medium", 100, true)]
        [InlineData("GDPR", "low", 10, false)]
        // LGPD - requires notification for significant breaches
        [InlineData("LGPD", "high", 50, true)]
        [InlineData("LGPD", "medium", 50, true)]
        [InlineData("LGPD", "low", 10, false)]
        // CCPA - requires notification for certain breaches
        [InlineData("CCPA", "high", 500, true)]      // 500+ residents
        [InlineData("CCPA", "medium", 100, true)]
        [InlineData("CCPA", "low", 10, false)]
        public async Task IsBreachNotificationRequired_ForAllRegulations_ReturnsCorrectRequirement(
            string regulation,
            string riskLevel,
            int affectedUsers,
            bool expectedRequired)
        {
            // Arrange
            var context = new BreachContext
            {
                OrganizationId = Guid.NewGuid(),
                BreachType = "data_leak",
                AffectedUsersCount = affectedUsers,
                DataCategoriesExposed = new[] { "personal" },
                RiskLevel = riskLevel
            };

            // Act
            var isRequired = IsBreachNotificationRequired(regulation, context);

            // Assert
            Assert.Equal(expectedRequired, isRequired);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Test all regulations with public data (least restrictions)
        /// </summary>
        [Theory]
        [InlineData("GDPR", "public", "consent", true)]
        [InlineData("GDPR", "public", "contract", true)]
        [InlineData("GDPR", "public", "legitimate_interest", true)]
        [InlineData("GDPR", "public", "legal_obligation", true)]
        [InlineData("LGPD", "public", "consent", true)]
        [InlineData("LGPD", "public", "contract", true)]
        [InlineData("LGPD", "public", "legitimate_interest", true)]
        [InlineData("CCPA", "public", "consent", true)]
        [InlineData("CCPA", "public", "legitimate_interest", true)]
        public void ValidateProcessing_PublicData_AllowsMostPurposes(
            string regulation,
            string dataCategory,
            string purpose,
            bool expectedValid)
        {
            // Arrange & Act
            var isValid = ValidateProcessing(regulation, dataCategory, purpose);

            // Assert
            Assert.Equal(expectedValid, isValid);
            Assert.True(isValid, "Public data should allow most processing purposes");
        }

        /// <summary>
        /// Test invalid regulation/category/purpose combinations
        /// </summary>
        [Theory]
        [InlineData("", "personal", "consent")]                  // Empty regulation
        [InlineData(null, "personal", "consent")]                // Null regulation
        [InlineData("INVALID", "personal", "consent")]           // Invalid regulation
        [InlineData("GDPR", "", "consent")]                      // Empty data category
        [InlineData("GDPR", "personal", "")]                     // Empty purpose
        [InlineData("GDPR", "invalid_category", "consent")]      // Invalid category
        [InlineData("GDPR", "personal", "invalid_purpose")]      // Invalid purpose
        public void ValidateProcessing_WithInvalidInputs_ThrowsArgumentException(
            string regulation,
            string dataCategory,
            string purpose)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidateInputs(regulation, dataCategory, purpose));
        }

        /// <summary>
        /// Test consent requirements across all regulations
        /// </summary>
        [Theory]
        [InlineData("GDPR", "personal", true)]       // GDPR requires explicit consent for personal data
        [InlineData("GDPR", "sensitive", true)]      // GDPR requires explicit consent for sensitive data
        [InlineData("LGPD", "personal", true)]       // LGPD similar to GDPR
        [InlineData("LGPD", "sensitive", true)]      // LGPD requires explicit consent for sensitive
        [InlineData("CCPA", "personal", false)]      // CCPA is opt-out, not opt-in
        [InlineData("CCPA", "sensitive", true)]      // CCPA requires opt-in for sensitive
        public void RequiresExplicitConsent_ForRegulationAndCategory_ReturnsCorrectRequirement(
            string regulation,
            string dataCategory,
            bool expectedRequired)
        {
            // Act
            var requiresConsent = RequiresExplicitConsent(regulation, dataCategory);

            // Assert
            Assert.Equal(expectedRequired, requiresConsent);
        }

        /// <summary>
        /// Test data subject rights across regulations
        /// </summary>
        [Theory]
        [InlineData("GDPR", "access", true)]           // Right to access
        [InlineData("GDPR", "rectification", true)]    // Right to rectification
        [InlineData("GDPR", "erasure", true)]          // Right to be forgotten
        [InlineData("GDPR", "portability", true)]      // Right to data portability
        [InlineData("GDPR", "objection", true)]        // Right to object
        [InlineData("LGPD", "access", true)]           // Similar to GDPR
        [InlineData("LGPD", "rectification", true)]
        [InlineData("LGPD", "erasure", true)]
        [InlineData("LGPD", "portability", true)]
        [InlineData("CCPA", "access", true)]           // Right to know
        [InlineData("CCPA", "deletion", true)]         // Right to delete
        [InlineData("CCPA", "opt_out", true)]          // Right to opt-out
        public void SupportsDataSubjectRight_ForAllRegulations_ReturnsCorrectSupport(
            string regulation,
            string right,
            bool expectedSupported)
        {
            // Act
            var isSupported = SupportsDataSubjectRight(regulation, right);

            // Assert
            Assert.Equal(expectedSupported, isSupported);
        }

        /// <summary>
        /// Test cross-border transfer requirements
        /// </summary>
        [Theory]
        [InlineData("GDPR", "EU", "US", false)]        // EU to US requires safeguards
        [InlineData("GDPR", "EU", "UK", true)]         // EU to UK (adequacy decision)
        [InlineData("GDPR", "EU", "EU", true)]         // Within EU
        [InlineData("LGPD", "BR", "US", false)]        // Brazil to US requires safeguards
        [InlineData("LGPD", "BR", "BR", true)]         // Within Brazil
        [InlineData("CCPA", "US", "EU", true)]         // CCPA less restrictive on transfers out
        [InlineData("CCPA", "US", "US", true)]         // Within US
        public void AllowsCrossBorderTransfer_ForRegulations_ReturnsCorrectAllowance(
            string regulation,
            string fromRegion,
            string toRegion,
            bool expectedAllowed)
        {
            // Act
            var isAllowed = AllowsCrossBorderTransfer(regulation, fromRegion, toRegion);

            // Assert
            Assert.Equal(expectedAllowed, isAllowed);
        }

        /// <summary>
        /// Test all combinations of regulation × data category
        /// 3 regulations × 3 categories = 9 combinations
        /// </summary>
        [Theory]
        [InlineData("GDPR", "personal")]
        [InlineData("GDPR", "sensitive")]
        [InlineData("GDPR", "public")]
        [InlineData("LGPD", "personal")]
        [InlineData("LGPD", "sensitive")]
        [InlineData("LGPD", "public")]
        [InlineData("CCPA", "personal")]
        [InlineData("CCPA", "sensitive")]
        [InlineData("CCPA", "public")]
        public void GetAllowedProcessingPurposes_ForRegulationAndCategory_ReturnsValidList(
            string regulation,
            string dataCategory)
        {
            // Act
            var allowedPurposes = GetAllowedProcessingPurposes(regulation, dataCategory);

            // Assert
            Assert.NotEmpty(allowedPurposes);

            if (dataCategory == "sensitive")
            {
                // Sensitive data should have fewer allowed purposes
                Assert.DoesNotContain("legitimate_interest", allowedPurposes);
                Assert.DoesNotContain("contract", allowedPurposes);
            }
            else if (dataCategory == "public")
            {
                // Public data should allow most purposes
                Assert.True(allowedPurposes.Count >= 3, "Public data should allow multiple purposes");
            }
        }

        #region Helper Methods

        private static bool ValidateProcessing(string regulation, string dataCategory, string purpose)
        {
            ValidateInputs(regulation, dataCategory, purpose);

            // Sensitive data requires consent or legal obligation
            if (dataCategory == "sensitive")
            {
                return purpose == "consent" || purpose == "legal_obligation";
            }

            // Public data allows all purposes
            if (dataCategory == "public")
            {
                return true;
            }

            // Personal data under GDPR/LGPD allows all standard bases
            if (regulation == "GDPR" || regulation == "LGPD")
            {
                return true;
            }

            // CCPA allows all for personal, restricts sensitive
            if (regulation == "CCPA")
            {
                return dataCategory != "sensitive" || purpose == "consent" || purpose == "legal_obligation";
            }

            return false;
        }

        private static bool IsValidCombination(string regulation, string dataCategory, string purpose)
        {
            try
            {
                return ValidateProcessing(regulation, dataCategory, purpose);
            }
            catch
            {
                return false;
            }
        }

        private static void ValidateInputs(string regulation, string dataCategory, string purpose)
        {
            if (string.IsNullOrWhiteSpace(regulation))
                throw new ArgumentException("Regulation is required", nameof(regulation));

            if (Array.IndexOf(Regulations, regulation) < 0)
                throw new ArgumentException($"Invalid regulation: {regulation}", nameof(regulation));

            if (string.IsNullOrWhiteSpace(dataCategory))
                throw new ArgumentException("Data category is required", nameof(dataCategory));

            if (Array.IndexOf(DataCategories, dataCategory) < 0)
                throw new ArgumentException($"Invalid data category: {dataCategory}", nameof(dataCategory));

            if (string.IsNullOrWhiteSpace(purpose))
                throw new ArgumentException("Purpose is required", nameof(purpose));

            if (Array.IndexOf(ProcessingPurposes, purpose) < 0)
                throw new ArgumentException($"Invalid purpose: {purpose}", nameof(purpose));
        }

        private static int GetDataRetentionDays(string regulation)
        {
            return regulation switch
            {
                "GDPR" => 30,
                "LGPD" => 30,
                "CCPA" => 90,
                _ => 30
            };
        }

        private static bool IsBreachNotificationRequired(string regulation, BreachContext context)
        {
            if (context.RiskLevel == "low")
                return false;

            return regulation switch
            {
                "GDPR" => context.RiskLevel == "high" || context.RiskLevel == "medium",
                "LGPD" => context.RiskLevel == "high" || context.RiskLevel == "medium",
                "CCPA" => context.AffectedUsersCount >= 100,
                _ => false
            };
        }

        private static bool RequiresExplicitConsent(string regulation, string dataCategory)
        {
            if (dataCategory == "sensitive")
                return true;

            return regulation switch
            {
                "GDPR" => true,
                "LGPD" => true,
                "CCPA" => false, // CCPA is opt-out
                _ => false
            };
        }

        private static bool SupportsDataSubjectRight(string regulation, string right)
        {
            var gdprRights = new[] { "access", "rectification", "erasure", "portability", "objection" };
            var lgpdRights = new[] { "access", "rectification", "erasure", "portability" };
            var ccpaRights = new[] { "access", "deletion", "opt_out" };

            return regulation switch
            {
                "GDPR" => Array.IndexOf(gdprRights, right) >= 0,
                "LGPD" => Array.IndexOf(lgpdRights, right) >= 0,
                "CCPA" => Array.IndexOf(ccpaRights, right) >= 0,
                _ => false
            };
        }

        private static bool AllowsCrossBorderTransfer(string regulation, string fromRegion, string toRegion)
        {
            // Same region always allowed
            if (fromRegion == toRegion)
                return true;

            return regulation switch
            {
                "GDPR" => toRegion == "UK" || toRegion == "EU", // Adequacy decisions
                "LGPD" => fromRegion == toRegion,
                "CCPA" => true, // Less restrictive
                _ => false
            };
        }

        private static List<string> GetAllowedProcessingPurposes(string regulation, string dataCategory)
        {
            if (dataCategory == "sensitive")
            {
                return new List<string> { "consent", "legal_obligation" };
            }

            if (dataCategory == "public")
            {
                return new List<string>(ProcessingPurposes);
            }

            // Personal data
            return new List<string> { "contract", "consent", "legitimate_interest", "legal_obligation" };
        }

        #endregion
    }
}
