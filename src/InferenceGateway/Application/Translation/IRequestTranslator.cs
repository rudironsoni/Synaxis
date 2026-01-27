namespace Synaxis.InferenceGateway.Application.Translation;

public interface IRequestTranslator
{
    bool CanHandle(CanonicalRequest request);
    CanonicalRequest Translate(CanonicalRequest request);
}
