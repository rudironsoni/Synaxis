// <copyright file="RateLimitResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    /// <summary>
    /// Represents the result of a rate limit check.
    /// </summary>
    public class RateLimitResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request is allowed.
        /// </summary>
        public bool IsAllowed { get; set; }

        /// <summary>
        /// Gets or sets the current usage count.
        /// </summary>
        public long Current { get; set; }

        /// <summary>
        /// Gets or sets the rate limit.
        /// </summary>
        public long Limit { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds until the limit resets.
        /// </summary>
        public long ResetAfter { get; set; }

        /// <summary>
        /// Gets or sets the remaining requests/tokens available.
        /// </summary>
        public long Remaining { get; set; }

        /// <summary>
        /// Gets or sets the scope that triggered the rate limit (User, Group, or Organization).
        /// </summary>
        public string? LimitedBy { get; set; }

        /// <summary>
        /// Gets or sets the rate limit type (RPM or TPM).
        /// </summary>
        public string? LimitType { get; set; }

        /// <summary>
        /// Creates a success result indicating the request is allowed.
        /// </summary>
        /// <param name="current">The current usage count.</param>
        /// <param name="limit">The rate limit.</param>
        /// <param name="resetAfter">Seconds until the limit resets.</param>
        /// <param name="limitType">The rate limit type (RPM or TPM).</param>
        /// <returns>An allowed rate limit result.</returns>
        public static RateLimitResult Allowed(long current, long limit, long resetAfter, string? limitType = null)
        {
            return new RateLimitResult
            {
                IsAllowed = true,
                Current = current,
                Limit = limit,
                ResetAfter = resetAfter,
                Remaining = Math.Max(0, limit - current),
                LimitType = limitType,
            };
        }

        /// <summary>
        /// Creates a failure result indicating the rate limit has been exceeded.
        /// </summary>
        /// <param name="current">The current usage count.</param>
        /// <param name="limit">The rate limit.</param>
        /// <param name="resetAfter">Seconds until the limit resets.</param>
        /// <param name="limitedBy">The scope that triggered the limit (User, Group, or Organization).</param>
        /// <param name="limitType">The rate limit type (RPM or TPM).</param>
        /// <returns>A denied rate limit result.</returns>
        public static RateLimitResult Denied(long current, long limit, long resetAfter, string limitedBy, string? limitType = null)
        {
            return new RateLimitResult
            {
                IsAllowed = false,
                Current = current,
                Limit = limit,
                ResetAfter = resetAfter,
                Remaining = 0,
                LimitedBy = limitedBy,
                LimitType = limitType,
            };
        }

        /// <summary>
        /// Gets the X-RateLimit headers for HTTP responses.
        /// </summary>
        /// <returns>A dictionary of HTTP header names and values.</returns>
        public IDictionary<string, string> ToHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                ["X-RateLimit-Limit"] = this.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["X-RateLimit-Remaining"] = this.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(this.ResetAfter).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            if (!string.IsNullOrEmpty(this.LimitedBy))
            {
                headers["X-RateLimit-Limited-By"] = this.LimitedBy;
            }

            if (!string.IsNullOrEmpty(this.LimitType))
            {
                headers["X-RateLimit-Type"] = this.LimitType;
            }

            return headers;
        }
    }
}
