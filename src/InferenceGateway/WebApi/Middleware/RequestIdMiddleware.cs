// <copyright file="RequestIdMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Middleware that ensures every request has a unique request ID for tracking.
    /// </summary>
    public class RequestIdMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestIdMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        public RequestIdMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        /// <summary>
        /// Invokes the middleware to assign or retrieve a request ID.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task InvokeAsync(HttpContext context)
        {
            var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                         ?? context.Request.Headers["x-request-id"].FirstOrDefault()
                         ?? context.TraceIdentifier;

            context.Response.Headers["X-Request-ID"] = requestId;

            return this._next(context);
        }
    }
}
