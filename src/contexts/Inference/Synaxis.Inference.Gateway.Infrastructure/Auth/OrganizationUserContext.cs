// <copyright file="OrganizationUserContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Provides access to the current authenticated user's context from HTTP claims.
    /// </summary>
    public class OrganizationUserContext : IOrganizationUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationUserContext"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The HTTP context accessor.</param>
        public OrganizationUserContext(IHttpContextAccessor httpContextAccessor)
        {
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            this._httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public Guid UserId
        {
            get
            {
                var userIdClaim = this._httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? this._httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    throw new InvalidOperationException("User ID not found in claims");
                }

                return Guid.Parse(userIdClaim);
            }
        }

        /// <inheritdoc/>
        public Guid? OrganizationId
        {
            get
            {
                var orgIdClaim = this._httpContextAccessor.HttpContext?.User.FindFirstValue("organizationId")
                                 ?? this._httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");

                return string.IsNullOrEmpty(orgIdClaim) ? null : Guid.Parse(orgIdClaim);
            }
        }

        /// <inheritdoc/>
        public bool IsAuthenticated => this._httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
