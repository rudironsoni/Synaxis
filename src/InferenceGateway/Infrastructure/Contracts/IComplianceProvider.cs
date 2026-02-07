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
        /// Gets the regulation code (GDPR, LGPD, CCPA, etc.).
        /// </summary>
        string RegulationCode { get; }

        /// <summary>
        /// Gets the region this regulation applies to.
        /// </summary>
        string Region { get; }

        /// <summary>
        /// Validate if a cross-border transfer is compliant.
        /// </summary>
        /// <param name="context">The transfer context.</param>
        /// <returns>True if compliant, false otherwise.</returns>
        Task<bool> ValidateTransferAsync(TransferContext context);

        /// <summary>
        /// Log a cross-border transfer for audit purposes.
        /// </summary>
        /// <param name="context">The transfer context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LogTransferAsync(TransferContext context);

        /// <summary>
        /// Export all data for a user (data portability).
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The exported data.</returns>
        Task<DataExport> ExportUserDataAsync(Guid userId);

        /// <summary>
        /// Delete all data for a user (right to erasure).
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>True if deletion succeeded.</returns>
        Task<bool> DeleteUserDataAsync(Guid userId);

        /// <summary>
        /// Check if processing is allowed under this regulation.
        /// </summary>
        /// <param name="context">The processing context.</param>
        /// <returns>True if allowed.</returns>
        Task<bool> IsProcessingAllowedAsync(ProcessingContext context);

        /// <summary>
        /// Get retention period for this regulation.
        /// </summary>
        /// <returns>The retention period in days.</returns>
        int? GetDataRetentionDays();

        /// <summary>
        /// Check if breach notification is required.
        /// </summary>
        /// <param name="context">The breach context.</param>
        /// <returns>True if notification required.</returns>
        Task<bool> IsBreachNotificationRequiredAsync(BreachContext context);
    }

    /// <summary>
    /// Context for cross-border data transfer.
    /// </summary>
    public class TransferContext
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the source region.
        /// </summary>
        public string? FromRegion { get; set; }

        /// <summary>
        /// Gets or sets the destination region.
        /// </summary>
        public string? ToRegion { get; set; }

        /// <summary>
        /// Gets or sets the legal basis.
        /// </summary>
        public string? LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the purpose.
        /// </summary>
        public string? Purpose { get; set; }

        /// <summary>
        /// Gets or sets the data categories.
        /// </summary>
        public string[]? DataCategories { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether encryption is used.
        /// </summary>
        public bool EncryptionUsed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether user consent was obtained.
        /// </summary>
        public bool UserConsentObtained { get; set; }
    }

    /// <summary>
    /// Context for data processing.
    /// </summary>
    public class ProcessingContext
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the processing purpose.
        /// </summary>
        public string? ProcessingPurpose { get; set; }

        /// <summary>
        /// Gets or sets the legal basis.
        /// </summary>
        public string? LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the data categories.
        /// </summary>
        public string[]? DataCategories { get; set; }
    }

    /// <summary>
    /// Context for data breach.
    /// </summary>
    public class BreachContext
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the breach type.
        /// </summary>
        public string? BreachType { get; set; }

        /// <summary>
        /// Gets or sets the affected users count.
        /// </summary>
        public int AffectedUsersCount { get; set; }

        /// <summary>
        /// Gets or sets the data categories exposed.
        /// </summary>
        public string[]? DataCategoriesExposed { get; set; }

        /// <summary>
        /// Gets or sets the risk level (low, medium, high).
        /// </summary>
        public string? RiskLevel { get; set; }
    }

    /// <summary>
    /// Data export result.
    /// </summary>
    public class DataExport
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the export format.
        /// </summary>
        public string Format { get; set; } = "json";

        /// <summary>
        /// Gets or sets the exported data.
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Gets or sets the export timestamp.
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        public IDictionary<string, object>? Metadata { get; set; }
    }
}
