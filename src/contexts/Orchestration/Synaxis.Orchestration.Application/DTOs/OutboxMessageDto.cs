// <copyright file="OutboxMessageDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Represents an outbox message summary.
/// </summary>
/// <param name="Id">The message identifier.</param>
/// <param name="EventType">The event type.</param>
/// <param name="Payload">The serialized payload.</param>
/// <param name="Status">The processing status.</param>
/// <param name="RetryCount">The retry count.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="ProcessedAt">The processed timestamp.</param>
/// <param name="ErrorMessage">The error message.</param>
public record OutboxMessageDto(
    Guid Id,
    string EventType,
    string Payload,
    string Status,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? ProcessedAt,
    string? ErrorMessage);
