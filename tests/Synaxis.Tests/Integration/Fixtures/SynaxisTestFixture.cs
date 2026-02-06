using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Tests.Integration.Fixtures;

/// <summary>
/// Test fixture for setting up in-memory database and test data.
/// </summary>
public class SynaxisTestFixture : IDisposable
{
    public SynaxisDbContext DbContext { get; }
    
    public Organization TestOrganization { get; private set; } = null!;
    public Team TestTeam { get; private set; } = null!;
    public User EuUser { get; private set; } = null!;
    public User UsUser { get; private set; } = null!;
    public User BrazilUser { get; private set; } = null!;
    public VirtualKey EuApiKey { get; private set; } = null!;
    public VirtualKey UsApiKey { get; private set; } = null!;
    
    public SynaxisTestFixture()
    {
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: $"SynaxisTest_{Guid.NewGuid()}")
            .Options;
            
        DbContext = new SynaxisDbContext(options);
        
        SeedTestDataAsync().GetAwaiter().GetResult();
    }
    
    private async Task SeedTestDataAsync()
    {
        // Create test organization
        TestOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Slug = "test-org",
            Name = "Test Organization",
            PrimaryRegion = "eu-west-1",
            AvailableRegions = new List<string> { "eu-west-1", "us-east-1", "sa-east-1" },
            Tier = "pro",
            BillingCurrency = "USD",
            CreditBalance = 100.00m,
            MaxConcurrentRequests = 100,
            MonthlyRequestLimit = 1000000,
            MonthlyTokenLimit = 10000000,
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Organizations.Add(TestOrganization);
        
        // Create test team
        TestTeam = new Team
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            Slug = "test-team",
            Name = "Test Team",
            MonthlyBudget = 500.00m,
            BudgetAlertThreshold = 80.00m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Teams.Add(TestTeam);
        
        // Create EU user
        EuUser = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            Email = "eu-user@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#"),
            FirstName = "EU",
            LastName = "User",
            DataResidencyRegion = "eu-west-1",
            CreatedInRegion = "eu-west-1",
            Role = "member",
            CrossBorderConsentGiven = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Users.Add(EuUser);
        
        // Create US user
        UsUser = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            Email = "us-user@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#"),
            FirstName = "US",
            LastName = "User",
            DataResidencyRegion = "us-east-1",
            CreatedInRegion = "us-east-1",
            Role = "member",
            CrossBorderConsentGiven = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Users.Add(UsUser);
        
        // Create Brazil user with consent
        BrazilUser = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            Email = "br-user@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#"),
            FirstName = "Brazil",
            LastName = "User",
            DataResidencyRegion = "sa-east-1",
            CreatedInRegion = "sa-east-1",
            Role = "member",
            CrossBorderConsentGiven = true,
            CrossBorderConsentDate = DateTime.UtcNow,
            CrossBorderConsentVersion = "1.0",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Users.Add(BrazilUser);
        
        // Create API keys
        EuApiKey = new VirtualKey
        {
            Id = Guid.NewGuid(),
            KeyHash = BCrypt.Net.BCrypt.HashPassword("synaxis_eu_test_key_123456789"),
            OrganizationId = TestOrganization.Id,
            TeamId = TestTeam.Id,
            CreatedBy = EuUser.Id,
            Name = "EU API Key",
            IsActive = true,
            IsRevoked = false,
            MaxBudget = 100.00m,
            CurrentSpend = 10.00m,
            RpmLimit = 60,
            TpmLimit = 100000,
            UserRegion = "eu-west-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.VirtualKeys.Add(EuApiKey);
        
        UsApiKey = new VirtualKey
        {
            Id = Guid.NewGuid(),
            KeyHash = BCrypt.Net.BCrypt.HashPassword("synaxis_us_test_key_123456789"),
            OrganizationId = TestOrganization.Id,
            TeamId = TestTeam.Id,
            CreatedBy = UsUser.Id,
            Name = "US API Key",
            IsActive = true,
            IsRevoked = false,
            MaxBudget = 100.00m,
            CurrentSpend = 5.00m,
            RpmLimit = 60,
            TpmLimit = 100000,
            UserRegion = "us-east-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.VirtualKeys.Add(UsApiKey);
        
        await DbContext.SaveChangesAsync();
    }
    
    public async Task<VirtualKey> CreateApiKeyWithQuotaAsync(decimal maxBudget, decimal currentSpend, int? rpmLimit = null)
    {
        var apiKey = new VirtualKey
        {
            Id = Guid.NewGuid(),
            KeyHash = BCrypt.Net.BCrypt.HashPassword($"synaxis_test_key_{Guid.NewGuid()}"),
            OrganizationId = TestOrganization.Id,
            TeamId = TestTeam.Id,
            CreatedBy = EuUser.Id,
            Name = $"Test Key {Guid.NewGuid()}",
            IsActive = true,
            IsRevoked = false,
            MaxBudget = maxBudget,
            CurrentSpend = currentSpend,
            RpmLimit = rpmLimit,
            TpmLimit = 100000,
            UserRegion = "eu-west-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.VirtualKeys.Add(apiKey);
        await DbContext.SaveChangesAsync();
        
        return apiKey;
    }
    
    public async Task<User> CreateUserWithConsentAsync(string region, bool crossBorderConsent)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = TestOrganization.Id,
            Email = $"user-{Guid.NewGuid()}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!@#"),
            FirstName = "Test",
            LastName = "User",
            DataResidencyRegion = region,
            CreatedInRegion = region,
            Role = "member",
            CrossBorderConsentGiven = crossBorderConsent,
            CrossBorderConsentDate = crossBorderConsent ? DateTime.UtcNow : null,
            CrossBorderConsentVersion = crossBorderConsent ? "1.0" : null,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        
        return user;
    }
    
    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
