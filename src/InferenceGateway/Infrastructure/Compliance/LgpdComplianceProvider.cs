// <copyright file="LgpdComplianceProvider.cs" company="PlaceholderCompany">
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
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Infrastructure.Contracts;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// LGPD (Lei Geral de Proteção de Dados) compliance provider for Brazilian data protection.
    /// Enforces ANPD notification, 10 legal bases validation, and Brazilian data protection rules.
    /// </summary>
    public class LgpdComplianceProvider : IComplianceProvider
    {
        private readonly Synaxis.Infrastructure.Data.SynaxisDbContext _auditDbContext;
        private readonly Synaxis.InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext _controlPlaneDbContext;

        // Brazilian regions for data residency validation
        private static readonly HashSet<string> BrazilianRegions = new()
        {
            "sa-east-1", "br-south-1", "sa-saopaulo-1",
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="LgpdComplianceProvider"/> class.
        /// </summary>
        /// <param name="auditDbContext">The audit database context.</param>
        /// <param name="controlPlaneDbContext">The control plane database context.</param>
        public LgpdComplianceProvider(
            Synaxis.Infrastructure.Data.SynaxisDbContext auditDbContext,
            Synaxis.InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext controlPlaneDbContext)
        {
            this._auditDbContext = auditDbContext ?? throw new ArgumentNullException(nameof(auditDbContext));
            this._controlPlaneDbContext = controlPlaneDbContext ?? throw new ArgumentNullException(nameof(controlPlaneDbContext));
        }

        /// <inheritdoc/>
        public string RegulationCode => "LGPD";

        /// <inheritdoc/>
        public string Region => "BR";

        /// <inheritdoc/>
        public async Task<bool> ValidateTransferAsync(TransferContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // 1. Check if transfer is within Brazil (no restrictions)
            if (IsWithinBrazil(context.FromRegion) && IsWithinBrazil(context.ToRegion))
            {
                return true;
            }

            // 2. Transfer FROM Brazil to outside requires specific safeguards (LGPD Art. 33)
            if (IsWithinBrazil(context.FromRegion) && !IsWithinBrazil(context.ToRegion))
            {
                return ValidateInternationalTransfer(context);
            }

            // 3. Transfer TO Brazil from outside (allowed with logging)
            return true;
        }

        private static bool ValidateInternationalTransfer(TransferContext context)
        {
            // Check for international cooperation agreement (adequacy decision)
            if (context.LegalBasis?.Equals("adequacy", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ValidateAdequacyTransfer(context);
            }

            // For all other legal bases, both encryption AND purpose are mandatory
            if (!context.EncryptionUsed || string.IsNullOrWhiteSpace(context.Purpose))
            {
                return false;
            }

            return ValidateLegalBasisForTransfer(context);
        }

        private static bool ValidateAdequacyTransfer(TransferContext context)
        {
            // Still require encryption even with adequacy
            return context.EncryptionUsed;
        }

        private static bool ValidateLegalBasisForTransfer(TransferContext context)
        {
            // Check for Standard Contractual Clauses
            if (context.LegalBasis?.Equals("SCC", StringComparison.OrdinalIgnoreCase) == true ||
                context.LegalBasis?.Equals("contract", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Check for explicit user consent (must be specific)
            if (context.UserConsentObtained &&
                context.LegalBasis?.Equals("consent", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Reject if no valid legal basis
            return false;
        }

        /// <inheritdoc/>
        public Task LogTransferAsync(TransferContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = context.OrganizationId,
                UserId = context.UserId,
                EventType = "transferencia_internacional", // Portuguese for international transfer
                EventCategory = "compliance",
                Action = "transferencia_internacional",
                ResourceType = "transferencia_dados",
                ResourceId = context.UserId?.ToString() ?? string.Empty,
                Metadata = new Dictionary<string, object>
                {
                    { "regiao_origem", context.FromRegion },
                    { "regiao_destino", context.ToRegion },
                    { "base_legal", context.LegalBasis },
                    { "finalidade", context.Purpose },
                    { "categorias_dados", context.DataCategories },
                    { "criptografia_utilizada", context.EncryptionUsed },
                    { "consentimento_obtido", context.UserConsentObtained },
                    { "regulamento", "LGPD" },
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
            var userData = await this.CollectUserDataAsync(userId).ConfigureAwait(false);
            var jsonData = this.SerializeUserData(userData);

            return this.CreateDataExport(userId, jsonData, userData);
        }

        private async Task<Dictionary<string, object>> CollectUserDataAsync(Guid userId)
        {
            var userData = new Dictionary<string, object>();

            // Get user profile
            var user = await this.GetUserProfileAsync(userId).ConfigureAwait(false);
            userData["perfil"] = user;

            // Get organization memberships
            userData["vinculos_organizacao"] = await this.GetOrganizationMembershipsAsync(userId).ConfigureAwait(false);

            // Get group memberships
            userData["vinculos_grupo"] = await this.GetGroupMembershipsAsync(userId).ConfigureAwait(false);

            // Get API keys created by user
            userData["chaves_api"] = await this.GetApiKeysAsync(userId).ConfigureAwait(false);

            // Get audit logs for this user
            userData["registros_auditoria"] = await this.GetAuditLogsAsync(userId).ConfigureAwait(false);

            return userData;
        }

        private async Task<object> GetUserProfileAsync(Guid userId)
        {
            var user = await this._controlPlaneDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                })
                .FirstOrDefaultAsync().ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"Usuário {userId} não encontrado");
            }

            return user;
        }

        private async Task<object> GetOrganizationMembershipsAsync(Guid userId)
        {
            return await this._controlPlaneDbContext.UserOrganizationMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.OrganizationId,
                    m.OrganizationRole,
                    m.Status,
                })
                .ToListAsync().ConfigureAwait(false);
        }

        private async Task<object> GetGroupMembershipsAsync(Guid userId)
        {
            return await this._controlPlaneDbContext.UserGroupMemberships
                .Where(m => m.UserId == userId)
                .Select(m => new
                {
                    m.GroupId,
                    m.GroupRole,
                    m.JoinedAt,
                })
                .ToListAsync().ConfigureAwait(false);
        }

        private async Task<object> GetApiKeysAsync(Guid userId)
        {
            return await this._controlPlaneDbContext.ApiKeys
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
        }

        private async Task<object> GetAuditLogsAsync(Guid userId)
        {
            return await this._auditDbContext.AuditLogs
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
        }

        private string SerializeUserData(Dictionary<string, object> userData)
        {
            return JsonSerializer.Serialize(userData, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Support Portuguese characters
            });
        }

        private DataExport CreateDataExport(Guid userId, string jsonData, Dictionary<string, object> userData)
        {
            var memberships = userData["vinculos_organizacao"] as System.Collections.ICollection;
            var groupMemberships = userData["vinculos_grupo"] as System.Collections.ICollection;
            var apiKeys = userData["chaves_api"] as System.Collections.ICollection;
            var auditLogs = userData["registros_auditoria"] as System.Collections.ICollection;

            return new DataExport
            {
                UserId = userId,
                Format = "json",
                Data = Encoding.UTF8.GetBytes(jsonData),
                ExportedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["regulamento"] = "LGPD",
                    ["direito"] = "portabilidade_dados",
                    ["artigo"] = "Artigo 18, V",
                    ["idioma"] = "pt-BR",
                    ["contagem_registros"] = new
                    {
                        vinculos_organizacao = memberships?.Count ?? 0,
                        vinculos_grupo = groupMemberships?.Count ?? 0,
                        chaves_api = apiKeys?.Count ?? 0,
                        registros_auditoria = auditLogs?.Count ?? 0,
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
                await this.LogDeletionRequestAsync(userId).ConfigureAwait(false);
                await this.DeleteUserRelatedDataAsync(userId).ConfigureAwait(false);
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

        private Task LogDeletionRequestAsync(Guid userId)
        {
            var deletionLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.Empty, // System-level event
                UserId = userId,
                EventType = "eliminacao_dados",
                EventCategory = "compliance",
                Action = "eliminacao_dados",
                ResourceType = "usuario",
                ResourceId = userId.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "regulamento", "LGPD" },
                    { "direito", "direito_eliminacao" },
                    { "artigo", "Artigo 18, VI" },
                    { "data_hora", DateTime.UtcNow },
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

        private async Task DeleteUserRelatedDataAsync(Guid userId)
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

            // Revoke API keys
            var apiKeys = await this._controlPlaneDbContext.ApiKeys
                .Where(k => k.CreatedBy == userId)
                .ToListAsync().ConfigureAwait(false);

            foreach (var key in apiKeys)
            {
                key.IsActive = false;
                key.RevokedAt = DateTime.UtcNow;
                key.RevocationReason = "Solicitação de eliminação de dados (LGPD Artigo 18, VI)";
            }

            // Delete user account
            var user = await this._controlPlaneDbContext.Users.FindAsync(userId).ConfigureAwait(false);
            if (user != null)
            {
                this._controlPlaneDbContext.Users.Remove(user);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsProcessingAllowedAsync(ProcessingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // LGPD has 10 legal bases for processing (Article 7)
            var validLegalBases = new[]
            {
                "consent",                    // I - User consent
                "legal_obligation",           // II - Compliance with legal obligation
                "public_administration",      // III - Public administration
                "research",                   // IV - Studies and research
                "contract",                   // V - Contract execution
                "legal_proceedings",          // VI - Exercise of rights in legal proceedings
                "life_protection",            // VII - Protection of life or physical safety
                "health_protection",          // VIII - Health protection
                "legitimate_interests",      // IX - Legitimate interests (not for sensitive data)
                "credit_protection",          // X - Credit protection
            };

            if (string.IsNullOrWhiteSpace(context.LegalBasis))
            {
                return false;
            }

            var normalizedBasis = context.LegalBasis.ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty);

            return validLegalBases.Any(basis =>
                normalizedBasis.Contains(basis.Replace("_", string.Empty).Replace("-", string.Empty)));
        }

        /// <summary>
        /// Gets the data retention period in days as defined by LGPD regulations.
        /// </summary>
        /// <returns>
        /// Returns null to indicate data should be kept only as necessary for the purpose.
        /// Organizations must define retention based on specific purposes per LGPD Article 15.
        /// </returns>
        public int? GetDataRetentionDays()
        {
            // LGPD Article 15: Data should be kept only for as long as necessary
            // Return null to indicate "as necessary for the purpose"
            // Organizations must define retention based on specific purposes
            return null;
        }

        /// <inheritdoc/>
        public async Task<bool> IsBreachNotificationRequiredAsync(BreachContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // LGPD Article 48: Notification to ANPD (Brazilian Authority) required for relevant incidents

            // Always notify ANPD for high-risk breaches
            if (context.RiskLevel?.Equals("high", StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }

            // Notify for medium risk if significant number of users affected
            // Lower threshold than GDPR (50 users)
            if (context.RiskLevel?.Equals("medium", StringComparison.OrdinalIgnoreCase) == true &&
                context.AffectedUsersCount >= 50)
            {
                return true;
            }

            // Check if sensitive data categories are exposed (LGPD Article 5, II)
            var sensitiveCategories = new[]
            {
                "racial_ethnic_origin",
                "religious_belief",
                "political_opinion",
                "union_membership",
                "philosophical_belief",
                "health_data",
                "genetic_data",
                "biometric_data",
                "sexual_life_data",
            };

            if (context.DataCategoriesExposed?.Any(cat =>
                sensitiveCategories.Contains(cat, StringComparer.OrdinalIgnoreCase)) == true)
            {
                return true;
            }

            // For low risk breaches with few affected users, notification might not be required
            return false;
        }

        private static bool IsWithinBrazil(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return false;
            }

            // Check if region code indicates Brazil
            return region.StartsWith("sa-east", StringComparison.OrdinalIgnoreCase) ||
                   region.StartsWith("br-", StringComparison.OrdinalIgnoreCase) ||
                   BrazilianRegions.Contains(region.ToLowerInvariant());
        }
    }
}
