// <copyright file="SynaxisWebApplicationFactory.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetEnv;
using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Tests.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime, ITestOutputHelperAccessor
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithCommand("-c", "max_connections=200")
            .Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
            .Build();

        public ITestOutputHelper? OutputHelper { get; set; }

        public async Task InitializeAsync()
        {
            // Start containers in parallel
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync()).ConfigureAwait(false);

            // Initialize the ControlPlane database schema
            var controlPlaneOptionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
            var connectionString = $"{this._postgres.GetConnectionString()};Pooling=true;Maximum Pool Size=200";
            controlPlaneOptionsBuilder.UseNpgsql(connectionString);

            using var controlPlaneContext = new ControlPlaneDbContext(controlPlaneOptionsBuilder.Options);

            // Apply EF Core migrations for ControlPlaneDbContext (infrastructure tables: identity.Users, etc.)
            await controlPlaneContext.Database.MigrateAsync().ConfigureAwait(false);

            // Apply EF Core migrations for SynaxisDbContext (multi-tenant tables: public.users, etc.)
            // Both contexts target the same database but create DIFFERENT tables
            var synaxisOptionsBuilder = new DbContextOptionsBuilder<Synaxis.Infrastructure.Data.SynaxisDbContext>();
            synaxisOptionsBuilder.UseNpgsql(connectionString);
            using var synaxisContext = new Synaxis.Infrastructure.Data.SynaxisDbContext(synaxisOptionsBuilder.Options);
            await synaxisContext.Database.MigrateAsync().ConfigureAwait(false);

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
                var config = builder.Build();

            // Seed the database
            await TestDatabaseSeeder.SeedAsync(controlPlaneContext, config).ConfigureAwait(false);
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            // Dispose containers
            return Task.WhenAll(this._postgres.DisposeAsync().AsTask(), this._redis.DisposeAsync().AsTask());
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Set environment variables BEFORE host creation so they're available when Program.cs reads configuration
            // This must happen before WebApplication.CreateBuilder is called
            Environment.SetEnvironmentVariable(
                "Synaxis__InferenceGateway__JwtSecret",
                "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm");

            // Disable Quartz scheduler for tests to prevent background jobs from interfering
            Environment.SetEnvironmentVariable(
                "Synaxis__InferenceGateway__EnableQuartz",
                "false");

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

                var connectionString = $"{this._postgres.GetConnectionString()};Pooling=true;Maximum Pool Size=200";
                var settings = new Dictionary<string, string?>
(StringComparer.Ordinal)
                {
                    ["Synaxis:ControlPlane:ConnectionString"] = connectionString,
                    ["Synaxis:ControlPlane:UseInMemory"] = "false",
                    ["Synaxis:ControlPlane:RunMigrations"] = "false",
                    ["ConnectionStrings:Redis"] = $"{this._redis.GetConnectionString()},abortConnect=false",
                    ["Synaxis:InferenceGateway:EnableQuartz"] = "false",
                };

                // Map a standard list of provider environment variables to configuration keys.
                // Support both modern names like GROQ_API_KEY and legacy SYNAPLEXER_* variants by trying
                // multiple candidate env var names for each provider.
                var providerMappings = new Dictionary<string, string>
(StringComparer.Ordinal)
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
                    $"SYNAPLEXER_{envKey.Replace("_API_KEY", "_KEY")}",
                    };

                    string? val = null;
                    foreach (var candidate in candidates)
                    {
                        if (string.IsNullOrEmpty(candidate))
                        {
                            continue;
                        }

                        val = Environment.GetEnvironmentVariable(candidate);
                        if (!string.IsNullOrEmpty(val))
                        {
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(val))
                    {
                        settings[configKey] = val;
                    }
                }

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                // Get connection string with pooling
                var connectionString = $"{this._postgres.GetConnectionString()};Pooling=true;Maximum Pool Size=200";

                // Remove the in-memory DbContext registrations from AddControlPlane
                var controlPlaneDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ControlPlaneDbContext>));
                if (controlPlaneDescriptor != null)
                {
                    services.Remove(controlPlaneDescriptor);
                }

                var synaxisDbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<Synaxis.Infrastructure.Data.SynaxisDbContext>));
                if (synaxisDbDescriptor != null)
                {
                    services.Remove(synaxisDbDescriptor);
                }

                // Re-register ControlPlaneDbContext with PostgreSQL (this is used by tests to query data)
                services.AddDbContext<ControlPlaneDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                });

                // Re-register SynaxisDbContext (used by Identity) with PostgreSQL
                services.AddDbContext<Synaxis.Infrastructure.Data.SynaxisDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                });
            });
        }
    }

    /// <summary>
    /// Collection fixture for integration tests to share the same SynaxisWebApplicationFactory instance.
    /// This ensures all tests in the collection use the same PostgreSQL container, reducing connection overhead.
    /// </summary>
    [CollectionDefinition("Integration")]
    public class IntegrationTestCollection : ICollectionFixture<SynaxisWebApplicationFactory>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
