// <copyright file="IComplianceProvider.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides compliance validation for data protection regulations (GDPR, LGPD, etc.)
    /// </summary>
    public interface IComplianceProvider
    {
        /// <summary>
        /// Regulation code: GDPR, LGPD, CCPA, etc.
        /// </summary>
        string RegulationCode { get; }

        /// <summary>
        /// Region this regulation applies to
        /// </summary>
        string Region { get; }

        /// <summary>
        /// Validate if a cross-border transfer is compliant
        /// </summary>
        Task<bool> ValidateTransferAsync(TransferContext context);

        /// <summary>
        /// Log a cross-border transfer for audit purposes
        /// </summary>
        Task LogTransferAsync(TransferContext context);

        /// <summary>
        /// Export all data for a user (data portability)
        /// </summary>
        Task<DataExport> ExportUserDataAsync(Guid userId);

        /// <summary>
        /// Delete all data for a user (right to erasure)
        /// </summary>
        Task<bool> DeleteUserDataAsync(Guid userId);

        /// <summary>
        /// Check if processing is allowed under this regulation
        /// </summary>
        Task<bool> IsProcessingAllowedAsync(ProcessingContext context);

        /// <summary>
        /// Get retention period for this regulation
        /// </summary>
        int? GetDataRetentionDays();

        /// <summary>
        /// Check if breach notification is required
        /// </summary>
        Task<bool> IsBreachNotificationRequiredAsync(BreachContext context);
    }

    public class TransferContext
    {
        public Guid OrganizationId { get; set; }

        public Guid? UserId { get; set; }

        public string? FromRegion { get; set; }

        public string? ToRegion { get; set; }

        public string? LegalBasis { get; set; }

        public string? Purpose { get; set; }

        public string[]? DataCategories { get; set; }

        public bool EncryptionUsed { get; set; }

        public bool UserConsentObtained { get; set; }
    }

    public class ProcessingContext
    {
        public Guid OrganizationId { get; set; }

        public Guid? UserId { get; set; }

        public string? ProcessingPurpose { get; set; }

        public string? LegalBasis { get; set; }

        public string[]? DataCategories { get; set; }
    }

    public class BreachContext
    {
        public Guid OrganizationId { get; set; }

        public string? BreachType { get; set; }

        public int AffectedUsersCount { get; set; }

        public string[]? DataCategoriesExposed { get; set; }

        public string? RiskLevel { get; set; } // low, medium, high
    }

    public class DataExport
    {
        public Guid UserId { get; set; }

        public string Format { get; set; } = "json";

        public byte[]? Data { get; set; }

        public DateTime ExportedAt { get; set; }

        public IDictionary<string, object>? Metadata { get; set; }
    }
}
