// <copyright file="LoggingBehavior.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Behaviors
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Pipeline behavior that logs request execution details.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response being returned.</typeparam>
    public sealed class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingBehavior{TMessage, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async ValueTask<TResponse> Handle(
            TMessage message,
            MessageHandlerDelegate<TMessage, TResponse> next,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(next);
            var messageType = typeof(TMessage).Name;
            this._logger.LogInformation("Handling {MessageType}", messageType);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await next(message, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                this._logger.LogInformation(
                    "Handled {MessageType} in {ElapsedMilliseconds}ms",
                    messageType,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex) when (this.LogException(ex, messageType, stopwatch))
            {
                throw;
            }
        }

        private bool LogException(Exception ex, string messageType, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            this._logger.LogError(
                ex,
                "Error handling {MessageType} after {ElapsedMilliseconds}ms",
                messageType,
                stopwatch.ElapsedMilliseconds);
            return false;
        }
    }
}
