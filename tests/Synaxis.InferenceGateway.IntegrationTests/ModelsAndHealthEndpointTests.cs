// <copyright file="ModelsAndHealthEndpointTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

[Collection("Integration")]
public class ModelsAndHealthEndpointTests
{
    private readonly SynaxisWebApplicationFactory _factory;

    public ModelsAndHealthEndpointTests(SynaxisWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(Dictionary<string, string?> settings, bool suppressHealthLogs = false)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            if (suppressHealthLogs)
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.AddFilter("Microsoft.Extensions.Diagnostics.HealthChecks", LogLevel.None);
                });
            }

            builder.ConfigureAppConfiguration((_, config) =>
            {
                var defaults = new Dictionary<string, string?>
(StringComparer.Ordinal)
                {
                    ["Synaxis:ControlPlane:UseInMemory"] = "true",
                    ["Synaxis:ControlPlane:ConnectionString"] = string.Empty,
                    ["Synaxis:InferenceGateway:JwtSecret"] = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm",
                };

                foreach (var kvp in defaults)
                {
                    if (!settings.ContainsKey(kvp.Key))
                    {
                        settings[kvp.Key] = kvp.Value;
                    }
                }

                config.AddInMemoryCollection(settings);
            });
        });
    }

    [Fact]
    public async Task Models_ReturnsDefaultCanonicalAndAliases()
    {
        var settings = new Dictionary<string, string?>
(StringComparer.Ordinal)
        {
            ["Synaxis:ControlPlane:UseInMemory"] = "true",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Type"] = "openai",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Enabled"] = "true",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Models:0"] = "test-model",
            ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "test-model",
            ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "TestProvider",
            ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "test-model",
            ["Synaxis:InferenceGateway:Aliases:fast:Candidates:0"] = "test-model",
            ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-model",
            ["Synaxis:InferenceGateway:JwtSecret"] = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm",
        };

        var factory = CreateFactory(settings);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openai/v1/models");

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ModelListResponse>();

        Assert.NotNull(payload);
        var ids = payload!.Data.Select(m => m.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("default", ids);
        Assert.Contains("test-model", ids);
        Assert.Contains("fast", ids);
    }

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Synaxis:InferenceGateway:JwtSecret"] = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm",
        };
        var factory = CreateFactory(settings);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/liveness");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_ReturnsUnhealthyWhenRedisUnavailable()
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["Synaxis:ControlPlane:UseInMemory"] = "true",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Enabled"] = "false",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Type"] = "openai",
            ["Synaxis:InferenceGateway:Providers:TestProvider:Models:0"] = "test-model",
            ["Synaxis:InferenceGateway:CanonicalModels:0:Id"] = "test-model",
            ["Synaxis:InferenceGateway:CanonicalModels:0:Provider"] = "TestProvider",
            ["Synaxis:InferenceGateway:CanonicalModels:0:ModelPath"] = "test-model",
            ["Synaxis:InferenceGateway:Aliases:default:Candidates:0"] = "test-model",
            ["Synaxis:ControlPlane:ConnectionString"] = string.Empty,
            ["ConnectionStrings:Redis"] = "localhost:6379,abortConnect=false,connectTimeout=100,asyncTimeout=100",
            ["Synaxis:InferenceGateway:JwtSecret"] = "TestJwtSecretKeyThatIsAtLeast32BytesLongForHmacSha256Algorithm",
        };

        var factory = CreateFactory(settings, suppressHealthLogs: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/readiness");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private sealed class ModelListResponse
    {
        public string Object { get; set; } = string.Empty;

        public List<ModelItem> Data { get; set; } = new();
    }

    private sealed class ModelItem
    {
        public string Id { get; set; } = string.Empty;

        public string Object { get; set; } = string.Empty;

        public long Created { get; set; }

        public string Owned_By { get; set; } = string.Empty;
    }
}
