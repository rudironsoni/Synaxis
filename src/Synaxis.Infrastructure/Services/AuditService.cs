// <copyright file="AuditService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for immutable audit logging with tamper detection.
    /// Uses repository for persistence operations.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditLogRepository _repository;
        private readonly ILogger<AuditService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditService"/> class.
        /// </summary>
        /// <param name="repository">The audit log repository.</param>
        /// <param name="logger">The logger.</param>
        public AuditService(IAuditLogRepository repository, ILogger<AuditService> logger)
        {
            ArgumentNullException.ThrowIfNull(repository);
            ArgumentNullException.ThrowIfNull(logger);
            this._repository = repository;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task<AuditLog> LogEventAsync(AuditEvent auditEvent)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);

            if (auditEvent.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(auditEvent));
            }

            if (string.IsNullOrWhiteSpace(auditEvent.EventType))
            {
                throw new ArgumentException("EventType is required", nameof(auditEvent));
            }

            // Create the audit log entry
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
                Metadata = auditEvent.Metadata ?? new Dictionary<string, object>(StringComparer.Ordinal),
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                Region = auditEvent.Region ?? "unknown",
                PreviousHash = string.Empty, // Will be set after computing hash
                Timestamp = DateTime.UtcNow,
            };

            // Compute integrity hash
            auditLog.IntegrityHash = ComputeIntegrityHash(auditLog);

            // Add to repository (immutable - no updates allowed)
            await this._repository.AddAsync(auditLog).ConfigureAwait(false);

            this._logger.LogInformation(
                "Audit event logged: {EventType} for org {OrganizationId}",
                auditEvent.EventType,
                auditEvent.OrganizationId);

            return auditLog;
        }

        /// <inheritdoc/>
        public async Task LogEventBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
        {
            var events = auditEvents.ToList();
            var logs = events.Select(MapToAuditLog).ToList();

            // Compute integrity hashes
            AuditLog? previousLog = null;
            foreach (var log in logs)
            {
                log.ComputeIntegrityHash(previousLog);
                previousLog = log;
            }

            await this._repository.AddBatchAsync(logs, cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation(
                "Logged batch of {Count} audit events",
                events.Count);
        }

        /// <inheritdoc/>
        public async Task<IList<AuditLog>> QueryAuditLogsAsync(AuditQuery query)
        {
            ArgumentNullException.ThrowIfNull(query);

            if (query.OrganizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(query));
            }

            var results = await this._repository.QueryAsync(query).ConfigureAwait(false);
            return results.ToList();
        }

        /// <inheritdoc/>
        public async Task<AuditLog> GetAuditLogAsync(Guid logId)
        {
            var log = await this._repository.GetByIdAsync(logId).ConfigureAwait(false);

            if (log == null)
            {
                throw new InvalidOperationException($"Audit log {logId} not found");
            }

            return log;
        }

        /// <inheritdoc/>
        public async Task<byte[]> ExportAuditLogsAsync(Guid organizationId, DateTime startDate, DateTime endDate)
        {
            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("OrganizationId is required", nameof(organizationId));
            }

            var criteria = new AuditSearchCriteria(
                OrganizationId: organizationId,
                FromDate: startDate,
                ToDate: endDate,
                PageSize: int.MaxValue);

            var result = await this._repository.SearchAsync(criteria).ConfigureAwait(false);

            // Export as JSON
            var json = JsonSerializer.Serialize(result.Items, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            this._logger.LogInformation(
                "Exported {Count} audit logs for organization {OrganizationId}",
                result.Items.Count,
                organizationId);

            return Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc/>
        public async Task<bool> VerifyIntegrityAsync(Guid logId)
        {
            var log = await this.GetAuditLogAsync(logId).ConfigureAwait(false);

            // Recompute hash and compare
            var computedHash = ComputeIntegrityHash(log);
            var isValid = string.Equals(computedHash, log.IntegrityHash, StringComparison.Ordinal);

            if (!isValid)
            {
                this._logger.LogWarning("Integrity check failed for audit log {LogId}", logId);
            }

            return isValid;
        }

        /// <inheritdoc/>
        public async Task<AuditAggregationResult> AggregateAnonymizedLogsAsync(DateTime startDate, DateTime endDate)
        {
            // Use search with date range for aggregation
            var criteria = new AuditSearchCriteria(
                FromDate: startDate,
                ToDate: endDate,
                PageSize: int.MaxValue);

            var result = await this._repository.SearchAsync(criteria).ConfigureAwait(false);
            var logs = result.Items;

            var aggregationResult = new AuditAggregationResult
            {
                TotalEvents = logs.Count,
                EventsByType = logs.GroupBy(l => l.EventType, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
                EventsByCategory = logs.GroupBy(l => l.EventCategory, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
                EventsByRegion = logs.GroupBy(l => l.Region, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
                StartDate = startDate,
                EndDate = endDate,
            };

            this._logger.LogInformation("Aggregated {Count} anonymized audit events", aggregationResult.TotalEvents);

            return aggregationResult;
        }

        /// <summary>
        /// Maps an audit event to an audit log entry.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        /// <returns>The audit log entry.</returns>
        private static AuditLog MapToAuditLog(AuditEvent auditEvent)
        {
            return new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = auditEvent.OrganizationId,
                UserId = auditEvent.UserId,
                EventType = auditEvent.EventType,
                EventCategory = auditEvent.EventCategory ?? "general",
                Action = auditEvent.Action ?? auditEvent.EventType,
                ResourceType = auditEvent.ResourceType,
                ResourceId = auditEvent.ResourceId,
                Metadata = auditEvent.Metadata ?? new Dictionary<string, object>(StringComparer.Ordinal),
                IpAddress = auditEvent.IpAddress,
                UserAgent = auditEvent.UserAgent,
                Region = auditEvent.Region ?? "unknown",
                PreviousHash = string.Empty,
                Timestamp = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// Computes the integrity hash for an audit log entry.
        /// </summary>
        /// <param name="log">The audit log to hash.</param>
        /// <returns>A base64-encoded SHA256 hash.</returns>
        private static string ComputeIntegrityHash(AuditLog log)
        {
            // Create deterministic string representation for hashing
            var data = $"{log.Id}|{log.OrganizationId}|{log.UserId}|" +
                       $"{log.EventType}|{log.EventCategory}|{log.Action}|" +
                       $"{log.ResourceType}|{log.ResourceId}|" +
                       $"{JsonSerializer.Serialize(log.Metadata)}|" +
                       $"{log.IpAddress}|{log.UserAgent}|{log.Region}|" +
                       $"{log.PreviousHash}|" +
                       $"{log.Timestamp:O}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }
    }
}
