// <copyright file="GdprComplianceProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.Compliance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Infrastructure.Contracts;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// GDPR (General Data Protection Regulation) compliance provider for EU data protection.
    /// Enforces 72-hour breach notification, data residency in EU, and user rights (export, erasure).
    /// </summary>
    public class GdprComplianceProvider : IComplianceProvider
    {
        private const int BreachNotificationHoursThreshold = 72;

        // EU regions for data residency validation
        private static readonly HashSet<string> EuRegions = new()
        {
            "eu-west-1", "eu-central-1", "eu-north-1", "eu-south-1",
        };

        // Adequate countries under GDPR (simplified list)
        private static readonly HashSet<string> AdequateCountries = new()
        {
            "EU", "UK", "CH", "NO", "IS", "LI", "NZ", "JP", "KR", "CA",
        };

        private readonly ControlPlane.SynaxisDbContext _controlPlaneDbContext;
        private readonly Synaxis.Infrastructure.Data.SynaxisDbContext _auditDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="GdprComplianceProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneDbContext">The control plane database context.</param>
        /// <param name="auditDbContext">The audit database context.</param>
        public GdprComplianceProvider(
            ControlPlane.SynaxisDbContext controlPlaneDbContext,
            Synaxis.Infrastructure.Data.SynaxisDbContext auditDbContext)
        {
            this._controlPlaneDbContext = controlPlaneDbContext ?? throw new ArgumentNullException(nameof(controlPlaneDbContext));
            this._auditDbContext = auditDbContext ?? throw new ArgumentNullException(nameof(auditDbContext));
        }

        /// <inheritdoc/>
        public string RegulationCode => "GDPR";

        /// <inheritdoc/>
        public string Region => "EU";

        /// <inheritdoc/>
        public async Task<bool> ValidateTransferAsync(TransferContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // 1. Check if transfer is within EU (no restrictions)
            if (IsWithinEu(context.FromRegion) && IsWithinEu(context.ToRegion))
            {
                return true;
            }

            // 2. Transfer FROM EU to outside EU requires special safeguards
            if (IsWithinEu(context.FromRegion) && !IsWithinEu(context.ToRegion))
            {
                // Check if destination is an adequate country (no additional requirements)
                if (IsAdequateCountry(context.ToRegion))
                {
                    return true;
                }

                // For non-adequate countries, encryption and purpose are mandatory
                if (!context.EncryptionUsed)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(context.Purpose))
                {
                    return false;
                }

                // Check for Standard Contractual Clauses (SCC)
                if (context.LegalBasis?.Equals("SCC", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                // Check for explicit user consent
                if (context.UserConsentObtained &&
                    context.LegalBasis?.Equals("consent", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                // Reject if no valid legal basis
                return false;
            }

            // 3. Transfer TO EU from outside (generally allowed but should be logged)
            return true;
        }

        /// <inheritdoc/>
        public Task LogTransferAsync(TransferContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var auditLog = new Synaxis.Core.Models.AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = context.OrganizationId,
                UserId = context.UserId,
                EventType = "cross_border_transfer",
                EventCategory = "compliance",
                Action = "cross_border_transfer",
                ResourceType = "data_transfer",
                ResourceId = context.UserId?.ToString() ?? string.Empty,
                Metadata = new Dictionary<string, object>
                {
                    { "fromRegion", context.FromRegion },
                    { "toRegion", context.ToRegion },
                    { "legalBasis", context.LegalBasis },
                    { "purpose", context.Purpose },
                    { "dataCategories", context.DataCategories },
                    { "encryptionUsed", context.EncryptionUsed },
                    { "userConsentObtained", context.UserConsentObtained },
                },
                IpAddress = string.Empty,
                UserAgent = string.Empty,
                Region = context.FromRegion ?? "unknown",
                IntegrityHash = string.Empty,
                PreviousHash = string.Empty,
                Timestamp = DateTime.UtcNow,
            };

            this._auditDbContext.AuditLogs.Add(auditLog);
            return this._auditDbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task<DataExport> ExportUserDataAsync(Guid userId)
        {
            // Collect all user data from various tables
            var userData = new Dictionary<string, object>();

            // Get user profile
            var user = await this.GetUserProfileAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found");
            }

            userData["profile"] = user;

            // Get all related data
            var memberships = await this.GetOrganizationMembershipsAsync(userId).ConfigureAwait(false);
            userData["organization_memberships"] = memberships;

            var groupMemberships = await this.GetGroupMembershipsAsync(userId).ConfigureAwait(false);
            userData["group_memberships"] = groupMemberships;

            var apiKeys = await this.GetUserApiKeysAsync(userId).ConfigureAwait(false);
            userData["api_keys"] = apiKeys;

            var auditLogs = await this.GetUserAuditLogsAsync(userId).ConfigureAwait(false);
            userData["audit_logs"] = auditLogs;

            // Serialize to JSON
            var jsonData = JsonSerializer.Serialize(userData, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            return new DataExport
            {
                UserId = userId,
                Format = "json",
                Data = Encoding.UTF8.GetBytes(jsonData),
                ExportedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["regulation"] = "GDPR",
                    ["right"] = "data_portability",
                    ["article"] = "Article 20",
                    ["record_count"] = new
                    {
                        memberships = memberships.Count,
                        group_memberships = groupMemberships.Count,
                        api_keys = apiKeys.Count,
                        audit_logs = auditLogs.Count,
                    },
                },
            };
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUserDataAsync(Guid userId)
        {
            using var transaction = await this._controlPlaneDbContext.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                // Log the deletion request (before deletion)
                await this.LogDataErasureAsync(userId).ConfigureAwait(false);

                // Delete user data (cascade will handle related records)
                // Note: Audit logs are kept for compliance purposes (legitimate interest)
                await this.DeleteUserMembershipsAsync(userId).ConfigureAwait(false);
                await this.RevokeUserApiKeysAsync(userId).ConfigureAwait(false);
                await this.DeleteUserAccountAsync(userId).ConfigureAwait(false);

                await this._controlPlaneDbContext.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                this._logger.LogError(ex, "Failed to delete user data for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsProcessingAllowedAsync(ProcessingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // GDPR requires one of six legal bases (Article 6)
            var validLegalBases = new[]
            {
                "consent",           // User has given clear consent
                "contract",          // Processing necessary for a contract
                "legal_obligation",  // Processing necessary to comply with law
                "vital_interests",   // Processing necessary to protect vital interests
                "public_task",       // Processing necessary for public interest
                "legitimate_interests", // Processing necessary for legitimate interests
            };

            if (string.IsNullOrWhiteSpace(context.LegalBasis))
            {
                return false;
            }

            return validLegalBases.Contains(context.LegalBasis.ToLowerInvariant());
        }

        /// <inheritdoc/>
        public int? GetDataRetentionDays()
        {
            // GDPR doesn't specify exact retention period - depends on legal basis and purpose
            // Return null to indicate "as necessary" - organizations must define their own
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> IsBreachNotificationRequiredAsync(BreachContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // GDPR Article 33: Notification required within 72 hours for high-risk breaches

            // Always notify for high-risk breaches
            if (context.RiskLevel?.Equals("high", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Notify for medium risk if significant number of users affected
            if (context.RiskLevel?.Equals("medium", StringComparison.OrdinalIgnoreCase) == true &&
                context.AffectedUsersCount >= 100)
            {
                return true;
            }

            // Check if sensitive data categories are exposed
            var sensitiveCategories = new[]
            {
                "health_data",
                "biometric_data",
                "genetic_data",
                "financial_data",
                "political_opinions",
                "religious_beliefs",
                "trade_union_membership",
            };

            if (context.DataCategoriesExposed?.Any(cat =>
                sensitiveCategories.Contains(cat, StringComparer.OrdinalIgnoreCase)) == true)
            {
                return true;
            }

            // For low risk breaches, notification might not be required
            return false;
        }

        private async Task<object> GetUserProfileAsync(Guid userId)
        {
            return await this._controlPlaneDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Status,
                })
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }

        private async Task<List<object>> GetOrganizationMembershipsAsync(Guid userId)
        {
            var memberships = await this._controlPlaneDbContext.UserOrganizationMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.OrganizationId,
                    m.OrganizationRole,
                    m.Status,
                })
                .ToListAsync().ConfigureAwait(false);

            return memberships.Cast<object>().ToList();
        }

        private async Task<List<object>> GetGroupMembershipsAsync(Guid userId)
        {
            var groupMemberships = await this._controlPlaneDbContext.UserGroupMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.GroupId,
                    m.GroupRole,
                    m.JoinedAt,
                })
                .ToListAsync().ConfigureAwait(false);

            return groupMemberships.Cast<object>().ToList();
        }

        private async Task<List<object>> GetUserApiKeysAsync(Guid userId)
        {
            var apiKeys = await this._controlPlaneDbContext.ApiKeys
                .Where(k => k.CreatedBy == userId)
                .Select(k => new
                {
                    k.Id,
                    k.Name,
                    k.KeyPrefix,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.IsActive,
                })
                .ToListAsync().ConfigureAwait(false);

            return apiKeys.Cast<object>().ToList();
        }

        private async Task<List<object>> GetUserAuditLogsAsync(Guid userId)
        {
            var auditLogs = await this._auditDbContext.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.Timestamp)
                .Take(1000) // Limit to last 1000 entries
                .Select(a => new
                {
                    a.Action,
                    a.EventType,
                    a.EventCategory,
                    a.ResourceType,
                    a.ResourceId,
                    a.Timestamp,
                })
                .ToListAsync().ConfigureAwait(false);

            return auditLogs.Cast<object>().ToList();
        }

        private Task LogDataErasureAsync(Guid userId)
        {
            var deletionLog = new Synaxis.Core.Models.AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.Empty, // System-level event
                UserId = userId,
                EventType = "data_erasure",
                EventCategory = "compliance",
                Action = "data_erasure",
                ResourceType = "user",
                ResourceId = userId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "regulation", "GDPR" },
                    { "right", "right_to_erasure" },
                    { "article", "Article 17" },
                    { "timestamp", DateTime.UtcNow },
                },
                IpAddress = string.Empty,
                UserAgent = string.Empty,
                Region = "unknown",
                IntegrityHash = string.Empty,
                PreviousHash = string.Empty,
                Timestamp = DateTime.UtcNow,
            };

            this._auditDbContext.AuditLogs.Add(deletionLog);
            return this._auditDbContext.SaveChangesAsync();
        }

        private async Task DeleteUserMembershipsAsync(Guid userId)
        {
            // Delete group memberships
            var groupMemberships = await this._controlPlaneDbContext.UserGroupMemberships
                .Where(m => m.UserId == userId)
                .ToListAsync().ConfigureAwait(false);
            this._controlPlaneDbContext.UserGroupMemberships.RemoveRange(groupMemberships);

            // Delete organization memberships
            var orgMemberships = await this._controlPlaneDbContext.UserOrganizationMemberships
                .Where(m => m.UserId == userId)
                .ToListAsync().ConfigureAwait(false);
            this._controlPlaneDbContext.UserOrganizationMemberships.RemoveRange(orgMemberships);
        }

        private async Task RevokeUserApiKeysAsync(Guid userId)
        {
            var apiKeys = await this._controlPlaneDbContext.ApiKeys
                .Where(k => k.CreatedBy == userId)
                .ToListAsync().ConfigureAwait(false);

            foreach (var key in apiKeys)
            {
                key.IsActive = false;
                key.RevokedAt = DateTime.UtcNow;
                key.RevocationReason = "User data erasure request (GDPR Article 17)";
            }
        }

        private async Task DeleteUserAccountAsync(Guid userId)
        {
            var user = await this._controlPlaneDbContext.Users.FindAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                this._controlPlaneDbContext.Users.Remove(user);
            }
        }

        private static bool IsWithinEu(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return false;
            }

            // Check if region code starts with EU or is explicitly in EU regions list
            return region.StartsWith("eu-", StringComparison.OrdinalIgnoreCase) ||
                   EuRegions.Contains(region.ToLowerInvariant());
        }

        private static bool IsAdequateCountry(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return false;
            }

            // Extract country code from region (e.g., "uk-west-1" -> "UK")
            var countryCode = region.Split('-')[0].ToUpperInvariant();

            return AdequateCountries.Contains(countryCode);
        }
    }
}
