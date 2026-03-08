// <copyright file="ApiKeyValidationMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Middleware;

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Abstractions;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Configuration;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

/// <summary>
/// Middleware for validating API keys against external API Management platform.
/// Extracts API key from headers, validates against configured provider,
/// and enforces rate limiting.
/// </summary>
public sealed class ApiKeyValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiManagementService _apiManagementService;
    private readonly ILogger<ApiKeyValidationMiddleware> _logger;
    private readonly ApiManagementOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="apiManagementService">The API management service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The API management options.</param>
    public ApiKeyValidationMiddleware(
        RequestDelegate next,
        IApiManagementService apiManagementService,
        ILogger<ApiKeyValidationMiddleware> logger,
        IOptions<ApiManagementOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _apiManagementService = apiManagementService ?? throw new ArgumentNullException(nameof(apiManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new ApiManagementOptions { Enabled = false };
    }

    /// <summary>
    /// Invokes the middleware to validate API key and enforce rate limiting.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if API Management is not enabled
        if (!_options.Enabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Skip validation for excluded paths
        if (ShouldSkipValidation(context.Request.Path))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Extract API key from header
        var apiKey = ExtractApiKey(context);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("API key not provided in request to {Path}", context.Request.Path);
            await WriteUnauthorizedResponseAsync(context, "API key is required").ConfigureAwait(false);
            return;
        }

        // Validate API key against external API Management
        var validationResult = await _apiManagementService.ValidateKeyAsync(apiKey, context.RequestAborted).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("API key validation failed: {Error}", validationResult.ErrorMessage);
            await WriteUnauthorizedResponseAsync(context, validationResult.ErrorMessage ?? "Invalid API key").ConfigureAwait(false);
            return;
        }

        // Check rate limiting
        if (_options.RateLimit.Enabled && validationResult.KeyId != null)
        {
            var rateLimitStatus = await _apiManagementService.GetRateLimitStatusAsync(
                validationResult.KeyId,
                context.RequestAborted).ConfigureAwait(false);

            if (rateLimitStatus.IsRateLimited)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key {KeyId}: {RequestsMade}/{TotalAllowed} requests",
                    validationResult.KeyId,
                    rateLimitStatus.RequestsMade,
                    rateLimitStatus.TotalRequestsAllowed);

                await WriteRateLimitedResponseAsync(context, rateLimitStatus).ConfigureAwait(false);
                return;
            }

            // Add rate limit headers
            AddRateLimitHeaders(context.Response, rateLimitStatus);
        }

        // Add API key information to request context
        context.Items["ApiKeyId"] = validationResult.KeyId;
        context.Items["ApiKeySubscriptionId"] = validationResult.SubscriptionId;
        context.Items["ApiKeyScopes"] = validationResult.Scopes;

        _logger.LogDebug(
            "API key validated: {KeyId}, Subscription: {SubscriptionId}, Path: {Path}",
            validationResult.KeyId,
            validationResult.SubscriptionId,
            context.Request.Path);

        await _next(context).ConfigureAwait(false);
    }

    private string ExtractApiKey(HttpContext context)
    {
        // Try header first
        if (context.Request.Headers.TryGetValue(_options.ApiKeyHeaderName, out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        // Try query string
        if (context.Request.Query.TryGetValue("api-key", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        // Try Authorization header with ApiKey scheme
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("ApiKey ".Length).Trim();
        }

        return null;
    }

    private bool ShouldSkipValidation(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip health checks and documentation
        var excludedPaths = new[]
        {
            "/health",
            "/healthz",
            "/swagger",
            "/openapi",
            "/scalar",
            "/swagger-ui",
            "/favicon.ico",
            "/robots.txt",
        };

        return excludedPaths.Any(excluded =>
            pathValue.StartsWith(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteUnauthorizedResponseAsync(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Unauthorized",
            message = message,
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
        };

        await context.Response.WriteAsJsonAsync(response).ConfigureAwait(false);
    }

    private static async Task WriteRateLimitedResponseAsync(HttpContext context, RateLimitStatus status)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";

        if (status.RetryAfter.HasValue)
        {
            context.Response.Headers.RetryAfter = status.RetryAfter.Value.TotalSeconds.ToString("F0");
        }

        var response = new
        {
            error = "RateLimitExceeded",
            message = "Rate limit exceeded. Please try again later.",
            retryAfter = status.RetryAfter?.TotalSeconds ?? 60,
            limit = status.TotalRequestsAllowed,
            remaining = status.RemainingRequests,
            timestamp = DateTimeOffset.UtcNow.ToString("O"),
        };

        await context.Response.WriteAsJsonAsync(response).ConfigureAwait(false);
    }

    private static void AddRateLimitHeaders(HttpResponse response, RateLimitStatus status)
    {
        if (status.TotalRequestsAllowed > 0)
        {
            response.Headers.Append("X-RateLimit-Limit", status.TotalRequestsAllowed.ToString());
        }

        if (status.RemainingRequests >= 0)
        {
            response.Headers.Append("X-RateLimit-Remaining", status.RemainingRequests.ToString());
        }

        response.Headers.Append("X-RateLimit-Reset", status.WindowResetTime.ToUnixTimeSeconds().ToString());
    }
}

/// <summary>
/// Extension methods for API key validation middleware.
/// </summary>
public static class ApiKeyValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds API key validation middleware to the request pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder.</returns>
    public static IApplicationBuilder UseApiKeyValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyValidationMiddleware>();
    }
}
