// <copyright file="IFailoverService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages automatic failover between regions based on health status.
    /// </summary>
    public interface IFailoverService
    {
        /// <summary>
        /// Selects the best available region for a request.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="primaryRegion">The primary region preference.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the failover decision.</returns>
        Task<FailoverDecision> SelectRegionAsync(Guid organizationId, Guid userId, string primaryRegion);

        /// <summary>
        /// Handles failover when primary region is unhealthy.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="fromRegion">The source region.</param>
        /// <param name="toRegion">The target region.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the failover result.</returns>
        Task<FailoverResult> FailoverAsync(Guid organizationId, Guid userId, string fromRegion, string toRegion);

        /// <summary>
        /// Checks if user has given cross-border consent.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether consent was given.</returns>
        Task<bool> HasCrossBorderConsentAsync(Guid userId);

        /// <summary>
        /// Records cross-border transfer for compliance.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="fromRegion">The source region.</param>
        /// <param name="toRegion">The target region.</param>
        /// <param name="legalBasis">The legal basis for transfer.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RecordCrossBorderTransferAsync(Guid organizationId, Guid userId, string fromRegion, string toRegion, string legalBasis);

        /// <summary>
        /// Checks if primary region has recovered and can handle requests again.
        /// </summary>
        /// <param name="region">The region to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether region has recovered.</returns>
        Task<bool> CanRecoverToPrimaryAsync(string region);

        /// <summary>
        /// Gets failover notification message for user.
        /// </summary>
        /// <param name="fromRegion">The source region.</param>
        /// <param name="toRegion">The target region.</param>
        /// <param name="needsConsent">Whether consent is needed.</param>
        /// <returns>The failover notification message.</returns>
        string GetFailoverNotificationMessage(string fromRegion, string toRegion, bool needsConsent);
    }

    /// <summary>
    /// Represents the result of a region selection decision.
    /// </summary>
    public class FailoverDecision
    {
        /// <summary>
        /// Gets or sets the selected region.
        /// </summary>
        public string SelectedRegion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a failover scenario.
        /// </summary>
        public bool IsFailover { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether cross-border consent is needed.
        /// </summary>
        public bool NeedsCrossBorderConsent { get; set; }

        /// <summary>
        /// Gets or sets the reason for the decision.
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Gets or sets the list of healthy regions.
        /// </summary>
        public IList<string> HealthyRegions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents the result of a failover operation.
    /// </summary>
    public class FailoverResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the failover was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the target region.
        /// </summary>
        public string TargetRegion { get; set; }

        /// <summary>
        /// Gets or sets the result message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether consent is required.
        /// </summary>
        public bool ConsentRequired { get; set; }

        /// <summary>
        /// Gets or sets the consent URL.
        /// </summary>
        public string ConsentUrl { get; set; }

        /// <summary>
        /// Creates a successful failover result.
        /// </summary>
        /// <param name="targetRegion">The target region.</param>
        /// <param name="message">The success message.</param>
        /// <returns>A successful failover result.</returns>
        public static FailoverResult Succeeded(string targetRegion, string message)
            => new () { Success = true, TargetRegion = targetRegion, Message = message };

        /// <summary>
        /// Creates a failed failover result.
        /// </summary>
        /// <param name="message">The failure message.</param>
        /// <returns>A failed failover result.</returns>
        public static FailoverResult Failed(string message)
            => new () { Success = false, Message = message };

        /// <summary>
        /// Creates a failover result that requires consent.
        /// </summary>
        /// <param name="targetRegion">The target region.</param>
        /// <param name="consentUrl">The consent URL.</param>
        /// <returns>A failover result requiring consent.</returns>
        public static FailoverResult NeedsConsent(string targetRegion, string consentUrl)
            => new () { Success = false, TargetRegion = targetRegion, ConsentRequired = true, ConsentUrl = consentUrl };
    }
}
