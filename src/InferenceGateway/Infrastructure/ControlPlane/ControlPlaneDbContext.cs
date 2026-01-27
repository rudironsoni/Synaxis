using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public sealed class ControlPlaneDbContext : DbContext
{
    public ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<OAuthAccount> OAuthAccounts => Set<OAuthAccount>();
    public DbSet<RoutingPolicy> RoutingPolicies => Set<RoutingPolicy>();
    public DbSet<ModelAlias> ModelAliases => Set<ModelAlias>();
    public DbSet<ModelCombo> ModelCombos => Set<ModelCombo>();
    public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();
    public DbSet<ModelCost> ModelCosts => Set<ModelCost>();
    public DbSet<RequestLog> RequestLogs => Set<RequestLog>();
    public DbSet<TokenUsage> TokenUsages => Set<TokenUsage>();
    public DbSet<QuotaSnapshot> QuotaSnapshots => Set<QuotaSnapshot>();
    public DbSet<DeviationEntry> Deviations => Set<DeviationEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Region).HasConversion<string>().IsRequired();
            entity.Property(t => t.Status).HasConversion<string>().IsRequired();
            entity.HasMany(t => t.Projects).WithOne(p => p.Tenant).HasForeignKey(p => p.TenantId);
            entity.HasMany(t => t.Users).WithOne(u => u.Tenant).HasForeignKey(u => u.TenantId);
            entity.HasMany(t => t.OAuthAccounts).WithOne(o => o.Tenant).HasForeignKey(o => o.TenantId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.AuthProvider).HasMaxLength(50).IsRequired();
            entity.Property(u => u.ProviderUserId).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Status).HasConversion<string>().IsRequired();
            entity.HasMany(p => p.ApiKeys).WithOne(k => k.Project).HasForeignKey(k => k.ProjectId);
        });

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Users)
            .WithMany(u => u.Projects)
            .UsingEntity<Dictionary<string, object>>(
                "ProjectUsers",
                join => join.HasOne<User>().WithMany().HasForeignKey("UserId"),
                join => join.HasOne<Project>().WithMany().HasForeignKey("ProjectId"));

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.KeyHash).HasMaxLength(512).IsRequired();
            entity.Property(k => k.Name).HasMaxLength(200).IsRequired();
            entity.Property(k => k.Status).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<OAuthAccount>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Provider).HasMaxLength(50).IsRequired();
            entity.Property(o => o.Status).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<RoutingPolicy>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.PolicyJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<ModelAlias>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Alias).HasMaxLength(200).IsRequired();
            entity.Property(m => m.TargetModel).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<ModelCombo>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).HasMaxLength(200).IsRequired();
            entity.Property(m => m.OrderedModelsJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<ProviderAccount>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Provider).HasMaxLength(100).IsRequired();
            entity.Property(p => p.AccountId).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Status).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<ModelCost>(entity =>
        {
            entity.HasKey(m => new { m.Provider, m.Model });
            entity.Property(m => m.Provider).HasMaxLength(100).IsRequired();
            entity.Property(m => m.Model).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RequestId).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Model).HasMaxLength(200);
            entity.Property(r => r.Provider).HasMaxLength(200);
        });

        modelBuilder.Entity<TokenUsage>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.RequestId).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<QuotaSnapshot>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Provider).HasMaxLength(100).IsRequired();
            entity.Property(q => q.AccountId).HasMaxLength(200).IsRequired();
            entity.Property(q => q.QuotaJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<DeviationEntry>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Field).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Reason).HasMaxLength(500).IsRequired();
            entity.Property(d => d.Mitigation).HasMaxLength(500).IsRequired();
            entity.Property(d => d.Status).HasConversion<string>().IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(200).IsRequired();
            entity.Property(a => a.PayloadJson).HasColumnType("jsonb");
        });
    }
}
