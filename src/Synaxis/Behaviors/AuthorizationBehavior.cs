// <copyright file="AuthorizationBehavior.cs" company="Synaxis">
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
    /// Pipeline behavior that performs authorization checks before request execution.
    /// </summary>
    /// <typeparam name="TMessage">The type of message being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response being returned.</typeparam>
    public sealed class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
        where TMessage : IMessage
    {
        private readonly ILogger<AuthorizationBehavior<TMessage, TResponse>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationBehavior{TMessage, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public AuthorizationBehavior(ILogger<AuthorizationBehavior<TMessage, TResponse>> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public ValueTask<TResponse> Handle(
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

            // TODO: Implement authorization logic
            this._logger.LogDebug("Authorizing {MessageType}", typeof(TMessage).Name);

            return next(message, cancellationToken);
        }
    }
}
