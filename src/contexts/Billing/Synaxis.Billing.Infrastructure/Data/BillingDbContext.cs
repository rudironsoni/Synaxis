// <copyright file="BillingDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Data
{
    using Billing.Domain.Entities;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Entity Framework database context for billing operations.
    /// </summary>
    public class BillingDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingDbContext"/> class.
        /// </summary>
        /// <param name="options">The database context options.</param>
        public BillingDbContext(DbContextOptions<BillingDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the invoices DbSet.
        /// </summary>
        public DbSet<Invoice> Invoices => this.Set<Invoice>();

        /// <summary>
        /// Gets the payments DbSet.
        /// </summary>
        public DbSet<Payment> Payments => this.Set<Payment>();

        /// <summary>
        /// Gets the subscriptions DbSet.
        /// </summary>
        public DbSet<Subscription> Subscriptions => this.Set<Subscription>();

        /// <summary>
        /// Gets the usage records DbSet.
        /// </summary>
        public DbSet<UsageRecord> UsageRecords => this.Set<UsageRecord>();

        /// <summary>
        /// Gets the cost savings records DbSet.
        /// </summary>
        public DbSet<CostSavingsRecord> CostSavingsRecords => this.Set<CostSavingsRecord>();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureInvoice(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigureSubscription(modelBuilder);
            ConfigureUsageRecord(modelBuilder);
            ConfigureCostSavingsRecord(modelBuilder);
        }

        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).IsRequired();
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.DueDate);
            });
        }

        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).IsRequired();
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.Currency).HasMaxLength(3);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.TransactionId).IsUnique();
            });
        }

        private static void ConfigureSubscription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).IsRequired();
                entity.Property(e => e.PlanId).IsRequired();
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.Status);
            });
        }

        private static void ConfigureUsageRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UsageRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).IsRequired();
                entity.Property(e => e.Quantity).HasPrecision(18, 6);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => e.ResourceType);
                entity.HasIndex(e => e.Timestamp);
            });
        }

        private static void ConfigureCostSavingsRecord(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CostSavingsRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrganizationId).IsRequired();
                entity.Property(e => e.OriginalCost).HasPrecision(18, 2);
                entity.Property(e => e.OptimizedCost).HasPrecision(18, 2);
                entity.Property(e => e.SavingsAmount).HasPrecision(18, 2);
                entity.Property(e => e.SavingsPercentage).HasPrecision(5, 2);
                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => new { e.OrganizationId, e.IsAppliedToInvoice });
                entity.HasIndex(e => e.AppliedAt);
            });
        }
    }
}
