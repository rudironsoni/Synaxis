using Synaplexer.Application.DependencyInjection;
using Synaplexer.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Mediator;

namespace Synaplexer.Application.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSynaplexerApplication_RegistersRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddSynaplexerApplication();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ITieredProviderRouter>().Should().NotBeNull();
        serviceProvider.GetService<IMediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddSynaplexerApplication_RegistersTieredProviderRouterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSynaplexerApplication();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using (var scope1 = serviceProvider.CreateScope())
        using (var scope2 = serviceProvider.CreateScope())
        {
            var router1 = scope1.ServiceProvider.GetRequiredService<ITieredProviderRouter>();
            var router2 = scope1.ServiceProvider.GetRequiredService<ITieredProviderRouter>();
            var router3 = scope2.ServiceProvider.GetRequiredService<ITieredProviderRouter>();

            router1.Should().BeSameAs(router2);
            router1.Should().NotBeSameAs(router3);
        }
    }
}
