// <copyright file="ExternalE2EFactAttribute.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Custom Fact attribute for external end-to-end tests.
/// Automatically skips tests when RUN_EXTERNAL_E2E != 1 or required API keys are missing.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ExternalE2EFactAttribute : FactAttribute
{
    private const string SkipReason = "External E2E tests are disabled. Set RUN_EXTERNAL_E2E=1 and provide required API keys to enable.";

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalE2EFactAttribute"/> class.
    /// Checks environment variables and sets Skip if conditions are not met.
    /// </summary>
    public ExternalE2EFactAttribute()
    {
        // Check if external E2E tests are enabled
        var runExternalE2E = Environment.GetEnvironmentVariable("RUN_EXTERNAL_E2E");
        if (!string.Equals(runExternalE2E, "1", StringComparison.Ordinal))
        {
            this.Skip = SkipReason;
            return;
        }

        // Check if at least one required API key is available
        var hasApiKey = HasRequiredApiKey();
        if (!hasApiKey)
        {
            this.Skip = SkipReason;
        }
    }

    /// <summary>
    /// Checks if at least one required API key is available.
    /// </summary>
    private static bool HasRequiredApiKey()
    {
        // List of supported API key environment variables
        var apiKeyEnvVars = new[]
        {
            "GROQ_API_KEY",
            "SYNAPLEXER_GROQ_API_KEY",
            "COHERE_API_KEY",
            "SYNAPLEXER_COHERE_API_KEY",
            "OPENAI_API_KEY",
            "SYNAPLEXER_OPENAI_API_KEY",
            "GEMINI_API_KEY",
            "SYNAPLEXER_GEMINI_API_KEY",
            "OPENROUTER_API_KEY",
            "SYNAPLEXER_OPENROUTER_API_KEY",
            "DEEPSEEK_API_KEY",
            "SYNAPLEXER_DEEPSEEK_API_KEY",
            "ANTIGRAVITY_API_KEY",
            "SYNAPLEXER_ANTIGRAVITY_API_KEY",
            "KILOCODE_API_KEY",
            "SYNAPLEXER_KILOCODE_API_KEY",
            "NVIDIA_API_KEY",
            "SYNAPLEXER_NVIDIA_API_KEY",
            "HUGGINGFACE_API_KEY",
            "SYNAPLEXER_HUGGINGFACE_API_KEY",
        };

        foreach (var envVar in apiKeyEnvVars)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrEmpty(value))
            {
                return true;
            }
        }

        return false;
    }
}
