using Synaplexer.Core.Metrics;
using Synaplexer.Application.Commands;
using AppChatCompletionResult = Synaplexer.Application.Dtos.ChatCompletionResult;
using Synaplexer.Application.Services;
using Synaplexer.Domain.Interfaces;
using Synaplexer.Domain.ValueObjects;
using DomainChatCompletionResult = Synaplexer.Domain.ValueObjects.ChatCompletionResult;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Synaplexer.Application.Tests.Services;

public class TieredProviderRouterTests
{
    private readonly ILogger<TieredProviderRouter> _logger;
    private readonly IMetricsCollector _metricsCollector;
    private readonly UsageTracker _usageTracker;
    private readonly List<ILlmProvider> _providers;
    private readonly TieredProviderRouter _router;

    public TieredProviderRouterTests()
    {
        _logger = Substitute.For<ILogger<TieredProviderRouter>>();
        _metricsCollector = Substitute.For<IMetricsCollector>();
        _usageTracker = new UsageTracker(Substitute.For<ILogger<UsageTracker>>());
        _providers = new List<ILlmProvider>();
        _router = new TieredProviderRouter(_providers, _logger, _metricsCollector, _usageTracker);
    }

    [Fact]
    public async Task RouteAsync_TriesProvidersInTierOrder()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", []);
        
        var p1 = CreateMockProvider("P1", ProviderTier.Tier2_Standard, true);
        var p2 = CreateMockProvider("P2", ProviderTier.Tier1_FreeFast, true); // Lower tier (Tier1 is 0 in enum)
        
        _providers.AddRange(new[] { p1, p2 });
        
        p2.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DomainChatCompletionResult("id2", "from p2", "stop", new Usage(5, 5, 10)));

        // Act
        var result = await _router.RouteAsync(command, CancellationToken.None);

        // Assert
        result.Content.Should().Be("from p2");
        await p2.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
        await p1.DidNotReceiveWithAnyArgs().ChatAsync(null!, default);
    }

    [Fact]
    public async Task RouteAsync_FallsBackToNextTierOnFailure()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", []);
        
        var p1 = CreateMockProvider("P1", ProviderTier.Tier1_FreeFast, true);
        var p2 = CreateMockProvider("P2", ProviderTier.Tier2_Standard, true);
        
        _providers.AddRange(new[] { p1, p2 });
        
        p1.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("Rate limited"));
        p2.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DomainChatCompletionResult("id2", "from p2", "stop", new Usage(5, 5, 10)));

        // Act
        var result = await _router.RouteAsync(command, CancellationToken.None);

        // Assert
        result.Content.Should().Be("from p2");
        await p1.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
        await p2.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_ReturnsFirstSuccessfulResponse()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", []);
        
        var p1 = CreateMockProvider("P1", ProviderTier.Tier1_FreeFast, true);
        var p2 = CreateMockProvider("P2", ProviderTier.Tier1_FreeFast, true);
        
        _providers.AddRange(new[] { p1, p2 });
        
        p1.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DomainChatCompletionResult("id1", "from p1", "stop", new Usage(5, 5, 10)));

        // Act
        var result = await _router.RouteAsync(command, CancellationToken.None);

        // Assert
        result.Content.Should().Be("from p1");
        await p1.Received(1).ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
        await p2.DidNotReceive().ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RouteAsync_ThrowsWhenAllTiersExhausted()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", []);
        
        var p1 = CreateMockProvider("P1", ProviderTier.Tier1_FreeFast, true);
        var p2 = CreateMockProvider("P2", ProviderTier.Tier2_Standard, true);
        
        _providers.AddRange(new[] { p1, p2 });
        
        p1.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("P1 failed"));
        p2.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>()).Throws(new Exception("P2 failed"));

        // Act & Assert
        var act = () => _router.RouteAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<AggregateException>()
            .Where(e => e.InnerExceptions.Count == 2);
    }

    [Fact]
    public async Task RouteAsync_ThrowsWhenNoEligibleProvidersFound()
    {
        // Arrange
        var command = new ChatCompletionCommand("non-existent-model", []);
        
        // Act & Assert
        var act = () => _router.RouteAsync(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No providers found*");
    }

    [Fact]
    public async Task RouteAsync_PropagatesCancellationToken()
    {
        // Arrange
        var command = new ChatCompletionCommand("gpt-4", []);
        var p1 = CreateMockProvider("P1", ProviderTier.Tier1_FreeFast, true);
        _providers.Add(p1);
        
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        p1.ChatAsync(Arg.Any<ChatRequest>(), cts.Token).Throws(new OperationCanceledException(cts.Token));

        // Act & Assert
        var act = () => _router.RouteAsync(command, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        
        await p1.Received(1).ChatAsync(Arg.Any<ChatRequest>(), cts.Token);
    }

    private ILlmProvider CreateMockProvider(string name, ProviderTier tier, bool supportsModel)
    {
        var provider = Substitute.For<ILlmProvider>();
        provider.Name.Returns(name);
        provider.Tier.Returns(tier);
        provider.SupportsModel(Arg.Any<string>()).Returns(supportsModel);
        return provider;
    }
}
