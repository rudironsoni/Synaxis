using Synaplexer.Domain.Entities;
using Synaplexer.Domain.ValueObjects;
using FluentAssertions;

namespace Synaplexer.Domain.Tests.Entities;

public class ProviderAccountTests
{
    [Fact]
    public void Constructor_ValidInput_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var provider = ProviderType.ChatGpt;
        var email = "test@example.com";

        // Act
        var account = new ProviderAccount(id, provider, email);

        // Assert
        account.Id.Should().Be(id);
        account.Provider.Should().Be(provider);
        account.Email.Should().Be(email);
        account.IsActive.Should().BeTrue();
        account.LastUsedAt.Should().BeNull();
        account.CooldownUntil.Should().BeNull();
    }

    [Fact]
    public void CanUse_WhenActiveAndNoCooldown_ShouldReturnTrue()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");

        // Act
        var result = account.CanUse();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanUse_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");
        account.Deactivate();

        // Act
        var result = account.CanUse();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanUse_WhenInCooldown_ShouldReturnFalse()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");
        account.SetCooldown(DateTime.UtcNow.AddMinutes(5));

        // Act
        var result = account.CanUse();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanUse_WhenCooldownExpired_ShouldReturnTrue()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");
        account.SetCooldown(DateTime.UtcNow.AddMinutes(-1));

        // Act
        var result = account.CanUse();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MarkUsed_WhenCalled_ShouldUpdateLastUsedAt()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");

        // Act
        account.MarkUsed();

        // Assert
        account.LastUsedAt.Should().NotBeNull();
        account.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetCooldown_WhenCalled_ShouldUpdateCooldownUntil()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");
        var until = DateTime.UtcNow.AddHours(1);

        // Act
        account.SetCooldown(until);

        // Assert
        account.CooldownUntil.Should().Be(until);
    }

    [Fact]
    public void ActivateDeactivate_WhenCalled_ShouldUpdateIsActive()
    {
        // Arrange
        var account = new ProviderAccount(Guid.NewGuid(), ProviderType.ChatGpt, "test@example.com");

        // Act & Assert
        account.Deactivate();
        account.IsActive.Should().BeFalse();

        account.Activate();
        account.IsActive.Should().BeTrue();
    }
}
