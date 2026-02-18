// <copyright file="ImpersonationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for impersonation operations.
    /// </summary>
    public class ImpersonationService : IImpersonationService
    {
        private readonly SynaxisDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ImpersonationService> _logger;
        private readonly string _currentRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImpersonationService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="auditService">The audit service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="currentRegion">The current region.</param>
        public ImpersonationService(
            SynaxisDbContext context,
            IAuditService auditService,
            ILogger<ImpersonationService> logger,
            string currentRegion = "us-east-1")
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._currentRegion = currentRegion;
        }

        /// <inheritdoc/>
        public async Task<ImpersonationToken> GenerateImpersonationTokenAsync(ImpersonationRequest request)
        {
            ValidateImpersonationRequest(request);

            this._logger.LogWarning(
                "Generating impersonation token for user {UserId} in org {OrgId}. Justification: {Justification}",
                request.UserId,
                request.OrganizationId,
                request.Justification);

            await this.ValidateUserExistsAsync(request.UserId, request.OrganizationId).ConfigureAwait(false);

            var tokenData = CreateTokenData(request);
            var token = GenerateSecureToken(tokenData);

            await this.LogImpersonationEventAsync(request, tokenData).ConfigureAwait(false);

            return new ImpersonationToken
            {
                Token = token,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
                Justification = request.Justification,
            };
        }

        private static void ValidateImpersonationRequest(ImpersonationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Justification))
            {
                throw new ArgumentException("Justification is required for impersonation", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                throw new ArgumentException("Approval is required for impersonation", nameof(request));
            }
        }

        private async Task ValidateUserExistsAsync(Guid userId, Guid organizationId)
        {
            var user = await this._context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == organizationId)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User {userId} not found in organization {organizationId}");
            }
        }

        private static object CreateTokenData(ImpersonationRequest request)
        {
            return new
            {
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
                Justification = request.Justification,
                ApprovedBy = request.ApprovedBy,
                Type = "Impersonation",
            };
        }

        private Task LogImpersonationEventAsync(ImpersonationRequest request, object tokenData)
        {
            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                EventType = "SUPER_ADMIN_IMPERSONATION",
                EventCategory = "SECURITY",
                Action = "generate_impersonation_token",
                ResourceType = "User",
                ResourceId = request.UserId.ToString(),
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "justification", request.Justification },
                    { "approved_by", request.ApprovedBy },
                    { "duration_minutes", request.DurationMinutes },
                    { "expires_at", ((dynamic)tokenData).ExpiresAt },
                },
                Region = this._currentRegion,
            });
        }

        private static string GenerateSecureToken(object tokenData)
        {
            var json = JsonSerializer.Serialize(tokenData);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("super-secret-key-should-be-in-config"));
            var hash = hmac.ComputeHash(bytes);

            var tokenBytes = bytes.Concat(hash).ToArray();
            return Convert.ToBase64String(tokenBytes);
        }
    }
}
