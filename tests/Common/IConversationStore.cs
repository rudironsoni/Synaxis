using Microsoft.Extensions.AI;

namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for managing conversation history.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IConversationStore
{
    Task AddMessageAsync(string sessionId, ChatMessage message, CancellationToken cancellationToken);

    Task<IEnumerable<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken);

    Task<IEnumerable<ChatMessage>> CompressHistoryAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken);
}
