using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Application.Agents;

/// <summary>
/// Base interface for all Synaxis agents with tenant context.
/// </summary>
public interface ISynaxisAgent
{
    Guid? OrganizationId { get; set; }
    Guid? UserId { get; set; }
    Guid? GroupId { get; set; }
    string Name { get; }
}

/// <summary>
/// Base class for all Synaxis agents with tenant context and tool access.
/// </summary>
public abstract class SynaxisAgent : ISynaxisAgent
{
    public Guid? OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? GroupId { get; set; }
    public abstract string Name { get; }

    protected ILogger Logger { get; }

    protected SynaxisAgent(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Get a correlation ID for logging agent actions.
    /// </summary>
    protected string GetCorrelationId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Log agent action with tenant context.
    /// </summary>
    protected void LogAction(string action, string details, string correlationId)
    {
        Logger.LogInformation(
            "[{Agent}][{CorrelationId}] Action: {Action}, OrgId: {OrgId}, UserId: {UserId}, Details: {Details}",
            Name, correlationId, action, OrganizationId, UserId, details);
    }

    /// <summary>
    /// Log agent error with tenant context.
    /// </summary>
    protected void LogError(Exception ex, string action, string correlationId)
    {
        Logger.LogError(ex,
            "[{Agent}][{CorrelationId}] Error during {Action}, OrgId: {OrgId}, UserId: {UserId}",
            Name, correlationId, action, OrganizationId, UserId);
    }
}
