// <copyright file="CustomWebApplicationFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.Inference.Api;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Configures the web host for testing.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Configure test-specific services here
            // For example, mock external dependencies, configure authentication, etc.

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });
        });
    }
}
