// <copyright file="ApiKeyAdded.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new API key is added.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="KeyId">The unique identifier of the API key.</param>
/// <param name="KeyIdValue">The key identifier value.</param>
/// <param name="ProviderType">The type of the key provider.</param>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="ExpiresAt">The expiration timestamp of the key.</param>
public sealed record ApiKeyAdded(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string KeyId,
    string KeyIdValue,
    string ProviderType,
    string TenantId,
    string UserId,
    DateTime? ExpiresAt) : IDomainEvent;
