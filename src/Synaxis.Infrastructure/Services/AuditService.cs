using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Service for immutable audit logging with tamper detection.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(SynaxisDbContext context, ILogger<AuditService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuditLog> LogEventAsync(AuditEvent auditEvent)
        {
            if (auditEvent == null)
            {
                throw new ArgumentNullException(nameof(auditEvent));
            }

            if (auditEvent.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(auditEvent));
            }

            if (string.IsNullOrWhiteSpace(auditEvent.EventType))
            {
                throw new ArgumentException("EventType is required", nameof(auditEvent));
            }

            // Get previous log hash for chain verification
            var previousLog = await _context.Set<AuditLog>()
                .Where(al => al.OrganizationId == auditEvent.OrganizationId)
                .OrderByDescending(al => al.Timestamp)
                .FirstOrDefaultAsync();

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = auditEvent.OrganizationId,
                UserId = auditEvent.UserId,
                EventType = auditEvent.EventType,
                EventCategory = auditEvent.EventCategory ?? "general",
                Action = auditEvent.Action ?? auditEvent.EventType,
                ResourceType = auditEvent.ResourceType,
                ResourceId = auditEvent.ResourceId,
                Metadata = auditEvent.Metadata ?? new Dictionary<string, object>(),
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                Region = auditEvent.Region ?? "unknown",
                PreviousHash = previousLog?.IntegrityHash,
                Timestamp = DateTime.UtcNow
            };

            // Compute integrity hash
            auditLog.IntegrityHash = ComputeIntegrityHash(auditLog);

            // Add to database (immutable - no updates allowed)
            _context.Set<AuditLog>().Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: Action={Action}, UserId={UserId}, ResourceType={ResourceType}, ResourceId={ResourceId}, OrganizationId={OrganizationId}, Timestamp={Timestamp}, Success={Success}",
                auditLog.Action,
                auditLog.UserId,
                auditLog.ResourceType,
                auditLog.ResourceId,
                auditLog.OrganizationId,
                auditLog.Timestamp,
                true);

            return auditLog;
        }

        public async Task<IList<AuditLog>> QueryAuditLogsAsync(AuditQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(query));
            }

            _logger.LogDebug(
                "Querying audit logs: OrganizationId={OrganizationId}, UserId={UserId}, EventType={EventType}, PageNumber={PageNumber}, PageSize={PageSize}",
                query.OrganizationId,
                query.UserId,
                query.EventType,
                query.PageNumber,
                query.PageSize);

            var logsQuery = _context.Set<AuditLog>()
                .Where(al => al.OrganizationId == query.OrganizationId);

            if (query.UserId.HasValue)
            {
                logsQuery = logsQuery.Where(al => al.UserId == query.UserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.EventType))
            {
                logsQuery = logsQuery.Where(al => al.EventType == query.EventType);
            }

            if (!string.IsNullOrWhiteSpace(query.EventCategory))
            {
                logsQuery = logsQuery.Where(al => al.EventCategory == query.EventCategory);
            }

            if (query.StartDate.HasValue)
            {
                logsQuery = logsQuery.Where(al => al.Timestamp >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                logsQuery = logsQuery.Where(al => al.Timestamp <= query.EndDate.Value);
            }

            // Apply pagination
            var logs = await logsQuery
                .OrderByDescending(al => al.Timestamp)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            _logger.LogInformation(
                "Query completed: Found {Count} audit logs for OrganizationId={OrganizationId}",
                logs.Count,
                query.OrganizationId);

            return logs;
        }

        public async Task<AuditLog> GetAuditLogAsync(Guid logId)
        {
            _logger.LogDebug("Retrieving audit log: LogId={LogId}", logId);

            var log = await _context.Set<AuditLog>().FindAsync(logId);

            if (log == null)
            {
                _logger.LogWarning("Audit log not found: LogId={LogId}", logId);
                throw new InvalidOperationException($"Audit log {logId} not found");
            }

            return log;
        }

        public async Task<byte[]> ExportAuditLogsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
        {
            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(organizationId));
            }

            var logs = await _context.Set<AuditLog>()
                .Where(al => al.OrganizationId == organizationId
                    && al.Timestamp >= startDate
                    && al.Timestamp <= endDate)
                .OrderBy(al => al.Timestamp)
                .ToListAsync();

            // Export as JSON
            var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation("Exported {Count} audit logs for organization {OrganizationId}",
                logs.Count, organizationId);

            return Encoding.UTF8.GetBytes(json);
        }

        public async Task<bool> VerifyIntegrityAsync(Guid logId)
        {
            _logger.LogDebug("Verifying audit log integrity: LogId={LogId}", logId);

            var log = await GetAuditLogAsync(logId);

            // Recompute hash and compare (must include previous hash that was used during creation)
            var computedHash = ComputeIntegrityHash(log, useStoredHash: true);
            var isValid = computedHash == log.IntegrityHash;

            if (!isValid)
            {
                _logger.LogWarning(
                    "Integrity check failed for audit log: LogId={LogId}, ExpectedHash={ExpectedHash}, ComputedHash={ComputedHash}",
                    logId,
                    log.IntegrityHash,
                    computedHash);
            }

            // Verify chain if previous hash exists
            if (!string.IsNullOrWhiteSpace(log.PreviousHash))
            {
                var previousLog = await _context.Set<AuditLog>()
                    .Where(al => al.OrganizationId == log.OrganizationId
                        && al.Timestamp < log.Timestamp)
                    .OrderByDescending(al => al.Timestamp)
                    .FirstOrDefaultAsync();

                if (previousLog != null && previousLog.IntegrityHash != log.PreviousHash)
                {
                    _logger.LogWarning(
                        "Chain verification failed for audit log: LogId={LogId}, PreviousLogId={PreviousLogId}",
                        logId,
                        previousLog.Id);
                    return false;
                }
            }

            _logger.LogInformation(
                "Integrity verification completed: LogId={LogId}, IsValid={IsValid}",
                logId,
                isValid);

            return isValid;
        }

        public async Task<AuditAggregationResult> AggregateAnonymizedLogsAsync(DateTime startDate, DateTime endDate)
        {
            // Cross-region aggregation with anonymized data
            var logs = await _context.Set<AuditLog>()
                .Where(al => al.Timestamp >= startDate && al.Timestamp <= endDate)
                .Select(al => new
                {
                    al.EventType,
                    al.EventCategory,
                    al.Region
                })
                .ToListAsync();

            var result = new AuditAggregationResult
            {
                TotalEvents = logs.Count,
                EventsByType = logs.GroupBy(l => l.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByCategory = logs.GroupBy(l => l.EventCategory)
                    .ToDictionary(g => g.Key, g => g.Count()),
                EventsByRegion = logs.GroupBy(l => l.Region)
                    .ToDictionary(g => g.Key, g => g.Count()),
                StartDate = startDate,
                EndDate = endDate
            };

            _logger.LogInformation("Aggregated {Count} anonymized audit events", result.TotalEvents);

            return result;
        }

        // Private helper methods
        private string ComputeIntegrityHash(AuditLog log, bool useStoredHash = true)
        {
            // Create deterministic string representation for hashing
            var data = $"{log.Id}|{log.OrganizationId}|{log.UserId}|" +
                       $"{log.EventType}|{log.EventCategory}|{log.Action}|" +
                       $"{log.ResourceType}|{log.ResourceId}|" +
                       $"{JsonSerializer.Serialize(log.Metadata)}|" +
                       $"{log.IpAddress}|{log.UserAgent}|{log.Region}|" +
                       $"{(useStoredHash ? log.PreviousHash : string.Empty)}|" +
                       $"{log.Timestamp:O}";

            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}
