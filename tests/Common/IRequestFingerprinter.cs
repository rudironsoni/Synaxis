using Microsoft.Extensions.AI;

namespace Synaxis.Common.Tests;

/// <summary>
/// Interface for request fingerprinting.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IRequestFingerprinter
{
    string GenerateFingerprint(IEnumerable<ChatMessage> messages, ChatOptions? options);
}
