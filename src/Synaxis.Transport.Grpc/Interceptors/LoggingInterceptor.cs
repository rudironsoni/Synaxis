// <copyright file="LoggingInterceptor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Grpc.Interceptors
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using global::Grpc.Core;
    using global::Grpc.Core.Interceptors;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// gRPC interceptor for logging all requests and responses with timing metrics.
    /// </summary>
    public sealed class LoggingInterceptor : Interceptor
    {
        private readonly ILogger<LoggingInterceptor> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingInterceptor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Intercepts unary server calls to log request/response and timing.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="context">The server call context.</param>
        /// <param name="continuation">The continuation to invoke after processing.</param>
        /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Method;

            this.logger.LogInformation("gRPC request started: {Method}", method);

            try
            {
                var response = await continuation(request, context).ConfigureAwait(false);
                stopwatch.Stop();

                this.logger.LogInformation(
                    "gRPC request completed: {Method} in {ElapsedMs}ms with status {Status}",
                    method,
                    stopwatch.ElapsedMilliseconds,
                    StatusCode.OK);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                this.logger.LogError(
                    ex,
                    "gRPC request failed: {Method} in {ElapsedMs}ms",
                    method,
                    stopwatch.ElapsedMilliseconds);

                throw new RpcException(new Status(StatusCode.Internal, "Request processing failed", ex));
            }
        }

        /// <summary>
        /// Intercepts server streaming calls to log request and timing.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request message.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="context">The server call context.</param>
        /// <param name="continuation">The continuation to invoke after processing.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
            TRequest request,
            IServerStreamWriter<TResponse> responseStream,
            ServerCallContext context,
            ServerStreamingServerMethod<TRequest, TResponse> continuation)
        {
            var stopwatch = Stopwatch.StartNew();
            var method = context.Method;

            this.logger.LogInformation("gRPC streaming request started: {Method}", method);

            try
            {
                await continuation(request, responseStream, context).ConfigureAwait(false);
                stopwatch.Stop();

                this.logger.LogInformation(
                    "gRPC streaming request completed: {Method} in {ElapsedMs}ms with status {Status}",
                    method,
                    stopwatch.ElapsedMilliseconds,
                    StatusCode.OK);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                this.logger.LogError(
                    ex,
                    "gRPC streaming request failed: {Method} in {ElapsedMs}ms",
                    method,
                    stopwatch.ElapsedMilliseconds);

                throw new RpcException(new Status(StatusCode.Internal, "Streaming request processing failed", ex));
            }
        }
    }
}
