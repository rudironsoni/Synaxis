using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform;
using ApplicationApiKey = Synaxis.InferenceGateway.Application.ControlPlane.Entities.ApiKey;
using OperationsApiKey = Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations.ApiKey;
using IdentityUserRole = Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity.UserRole;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public sealed class ControlPlaneDbContext : DbContext
{
    public ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options)
        : base(options)
    {
    }

    // Legacy entities (to be migrated)
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ApplicationApiKey> ApiKeys => Set<ApplicationApiKey>();
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
    public DbSet<GlobalModel> GlobalModels => Set<GlobalModel>();
    public DbSet<ProviderModel> ProviderModels => Set<ProviderModel>();
    public DbSet<TenantModelLimit> TenantModelLimits => Set<TenantModelLimit>();

    // Platform schema entities
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Model> Models => Set<Model>();

    // Identity schema entities
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<SynaxisUser> SynaxisUsers => Set<SynaxisUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<IdentityUserRole> UserRoles => Set<IdentityUserRole>();
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships => Set<UserOrganizationMembership>();
    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();

    // Operations schema entities
    public DbSet<OperationsApiKey> OperationsApiKeys => Set<OperationsApiKey>();
    public DbSet<OrganizationProvider> OrganizationProviders => Set<OrganizationProvider>();
    public DbSet<OrganizationModel> OrganizationModels => Set<OrganizationModel>();
    public DbSet<RoutingStrategy> RoutingStrategies => Set<RoutingStrategy>();
    public DbSet<ProviderHealthStatus> ProviderHealthStatuses => Set<ProviderHealthStatus>();

    // Audit schema entities
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure schemas for PostgreSQL
        ConfigureSchemas(modelBuilder);

        // Configure soft delete global query filters
        ConfigureSoftDeleteFilters(modelBuilder);

        // Configure legacy entities (to be migrated)
        ConfigureLegacyEntities(modelBuilder);

        // Configure new schema entities
        ConfigurePlatformSchema(modelBuilder);
        ConfigureIdentitySchema(modelBuilder);
        ConfigureOperationsSchema(modelBuilder);
        ConfigureAuditSchema(modelBuilder);
    }

    private static void ConfigureSchemas(ModelBuilder modelBuilder)
    {
        // Platform schema
        modelBuilder.Entity<Provider>().ToTable("Providers", "platform");
        modelBuilder.Entity<Model>().ToTable("Models", "platform");

        // Identity schema
        modelBuilder.Entity<Organization>().ToTable("Organizations", "identity");
        modelBuilder.Entity<Group>().ToTable("Groups", "identity");
        modelBuilder.Entity<OrganizationSettings>().ToTable("OrganizationSettings", "identity");
        modelBuilder.Entity<SynaxisUser>().ToTable("Users", "identity");
        modelBuilder.Entity<Role>().ToTable("Roles", "identity");
        modelBuilder.Entity<IdentityUserRole>().ToTable("UserRoles", "identity");
        modelBuilder.Entity<UserOrganizationMembership>().ToTable("UserOrganizationMemberships", "identity");
        modelBuilder.Entity<UserGroupMembership>().ToTable("UserGroupMemberships", "identity");

        // Operations schema
        modelBuilder.Entity<OperationsApiKey>().ToTable("ApiKeys", "operations");
        modelBuilder.Entity<OrganizationProvider>().ToTable("OrganizationProviders", "operations");
        modelBuilder.Entity<OrganizationModel>().ToTable("OrganizationModels", "operations");
        modelBuilder.Entity<RoutingStrategy>().ToTable("RoutingStrategies", "operations");
        modelBuilder.Entity<ProviderHealthStatus>().ToTable("ProviderHealthStatuses", "operations");

        // Audit schema
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs", "audit");
    }

    private static void ConfigureSoftDeleteFilters(ModelBuilder modelBuilder)
    {
        // Apply soft delete filter to entities with DeletedAt property
        modelBuilder.Entity<Organization>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Group>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<SynaxisUser>().HasQueryFilter(e => e.DeletedAt == null);
    }

    private static void ConfigurePlatformSchema(ModelBuilder modelBuilder)
    {
        // Provider configuration
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Key).HasMaxLength(100).IsRequired();
            entity.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.ProviderType).HasMaxLength(50).IsRequired();
            entity.Property(p => p.BaseEndpoint).HasMaxLength(500);
            entity.Property(p => p.DefaultApiKeyEnvironmentVariable).HasMaxLength(200);
            entity.Property(p => p.DefaultInputCostPer1MTokens).HasPrecision(18, 9);
            entity.Property(p => p.DefaultOutputCostPer1MTokens).HasPrecision(18, 9);

            entity.HasIndex(p => p.Key).IsUnique();
            entity.HasIndex(p => p.IsActive);
        });

        // Model configuration
        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.CanonicalId).HasMaxLength(200).IsRequired();
            entity.Property(m => m.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Description).HasMaxLength(1000);

            entity.HasOne(m => m.Provider)
                .WithMany(p => p.Models)
                .HasForeignKey(m => m.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.ProviderId);
            entity.HasIndex(m => new { m.ProviderId, m.CanonicalId }).IsUnique();
            entity.HasIndex(m => m.IsActive);
        });
    }

    private static void ConfigureIdentitySchema(ModelBuilder modelBuilder)
    {
        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.LegalName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.Slug).HasMaxLength(100).IsRequired();
            entity.Property(o => o.RegistrationNumber).HasMaxLength(100);
            entity.Property(o => o.TaxId).HasMaxLength(100);
            entity.Property(o => o.LegalAddress).HasMaxLength(500);
            entity.Property(o => o.PrimaryContactEmail).HasMaxLength(320);
            entity.Property(o => o.BillingEmail).HasMaxLength(320);
            entity.Property(o => o.SupportEmail).HasMaxLength(320);
            entity.Property(o => o.PhoneNumber).HasMaxLength(50);
            entity.Property(o => o.Industry).HasMaxLength(100);
            entity.Property(o => o.CompanySize).HasMaxLength(50);
            entity.Property(o => o.WebsiteUrl).HasMaxLength(500);
            entity.Property(o => o.Status).HasMaxLength(50).IsRequired();
            entity.Property(o => o.PlanTier).HasMaxLength(50).IsRequired();

            entity.HasIndex(o => o.Slug).IsUnique();
            entity.HasIndex(o => o.Status);
            entity.HasIndex(o => o.CreatedAt);
        });

        // OrganizationSettings configuration
        modelBuilder.Entity<OrganizationSettings>(entity =>
        {
            entity.HasKey(s => s.OrganizationId);

            entity.HasOne(s => s.Organization)
                .WithOne(o => o.Settings)
                .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Description).HasMaxLength(1000);
            entity.Property(g => g.Slug).HasMaxLength(100).IsRequired();
            entity.Property(g => g.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(g => g.Organization)
                .WithMany(o => o.Groups)
                .HasForeignKey(g => g.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(g => new { g.OrganizationId, g.Slug }).IsUnique();
            entity.HasIndex(g => g.Status);
        });

        // SynaxisUser configuration
        modelBuilder.Entity<SynaxisUser>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Status).HasMaxLength(50).IsRequired();

            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Status);
        });

        // Role configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(256).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(1000);

            entity.HasOne(r => r.Organization)
                .WithMany(o => o.Roles)
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.OrganizationId, r.Name }).IsUnique()
                .HasFilter("\"OrganizationId\" IS NOT NULL");
            entity.HasIndex(r => r.IsSystemRole);
        });

        // UserRole configuration
        modelBuilder.Entity<IdentityUserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany()
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Organization)
                .WithMany()
                .HasForeignKey(ur => ur.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(ur => ur.OrganizationId);
        });

        // UserOrganizationMembership configuration
        modelBuilder.Entity<UserOrganizationMembership>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.OrganizationRole).HasMaxLength(50).IsRequired();
            entity.Property(m => m.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(m => m.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Organization)
                .WithMany(o => o.UserMemberships)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.PrimaryGroup)
                .WithMany()
                .HasForeignKey(m => m.PrimaryGroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(m => new { m.UserId, m.OrganizationId }).IsUnique();
            entity.HasIndex(m => m.OrganizationId);
            entity.HasIndex(m => m.Status);
        });

        // UserGroupMembership configuration
        modelBuilder.Entity<UserGroupMembership>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.GroupRole).HasMaxLength(50).IsRequired();

            entity.HasOne(m => m.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Group)
                .WithMany(g => g.UserMemberships)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => new { m.UserId, m.GroupId }).IsUnique();
            entity.HasIndex(m => m.GroupId);
            entity.HasIndex(m => m.IsPrimary);
        });
    }

    private static void ConfigureOperationsSchema(ModelBuilder modelBuilder)
    {
        // ApiKey configuration (operations schema - new)
        modelBuilder.Entity<OperationsApiKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.KeyHash).HasMaxLength(512).IsRequired();
            entity.Property(k => k.Name).HasMaxLength(200).IsRequired();
            entity.Property(k => k.KeyPrefix).HasMaxLength(20).IsRequired();
            entity.Property(k => k.Scopes).HasMaxLength(1000);
            entity.Property(k => k.RevocationReason).HasMaxLength(500);

            entity.HasIndex(k => k.OrganizationId);
            entity.HasIndex(k => k.KeyHash).IsUnique();
            entity.HasIndex(k => k.IsActive);
        });

        // OrganizationProvider configuration
        modelBuilder.Entity<OrganizationProvider>(entity =>
        {
            entity.HasKey(op => op.Id);
            entity.Property(op => op.ApiKeyEncrypted).HasMaxLength(500);
            entity.Property(op => op.CustomEndpoint).HasMaxLength(500);
            entity.Property(op => op.InputCostPer1MTokens).HasPrecision(18, 9);
            entity.Property(op => op.OutputCostPer1MTokens).HasPrecision(18, 9);

            entity.HasIndex(op => new { op.OrganizationId, op.ProviderId }).IsUnique();
            entity.HasIndex(op => op.ProviderId);
            entity.HasIndex(op => op.IsEnabled);
        });

        // OrganizationModel configuration
        modelBuilder.Entity<OrganizationModel>(entity =>
        {
            entity.HasKey(om => om.Id);
            entity.Property(om => om.DisplayName).HasMaxLength(200);
            entity.Property(om => om.CustomAlias).HasMaxLength(200);
            entity.Property(om => om.InputCostPer1MTokens).HasPrecision(18, 9);
            entity.Property(om => om.OutputCostPer1MTokens).HasPrecision(18, 9);

            entity.HasIndex(om => new { om.OrganizationId, om.ModelId }).IsUnique();
            entity.HasIndex(om => om.ModelId);
            entity.HasIndex(om => om.IsEnabled);
        });

        // RoutingStrategy configuration
        modelBuilder.Entity<RoutingStrategy>(entity =>
        {
            entity.HasKey(rs => rs.Id);
            entity.Property(rs => rs.Name).HasMaxLength(200).IsRequired();
            entity.Property(rs => rs.Description).HasMaxLength(1000);
            entity.Property(rs => rs.StrategyType).HasMaxLength(50).IsRequired();
            entity.Property(rs => rs.MaxCostPer1MTokens).HasPrecision(18, 9);
            entity.Property(rs => rs.MinHealthScore).HasPrecision(3, 2);

            entity.HasIndex(rs => rs.OrganizationId);
            entity.HasIndex(rs => new { rs.OrganizationId, rs.IsDefault }).IsUnique()
                .HasFilter("\"IsDefault\" = true");
            entity.HasIndex(rs => rs.IsActive);
        });

        // ProviderHealthStatus configuration
        modelBuilder.Entity<ProviderHealthStatus>(entity =>
        {
            entity.HasKey(phs => phs.Id);
            entity.Property(phs => phs.HealthScore).HasPrecision(3, 2);
            entity.Property(phs => phs.LastErrorMessage).HasMaxLength(1000);
            entity.Property(phs => phs.LastErrorCode).HasMaxLength(100);
            entity.Property(phs => phs.SuccessRate).HasPrecision(5, 4);

            entity.HasOne(phs => phs.OrganizationProvider)
                .WithMany()
                .HasForeignKey(phs => phs.OrganizationProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(phs => phs.OrganizationProviderId).IsUnique();
            entity.HasIndex(phs => new { phs.OrganizationId, phs.IsHealthy });
            entity.HasIndex(phs => phs.LastCheckedAt);
        });
    }

    private static void ConfigureAuditSchema(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Action).HasMaxLength(200).IsRequired();
            entity.Property(a => a.EntityType).HasMaxLength(100);
            entity.Property(a => a.EntityId).HasMaxLength(100);
            entity.Property(a => a.PreviousValues).HasColumnType("jsonb");
            entity.Property(a => a.NewValues).HasColumnType("jsonb");
            entity.Property(a => a.IpAddress).HasMaxLength(45);
            entity.Property(a => a.UserAgent).HasMaxLength(500);
            entity.Property(a => a.CorrelationId).HasMaxLength(100);

            entity.HasIndex(a => a.CreatedAt);
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.Action);
            entity.HasIndex(a => a.OrganizationId);
            entity.HasIndex(a => a.PartitionDate);
        });
    }

    private static void ConfigureLegacyEntities(ModelBuilder modelBuilder)
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
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
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

        // Legacy ApiKey configuration
        modelBuilder.Entity<ApplicationApiKey>(entity =>
        {
            entity.HasKey(k => k.Id);
            entity.Property(k => k.KeyHash).HasMaxLength(512).IsRequired();
            entity.Property(k => k.Name).HasMaxLength(200).IsRequired();
            entity.Property(k => k.Status).HasConversion<string>().IsRequired();
            entity.HasOne(k => k.User).WithMany().HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.SetNull);
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

        modelBuilder.Entity<GlobalModel>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Family).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Description).HasMaxLength(1000);
            // Prices: high precision for small token prices
            entity.Property(g => g.InputPrice).HasPrecision(18, 9);
            entity.Property(g => g.OutputPrice).HasPrecision(18, 9);
        });

        modelBuilder.Entity<ProviderModel>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.ProviderId).HasMaxLength(100).IsRequired();
            entity.Property(p => p.ProviderSpecificId).HasMaxLength(200).IsRequired();
            entity.HasOne(p => p.GlobalModel).WithMany(g => g.ProviderModels).HasForeignKey(p => p.GlobalModelId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(p => p.OverrideInputPrice).HasPrecision(18, 9);
            entity.Property(p => p.OverrideOutputPrice).HasPrecision(18, 9);
        });

        modelBuilder.Entity<TenantModelLimit>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.TenantId).HasMaxLength(200).IsRequired();
            entity.HasOne(t => t.GlobalModel).WithMany().HasForeignKey(t => t.GlobalModelId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(t => t.MonthlyBudget).HasPrecision(18, 9);
        });
    }
}
