// <copyright file="MetricsBehavior.cs" company="Synaxis">
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
    /// Pipeline behavior that collects metrics for request execution.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response being returned.</typeparam>
    public sealed class MetricsBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        private readonly ILogger<MetricsBehavior<TMessage, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsBehavior{TMessage, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public MetricsBehavior(ILogger<MetricsBehavior<TMessage, TResponse>> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async ValueTask<TResponse> Handle(
            TMessage message,
            MessageHandlerDelegate<TMessage, TResponse> next,
            CancellationToken cancellationToken)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (next is null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            var activity = Activity.Current;
            if (activity is not null)
            {
                activity.SetTag("synaxis.message_type", typeof(TMessage).Name);
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await next(message, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                // TODO: Emit metrics to telemetry system
                this._logger.LogDebug(
                    "Metrics for {MessageType}: Duration={ElapsedMilliseconds}ms",
                    typeof(TMessage).Name,
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch
            {
                stopwatch.Stop();
                if (activity is not null)
                {
                    activity.SetTag("synaxis.error", true);
                }

                throw;
            }
        }
    }
}
