// <copyright file="OrganizationSettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity
{
    /// <summary>
    /// Represents settings for an organization.
    /// </summary>
    public class OrganizationSettings
    {
        public Guid OrganizationId { get; set; }

        public int JwtTokenLifetimeMinutes { get; set; } = 10080;

        public int MaxRequestBodySizeBytes { get; set; } = 31457280;

        public int DefaultRateLimitRpm { get; set; } = 60;

        public int DefaultRateLimitTpm { get; set; } = 100000;

        public bool AllowAutoOptimization { get; set; } = true;

        public bool AllowCustomProviders { get; set; } = false;

        public bool AllowAuditLogExport { get; set; } = false;

        public int MaxUsers { get; set; } = 10;

        public int MaxGroups { get; set; } = 5;

        public long? MonthlyTokenQuota { get; set; }

        public int AuditLogRetentionDays { get; set; } = 90;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Guid UpdatedBy { get; set; }

        // Navigation properties
        public Organization Organization { get; set; } = null!;
    }
}
