// <copyright file="ApiKeyUsageStatistics.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ApiKeys.Models
{
    using System;

    /// <summary>
    /// Response model for API key usage statistics.
    /// </summary>
    public class ApiKeyUsageStatistics
    {
        /// <summary>
        /// Gets or sets the API key ID.
        /// </summary>
        public Guid ApiKeyId { get; set; }

        /// <summary>
        /// Gets or sets the total number of requests.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the total number of successful requests.
        /// </summary>
        public long SuccessfulRequests { get; set; }

        /// <summary>
        /// Gets or sets the total number of failed requests.
        /// </summary>
        public long FailedRequests { get; set; }

        /// <summary>
        /// Gets or sets the date range start.
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// Gets or sets the date range end.
        /// </summary>
        public DateTime To { get; set; }

        /// <summary>
        /// Gets or sets the last used date.
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }
}
