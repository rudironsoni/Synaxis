// <copyright file="TenantTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.UnitTests.Aggregates;

using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

[Trait("Category", "Unit")]
public class TenantTests
{
    [Fact]
    public void Provision_ValidData_CreatesTenant()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = TenantName.Create("Test Tenant");
        var slug = "test-tenant";
        var primaryRegion = "eastus";
        var timeProvider = new TestTimeProvider();

        // Act
        var tenant = Tenant.Provision(id, name, slug, primaryRegion, timeProvider);

        // Assert
        tenant.Id.Should().Be(id);
        tenant.Name.Should().Be(name);
        tenant.Slug.Should().Be(slug);
        tenant.PrimaryRegion.Should().Be(primaryRegion);
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tenant.Settings.Should().BeEmpty();
    }

    [Fact]
    public void UpdateSettings_UpdatesProperties()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var newSettings = new Dictionary<string, string>
        {
            ["setting1"] = "value1",
            ["setting2"] = "value2"
        };

        // Act
        tenant.UpdateSettings(newSettings);

        // Assert
        tenant.Settings.Should().BeEquivalentTo(newSettings);
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateSettings_DeletedTenant_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Delete();
        var newSettings = new Dictionary<string, string>
        {
            ["setting1"] = "value1"
        };

        // Act
        var act = () => tenant.UpdateSettings(newSettings);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update settings for a deleted tenant.");
    }

    [Fact]
    public void UpdateProfile_UpdatesProperties()
    {
        // Arrange
        var tenant = CreateTestTenant();
        var newName = TenantName.Create("Updated Tenant");
        var newRegion = "westus";

        // Act
        tenant.UpdateProfile(newName, newRegion);

        // Assert
        tenant.Name.Should().Be(newName);
        tenant.PrimaryRegion.Should().Be(newRegion);
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateProfile_DeletedTenant_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Delete();
        var newName = TenantName.Create("Updated Tenant");

        // Act
        var act = () => tenant.UpdateProfile(newName, "westus");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update profile for a deleted tenant.");
    }

    [Fact]
    public void Suspend_SetsStatusToSuspended()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        tenant.Suspend();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Suspended);
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Suspend_AlreadySuspended_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Suspend();

        // Act
        var act = () => tenant.Suspend();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Tenant is already suspended.");
    }

    [Fact]
    public void Suspend_DeletedTenant_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Delete();

        // Act
        var act = () => tenant.Suspend();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot suspend a deleted tenant.");
    }

    [Fact]
    public void Activate_SetsStatusToActive()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Suspend();

        // Act
        tenant.Activate();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Active);
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_AlreadyActive_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        var act = () => tenant.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Tenant is already active.");
    }

    [Fact]
    public void Activate_DeletedTenant_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Delete();

        // Act
        var act = () => tenant.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot activate a deleted tenant.");
    }

    [Fact]
    public void Delete_SetsStatusToDeleted()
    {
        // Arrange
        var tenant = CreateTestTenant();

        // Act
        tenant.Delete();

        // Assert
        tenant.Status.Should().Be(TenantStatus.Deleted);
        tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Delete_AlreadyDeleted_ThrowsException()
    {
        // Arrange
        var tenant = CreateTestTenant();
        tenant.Delete();

        // Act
        var act = () => tenant.Delete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Tenant is already deleted.");
    }

    private static Tenant CreateTestTenant()
    {
        var timeProvider = new TestTimeProvider();
        return Tenant.Provision(
            Guid.NewGuid().ToString(),
            TenantName.Create("Test Tenant"),
            "test-tenant",
            "eastus",
            timeProvider);
    }
}
