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
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.Common.Tests.Fixtures;
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
using Tests.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace Synaxis.InferenceGateway.IntegrationTests;

/// <summary>
/// Lightweight WebApplicationFactory for integration tests.
/// Owns PostgresFixture and RedisFixture as instance fields to avoid per-test container churn.
/// Constructor is lightweight; startup/migrations/seeding happen in async lifecycle.
/// </summary>
public class SynaxisWebApplicationFactory : WebApplicationFactory<Program>, ITestOutputHelperAccessor, IAsyncLifetime
{
    private readonly PostgresFixture _postgresFixture;
    private readonly RedisFixture _redisFixture;
    private bool _initialized = false;

    public ITestOutputHelper? OutputHelper { get; set; }

    /// <summary>
    /// Gets the PostgreSQL connection string.
    /// </summary>
    public string PostgresConnectionString => _postgresFixture.ConnectionString;

    /// <summary>
    /// Gets the Redis connection string.
    /// </summary>
    public string RedisConnectionString => _redisFixture.ConnectionString;

    public SynaxisWebApplicationFactory()
    {
        // Create fixture instances - lightweight constructor
        _postgresFixture = new PostgresFixture();
        _redisFixture = new RedisFixture();
    }

    /// <summary>
    /// Initializes the factory asynchronously.
    /// Starts fixtures, applies migrations, and seeds test data.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        // Start fixtures once per factory instance
        await _postgresFixture.InitializeAsync();
        await _redisFixture.InitializeAsync();

        // Initialize the ControlPlane database schema
        var controlPlaneOptionsBuilder = new DbContextOptionsBuilder<ControlPlaneDbContext>();
        var connectionString = $"{_postgresFixture.ConnectionString};Pooling=true;Maximum Pool Size=200";
        controlPlaneOptionsBuilder.UseNpgsql(connectionString);
        controlPlaneOptionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        using var controlPlaneContext = new ControlPlaneDbContext(controlPlaneOptionsBuilder.Options);

        // Apply EF Core migrations for ControlPlaneDbContext (infrastructure tables: identity.Users, etc.)
        await controlPlaneContext.Database.MigrateAsync();

        // Apply EF Core migrations for SynaxisDbContext (multi-tenant tables: public.users, etc.)
        // Both contexts target the same database but create DIFFERENT tables
        var synaxisOptionsBuilder = new DbContextOptionsBuilder<Synaxis.Infrastructure.Data.SynaxisDbContext>();
        synaxisOptionsBuilder.UseNpgsql(connectionString);
        synaxisOptionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        using var synaxisContext = new Synaxis.Infrastructure.Data.SynaxisDbContext(synaxisOptionsBuilder.Options);
        await synaxisContext.Database.MigrateAsync();

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
        await TestDatabaseSeeder.SeedAsync(controlPlaneContext, config);

        _initialized = true;
    }

    /// <summary>
    /// Creates test-specific model data after initial seeding.
    /// Ensures "test-alias" and "test-model" models exist for gRPC and WebSocket transport tests.
    /// </summary>
    private async Task PostSeedTestDataAsync(ControlPlaneDbContext context)
    {
        // Seed test-alias for gRPC tests
        var existingAlias = await context.GlobalModels.FindAsync("test-alias");
        if (existingAlias == null)
        {
            // Create GlobalModel for test-alias
            var testAlias = new Synaxis.InferenceGateway.Application.ControlPlane.Entities.GlobalModel
            {
                Id = "test-alias",
                Name = "Test Model",
                Family = "test",
                Description = "Test model alias for integration tests"
            };
            context.GlobalModels.Add(testAlias);

            // Create ProviderModel linking test-alias to Pollinations provider (free, no API key required)
            var aliasProviderModel = new Synaxis.InferenceGateway.Application.ControlPlane.Entities.ProviderModel
            {
                ProviderId = "Pollinations",
                GlobalModelId = "test-alias",
                ProviderSpecificId = "test-alias",
                IsAvailable = true
            };
            context.ProviderModels.Add(aliasProviderModel);
        }

        // Seed test-model for WebSocket tests
        var existingTestModel = await context.GlobalModels.FindAsync("test-model");
        if (existingTestModel == null)
        {
            // Create GlobalModel for test-model
            var testModel = new Synaxis.InferenceGateway.Application.ControlPlane.Entities.GlobalModel
            {
                Id = "test-model",
                Name = "Test Model",
                Family = "test",
                Description = "Test model for WebSocket integration tests"
            };
            context.GlobalModels.Add(testModel);

            // Create ProviderModel linking test-model to Pollinations provider (free, no API key required)
            var modelProviderModel = new Synaxis.InferenceGateway.Application.ControlPlane.Entities.ProviderModel
            {
                ProviderId = "Pollinations",
                GlobalModelId = "test-model",
                ProviderSpecificId = "test-model",
                IsAvailable = true
            };
            context.ProviderModels.Add(modelProviderModel);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Disposes the factory and owned fixtures asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public new async Task DisposeAsync()
    {
        // Dispose owned fixtures
        await _postgresFixture.DisposeAsync();
        await _redisFixture.DisposeAsync();
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

            var connectionString = $"{_postgresFixture.ConnectionString};Pooling=true;Maximum Pool Size=200";
            var settings = new Dictionary<string, string?>
(StringComparer.Ordinal)
            {
                ["Synaxis:ControlPlane:ConnectionString"] = connectionString,
                ["Synaxis:ControlPlane:UseInMemory"] = "false",
                ["Synaxis:ControlPlane:RunMigrations"] = "false",
                ["ConnectionStrings:Redis"] = $"{_redisFixture.ConnectionString},abortConnect=false",
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
            var connectionString = $"{_postgresFixture.ConnectionString};Pooling=true;Maximum Pool Size=200";

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

            // Register CORS policy
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:8080", "http://localhost:5000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Register transport services for integration testing
            services.AddSynaxisTransportHttp();
            services.AddSynaxisTransportGrpc();
            services.AddSynaxisTransportWebSocket();

            // Register mock chat client for Pollinations provider to avoid external HTTP calls
            services.AddKeyedSingleton<IChatClient>("Pollinations", new MockChatClient());

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
                app.UseCors();
                app.UseAuthentication();
                app.UseAuthorization();

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

    /// <summary>
    /// Mock chat client for integration tests.
    /// Returns deterministic responses without making external HTTP calls.
    /// </summary>
    private class MockChatClient : IChatClient
    {
        public ChatClientMetadata Metadata => new("mock", new Uri("http://mock.local"));

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from mock!"))
            {
                Usage = new UsageDetails { InputTokenCount = 10, OutputTokenCount = 5 },
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent("Hello") } };
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent(" from") } };
            yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = { new TextContent(" mock!") } };
            yield return new ChatResponseUpdate { FinishReason = ChatFinishReason.Stop };
        }

        public void Dispose()
        {
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
