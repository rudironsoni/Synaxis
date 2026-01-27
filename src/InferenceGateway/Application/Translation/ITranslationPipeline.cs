using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Translation;

public interface ITranslationPipeline
{
    CanonicalRequest TranslateRequest(CanonicalRequest request);
    CanonicalResponse TranslateResponse(CanonicalResponse response);
    ChatResponseUpdate TranslateUpdate(ChatResponseUpdate update);
}
