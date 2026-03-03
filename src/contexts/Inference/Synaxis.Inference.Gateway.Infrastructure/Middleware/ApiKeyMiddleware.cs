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
        private const string ApiKeyPrefix = "Bearer synaxis_build_";
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKeyMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate in the pipeline.</param>
        public ApiKeyMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        /// <summary>
        /// Invokes the middleware to process HTTP requests and validate API keys.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <param name="apiKeyService">The service used to validate API keys.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
                    var validationResult = await apiKeyService.ValidateApiKeyAsync(apiKey).ConfigureAwait(false);

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
                            new System.Security.Claims.Claim("authenticationType", "ApiKey"),
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
                            message = validationResult.ErrorMessage ?? "Invalid API key",
                        })).ConfigureAwait(false);
                        return;
                    }
                }
            }

            await this._next(context).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Extension methods for registering the API key middleware.
    /// </summary>
    public static class ApiKeyMiddlewareExtensions
    {
        /// <summary>
        /// Adds the API key middleware to the application request pipeline.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <returns>The application builder with API key middleware configured.</returns>
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}
