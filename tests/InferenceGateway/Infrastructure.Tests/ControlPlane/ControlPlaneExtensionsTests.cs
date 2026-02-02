using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.ControlPlane;

public class ControlPlaneExtensionsTests
{
    [Fact]
    public void AddControlPlane_RegistersDbContext_WithInMemoryDatabase_WhenUseInMemoryTrue()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Synaxis:ControlPlane:UseInMemory", "true" },
                { "Synaxis:ControlPlane:ConnectionString", "" }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("InMemory", dbContext.Database.ProviderName ?? "");
    }

    [Fact]
    public void AddControlPlane_RegistersDbContext_WithPostgreSQL_WhenConnectionStringProvided()
    {
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Synaxis:ControlPlane:UseInMemory", "false" },
                { "Synaxis:ControlPlane:ConnectionString", connectionString }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("Npgsql", dbContext.Database.ProviderName ?? "");
    }

    [Fact]
    public void AddControlPlane_RegistersIDeviationRegistry_AsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Synaxis:ControlPlane:UseInMemory", "true" },
                { "Synaxis:ControlPlane:ConnectionString", "" }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var registry1 = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IDeviationRegistry>();
        var registry2 = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IDeviationRegistry>();

        Assert.NotNull(registry1);
        Assert.NotNull(registry2);
        Assert.NotSame(registry1, registry2);
    }

    [Fact]
    public void AddControlPlane_UsesInMemory_WhenConnectionStringIsEmpty()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Synaxis:ControlPlane:UseInMemory", "false" },
                { "Synaxis:ControlPlane:ConnectionString", "" }
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("InMemory", dbContext.Database.ProviderName ?? "");
    }
}
