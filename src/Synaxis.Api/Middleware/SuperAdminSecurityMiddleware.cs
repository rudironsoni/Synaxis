// <copyright file="SuperAdminSecurityMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Api.Middleware
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Middleware to enforce Super Admin security requirements:
    /// - 15-minute idle timeout
    /// - Session tracking
    /// - Additional security logging.
    /// </summary>
    public class SuperAdminSecurityMiddleware
    {
        private const int IdleTimeoutMinutes = 15;
        private readonly RequestDelegate _next;
        private readonly ILogger<SuperAdminSecurityMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperAdminSecurityMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public SuperAdminSecurityMiddleware(
            RequestDelegate next,
            ILogger<SuperAdminSecurityMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to enforce super admin security requirements.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            // Only apply to super admin routes
            if (path != null && path.StartsWith("/admin/super", StringComparison.Ordinal))
            {
                // Check session timeout
                var lastActivity = context.Session.GetString("SuperAdminLastActivity");
                if (!string.IsNullOrEmpty(lastActivity) &&
                    DateTime.TryParse(lastActivity, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastActivityTime))
                {
                    var idleTime = DateTime.UtcNow - lastActivityTime;
                    if (idleTime.TotalMinutes > IdleTimeoutMinutes)
                    {
                        this._logger.LogWarning(
                            "Super admin session timed out after {IdleMinutes} minutes of inactivity",
                            idleTime.TotalMinutes);

                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "Session expired",
                            message = $"Super admin sessions expire after {IdleTimeoutMinutes} minutes of inactivity",
                        }).ConfigureAwait(false);
                        return;
                    }
                }

                // Update last activity timestamp
                context.Session.SetString("SuperAdminLastActivity", DateTime.UtcNow.ToString("O"));

                // Log all super admin access
                var userId = context.User?.FindFirst("sub")?.Value ?? "unknown";
                var ipAddress = GetClientIpAddress(context);
                var method = context.Request.Method;

                this._logger.LogWarning(
                    "🔒 SUPER ADMIN ACCESS - User: {UserId}, IP: {IpAddress}, Method: {Method}, Path: {Path}",
                    userId,
                    ipAddress,
                    method,
                    path);
            }

            await this._next(context).ConfigureAwait(false);
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
