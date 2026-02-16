// <copyright file="User.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Identity.Domain.Events;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Represents a user aggregate in the Identity bounded context.
/// </summary>
public sealed class User : AggregateRoot
{
    private const int MaxFailedLoginAttempts = 5;
    private readonly List<string> _teamIds = new();

    private User()
    {
    }

    /// <summary>
    /// Gets the email address of the user.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Gets the password hash of the user.
    /// </summary>
    public PasswordHash PasswordHash { get; private set; } = null!;

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the status of the user.
    /// </summary>
    public UserStatus Status { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user's email was verified.
    /// </summary>
    public DateTime? EmailVerifiedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the user last logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// Gets the number of failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; private set; }

    /// <summary>
    /// Gets the timestamp until which the user is locked.
    /// </summary>
    public DateTime? LockedUntil { get; private set; }

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the team IDs the user belongs to.
    /// </summary>
    public IReadOnlyList<string> TeamIds => this._teamIds.AsReadOnly();

    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <param name="passwordHash">The password hash of the user.</param>
    /// <param name="firstName">The first name of the user.</param>
    /// <param name="lastName">The last name of the user.</param>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>A new <see cref="User"/> instance.</returns>
    public static User Create(
        string id,
        Email email,
        PasswordHash passwordHash,
        string firstName,
        string lastName,
        string tenantId)
    {
        var user = new User
        {
            Id = id,
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            TenantId = tenantId,
        };

        var @event = new UserCreated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserCreated),
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.TenantId);

        user.ApplyEvent(@event);

        return user;
    }

    /// <summary>
    /// Changes the user's password.
    /// </summary>
    /// <param name="newPasswordHash">The new password hash.</param>
    public void ChangePassword(PasswordHash newPasswordHash)
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot change password for a deleted user.");
        }

        this.PasswordHash = newPasswordHash;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserPasswordChanged(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserPasswordChanged),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Verifies the user's email.
    /// </summary>
    public void VerifyEmail()
    {
        if (this.EmailVerifiedAt.HasValue)
        {
            throw new InvalidOperationException("Email is already verified.");
        }

        this.EmailVerifiedAt = DateTime.UtcNow;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserEmailVerified(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserEmailVerified),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Suspends the user.
    /// </summary>
    public void Suspend()
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot suspend a deleted user.");
        }

        if (this.Status == UserStatus.Suspended)
        {
            throw new InvalidOperationException("User is already suspended.");
        }

        this.Status = UserStatus.Suspended;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserSuspended(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserSuspended),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Activates the user.
    /// </summary>
    public void Activate()
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot activate a deleted user.");
        }

        if (this.Status == UserStatus.Active)
        {
            throw new InvalidOperationException("User is already active.");
        }

        this.Status = UserStatus.Active;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserActivated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserActivated),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Locks the user.
    /// </summary>
    /// <param name="duration">The duration of the lock.</param>
    public void Lock(TimeSpan duration)
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot lock a deleted user.");
        }

        this.LockedUntil = DateTime.UtcNow.Add(duration);
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserLocked(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserLocked),
            this.Id,
            this.LockedUntil.Value);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Unlocks the user.
    /// </summary>
    public void Unlock()
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot unlock a deleted user.");
        }

        this.LockedUntil = null;
        this.FailedLoginAttempts = 0;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserUnlocked(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserUnlocked),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Records a failed login attempt.
    /// </summary>
    public void RecordFailedLoginAttempt()
    {
        this.FailedLoginAttempts++;

        if (this.FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            this.Lock(TimeSpan.FromMinutes(30));
        }
    }

    /// <summary>
    /// Records a successful login.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        this.LastLoginAt = DateTime.UtcNow;
        this.FailedLoginAttempts = 0;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the user's profile.
    /// </summary>
    /// <param name="firstName">The new first name.</param>
    /// <param name="lastName">The new last name.</param>
    public void UpdateProfile(string firstName, string lastName)
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot update profile for a deleted user.");
        }

        this.FirstName = firstName;
        this.LastName = lastName;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserUpdated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserUpdated),
            this.Id,
            this.FirstName,
            this.LastName);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deletes the user.
    /// </summary>
    public void Delete()
    {
        if (this.Status == UserStatus.Deleted)
        {
            throw new InvalidOperationException("User is already deleted.");
        }

        this.Status = UserStatus.Deleted;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserDeleted(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserDeleted),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Applies a domain event to update the aggregate state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case UserCreated:
                this.CreatedAt = @event.OccurredOn;
                this.UpdatedAt = @event.OccurredOn;
                break;
            case UserUpdated:
            case UserPasswordChanged:
            case UserEmailVerified:
            case UserSuspended:
            case UserActivated:
            case UserLocked:
            case UserUnlocked:
                this.UpdatedAt = @event.OccurredOn;
                break;
            case UserDeleted:
                this.Status = UserStatus.Deleted;
                this.UpdatedAt = @event.OccurredOn;
                break;
        }
    }
}
