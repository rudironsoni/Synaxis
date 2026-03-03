// <copyright file="InferenceDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Entity Framework Core database context for inference operations.
/// </summary>
public class InferenceDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InferenceDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public InferenceDbContext(DbContextOptions<InferenceDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the inference requests DbSet.
    /// </summary>
    public DbSet<InferenceRequestEntity> InferenceRequests => this.Set<InferenceRequestEntity>();

    /// <summary>
    /// Gets the model configurations DbSet.
    /// </summary>
    public DbSet<ModelConfigEntity> ModelConfigs => this.Set<ModelConfigEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureInferenceRequest(modelBuilder);
        ConfigureModelConfig(modelBuilder);
    }

    private static void ConfigureInferenceRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InferenceRequestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.RequestContent).IsRequired().HasMaxLength(8192);
            entity.Property(e => e.ResponseContent).HasMaxLength(32768);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2048);
            entity.Property(e => e.Cost).HasPrecision(18, 8);

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }

    private static void ConfigureModelConfig(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModelConfigEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(256);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1024);

            entity.OwnsOne(e => e.Settings, settings =>
            {
                settings.Property(p => p.MaxTokens);
                settings.Property(p => p.Temperature);
                settings.Property(p => p.TopP);
            });

            entity.OwnsOne(e => e.Pricing, pricing =>
            {
                pricing.Property(p => p.InputPricePer1K).HasPrecision(18, 8);
                pricing.Property(p => p.OutputPricePer1K).HasPrecision(18, 8);
            });

            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
