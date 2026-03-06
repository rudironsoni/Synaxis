// <copyright file="UserSuspendedHandler.cs" company="Synaxis">
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
/// Handles UserSuspended domain events.
/// </summary>
public sealed class UserSuspendedHandler : INotificationHandler<UserSuspended>
{
    private readonly ILogger<UserSuspendedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="UserSuspendedHandler"/> class.
    /// </summary>
    public UserSuspendedHandler(ILogger<UserSuspendedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserSuspended notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserSuspended handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
