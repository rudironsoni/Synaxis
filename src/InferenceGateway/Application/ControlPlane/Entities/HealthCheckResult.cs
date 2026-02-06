// <copyright file="HealthCheckResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Results from provider health check validation.
    /// </summary>
    /// <param name="isHealthy">Whether the provider is healthy.</param>
    /// <param name="endpoint">The provider endpoint.</param>
    /// <param name="supportsStreaming">Whether the provider supports streaming.</param>
    /// <param name="supportsChat">Whether the provider supports chat.</param>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    /// <param name="supportedModels">The list of supported models.</param>
    /// <param name="errors">The list of errors if any.</param>
    public record HealthCheckResult(
        bool isHealthy,
        string? endpoint,
        bool supportsStreaming,
        bool supportsChat,
        int? latencyMs,
        string[] supportedModels,
        string[] errors);
}
