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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.DependencyInjection;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.Infrastructure.Data;
using Synaxis.Transport.Grpc;
using Synaxis.Transport.Grpc.DependencyInjection;
using Synaxis.Transport.Http;
using Synaxis.Transport.Http.DependencyInjection;
using Synaxis.Transport.WebSocket;
using Synaxis.Transport.WebSocket.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Tests.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests
{
    public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, ITestOutputHelperAccessor
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithCommand("-c", "max_connections=200")
            .Build();

        private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
            .Build();

        public ITestOutputHelper? OutputHelper { get; set; }

        public SynaxisWebApplicationFactory()
        {
            // Set base address for HTTP client testing
            this.ClientOptions.BaseAddress = new Uri("http://localhost:5001");

            // Start containers in parallel
            Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync()).GetAwaiter().GetResult();

            // Initialize the ControlPlane database schema
            var controlPlaneOptionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
            var connectionString = $"{this._postgres.GetConnectionString()};Pooling=true;Maximum Pool Size=200";
            controlPlaneOptionsBuilder.UseNpgsql(connectionString);
            controlPlaneOptionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

            using var controlPlaneContext = new ControlPlaneDbContext(controlPlaneOptionsBuilder.Options);

            // Apply EF Core migrations for ControlPlaneDbContext (infrastructure tables: identity.Users, etc.)
            controlPlaneContext.Database.MigrateAsync().GetAwaiter().GetResult();

            // Apply EF Core migrations for SynaxisDbContext (multi-tenant tables: public.users, etc.)
            // Both contexts target the same database but create DIFFERENT tables
            var synaxisOptionsBuilder = new DbContextOptionsBuilder<Synaxis.Infrastructure.Data.SynaxisDbContext>();
            synaxisOptionsBuilder.UseNpgsql(connectionString);
            synaxisOptionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            using var synaxisContext = new Synaxis.Infrastructure.Data.SynaxisDbContext(synaxisOptionsBuilder.Options);
            synaxisContext.Database.MigrateAsync().GetAwaiter().GetResult();

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
            TestDatabaseSeeder.SeedAsync(controlPlaneContext, config).GetAwaiter().GetResult();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose containers synchronously
                _postgres.DisposeAsync().GetAwaiter().GetResult();
                _redis.DisposeAsync().GetAwaiter().GetResult();
            }
            base.Dispose(disposing);
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
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                // Re-register SynaxisDbContext (used by Identity) with PostgreSQL
                services.AddDbContext<Synaxis.Infrastructure.Data.SynaxisDbContext>(options =>
                {
                    options.UseNpgsql(connectionString);
                    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                // Register core Synaxis services (includes Mediator, handlers, ProviderSelector)
                services.AddSynaxis();

                // Register transport services for integration testing
                services.AddSynaxisTransportHttp();
                services.AddSynaxisTransportGrpc();
                services.AddSynaxisTransportWebSocket();

                // Register startup filter to configure middleware pipeline
                services.AddSingleton<IStartupFilter, TransportStartupFilter>();
            });
        }

        /// <summary>
        /// Startup filter to configure transport middleware pipeline.
        /// </summary>
        private class TransportStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.UseRouting();

                    app.UseSynaxisTransportHttp();
                    app.UseSynaxisTransportWebSocket();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();
                        endpoints.MapSynaxisTransportGrpc();
                    });

                    next(app);
                };
            }
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
