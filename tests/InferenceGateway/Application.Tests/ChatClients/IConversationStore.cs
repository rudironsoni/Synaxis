using Microsoft.Extensions.AI;

namespace Synaxis.InferenceGateway.Application.Tests.ChatClients;

/// <summary>
/// Interface for managing conversation history
/// </summary>
public interface IConversationStore
{
    Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken);

    Task<IEnumerable<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken);

    Task<IEnumerable<ChatMessage>> CompressHistoryAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}
