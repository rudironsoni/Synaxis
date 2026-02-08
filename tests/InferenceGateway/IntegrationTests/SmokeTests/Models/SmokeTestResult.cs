// <copyright file="SmokeTestResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models
{
    public record SmokeTestResult(
        SmokeTestCase @case,
        bool success,
        TimeSpan responseTime,
        string? error = null,
        string? responseSnippet = null,
        int attemptCount = 1);
}
