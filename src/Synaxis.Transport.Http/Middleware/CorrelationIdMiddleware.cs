// <copyright file="CorrelationIdMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Middleware that generates or propagates correlation IDs for request tracking.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeaderName = "X-Correlation-ID";
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public CorrelationIdMiddleware(RequestDelegate next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task InvokeAsync(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Get or generate correlation ID
            string correlationId = context.Request.Headers[CorrelationIdHeaderName].ToString();
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Add to response headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
                {
                    context.Response.Headers[CorrelationIdHeaderName] = correlationId;
                }

                return Task.CompletedTask;
            });

            // Store in HttpContext.Items for downstream use
            context.Items["CorrelationId"] = correlationId;

            return this.next(context);
        }
    }
}
