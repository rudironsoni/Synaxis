// <copyright file="SmokeTestCase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models
{
    public record SmokeTestCase(
        string Provider,
        string Model,
        string CanonicalId,
        EndpointType Endpoint,
        int TimeoutMs = 30000,
        int MaxRetries = 3);
}
