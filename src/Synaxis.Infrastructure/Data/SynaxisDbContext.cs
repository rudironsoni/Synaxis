// <copyright file="SynaxisDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Synaxis.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Synaxis.Infrastructure.Data
{
    /// <summary>
    /// Database context for Synaxis multi-tenant platform.
    /// </summary>
    public class SynaxisDbContext : DbContext
    {
        public SynaxisDbContext(DbContextOptions<SynaxisDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<TeamMembership> TeamMemberships { get; set; }

        public DbSet<VirtualKey> VirtualKeys { get; set; }

        public DbSet<Request> Requests { get; set; }

        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

        public DbSet<BackupConfig> BackupConfigs { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        public DbSet<SpendLog> SpendLogs { get; set; }

        public DbSet<CreditTransaction> CreditTransactions { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<Invitation> Invitations { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

        public DbSet<Collection> Collections { get; set; }

        public DbSet<CollectionMembership> CollectionMemberships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names (lowercase for PostgreSQL convention)
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
            modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");
            modelBuilder.Entity<PasswordResetToken>().ToTable("password_reset_tokens");
            modelBuilder.Entity<EmailVerificationToken>().ToTable("email_verification_tokens");

            // Configure Organizations
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

                // Configure PrivacyConsent as JSON column
                entity.Property(e => e.PrivacyConsent)
                    .HasColumnName("privacy_consent")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.Tier);
                entity.HasIndex(e => e.PrimaryRegion);

                // Data integrity constraints
                entity.ToTable(t => t.HasCheckConstraint("CK_Organization_Slug_Lowercase", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"));
            });

            // Configure Teams
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

                // Data integrity constraints
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Team_MonthlyBudget_NonNegative", "monthly_budget IS NULL OR monthly_budget >= 0");
                    t.HasCheckConstraint("CK_Team_BudgetAlertThreshold_Range", "budget_alert_threshold >= 0 AND budget_alert_threshold <= 100");
                });
            });

            // Configure Users
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
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // Configure PrivacyConsent as JSON column
                entity.Property(e => e.PrivacyConsent)
                    .HasColumnName("privacy_consent")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Users)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.DataResidencyRegion);
            });

            // Configure TeamMemberships
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

                // Add composite indexes for tenant queries
                entity.HasIndex(e => new { e.OrganizationId, e.UserId });
                entity.HasIndex(e => new { e.TeamId, e.UserId });

                // Data integrity constraints
                entity.ToTable(t => t.HasCheckConstraint("CK_TeamMembership_Role_Valid", "role IN ('OrgAdmin', 'TeamAdmin', 'Member', 'Viewer')"));
            });

            // Configure VirtualKeys
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

                // Configure Metadata as JSON column
                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

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

                // Data integrity constraints
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_VirtualKey_MaxBudget_NonNegative", "max_budget IS NULL OR max_budget >= 0");
                    t.HasCheckConstraint("CK_VirtualKey_CurrentSpend_NonNegative", "current_spend >= 0");
                });
            });

            // Configure Requests
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

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

                // Configure RequestHeaders as JSON column
                entity.Property(e => e.RequestHeaders)
                    .HasColumnName("request_headers")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, string>(),
                        new ValueComparer<IDictionary<string, string>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), StringComparer.Ordinal.GetHashCode(v.Value))),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

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

                entity.HasIndex(e => e.RequestId).IsUnique();
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.VirtualKeyId);
                entity.HasIndex(e => e.CrossBorderTransfer);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            });

            // Configure BackupConfigs
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

            // Configure AuditLogs (immutable)
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

                // Configure Metadata as JSON column with value converter
                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
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

            // Configure SpendLogs
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

            // Configure CreditTransactions
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

            // Configure Invoices
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

                // Ignore LineItems for InMemory database - it's a complex type that needs JSON serialization for real DB
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

            // Configure SubscriptionPlans
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

                // Configure LimitsConfig as JSON column
                entity.Property(e => e.LimitsConfig)
                    .HasColumnName("limits_config")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                // Configure Features as JSON column
                entity.Property(e => e.Features)
                    .HasColumnName("features")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.Slug).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Configure Invitations
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

            // Configure RefreshTokens
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // Configure PasswordResetTokens
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.IsUsed).HasColumnName("is_used");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // Configure EmailVerificationTokens
            modelBuilder.Entity<EmailVerificationToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.IsUsed).HasColumnName("is_used");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.TokenHash).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
            });

            // Configure Collections
            modelBuilder.Entity<Collection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
                entity.Property(e => e.Slug).HasColumnName("slug");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Visibility).HasColumnName("visibility");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

                // Configure Metadata as JSON column
                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!) ?? new Dictionary<string, object>(),
                        new ValueComparer<IDictionary<string, object>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v.Key), v.Value.GetHashCode())),
                            c => c.ToDictionary(entry => entry.Key, entry => entry.Value)))
                    .HasColumnType("jsonb");

                entity.HasOne(e => e.Organization)
                    .WithMany(o => o.Collections)
                    .HasForeignKey(e => e.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Team)
                    .WithMany(t => t.Collections)
                    .HasForeignKey(e => e.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Creator)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.OrganizationId, e.Slug }).IsUnique();
                entity.HasIndex(e => new { e.OrganizationId, e.Name });
                entity.HasIndex(e => e.TeamId);
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Visibility);
                entity.HasIndex(e => e.IsActive);

                // Data integrity constraints
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Collection_Type_Valid", "type IN ('general', 'models', 'prompts', 'datasets', 'workflows')");
                    t.HasCheckConstraint("CK_Collection_Visibility_Valid", "visibility IN ('public', 'private', 'team')");
                });
            });

            // Configure CollectionMemberships
            modelBuilder.Entity<CollectionMembership>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CollectionId).HasColumnName("collection_id");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.Role).HasColumnName("role");
                entity.Property(e => e.JoinedAt).HasColumnName("joined_at");
                entity.Property(e => e.AddedBy).HasColumnName("added_by");

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CollectionMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Collection)
                    .WithMany(c => c.CollectionMemberships)
                    .HasForeignKey(e => e.CollectionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Adder)
                    .WithMany()
                    .HasForeignKey(e => e.AddedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.UserId, e.CollectionId }).IsUnique();

                // Add composite indexes for tenant queries
                entity.HasIndex(e => new { e.OrganizationId, e.UserId });
                entity.HasIndex(e => new { e.CollectionId, e.UserId });

                // Data integrity constraints
                entity.ToTable(t => t.HasCheckConstraint("CK_CollectionMembership_Role_Valid", "role IN ('Admin', 'Member', 'Viewer')"));
            });
        }
    }
}
