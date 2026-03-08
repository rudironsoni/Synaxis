// <copyright file="AuditLogDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Services.Audit;

using System;
using System.Collections.Generic;

/// <summary>
/// Data transfer object for an audit log entry.
/// </summary>
/// <param name="Id">The unique identifier.</param>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="UserId">The user identifier (if applicable).</param>
/// <param name="EventType">The event type.</param>
/// <param name="EventCategory">The event category.</param>
/// <param name="Action">The action performed.</param>
/// <param name="ResourceType">The resource type.</param>
/// <param name="ResourceId">The resource identifier.</param>
/// <param name="Metadata">Additional event metadata.</param>
/// <param name="IpAddress">The IP address.</param>
/// <param name="UserAgent">The user agent string.</param>
/// <param name="Region">The region where the event occurred.</param>
/// <param name="Timestamp">The timestamp when the event occurred.</param>
public record AuditLogDto(
    Guid Id,
    Guid OrganizationId,
    Guid? UserId,
    string EventType,
    string EventCategory,
    string Action,
    string ResourceType,
    string? ResourceId,
    IDictionary<string, object>? Metadata,
    string? IpAddress,
    string? UserAgent,
    string? Region,
    DateTime Timestamp);
