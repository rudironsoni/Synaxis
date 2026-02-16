// <copyright file="Tenant.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Identity.Domain.Events;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Represents a tenant aggregate in the Identity bounded context.
/// </summary>
public sealed class Tenant : AggregateRoot
{
    private Tenant()
    {
    }

    /// <summary>
    /// Gets the name of the tenant.
    /// </summary>
    public TenantName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the slug of the tenant.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the status of the tenant.
    /// </summary>
    public TenantStatus Status { get; private set; }

    /// <summary>
    /// Gets the primary region of the tenant.
    /// </summary>
    public string PrimaryRegion { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the settings of the tenant.
    /// </summary>
    public IDictionary<string, string> Settings { get; private set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the timestamp when the tenant was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the tenant was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="id">The unique identifier of the tenant.</param>
    /// <param name="name">The name of the tenant.</param>
    /// <param name="slug">The slug of the tenant.</param>
    /// <param name="primaryRegion">The primary region of the tenant.</param>
    /// <returns>A new <see cref="Tenant"/> instance.</returns>
    public static Tenant Provision(
        string id,
        TenantName name,
        string slug,
        string primaryRegion)
    {
        var tenant = new Tenant
        {
            Id = id,
            Name = name,
            Slug = slug,
            Status = TenantStatus.Active,
            PrimaryRegion = primaryRegion,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var @event = new TenantProvisioned(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantProvisioned),
            tenant.Id,
            tenant.Name.Value,
            tenant.Slug,
            tenant.PrimaryRegion);

        tenant.ApplyEvent(@event);

        return tenant;
    }

    /// <summary>
    /// Updates the tenant settings.
    /// </summary>
    /// <param name="settings">The new settings.</param>
    public void UpdateSettings(IDictionary<string, string> settings)
    {
        if (this.Status == TenantStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot update settings for a deleted tenant.");
        }

        this.Settings = new Dictionary<string, string>(settings, StringComparer.Ordinal);
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TenantUpdated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantUpdated),
            this.Id,
            this.Name.Value,
            this.PrimaryRegion);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Updates the tenant profile.
    /// </summary>
    /// <param name="name">The new name of the tenant.</param>
    /// <param name="primaryRegion">The new primary region of the tenant.</param>
    public void UpdateProfile(TenantName name, string primaryRegion)
    {
        if (this.Status == TenantStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot update profile for a deleted tenant.");
        }

        this.Name = name;
        this.PrimaryRegion = primaryRegion;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TenantUpdated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantUpdated),
            this.Id,
            this.Name.Value,
            this.PrimaryRegion);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Suspends the tenant.
    /// </summary>
    public void Suspend()
    {
        if (this.Status == TenantStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot suspend a deleted tenant.");
        }

        if (this.Status == TenantStatus.Suspended)
        {
            throw new InvalidOperationException("Tenant is already suspended.");
        }

        this.Status = TenantStatus.Suspended;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TenantSuspended(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantSuspended),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Activates the tenant.
    /// </summary>
    public void Activate()
    {
        if (this.Status == TenantStatus.Deleted)
        {
            throw new InvalidOperationException("Cannot activate a deleted tenant.");
        }

        if (this.Status == TenantStatus.Active)
        {
            throw new InvalidOperationException("Tenant is already active.");
        }

        this.Status = TenantStatus.Active;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TenantActivated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantActivated),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deletes the tenant.
    /// </summary>
    public void Delete()
    {
        if (this.Status == TenantStatus.Deleted)
        {
            throw new InvalidOperationException("Tenant is already deleted.");
        }

        this.Status = TenantStatus.Deleted;
        this.UpdatedAt = DateTime.UtcNow;

        var @event = new TenantDeleted(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(TenantDeleted),
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
            case TenantProvisioned:
                this.CreatedAt = @event.OccurredOn;
                this.UpdatedAt = @event.OccurredOn;
                break;
            case TenantUpdated:
            case TenantSuspended:
            case TenantActivated:
                this.UpdatedAt = @event.OccurredOn;
                break;
            case TenantDeleted:
                this.Status = TenantStatus.Deleted;
                this.UpdatedAt = @event.OccurredOn;
                break;
        }
    }
}
