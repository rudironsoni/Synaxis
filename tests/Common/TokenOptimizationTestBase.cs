using Moq;
using Microsoft.Extensions.AI;

namespace Synaxis.Common.Tests;

/// <summary>
/// Base class for token optimization tests.
/// Provides common mock objects and setup for testing token optimization features.
/// </summary>
public abstract class TokenOptimizationTestBase : TestBase
{
    protected Mock<ISemanticCacheService> CacheMock { get; }
    protected Mock<IConversationStore> ConversationStoreMock { get; }
    protected Mock<ISessionStore> SessionStoreMock { get; }
    protected Mock<IInFlightDeduplicationService> DeduplicationMock { get; }
    protected Mock<IRequestFingerprinter> FingerprinterMock { get; }
    protected Mock<ITokenOptimizationConfigurationResolver> ConfigResolverMock { get; }
    protected Mock<IRequestContextProvider> ContextProviderMock { get; }
    
    protected TokenOptimizationTestBase()
    {
        CacheMock = new Mock<ISemanticCacheService>();
        ConversationStoreMock = new Mock<IConversationStore>();
        SessionStoreMock = new Mock<ISessionStore>();
        DeduplicationMock = new Mock<IInFlightDeduplicationService>();
        FingerprinterMock = new Mock<IRequestFingerprinter>();
        ConfigResolverMock = new Mock<ITokenOptimizationConfigurationResolver>();
        ContextProviderMock = new Mock<IRequestContextProvider>();
        
        SetupDefaultMockBehaviors();
    }
    
    /// <summary>
    /// Sets up default behaviors for mocks to avoid null reference exceptions.
    /// Override this method to customize default behaviors in derived classes.
    /// </summary>
    protected virtual void SetupDefaultMockBehaviors()
    {
        // Default: optimization enabled with standard config
        ConfigResolverMock.Setup(x => x.ResolveAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateOptimizationConfig());
        
        // Default: cache miss
        CacheMock.Setup(x => x.TryGetCachedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<float?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SemanticCacheResult.Miss(null));
        
        // Default: no in-flight requests
        DeduplicationMock.Setup(x => x.TryGetInFlightAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatResponse?)null);
        
        // Default: no session affinity
        SessionStoreMock.Setup(x => x.GetPreferredProviderAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        
        // Default: return same messages for compression
        ConversationStoreMock.Setup(x => x.CompressHistoryAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> msgs, CancellationToken _) => msgs);
        
        // Default: generate unique fingerprints
        FingerprinterMock.Setup(x => x.GenerateFingerprint(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>()))
            .Returns((IEnumerable<ChatMessage> msgs, ChatOptions? opts) => 
                $"fingerprint-{Guid.NewGuid()}");
        
        // Default: return empty context
        ContextProviderMock.Setup(x => x.GetTenantId())
            .Returns("test-tenant");
        ContextProviderMock.Setup(x => x.GetUserId())
            .Returns("test-user");
        ContextProviderMock.Setup(x => x.GetSessionId())
            .Returns("test-session");
    }
}

/// <summary>
/// Result of a semantic cache lookup operation.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public class SemanticCacheResult
{
    public bool IsHit { get; init; }
    public string? Response { get; init; }
    public float? SimilarityScore { get; init; }
    public float[]? QueryEmbedding { get; init; }
    
    public static SemanticCacheResult Hit(string response, float similarityScore)
        => new() { IsHit = true, Response = response, SimilarityScore = similarityScore };
    
    public static SemanticCacheResult Miss(float[]? embedding)
        => new() { IsHit = false, QueryEmbedding = embedding };
}

/// <summary>
/// Interface for semantic cache service.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ISemanticCacheService
{
    Task<SemanticCacheResult> TryGetCachedAsync(
        string query, 
        string sessionId, 
        string model, 
        string tenantId, 
        float? temperature, 
        CancellationToken cancellationToken);
    
    Task StoreAsync(
        string query, 
        string response, 
        string sessionId, 
        string model, 
        string tenantId,
        float? temperature, 
        float[]? embedding, 
        CancellationToken cancellationToken);
}

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

/// <summary>
/// Interface for managing session affinity.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ISessionStore
{
    Task<string?> GetPreferredProviderAsync(string sessionId, CancellationToken cancellationToken);
    Task SetPreferredProviderAsync(string sessionId, string providerId, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for in-flight request deduplication.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IInFlightDeduplicationService
{
    Task<ChatResponse?> TryGetInFlightAsync(string fingerprint, CancellationToken cancellationToken);
    Task RegisterInFlightAsync(string fingerprint, Task<ChatResponse> responseTask, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for request fingerprinting.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IRequestFingerprinter
{
    string GenerateFingerprint(IEnumerable<ChatMessage> messages, ChatOptions? options);
}

/// <summary>
/// Interface for token optimization configuration resolution.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface ITokenOptimizationConfigurationResolver
{
    Task<TokenOptimizationOptions> ResolveAsync(string tenantId, string userId, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for request context information.
/// This is a test stub - replace with actual implementation reference when available.
/// </summary>
public interface IRequestContextProvider
{
    string GetTenantId();
    string GetUserId();
    string GetSessionId();
}
