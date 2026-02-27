// <copyright file="WebSocketMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Transport.WebSocket.Handlers;

    /// <summary>
    /// Middleware for handling WebSocket connections.
    /// </summary>
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate next;
        private readonly WebSocketHandler handler;
        private readonly ILogger<WebSocketMiddleware> logger;
        private readonly WebSocketTransportOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="handler">The WebSocket handler.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="options">The WebSocket transport options.</param>
        public WebSocketMiddleware(
            RequestDelegate next,
            WebSocketHandler handler,
            ILogger<WebSocketMiddleware> logger,
            IOptions<WebSocketTransportOptions> options)
        {
            this.next = next!;
            this.handler = handler!;
            this.logger = logger!;
            ArgumentNullException.ThrowIfNull(options);
            this.options = options.Value;
        }

        /// <summary>
        /// Invokes the middleware.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (context.Request.Path == this.options.Path && context.WebSockets.IsWebSocketRequest)
            {
                this.logger.LogInformation("WebSocket upgrade request received for path: {Path}", context.Request.Path);

                // WebSocket is owned by ASP.NET Core and will be disposed by the framework
                var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await this.handler.HandleAsync(webSocket, context).ConfigureAwait(false);
            }
            else
            {
                await this.next(context).ConfigureAwait(false);
            }
        }
    }
}
