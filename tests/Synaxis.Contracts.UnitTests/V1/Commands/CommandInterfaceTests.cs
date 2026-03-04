namespace Synaxis.Contracts.Tests.V1.Commands;

using FluentAssertions;
using Synaxis.Abstractions.Commands;
using Synaxis.Contracts.V1.Commands;

[Trait("Category", "Unit")]
public class CommandInterfaceTests
{
    [Fact]
    public void IChatCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestChatCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestChatResponse>>();
        command.Should().BeAssignableTo<IChatCommand<TestChatResponse>>();
    }

    [Fact]
    public void IEmbeddingCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestEmbeddingCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestEmbeddingResponse>>();
        command.Should().BeAssignableTo<IEmbeddingCommand<TestEmbeddingResponse>>();
    }

    [Fact]
    public void IImageGenerationCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestImageGenerationCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestImageResponse>>();
        command.Should().BeAssignableTo<IImageGenerationCommand<TestImageResponse>>();
    }

    [Fact]
    public void IChatStreamCommand_ExtendsIStreamRequest()
    {
        // Arrange
        var command = new TestChatStreamCommand();

        // Act & Assert
        command.Should().BeAssignableTo<IStreamRequest<TestChatChunk>>();
        command.Should().BeAssignableTo<IChatStreamCommand<TestChatChunk>>();
    }

    [Fact]
    public void IAudioTranscriptionCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestAudioTranscriptionCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestTranscriptionResponse>>();
        command.Should().BeAssignableTo<IAudioTranscriptionCommand<TestTranscriptionResponse>>();
    }

    [Fact]
    public void IAudioSynthesisCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestAudioSynthesisCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestAudioResponse>>();
        command.Should().BeAssignableTo<IAudioSynthesisCommand<TestAudioResponse>>();
    }

    [Fact]
    public void IRerankCommand_ExtendsICommand()
    {
        // Arrange
        var command = new TestRerankCommand();

        // Act & Assert
        command.Should().BeAssignableTo<ICommand<TestRerankResponse>>();
        command.Should().BeAssignableTo<IRerankCommand<TestRerankResponse>>();
    }

    private sealed class TestChatCommand : IChatCommand<TestChatResponse>
    {
        public TestChatResponse Response => new();
    }

    private sealed class TestEmbeddingCommand : IEmbeddingCommand<TestEmbeddingResponse>
    {
        public TestEmbeddingResponse Response => new();
    }

    private sealed class TestImageGenerationCommand : IImageGenerationCommand<TestImageResponse>
    {
        public TestImageResponse Response => new();
    }

    private sealed class TestChatStreamCommand : IChatStreamCommand<TestChatChunk>
    {
        public TestChatChunk Response => new();
    }

    private sealed class TestAudioTranscriptionCommand : IAudioTranscriptionCommand<TestTranscriptionResponse>
    {
        public TestTranscriptionResponse Response => new();
    }

    private sealed class TestAudioSynthesisCommand : IAudioSynthesisCommand<TestAudioResponse>
    {
        public TestAudioResponse Response => new();
    }

    private sealed class TestRerankCommand : IRerankCommand<TestRerankResponse>
    {
        public TestRerankResponse Response => new();
    }

    private sealed class TestChatResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestEmbeddingResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestImageResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestChatChunk
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestTranscriptionResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestAudioResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed class TestRerankResponse
    {
        public string Id { get; init; } = string.Empty;
    }
}
