using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Translation;

public interface IStreamingTranslator
{
    bool CanHandle(ChatResponseUpdate update);
    ChatResponseUpdate Translate(ChatResponseUpdate update);
}
