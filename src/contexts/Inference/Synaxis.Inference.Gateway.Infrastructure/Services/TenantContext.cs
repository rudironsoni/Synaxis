// <copyright file="TenantContext.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Implementation of tenant context that stores request-scoped tenant information.
    /// This is registered as scoped service and populated by TenantResolutionMiddleware.
    /// </summary>
    public sealed class TenantContext : ITenantContext
    {
        /// <inheritdoc/>
        public Guid? OrganizationId { get; private set; }

        /// <inheritdoc/>
        public Guid? UserId { get; private set; }

        /// <inheritdoc/>
        public Guid? ApiKeyId { get; private set; }

        /// <inheritdoc/>
        public bool IsApiKeyAuthenticated => this.ApiKeyId.HasValue;

        /// <inheritdoc/>
        public bool IsJwtAuthenticated => this.UserId.HasValue;

        /// <inheritdoc/>
        public string[] Scopes { get; private set; } = Array.Empty<string>();

        /// <inheritdoc/>
        public int? RateLimitRpm { get; private set; }

        /// <inheritdoc/>
        public int? RateLimitTpm { get; private set; }

        /// <inheritdoc/>
        public void SetApiKeyContext(Guid organizationId, Guid apiKeyId, string[] scopes, int? rateLimitRpm, int? rateLimitTpm)
        {
            this.OrganizationId = organizationId;
            this.ApiKeyId = apiKeyId;
            this.Scopes = scopes ?? Array.Empty<string>();
            this.RateLimitRpm = rateLimitRpm;
            this.RateLimitTpm = rateLimitTpm;
            this.UserId = null; // Clear JWT-specific data
        }

        /// <inheritdoc/>
        public void SetJwtContext(Guid organizationId, Guid userId, string[] scopes)
        {
            this.OrganizationId = organizationId;
            this.UserId = userId;
            this.Scopes = scopes ?? Array.Empty<string>();
            this.ApiKeyId = null; // Clear API key-specific data
            this.RateLimitRpm = null;
            this.RateLimitTpm = null;
        }
    }
}
