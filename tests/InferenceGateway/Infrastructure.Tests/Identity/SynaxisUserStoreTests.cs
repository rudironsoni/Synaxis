using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Synaxis.InferenceGateway.Infrastructure.Identity;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity;

/// <summary>
/// Unit tests for SynaxisUserStore.
/// </summary>
public class SynaxisUserStoreTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly SynaxisUserStore _store;

    public SynaxisUserStoreTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SynaxisDbContext(options);
        _store = new SynaxisUserStore(_context);
    }

    [Fact]
    public async Task FindByEmailInOrganizationAsync_WithValidData_ReturnsUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var user = new SynaxisUser
        {
            Id = userId,
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            UserName = "test@example.com",
            Status = "Active"
        };

        var org = new Organization
        {
            Id = orgId,
            LegalName = "Test",
            DisplayName = "Test",
            Slug = "test",
            Status = "Active",
            PlanTier = "Free"
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            OrganizationRole = "Member",
            Status = "Active"
        };

        _context.Users.Add(user);
        _context.Organizations.Add(org);
        _context.UserOrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _store.FindByEmailInOrganizationAsync("test@example.com", orgId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
    }

    [Fact]
    public async Task FindByEmailInOrganizationAsync_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        var user = new SynaxisUser
        {
            Id = userId,
            Email = "deleted@example.com",
            NormalizedEmail = "DELETED@EXAMPLE.COM",
            UserName = "deleted@example.com",
            Status = "Deactivated",
            DeletedAt = DateTime.UtcNow
        };

        var org = new Organization
        {
            Id = orgId,
            LegalName = "Test",
            DisplayName = "Test",
            Slug = "test",
            Status = "Active",
            PlanTier = "Free"
        };

        var membership = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = orgId,
            OrganizationRole = "Member",
            Status = "Active"
        };

        _context.Users.Add(user);
        _context.Organizations.Add(org);
        _context.UserOrganizationMemberships.Add(membership);
        await _context.SaveChangesAsync();

        // Act
        var result = await _store.FindByEmailInOrganizationAsync("deleted@example.com", orgId);

        // Assert - should be null because user is soft deleted
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrganizationsAsync_ReturnsUserOrganizations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var org1Id = Guid.NewGuid();
        var org2Id = Guid.NewGuid();

        var user = new SynaxisUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "test@example.com",
            Status = "Active"
        };

        var org1 = new Organization
        {
            Id = org1Id,
            LegalName = "Org 1",
            DisplayName = "Org 1",
            Slug = "org-1",
            Status = "Active",
            PlanTier = "Free"
        };

        var org2 = new Organization
        {
            Id = org2Id,
            LegalName = "Org 2",
            DisplayName = "Org 2",
            Slug = "org-2",
            Status = "Active",
            PlanTier = "Free"
        };

        var membership1 = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = org1Id,
            OrganizationRole = "Owner",
            Status = "Active"
        };

        var membership2 = new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrganizationId = org2Id,
            OrganizationRole = "Member",
            Status = "Active"
        };

        _context.Users.Add(user);
        _context.Organizations.AddRange(org1, org2);
        _context.UserOrganizationMemberships.AddRange(membership1, membership2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _store.GetOrganizationsAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Id == org1Id);
        Assert.Contains(result, o => o.Id == org2Id);
    }

    [Fact]
    public async Task FindByIdAsync_WithDeletedUser_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var user = new SynaxisUser
        {
            Id = userId,
            Email = "deleted@example.com",
            UserName = "deleted@example.com",
            Status = "Deactivated",
            DeletedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _store.FindByIdAsync(userId.ToString());

        // Assert
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _store.Dispose();
    }
}
