// <copyright file="TenantResolutionMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;
    using Synaxis.InferenceGateway.Application.ApiKeys;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Middleware that resolves tenant context from API key or JWT token authentication.
    /// Extracts tenant information from the Authorization header and populates ITenantContext.
    /// </summary>
    /// <remarks>
    /// Authentication Flow:
    /// 1. Check Authorization header for Bearer token
    /// 2. If token starts with "synaxis_", treat as API key:
    ///    - Extract prefix from key (synaxis_xxxx...)
    ///    - Query database for matching key by prefix
    ///    - Validate full key using bcrypt
    ///    - Set tenant context with OrganizationId, ApiKeyId
    /// 3. If token is JWT (not API key):
    ///    - Extract claims from JWT (already validated by ASP.NET Core JWT middleware)
    ///    - Set tenant context with OrganizationId, UserId.
    /// 4. If no valid authentication found, return 401 Unauthorized.
    /// </remarks>
    public sealed class TenantResolutionMiddleware
    {
        private const string BearerPrefix = "Bearer ";
        private const string ApiKeyPrefix = "synaxis_";
        private readonly RequestDelegate _next;
        private readonly ILogger<TenantResolutionMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantResolutionMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to resolve tenant context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context to populate.</param>
        /// <param name="apiKeyService">The API key service for validation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
#pragma warning disable MA0051 // Method is too long
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IApiKeyService apiKeyService)
        {
            try
            {
                // Extract Authorization header
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    // No authorization header - let subsequent middleware handle it
                    // (some endpoints may be public or use other auth mechanisms)
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                // Check if it's a Bearer token
                if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    this._logger.LogWarning("Authorization header does not use Bearer scheme");
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = "Invalid authorization scheme. Use 'Bearer <token>'",
                            type = "invalid_request_error",
                        },
                    }).ConfigureAwait(false);
                    return;
                }

                var token = authHeader.Substring(BearerPrefix.Length).Trim();

                // Determine if it's an API key or JWT token
                if (token.StartsWith(ApiKeyPrefix, StringComparison.Ordinal))
                {
                    // API Key authentication
                    await this.HandleApiKeyAuthenticationAsync(context, token, tenantContext, apiKeyService).ConfigureAwait(false);
                }
                else
                {
                    // JWT token authentication (already validated by ASP.NET Core JWT middleware)
                    this.HandleJwtAuthentication(context, tenantContext);
                }

                // Only proceed if authentication was successful
                if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                {
                    return;
                }

                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during tenant resolution");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "An error occurred during authentication",
                        type = "internal_server_error",
                    },
                }).ConfigureAwait(false);
            }
        }
#pragma warning restore MA0051 // Method is too long

        /// <summary>
        /// Handles API key authentication by validating the key and populating tenant context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="apiKey">The API key to validate.</param>
        /// <param name="tenantContext">The tenant context to populate.</param>
        /// <param name="apiKeyService">The API key service for validation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
#pragma warning disable MA0051 // Method is too long
        private async Task HandleApiKeyAuthenticationAsync(
            HttpContext context,
            string apiKey,
            ITenantContext tenantContext,
            IApiKeyService apiKeyService)
        {
            try
            {
                // Validate API key using the service
                var validationResult = await apiKeyService.ValidateApiKeyAsync(apiKey, context.RequestAborted).ConfigureAwait(false);

                if (!validationResult.IsValid)
                {
                    this._logger.LogWarning("API key validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = validationResult.ErrorMessage ?? "Invalid API key",
                            type = "invalid_api_key",
                        },
                    }).ConfigureAwait(false);
                    return;
                }

                // API key is valid - populate tenant context
                if (!validationResult.OrganizationId.HasValue || !validationResult.ApiKeyId.HasValue)
                {
                    this._logger.LogError("API key validation succeeded but OrganizationId or ApiKeyId is missing");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = "Authentication data is incomplete",
                            type = "internal_server_error",
                        },
                    }).ConfigureAwait(false);
                    return;
                }

                tenantContext.SetApiKeyContext(
                    validationResult.OrganizationId.Value,
                    validationResult.ApiKeyId.Value,
                    validationResult.Scopes,
                    validationResult.RateLimitRpm,
                    validationResult.RateLimitTpm);

                this._logger.LogDebug(
                    "API key authenticated successfully for OrganizationId={OrganizationId}, ApiKeyId={ApiKeyId}",
                    validationResult.OrganizationId,
                    validationResult.ApiKeyId);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during API key validation");
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "An error occurred during API key validation",
                        type = "internal_server_error",
                    },
                }).ConfigureAwait(false);
            }
        }
#pragma warning restore MA0051 // Method is too long

        /// <summary>
        /// Handles JWT token authentication by extracting claims and populating tenant context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context to populate.</param>
#pragma warning disable MA0051 // Method is too long
        private void HandleJwtAuthentication(HttpContext context, ITenantContext tenantContext)
        {
            // JWT validation is handled by ASP.NET Core JWT middleware
            // We just need to extract the claims from the authenticated user
            var user = context.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                this._logger.LogWarning("JWT token is not authenticated");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "Invalid or expired JWT token",
                        type = "invalid_token",
                    },
                }).Wait();
                return;
            }

            // Extract claims
            var organizationIdClaim = user.FindFirst("organization_id") ?? user.FindFirst("org_id");
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
            var scopesClaim = user.FindFirst("scope") ?? user.FindFirst("scopes");

            if (organizationIdClaim == null || userIdClaim == null)
            {
                this._logger.LogWarning("JWT token is missing required claims (organization_id or user_id)");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "JWT token is missing required claims",
                        type = "invalid_token",
                    },
                }).Wait();
                return;
            }

            if (!Guid.TryParse(organizationIdClaim.Value, out var organizationId))
            {
                this._logger.LogWarning("Invalid organization_id claim value: {Value}", organizationIdClaim.Value);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "Invalid organization_id in JWT token",
                        type = "invalid_token",
                    },
                }).Wait();
                return;
            }

            if (!Guid.TryParse(userIdClaim.Value, out var userId))
            {
                this._logger.LogWarning("Invalid user_id claim value: {Value}", userIdClaim.Value);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        message = "Invalid user_id in JWT token",
                        type = "invalid_token",
                    },
                }).Wait();
                return;
            }

            // Parse scopes (space-separated string or array)
            var scopes = scopesClaim != null
                ? scopesClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            tenantContext.SetJwtContext(organizationId, userId, scopes);

            this._logger.LogDebug(
                "JWT authenticated successfully for OrganizationId={OrganizationId}, UserId={UserId}",
                organizationId,
                userId);
        }
#pragma warning restore MA0051 // Method is too long
    }
}
