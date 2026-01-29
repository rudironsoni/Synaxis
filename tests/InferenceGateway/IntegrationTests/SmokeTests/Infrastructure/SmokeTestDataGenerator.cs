using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using DotNetEnv;
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
            if (!providersSection.Exists()) yield break;

            foreach (var providerSection in providersSection.GetChildren())
            {
                if (!providerSection.GetValue<bool>("Enabled")) continue;

                var providerName = providerSection.Key;
                var modelsSection = providerSection.GetSection("Models");
                foreach (var modelItem in modelsSection.GetChildren())
                {
                    var modelName = modelItem.Value;
                    if (string.IsNullOrEmpty(modelName)) continue;

                    var canonicalId = FindCanonicalId(configuration, providerName, modelName) ?? modelName;

                    yield return new object[] { providerName, modelName, canonicalId, endpoint };
                }
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
                    if (File.Exists(appsettings)) builder.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
                    if (File.Exists(appsettingsDev)) builder.AddJsonFile(appsettingsDev, optional: true, reloadOnChange: false);
                }
            }

            // Load .env files (if present) so that AddEnvironmentVariables picks them up
            Env.TraversePath().Load();

            builder.AddEnvironmentVariables();
            return builder.Build();
        }

        private static string? FindCanonicalId(IConfigurationRoot config, string provider, string modelPath)
        {
            var canonicals = config.GetSection("Synaxis:InferenceGateway:CanonicalModels");
            if (!canonicals.Exists()) return null;

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
                if (dir.GetFiles("*.sln").Any()) return dir.FullName;
                var src = Path.Combine(dir.FullName, "src");
                if (Directory.Exists(src)) return dir.FullName;
                dir = dir.Parent;
            }

            return null;
        }
    }
}
