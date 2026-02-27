// <copyright file="AntigravityEndpoints.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Endpoints.Antigravity
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Synaxis.InferenceGateway.Infrastructure.Auth;

    /// <summary>
    /// Endpoints for Antigravity authentication.
    /// </summary>
    public static class AntigravityEndpoints
    {
        /// <summary>
        /// Maps Antigravity endpoints to the application.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        public static void MapAntigravityEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/oauth/antigravity/callback", async (IAntigravityAuthManager authManager, HttpRequest request) =>
            {
                var code = request.Query["code"].ToString();
                var state = request.Query["state"].ToString();
                var redirectUrl = $"{request.Scheme}://{request.Host}{request.Path}";

                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                {
                    return Results.Content(RenderCallbackHtml("Missing code or state in callback URL."), "text/html", System.Text.Encoding.UTF8, 400);
                }

                try
                {
                    await authManager.CompleteAuthFlowAsync(code, redirectUrl, state).ConfigureAwait(false);
                    return Results.Content(RenderCallbackHtml("Authentication successful. You can close this window."), "text/html", System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    return Results.Content(RenderCallbackHtml($"Authentication failed: {ex.Message}"), "text/html", System.Text.Encoding.UTF8, 400);
                }
            })
            .WithTags("Antigravity")
            .WithName("AntigravityAuthCallback");

            var group = app.MapGroup("/antigravity")
                .WithTags("Antigravity")
                .RequireCors("PublicAPI");

            group.MapGet("/accounts", (IAntigravityAuthManager authManager) =>
            {
                return Results.Ok(authManager.ListAccounts());
            })
            .WithName("ListAntigravityAccounts");

            group.MapPost("/auth/start", async (IAntigravityAuthManager authManager, [FromBody] StartAuthRequest request) =>
            {
                const string defaultRedirectUrl = "http://localhost:51121/oauth/antigravity/callback";
                var redirectUrl = string.IsNullOrWhiteSpace(request.RedirectUrl) ? defaultRedirectUrl : request.RedirectUrl;
                var url = await authManager.StartAuthFlowAsync(redirectUrl).ConfigureAwait(false);
                return Results.Ok(new
                {
                    AuthUrl = url,
                    RedirectUrl = redirectUrl,
                    Instructions = "Open AuthUrl in your browser. After login, you will be redirected to the callback; no manual code copy is required.",
                });
            })
            .WithName("StartAntigravityAuth");

            group.MapPost("/auth/complete", async (IAntigravityAuthManager authManager, [FromBody] CompleteAuthRequest request) =>
            {
                try
                {
                    var (code, state, redirectUrl) = ResolveAuthCompletion(request);
                    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                    {
                        return Results.BadRequest(new { Error = "Both code and state are required. Provide them directly or via callbackUrl." });
                    }

                    await authManager.CompleteAuthFlowAsync(code, redirectUrl, state).ConfigureAwait(false);
                    return Results.Ok(new { Message = "Authentication successful. Account added." });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { Error = ex.Message });
                }
            })
            .WithName("CompleteAntigravityAuth");
        }

        private static (string Code, string State, string RedirectUrl) ResolveAuthCompletion(CompleteAuthRequest request)
        {
            const string defaultRedirectUrl = "http://localhost:51121/oauth/antigravity/callback";
            var code = request.Code;
            var state = request.State;
            var redirectUrl = request.RedirectUrl;

            if (!string.IsNullOrWhiteSpace(request.CallbackUrl))
            {
                var callbackUri = new Uri(request.CallbackUrl);
                var query = System.Web.HttpUtility.ParseQueryString(callbackUri.Query);
                code ??= query["code"];
                state ??= query["state"];
                redirectUrl ??= callbackUri.GetLeftPart(UriPartial.Path);
            }

            if (string.IsNullOrWhiteSpace(redirectUrl))
            {
                redirectUrl = defaultRedirectUrl;
            }

            return (code ?? string.Empty, state ?? string.Empty, redirectUrl);
        }

        private static string RenderCallbackHtml(string message)
        {
            return $"<!doctype html><html><head><meta charset=\"utf-8\"><title>Antigravity Auth</title></head><body style=\"font-family: Arial, sans-serif; padding: 24px;\"><h2>Antigravity Auth</h2><p>{System.Net.WebUtility.HtmlEncode(message)}</p></body></html>";
        }
    }

    /// <summary>
    /// Request to start Antigravity authentication.
    /// </summary>
    public class StartAuthRequest
    {
        /// <summary>
        /// Gets or sets the redirect URL.
        /// </summary>
        public string? RedirectUrl { get; set; }
    }

    /// <summary>
    /// Request to complete Antigravity authentication.
    /// </summary>
    public class CompleteAuthRequest
    {
        /// <summary>
        /// Gets or sets the authorization code.
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// Gets or sets the state parameter.
        /// </summary>
        public string? State { get; set; }

        /// <summary>
        /// Gets or sets the redirect URL.
        /// </summary>
        public string? RedirectUrl { get; set; }

        /// <summary>
        /// Gets or sets the callback URL.
        /// </summary>
        public string? CallbackUrl { get; set; }
    }
}
