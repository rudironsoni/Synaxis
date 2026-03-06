// <copyright file="PasswordResetRequestedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles PasswordResetRequested domain events.
/// </summary>
public sealed class PasswordResetRequestedHandler : INotificationHandler<PasswordResetRequested>
{
    private readonly ILogger<PasswordResetRequestedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordResetRequestedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PasswordResetRequestedHandler(ILogger<PasswordResetRequestedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(PasswordResetRequested notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("PasswordResetRequested handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
