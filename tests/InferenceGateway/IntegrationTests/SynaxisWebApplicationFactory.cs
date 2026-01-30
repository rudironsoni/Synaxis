using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using DotNetEnv;
using Tests.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
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
            // Apply EF Core migrations so the schema matches migrations rather than EnsureCreated
            await dbContext.Database.MigrateAsync();

            // Build temporary configuration to seed test data. Reuse logic from SmokeTestDataGenerator.
            var builder = new ConfigurationBuilder();

            // Find project root to locate appsettings
            string? projectRoot = null;
            var dir = new DirectoryInfo(AppContext.BaseDirectory ?? Directory.GetCurrentDirectory());
            while (dir != null)
            {
                if (dir.GetFiles("*.sln").Any())
                {
                    projectRoot = dir.FullName;
                    break;
                }

                var src = Path.Combine(dir.FullName, "src");
                if (Directory.Exists(src))
                {
                    projectRoot = dir.FullName;
                    break;
                }

                dir = dir.Parent;
            }

            if (!string.IsNullOrEmpty(projectRoot))
            {
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
            var config = builder.Build();

            // Seed the database
            await TestDatabaseSeeder.SeedAsync(dbContext, config);
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose containers
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set JWT secret BEFORE host creation so it's available when Program.cs reads configuration
        // This must happen before WebApplication.CreateBuilder is called
        Environment.SetEnvironmentVariable("Synaxis__InferenceGateway__JwtSecret",
            "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm");

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // JWT secret is already set in CreateHost above

        builder.UseEnvironment("Development"); // Use Development to load appsettings.Development.json

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddXUnit(this);
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Load .env files if present so environment variables are available for tests
            DotNetEnv.Env.TraversePath().Load();

        var settings = new Dictionary<string, string?>
        {
            ["Synaxis:ControlPlane:ConnectionString"] = _postgres.GetConnectionString(),
            ["Synaxis:ControlPlane:UseInMemory"] = "false",
            ["Synaxis:ControlPlane:RunMigrations"] = "false",
            ["ConnectionStrings:Redis"] = $"{_redis.GetConnectionString()},abortConnect=false"
        };
            // Map a standard list of provider environment variables to configuration keys.
            // Support both modern names like GROQ_API_KEY and legacy SYNAPLEXER_* variants by trying
            // multiple candidate env var names for each provider.
            var providerMappings = new Dictionary<string, string>
            {
                ["GROQ_API_KEY"] = "Synaxis:InferenceGateway:Providers:Groq:Key",
                ["COHERE_API_KEY"] = "Synaxis:InferenceGateway:Providers:Cohere:Key",
                ["CLOUDFLARE_API_KEY"] = "Synaxis:InferenceGateway:Providers:Cloudflare:Key",
                ["CLOUDFLARE_ACCOUNT_ID"] = "Synaxis:InferenceGateway:Providers:Cloudflare:AccountId",
                ["GEMINI_API_KEY"] = "Synaxis:InferenceGateway:Providers:Gemini:Key",
                ["OPENROUTER_API_KEY"] = "Synaxis:InferenceGateway:Providers:OpenRouter:Key",
                ["DEEPSEEK_API_KEY"] = "Synaxis:InferenceGateway:Providers:DeepSeek:Key",
                ["OPENAI_API_KEY"] = "Synaxis:InferenceGateway:Providers:OpenAI:Key",
                ["ANTIGRAVITY_API_KEY"] = "Synaxis:InferenceGateway:Providers:Antigravity:Key",
                ["KILOCODE_API_KEY"] = "Synaxis:InferenceGateway:Providers:KiloCode:Key",
                ["NVIDIA_API_KEY"] = "Synaxis:InferenceGateway:Providers:NVIDIA:Key",
                ["HUGGINGFACE_API_KEY"] = "Synaxis:InferenceGateway:Providers:HuggingFace:Key",
            };

            foreach (var kv in providerMappings)
            {
                var envKey = kv.Key;
                var configKey = kv.Value;

                // Try multiple candidate environment variable names to be robust
                var candidates = new[]
                {
                    envKey,
                    $"SYNAPLEXER_{envKey}",
                    // Some legacy names used _KEY instead of _API_KEY, support those too
                    envKey.Replace("_API_KEY", "_KEY"),
                    $"SYNAPLEXER_{envKey.Replace("_API_KEY", "_KEY")}"
                };

                string? val = null;
                foreach (var candidate in candidates)
                {
                    if (string.IsNullOrEmpty(candidate)) continue;
                    val = Environment.GetEnvironmentVariable(candidate);
                    if (!string.IsNullOrEmpty(val)) break;
                }

                if (!string.IsNullOrEmpty(val)) settings[configKey] = val;
            }

            config.AddInMemoryCollection(settings);
        });
    }
}
