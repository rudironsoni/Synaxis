// <copyright file="AuthenticationInterceptor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Grpc.Interceptors
{
    using System;
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using global::Grpc.Core.Interceptors;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// gRPC interceptor for authenticating requests using metadata tokens.
    /// </summary>
    public sealed class AuthenticationInterceptor : Interceptor
    {
        private const string AuthorizationHeader = "authorization";
        private const string BearerPrefix = "Bearer ";
        private readonly ILogger<AuthenticationInterceptor> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationInterceptor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public AuthenticationInterceptor(ILogger<AuthenticationInterceptor> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Intercepts unary server calls to validate authentication.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="context">The server call context.</param>
        /// <param name="continuation">The continuation to invoke after processing.</param>
        /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns>
        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            if (!this.ValidateToken(context))
            {
                throw new RpcException(
                    new Status(StatusCode.Unauthenticated, "Invalid or missing authentication token"));
            }

            return continuation(request, context);
        }

        /// <summary>
        /// Intercepts server streaming calls to validate authentication.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="context">The server call context.</param>
        /// <param name="continuation">The continuation to invoke after processing.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            if (!this.ValidateToken(context))
            {
                throw new RpcException(
                    new Status(StatusCode.Unauthenticated, "Invalid or missing authentication token"));
            }

            return continuation(request, responseStream, context);
        }

        private bool ValidateToken(ServerCallContext context)
        {
            var metadata = context.RequestHeaders.Get(AuthorizationHeader);
            if (metadata == null)
            {
                this.logger.LogWarning("Request missing authorization header");
                return false;
            }

            var token = metadata.Value;
            if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                this.logger.LogWarning("Invalid authorization header format");
                return false;
            }

            var tokenValue = token.Substring(BearerPrefix.Length);
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                this.logger.LogWarning("Empty bearer token");
                return false;
            }

            // NOTE: Implement actual token validation logic in production
            // This is a placeholder that accepts any non-empty bearer token
            this.logger.LogDebug("Token validated successfully");
            return true;
        }
    }
}
