using Mediator;
using Microsoft.Extensions.Logging;
using Synaxis.Identity.Domain.Events;

namespace Synaxis.Identity.Application.EventHandlers;

public sealed class PasswordResetRequestedHandler : INotificationHandler<PasswordResetRequested>
{
    private readonly ILogger<PasswordResetRequestedHandler> _logger;

    public PasswordResetRequestedHandler(ILogger<PasswordResetRequestedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(PasswordResetRequested notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password reset requested for user {UserId}", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class PermissionGrantedHandler : INotificationHandler<PermissionGranted>
{
    private readonly ILogger<PermissionGrantedHandler> _logger;

    public PermissionGrantedHandler(ILogger<PermissionGrantedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(PermissionGranted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Permission {PermissionId} granted to user {UserId}", notification.PermissionId, notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class PermissionRevokedHandler : INotificationHandler<PermissionRevoked>
{
    private readonly ILogger<PermissionRevokedHandler> _logger;

    public PermissionRevokedHandler(ILogger<PermissionRevokedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(PermissionRevoked notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Permission {PermissionId} revoked from user {UserId}", notification.PermissionId, notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class RoleAssignedHandler : INotificationHandler<RoleAssigned>
{
    private readonly ILogger<RoleAssignedHandler> _logger;

    public RoleAssignedHandler(ILogger<RoleAssignedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(RoleAssigned notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Role {RoleId} assigned to user {UserId}", notification.RoleId, notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class RoleRevokedHandler : INotificationHandler<RoleRevoked>
{
    private readonly ILogger<RoleRevokedHandler> _logger;

    public RoleRevokedHandler(ILogger<RoleRevokedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(RoleRevoked notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Role {RoleId} revoked from user {UserId}", notification.RoleId, notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserActivatedHandler : INotificationHandler<UserActivated>
{
    private readonly ILogger<UserActivatedHandler> _logger;

    public UserActivatedHandler(ILogger<UserActivatedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserActivated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} activated", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserCreatedHandler : INotificationHandler<UserCreated>
{
    private readonly ILogger<UserCreatedHandler> _logger;

    public UserCreatedHandler(ILogger<UserCreatedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserCreated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} created with email {Email}", notification.UserId, notification.Email);
        await Task.CompletedTask;
    }
}

public sealed class UserDeletedHandler : INotificationHandler<UserDeleted>
{
    private readonly ILogger<UserDeletedHandler> _logger;

    public UserDeletedHandler(ILogger<UserDeletedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserDeleted notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} deleted", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserLoggedInHandler : INotificationHandler<UserLoggedIn>
{
    private readonly ILogger<UserLoggedInHandler> _logger;

    public UserLoggedInHandler(ILogger<UserLoggedInHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserLoggedIn notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} logged in", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserLoginFailedHandler : INotificationHandler<UserLoginFailed>
{
    private readonly ILogger<UserLoginFailedHandler> _logger;

    public UserLoginFailedHandler(ILogger<UserLoginFailedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserLoginFailed notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Login failed for user {UserId}", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserPasswordChangedHandler : INotificationHandler<UserPasswordChanged>
{
    private readonly ILogger<UserPasswordChangedHandler> _logger;

    public UserPasswordChangedHandler(ILogger<UserPasswordChangedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserPasswordChanged notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Password changed for user {UserId}", notification.UserId);
        await Task.CompletedTask;
    }
}

public sealed class UserSuspendedHandler : INotificationHandler<UserSuspended>
{
    private readonly ILogger<UserSuspendedHandler> _logger;

    public UserSuspendedHandler(ILogger<UserSuspendedHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask Handle(UserSuspended notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} suspended", notification.UserId);
        await Task.CompletedTask;
    }
}
