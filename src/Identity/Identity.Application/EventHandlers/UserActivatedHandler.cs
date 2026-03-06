// <copyright file="UserActivatedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles UserActivated domain events.
/// </summary>
public sealed class UserActivatedHandler : INotificationHandler<UserActivated>
{
    private readonly ILogger<UserActivatedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActivatedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public UserActivatedHandler(ILogger<UserActivatedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserActivated notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("UserActivated handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
