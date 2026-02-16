// <copyright file="SynaxisDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Synaxis.Core.Models;

    /// <summary>
    /// Database context for Synaxis multi-tenant platform.
    /// </summary>
    public class SynaxisDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisDbContext"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public SynaxisDbContext(DbContextOptions<SynaxisDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the organizations.
        /// </summary>
        public DbSet<Organization> Organizations { get; set; }

        /// <summary>
        /// Gets or sets the teams.
        /// </summary>
        public DbSet<Team> Teams { get; set; }

        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the team memberships.
        /// </summary>
        public DbSet<TeamMembership> TeamMemberships { get; set; }

        /// <summary>
        /// Gets or sets the virtual keys.
        /// </summary>
        public DbSet<VirtualKey> VirtualKeys { get; set; }

        /// <summary>
        /// Gets or sets the requests.
        /// </summary>
        public DbSet<Request> Requests { get; set; }

        /// <summary>
        /// Gets or sets the subscription plans.
        /// </summary>
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        /// <summary>
        /// Gets or sets the backup configurations.
        /// </summary>
        public DbSet<BackupConfig> BackupConfigs { get; set; }

        /// <summary>
        /// Gets or sets the audit logs.
        /// </summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        /// <summary>
        /// Gets or sets the spend logs.
        /// </summary>
        public DbSet<SpendLog> SpendLogs { get; set; }

        /// <summary>
        /// Gets or sets the credit transactions.
        /// </summary>
        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        /// <summary>
        /// Gets or sets the invoices.
        /// </summary>
        public DbSet<Invoice> Invoices { get; set; }

        /// <summary>
        /// Gets or sets the invitations.
        /// </summary>
        public DbSet<Invitation> Invitations { get; set; }

        /// <summary>
        /// Gets or sets the conversations.
        /// </summary>
        public DbSet<Conversation> Conversations { get; set; }

        /// <summary>
        /// Gets or sets the conversation turns.
        /// </summary>
        public DbSet<ConversationTurn> ConversationTurns { get; set; }

        /// <summary>
        /// Gets or sets the refresh tokens.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        /// <summary>
        /// Gets or sets the JWT blacklists.
        /// </summary>
        public DbSet<JwtBlacklist> JwtBlacklists { get; set; }

        /// <summary>
        /// Gets or sets the password histories.
        /// </summary>
        public DbSet<PasswordHistory> PasswordHistories { get; set; }

        /// <summary>
        /// Gets or sets the password policies.
        /// </summary>
        public DbSet<PasswordPolicy> PasswordPolicies { get; set; }

        /// <summary>
        /// Gets or sets the email verification tokens.
        /// </summary>
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        /// <summary>
        /// Gets or sets the password reset tokens.
        /// </summary>
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        /// <summary>
        /// Gets or sets the collections.
        /// </summary>
        public DbSet<Collection> Collections { get; set; }

        /// <summary>
        /// Gets or sets the collection memberships.
        /// </summary>
        public DbSet<CollectionMembership> CollectionMemberships { get; set; }

        /// <summary>
        /// Gets or sets the organization API keys.
        /// </summary>
        public DbSet<OrganizationApiKey> OrganizationApiKeys { get; set; }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureTableNames(modelBuilder);
            ConfigureOrganization(modelBuilder);
            ConfigureTeam(modelBuilder);
            ConfigureUser(modelBuilder);
            ConfigureTeamMembership(modelBuilder);
            ConfigureVirtualKey(modelBuilder);
            ConfigureRequest(modelBuilder);
            ConfigureBackupConfig(modelBuilder);
            ConfigureAuditLog(modelBuilder);
            ConfigureSpendLog(modelBuilder);
            ConfigureCreditTransaction(modelBuilder);
            ConfigureInvoice(modelBuilder);
            ConfigureSubscriptionPlan(modelBuilder);
            ConfigureInvitation(modelBuilder);
            ConfigureConversation(modelBuilder);
            ConfigureConversationTurn(modelBuilder);
            ConfigureRefreshToken(modelBuilder);
            ConfigureJwtBlacklist(modelBuilder);
            ConfigurePasswordHistory(modelBuilder);
            ConfigurePasswordPolicy(modelBuilder);
        }

        private static void ConfigureTableNames(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>().ToTable("organizations");
            modelBuilder.Entity<Team>().ToTable("teams");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<TeamMembership>().ToTable("team_memberships");
            modelBuilder.Entity<VirtualKey>().ToTable("virtual_keys");
            modelBuilder.Entity<Request>().ToTable("requests");
            modelBuilder.Entity<SubscriptionPlan>().ToTable("subscription_plans");
            modelBuilder.Entity<BackupConfig>().ToTable("organization_backup_config");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            modelBuilder.Entity<SpendLog>().ToTable("spend_logs");
            modelBuilder.Entity<CreditTransaction>().ToTable("credit_transactions");
            modelBuilder.Entity<Invoice>().ToTable("invoices");
            modelBuilder.Entity<Invitation>().ToTable("invitations");
            modelBuilder.Entity<Conversation>().ToTable("conversations");
            modelBuilder.Entity<ConversationTurn>().ToTable("conversation_turns");
            modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
            modelBuilder.Entity<JwtBlacklist>().ToTable("jwt_blacklists");
            modelBuilder.Entity<PasswordHistory>().ToTable("password_histories");
            modelBuilder.Entity<PasswordPolicy>().ToTable("password_policies");
        }

        private static void ConfigureOrganization(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.PrimaryRegion).HasColumnName("primary_region");
                entity.Property(e => e.Tier).HasColumnName("tier");
                entity.Property(e => e.BillingCurrency).HasColumnName("billing_currency");
                entity.Property(e => e.CreditBalance).HasColumnName("credit_balance");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsVerified).HasColumnName("is_verified");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.PrivacyConsent)
                    .HasColumnName("privacy_consent")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.Tier);
                entity.HasIndex(e => e.PrimaryRegion);

                entity.ToTable(t => t.HasCheckConstraint("CK_Organization_Slug_Lowercase", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));
            });
        }

        private static void ConfigureTeam(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Team>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.MonthlyBudget).HasColumnName("monthly_budget");
                entity.Property(e => e.BudgetAlertThreshold).HasColumnName("budget_alert_threshold");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Teams)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.OrganizationId, e.Slug }).IsUnique();
                entity.HasIndex(e => new { e.OrganizationId, e.Name });

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Team_MonthlyBudget_NonNegative", "monthly_budget IS NULL OR monthly_budget >= 0");
                    t.HasCheckConstraint("CK_Team_BudgetAlertThreshold_Range", "budget_alert_threshold >= 0 AND budget_alert_threshold <= 100");
                });
            });
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.EmailVerifiedAt).HasColumnName("email_verified_at");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.DataResidencyRegion).HasColumnName("data_residency_region");
                entity.Property(e => e.CreatedInRegion).HasColumnName("created_in_region");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
                entity.Property(e => e.Timezone).HasColumnName("timezone");
                entity.Property(e => e.Locale).HasColumnName("locale");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.CrossBorderConsentGiven).HasColumnName("cross_border_consent_given");
                entity.Property(e => e.CrossBorderConsentDate).HasColumnName("cross_border_consent_date");
                entity.Property(e => e.CrossBorderConsentVersion).HasColumnName("cross_border_consent_version");
                entity.Property(e => e.MfaEnabled).HasColumnName("mfa_enabled");
                entity.Property(e => e.MfaSecret).HasColumnName("mfa_secret");
                entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
                entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts");
                entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
                entity.Property(e => e.PasswordExpiresAt).HasColumnName("password_expires_at");
                entity.Property(e => e.FailedPasswordChangeAttempts).HasColumnName("failed_password_change_attempts");
                entity.Property(e => e.PasswordChangeLockedUntil).HasColumnName("password_change_locked_until");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.PrivacyConsent)
                    .HasColumnName("privacy_consent")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.DataResidencyRegion);
            });
        }

        private static void ConfigureTeamMembership(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TeamMembership>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
                entity.Property(e => e.InvitedBy).HasColumnName("invited_by");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.TeamMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Team)
                    .WithMany(t => t.TeamMemberships)
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.TeamId }).IsUnique();
                entity.HasIndex(e => new { e.OrganizationId, e.UserId });
                entity.HasIndex(e => new { e.TeamId, e.UserId });

                entity.ToTable(t => t.HasCheckConstraint("CK_TeamMembership_Role_Valid", "role IN ('OrgAdmin', 'TeamAdmin', 'Member', 'Viewer')"));
            });
        }

        private static void ConfigureVirtualKey(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VirtualKey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.KeyHash).HasColumnName("key_hash");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.RevokedReason).HasColumnName("revoked_reason");
                entity.Property(e => e.MaxBudget).HasColumnName("max_budget");
                entity.Property(e => e.CurrentSpend).HasColumnName("current_spend");
                entity.Property(e => e.RpmLimit).HasColumnName("rpm_limit");
                entity.Property(e => e.TpmLimit).HasColumnName("tpm_limit");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.UserRegion).HasColumnName("user_region");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                ConfigureVirtualKeyMetadataJson(entity);

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.VirtualKeys)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Team)
                    .WithMany(t => t.VirtualKeys)
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.KeyHash).IsUnique();
                entity.HasIndex(e => new { e.OrganizationId, e.TeamId });
                entity.HasIndex(e => new { e.OrganizationId, e.Name });

                ConfigureVirtualKeyCheckConstraints(entity);
            });
        }

        private static void ConfigureVirtualKeyMetadataJson(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<VirtualKey> entity)
        {
            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                    new ValueComparer<Dictionary<string, object>>(
                        (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                        c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                .HasColumnType("jsonb");
        }

        private static void ConfigureVirtualKeyCheckConstraints(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<VirtualKey> entity)
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_VirtualKey_MaxBudget_NonNegative", "max_budget IS NULL OR max_budget >= 0");
                t.HasCheckConstraint("CK_VirtualKey_CurrentSpend_NonNegative", "current_spend >= 0");
            });
        }

        private static void ConfigureRequest(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                ConfigureRequestProperties(entity);
                ConfigureRequestHeadersJsonColumn(entity);
                ConfigureRequestRelationships(entity);
                ConfigureRequestIndexes(entity);
            });
        }

        private static void ConfigureRequestProperties(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Request> entity)
        {
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VirtualKeyId).HasColumnName("virtual_key_id");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UserRegion).HasColumnName("user_region");
            entity.Property(e => e.ProcessedRegion).HasColumnName("processed_region");
            entity.Property(e => e.StoredRegion).HasColumnName("stored_region");
            entity.Property(e => e.CrossBorderTransfer).HasColumnName("cross_border_transfer");
            entity.Property(e => e.TransferLegalBasis).HasColumnName("transfer_legal_basis");
            entity.Property(e => e.TransferPurpose).HasColumnName("transfer_purpose");
            entity.Property(e => e.TransferTimestamp).HasColumnName("transfer_timestamp");
            entity.Property(e => e.Model).HasColumnName("model");
            entity.Property(e => e.Provider).HasColumnName("provider");
            entity.Property(e => e.InputTokens).HasColumnName("input_tokens");
            entity.Property(e => e.OutputTokens).HasColumnName("output_tokens");
            entity.Property(e => e.Cost).HasColumnName("cost").HasColumnType("decimal(18,8)");
            entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
            entity.Property(e => e.QueueTimeMs).HasColumnName("queue_time_ms");
            entity.Property(e => e.RequestSizeBytes).HasColumnName("request_size_bytes");
            entity.Property(e => e.ResponseSizeBytes).HasColumnName("response_size_bytes");
            entity.Property(e => e.StatusCode).HasColumnName("status_code");
            entity.Property(e => e.ClientIpAddress).HasColumnName("client_ip_address");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
        }

        private static void ConfigureRequestRelationships(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Request> entity)
        {
            entity.HasOne(e => e.Organization)
                .WithMany(o => o.Requests)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.VirtualKey)
                .WithMany(k => k.Requests)
                .HasForeignKey(e => e.VirtualKeyId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Team)
                .WithMany()
                .HasForeignKey(e => e.TeamId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private static void ConfigureRequestIndexes(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Request> entity)
        {
            entity.HasIndex(e => e.RequestId).IsUnique();
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.VirtualKeyId);
            entity.HasIndex(e => e.CrossBorderTransfer);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
        }

        private static void ConfigureRequestHeadersJsonColumn(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Request> entity)
        {
            entity.Property(e => e.RequestHeaders)
                .HasColumnName("request_headers")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, string>(StringComparer.Ordinal),
                    new ValueComparer<IDictionary<string, string>>(
                        (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), StringComparer.Ordinal.GetHashCode(v.Value))),
                        c => c == null ? new Dictionary<string, string>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                .HasColumnType("jsonb");
        }

        private static void ConfigureBackupConfig(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BackupConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Strategy).HasColumnName("strategy");
                entity.Property(e => e.Frequency).HasColumnName("frequency");
                entity.Property(e => e.ScheduleHour).HasColumnName("schedule_hour");
                entity.Property(e => e.EnableEncryption).HasColumnName("enable_encryption");
                entity.Property(e => e.EncryptionKeyId).HasColumnName("encryption_key_id");
                entity.Property(e => e.RetentionDays).HasColumnName("retention_days");
                entity.Property(e => e.EnablePostgresBackup).HasColumnName("enable_postgres_backup");
                entity.Property(e => e.EnableRedisBackup).HasColumnName("enable_redis_backup");
                entity.Property(e => e.EnableQdrantBackup).HasColumnName("enable_qdrant_backup");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.LastBackupAt).HasColumnName("last_backup_at");
                entity.Property(e => e.LastBackupStatus).HasColumnName("last_backup_status");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => new { e.OrganizationId, e.IsActive });
            });
        }

        private static void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.EventType).HasColumnName("event_type");
                entity.Property(e => e.EventCategory).HasColumnName("event_category");
                entity.Property(e => e.Action).HasColumnName("action");
                entity.Property(e => e.ResourceType).HasColumnName("resource_type");
                entity.Property(e => e.ResourceId).HasColumnName("resource_id");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).HasColumnName("user_agent");
                entity.Property(e => e.Region).HasColumnName("region");
                entity.Property(e => e.IntegrityHash).HasColumnName("integrity_hash");
                entity.Property(e => e.PreviousHash).HasColumnName("previous_hash");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");

                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                        new ValueComparer<Dictionary<string, object>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.EventCategory);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.OrganizationId, e.Timestamp });
            });
        }

        private static void ConfigureSpendLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SpendLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
                entity.Property(e => e.VirtualKeyId).HasColumnName("virtual_key_id");
                entity.Property(e => e.RequestId).HasColumnName("request_id");
                entity.Property(e => e.AmountUsd).HasColumnName("amount_usd").HasColumnType("decimal(18,8)");
                entity.Property(e => e.Model).HasColumnName("model");
                entity.Property(e => e.Provider).HasColumnName("provider");
                entity.Property(e => e.Tokens).HasColumnName("tokens");
                entity.Property(e => e.Region).HasColumnName("region");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.TeamId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            });
        }

        private static void ConfigureCreditTransaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CreditTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.TransactionType).HasColumnName("transaction_type");
                entity.Property(e => e.AmountUsd).HasColumnName("amount_usd").HasColumnType("decimal(18,8)");
                entity.Property(e => e.BalanceBeforeUsd).HasColumnName("balance_before_usd").HasColumnType("decimal(18,8)");
                entity.Property(e => e.BalanceAfterUsd).HasColumnName("balance_after_usd").HasColumnType("decimal(18,8)");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
                entity.Property(e => e.InitiatedBy).HasColumnName("initiated_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.TransactionType);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            });
        }

        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.InvoiceNumber).HasColumnName("invoice_number");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.PeriodStart).HasColumnName("period_start");
                entity.Property(e => e.PeriodEnd).HasColumnName("period_end");
                entity.Property(e => e.TotalAmountUsd).HasColumnName("total_amount_usd").HasColumnType("decimal(18,8)");
                entity.Property(e => e.TotalAmountBillingCurrency).HasColumnName("total_amount_billing_currency").HasColumnType("decimal(18,8)");
                entity.Property(e => e.BillingCurrency).HasColumnName("billing_currency");
                entity.Property(e => e.ExchangeRate).HasColumnName("exchange_rate").HasColumnType("decimal(18,8)");
                entity.Property(e => e.DueDate).HasColumnName("due_date");
                entity.Property(e => e.PaidAt).HasColumnName("paid_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Ignore(e => e.LineItems);

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => new { e.OrganizationId, e.PeriodStart, e.PeriodEnd });
            });
        }

        private static void ConfigureSubscriptionPlan(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.MonthlyPriceUsd).HasColumnName("monthly_price_usd");
                entity.Property(e => e.YearlyPriceUsd).HasColumnName("yearly_price_usd");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.Property(e => e.LimitsConfig)
                    .HasColumnName("limits_config")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.Property(e => e.Features)
                    .HasColumnName("features")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(StringComparer.Ordinal),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c == null ? new Dictionary<string, object>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });
        }

        private static void ConfigureInvitation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invitation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Token).HasColumnName("token");
                entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.AcceptedAt).HasColumnName("accepted_at");
                entity.Property(e => e.AcceptedBy).HasColumnName("accepted_by");
                entity.Property(e => e.DeclinedAt).HasColumnName("declined_at");
                entity.Property(e => e.DeclinedBy).HasColumnName("declined_by");
                entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
                entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Team)
                    .WithMany()
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Inviter)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => new { e.OrganizationId, e.Status });
                entity.HasIndex(e => new { e.TeamId, e.Email, e.Status });
            });
        }

        private static void ConfigureConversation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Organization)
                    .WithMany()
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private static void ConfigureConversationTurn(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConversationTurn>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.Content).HasColumnName("content");
                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, string>(StringComparer.Ordinal),
                        new ValueComparer<Dictionary<string, string>>(
                            (c1, c2) => c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), StringComparer.Ordinal.GetHashCode(v.Value))),
                            c => c == null ? new Dictionary<string, string>(StringComparer.Ordinal) : c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.Conversation)
                    .WithMany(c => c.Turns)
                    .HasForeignKey(e => e.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private static void ConfigureRefreshToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");
                entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });
        }

        private static void ConfigureJwtBlacklist(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<JwtBlacklist>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenId).HasColumnName("token_id");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenId).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });
        }

        private static void ConfigurePasswordHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.SetAt).HasColumnName("set_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => new { e.UserId, e.SetAt });
            });
        }

        private static void ConfigurePasswordPolicy(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordPolicy>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.MinLength).HasColumnName("min_length");
                entity.Property(e => e.RequireUppercase).HasColumnName("require_uppercase");
                entity.Property(e => e.RequireLowercase).HasColumnName("require_lowercase");
                entity.Property(e => e.RequireNumbers).HasColumnName("require_numbers");
                entity.Property(e => e.RequireSpecialCharacters).HasColumnName("require_special_characters");
                entity.Property(e => e.PasswordHistoryCount).HasColumnName("password_history_count");
                entity.Property(e => e.PasswordExpirationDays).HasColumnName("password_expiration_days");
                entity.Property(e => e.PasswordExpirationWarningDays).HasColumnName("password_expiration_warning_days");
                entity.Property(e => e.MaxFailedChangeAttempts).HasColumnName("max_failed_change_attempts");
                entity.Property(e => e.LockoutDurationMinutes).HasColumnName("lockout_duration_minutes");
                entity.Property(e => e.BlockCommonPasswords).HasColumnName("block_common_passwords");
                entity.Property(e => e.BlockUserInfoInPassword).HasColumnName("block_user_info_in_password");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                entity.HasOne(e => e.Organization)
                    .WithOne(o => o.PasswordPolicy)
                    .HasForeignKey<PasswordPolicy>(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.OrganizationId).IsUnique();
            });
        }
    }
}
