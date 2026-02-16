// <copyright file="Team.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Identity.Domain.Events;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Represents a team aggregate in the Identity bounded context.
/// </summary>
public sealed class Team : AggregateRoot
{
    private readonly Dictionary<string, Role> _members = new(StringComparer.Ordinal);

    private Team()
    {
    }

    /// <summary>
    /// Gets the name of the team.
    /// </summary>
    public TeamName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the description of the team.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the team was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the team was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Gets the members of the team.
    /// </summary>
    public IReadOnlyDictionary<string, Role> Members => this._members.AsReadOnly();

    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="id">The unique identifier of the team.</param>
    /// <param name="name">The name of the team.</param>
    /// <param name="description">The description of the team.</param>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <returns>A new <see cref="Team"/> instance.</returns>
    public static Team Create(
        string id,
        TeamName name,
        string description,
        string tenantId)
    {
        var team = new Team
        {
            Id = id,
            Name = name,
            Description = description,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var @event = new TeamCreated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TeamCreated),
            team.Id,
            team.Name.Value,
            team.Description,
            team.TenantId);

        team.ApplyEvent(@event);

        return team;
    }

    /// <summary>
    /// Updates the team profile.
    /// </summary>
    /// <param name="name">The new name of the team.</param>
    /// <param name="description">The new description of the team.</param>
    public void UpdateProfile(TeamName name, string description)
    {
        this.Name = name;
        this.Description = description;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TeamUpdated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TeamUpdated),
            this.Id,
            this.Name.Value,
            this.Description);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Adds a member to the team.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="role">The role of the user in the team.</param>
    public void AddMember(string userId, Role role)
    {
        if (this._members.ContainsKey(userId))
        {
            throw new InvalidOperationException($"User {userId} is already a member of the team.");
        }

        this._members[userId] = role;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserAddedToTeam(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserAddedToTeam),
            this.Id,
            userId,
            role.ToString());

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Removes a member from the team.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    public void RemoveMember(string userId)
    {
        if (!this._members.ContainsKey(userId))
        {
            throw new InvalidOperationException($"User {userId} is not a member of the team.");
        }

        this._members.Remove(userId);
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new UserRemovedFromTeam(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(UserRemovedFromTeam),
            this.Id,
            userId);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the role of a member.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="role">The new role of the user.</param>
    public void UpdateMemberRole(string userId, Role role)
    {
        if (!this._members.ContainsKey(userId))
        {
            throw new InvalidOperationException($"User {userId} is not a member of the team.");
        }

        this._members[userId] = role;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Archives the team.
    /// </summary>
    public void Archive()
    {
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TeamArchived(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TeamArchived),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Checks if a user is a member of the team.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>True if the user is a member, otherwise false.</returns>
    public bool IsMember(string userId)
    {
        return this._members.ContainsKey(userId);
    }

    /// <summary>
    /// Gets the role of a user in the team.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The role of the user.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user is not a member of the team.</exception>
    public Role GetMemberRole(string userId)
    {
        if (!this._members.TryGetValue(userId, out var role))
        {
            throw new InvalidOperationException($"User {userId} is not a member of the team.");
        }

        return role;
    }

    /// <summary>
    /// Applies a domain event to update the aggregate state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case TeamCreated:
                this.CreatedAt = @event.OccurredOn;
                this.UpdatedAt = @event.OccurredOn;
                break;
            case TeamUpdated:
            case TeamArchived:
            case UserAddedToTeam:
            case UserRemovedFromTeam:
                this.UpdatedAt = @event.OccurredOn;
                break;
        }
    }
}
