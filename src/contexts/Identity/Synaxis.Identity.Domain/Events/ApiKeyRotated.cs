// <copyright file="ApiKeyRotated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an API key is rotated.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="KeyId">The unique identifier of the API key.</param>
/// <param name="NewKeyIdValue">The new key identifier value.</param>
public sealed record ApiKeyRotated(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string KeyId,
    string NewKeyIdValue) : IDomainEvent;
