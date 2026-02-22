namespace Synaxis.Abstractions.Tests.Execution;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Commands;
using Synaxis.Abstractions.Execution;

public class StreamExecutorTests
{
    [Fact]
    public async Task ExecuteStreamAsync_CanStreamResults_ReturnsMultipleResults()
    {
        // Arrange
        var executor = new TestStreamExecutor();
        var request = new TestStreamRequest { Count = 5 };
        var cancellationToken = CancellationToken.None;

        // Act
        var results = new List<int>();
        await foreach (var result in executor.ExecuteStreamAsync(request, cancellationToken))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(5);
        results.Should().BeInAscendingOrder();
        results.Should().Equal(0, 1, 2, 3, 4);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var executor = new TestStreamExecutor();
        var request = new TestStreamRequest { Count = 100 };
        using var cts = new CancellationTokenSource();

        // Act
        var results = new List<int>();
        try
        {
            await foreach (var result in executor.ExecuteStreamAsync(request, cts.Token))
            {
                results.Add(result);
                if (results.Count == 3)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        results.Count.Should().BeInRange(0, 5);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithComplexType_StreamsComplexResults()
    {
        // Arrange
        var executor = new ComplexStreamExecutor();
        var request = new ComplexStreamRequest { Query = "test" };
        var cancellationToken = CancellationToken.None;

        // Act
        var results = new List<ComplexResult>();
        await foreach (var result in executor.ExecuteStreamAsync(request, cancellationToken))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.Data.Should().StartWith("test"));
    }

    [Fact]
    public async Task ExecuteStreamAsync_EmptyStream_ReturnsNoResults()
    {
        // Arrange
        var executor = new TestStreamExecutor();
        var request = new TestStreamRequest { Count = 0 };
        var cancellationToken = CancellationToken.None;

        // Act
        var results = new List<int>();
        await foreach (var result in executor.ExecuteStreamAsync(request, cancellationToken))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public void IStreamExecutor_InterfaceContract_IsCorrectlyDefined()
    {
        // Arrange
        var executor = new TestStreamExecutor();

        // Act & Assert
        executor.Should().BeAssignableTo<IStreamExecutor<TestStreamRequest, int>>();
    }

    private sealed class TestStreamRequest : IStreamRequest<int>
    {
        public int Count { get; init; }
    }

    private sealed class ComplexStreamRequest : IStreamRequest<ComplexResult>
    {
        public string Query { get; init; } = string.Empty;
    }

    private sealed class ComplexResult
    {
        public string Data { get; init; } = string.Empty;
    }

    private sealed class TestStreamExecutor : IStreamExecutor<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<int> ExecuteStreamAsync(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                yield return i;
            }
        }
    }

    private sealed class ComplexStreamExecutor : IStreamExecutor<ComplexStreamRequest, ComplexResult>
    {
        public async IAsyncEnumerable<ComplexResult> ExecuteStreamAsync(
            ComplexStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < 3; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                yield return new ComplexResult { Data = $"{request.Query}_{i}" };
            }
        }
    }
}
