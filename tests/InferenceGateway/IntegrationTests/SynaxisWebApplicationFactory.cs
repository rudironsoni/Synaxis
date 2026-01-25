using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using MartinCostello.Logging.XUnit;
using System.Collections.Generic;

namespace Synaxis.InferenceGateway.IntegrationTests;

public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, ITestOutputHelperAccessor
{
    public ITestOutputHelper? OutputHelper { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddXUnit(this);
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override keys from environment variables if present
            var overrides = new Dictionary<string, string?>();

            var groqKey = Environment.GetEnvironmentVariable("SYNAPLEXER_GROQ_KEY");
            if (!string.IsNullOrEmpty(groqKey)) overrides["Synaxis:InferenceGateway:Providers:Groq:Key"] = groqKey;

            var cohereKey = Environment.GetEnvironmentVariable("SYNAPLEXER_COHERE_KEY");
            if (!string.IsNullOrEmpty(cohereKey)) overrides["Synaxis:InferenceGateway:Providers:Cohere:Key"] = cohereKey;

            var cfKey = Environment.GetEnvironmentVariable("SYNAPLEXER_CLOUDFLARE_KEY");
            if (!string.IsNullOrEmpty(cfKey)) overrides["Synaxis:InferenceGateway:Providers:Cloudflare:Key"] = cfKey;

            var cfAccount = Environment.GetEnvironmentVariable("SYNAPLEXER_CLOUDFLARE_ACCOUNT_ID");
            if (!string.IsNullOrEmpty(cfAccount)) overrides["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"] = cfAccount;

            config.AddInMemoryCollection(overrides);
            config.AddEnvironmentVariables();
        });
    }
}
