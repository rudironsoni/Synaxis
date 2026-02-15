// <copyright file="IInFlightDeduplicationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

using Microsoft.Extensions.AI;

/// <summary>
/// Interface for in-flight request deduplication.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IInFlightDeduplicationService
{
    Task<ChatResponse?> TryGetInFlightAsync(string fingerprint, CancellationToken cancellationToken);

    Task RegisterInFlightAsync(string fingerprint, Task<ChatResponse> responseTask, CancellationToken cancellationToken);
}
