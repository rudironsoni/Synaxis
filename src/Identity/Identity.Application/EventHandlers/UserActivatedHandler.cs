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
    private readonly ILogger<UserActivatedHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserActivatedHandler"/> class.
    /// </summary>
    public UserActivatedHandler(ILogger<UserActivatedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserActivated notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserActivated handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
