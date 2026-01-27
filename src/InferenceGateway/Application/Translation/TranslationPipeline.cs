using Microsoft.Extensions.AI;
using System.Linq;

namespace Synaxis.InferenceGateway.Application.Translation;

public sealed class TranslationPipeline : ITranslationPipeline
{
    private readonly IReadOnlyList<IRequestTranslator> _requestTranslators;
    private readonly IReadOnlyList<IResponseTranslator> _responseTranslators;
    private readonly IReadOnlyList<IStreamingTranslator> _streamingTranslators;
    private readonly IToolNormalizer _toolNormalizer;

    public TranslationPipeline(
        IEnumerable<IRequestTranslator> requestTranslators,
        IEnumerable<IResponseTranslator> responseTranslators,
        IEnumerable<IStreamingTranslator> streamingTranslators,
        IToolNormalizer toolNormalizer)
    {
        _requestTranslators = requestTranslators.ToList();
        _responseTranslators = responseTranslators.ToList();
        _streamingTranslators = streamingTranslators.ToList();
        _toolNormalizer = toolNormalizer;
    }

    public CanonicalRequest TranslateRequest(CanonicalRequest request)
    {
        foreach (var translator in _requestTranslators)
        {
            if (translator.CanHandle(request))
            {
                return translator.Translate(request);
            }
        }

        return request;
    }

    public CanonicalResponse TranslateResponse(CanonicalResponse response)
    {
        var normalized = _toolNormalizer.NormalizeResponse(response);
        foreach (var translator in _responseTranslators)
        {
            if (translator.CanHandle(normalized))
            {
                return translator.Translate(normalized);
            }
        }

        return normalized;
    }

    public ChatResponseUpdate TranslateUpdate(ChatResponseUpdate update)
    {
        var normalized = _toolNormalizer.NormalizeUpdate(update);
        foreach (var translator in _streamingTranslators)
        {
            if (translator.CanHandle(normalized))
            {
                return translator.Translate(normalized);
            }
        }

        return normalized;
    }
}
