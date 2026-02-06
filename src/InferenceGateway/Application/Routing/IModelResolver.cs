// <copyright file="IModelResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Resolves model identifiers to provider mappings.
    /// </summary>
    public interface IModelResolver
    {
        /// <summary>
        /// Resolves a model identifier synchronously (deprecated).
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="required">Required capabilities.</param>
        /// <returns>The resolution result.</returns>
        ResolutionResult Resolve(string modelId, RequiredCapabilities? required = null);

        /// <summary>
        /// Resolves a model identifier asynchronously.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="kind">The endpoint kind.</param>
        /// <param name="required">Required capabilities.</param>
        /// <param name="tenantId">Optional tenant ID.</param>
        /// <returns>The resolution result.</returns>
        Task<ResolutionResult> ResolveAsync(string modelId, EndpointKind kind, RequiredCapabilities? required = null, Guid? tenantId = null);
    }
}
