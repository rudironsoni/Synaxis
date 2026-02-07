// <copyright file="OpenAIMetadataMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Middleware that adds OpenAI-compatible metadata headers to responses.
    /// </summary>
    public class OpenAIMetadataMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIMetadataMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        public OpenAIMetadataMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        /// <summary>
        /// Invokes the middleware to add metadata headers.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                if (context.Items.TryGetValue("RoutingContext", out var value) && value is RoutingContext rc)
                {
                    context.Response.Headers["x-gateway-model-requested"] = rc.RequestedModel;
                    context.Response.Headers["x-gateway-model-resolved"] = rc.ResolvedCanonicalId;
                    context.Response.Headers["x-gateway-provider"] = rc.Provider;
                }
                else
                {
                    // Ensure headers are present even if empty
                    context.Response.Headers["x-gateway-model-requested"] = "";
                    context.Response.Headers["x-gateway-model-resolved"] = "";
                    context.Response.Headers["x-gateway-provider"] = "";
                }

                if (!context.Response.Headers.ContainsKey("x-request-id"))
                {
                    context.Response.Headers["x-request-id"] = context.TraceIdentifier;
                }

                return Task.CompletedTask;
            });

            await this._next(context);
        }
    }
}
