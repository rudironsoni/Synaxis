// <copyright file="IComplianceProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides compliance validation for data protection regulations (GDPR, LGPD, etc.).
    /// </summary>
    public interface IComplianceProvider
    {
        /// <summary>
        /// Gets the regulation code: GDPR, LGPD, CCPA, etc.
        /// </summary>
        string RegulationCode { get; }

        /// <summary>
        /// Gets the region this regulation applies to.
        /// </summary>
        string Region { get; }

        /// <summary>
        /// Validate if a cross-border transfer is compliant.
        /// </summary>
        /// <param name="context">The transfer context containing transfer details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the transfer is valid.</returns>
        Task<bool> ValidateTransferAsync(TransferContext context);

        /// <summary>
        /// Log a cross-border transfer for audit purposes.
        /// </summary>
        /// <param name="context">The transfer context containing transfer details.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LogTransferAsync(TransferContext context);

        /// <summary>
        /// Export all data for a user (data portability).
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the exported data.</returns>
        Task<DataExport> ExportUserDataAsync(Guid userId);

        /// <summary>
        /// Delete all data for a user (right to erasure).
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the deletion was successful.</returns>
        Task<bool> DeleteUserDataAsync(Guid userId);

        /// <summary>
        /// Check if processing is allowed under this regulation.
        /// </summary>
        /// <param name="context">The processing context containing processing details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether processing is allowed.</returns>
        Task<bool> IsProcessingAllowedAsync(ProcessingContext context);

        /// <summary>
        /// Get retention period for this regulation.
        /// </summary>
        /// <returns>The data retention period in days, or null if not specified.</returns>
        int? GetDataRetentionDays();

        /// <summary>
        /// Check if breach notification is required.
        /// </summary>
        /// <param name="context">The breach context containing breach details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether notification is required.</returns>
        Task<bool> IsBreachNotificationRequiredAsync(BreachContext context);
    }

    /// <summary>
    /// Represents the context for a cross-border data transfer.
    /// </summary>
    public class TransferContext
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the source region.
        /// </summary>
        public required string FromRegion { get; set; }

        /// <summary>
        /// Gets or sets the destination region.
        /// </summary>
        public required string ToRegion { get; set; }

        /// <summary>
        /// Gets or sets the legal basis for transfer.
        /// </summary>
        public required string LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the purpose of transfer.
        /// </summary>
        public required string Purpose { get; set; }

        /// <summary>
        /// Gets or sets the data categories being transferred.
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
    /// Represents the context for data processing.
    /// </summary>
    public class ProcessingContext
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the processing purpose.
        /// </summary>
        public required string ProcessingPurpose { get; set; }

        /// <summary>
        /// Gets or sets the legal basis for processing.
        /// </summary>
        public required string LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the data categories being processed.
        /// </summary>
        public string[]? DataCategories { get; set; }
    }

    /// <summary>
    /// Represents the context for a data breach.
    /// </summary>
    public class BreachContext
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the breach type.
        /// </summary>
        public required string BreachType { get; set; }

        /// <summary>
        /// Gets or sets the number of affected users.
        /// </summary>
        public int AffectedUsersCount { get; set; }

        /// <summary>
        /// Gets or sets the data categories exposed in breach.
        /// </summary>
        public string[]? DataCategoriesExposed { get; set; }

        /// <summary>
        /// Gets or sets the risk level (low, medium, high).
        /// </summary>
        public required string RiskLevel { get; set; }
    }

    /// <summary>
    /// Represents exported user data.
    /// </summary>
    public class DataExport
    {
        /// <summary>
        /// Gets or sets the user identifier.
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
        /// Gets or sets the timestamp when data was exported.
        /// </summary>
        public DateTime ExportedAt { get; set; }

        /// <summary>
        /// Gets or sets the export metadata.
        /// </summary>
        public required IDictionary<string, object> Metadata { get; set; }
    }
}
