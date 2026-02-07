// <copyright file="ITenantContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Interfaces
{
    using System;

    /// <summary>
    /// Represents the tenant context for the current request.
    /// Provides access to tenant-specific information extracted from the API key or JWT token.
    /// </summary>
    public interface ITenantContext
    {
        /// <summary>
        /// Gets the organization ID for the current request.
        /// </summary>
        Guid? OrganizationId { get; }

        /// <summary>
        /// Gets the user ID for the current request (if authenticated via JWT).
        /// </summary>
        Guid? UserId { get; }

        /// <summary>
        /// Gets the API key ID for the current request (if authenticated via API key).
        /// </summary>
        Guid? ApiKeyId { get; }

        /// <summary>
        /// Gets a value indicating whether the request is authenticated via API key.
        /// </summary>
        bool IsApiKeyAuthenticated { get; }

        /// <summary>
        /// Gets a value indicating whether the request is authenticated via JWT token.
        /// </summary>
        bool IsJwtAuthenticated { get; }

        /// <summary>
        /// Gets the scopes available for the current request.
        /// </summary>
        string[] Scopes { get; }

        /// <summary>
        /// Gets the rate limit in requests per minute for the API key (if applicable).
        /// </summary>
        int? RateLimitRpm { get; }

        /// <summary>
        /// Gets the rate limit in tokens per minute for the API key (if applicable).
        /// </summary>
        int? RateLimitTpm { get; }

        /// <summary>
        /// Sets the tenant context from API key validation result.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="apiKeyId">The API key ID.</param>
        /// <param name="scopes">The scopes for the API key.</param>
        /// <param name="rateLimitRpm">The rate limit in requests per minute.</param>
        /// <param name="rateLimitTpm">The rate limit in tokens per minute.</param>
        void SetApiKeyContext(Guid organizationId, Guid apiKeyId, string[] scopes, int? rateLimitRpm, int? rateLimitTpm);

        /// <summary>
        /// Sets the tenant context from JWT token claims.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <param name="scopes">The scopes for the user.</param>
        void SetJwtContext(Guid organizationId, Guid userId, string[] scopes);
    }
}
