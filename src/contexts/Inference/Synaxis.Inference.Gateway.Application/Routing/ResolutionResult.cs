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
    /// <param name="OriginalModelId">The original model identifier.</param>
    /// <param name="CanonicalId">The canonical model identifier.</param>
    /// <param name="Candidates">The list of provider candidates.</param>
    public record ResolutionResult(string OriginalModelId, CanonicalModelId CanonicalId, IList<ProviderConfig> Candidates);
}
