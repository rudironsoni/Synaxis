namespace Synaxis.Core.Authorization.Tests;

using FluentAssertions;
using Xunit;

[Trait("Category", "Unit")]
public class AuthorizationPoliciesTests
{
    [Fact]
    public void OrgAdminConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.OrgAdminPolicy.Should().Be("OrgAdmin");
    }

    [Fact]
    public void TeamAdminConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.TeamAdminPolicy.Should().Be("TeamAdmin");
    }

    [Fact]
    public void MemberConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.MemberPolicy.Should().Be("Member");
    }

    [Fact]
    public void ViewerConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.ViewerPolicy.Should().Be("Viewer");
    }

    [Fact]
    public void Roles_OrgAdminConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.Roles.OrgAdmin.Should().Be("OrgAdmin");
    }

    [Fact]
    public void Roles_TeamAdminConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.Roles.TeamAdmin.Should().Be("TeamAdmin");
    }

    [Fact]
    public void Roles_MemberConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.Roles.Member.Should().Be("Member");
    }

    [Fact]
    public void Roles_ViewerConstant_ShouldHaveCorrectValue()
    {
        // Assert
        AuthorizationPolicies.Roles.Viewer.Should().Be("Viewer");
    }

    [Theory]
    [InlineData("OrgAdmin")]
    [InlineData("TeamAdmin")]
    [InlineData("Member")]
    [InlineData("Viewer")]
    public void RootAndNestedConstants_ShouldMatch(string roleName)
    {
        // This ensures no magic strings - both root and nested have same values
        switch (roleName)
        {
            case "OrgAdmin":
                AuthorizationPolicies.OrgAdminPolicy.Should().Be(AuthorizationPolicies.Roles.OrgAdmin);
                break;
            case "TeamAdmin":
                AuthorizationPolicies.TeamAdminPolicy.Should().Be(AuthorizationPolicies.Roles.TeamAdmin);
                break;
            case "Member":
                AuthorizationPolicies.MemberPolicy.Should().Be(AuthorizationPolicies.Roles.Member);
                break;
            case "Viewer":
                AuthorizationPolicies.ViewerPolicy.Should().Be(AuthorizationPolicies.Roles.Viewer);
                break;
        }
    }

    [Fact]
    public void AuthorizationPolicies_ShouldBeStaticClass()
    {
        // Assert - verify the type is static
        var type = typeof(AuthorizationPolicies);
        type.IsAbstract.Should().BeTrue("static classes are abstract and sealed");
        type.IsSealed.Should().BeTrue("static classes are abstract and sealed");
    }
}
