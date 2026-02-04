namespace Synaxis.InferenceGateway.WebApi.Hubs;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// SignalR hub for real-time Synaxis updates.
/// Enables real-time notifications for provider health, cost optimization,
/// model discovery, security alerts, and audit events.
/// </summary>
[Authorize]
public class SynaxisHub : Hub
{
    private readonly ILogger<SynaxisHub> _logger;

    public SynaxisHub(ILogger<SynaxisHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Join organization group for targeted updates.
    /// </summary>
    /// <param name="organizationId">The organization ID to join</param>
    public async Task JoinOrganization(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            _logger.LogWarning("Connection {ConnectionId} attempted to join with empty organization ID", Context.ConnectionId);
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }

        // TODO: Validate user belongs to organization
        // var userId = Context.User?.Identity?.Name;
        // if (!await _authService.UserBelongsToOrganization(userId, organizationId))
        // {
        //     throw new UnauthorizedAccessException("User does not belong to organization");
        // }

        await Groups.AddToGroupAsync(Context.ConnectionId, organizationId);
        _logger.LogInformation("Connection {ConnectionId} joined organization {OrganizationId}", 
            Context.ConnectionId, organizationId);
    }

    /// <summary>
    /// Leave organization group.
    /// </summary>
    /// <param name="organizationId">The organization ID to leave</param>
    public async Task LeaveOrganization(string organizationId)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            _logger.LogWarning("Connection {ConnectionId} attempted to leave with empty organization ID", Context.ConnectionId);
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, organizationId);
        _logger.LogInformation("Connection {ConnectionId} left organization {OrganizationId}", 
            Context.ConnectionId, organizationId);
    }

    /// <summary>
    /// Authentication check on connection.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("Unauthenticated connection attempt: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        _logger.LogInformation("Client connected: {ConnectionId}, User: {UserName}", 
            Context.ConnectionId, user.Identity.Name);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Handle disconnection.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
