// <copyright file="ServiceCollectionExtensionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DependencyInjection.Tests;

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Configuration.Options;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSynaxisConfiguration_ShouldRegisterAllOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Cloud:DefaultProvider", "Azure" },
                { "Cloud:Azure:SubscriptionId", "test-sub" },
                { "Cloud:Azure:ResourceGroup", "test-rg" },
                { "Cloud:Azure:Region", "eastus" },
                { "Cloud:Azure:TenantId", "test-tenant" },
            })
            .Build();

        // Act
        services.AddSynaxisConfiguration(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var cloudOptions = provider.GetService<Microsoft.Extensions.Options.IOptions<CloudProviderOptions>>();
        cloudOptions.Should().NotBeNull();
        cloudOptions?.Value.DefaultProvider.Should().Be("Azure");
    }

    [Fact]
    public void AddSynaxisEventSourcing_ShouldRegisterEventStore()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSynaxisEventSourcing();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType.Name == "IEventStore");
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddSynaxisEncryption_ShouldRegisterEncryptionService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSynaxisEncryption();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType.Name == "IEncryptionService");
        descriptor.Should().NotBeNull();
    }
}
