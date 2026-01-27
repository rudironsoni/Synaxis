using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime, ITestOutputHelperAccessor
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    public ITestOutputHelper? OutputHelper { get; set; }

    public async Task InitializeAsync()
    {
        // Start containers in parallel
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        // Initialize the database schema
        // We do this here to ensure the DB is ready before the application starts
        var optionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
        optionsBuilder.UseNpgsql(_postgres.GetConnectionString());

        using var dbContext = new ControlPlaneDbContext(optionsBuilder.Options);

        // Use execution strategy to handle potential transient failures during startup
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.EnsureCreatedAsync();
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose containers
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddXUnit(this);
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Synaxis:ControlPlane:ConnectionString"] = _postgres.GetConnectionString(),
                ["Synaxis:ControlPlane:UseInMemory"] = "false",
                ["ConnectionStrings:Redis"] = $"{_redis.GetConnectionString()},abortConnect=false"
            };

            // Pass through environment variables for secrets if present
            var groqKey = Environment.GetEnvironmentVariable("SYNAPLEXER_GROQ_KEY");
            if (!string.IsNullOrEmpty(groqKey)) settings["Synaxis:InferenceGateway:Providers:Groq:Key"] = groqKey;

            var cohereKey = Environment.GetEnvironmentVariable("SYNAPLEXER_COHERE_KEY");
            if (!string.IsNullOrEmpty(cohereKey)) settings["Synaxis:InferenceGateway:Providers:Cohere:Key"] = cohereKey;

            var cfKey = Environment.GetEnvironmentVariable("SYNAPLEXER_CLOUDFLARE_KEY");
            if (!string.IsNullOrEmpty(cfKey)) settings["Synaxis:InferenceGateway:Providers:Cloudflare:Key"] = cfKey;

            var cfAccount = Environment.GetEnvironmentVariable("SYNAPLEXER_CLOUDFLARE_ACCOUNT_ID");
            if (!string.IsNullOrEmpty(cfAccount)) settings["Synaxis:InferenceGateway:Providers:Cloudflare:AccountId"] = cfAccount;

            config.AddInMemoryCollection(settings);
        });
    }
}
