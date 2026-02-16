// <copyright file="ApiKey.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

using Synaxis.Abstractions.Cloud;
using Synaxis.Identity.Domain.Events;
using Synaxis.Identity.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Represents an API key aggregate in the Identity bounded context.
/// </summary>
public sealed class ApiKey : AggregateRoot
{
    private ApiKey()
    {
    }

    /// <summary>
    /// Gets the key identifier.
    /// </summary>
    public KeyId KeyId { get; private set; } = null!;

    /// <summary>
    /// Gets the encrypted key value.
    /// </summary>
    public string EncryptedKey { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the type of the key provider.
    /// </summary>
    public string ProviderType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the API key is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the timestamp when the API key was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the expiration timestamp of the API key.
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the API key was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; private set; }

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="id">The unique identifier of the API key.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="encryptedKey">The encrypted key value.</param>
    /// <param name="providerType">The type of the key provider.</param>
    /// <param name="tenantId">The unique identifier of the tenant.</param>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="expiresAt">The expiration timestamp of the key.</param>
    /// <returns>A new <see cref="ApiKey"/> instance.</returns>
    public static ApiKey Create(
        string id,
        KeyId keyId,
        string encryptedKey,
        string providerType,
        string tenantId,
        string userId,
        DateTime? expiresAt)
    {
        var apiKey = new ApiKey
        {
            Id = id,
            KeyId = keyId,
            EncryptedKey = encryptedKey,
            ProviderType = providerType,
            TenantId = tenantId,
            UserId = userId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        };

        var @event = new ApiKeyAdded(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(ApiKeyAdded),
            apiKey.Id,
            apiKey.KeyId.Value,
            apiKey.ProviderType,
            apiKey.TenantId,
            apiKey.UserId,
            apiKey.ExpiresAt);

        apiKey.ApplyEvent(@event);

        return apiKey;
    }

    /// <summary>
    /// Rotates the API key.
    /// </summary>
    /// <param name="newKeyId">The new key identifier.</param>
    /// <param name="newEncryptedKey">The new encrypted key value.</param>
    public void Rotate(KeyId newKeyId, string newEncryptedKey)
    {
        if (!this.IsActive)
        {
            throw new InvalidOperationException("Cannot rotate an inactive API key.");
        }

        this.KeyId = newKeyId;
        this.EncryptedKey = newEncryptedKey;

        var @event = new ApiKeyRotated(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(ApiKeyRotated),
            this.Id,
            this.KeyId.Value);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Revokes the API key.
    /// </summary>
    public void Revoke()
    {
        if (!this.IsActive)
        {
            throw new InvalidOperationException("API key is already revoked.");
        }

        this.IsActive = false;

        var @event = new ApiKeyRevoked(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(ApiKeyRevoked),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Marks the API key as used.
    /// </summary>
    public void MarkAsUsed()
    {
        if (!this.IsActive)
        {
            throw new InvalidOperationException("Cannot mark an inactive API key as used.");
        }

        if (this.ExpiresAt.HasValue && this.ExpiresAt.Value < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Cannot mark an expired API key as used.");
        }

        this.LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deletes the API key.
    /// </summary>
    public void Delete()
    {
        var @event = new ApiKeyDeleted(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            nameof(ApiKeyDeleted),
            this.Id);

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Checks if the API key is expired.
    /// </summary>
    /// <returns>True if the API key is expired, otherwise false.</returns>
    public bool IsExpired()
    {
        return this.ExpiresAt.HasValue && this.ExpiresAt.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the API key is valid.
    /// </summary>
    /// <returns>True if the API key is valid, otherwise false.</returns>
    public bool IsValid()
    {
        return this.IsActive && !this.IsExpired();
    }

    /// <summary>
    /// Applies a domain event to update the aggregate state.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case ApiKeyAdded:
                this.CreatedAt = @event.OccurredOn;
                break;
            case ApiKeyRotated:
            case ApiKeyRevoked:
                break;
            case ApiKeyDeleted:
                this.IsActive = false;
                break;
        }
    }
}
