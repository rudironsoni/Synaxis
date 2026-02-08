// <copyright file="HealthCheckResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Results from provider health check validation.
    /// </summary>
    /// <param name="IsHealthy">Whether the provider is healthy.</param>
    /// <param name="Endpoint">The provider endpoint.</param>
    /// <param name="SupportsStreaming">Whether the provider supports streaming.</param>
    /// <param name="SupportsChat">Whether the provider supports chat.</param>
    /// <param name="LatencyMs">The latency in milliseconds.</param>
    /// <param name="SupportedModels">The list of supported models.</param>
    /// <param name="Errors">The list of errors if any.</param>
    public record HealthCheckResult(
        bool IsHealthy,
        string? Endpoint,
        bool SupportsStreaming,
        bool SupportsChat,
        int? LatencyMs,
        string[] SupportedModels,
        string[] Errors);
}
