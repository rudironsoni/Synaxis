namespace Synaxis.Abstractions.Tests.Commands;

using FluentAssertions;
using Synaxis.Abstractions.Commands;

public class MarkerInterfaceTests
{
    [Fact]
    public void ICommand_CanBeImplemented()
    {
        // Arrange & Act
        var command = new TestCommand();

        // Assert
        command.Should().BeAssignableTo<ICommand<string>>();
    }

    [Fact]
    public void IStreamRequest_CanBeImplemented()
    {
        // Arrange & Act
        var request = new TestStreamRequest();

        // Assert
        request.Should().BeAssignableTo<IStreamRequest<int>>();
    }

    [Fact]
    public void INotification_CanBeImplemented()
    {
        // Arrange & Act
        var notification = new TestNotification();

        // Assert
        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void ICommand_WithComplexType_CanBeImplemented()
    {
        // Arrange & Act
        var command = new ComplexCommand();

        // Assert
        command.Should().BeAssignableTo<ICommand<ComplexResult>>();
        command.Data.Should().Be("test");
    }

    [Fact]
    public void IStreamRequest_WithComplexType_CanBeImplemented()
    {
        // Arrange & Act
        var request = new ComplexStreamRequest();

        // Assert
        request.Should().BeAssignableTo<IStreamRequest<ComplexResult>>();
        request.Query.Should().Be("query");
    }

    private sealed class TestCommand : ICommand<string>
    {
    }

    private sealed class TestStreamRequest : IStreamRequest<int>
    {
    }

    private sealed class TestNotification : INotification
    {
    }

    private sealed class ComplexCommand : ICommand<ComplexResult>
    {
        public string Data { get; init; } = "test";
    }

    private sealed class ComplexStreamRequest : IStreamRequest<ComplexResult>
    {
        public string Query { get; init; } = "query";
    }

    private sealed class ComplexResult
    {
        public int Value { get; init; }
    }
}
