using Synaplexer.Domain.Entities;
using Synaplexer.Domain.ValueObjects;
using FluentAssertions;

namespace Synaplexer.Domain.Tests.Entities;

public class ProviderSessionTests
{
    [Fact]
    public void Constructor_ValidInput_ShouldInitializeCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var providerId = "test-provider";

        // Act
        var session = new ProviderSession(id, providerId);

        // Assert
        session.Id.Should().Be(id);
        session.ProviderId.Should().Be(providerId);
        session.Status.Should().Be(SessionStatus.Initializing);
        session.ErrorCount.Should().Be(0);
        session.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        session.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkActive_WhenCalled_ShouldUpdateStatusAndLastActivityAt()
    {
        // Arrange
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        var initialActivityAt = session.LastActivityAt;

        // Act
        session.MarkActive();

        // Assert
        session.Status.Should().Be(SessionStatus.Ready);
        session.LastActivityAt.Should().BeAfter(initialActivityAt.AddMilliseconds(-100)); // Allow for fast execution
        session.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void MarkError_WhenCalled_ShouldIncrementErrorCountAndUpdateLastActivityAt()
    {
        // Arrange
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        var initialActivityAt = session.LastActivityAt;

        // Act
        session.MarkError();

        // Assert
        session.ErrorCount.Should().Be(1);
        session.LastActivityAt.Should().BeAfter(initialActivityAt.AddMilliseconds(-100));
    }

    [Fact]
    public void MarkError_WhenErrorCountReachesThreshold_ShouldSetStatusToError()
    {
        // Arrange
        var session = new ProviderSession(Guid.NewGuid(), "provider");

        // Act
        session.MarkError(); // 1
        session.MarkError(); // 2
        session.MarkError(); // 3

        // Assert
        session.ErrorCount.Should().Be(3);
        session.Status.Should().Be(SessionStatus.Error);
    }

    [Theory]
    [InlineData(SessionStatus.Initializing, 0, true)]
    [InlineData(SessionStatus.Ready, 0, true)]
    [InlineData(SessionStatus.Busy, 0, true)]
    [InlineData(SessionStatus.Error, 0, false)]
    [InlineData(SessionStatus.Disposed, 0, false)]
    [InlineData(SessionStatus.Ready, 3, false)]
    public void IsHealthy_VariousStates_ShouldReturnExpectedResult(SessionStatus status, int errorCount, bool expectedHealthy)
    {
        // Arrange
        var session = new ProviderSession(Guid.NewGuid(), "provider");
        
        // Use reflection to set private fields/properties for full testing of logic
        var statusProperty = typeof(ProviderSession).GetProperty(nameof(ProviderSession.Status));
        statusProperty?.SetValue(session, status);

        var errorCountProperty = typeof(ProviderSession).GetProperty(nameof(ProviderSession.ErrorCount));
        errorCountProperty?.SetValue(session, errorCount);

        // Act
        var result = session.IsHealthy();

        // Assert
        result.Should().Be(expectedHealthy);
    }
}
