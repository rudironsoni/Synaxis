using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform;
using Synaxis.InferenceGateway.Infrastructure.Services;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Configuration;

/// <summary>
/// Unit tests for ConfigurationResolver.
/// </summary>
public sealed class ConfigurationResolverTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly ConfigurationResolver _resolver;

    public ConfigurationResolverTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this._context = new SynaxisDbContext(options);
        this._resolver = new ConfigurationResolver(this._context);
    }

    [Fact]
    public async Task GetRateLimitsAsync_WithUserMembershipLimits_ReturnsUserLimits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var org = new Organization
        {
            Id = orgId,
            LegalName = "Test",
            DisplayName = "Test",
            Slug = "test",
            Status = "Active",
            PlanTier = "Free",
        };

        var orgSettings = new OrganizationSettings
        {
            OrganizationId = orgId,
            DefaultRateLimitRpm = 100,
            DefaultRateLimitTpm = 10000,
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            OrganizationRole = "Member",
            RateLimitRpm = 50,
            RateLimitTpm = 5000,
            Status = "Active",
        };

        this._context.Organizations.Add(org);
        this._context.OrganizationSettings.Add(orgSettings);
        this._context.UserOrganizationMemberships.Add(membership);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._resolver.GetRateLimitsAsync(userId, orgId);

        // Assert
        Assert.Equal(50, result.RequestsPerMinute);
        Assert.Equal(5000, result.TokensPerMinute);
        Assert.Equal("UserMembership", result.Source);
    }

    [Fact]
    public async Task GetRateLimitsAsync_WithoutUserLimits_ReturnsGroupLimits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var org = new Organization
        {
            Id = orgId,
            LegalName = "Test",
            DisplayName = "Test",
            Slug = "test",
            Status = "Active",
            PlanTier = "Free",
        };

        var group = new Group
        {
            Id = groupId,
            OrganizationId = orgId,
            Name = "Test Group",
            Slug = "test-group",
            Status = "Active",
            RateLimitRpm = 75,
            RateLimitTpm = 7500,
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            OrganizationRole = "Member",
            PrimaryGroupId = groupId,
            Status = "Active",
        };

        this._context.Organizations.Add(org);
        this._context.Groups.Add(group);
        this._context.UserOrganizationMemberships.Add(membership);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._resolver.GetRateLimitsAsync(userId, orgId);

        // Assert
        Assert.Equal(75, result.RequestsPerMinute);
        Assert.Equal(7500, result.TokensPerMinute);
        Assert.Equal("Group", result.Source);
    }

    [Fact]
    public async Task GetEffectiveCostPer1MTokensAsync_WithOrganizationModel_ReturnsModelCost()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var modelId = Guid.NewGuid();

        var provider = new Provider
        {
            Id = providerId,
            Key = "test-provider",
            DisplayName = "Test Provider",
            ProviderType = "OpenAI",
            DefaultInputCostPer1MTokens = 1.0m,
            DefaultOutputCostPer1MTokens = 2.0m,
        };

        var model = new Model
        {
            Id = modelId,
            ProviderId = providerId,
            CanonicalId = "test-model",
            DisplayName = "Test Model",
        };

        var orgModel = new OrganizationModel
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ModelId = modelId,
            InputCostPer1MTokens = 0.5m,
            OutputCostPer1MTokens = 1.0m,
        };

        this._context.Providers.Add(provider);
        this._context.Models.Add(model);
        this._context.OrganizationModels.Add(orgModel);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._resolver.GetEffectiveCostPer1MTokensAsync(orgId, providerId, modelId);

        // Assert
        Assert.Equal(0.5m, result.InputCostPer1MTokens);
        Assert.Equal(1.0m, result.OutputCostPer1MTokens);
        Assert.Equal("OrganizationModel", result.Source);
    }

    [Fact]
    public async Task ShouldAutoOptimizeAsync_WithUserMembership_ReturnsUserSetting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var org = new Organization
        {
            Id = orgId,
            LegalName = "Test",
            DisplayName = "Test",
            Slug = "test",
            Status = "Active",
            PlanTier = "Free",
        };

        var orgSettings = new OrganizationSettings
        {
            OrganizationId = orgId,
            AllowAutoOptimization = true,
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            OrganizationRole = "Member",
            AllowAutoOptimization = false,
            Status = "Active",
        };

        this._context.Organizations.Add(org);
        this._context.OrganizationSettings.Add(orgSettings);
        this._context.UserOrganizationMemberships.Add(membership);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._resolver.ShouldAutoOptimizeAsync(userId, orgId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldAutoOptimizeAsync_WithNoSettings_ReturnsDefaultTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        // Act
        var result = await this._resolver.ShouldAutoOptimizeAsync(userId, orgId);

        // Assert
        Assert.True(result);
    }

    public void Dispose()
    {
        this._context.Database.EnsureDeleted();
        this._context.Dispose();
    }
}
