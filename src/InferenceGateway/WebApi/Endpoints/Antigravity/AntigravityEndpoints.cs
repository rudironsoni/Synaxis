using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity;

public static class AntigravityEndpoints
{
    public static void MapAntigravityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/antigravity")
            .WithTags("Antigravity");

        group.MapGet("/accounts", (IAntigravityAuthManager authManager) =>
        {
            return Results.Ok(authManager.ListAccounts());
        })
        .WithName("ListAntigravityAccounts");

        group.MapPost("/auth/start", (IAntigravityAuthManager authManager, [FromBody] StartAuthRequest request) =>
        {
            var url = authManager.StartAuthFlow(request.RedirectUrl ?? "http://localhost:51121/oauth-callback");
            return Results.Ok(new { AuthUrl = url });
        })
        .WithName("StartAntigravityAuth");

        group.MapPost("/auth/complete", async (IAntigravityAuthManager authManager, [FromBody] CompleteAuthRequest request) =>
        {
            try
            {
                await authManager.CompleteAuthFlowAsync(request.Code, request.RedirectUrl ?? "http://localhost:51121/oauth-callback");
                return Results.Ok(new { Message = "Authentication successful. Account added." });
            }
            catch (System.Exception ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        })
        .WithName("CompleteAntigravityAuth");
    }
}

public class StartAuthRequest
{
    public string? RedirectUrl { get; set; }
}

public class CompleteAuthRequest
{
    public string Code { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
}
