// <copyright file="ICommandExecutor.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Execution
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a contract for executing commands that produce a single result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <typeparam name="TResult">The type of result produced by the command.</typeparam>
    public interface ICommandExecutor<in TCommand, TResult>
    {
        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation.</returns>
        ValueTask<TResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken);
    }
}
