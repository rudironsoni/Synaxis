// <copyright file="SmokeTestCase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models
{
    public record SmokeTestCase(
        string provider,
        string model,
        string canonicalId,
        EndpointType endpoint,
        int timeoutMs = 30000,
        int maxRetries = 3);
}
