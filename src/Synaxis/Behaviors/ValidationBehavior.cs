// <copyright file="ValidationBehavior.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Behaviors
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Pipeline behavior that validates requests before execution.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response being returned.</typeparam>
    public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        private readonly ILogger<ValidationBehavior<TMessage, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationBehavior{TMessage, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ValidationBehavior(ILogger<ValidationBehavior<TMessage, TResponse>> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <inheritdoc/>
        public ValueTask<TResponse> Handle(
            TMessage message,
            MessageHandlerDelegate<TMessage, TResponse> next,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(next);
            this._logger.LogDebug("Validating {MessageType}", typeof(TMessage).Name);

            return next(message, cancellationToken);
        }
    }
}
