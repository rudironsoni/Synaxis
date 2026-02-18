// <copyright file="SuperAdminAccessValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;

    /// <summary>
    /// Service for validating super admin access.
    /// </summary>
    public class SuperAdminAccessValidator : ISuperAdminAccessValidator
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly ILogger<SuperAdminAccessValidator> _logger;
        private readonly string _currentRegion;
        private readonly int _businessHoursStart;
        private readonly int _businessHoursEnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperAdminAccessValidator"/> class.
        /// </summary>
        /// <param name="userService">The user service.</param>
        /// <param name="auditService">The audit service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="currentRegion">The current region.</param>
        /// <param name="businessHoursStart">Business hours start.</param>
        /// <param name="businessHoursEnd">Business hours end.</param>
        public SuperAdminAccessValidator(
            IUserService userService,
            IAuditService auditService,
            ILogger<SuperAdminAccessValidator> logger,
            string currentRegion = "us-east-1",
            int businessHoursStart = 8,
            int businessHoursEnd = 18)
        {
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._currentRegion = currentRegion;
            this._businessHoursStart = businessHoursStart;
            this._businessHoursEnd = businessHoursEnd;
        }

        /// <inheritdoc/>
        public async Task<SuperAdminAccessValidation> ValidateAccessAsync(SuperAdminAccessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var validation = this.CreateValidation();
            var user = await this._userService.GetUserAsync(context.UserId).ConfigureAwait(false);

            var roleValidation = ValidateUserRole(user, validation);
            if (roleValidation != null)
            {
                return roleValidation;
            }

            var mfaValidation = await this.ValidateMfaAsync(user, context, validation).ConfigureAwait(false);
            if (mfaValidation != null)
            {
                return mfaValidation;
            }

            var ipValidation = await this.ValidateIpAddressAsync(user, context, validation).ConfigureAwait(false);
            if (ipValidation != null)
            {
                return ipValidation;
            }

            var justificationValidation = ValidateJustification(context, validation);
            if (justificationValidation != null)
            {
                return justificationValidation;
            }

            var businessHoursValidation = await this.ValidateBusinessHoursAsync(user, context, validation).ConfigureAwait(false);
            if (businessHoursValidation != null)
            {
                return businessHoursValidation;
            }

            validation.IsValid = true;
            await this.LogSuccessfulAccessAsync(user, context).ConfigureAwait(false);

            return validation;
        }

        /// <inheritdoc/>
        public bool IsIpAllowed(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            return ipAddress.StartsWith("10.", StringComparison.Ordinal) ||
                   ipAddress.StartsWith("172.", StringComparison.Ordinal) ||
                   ipAddress.StartsWith("192.168.", StringComparison.Ordinal) ||
                   string.Equals(ipAddress, "127.0.0.1", StringComparison.Ordinal) ||
                   string.Equals(ipAddress, "::1", StringComparison.Ordinal);
        }

        private SuperAdminAccessValidation CreateValidation()
        {
            return new SuperAdminAccessValidation
            {
                ValidatedAt = DateTime.UtcNow,
                MfaRequired = true,
                JustificationRequired = true,
            };
        }

        private static SuperAdminAccessValidation? ValidateUserRole(User user, SuperAdminAccessValidation validation)
        {
            if (!string.Equals(user.Role, "SuperAdmin", StringComparison.Ordinal))
            {
                validation.IsValid = false;
                validation.FailureReason = "User is not a Super Admin";
                return validation;
            }

            return null;
        }

        private async Task<SuperAdminAccessValidation?> ValidateMfaAsync(User user, SuperAdminAccessContext context, SuperAdminAccessValidation validation)
        {
            if (!user.MfaEnabled)
            {
                validation.IsValid = false;
                validation.MfaValid = false;
                validation.FailureReason = "MFA is not enabled for Super Admin account";
                return validation;
            }

            if (string.IsNullOrWhiteSpace(context.MfaCode))
            {
                validation.IsValid = false;
                validation.MfaValid = false;
                validation.FailureReason = "MFA code is required";
                return validation;
            }

            var mfaValid = await this._userService.VerifyMfaCodeAsync(context.UserId, context.MfaCode).ConfigureAwait(false);
            validation.MfaValid = mfaValid;

            if (!mfaValid)
            {
                validation.IsValid = false;
                validation.FailureReason = "Invalid MFA code";
                await this.LogMfaFailureAsync(user, context).ConfigureAwait(false);
                return validation;
            }

            return null;
        }

        private async Task<SuperAdminAccessValidation?> ValidateIpAddressAsync(User user, SuperAdminAccessContext context, SuperAdminAccessValidation validation)
        {
            validation.IpAllowed = this.IsIpAllowed(context.IpAddress);
            if (!validation.IpAllowed)
            {
                validation.IsValid = false;
                validation.FailureReason = "IP address not in allowlist";
                await this.LogUnauthorizedIpAttemptAsync(user, context).ConfigureAwait(false);
                return validation;
            }

            return null;
        }

        private static SuperAdminAccessValidation? ValidateJustification(SuperAdminAccessContext context, SuperAdminAccessValidation validation)
        {
            var sensitiveActions = new[] { "impersonate", "modify_limits", "export_pii", "delete_organization" };
            if (sensitiveActions.Contains(context.Action?.ToLowerInvariant()) &&
                string.IsNullOrWhiteSpace(context.Justification))
            {
                validation.IsValid = false;
                validation.FailureReason = "Justification is required for sensitive actions";
                return validation;
            }

            return null;
        }

        private async Task<SuperAdminAccessValidation?> ValidateBusinessHoursAsync(User user, SuperAdminAccessContext context, SuperAdminAccessValidation validation)
        {
            var requestTime = context.RequestTime == default ? DateTime.UtcNow : context.RequestTime;
            var currentHour = requestTime.Hour;
            validation.WithinBusinessHours = currentHour >= this._businessHoursStart && currentHour < this._businessHoursEnd;

            if (!validation.WithinBusinessHours)
            {
                validation.IsValid = false;
                validation.FailureReason = $"Super Admin access is restricted to business hours ({this._businessHoursStart}:00 - {this._businessHoursEnd}:00 UTC)";
                await this.LogOffHoursAttemptAsync(user, context, currentHour).ConfigureAwait(false);
                return validation;
            }

            return null;
        }

        private Task LogMfaFailureAsync(User user, SuperAdminAccessContext context)
        {
            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = user.OrganizationId,
                UserId = context.UserId,
                EventType = "SUPER_ADMIN_MFA_FAILED",
                EventCategory = "SECURITY",
                Action = "failed_mfa_verification",
                ResourceType = "SuperAdmin",
                ResourceId = context.UserId.ToString(),
                IpAddress = context.IpAddress,
                Region = this._currentRegion,
            });
        }

        private Task LogUnauthorizedIpAttemptAsync(User user, SuperAdminAccessContext context)
        {
            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = user.OrganizationId,
                UserId = context.UserId,
                EventType = "SUPER_ADMIN_UNAUTHORIZED_IP",
                EventCategory = "SECURITY",
                Action = "unauthorized_ip_attempt",
                ResourceType = "SuperAdmin",
                ResourceId = context.UserId.ToString(),
                IpAddress = context.IpAddress,
                Region = this._currentRegion,
            });
        }

        private Task LogOffHoursAttemptAsync(User user, SuperAdminAccessContext context, int currentHour)
        {
            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = user.OrganizationId,
                UserId = context.UserId,
                EventType = "SUPER_ADMIN_OFF_HOURS",
                EventCategory = "SECURITY",
                Action = "off_hours_access_attempt",
                ResourceType = "SuperAdmin",
                ResourceId = context.UserId.ToString(),
                IpAddress = context.IpAddress,
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "requested_action", context.Action ?? "unknown" },
                    { "hour_utc", currentHour },
                },
                Region = this._currentRegion,
            });
        }

        private Task LogSuccessfulAccessAsync(User user, SuperAdminAccessContext context)
        {
            this._logger.LogInformation(
                "Super admin access granted for user {UserId} performing action {Action}",
                context.UserId,
                context.Action);

            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = user.OrganizationId,
                UserId = context.UserId,
                EventType = "SUPER_ADMIN_ACCESS_GRANTED",
                EventCategory = "SECURITY",
                Action = "access_validation_success",
                ResourceType = "SuperAdmin",
                ResourceId = context.UserId.ToString(),
                IpAddress = context.IpAddress,
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "requested_action", context.Action ?? "unknown" },
                    { "justification", context.Justification ?? "none" },
                },
                Region = this._currentRegion,
            });
        }
    }
}
