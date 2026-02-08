namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class ConversationMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
