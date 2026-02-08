using Microsoft.Extensions.AI;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Application.Translation;

namespace Synaxis.InferenceGateway.Application.Tests.Translation;

public class TranslationPipelineTests
{
    [Fact]
    public void TranslateRequest_UsesFirstMatchingTranslator()
    {
        var first = new TestRequestTranslator("first-model", true);
        var second = new TestRequestTranslator("second-model", true);
        var pipeline = new TranslationPipeline(new[] { first, second }, Array.Empty<IResponseTranslator>(), Array.Empty<IStreamingTranslator>(), new OpenAIToolNormalizer());

        var request = new CanonicalRequest(EndpointKind.ChatCompletions, "original", new List<ChatMessage>());

        var result = pipeline.TranslateRequest(request);

        Assert.Equal("first-model", result.Model);
    }

    [Fact]
    public void TranslateResponse_UsesFirstMatchingTranslator()
    {
        var first = new TestResponseTranslator("first-response", true);
        var second = new TestResponseTranslator("second-response", true);
        var pipeline = new TranslationPipeline(Array.Empty<IRequestTranslator>(), new[] { first, second }, Array.Empty<IStreamingTranslator>(), new OpenAIToolNormalizer());

        var response = new CanonicalResponse("original");

        var result = pipeline.TranslateResponse(response);

        Assert.Equal("first-response", result.Content);
    }

    [Fact]
    public void TranslateUpdate_UsesFirstMatchingTranslator()
    {
        var first = new TestStreamingTranslator("first");
        var second = new TestStreamingTranslator("second");
        var pipeline = new TranslationPipeline(Array.Empty<IRequestTranslator>(), Array.Empty<IResponseTranslator>(), new[] { first, second }, new OpenAIToolNormalizer());

        var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
        update.Contents.Add(new TextContent("original"));

        var result = pipeline.TranslateUpdate(update);

        Assert.Equal("first", result.Text);
    }

    private sealed class TestRequestTranslator : IRequestTranslator
    {
        private readonly string _model;
        private readonly bool _canHandle;

        public TestRequestTranslator(string model, bool canHandle)
        {
            this._model = model;
            this._canHandle = canHandle;
        }

        public bool CanHandle(CanonicalRequest request) => this._canHandle;

#pragma warning disable SA1101 // False positive with record 'with' expressions
        public CanonicalRequest Translate(CanonicalRequest request)
            => request with { Model = this._model };
#pragma warning restore SA1101
    }

    private sealed class TestResponseTranslator : IResponseTranslator
    {
        private readonly string _content;
        private readonly bool _canHandle;

        public TestResponseTranslator(string content, bool canHandle)
        {
            this._content = content;
            this._canHandle = canHandle;
        }

        public bool CanHandle(CanonicalResponse response) => this._canHandle;

#pragma warning disable SA1101 // False positive with record 'with' expressions
        public CanonicalResponse Translate(CanonicalResponse response)
            => response with { Content = this._content };
#pragma warning restore SA1101
    }

    private sealed class TestStreamingTranslator : IStreamingTranslator
    {
        private readonly string _text;

        public TestStreamingTranslator(string text)
        {
            this._text = text;
        }

        public bool CanHandle(ChatResponseUpdate update) => true;

        public ChatResponseUpdate Translate(ChatResponseUpdate update)
        {
            var translated = new ChatResponseUpdate { Role = update.Role };
            translated.Contents.Add(new TextContent(this._text));
            return translated;
        }
    }
}
