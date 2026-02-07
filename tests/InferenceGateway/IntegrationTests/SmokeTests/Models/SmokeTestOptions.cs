using System;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Models
{
    public class ProviderSmokeTestOptions
    {
        public int? TimeoutMs { get; set; }

        public int? MaxRetries { get; set; }
    }

    public class SmokeTestOptions
    {
        public int DefaultTimeoutMs { get; set; } = 30000;

        public int MaxRetries { get; set; } = 3;

        public int InitialRetryDelayMs { get; set; } = 1000;

        public double RetryBackoffMultiplier { get; set; } = 2.0;

        public Dictionary<string, ProviderSmokeTestOptions> ProviderOverrides { get; set; } = new Dictionary<string, ProviderSmokeTestOptions>(StringComparer.OrdinalIgnoreCase);
    }
}
