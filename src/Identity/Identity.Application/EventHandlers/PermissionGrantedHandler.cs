// <copyright file="PermissionGrantedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Application.EventHandlers;

using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

/// <summary>
/// Handles PermissionGranted domain events.
/// </summary>
public sealed class PermissionGrantedHandler : INotificationHandler<PermissionGranted>
{
    private readonly ILogger<PermissionGrantedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionGrantedHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PermissionGrantedHandler(ILogger<PermissionGrantedHandler> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public ValueTask Handle(PermissionGranted notification, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("PermissionGranted handled for user {UserId}", notification.UserId);
        return ValueTask.CompletedTask;
    }
}
