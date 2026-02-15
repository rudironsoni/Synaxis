// <copyright file="ISemanticCacheService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

using Microsoft.Extensions.AI;

/// <summary>
/// Interface for semantic cache service.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ISemanticCacheService
{
    Task<SemanticCacheResult> TryGetCachedAsync(
        string query,
        string sessionId,
        string model,
        string tenantId,
        float? temperature,
        CancellationToken cancellationToken);

    Task StoreAsync(
        string query,
        string response,
        string sessionId,
        string model,
        string tenantId,
        float? temperature,
        float[]? embedding,
        CancellationToken cancellationToken);
}
