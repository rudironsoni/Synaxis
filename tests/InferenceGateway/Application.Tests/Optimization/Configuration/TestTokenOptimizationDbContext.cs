using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

// Test-specific DbContext for Token Optimization
public class TestTokenOptimizationDbContext(DbContextOptions<TestTokenOptimizationDbContext> options) : DbContext(options)
{
    public DbSet<TenantTokenOptimizationConfig> TenantTokenOptimizationConfigs => this.Set<TenantTokenOptimizationConfig>();

    public DbSet<UserTokenOptimizationConfig> UserTokenOptimizationConfigs => this.Set<UserTokenOptimizationConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TenantTokenOptimizationConfig>(entity =>
        {
            entity.HasKey(t => t.TenantId);
        });

        modelBuilder.Entity<UserTokenOptimizationConfig>(entity =>
        {
            entity.HasKey(u => u.UserId);
        });
    }
}
