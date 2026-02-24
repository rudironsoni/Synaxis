namespace Synaxis.Abstractions.Tests.Execution;

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Commands;
using Synaxis.Abstractions.Execution;

public class CommandExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_CanExecuteCommand_ReturnsExpectedResult()
    {
        // Arrange
        var executor = new TestCommandExecutor();
        var command = new TestCommand { Value = 42 };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await executor.ExecuteAsync(command, cancellationToken);

        // Assert
        result.Should().Be("42");
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_PropagatesToken()
    {
        // Arrange
        var executor = new TestCommandExecutor();
        var command = new TestCommand { Value = 10 };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync(command, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexResult_ReturnsComplexType()
    {
        // Arrange
        var executor = new ComplexCommandExecutor();
        var command = new ComplexCommand { Name = "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await executor.ExecuteAsync(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("test");
    }

    [Fact]
    public void ICommandExecutor_InterfaceContract_IsCorrectlyDefined()
    {
        // Arrange
        var executor = new TestCommandExecutor();

        // Act & Assert
        executor.Should().BeAssignableTo<ICommandExecutor<TestCommand, string>>();
    }

    private sealed class TestCommand : ICommand<string>
    {
        public int Value { get; init; }
    }

    private sealed class ComplexCommand : ICommand<ComplexResult>
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class ComplexResult
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;
    }

    private sealed class TestCommandExecutor : ICommandExecutor<TestCommand, string>
    {
        public ValueTask<string> ExecuteAsync(TestCommand command, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(command.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private sealed class ComplexCommandExecutor : ICommandExecutor<ComplexCommand, ComplexResult>
    {
        public ValueTask<ComplexResult> ExecuteAsync(ComplexCommand command, CancellationToken cancellationToken)
        {
            var result = new ComplexResult
            {
                Success = true,
                Message = command.Name,
            };
            return ValueTask.FromResult(result);
        }
    }
}
