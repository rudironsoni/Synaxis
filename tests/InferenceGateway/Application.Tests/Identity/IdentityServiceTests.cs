using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Synaxis.InferenceGateway.Application.Identity;
using Synaxis.InferenceGateway.Application.Identity.Models;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Identity;

/// <summary>
/// Unit tests for IdentityService.
/// </summary>
public class IdentityServiceTests : IDisposable
{
    private readonly SynaxisDbContext _context;
    private readonly Mock<UserManager<SynaxisUser>> _userManagerMock;
    private readonly Mock<SignInManager<SynaxisUser>> _signInManagerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        this._context = new SynaxisDbContext(options);

        // Setup UserManager mock
        var userStoreMock = new Mock<IUserStore<SynaxisUser>>();
        this._userManagerMock = new Mock<UserManager<SynaxisUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Setup SignInManager mock
        var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<SynaxisUser>>();
        this._signInManagerMock = new Mock<SignInManager<SynaxisUser>>(
            this._userManagerMock.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null, null, null, null);

        // Setup Configuration mock
        this._configurationMock = new Mock<IConfiguration>();
        this._configurationMock.Setup(c => c["Jwt:Secret"]).Returns("test-secret-key-min-32-chars-long!");
        this._configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        this._configurationMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        this._service = new IdentityService(
            this._userManagerMock.Object,
            this._signInManagerMock.Object,
            this._context,
            this._configurationMock.Object);
    }

    [Fact]
    public async Task RegisterOrganizationAsync_WithValidRequest_CreatesUserAndOrganization()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User",
            OrganizationName = "Test Organization",
            OrganizationSlug = "test-org",
        };

        this._userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((SynaxisUser?)null);

        this._userManagerMock.Setup(um => um.CreateAsync(It.IsAny<SynaxisUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        this._userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<SynaxisUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await this._service.RegisterOrganizationAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.User);
        Assert.NotNull(result.Organization);
        Assert.Equal("test@example.com", result.User.Email);
        Assert.Equal("Test Organization", result.Organization.DisplayName);

        // Verify database state
        var org = await this._context.Organizations.FirstOrDefaultAsync();
        Assert.NotNull(org);
        Assert.Equal("test-org", org.Slug);

        var group = await this._context.Groups.FirstOrDefaultAsync();
        Assert.NotNull(group);
        Assert.True(group.IsDefaultGroup);
    }

    [Fact]
    public async Task RegisterOrganizationAsync_WithDuplicateEmail_ReturnsError()
    {
        // Arrange
        var existingUser = new SynaxisUser
        {
            Email = "existing@example.com",
            UserName = "existing@example.com",
            Status = "Active",
        };

        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            OrganizationName = "Test Organization",
        };

        this._userManagerMock.Setup(um => um.FindByEmailAsync("existing@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await this._service.RegisterOrganizationAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already exists", result.Errors.First());
    }

    [Fact]
    public async Task AssignUserToOrganizationAsync_WithValidData_CreatesMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();

        var organization = new Organization
        {
            Id = organizationId,
            LegalName = "Test Org",
            DisplayName = "Test Org",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var defaultGroup = new Group
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = "Default",
            Slug = "default",
            Status = "Active",
            IsDefaultGroup = true,
        };

        this._context.Organizations.Add(organization);
        this._context.Groups.Add(defaultGroup);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._service.AssignUserToOrganizationAsync(userId, organizationId, "Member");

        // Assert
        Assert.True(result);

        var membership = await this._context.UserOrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.OrganizationId == organizationId);

        Assert.NotNull(membership);
        Assert.Equal("Member", membership.OrganizationRole);
    }

    [Fact]
    public async Task AssignUserToGroupAsync_WithValidData_CreatesMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        var organization = new Organization
        {
            Id = organizationId,
            LegalName = "Test Org",
            DisplayName = "Test Org",
            Slug = "test-org",
            Status = "Active",
            PlanTier = "Free",
        };

        var group = new Group
        {
            Id = groupId,
            OrganizationId = organizationId,
            Name = "Test Group",
            Slug = "test-group",
            Status = "Active",
        };

        this._context.Organizations.Add(organization);
        this._context.Groups.Add(group);
        await this._context.SaveChangesAsync();

        // Act
        var result = await this._service.AssignUserToGroupAsync(userId, groupId, "Member");

        // Assert
        Assert.True(result);

        var membership = await this._context.UserGroupMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId);

        Assert.NotNull(membership);
        Assert.Equal("Member", membership.GroupRole);
    }

    public void Dispose()
    {
        this._context.Database.EnsureDeleted();
        this._context.Dispose();
    }
}
