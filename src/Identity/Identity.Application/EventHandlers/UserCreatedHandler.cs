// <copyright file="UserCreatedHandler.cs" company="Synaxis">
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
/// Handles UserCreated domain events.
/// </summary>
public sealed class UserCreatedHandler : INotificationHandler<UserCreated>
{
    private readonly ILogger<UserCreatedHandler> logger;

    /// <summary>
    </summary>
    <param name="logger">The logger instance.</param>
    <summary>
    /// Initializes a new instance of the <see cref="UserCreatedHandler"/> class.
    /// </summary>
    public UserCreatedHandler(ILogger<UserCreatedHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("UserCreated handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
