// <copyright file="RoutingContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    /// <summary>
    /// Context information for request routing.
    /// </summary>
    /// <param name="requestedModel">The requested model name.</param>
    /// <param name="resolvedCanonicalId">The resolved canonical model ID.</param>
    /// <param name="provider">The provider that will handle the request.</param>
    public record RoutingContext(string requestedModel, string resolvedCanonicalId, string provider);
}
