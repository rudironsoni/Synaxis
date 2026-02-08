// <copyright file="SmokeTestResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models
{
    public record SmokeTestResult(
        SmokeTestCase Case,
        bool Success,
        TimeSpan ResponseTime,
        string? Error = null,
        string? ResponseSnippet = null,
        int AttemptCount = 1);
}
