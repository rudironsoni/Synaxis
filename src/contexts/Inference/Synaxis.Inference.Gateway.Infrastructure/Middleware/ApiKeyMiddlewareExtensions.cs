// <copyright file="ApiKeyMiddlewareExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Middleware
{
    using Microsoft.AspNetCore.Builder;

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