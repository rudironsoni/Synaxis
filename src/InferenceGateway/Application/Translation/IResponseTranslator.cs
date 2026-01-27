namespace Synaxis.InferenceGateway.Application.Translation;

public interface IResponseTranslator
{
    bool CanHandle(CanonicalResponse response);
    CanonicalResponse Translate(CanonicalResponse response);
}
