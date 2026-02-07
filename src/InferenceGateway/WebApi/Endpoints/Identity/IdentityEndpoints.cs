// <copyright file="IdentityEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Identity
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    /// <summary>
    /// Endpoints for identity and authentication management.
    /// </summary>
    public static class IdentityEndpoints
    {
        /// <summary>
        /// Maps identity-related endpoints to the application.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/identity")
                .WithTags("Identity")
                .RequireCors("WebApp");

            group.MapPost("/{provider}/start", async (IdentityManager manager, [FromRoute] string provider) =>
            {
                var result = await manager.StartAuth(provider);
                return Results.Ok(result);
            });

            group.MapPost("/{provider}/complete", async (IdentityManager manager, [FromRoute] string provider, [FromBody] CompleteRequest body) =>
            {
                var res = await manager.CompleteAuth(provider, body.Code, body.State);
                return Results.Ok(res);
            });

            group.MapGet("/accounts", async (ISecureTokenStore store) =>
            {
                var accounts = await store.LoadAsync().ConfigureAwait(false);
                var masked = accounts.Select(a => new
                {
                    a.Id,
                    a.Provider,
                    a.Email,
                    AccessToken = string.IsNullOrEmpty(a.AccessToken) ? string.Empty : (a.AccessToken.Length <= 8 ? "****" : a.AccessToken.Substring(0, 4) + "...." + a.AccessToken.Substring(a.AccessToken.Length - 4)),
                });
                return Results.Ok(masked);
            });
        }

        /// <summary>
        /// Request model for completing authentication.
        /// </summary>
        public class CompleteRequest
        {
            /// <summary>
            /// Gets or sets the authorization code.
            /// </summary>
            public string Code { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the state parameter for CSRF protection.
            /// </summary>
            public string State { get; set; } = string.Empty;
        }
    }
}
