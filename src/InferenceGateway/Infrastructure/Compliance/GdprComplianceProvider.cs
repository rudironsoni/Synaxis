#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;

namespace Synaxis.InferenceGateway.Infrastructure.Compliance
{
    /// <summary>
    /// GDPR (General Data Protection Regulation) compliance provider for EU data protection.
    /// Enforces 72-hour breach notification, data residency in EU, and user rights (export, erasure).
    /// </summary>
    public class GdprComplianceProvider : IComplianceProvider
    {
        private readonly SynaxisDbContext _dbContext;
        private const int BreachNotificationHoursThreshold = 72;
        
        // EU regions for data residency validation
        private static readonly HashSet<string> EuRegions = new()
        {
            "eu-west-1", "eu-central-1", "eu-north-1", "eu-south-1"
        };
        
        // Adequate countries under GDPR (simplified list)
        private static readonly HashSet<string> AdequateCountries = new()
        {
            "EU", "UK", "CH", "NO", "IS", "LI", "NZ", "JP", "KR", "CA"
        };

        public GdprComplianceProvider(SynaxisDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public string RegulationCode => "GDPR";
        
        public string Region => "EU";

        public async Task<bool> ValidateTransferAsync(TransferContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // 1. Check if transfer is within EU (no restrictions)
            if (IsWithinEu(context.FromRegion) && IsWithinEu(context.ToRegion))
                return true;

            // 2. Transfer FROM EU to outside EU requires special safeguards
            if (IsWithinEu(context.FromRegion) && !IsWithinEu(context.ToRegion))
            {
                // Check if destination is an adequate country (no additional requirements)
                if (IsAdequateCountry(context.ToRegion))
                    return true;

                // For non-adequate countries, encryption and purpose are mandatory
                if (!context.EncryptionUsed)
                    return false;

                if (string.IsNullOrWhiteSpace(context.Purpose))
                    return false;

                // Check for Standard Contractual Clauses (SCC)
                if (context.LegalBasis?.Equals("SCC", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Check for explicit user consent
                if (context.UserConsentObtained && 
                    context.LegalBasis?.Equals("consent", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Reject if no valid legal basis
                return false;
            }

            // 3. Transfer TO EU from outside (generally allowed but should be logged)
            return true;
        }

        public async Task LogTransferAsync(TransferContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = context.OrganizationId,
                UserId = context.UserId,
                Action = "cross_border_transfer",
                EntityType = "data_transfer",
                EntityId = context.UserId?.ToString(),
                NewValues = JsonSerializer.Serialize(new
                {
                    context.FromRegion,
                    context.ToRegion,
                    context.LegalBasis,
                    context.Purpose,
                    context.DataCategories,
                    context.EncryptionUsed,
                    context.UserConsentObtained
                }),
                CreatedAt = DateTime.UtcNow,
                PartitionDate = DateTime.UtcNow.Date
            };

            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<DataExport> ExportUserDataAsync(Guid userId)
        {
            // Collect all user data from various tables
            var userData = new Dictionary<string, object>();

            // Get user profile
            var user = await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.PhoneNumber,
                    u.Status,
                })
                .FirstOrDefaultAsync();

            if (user == null)
                throw new InvalidOperationException($"User {userId} not found");

            userData["profile"] = user;

            // Get organization memberships
            var memberships = await _dbContext.UserOrganizationMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.OrganizationId,
                    m.OrganizationRole,
                    
                    m.Status
                })
                .ToListAsync();

            userData["organization_memberships"] = memberships;

            // Get group memberships
            var groupMemberships = await _dbContext.UserGroupMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.GroupId,
                    m.GroupRole,
                    m.JoinedAt
                })
                .ToListAsync();

            userData["group_memberships"] = groupMemberships;

            // Get API keys created by user
            var apiKeys = await _dbContext.ApiKeys
                .Where(k => k.CreatedBy == userId)
                .Select(k => new
                {
                    k.Id,
                    k.Name,
                    k.KeyPrefix,
                    k.CreatedAt,
                    k.ExpiresAt,
                    k.IsActive
                })
                .ToListAsync();

            userData["api_keys"] = apiKeys;

            // Get audit logs for this user
            var auditLogs = await _dbContext.AuditLogs
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(1000) // Limit to last 1000 entries
                .Select(a => new
                {
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.CreatedAt
                })
                .ToListAsync();

