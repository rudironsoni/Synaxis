// <copyright file="MediatorStreamExecutor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Execution
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Mediator;
    using Synaxis.Abstractions.Execution;

    /// <summary>
    /// Stream executor implementation that delegates to Mediator.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to execute.</typeparam>
    /// <typeparam name="TResult">The type of result produced by the stream.</typeparam>
    public sealed class MediatorStreamExecutor<TRequest, TResult> : IStreamExecutor<TRequest, TResult>
        where TRequest : Abstractions.Commands.IStreamRequest<TResult>
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorStreamExecutor{TRequest, TResult}"/> class.
        /// </summary>
        /// <param name="mediator">The mediator instance.</param>
        public MediatorStreamExecutor(IMediator mediator)
        {
            this._mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<TResult> ExecuteStreamAsync(TRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request is not IStreamRequest<TResult> streamRequest)
            {
                throw new InvalidOperationException(
                    $"Request type {typeof(TRequest).Name} does not implement IStreamRequest<{typeof(TResult).Name}>");
            }

            return this._mediator.CreateStream(streamRequest, cancellationToken);
        }
    }
}
