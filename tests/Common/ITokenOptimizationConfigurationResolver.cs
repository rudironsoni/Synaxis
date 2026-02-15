// <copyright file="ITokenOptimizationConfigurationResolver.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for token optimization configuration resolution.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ITokenOptimizationConfigurationResolver
{
    Task<TokenOptimizationOptions> ResolveAsync(string tenantId, string userId, CancellationToken cancellationToken);
}