            userData["audit_logs"] = auditLogs;

            // Serialize to JSON
            var jsonData = JsonSerializer.Serialize(userData, new JsonSerializerOptions
            {
                WriteIndented = true
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
                        audit_logs = auditLogs.Count
                    }
                }
            };
        }

        public async Task<bool> DeleteUserDataAsync(Guid userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            
            try
            {
                // Log the deletion request (before deletion)
                var deletionLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = "data_erasure",
                    EntityType = "user",
                    EntityId = userId.ToString(),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        regulation = "GDPR",
                        right = "right_to_erasure",
                        article = "Article 17",
                        timestamp = DateTime.UtcNow
                    }),
                    CreatedAt = DateTime.UtcNow,
                    PartitionDate = DateTime.UtcNow.Date
                };
                
                _dbContext.AuditLogs.Add(deletionLog);
                await _dbContext.SaveChangesAsync();

                // Delete user data (cascade will handle related records)
                // Note: Audit logs are kept for compliance purposes (legitimate interest)
                
                // Delete group memberships
                var groupMemberships = await _dbContext.UserGroupMemberships
                    .Where(m => m.UserId == userId)
                    .ToListAsync();
                _dbContext.UserGroupMemberships.RemoveRange(groupMemberships);

                // Delete organization memberships
                var orgMemberships = await _dbContext.UserOrganizationMemberships
                    .Where(m => m.UserId == userId)
                    .ToListAsync();
                _dbContext.UserOrganizationMemberships.RemoveRange(orgMemberships);

                // Revoke API keys
                var apiKeys = await _dbContext.ApiKeys
                    .Where(k => k.CreatedBy == userId)
                    .ToListAsync();
                
                foreach (var key in apiKeys)
                {
                    key.IsActive = false;
                    key.RevokedAt = DateTime.UtcNow;
                    key.RevocationReason = "User data erasure request (GDPR Article 17)";
                }

                // Delete user account
                var user = await _dbContext.Users.FindAsync(userId);
                if (user != null)
                {
                    _dbContext.Users.Remove(user);
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> IsProcessingAllowedAsync(ProcessingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // GDPR requires one of six legal bases (Article 6)
            var validLegalBases = new[]
            {
                "consent",           // User has given clear consent
                "contract",          // Processing necessary for a contract
                "legal_obligation",  // Processing necessary to comply with law
                "vital_interests",   // Processing necessary to protect vital interests
                "public_task",       // Processing necessary for public interest
                "legitimate_interests" // Processing necessary for legitimate interests
            };

            if (string.IsNullOrWhiteSpace(context.LegalBasis))
                return false;

            return validLegalBases.Contains(context.LegalBasis.ToLowerInvariant());
        }

        public int? GetDataRetentionDays()
        {
            // GDPR doesn't specify exact retention period - depends on legal basis and purpose
            // Return null to indicate "as necessary" - organizations must define their own
            return null;
        }

        public async Task<bool> IsBreachNotificationRequiredAsync(BreachContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // GDPR Article 33: Notification required within 72 hours for high-risk breaches
            
            // Always notify for high-risk breaches
            if (context.RiskLevel?.Equals("high", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Notify for medium risk if significant number of users affected
            if (context.RiskLevel?.Equals("medium", StringComparison.OrdinalIgnoreCase) == true &&
                context.AffectedUsersCount >= 100)
                return true;

            // Check if sensitive data categories are exposed
            var sensitiveCategories = new[] 
            { 
                "health_data", 
                "biometric_data", 
                "genetic_data", 
                "financial_data",
                "political_opinions",
                "religious_beliefs",
                "trade_union_membership"
            };

            if (context.DataCategoriesExposed?.Any(cat => 
                sensitiveCategories.Contains(cat, StringComparer.OrdinalIgnoreCase)) == true)
                return true;

            // For low risk breaches, notification might not be required
            return false;
        }

        private bool IsWithinEu(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return false;

            // Check if region code starts with EU or is explicitly in EU regions list
            return region.StartsWith("eu-", StringComparison.OrdinalIgnoreCase) ||
                   EuRegions.Contains(region.ToLowerInvariant());
        }

        private bool IsAdequateCountry(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return false;

            // Extract country code from region (e.g., "uk-west-1" -> "UK")
            var countryCode = region.Split('-')[0].ToUpperInvariant();
            
            return AdequateCountries.Contains(countryCode);
        }
    }
}
