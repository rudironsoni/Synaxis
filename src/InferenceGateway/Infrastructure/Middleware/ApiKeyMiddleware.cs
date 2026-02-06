// <copyright file="ApiKeyMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Middleware
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Synaxis.InferenceGateway.Application.ApiKeys;

    /// <summary>
    /// Middleware for authenticating requests via API key.
    /// Checks the Authorization header for API keys and validates them.
    /// </summary>
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ApiKeyPrefix = "Bearer synaxis_build_";

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyMiddleware"/> class.
        /// </summary>
        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        public async Task InvokeAsync(HttpContext context, IApiKeyService apiKeyService)
        {
            // Check if Authorization header contains an API key
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authValue = authHeader.ToString();
                if (authValue.StartsWith("Bearer synaxis_build_", StringComparison.OrdinalIgnoreCase))
                {
                    var apiKey = authValue.Substring("Bearer ".Length);

                    // Validate the API key
                    var validationResult = await apiKeyService.ValidateApiKeyAsync(apiKey);

                    if (validationResult.IsValid)
                    {
                        // Set organization ID and API key ID in HttpContext.Items for downstream use
                        context.Items["OrganizationId"] = validationResult.OrganizationId;
                        context.Items["ApiKeyId"] = validationResult.ApiKeyId;
                        context.Items["ApiKeyScopes"] = validationResult.Scopes;
                        context.Items["AuthenticationType"] = "ApiKey";

                        // Set user claims if needed for authorization
                        var claims = new[]
                        {
                            new System.Security.Claims.Claim("organizationId", validationResult.OrganizationId!.Value.ToString()),
                            new System.Security.Claims.Claim("apiKeyId", validationResult.ApiKeyId!.Value.ToString()),
                            new System.Security.Claims.Claim("authenticationType", "ApiKey")
                        };

                        var identity = new System.Security.Claims.ClaimsIdentity(claims, "ApiKey");
                        context.User = new System.Security.Claims.ClaimsPrincipal(identity);
                    }
                    else
                    {
                        // Invalid API key - return 401 Unauthorized
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = "Unauthorized",
                            message = validationResult.ErrorMessage ?? "Invalid API key"
                        }));
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension methods for registering the API key middleware.
    /// </summary>
    public static class ApiKeyMiddlewareExtensions
    {
        /// <summary>
        /// Adds the API key middleware to the pipeline.
        /// </summary>
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}