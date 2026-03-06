// <copyright file="UserPasswordChangedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
/// Handles UserPasswordChanged domain events.
/// </summary>
public sealed class UserPasswordChangedHandler : INotificationHandler<UserPasswordChanged>
{
    private readonly ILogger<UserPasswordChangedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="UserPasswordChangedHandler"/> class.
    /// </summary>
    public UserPasswordChangedHandler(ILogger<UserPasswordChangedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserPasswordChanged notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserPasswordChanged handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
