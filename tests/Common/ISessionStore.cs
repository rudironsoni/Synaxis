// <copyright file="ISessionStore.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for managing session affinity.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ISessionStore
{
    Task<string?> GetPreferredProviderAsync(string sessionId, CancellationToken cancellationToken);

    Task SetPreferredProviderAsync(string sessionId, string providerId, CancellationToken cancellationToken);
}
