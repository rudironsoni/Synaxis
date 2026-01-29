using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    public static class SmokeTestDataGenerator
    {
        public static IEnumerable<object[]> GenerateChatCompletionCases()
        {
            return GenerateTestCases(Models.EndpointType.ChatCompletions);
        }

        public static IEnumerable<object[]> GenerateLegacyCompletionCases()
        {
            return GenerateTestCases(Models.EndpointType.LegacyCompletions);
        }

        private static IEnumerable<object[]> GenerateTestCases(Models.EndpointType endpoint)
        {
            var configuration = BuildConfiguration();

            // Expect configuration structure like: SmokeTests:Providers:{provider}:Enabled and Models
            var section = configuration.GetSection("SmokeTests:Providers");
            if (!section.Exists()) yield break;

            foreach (var provider in section.GetChildren())
            {
                var enabled = provider.GetValue<bool>("Enabled");
                if (!enabled) continue;

                var providerName = provider.Key;
                var modelsSection = provider.GetSection("Models");
                foreach (var model in modelsSection.GetChildren())
                {
                    var modelName = model.Key;
                    var canonicalId = model.GetValue<string>("CanonicalId") ?? model.GetValue<string>("Id") ?? modelName;
                    var modelEndpoint = model.GetValue<string>("Endpoint");
                    // If model specifies endpoint, skip mismatched
                    if (!string.IsNullOrEmpty(modelEndpoint))
                    {
                        if (!Enum.TryParse<Models.EndpointType>(modelEndpoint, ignoreCase: true, out var parsed))
                        {
                            // ignore unknown
                            continue;
                        }
                        if (parsed != endpoint) continue;
                    }

                    yield return new object[] { new Models.SmokeTestCase(providerName, modelName, canonicalId, endpoint) };
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
                var webApiPath = Path.Combine(projectRoot, "src", "Synaxis.WebApi");
                if (Directory.Exists(webApiPath))
                {
                    var appsettings = Path.Combine(webApiPath, "appsettings.json");
                    var appsettingsDev = Path.Combine(webApiPath, "appsettings.Development.json");
                    if (File.Exists(appsettings)) builder.AddJsonFile(appsettings, optional: true, reloadOnChange: false);
                    if (File.Exists(appsettingsDev)) builder.AddJsonFile(appsettingsDev, optional: true, reloadOnChange: false);
                }
            }

            builder.AddEnvironmentVariables();
            return builder.Build();
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
