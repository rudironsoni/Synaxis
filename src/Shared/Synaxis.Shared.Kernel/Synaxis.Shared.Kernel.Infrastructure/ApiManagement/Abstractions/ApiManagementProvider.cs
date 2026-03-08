// <copyright file="ApiManagementProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Abstractions;

/// <summary>
/// Represents supported API Management providers.
/// </summary>
public enum ApiManagementProvider
{
    /// <summary>
    /// Azure API Management.
    /// </summary>
    AzureApiManagement,

    /// <summary>
    /// Kong API Gateway.
    /// </summary>
    Kong,

    /// <summary>
    /// In-memory implementation for testing/development.
    /// </summary>
    InMemory,
}
