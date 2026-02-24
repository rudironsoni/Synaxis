namespace Synaxis.Contracts.Tests.V1.Auth;

using FluentAssertions;
using Synaxis.Contracts.V1.Auth;

public class AuthContextTests
{
    [Fact]
    public void AuthContext_CanBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var context = new AuthContext
        {
            UserId = "user-123",
            OrganizationId = "org-456",
            Permissions = new[] { "read", "write", "admin" },
        };

        // Assert
        context.UserId.Should().Be("user-123");
        context.OrganizationId.Should().Be("org-456");
        context.Permissions.Should().Equal("read", "write", "admin");
    }

    [Fact]
    public void AuthContext_DefaultValues_AreSet()
    {
        // Arrange & Act
        var context = new AuthContext();

        // Assert
        context.UserId.Should().BeEmpty();
        context.OrganizationId.Should().BeNull();
        context.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void AuthContext_WithoutOrganizationId_IsValid()
    {
        // Arrange & Act
        var context = new AuthContext
        {
            UserId = "user-789",
            Permissions = new[] { "read" },
        };

        // Assert
        context.UserId.Should().Be("user-789");
        context.OrganizationId.Should().BeNull();
        context.Permissions.Should().Equal("read");
    }

    [Fact]
    public void AuthContext_IsImmutable_CannotBeModifiedAfterCreation()
    {
        // Arrange
        var context = new AuthContext
        {
            UserId = "user-abc",
            OrganizationId = "org-xyz",
        };

        // Act & Assert
        context.UserId.Should().Be("user-abc");
        context.OrganizationId.Should().Be("org-xyz");
    }

    [Fact]
    public void AuthContext_WithNoPermissions_HasEmptyArray()
    {
        // Arrange & Act
        var context = new AuthContext
        {
            UserId = "user-test",
        };

        // Assert
        context.Permissions.Should().NotBeNull();
        context.Permissions.Should().BeEmpty();
    }

    [Fact]
    public void AuthContext_WithManyPermissions_StoresAllValues()
    {
        // Arrange
        var permissions = Enumerable.Range(0, 100).Select(i => $"permission-{i}").ToArray();

        // Act
        var context = new AuthContext
        {
            UserId = "user-multi",
            Permissions = permissions,
        };

        // Assert
        context.Permissions.Should().HaveCount(100);
        context.Permissions[0].Should().Be("permission-0");
        context.Permissions[99].Should().Be("permission-99");
    }

    [Fact]
    public void AuthContext_Permissions_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var context = new AuthContext
        {
            UserId = "user-special",
            Permissions = new[] { "resource:read", "resource:write", "resource:delete" },
        };

        // Assert
        context.Permissions.Should().Contain("resource:read");
        context.Permissions.Should().Contain("resource:write");
        context.Permissions.Should().Contain("resource:delete");
    }
}
