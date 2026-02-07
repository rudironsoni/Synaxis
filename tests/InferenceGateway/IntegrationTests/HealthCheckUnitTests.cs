using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.WebApi.Health;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests;

public class HealthCheckUnitTests
{
    [Fact]
    public async Task ConfigHealthCheck_ReturnsUnhealthy_WhenNoProviders()
    {
        var config = new SynaxisConfiguration();
        var check = new ConfigHealthCheck(Options.Create(config));

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ConfigHealthCheck_ReturnsUnhealthy_WhenCanonicalModelProviderMissing()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig { Type = "openai", Models = new List<string> { "model-a" } },
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new () { Id = "model-x", Provider = "MissingProvider", ModelPath = "model-x" }
            },
        };

        var check = new ConfigHealthCheck(Options.Create(config));
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ConfigHealthCheck_ReturnsUnhealthy_WhenAliasCandidateMissing()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig { Type = "openai", Models = new List<string> { "model-a" } },
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new () { Id = "model-a", Provider = "ProviderA", ModelPath = "model-a" },
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["fast"] = new AliasConfig { Candidates = new List<string> { "missing-model" } }
            },
        };

        var check = new ConfigHealthCheck(Options.Create(config));
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ConfigHealthCheck_ReturnsHealthy_WhenConfigIsValid()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig { Type = "openai", Models = new List<string> { "model-a" } },
            },
            CanonicalModels = new List<CanonicalModelConfig>
            {
                new () { Id = "model-a", Provider = "ProviderA", ModelPath = "model-a" },
            },
            Aliases = new Dictionary<string, AliasConfig>
            {
                ["fast"] = new AliasConfig { Candidates = new List<string> { "model-a" } }
            },
        };

        var check = new ConfigHealthCheck(Options.Create(config));
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ProviderConnectivityHealthCheck_ReturnsHealthy_WhenAllProvidersDisabled()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig { Enabled = false, Type = "openai" }
            },
        };

        var check = new ProviderConnectivityHealthCheck(Options.Create(config), NullLogger<ProviderConnectivityHealthCheck>.Instance);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task ProviderConnectivityHealthCheck_ReturnsUnhealthy_WhenEndpointMissing()
    {
        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig { Enabled = true, Type = "custom" }
            },
        };

        var check = new ProviderConnectivityHealthCheck(Options.Create(config), NullLogger<ProviderConnectivityHealthCheck>.Instance);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task ProviderConnectivityHealthCheck_ReturnsHealthy_WhenHttpEndpointReachable()
    {
        await using var server = await LocalHttpServer.StartAsync();

        var config = new SynaxisConfiguration
        {
            Providers = new Dictionary<string, ProviderConfig>
            {
                ["ProviderA"] = new ProviderConfig
                {
                    Enabled = true,
                    Type = "custom",
                    Endpoint = $"http://127.0.0.1:{server.Port}"
                }
            },
        };

        var check = new ProviderConnectivityHealthCheck(Options.Create(config), NullLogger<ProviderConnectivityHealthCheck>.Instance);
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    private sealed class LocalHttpServer : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private Task _acceptTask;

        private LocalHttpServer(TcpListener listener)
        {
            this._listener = listener;
            this._acceptTask = this.AcceptOnceAsync();
        }

        public int Port => ((IPEndPoint)this._listener.LocalEndpoint).Port;

        public static Task<LocalHttpServer> StartAsync()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var server = new LocalHttpServer(listener);
            return Task.FromResult(server);
        }

        private async Task AcceptOnceAsync()
        {
            try
            {
                using var tcpProbe = await this._listener.AcceptTcpClientAsync(this._cts.Token);
                tcpProbe.Close();

                using var httpClient = await this._listener.AcceptTcpClientAsync(this._cts.Token);
                using var stream = httpClient.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);

                string? line;
                do
                {
                    line = await reader.ReadLineAsync();
                } while (line != null && line.Length > 0);

                var response = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\n\r\n";
                var bytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(bytes, 0, bytes.Length, this._cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public async ValueTask DisposeAsync()
        {
            this._cts.Cancel();
            this._listener.Stop();
            await this._acceptTask;
            this._cts.Dispose();
        }
    }
}
