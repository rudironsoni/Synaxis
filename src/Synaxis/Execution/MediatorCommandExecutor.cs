// <copyright file="MediatorCommandExecutor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Execution
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Mediator;
    using Synaxis.Abstractions.Execution;

    /// <summary>
    /// Command executor implementation that delegates to Mediator.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <typeparam name="TResult">The type of result produced by the command.</typeparam>
    public sealed class MediatorCommandExecutor<TCommand, TResult> : ICommandExecutor<TCommand, TResult>
        where TCommand : Abstractions.Commands.ICommand<TResult>
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediatorCommandExecutor{TCommand, TResult}"/> class.
        /// </summary>
        /// <param name="mediator">The mediator instance.</param>
        public MediatorCommandExecutor(IMediator mediator)
        {
            this._mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        /// <inheritdoc/>
        public ValueTask<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (command is not IRequest<TResult> request)
            {
                throw new InvalidOperationException(
                    $"Command type {typeof(TCommand).Name} does not implement IRequest<{typeof(TResult).Name}>");
            }

            return this._mediator.Send(request, cancellationToken);
        }
    }
}
