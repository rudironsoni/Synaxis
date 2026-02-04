using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

/// <summary>
/// Multi-tenant DbContext for Synaxis with four separate schemas:
/// - platform: Tenant-agnostic provider and model catalog
/// - identity: Multi-tenant organization, user, and group management
/// - operations: Runtime provider configurations and API keys
/// - audit: System-wide audit logging
/// </summary>
public sealed class SynaxisDbContext : IdentityDbContext<SynaxisUser, Role, Guid>
{
    public SynaxisDbContext(DbContextOptions<SynaxisDbContext> options)
        : base(options)
    {
    }

    // Platform Schema (tenant-agnostic)
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Model> Models => Set<Model>();

    // Identity Schema (multi-tenant)
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<UserRole> UserRoleAssignments => Set<UserRole>();
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships => Set<UserOrganizationMembership>();
    public DbSet<UserGroupMembership> UserGroupMemberships => Set<UserGroupMembership>();

    // Operations Schema (runtime)
    public DbSet<OrganizationProvider> OrganizationProviders => Set<OrganizationProvider>();
    public DbSet<OrganizationModel> OrganizationModels => Set<OrganizationModel>();
    public DbSet<RoutingStrategy> RoutingStrategies => Set<RoutingStrategy>();
    public DbSet<ProviderHealthStatus> ProviderHealthStatuses => Set<ProviderHealthStatus>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    // Audit Schema (logs)
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePlatformSchema(modelBuilder);
        ConfigureIdentitySchema(modelBuilder);
        ConfigureOperationsSchema(modelBuilder);
        ConfigureAuditSchema(modelBuilder);
    }

    private static void ConfigurePlatformSchema(ModelBuilder modelBuilder)
    {
        // Provider Configuration
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("Providers", "platform", t => 
                t.HasCheckConstraint("CK_Provider_ProviderType", 
                    "\"ProviderType\" IN ('OpenAI', 'Anthropic', 'Google', 'Cohere', 'Azure', 'AWS', 'Cloudflare', 'Generic')"));
            
            entity.HasKey(p => p.Id);
            
            entity.Property(p => p.Key).HasMaxLength(100).IsRequired();
            entity.Property(p => p.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(p => p.ProviderType).HasMaxLength(50).IsRequired();
            entity.Property(p => p.BaseEndpoint).HasMaxLength(500);
            entity.Property(p => p.DefaultApiKeyEnvironmentVariable).HasMaxLength(100);
            entity.Property(p => p.DefaultInputCostPer1MTokens).HasPrecision(18, 6);
            entity.Property(p => p.DefaultOutputCostPer1MTokens).HasPrecision(18, 6);

            entity.HasIndex(p => p.Key).IsUnique();
            entity.HasIndex(p => p.IsActive);
        });

        // Model Configuration
        modelBuilder.Entity<Model>(entity =>
        {
            entity.ToTable("Models", "platform");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.CanonicalId).HasMaxLength(200).IsRequired();
            entity.Property(m => m.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(m => m.Description).HasMaxLength(1000);

            entity.HasOne(m => m.Provider)
                .WithMany(p => p.Models)
                .HasForeignKey(m => m.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => new { m.ProviderId, m.CanonicalId }).IsUnique();
            entity.HasIndex(m => m.IsActive);
        });
    }

    private static void ConfigureIdentitySchema(ModelBuilder modelBuilder)
    {
        // Rename ASP.NET Identity tables to plural and move to identity schema
        modelBuilder.Entity<SynaxisUser>().ToTable("Users", "identity");
        modelBuilder.Entity<Role>().ToTable("Roles", "identity");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles", "identity");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims", "identity");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins", "identity");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens", "identity");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims", "identity");

        // Organization Configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("Organizations", "identity", t => 
            {
                t.HasCheckConstraint("CK_Organization_Status", 
                    "\"Status\" IN ('Active', 'Suspended', 'PendingActivation', 'Deactivated')");
                t.HasCheckConstraint("CK_Organization_PlanTier", 
                    "\"PlanTier\" IN ('Free', 'Starter', 'Professional', 'Enterprise', 'Custom')");
            });
            
            entity.HasKey(o => o.Id);

            entity.Property(o => o.LegalName).HasMaxLength(500).IsRequired();
            entity.Property(o => o.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(o => o.Slug).HasMaxLength(100).IsRequired();
            entity.Property(o => o.RegistrationNumber).HasMaxLength(100);
            entity.Property(o => o.TaxId).HasMaxLength(100);
            entity.Property(o => o.LegalAddress).HasMaxLength(1000);
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
            entity.HasIndex(o => o.DeletedAt);

            entity.HasQueryFilter(o => o.DeletedAt == null);
        });

        // OrganizationSettings Configuration
        modelBuilder.Entity<OrganizationSettings>(entity =>
        {
            entity.ToTable("OrganizationSettings", "identity");
            entity.HasKey(s => s.OrganizationId);

            entity.HasOne(s => s.Organization)
                .WithOne(o => o.Settings)
                .HasForeignKey<OrganizationSettings>(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Group Configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("Groups", "identity", t => 
                t.HasCheckConstraint("CK_Group_Status", 
                    "\"Status\" IN ('Active', 'Suspended', 'Archived')"));
            
            entity.HasKey(g => g.Id);

            entity.Property(g => g.Name).HasMaxLength(200).IsRequired();
            entity.Property(g => g.Description).HasMaxLength(1000);
            entity.Property(g => g.Slug).HasMaxLength(100).IsRequired();
            entity.Property(g => g.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(g => g.Organization)
                .WithMany(o => o.Groups)
                .HasForeignKey(g => g.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.ParentGroup)
                .WithMany(g => g.ChildGroups)
                .HasForeignKey(g => g.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(g => new { g.OrganizationId, g.Slug }).IsUnique();
            entity.HasIndex(g => g.Status);
            entity.HasIndex(g => g.DeletedAt);

            entity.HasQueryFilter(g => g.DeletedAt == null);
        });

        // SynaxisUser Configuration
        modelBuilder.Entity<SynaxisUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);
            entity.Property(u => u.Status).HasMaxLength(50).IsRequired();

            entity.HasIndex(u => u.Status);
            entity.HasIndex(u => u.DeletedAt);

            entity.HasQueryFilter(u => u.DeletedAt == null);
            
            entity.ToTable("Users", "identity", t => 
                t.HasCheckConstraint("CK_User_Status", 
                    "\"Status\" IN ('Active', 'Suspended', 'PendingVerification', 'Deactivated')"));
        });

        // Role Configuration
        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(r => r.Description).HasMaxLength(500);

            entity.HasOne(r => r.Organization)
                .WithMany(o => o.Roles)
                .HasForeignKey(r => r.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.OrganizationId, r.Name }).IsUnique();
        });

        // UserRole Configuration (custom table for organization-scoped roles)
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoleAssignments", "identity");
            entity.HasKey(ur => new { ur.UserId, ur.RoleId, ur.OrganizationId });

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
        });

        // UserOrganizationMembership Configuration
        modelBuilder.Entity<UserOrganizationMembership>(entity =>
        {
            entity.ToTable("UserOrganizationMemberships", "identity", t => 
            {
                t.HasCheckConstraint("CK_UserOrgMembership_OrganizationRole", 
                    "\"OrganizationRole\" IN ('Owner', 'Admin', 'Member', 'Guest')");
                t.HasCheckConstraint("CK_UserOrgMembership_Status", 
                    "\"Status\" IN ('Active', 'Suspended', 'Invited', 'Rejected')");
            });
            
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
            entity.HasIndex(m => m.Status);
        });

        // UserGroupMembership Configuration
        modelBuilder.Entity<UserGroupMembership>(entity =>
        {
            entity.ToTable("UserGroupMemberships", "identity", t => 
                t.HasCheckConstraint("CK_UserGroupMembership_GroupRole", 
                    "\"GroupRole\" IN ('Admin', 'Member', 'Viewer')"));
            
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
            entity.HasIndex(m => m.IsPrimary);
        });
    }

    private static void ConfigureOperationsSchema(ModelBuilder modelBuilder)
    {
        // OrganizationProvider Configuration
        modelBuilder.Entity<OrganizationProvider>(entity =>
        {
            entity.ToTable("OrganizationProviders", "operations");
            entity.HasKey(op => op.Id);

            entity.Property(op => op.ApiKeyEncrypted).HasMaxLength(1000);
            entity.Property(op => op.CustomEndpoint).HasMaxLength(500);
            entity.Property(op => op.InputCostPer1MTokens).HasPrecision(18, 6);
            entity.Property(op => op.OutputCostPer1MTokens).HasPrecision(18, 6);

            entity.HasIndex(op => new { op.OrganizationId, op.ProviderId }).IsUnique();
            entity.HasIndex(op => op.IsEnabled);
        });

        // OrganizationModel Configuration
        modelBuilder.Entity<OrganizationModel>(entity =>
        {
            entity.ToTable("OrganizationModels", "operations");
            entity.HasKey(om => om.Id);

            entity.Property(om => om.DisplayName).HasMaxLength(200);
            entity.Property(om => om.InputCostPer1MTokens).HasPrecision(18, 6);
            entity.Property(om => om.OutputCostPer1MTokens).HasPrecision(18, 6);
            entity.Property(om => om.CustomAlias).HasMaxLength(200);

            entity.HasIndex(om => new { om.OrganizationId, om.ModelId }).IsUnique();
            entity.HasIndex(om => om.IsEnabled);
        });

        // RoutingStrategy Configuration
        modelBuilder.Entity<RoutingStrategy>(entity =>
        {
            entity.ToTable("RoutingStrategies", "operations", t => 
                t.HasCheckConstraint("CK_RoutingStrategy_StrategyType", 
                    "\"StrategyType\" IN ('CostOptimized', 'Performance', 'Reliability', 'Custom')"));
            
            entity.HasKey(rs => rs.Id);

            entity.Property(rs => rs.Name).HasMaxLength(200).IsRequired();
            entity.Property(rs => rs.Description).HasMaxLength(1000);
            entity.Property(rs => rs.StrategyType).HasMaxLength(50).IsRequired();
            entity.Property(rs => rs.MaxCostPer1MTokens).HasPrecision(18, 6);
            entity.Property(rs => rs.MinHealthScore).HasPrecision(5, 4);

            entity.HasIndex(rs => new { rs.OrganizationId, rs.Name }).IsUnique();
            entity.HasIndex(rs => rs.IsActive);
        });

        // ProviderHealthStatus Configuration
        modelBuilder.Entity<ProviderHealthStatus>(entity =>
        {
            entity.ToTable("ProviderHealthStatuses", "operations");
            entity.HasKey(phs => phs.Id);

            entity.Property(phs => phs.HealthScore).HasPrecision(5, 4);
            entity.Property(phs => phs.LastErrorMessage).HasMaxLength(2000);
            entity.Property(phs => phs.LastErrorCode).HasMaxLength(100);
            entity.Property(phs => phs.SuccessRate).HasPrecision(5, 4);

            entity.HasOne(phs => phs.OrganizationProvider)
                .WithMany()
                .HasForeignKey(phs => phs.OrganizationProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(phs => phs.OrganizationProviderId).IsUnique();
            entity.HasIndex(phs => phs.IsHealthy);
            entity.HasIndex(phs => phs.LastCheckedAt);
        });

        // ApiKey Configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys", "operations");
            entity.HasKey(ak => ak.Id);

            entity.Property(ak => ak.Name).HasMaxLength(200).IsRequired();
            entity.Property(ak => ak.KeyHash).HasMaxLength(512).IsRequired();
            entity.Property(ak => ak.KeyPrefix).HasMaxLength(20).IsRequired();
            entity.Property(ak => ak.Scopes).HasMaxLength(1000);
            entity.Property(ak => ak.RevocationReason).HasMaxLength(500);

            entity.HasIndex(ak => ak.KeyHash).IsUnique();
            entity.HasIndex(ak => ak.OrganizationId);
            entity.HasIndex(ak => ak.IsActive);
            entity.HasIndex(ak => ak.ExpiresAt);
        });
    }

    private static void ConfigureAuditSchema(ModelBuilder modelBuilder)
    {
        // AuditLog Configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs", "audit", t => 
                t.HasCheckConstraint("CK_AuditLog_Action", 
                    "\"Action\" IN ('Create', 'Update', 'Delete', 'Read', 'Login', 'Logout', 'ApiCall', 'PermissionChange', 'ConfigChange')"));
            
            entity.HasKey(al => al.Id);

            entity.Property(al => al.Action).HasMaxLength(200).IsRequired();
            entity.Property(al => al.EntityType).HasMaxLength(100);
            entity.Property(al => al.EntityId).HasMaxLength(100);
            entity.Property(al => al.PreviousValues).HasColumnType("jsonb");
            entity.Property(al => al.NewValues).HasColumnType("jsonb");
            entity.Property(al => al.IpAddress).HasMaxLength(45);
            entity.Property(al => al.UserAgent).HasMaxLength(500);
            entity.Property(al => al.CorrelationId).HasMaxLength(100);

            entity.HasIndex(al => al.OrganizationId);
            entity.HasIndex(al => al.UserId);
            entity.HasIndex(al => al.Action);
            entity.HasIndex(al => al.EntityType);
            entity.HasIndex(al => al.CreatedAt);
            entity.HasIndex(al => al.PartitionDate);
            entity.HasIndex(al => al.CorrelationId);
        });
    }
}
