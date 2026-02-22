// <copyright file="AuditService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// AuditService class.
    /// </summary>
    public sealed class AuditService : IAuditService
    {
        private readonly SynaxisDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditService"/> class.
        /// </summary>
        /// <param name="dbContext">The dbContext.</param>
        public AuditService(SynaxisDbContext dbContext)
        {
            ArgumentNullException.ThrowIfNull(dbContext);
            this._dbContext = dbContext;
        }

        /// <inheritdoc/>
        public Task LogAsync(Guid tenantId, Guid? userId, string action, object? payload, CancellationToken cancellationToken = default)
        {
            var metadata = new Dictionary<string, object>();
            if (payload != null)
            {
                metadata["payload"] = payload;
            }

            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                OrganizationId = tenantId,
                UserId = userId,
                EventType = action,
                EventCategory = "operation",
                Action = action,
                ResourceType = "team",
                Metadata = metadata,
                Region = "unknown",
                Timestamp = DateTime.UtcNow,
                IntegrityHash = string.Empty,
            };

            log.IntegrityHash = ComputeIntegrityHash(log);

            this._dbContext.AuditLogs.Add(log);
            return this._dbContext.SaveChangesAsync(cancellationToken);
        }

        private static string ComputeIntegrityHash(AuditLog log)
        {
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
