using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

namespace Synaxis.Application.Services.Audit;

/// <summary>
/// Provides methods for logging audit events at the application layer.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Logs a single audit event asynchronously.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a batch of audit events asynchronously.
    /// </summary>
    /// <param name="auditEvents">The audit events to log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application-level service for logging audit events.
/// Validates events and delegates to the infrastructure audit service.
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly IAuditLogRepository _repository;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditLoggingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLoggingService"/> class.
    /// </summary>
    /// <param name="repository">The audit log repository.</param>
    /// <param name="auditService">The audit service.</param>
    /// <param name="logger">The logger.</param>
    public AuditLoggingService(
        IAuditLogRepository repository,
        IAuditService auditService,
        ILogger<AuditLoggingService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (!auditEvent.IsValid())
        {
            _logger.LogWarning("Invalid audit event rejected: {EventType}", auditEvent.EventType);
            throw new ArgumentException("Invalid audit event", nameof(auditEvent));
        }

        try
        {
            await _auditService.LogEventAsync(auditEvent, cancellationToken);
            _logger.LogDebug("Audit event logged: {EventType} by {UserId}",
                auditEvent.EventType, auditEvent.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit event: {EventType}", auditEvent.EventType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task LogBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvents);

        var events = auditEvents.ToList();
        if (events.Count == 0)
        {
            return;
        }

        var invalidEvents = events.Where(e => !e.IsValid()).ToList();
        if (invalidEvents.Any())
        {
            _logger.LogWarning("{Count} invalid audit events rejected", invalidEvents.Count);
            throw new ArgumentException("One or more audit events are invalid", nameof(auditEvents));
        }

        try
        {
            await _auditService.LogEventBatchAsync(events, cancellationToken);
            _logger.LogDebug("Batch of {Count} audit events logged", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log batch of {Count} audit events", events.Count);
            throw;
        }
    }
}

/// <summary>
/// Extension methods for validating audit events.
/// </summary>
public static class AuditEventValidator
{
    /// <summary>
    /// Validates an audit event.
    /// </summary>
    /// <param name="auditEvent">The audit event to validate.</param>
    /// <returns>True if the event is valid; otherwise, false.</returns>
    public static bool IsValid(this AuditEvent auditEvent)
    {
        if (auditEvent == null) return false;
        if (string.IsNullOrWhiteSpace(auditEvent.EventType)) return false;
        if (auditEvent.OrganizationId == Guid.Empty) return false;
        return true;
    }
}
