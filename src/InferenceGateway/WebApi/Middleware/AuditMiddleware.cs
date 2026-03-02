// <copyright file="AuditMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Middleware that logs all API requests for audit and compliance purposes.
    /// Captures request/response details without logging sensitive data.
    /// </summary>
    public sealed class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public AuditMiddleware(
            RequestDelegate next,
            ILogger<AuditMiddleware> logger)
        {
            this._next = next!;
            this._logger = logger!;
        }

        /// <summary>
        /// Invokes the middleware to log request details.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext)
        {
            if (AuditMiddleware.ShouldSkipAudit(context))
            {
                await this._next(context).ConfigureAwait(false);
                return;
            }

            var auditContext = CreateAuditContext(context);
            this.LogRequestStart(auditContext);

            var originalBodyStream = context.Response.Body;

            try
            {
                await this.ProcessRequestAsync(
                    context,
                    tenantContext,
                    auditContext,
                    originalBodyStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                auditContext.Stopwatch.Stop();

                this._logger.LogError(
                    ex,
                    "API Request Failed: {RequestId} {Method} {Path} after {Duration}ms | OrgId: {OrgId}",
                    auditContext.RequestId,
                    auditContext.RequestMethod,
                    auditContext.RequestPath,
                    auditContext.Stopwatch.ElapsedMilliseconds,
                    tenantContext.OrganizationId);

                // Re-throw with contextual information
                throw new InvalidOperationException($"API request {auditContext.RequestId} failed after {auditContext.Stopwatch.ElapsedMilliseconds}ms", ex);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task ProcessRequestAsync(
            HttpContext context,
            ITenantContext tenantContext,
            AuditRequestContext auditContext,
            Stream originalBodyStream)
        {
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await this._next(context).ConfigureAwait(false);

            await this.LogResponseAsync(
                context,
                tenantContext,
                responseBodyStream,
                originalBodyStream,
                auditContext).ConfigureAwait(false);
        }

        private Task LogResponseAsync(
            HttpContext context,
            ITenantContext tenantContext,
            MemoryStream responseBodyStream,
            Stream originalBodyStream,
            AuditRequestContext auditContext)
        {
            auditContext.Stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var responseSize = responseBodyStream.Length;
            var logLevel = AuditMiddleware.ResolveLogLevel(statusCode);

            this.LogResponseCompletion(
                context,
                tenantContext,
                auditContext.RequestId,
                auditContext.RequestMethod,
                auditContext.RequestPath,
                auditContext.Stopwatch,
                statusCode,
                responseSize,
                logLevel);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            return responseBodyStream.CopyToAsync(originalBodyStream);
        }

        private static AuditRequestContext CreateAuditContext(HttpContext context)
        {
            return new AuditRequestContext(
                AuditMiddleware.ResolveRequestId(context),
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.ToString(),
                AuditMiddleware.GetClientIpAddress(context),
                Stopwatch.StartNew());
        }

        private void LogRequestStart(AuditRequestContext auditContext)
        {
            this._logger.LogInformation(
                "API Request Started: {RequestId} {Method} {Path}{Query} from {ClientIp}",
                auditContext.RequestId,
                auditContext.RequestMethod,
                auditContext.RequestPath,
                auditContext.RequestQuery,
                auditContext.ClientIp);
        }

        private static bool ShouldSkipAudit(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal)
                   || context.Request.Path.StartsWithSegments("/openapi", StringComparison.Ordinal)
                   || context.Request.Path.Value?.Contains("swagger", StringComparison.OrdinalIgnoreCase) == true;
        }

        private void LogResponseCompletion(
            HttpContext context,
            ITenantContext tenantContext,
            string requestId,
            string requestMethod,
            PathString requestPath,
            Stopwatch stopwatch,
            int statusCode,
            long responseSize,
            LogLevel logLevel)
        {
            this._logger.Log(
                logLevel,
                "API Request Completed: {RequestId} {Method} {Path} -> {StatusCode} in {Duration}ms | OrgId: {OrgId} | UserId: {UserId} | ApiKeyId: {ApiKeyId} | Region: {UserRegion} | CrossBorder: {CrossBorder} | Size: {ResponseSize} bytes",
                requestId,
                requestMethod,
                requestPath,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                tenantContext.OrganizationId,
                tenantContext.UserId,
                tenantContext.ApiKeyId,
                context.Items["UserRegion"],
                context.Items["IsCrossBorder"],
                responseSize);
        }

        private sealed record AuditRequestContext(
            string RequestId,
            string RequestMethod,
            PathString RequestPath,
            string RequestQuery,
            string ClientIp,
            Stopwatch Stopwatch);

        private static string ResolveRequestId(HttpContext context)
        {
            return context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();
        }

        private static LogLevel ResolveLogLevel(int statusCode)
        {
            if (statusCode >= 500)
            {
                return LogLevel.Error;
            }

            if (statusCode >= 400)
            {
                return LogLevel.Warning;
            }

            return LogLevel.Information;
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
