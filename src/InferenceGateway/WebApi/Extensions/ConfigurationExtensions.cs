// <copyright file="ConfigurationExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Extensions;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension methods for configuring application environment variables.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Maps environment variables to configuration keys for provider credentials.
    /// This allows Docker/environment-based configuration without code changes.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <returns>The configuration builder for chaining.</returns>
    public static IConfigurationBuilder AddInferenceGatewayEnvironmentVariables(
        this IConfigurationBuilder builder)
    {
        var envMapping = new Dictionary<string, string?>
        {
            { "Synaxis:InferenceGateway:Providers:Groq:Key", Environment.GetEnvironmentVariable("GROQ_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:Cohere:Key", Environment.GetEnvironmentVariable("COHERE_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:Cloudflare:Key", Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:Cloudflare:AccountId", Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID") },
            { "Synaxis:InferenceGateway:Providers:Gemini:Key", Environment.GetEnvironmentVariable("GEMINI_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:OpenRouter:Key", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:DeepSeek:Key", Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:DeepSeek:Endpoint", Environment.GetEnvironmentVariable("DEEPSEEK_API_ENDPOINT") },
            { "Synaxis:InferenceGateway:Providers:OpenAI:Key", Environment.GetEnvironmentVariable("OPENAI_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:OpenAI:Endpoint", Environment.GetEnvironmentVariable("OPENAI_API_ENDPOINT") },
            { "Synaxis:InferenceGateway:Providers:Antigravity:ProjectId", Environment.GetEnvironmentVariable("ANTIGRAVITY_PROJECT_ID") },
            { "Synaxis:InferenceGateway:Providers:Antigravity:Endpoint", Environment.GetEnvironmentVariable("ANTIGRAVITY_API_ENDPOINT") },
            { "Synaxis:InferenceGateway:Providers:Antigravity:FallbackEndpoint", Environment.GetEnvironmentVariable("ANTIGRAVITY_API_ENDPOINT_FALLBACK") },
            { "Synaxis:InferenceGateway:Providers:KiloCode:Key", Environment.GetEnvironmentVariable("KILOCODE_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:NVIDIA:Key", Environment.GetEnvironmentVariable("NVIDIA_API_KEY") },
            { "Synaxis:InferenceGateway:Providers:HuggingFace:Key", Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") },
        };

        var filteredMapping = envMapping
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return builder.AddInMemoryCollection(filteredMapping!);
    }
}
