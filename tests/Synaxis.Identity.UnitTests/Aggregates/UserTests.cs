// <copyright file="UserTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.UnitTests.Aggregates;

using Synaxis.Common.Tests.Time;
using Synaxis.Identity.Domain.Aggregates;
using Synaxis.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

[Trait("Category", "Unit")]
public class UserTests
{
    [Fact]
    public void Constructor_ValidData_CreatesUser()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var email = Email.Create("test@example.com");
        var passwordHash = PasswordHash.Create("hashedpassword");
        var firstName = "John";
        var lastName = "Doe";
        var tenantId = Guid.NewGuid().ToString();
        var timeProvider = new TestTimeProvider();

        // Act
        var user = User.Create(id, email, passwordHash, firstName, lastName, tenantId, timeProvider);

        // Assert
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.TenantId.Should().Be(tenantId);
        user.Status.Should().Be(UserStatus.Active);
        user.CreatedAt.Should().Be(timeProvider.UtcNow);
        user.UpdatedAt.Should().Be(timeProvider.UtcNow);
        user.EmailVerifiedAt.Should().BeNull();
        user.LastLoginAt.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.TeamIds.Should().BeEmpty();
    }

    [Fact]
    public void ChangePassword_ValidPassword_UpdatesHash()
    {
        // Arrange
        var user = CreateTestUser();
        var newPasswordHash = PasswordHash.Create("newhashedpassword");

        // Act
        user.ChangePassword(newPasswordHash);

        // Assert
        user.PasswordHash.Should().Be(newPasswordHash);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangePassword_DeletedUser_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Delete();
        var newPasswordHash = PasswordHash.Create("newhashedpassword");

        // Act
        var act = () => user.ChangePassword(newPasswordHash);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot change password for a deleted user.");
    }

    [Fact]
    public void VerifyEmail_SetsEmailVerifiedAt()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.VerifyEmail();

        // Assert
        user.EmailVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void VerifyEmail_AlreadyVerified_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.VerifyEmail();

        // Act
        var act = () => user.VerifyEmail();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Email is already verified.");
    }

    [Fact]
    public void Lock_Account_LocksUser()
    {
        // Arrange
        var user = CreateTestUser();
        var duration = TimeSpan.FromMinutes(30);

        // Act
        user.Lock(duration);

        // Assert
        user.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.Add(duration), TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Lock_DeletedUser_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Delete();

        // Act
        var act = () => user.Lock(TimeSpan.FromMinutes(30));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot lock a deleted user.");
    }

    [Fact]
    public void Unlock_ResetsLockAndFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.Lock(TimeSpan.FromMinutes(30));
        user.RecordFailedLoginAttempt();

        // Act
        user.Unlock();

        // Assert
        user.LockedUntil.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Unlock_DeletedUser_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Delete();

        // Act
        var act = () => user.Unlock();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot unlock a deleted user.");
    }

    [Fact]
    public void RecordFailedLoginAttempt_IncrementsCounter()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLoginAttempt();

        // Assert
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public void RecordFailedLoginAttempt_ExceedsMax_LocksAccount()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLoginAttempt();
        }

        // Assert
        user.FailedLoginAttempts.Should().Be(5);
        user.LockedUntil.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLoginAttempt();
        user.RecordFailedLoginAttempt();

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Suspend_SetsStatusToSuspended()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Suspend();

        // Assert
        user.Status.Should().Be(UserStatus.Suspended);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Suspend_AlreadySuspended_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Suspend();

        // Act
        var act = () => user.Suspend();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already suspended.");
    }

    [Fact]
    public void Activate_SetsStatusToActive()
    {
        // Arrange
        var user = CreateTestUser();
        user.Suspend();

        // Act
        user.Activate();

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_AlreadyActive_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var act = () => user.Activate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already active.");
    }

    [Fact]
    public void UpdateProfile_UpdatesProperties()
    {
        // Arrange
        var user = CreateTestUser();
        var newFirstName = "Jane";
        var newLastName = "Smith";

        // Act
        user.UpdateProfile(newFirstName, newLastName);

        // Assert
        user.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Delete_SetsStatusToDeleted()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.Delete();

        // Assert
        user.Status.Should().Be(UserStatus.Deleted);
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Delete_AlreadyDeleted_ThrowsException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Delete();

        // Act
        var act = () => user.Delete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already deleted.");
    }

    private static User CreateTestUser()
    {
        return User.Create(
            Guid.NewGuid().ToString(),
            Email.Create("test@example.com"),
            PasswordHash.Create("hashedpassword"),
            "John",
            "Doe",
            Guid.NewGuid().ToString(),
            new TestTimeProvider());
    }
}
