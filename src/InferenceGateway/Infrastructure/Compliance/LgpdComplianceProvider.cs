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
    using Synaxis.InferenceGateway.Infrastructure.Contracts;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;

    /// <summary>
    /// LGPD (Lei Geral de Proteção de Dados) compliance provider for Brazilian data protection.
    /// Enforces ANPD notification, 10 legal bases validation, and Brazilian data protection rules.
    /// </summary>
    public class LgpdComplianceProvider : IComplianceProvider
    {
        private readonly SynaxisDbContext _dbContext;

        // Brazilian regions for data residency validation
        private static readonly HashSet<string> BrazilianRegions = new ()
        {
            "sa-east-1", "br-south-1", "sa-saopaulo-1"
        };

        public LgpdComplianceProvider(SynaxisDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public string RegulationCode => "LGPD";

        public string Region => "BR";

        public async Task<bool> ValidateTransferAsync(TransferContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // 1. Check if transfer is within Brazil (no restrictions)
            if (IsWithinBrazil(context.FromRegion) && IsWithinBrazil(context.ToRegion))
                return true;

            // 2. Transfer FROM Brazil to outside requires specific safeguards (LGPD Art. 33)
            if (IsWithinBrazil(context.FromRegion) && !IsWithinBrazil(context.ToRegion))
            {
                // Check for international cooperation agreement (adequacy decision)
                // When there's adequacy, encryption is still required but purpose may be implicit
                if (context.LegalBasis?.Equals("adequacy", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Still require encryption even with adequacy
                    if (!context.EncryptionUsed)
                        return false;
                    return true;
                }

                // For all other legal bases, both encryption AND purpose are mandatory
                // Encryption is required for ALL international transfers
                if (!context.EncryptionUsed)
                    return false;

                // Purpose must be clearly defined for all international transfers
                if (string.IsNullOrWhiteSpace(context.Purpose))
                    return false;

                // Check for Standard Contractual Clauses
                if (context.LegalBasis?.Equals("SCC", StringComparison.OrdinalIgnoreCase) == true ||
                    context.LegalBasis?.Equals("contract", StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                // Check for explicit user consent (must be specific)
                if (context.UserConsentObtained &&
                    context.LegalBasis?.Equals("consent", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                // Reject if no valid legal basis
                return false;
            }

            // 3. Transfer TO Brazil from outside (allowed with logging)
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
                Action = "transferencia_internacional", // Portuguese for international transfer
                EntityType = "transferencia_dados",
                EntityId = context.UserId?.ToString(),
                NewValues = JsonSerializer.Serialize(new
                {
                    regiao_origem = context.FromRegion,
                    regiao_destino = context.ToRegion,
                    base_legal = context.LegalBasis,
                    finalidade = context.Purpose,
                    categorias_dados = context.DataCategories,
                    criptografia_utilizada = context.EncryptionUsed,
                    consentimento_obtido = context.UserConsentObtained,
                    regulamento = "LGPD"
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
                throw new InvalidOperationException($"Usuário {userId} não encontrado");

            userData["perfil"] = user;

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

            userData["vinculos_organizacao"] = memberships;

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

            userData["vinculos_grupo"] = groupMemberships;

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

            userData["chaves_api"] = apiKeys;

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

            userData["registros_auditoria"] = auditLogs;

            // Serialize to JSON
            var jsonData = JsonSerializer.Serialize(userData, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Support Portuguese characters
            });

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
                        vinculos_organizacao = memberships.Count,
                        vinculos_grupo = groupMemberships.Count,
                        chaves_api = apiKeys.Count,
                        registros_auditoria = auditLogs.Count
                    }
                }
            };
        }

        public async Task<bool> DeleteUserDataAsync(Guid userId)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                // Log the deletion request (before deletion) in Portuguese
                var deletionLog = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Action = "eliminacao_dados",
                    EntityType = "usuario",
                    EntityId = userId.ToString(),
                    NewValues = JsonSerializer.Serialize(new
                    {
                        regulamento = "LGPD",
                        direito = "direito_eliminacao",
                        artigo = "Artigo 18, VI",
                        data_hora = DateTime.UtcNow
                    }),
                    CreatedAt = DateTime.UtcNow,
                    PartitionDate = DateTime.UtcNow.Date
                };

                _dbContext.AuditLogs.Add(deletionLog);
                await _dbContext.SaveChangesAsync();

                // Delete user data (cascade will handle related records)
                // Note: Audit logs are kept for compliance with ANPD requirements

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
                    key.RevocationReason = "Solicitação de eliminação de dados (LGPD Artigo 18, VI)";
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

            // LGPD has 10 legal bases for processing (Article 7)
            var validLegalBases = new[]
            {
                "consent",                    // I - User consent
                "legal_obligation",           // II - Compliance with legal obligation
                "public_administration",      // III - Public administration
                "research",                   // IV - Studies and research
                "contract",                   // V - Contract execution
                "legal_proceedings", // VI - Exercise of rights in legal proceedings
                "life_protection", // VII - Protection of life or physical safety
                "health_protection", // VIII - Health protection
                "legitimate_interests", // IX - Legitimate interests (not for sensitive data)
                "credit_protection" // X - Credit protection
            };

            if (string.IsNullOrWhiteSpace(context.LegalBasis))
                return false;

            var normalizedBasis = context.LegalBasis.ToLowerInvariant().Replace("_", "").Replace("-", "");

            return validLegalBases.Any(basis =>
                normalizedBasis.Contains(basis.Replace("_", "").Replace("-", "")));
        }

        public int? GetDataRetentionDays()
        {
            // LGPD Article 15: Data should be kept only for as long as necessary
            // Return null to indicate "as necessary for the purpose"
            // Organizations must define retention based on specific purposes
            return null;
        }

        public async Task<bool> IsBreachNotificationRequiredAsync(BreachContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // LGPD Article 48: Notification to ANPD (Brazilian Authority) required for relevant incidents

            // Always notify ANPD for high-risk breaches
            if (context.RiskLevel?.Equals("high", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            // Notify for medium risk if significant number of users affected
            if (context.RiskLevel?.Equals("medium", StringComparison.OrdinalIgnoreCase) == true &&
                context.AffectedUsersCount >= 50) // Lower threshold than GDPR
                return true;

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
                "sexual_life_data"
            };

            if (context.DataCategoriesExposed?.Any(cat =>
                sensitiveCategories.Contains(cat, StringComparer.OrdinalIgnoreCase)) == true)
                return true;

            // For low risk breaches with few affected users, notification might not be required
            return false;
        }

        private bool IsWithinBrazil(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return false;

            // Check if region code indicates Brazil
            return region.StartsWith("sa-east", StringComparison.OrdinalIgnoreCase) ||
                   region.StartsWith("br-", StringComparison.OrdinalIgnoreCase) ||
                   BrazilianRegions.Contains(region.ToLowerInvariant());
        }
    }
}
