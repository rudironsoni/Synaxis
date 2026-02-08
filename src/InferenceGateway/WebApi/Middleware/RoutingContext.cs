// <copyright file="RoutingContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    /// <summary>
    /// Context information for request routing.
    /// </summary>
    /// <param name="RequestedModel">The requested model name.</param>
    /// <param name="ResolvedCanonicalId">The resolved canonical model ID.</param>
    /// <param name="Provider">The provider that will handle the request.</param>
    public record RoutingContext(string RequestedModel, string ResolvedCanonicalId, string Provider);
}
