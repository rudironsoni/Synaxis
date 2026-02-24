// <copyright file="ControlPlaneExtensionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.ControlPlane;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public class ControlPlaneExtensionsTests
{
    [Fact]
    public void AddControlPlane_RegistersDbContext_WithInMemoryDatabase_WhenUseInMemoryTrue()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                { "Synaxis:ControlPlane:UseInMemory", "true" },
                { "Synaxis:ControlPlane:ConnectionString", string.Empty },
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("InMemory", dbContext.Database.ProviderName ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void AddControlPlane_RegistersDbContext_WithPostgreSQL_WhenConnectionStringProvided()
    {
        var services = new ServiceCollection();
        var connectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                { "Synaxis:ControlPlane:UseInMemory", "false" },
                { "Synaxis:ControlPlane:ConnectionString", connectionString },
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("Npgsql", dbContext.Database.ProviderName ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void AddControlPlane_RegistersIDeviationRegistry_AsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                { "Synaxis:ControlPlane:UseInMemory", "true" },
                { "Synaxis:ControlPlane:ConnectionString", string.Empty },
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
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
            .AddInMemoryCollection(new Dictionary<string, string?>(
StringComparer.Ordinal)
            {
                { "Synaxis:ControlPlane:UseInMemory", "false" },
                { "Synaxis:ControlPlane:ConnectionString", string.Empty },
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddControlPlane(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var dbContext = serviceProvider.GetRequiredService<ControlPlaneDbContext>();
        Assert.NotNull(dbContext);
        Assert.Contains("InMemory", dbContext.Database.ProviderName ?? string.Empty, StringComparison.Ordinal);
    }
}
