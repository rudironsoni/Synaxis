using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Identity
{
    public static class IdentityEndpoints
    {
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
                    AccessToken = string.IsNullOrEmpty(a.AccessToken) ? string.Empty : (a.AccessToken.Length <= 8 ? "****" : a.AccessToken.Substring(0, 4) + "...." + a.AccessToken.Substring(a.AccessToken.Length - 4))
                });
                return Results.Ok(masked);
            });
        }

        public class CompleteRequest
        {
            public string Code { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
        }
    }
}
