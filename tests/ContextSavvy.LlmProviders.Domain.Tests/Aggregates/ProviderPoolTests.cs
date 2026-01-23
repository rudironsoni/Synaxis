using ContextSavvy.LlmProviders.Domain.Aggregates;
using ContextSavvy.LlmProviders.Domain.Entities;
using ContextSavvy.LlmProviders.Domain.ValueObjects;
using FluentAssertions;

namespace ContextSavvy.LlmProviders.Domain.Tests.Aggregates;

public class ProviderPoolTests
{
    [Fact]
    public void Constructor_ValidInput_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var provider = ProviderType.ChatGpt;

        // Act
        var pool = new ProviderPool(id, provider);

        // Assert
        pool.Id.Should().Be(id);
        pool.Provider.Should().Be(provider);
        pool.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void AddSession_NewSession_ShouldAddToList()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");

        // Act
        pool.AddSession(session);

        // Assert
        pool.Sessions.Should().Contain(session);
        pool.Sessions.Should().HaveCount(1);
    }

    [Fact]
    public void AddSession_DuplicateSession_ShouldNotAddAgain()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        pool.AddSession(session);

        // Act
        pool.AddSession(session);

        // Assert
        pool.Sessions.Should().HaveCount(1);
    }

    [Fact]
    public void GetAvailableSession_ReadyAndHealthySessionExists_ShouldReturnIt()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        session.MarkActive(); // Sets status to Ready
        pool.AddSession(session);

        // Act
        var result = pool.GetAvailableSession();

        // Assert
        result.Should().Be(session);
    }

    [Fact]
    public void GetAvailableSession_NoReadySession_ShouldReturnNull()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        // Status is Initializing by default
        pool.AddSession(session);

        // Act
        var result = pool.GetAvailableSession();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAvailableSession_UnhealthySession_ShouldReturnNull()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        session.MarkActive();
        session.MarkError();
        session.MarkError();
        session.MarkError(); // Becomes unhealthy (Status = Error)
        pool.AddSession(session);

        // Act
        var result = pool.GetAvailableSession();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ReturnSession_HealthySession_ShouldMarkAsReady()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        pool.AddSession(session);
        // Initially Initializing

        // Act
        pool.ReturnSession(session.Id);

        // Assert
        session.Status.Should().Be(SessionStatus.Ready);
    }

    [Fact]
    public void ReturnSession_SessionNotFound_ShouldDoNothing()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        pool.AddSession(session);

        // Act
        pool.ReturnSession(Guid.NewGuid()); // Different ID

        // Assert
        session.Status.Should().Be(SessionStatus.Initializing);
    }

    [Fact]
    public void GetHealthySessionCount_Called_ShouldReturnCorrectCount()
    {
        // Arrange
        var pool = new ProviderPool(Guid.NewGuid(), ProviderType.ChatGpt);
        
        var healthySession1 = new ProviderSession(Guid.NewGuid(), "p1");
        var healthySession2 = new ProviderSession(Guid.NewGuid(), "p2");
        var unhealthySession = new ProviderSession(Guid.NewGuid(), "p3");
        unhealthySession.MarkError();
        unhealthySession.MarkError();
        unhealthySession.MarkError();

        pool.AddSession(healthySession1);
        pool.AddSession(healthySession2);
        pool.AddSession(unhealthySession);

        // Act
        var count = pool.GetHealthySessionCount();

        // Assert
        count.Should().Be(2);
    }
}
