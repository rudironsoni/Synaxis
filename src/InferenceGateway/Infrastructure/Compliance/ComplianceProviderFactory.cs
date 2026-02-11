// <copyright file="ComplianceProviderFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace Synaxis.InferenceGateway.Infrastructure.Compliance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Synaxis.InferenceGateway.Infrastructure.Contracts;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.Infrastructure.Data;
    using ControlPlaneSynaxisDbContext = Synaxis.InferenceGateway.Infrastructure.ControlPlane.SynaxisDbContext;

    /// <summary>
    /// Factory for creating and managing compliance providers based on region.
    /// Supports GDPR (EU) and LGPD (Brazil) with fallback strategy.
    /// </summary>
    public class ComplianceProviderFactory
    {
        private readonly Dictionary<string, IComplianceProvider> _providers;
        private readonly IComplianceProvider _defaultProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceProviderFactory"/> class.
        /// </summary>
        /// <param name="auditDbContext">The audit database context.</param>
        /// <param name="controlPlaneDbContext">The control plane database context.</param>
        public ComplianceProviderFactory(
            Synaxis.Infrastructure.Data.SynaxisDbContext auditDbContext,
            ControlPlaneSynaxisDbContext controlPlaneDbContext)
        {
            if (auditDbContext == null)
            {
                throw new ArgumentNullException(nameof(auditDbContext));
            }

            if (controlPlaneDbContext == null)
            {
                throw new ArgumentNullException(nameof(controlPlaneDbContext));
            }

            // Initialize providers
            var gdprProvider = new GdprComplianceProvider(controlPlaneDbContext, auditDbContext);
            var lgpdProvider = new LgpdComplianceProvider(auditDbContext, controlPlaneDbContext);

            this._providers = new Dictionary<string, IComplianceProvider>(StringComparer.OrdinalIgnoreCase)
            {
                // GDPR for EU regions
                ["EU"] = gdprProvider,
                ["eu-west-1"] = gdprProvider,
                ["eu-central-1"] = gdprProvider,
                ["eu-north-1"] = gdprProvider,
                ["eu-south-1"] = gdprProvider,

                // LGPD for Brazilian regions
                ["BR"] = lgpdProvider,
                ["sa-east-1"] = lgpdProvider,
                ["br-south-1"] = lgpdProvider,
                ["sa-saopaulo-1"] = lgpdProvider,
            };

            // Default to GDPR as it's most stringent
            this._defaultProvider = gdprProvider;
        }

        /// <summary>
        /// Gets a compliance provider for the specified region.
        /// Returns the appropriate provider or defaults to GDPR if no specific provider exists.
        /// </summary>
        /// <param name="region">The region code (e.g., "eu-west-1", "sa-east-1").</param>
        /// <returns>The compliance provider for the region.</returns>
        public IComplianceProvider GetProvider(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return this._defaultProvider;
            }

            // Try exact match first
            if (this._providers.TryGetValue(region, out var provider))
            {
                return provider;
            }

            // Try to match by region prefix (e.g., "eu-" matches GDPR, "sa-" or "br-" matches LGPD)
            if (region.StartsWith("eu-", StringComparison.OrdinalIgnoreCase))
            {
                return this._providers["EU"];
            }

            if (region.StartsWith("sa-", StringComparison.OrdinalIgnoreCase) ||
                region.StartsWith("br-", StringComparison.OrdinalIgnoreCase))
            {
                return this._providers["BR"];
            }

            // Fallback to most stringent (GDPR)
            return this._defaultProvider;
        }

        /// <summary>
        /// Gets a compliance provider by regulation code.
        /// </summary>
        /// <param name="regulationCode">The regulation code (e.g., "GDPR", "LGPD").</param>
        /// <returns>The compliance provider for the regulation.</returns>
        public IComplianceProvider GetProviderByRegulation(string regulationCode)
        {
            if (string.IsNullOrWhiteSpace(regulationCode))
            {
                return this._defaultProvider;
            }

            var provider = this._providers.Values.FirstOrDefault(p =>
                p.RegulationCode.Equals(regulationCode, StringComparison.OrdinalIgnoreCase));

            return provider ?? this._defaultProvider;
        }

        /// <summary>
        /// Gets all registered compliance providers.
        /// </summary>
        /// <returns>Collection of all compliance providers.</returns>
        public IEnumerable<IComplianceProvider> GetAllProviders()
        {
            return this._providers.Values.Distinct();
        }

        /// <summary>
        /// Registers a custom compliance provider for a specific region.
        /// </summary>
        /// <param name="region">The region code.</param>
        /// <param name="provider">The compliance provider.</param>
        public void RegisterProvider(string region, IComplianceProvider provider)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentException("Region cannot be null or empty", nameof(region));
            }

            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this._providers[region] = provider;
        }

        /// <summary>
        /// Checks if a provider is registered for a specific region.
        /// </summary>
        /// <param name="region">The region code.</param>
        /// <returns>True if a provider is registered, false otherwise.</returns>
        public bool HasProvider(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return false;
            }

            return this._providers.ContainsKey(region);
        }
    }
}
