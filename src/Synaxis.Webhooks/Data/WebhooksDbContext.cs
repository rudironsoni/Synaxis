// <copyright file="WebhooksDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Webhooks.Models;

    /// <summary>
    /// Database context for webhook entities.
    /// </summary>
    public class WebhooksDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebhooksDbContext"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public WebhooksDbContext(DbContextOptions<WebhooksDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the webhooks.
        /// </summary>
        public DbSet<Webhook> Webhooks { get; set; }

        /// <summary>
        /// Gets or sets the webhook delivery logs.
        /// </summary>
        public DbSet<WebhookDeliveryLog> WebhookDeliveryLogs { get; set; }

#pragma warning disable MA0051 // Method is too long
        /// <summary>
        /// Configures the model that was discovered by convention from the entity types
        /// exposed in <see cref="DbSet{TEntity}"/> properties on your derived context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
#pragma warning restore MA0051 // Method is too long
        {
            base.OnModelCreating(modelBuilder);

            // Configure Webhooks
            modelBuilder.Entity<Webhook>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Url).HasColumnName("url");
                entity.Property(e => e.Secret).HasColumnName("secret");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.LastSuccessfulDeliveryAt).HasColumnName("last_successful_delivery_at");
                entity.Property(e => e.FailedDeliveryAttempts).HasColumnName("failed_delivery_attempts");

                // Configure Events as JSON column
                entity.Property(e => e.Events)
                    .HasColumnName("events")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!) ?? new List<string>(),
                        new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                            (c1, c2) => c1.Count == c2.Count && !c1.Except(c2).Any(),
                            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, StringComparer.Ordinal.GetHashCode(v))),
                            c => c.ToList()))
                    .HasColumnType("jsonb");

                entity.HasIndex(e => e.OrganizationId);
                entity.HasIndex(e => new { e.OrganizationId, e.IsActive });
            });

            // Configure WebhookDeliveryLogs
            modelBuilder.Entity<WebhookDeliveryLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.WebhookId).HasColumnName("webhook_id");
                entity.Property(e => e.EventType).HasColumnName("event_type");
                entity.Property(e => e.Payload).HasColumnName("payload");
                entity.Property(e => e.StatusCode).HasColumnName("status_code");
                entity.Property(e => e.ResponseBody).HasColumnName("response_body");
                entity.Property(e => e.ErrorMessage).HasColumnName("error_message");
                entity.Property(e => e.RetryAttempt).HasColumnName("retry_attempt");
                entity.Property(e => e.IsSuccess).HasColumnName("is_success");
                entity.Property(e => e.DeliveredAt).HasColumnName("delivered_at");
                entity.Property(e => e.DurationMs).HasColumnName("duration_ms");

                entity.HasOne(e => e.Webhook)
                    .WithMany(w => w.DeliveryLogs)
                    .HasForeignKey(e => e.WebhookId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.WebhookId);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => e.DeliveredAt);
                entity.HasIndex(e => new { e.WebhookId, e.DeliveredAt });
            });
        }
    }
}
