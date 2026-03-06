// <copyright file="UserDeletedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles UserDeleted domain events.
/// </summary>
public sealed class UserDeletedHandler : INotificationHandler<UserDeleted>
{
    private readonly ILogger<UserDeletedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDeletedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserDeletedHandler(ILogger<UserDeletedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserDeleted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("UserDeleted handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
