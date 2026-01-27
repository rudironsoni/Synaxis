using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Translation;

public sealed class NoOpRequestTranslator : IRequestTranslator
{
    public bool CanHandle(CanonicalRequest request) => false;

    public CanonicalRequest Translate(CanonicalRequest request) => request;
}

public sealed class NoOpResponseTranslator : IResponseTranslator
{
    public bool CanHandle(CanonicalResponse response) => false;

    public CanonicalResponse Translate(CanonicalResponse response) => response;
}

public sealed class NoOpStreamingTranslator : IStreamingTranslator
{
    public bool CanHandle(ChatResponseUpdate update) => false;

    public ChatResponseUpdate Translate(ChatResponseUpdate update) => update;
}
