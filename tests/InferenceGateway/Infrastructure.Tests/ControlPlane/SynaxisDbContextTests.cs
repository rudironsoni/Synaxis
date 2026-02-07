using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Audit;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Platform;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.ControlPlane;

/// <summary>
/// Tests for the SynaxisDbContext with all four schemas.
/// </summary>
public class SynaxisDbContextTests : IDisposable
{
    private readonly SynaxisDbContext _context;

    public SynaxisDbContextTests()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this._context = new SynaxisDbContext(options);
    }

    public void Dispose()
    {
        this._context.Dispose();
    }

    [Fact]
    public void Should_Create_Provider_In_Platform_Schema()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Key = "openai",
            DisplayName = "OpenAI",
            ProviderType = "OpenAI",
            SupportsStreaming = true,
            SupportsTools = true,
            SupportsVision = true,
            DefaultInputCostPer1MTokens = 0.03m,
            DefaultOutputCostPer1MTokens = 0.06m,
            IsActive = true,
            IsPublic = true,
        };

        // Act
        this._context.Providers.Add(provider);
        this._context.SaveChanges();

        // Assert
        var savedProvider = this._context.Providers.Find(provider.Id);
        savedProvider.Should().NotBeNull();
        savedProvider!.Key.Should().Be("openai");
        savedProvider.DisplayName.Should().Be("OpenAI");
    }

    [Fact]
    public void Should_Create_Model_With_Provider_Relationship()
    {
        // Arrange
        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Key = "openai",
            DisplayName = "OpenAI",
            ProviderType = "OpenAI",
            IsActive = true,
        };

        var model = new Model
        {
            Id = Guid.NewGuid(),
            ProviderId = provider.Id,
            CanonicalId = "gpt-4",
            DisplayName = "GPT-4",
            Description = "Most capable GPT-4 model",
            ContextWindowTokens = 8192,
            MaxOutputTokens = 4096,
            SupportsStreaming = true,
            SupportsTools = true,
            IsActive = true,
        };

        // Act
        this._context.Providers.Add(provider);
        this._context.Models.Add(model);
        this._context.SaveChanges();

        // Assert
        var savedModel = this._context.Models
            .Include(m => m.Provider)
            .FirstOrDefault(m => m.Id == model.Id);

        savedModel.Should().NotBeNull();
        savedModel!.Provider.Should().NotBeNull();
        savedModel.Provider.Key.Should().Be("openai");
    }

    [Fact]
    public void Should_Create_Organization_With_Settings()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Acme Corporation",
            DisplayName = "ACME",
            Slug = "acme-corp",
            Status = "Active",
            PlanTier = "Professional",
            CreatedAt = DateTime.UtcNow,
        };

        var settings = new OrganizationSettings
        {
            OrganizationId = organization.Id,
            JwtTokenLifetimeMinutes = 120,
            DefaultRateLimitRpm = 1000,
            AllowAutoOptimization = true,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.OrganizationSettings.Add(settings);
        this._context.SaveChanges();

        // Assert
        var savedOrg = this._context.Organizations
            .Include(o => o.Settings)
            .FirstOrDefault(o => o.Id == organization.Id);

        savedOrg.Should().NotBeNull();
        savedOrg!.Settings.Should().NotBeNull();
        savedOrg.Settings!.JwtTokenLifetimeMinutes.Should().Be(120);
    }

    [Fact]
    public void Should_Create_User_And_Organization_Membership()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var user = new SynaxisUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            Status = "Active",
            EmailConfirmed = true,
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = organization.Id,
            OrganizationRole = "Admin",
            Status = "Active",
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.Users.Add(user);
        this._context.UserOrganizationMemberships.Add(membership);
        this._context.SaveChanges();

        // Assert
        var savedMembership = this._context.UserOrganizationMemberships
            .Include(m => m.User)
            .Include(m => m.Organization)
            .FirstOrDefault(m => m.Id == membership.Id);

        savedMembership.Should().NotBeNull();
        savedMembership!.User.FirstName.Should().Be("Test");
        savedMembership.Organization.DisplayName.Should().Be("Test");
    }

    [Fact]
    public void Should_Create_Group_Hierarchy()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var parentGroup = new Group
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Engineering",
            Slug = "engineering",
            Status = "Active",
        };

        var childGroup = new Group
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Backend Team",
            Slug = "backend-team",
            Status = "Active",
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.Groups.Add(parentGroup);
        this._context.Groups.Add(childGroup);
        this._context.SaveChanges();

        // Assert
        var savedChild = this._context.Groups
            .FirstOrDefault(g => g.Id == childGroup.Id);

        savedChild.Should().NotBeNull();
        savedChild!.Name.Should().Be("Backend Team");
    }

    [Fact]
    public void Should_Create_OrganizationProvider_Configuration()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Key = "openai",
            DisplayName = "OpenAI",
            ProviderType = "OpenAI",
            IsActive = true,
        };

        var orgProvider = new OrganizationProvider
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            ApiKeyEncrypted = "encrypted_key_here",
            InputCostPer1MTokens = 0.03m,
            OutputCostPer1MTokens = 0.06m,
            IsEnabled = true,
            IsDefault = true,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.Providers.Add(provider);
        this._context.OrganizationProviders.Add(orgProvider);
        this._context.SaveChanges();

        // Assert
        var savedOrgProvider = this._context.OrganizationProviders
            .FirstOrDefault(op => op.Id == orgProvider.Id);

        savedOrgProvider.Should().NotBeNull();
        savedOrgProvider!.IsDefault.Should().BeTrue();
        savedOrgProvider.InputCostPer1MTokens.Should().Be(0.03m);
    }

    [Fact]
    public void Should_Create_RoutingStrategy()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var strategy = new RoutingStrategy
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Cost Optimized",
            Description = "Minimize costs while maintaining quality",
            StrategyType = "CostOptimized",
            PrioritizeFreeProviders = true,
            MaxCostPer1MTokens = 0.05m,
            IsDefault = true,
            IsActive = true,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.RoutingStrategies.Add(strategy);
        this._context.SaveChanges();

        // Assert
        var savedStrategy = this._context.RoutingStrategies
            .FirstOrDefault(s => s.Id == strategy.Id);

        savedStrategy.Should().NotBeNull();
        savedStrategy!.Name.Should().Be("Cost Optimized");
        savedStrategy.StrategyType.Should().Be("CostOptimized");
    }

    [Fact]
    public void Should_Create_ProviderHealthStatus()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var provider = new Provider
        {
            Id = Guid.NewGuid(),
            Key = "openai",
            DisplayName = "OpenAI",
            ProviderType = "OpenAI",
            IsActive = true,
        };

        var orgProvider = new OrganizationProvider
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            ProviderId = provider.Id,
            IsEnabled = true,
        };

        var healthStatus = new ProviderHealthStatus
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            OrganizationProviderId = orgProvider.Id,
            IsHealthy = true,
            HealthScore = 0.95m,
            LastCheckedAt = DateTime.UtcNow,
            AverageLatencyMs = 250,
            SuccessRate = 0.99m,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.Providers.Add(provider);
        this._context.OrganizationProviders.Add(orgProvider);
        this._context.ProviderHealthStatuses.Add(healthStatus);
        this._context.SaveChanges();

        // Assert
        var savedHealth = this._context.ProviderHealthStatuses
            .Include(h => h.OrganizationProvider)
            .FirstOrDefault(h => h.Id == healthStatus.Id);

        savedHealth.Should().NotBeNull();
        savedHealth!.IsHealthy.Should().BeTrue();
        savedHealth.HealthScore.Should().Be(0.95m);
    }

    [Fact]
    public void Should_Create_ApiKey()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Production API Key",
            KeyHash = "hashed_key_value",
            KeyPrefix = "sk_live_",
            Scopes = "read,write",
            RateLimitRpm = 1000,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.ApiKeys.Add(apiKey);
        this._context.SaveChanges();

        // Assert
        var savedKey = this._context.ApiKeys
            .FirstOrDefault(k => k.Id == apiKey.Id);

        savedKey.Should().NotBeNull();
        savedKey!.Name.Should().Be("Production API Key");
        savedKey.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Should_Create_AuditLog()
    {
        // Arrange
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Test Org",
            DisplayName = "Test",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var user = new SynaxisUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            Status = "Active",
            EmailConfirmed = true,
        };

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            Action = "Create",
            EntityType = "Organization",
            EntityId = organization.Id.ToString(),
            NewValues = "{\"name\":\"Test Org\"}",
            IpAddress = "192.168.1.1",
            UserAgent = "Mozilla/5.0",
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            PartitionDate = DateTime.UtcNow.Date,
        };

        // Act
        this._context.Organizations.Add(organization);
        this._context.Users.Add(user);
        this._context.AuditLogs.Add(auditLog);
        this._context.SaveChanges();

        // Assert
        var savedLog = this._context.AuditLogs
            .FirstOrDefault(l => l.Id == auditLog.Id);

        savedLog.Should().NotBeNull();
        savedLog!.Action.Should().Be("Create");
        savedLog.EntityType.Should().Be("Organization");
    }

    [Fact]
    public void Should_Apply_Soft_Delete_Query_Filter_On_Organizations()
    {
        // Arrange
        var activeOrg = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Active Org",
            DisplayName = "Active",
            Slug = "active-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var deletedOrg = new Organization
        {
            Id = Guid.NewGuid(),
            LegalName = "Deleted Org",
            DisplayName = "Deleted",
            Slug = "deleted-org",
            Status = "Active",
            PlanTier = "Free",
            DeletedAt = DateTime.UtcNow,
        };

        this._context.Organizations.Add(activeOrg);
        this._context.Organizations.Add(deletedOrg);
        this._context.SaveChanges();

        // Act
        var organizations = this._context.Organizations.ToList();

        // Assert
        organizations.Should().HaveCount(1);
        organizations.First().Slug.Should().Be("active-org");
    }

    [Fact]
    public void Should_Apply_Soft_Delete_Query_Filter_On_Users()
    {
        // Arrange
        var activeUser = new SynaxisUser
        {
            Id = Guid.NewGuid(),
            UserName = "active@example.com",
            Email = "active@example.com",
            Status = "Active",
            EmailConfirmed = true,
        };

        var deletedUser = new SynaxisUser
        {
            Id = Guid.NewGuid(),
            UserName = "deleted@example.com",
            Email = "deleted@example.com",
            Status = "Deactivated",
            EmailConfirmed = true,
            DeletedAt = DateTime.UtcNow,
        };

        this._context.Users.Add(activeUser);
        this._context.Users.Add(deletedUser);
        this._context.SaveChanges();

        // Act
        var users = this._context.Users.ToList();

        // Assert
        users.Should().HaveCount(1);
        users.First().Email.Should().Be("active@example.com");
    }
}
