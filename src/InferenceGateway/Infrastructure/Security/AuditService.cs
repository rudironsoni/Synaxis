// <copyright file="AuditService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Text.Json;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// AuditService class.
    /// </summary>
    public sealed class AuditService : IAuditService
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly ILogger<AuditService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditService"/> class.
        /// </summary>
        /// <param name="dbContext">The dbContext.</param>
        /// <param name="logger">The logger.</param>
        public AuditService(SynaxisDbContext dbContext, ILogger<AuditService> logger)
        {
            if (dbContext is null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this._dbContext = dbContext;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default)
        {
            try
            {
                var log = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = tenantId,
                    UserId = userId,
                    EventType = action,
                    EventCategory = "general",
                    Action = action,
                    ResourceType = string.Empty,
                    ResourceId = string.Empty,
                    Metadata = payload != null
                        ? new Dictionary<string, object> { { "payload", payload } }
                        : new Dictionary<string, object>(),
                    IpAddress = string.Empty,
                    UserAgent = string.Empty,
                    Region = "unknown",
                    IntegrityHash = string.Empty,
                    PreviousHash = string.Empty,
                    Timestamp = DateTime.UtcNow,
                };

                this._dbContext.AuditLogs.Add(log);
                await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                this._logger.LogInformation(
                    "Audit log created: Action={Action}, UserId={UserId}, OrganizationId={OrganizationId}, Timestamp={Timestamp}",
                    action,
                    userId,
                    tenantId,
                    log.Timestamp);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                this._logger.LogError(
                    ex,
                    "Failed to create audit log: Action={Action}, UserId={UserId}, OrganizationId={OrganizationId}",
                    action,
                    userId,
                    tenantId);
                throw new InvalidOperationException($"Failed to create audit log for action '{action}'", ex);
            }
        }
    }
}
