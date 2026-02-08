// <copyright file="ResolutionResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Collections.Generic;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Represents the result of model resolution with candidates.
    /// </summary>
    /// <param name="originalModelId">The original model identifier.</param>
    /// <param name="canonicalId">The canonical model identifier.</param>
    /// <param name="candidates">The list of provider candidates.</param>
    public record ResolutionResult(string originalModelId, CanonicalModelId canonicalId, IList<ProviderConfig> candidates);
}
