// <copyright file="SmokeTestDataGenerator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    public static class SmokeTestDataGenerator
    {
        public static IEnumerable<object[]> GenerateChatCompletionCases()
            => GenerateTestCases(EndpointType.ChatCompletions);

        public static IEnumerable<object[]> GenerateLegacyCompletionCases()
            => GenerateTestCases(EndpointType.LegacyCompletions);

        private static IEnumerable<object[]> GenerateTestCases(EndpointType endpoint)
        {
            var configuration = BuildConfiguration();

            var providersSection = configuration.GetSection("Synaxis:InferenceGateway:Providers");
            if (!providersSection.Exists())
            {
                // Return mock test data if no real providers configured
                yield return new object[] { "MockProvider", "mock-model", "mock-model", endpoint };
                yield break;
            }

            var hasData = false;
            foreach (var providerSection in providersSection.GetChildren())
            {
                if (!providerSection.GetValue<bool>("Enabled"))
                {
                    continue;
                }

                var providerName = providerSection.Key;

                // Skip providers with placeholder API keys
                var apiKey = providerSection.GetValue<string>("Key");
                if (string.IsNullOrEmpty(apiKey) ||
                    apiKey.Contains("REPLACE_WITH", StringComparison.OrdinalIgnoreCase) ||
                    apiKey.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    apiKey.Contains("CHANGE", StringComparison.OrdinalIgnoreCase) ||
string.Equals(apiKey, "0000000000", StringComparison.Ordinal))
                {
                    continue;
                }

                var modelsSection = providerSection.GetSection("Models");
                foreach (var modelItem in modelsSection.GetChildren())
                {
                    var modelName = modelItem.Value;
                    if (string.IsNullOrEmpty(modelName))
                    {
                        continue;
                    }

                    var canonicalId = FindCanonicalId(configuration, providerName, modelName) ?? modelName;

                    hasData = true;
                    yield return new object[] { providerName, modelName, canonicalId, endpoint };
                }
            }

            // If no valid providers found, return mock data to prevent "No data found" errors
            if (!hasData)
            {
                yield return new object[] { "MockProvider", "mock-model", "mock-model", endpoint };
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder();

            // Find project root to locate appsettings
            var projectRoot = FindProjectRoot();
            if (!string.IsNullOrEmpty(projectRoot))
            {
                // Correct path: src/InferenceGateway/WebApi
                var webApiPath = Path.Combine(projectRoot, "src", "InferenceGateway", "WebApi");
                if (Directory.Exists(webApiPath))
                {
                    var appsettings = Path.Combine(webApiPath, "appsettings.json");
                    var appsettingsDev = Path.Combine(webApiPath, "appsettings.Development.json");
                    if (File.Exists(appsettings))
                    {
                        builder.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
                    }

                    if (File.Exists(appsettingsDev))
                    {
                        builder.AddJsonFile(appsettingsDev, optional: true, reloadOnChange: false);
                    }
                }
            }

            // Load .env files (if present) so that AddEnvironmentVariables picks them up
            Env.TraversePath().Load();

            builder.AddEnvironmentVariables();

            // Map environment variables to configuration keys (same as WebApi Program.cs)
            var envMapping = new Dictionary<string, string?>
(StringComparer.Ordinal)
            {
                { "Synaxis:InferenceGateway:Providers:Groq:Key", Environment.GetEnvironmentVariable("GROQ_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:Cohere:Key", Environment.GetEnvironmentVariable("COHERE_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:Cloudflare:Key", Environment.GetEnvironmentVariable("CLOUDFLARE_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:Cloudflare:AccountId", Environment.GetEnvironmentVariable("CLOUDFLARE_ACCOUNT_ID") },
                { "Synaxis:InferenceGateway:Providers:Gemini:Key", Environment.GetEnvironmentVariable("GEMINI_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:OpenRouter:Key", Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:NVIDIA:Key", Environment.GetEnvironmentVariable("NVIDIA_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:HuggingFace:Key", Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") },
                { "Synaxis:InferenceGateway:Providers:KiloCode:Key", Environment.GetEnvironmentVariable("KILOCODE_API_KEY") },
            };

            // Filter out null or empty values so we don't overwrite other config values with nulls
            var filteredMapping = envMapping.Where(kv => !string.IsNullOrEmpty(kv.Value))
                                        .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

            builder.AddInMemoryCollection(filteredMapping);

            return builder.Build();
        }

        private static string? FindCanonicalId(IConfigurationRoot config, string provider, string modelPath)
        {
            var canonicals = config.GetSection("Synaxis:InferenceGateway:CanonicalModels");
            if (!canonicals.Exists())
            {
                return null;
            }

            foreach (var item in canonicals.GetChildren())
            {
                var itemProvider = item.GetValue<string>("Provider");
                var itemModelPath = item.GetValue<string>("ModelPath");
                if (string.Equals(itemProvider, provider, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(itemModelPath, modelPath, StringComparison.OrdinalIgnoreCase))
                {
                    return item.GetValue<string>("Id");
                }
            }

            return null;
        }

        private static string? FindProjectRoot()
        {
            // Walk up from base directory until we find a .sln file or a src folder
            var dir = new DirectoryInfo(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Any())
                {
                    return dir.FullName;
                }

                var src = Path.Combine(dir.FullName, "src");
                if (Directory.Exists(src))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            return null;
        }
    }
}
