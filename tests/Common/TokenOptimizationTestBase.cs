// <copyright file="TokenOptimizationTestBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

using Microsoft.Extensions.AI;
using Moq;

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
        this.CacheMock = new Mock<ISemanticCacheService>();
        this.ConversationStoreMock = new Mock<IConversationStore>();
        this.SessionStoreMock = new Mock<ISessionStore>();
        this.DeduplicationMock = new Mock<IInFlightDeduplicationService>();
        this.FingerprinterMock = new Mock<IRequestFingerprinter>();
        this.ConfigResolverMock = new Mock<ITokenOptimizationConfigurationResolver>();
        this.ContextProviderMock = new Mock<IRequestContextProvider>();
    }

    /// <summary>
    /// Sets up default behaviors for mocks to avoid null reference exceptions.
    /// Override this method to customize default behaviors in derived classes.
    /// </summary>
    protected virtual void SetupDefaultMockBehaviors()
    {
        // Default: optimization enabled with standard config
        this.ConfigResolverMock.Setup(x => x.ResolveAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateOptimizationConfig());

        // Default: cache miss
        this.CacheMock.Setup(x => x.TryGetCachedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<float?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SemanticCacheResult.Miss(null));

        // Default: no in-flight requests
        this.DeduplicationMock.Setup(x => x.TryGetInFlightAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatResponse?)null);

        // Default: no session affinity
        this.SessionStoreMock.Setup(x => x.GetPreferredProviderAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Default: return same messages for compression
        this.ConversationStoreMock.Setup(x => x.CompressHistoryAsync(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> msgs, CancellationToken _) => msgs);

        // Default: generate unique fingerprints
        this.FingerprinterMock.Setup(x => x.GenerateFingerprint(
                It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions?>()))
            .Returns((IEnumerable<ChatMessage> msgs, ChatOptions? opts) =>
                $"fingerprint-{Guid.NewGuid()}");

        // Default: return empty context
        this.ContextProviderMock.Setup(x => x.GetTenantId())
            .Returns("test-tenant");
        this.ContextProviderMock.Setup(x => x.GetUserId())
            .Returns("test-user");
        this.ContextProviderMock.Setup(x => x.GetSessionId())
            .Returns("test-session");
    }
}
