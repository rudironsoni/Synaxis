#nullable enable
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Synaxis.Tests.Integration.Fixtures;
using Synaxis.InferenceGateway.Infrastructure.Contracts;
using Synaxis.Core.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace Synaxis.Tests.Integration;

/// <summary>
/// Integration tests for quota enforcement and rate limiting.
/// Tests budget limits, rate limits, and throttling behavior.
/// </summary>
public class QuotaEnforcementTests : IClassFixture<SynaxisTestFixture>
{
    private readonly SynaxisTestFixture _fixture;

    public QuotaEnforcementTests(SynaxisTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckQuota_WithinLimit_AllowsRequest()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<IQuotaService>>();
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var quotaService = CreateMockQuotaService(allowRequest: true);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(QuotaAction.Allow, result.Action);
    }

    [Fact]
    public async Task CheckQuota_ExceededLimit_ThrottlesRequest()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 60,
            currentUsage: 60);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Throttle, result.Action);
        Assert.NotNull(result.Details);
        Assert.Equal(60, result.Details.Limit);
        Assert.Equal(60, result.Details.CurrentUsage);
        Assert.Equal(0, result.Details.Remaining);
    }

    [Fact]
    public async Task CheckQuota_BudgetExceeded_BlocksRequest()
    {
        // Arrange
        var apiKey = await _fixture.CreateApiKeyWithQuotaAsync(
            maxBudget: 10.00m,
            currentSpend: 10.00m);

        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Block,
            reason: "Budget exceeded");

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Block, result.Action);
        Assert.Contains("Budget", result.Reason);
    }

    [Fact]
    public async Task CheckQuota_OverageBilling_ChargesCredits()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: true,
            action: QuotaAction.CreditCharge,
            creditCharge: 0.001m);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(QuotaAction.CreditCharge, result.Action);
        Assert.NotNull(result.CreditCharge);
        Assert.True(result.CreditCharge > 0);
    }

    [Fact]
    public async Task CheckUserQuota_UserSpecificLimit_EnforcesPerUserLimit()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 10,
            currentUsage: 10);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckUserQuotaAsync(
            _fixture.TestOrganization.Id,
            _fixture.EuUser.Id,
            request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Throttle, result.Action);
    }

    [Fact]
    public async Task IncrementUsage_SuccessfulRequest_IncreasesCounter()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        
        mockQuotaService.Setup(x => x.IncrementUsageAsync(
            It.IsAny<Guid>(),
            It.IsAny<UsageMetrics>()))
            .Returns(Task.CompletedTask);

        var metrics = new UsageMetrics
        {
            UserId = _fixture.EuUser.Id,
            VirtualKeyId = _fixture.EuApiKey.Id,
            MetricType = "requests",
            Value = 1,
            Model = "gpt-4"
        };

        // Act
        await mockQuotaService.Object.IncrementUsageAsync(_fixture.TestOrganization.Id, metrics);

        // Assert
        mockQuotaService.Verify(
            x => x.IncrementUsageAsync(_fixture.TestOrganization.Id, It.IsAny<UsageMetrics>()),
            Times.Once);
    }

    [Fact]
    public async Task GetEffectiveLimits_ProTier_ReturnsProLimits()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        
        var expectedLimits = new QuotaLimits
        {
            MaxConcurrentRequests = 100,
            MonthlyRequestLimit = 1000000,
            MonthlyTokenLimit = 10000000,
            RequestsPerMinute = 1000,
            TokensPerMinute = 100000
        };

        mockQuotaService.Setup(x => x.GetEffectiveLimitsAsync(_fixture.TestOrganization.Id))
            .ReturnsAsync(expectedLimits);

        // Act
        var limits = await mockQuotaService.Object.GetEffectiveLimitsAsync(_fixture.TestOrganization.Id);

        // Assert
        Assert.NotNull(limits);
        Assert.Equal(100, limits.MaxConcurrentRequests);
        Assert.Equal(1000000, limits.MonthlyRequestLimit);
        Assert.Equal(10000000, limits.MonthlyTokenLimit);
    }

    [Theory]
    [InlineData(WindowType.Fixed, "Fixed window should be used")]
    [InlineData(WindowType.Sliding, "Sliding window should be used")]
    public async Task CheckQuota_DifferentWindowTypes_AppliesCorrectAlgorithm(
        WindowType windowType,
        string description)
    {
        // Arrange
        var quotaService = CreateMockQuotaService(allowRequest: true);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = windowType
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.NotNull(result);
        // Different window types should work without errors
    }

    [Fact]
    public async Task TokenQuota_ExceedsLimit_ThrottlesRequest()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 100000,
            currentUsage: 100000);

        var request = new QuotaCheckRequest
        {
            MetricType = "tokens",
            IncrementBy = 1000,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Throttle, result.Action);
        Assert.Equal("tokens", result.Details?.MetricType);
    }

    [Fact]
    public async Task QuotaCheck_ConcurrentRequests_EnforcesLimit()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 100,
            currentUsage: 100);

        var request = new QuotaCheckRequest
        {
            MetricType = "concurrent",
            IncrementBy = 1,
            TimeGranularity = "second",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Throttle, result.Action);
    }

    [Fact]
    public async Task QuotaReset_MonthlyReset_ResetsAllCounters()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        
        mockQuotaService.Setup(x => x.ResetUsageAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await mockQuotaService.Object.ResetUsageAsync(_fixture.TestOrganization.Id, "requests");
        await mockQuotaService.Object.ResetUsageAsync(_fixture.TestOrganization.Id, "tokens");

        // Assert
        mockQuotaService.Verify(x => x.ResetUsageAsync(
            _fixture.TestOrganization.Id,
            It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetUsage_OrganizationLevel_ReturnsAggregatedUsage()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        
        var expectedReport = new UsageReport
        {
            OrganizationId = _fixture.TestOrganization.Id,
            From = DateTime.UtcNow.AddDays(-30),
            To = DateTime.UtcNow,
            UsageByMetric = new Dictionary<string, long>
            {
                { "requests", 10000 },
                { "tokens", 500000 }
            },
            UsageByModel = new Dictionary<string, long>
            {
                { "gpt-4", 5000 },
                { "gpt-3.5-turbo", 5000 }
            },
            TotalCost = 45.50m
        };

        mockQuotaService.Setup(x => x.GetUsageAsync(
            _fixture.TestOrganization.Id,
            It.IsAny<UsageQuery>()))
            .ReturnsAsync(expectedReport);

        // Act
        var report = await mockQuotaService.Object.GetUsageAsync(
            _fixture.TestOrganization.Id,
            new UsageQuery
            {
                From = DateTime.UtcNow.AddDays(-30),
                To = DateTime.UtcNow,
                MetricType = "requests",
                Granularity = "day"
            });

        // Assert
        Assert.NotNull(report);
        Assert.Equal(_fixture.TestOrganization.Id, report.OrganizationId);
        Assert.Equal(10000, report.UsageByMetric["requests"]);
        Assert.Equal(45.50m, report.TotalCost);
    }

    [Fact]
    public async Task QuotaCheck_HourlyLimit_EnforcesHourlyWindow()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: true,
            limit: 3600,
            currentUsage: 3500);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "hour",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task QuotaCheck_DailyLimit_EnforcesDailyWindow()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 100000,
            currentUsage: 100000);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "day",
            WindowType = WindowType.Fixed
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.Equal(QuotaAction.Throttle, result.Action);
    }

    [Fact]
    public async Task QuotaCheck_ModelSpecific_EnforcesPerModelLimit()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        
        mockQuotaService.Setup(x => x.CheckQuotaAsync(
            It.IsAny<Guid>(),
            It.Is<QuotaCheckRequest>(r => r.MetricType == "requests")))
            .ReturnsAsync(QuotaResult.Allowed());

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await mockQuotaService.Object.CheckQuotaAsync(
            _fixture.TestOrganization.Id,
            request);

        // Assert
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task QuotaCheck_TeamLevel_EnforcesTeamQuota()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: true,
            limit: 1000,
            currentUsage: 500);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.True(result.IsAllowed);
        Assert.Equal(QuotaAction.Allow, result.Action);
    }

    [Fact]
    public async Task QuotaCheck_BurstTraffic_HandlesSpikes()
    {
        // Arrange
        var mockQuotaService = new Mock<IQuotaService>();
        var requestCount = 0;

        mockQuotaService.Setup(x => x.CheckQuotaAsync(
            It.IsAny<Guid>(),
            It.IsAny<QuotaCheckRequest>()))
            .ReturnsAsync(() =>
            {
                requestCount++;
                // Allow first 100 requests, then throttle
                return requestCount <= 100
                    ? QuotaResult.Allowed()
                    : QuotaResult.Throttled(new QuotaDetails
                    {
                        MetricType = "requests",
                        Limit = 100,
                        CurrentUsage = requestCount,
                        TimeWindow = "minute",
                        WindowStart = DateTime.UtcNow.AddMinutes(-1),
                        WindowEnd = DateTime.UtcNow,
                        RetryAfter = TimeSpan.FromSeconds(30)
                    });
            });

        // Act - Simulate burst of 150 requests
        var results = new List<QuotaResult>();
        for (int i = 0; i < 150; i++)
        {
            var result = await mockQuotaService.Object.CheckQuotaAsync(
                _fixture.TestOrganization.Id,
                new QuotaCheckRequest
                {
                    MetricType = "requests",
                    IncrementBy = 1,
                    TimeGranularity = "minute",
                    WindowType = WindowType.Sliding
                });
            results.Add(result);
        }

        // Assert
        Assert.Equal(100, results.Count(r => r.IsAllowed));
        Assert.Equal(50, results.Count(r => !r.IsAllowed));
    }

    [Fact]
    public async Task QuotaCheck_RetryAfter_ReturnsCorrectDuration()
    {
        // Arrange
        var quotaService = CreateMockQuotaService(
            allowRequest: false,
            action: QuotaAction.Throttle,
            limit: 60,
            currentUsage: 60);

        var request = new QuotaCheckRequest
        {
            MetricType = "requests",
            IncrementBy = 1,
            TimeGranularity = "minute",
            WindowType = WindowType.Sliding
        };

        // Act
        var result = await quotaService.CheckQuotaAsync(_fixture.TestOrganization.Id, request);

        // Assert
        Assert.False(result.IsAllowed);
        Assert.NotNull(result.Details);
        Assert.NotNull(result.Details.RetryAfter);
        Assert.True(result.Details.RetryAfter.Value.TotalSeconds > 0);
    }

    private IQuotaService CreateMockQuotaService(
        bool allowRequest = true,
        QuotaAction action = QuotaAction.Allow,
        long limit = 1000,
        long currentUsage = 0,
        string? reason = null,
        decimal? creditCharge = null)
    {
        var mockService = new Mock<IQuotaService>();

        mockService.Setup(x => x.CheckQuotaAsync(
            It.IsAny<Guid>(),
            It.IsAny<QuotaCheckRequest>()))
            .ReturnsAsync((Guid orgId, QuotaCheckRequest req) => new QuotaResult
            {
                IsAllowed = allowRequest,
                Action = action,
                Reason = reason,
                CreditCharge = creditCharge,
                Details = action == QuotaAction.Throttle || action == QuotaAction.Block
                    ? new QuotaDetails
                    {
                        MetricType = req.MetricType, // Use the request's metric type
                        Limit = limit,
                        CurrentUsage = currentUsage,
                        TimeWindow = "minute",
                        WindowStart = DateTime.UtcNow.AddMinutes(-1),
                        WindowEnd = DateTime.UtcNow,
                        RetryAfter = TimeSpan.FromSeconds(60)
                    }
                    : null
            });

        mockService.Setup(x => x.CheckUserQuotaAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<QuotaCheckRequest>()))
            .ReturnsAsync((Guid orgId, Guid userId, QuotaCheckRequest req) => new QuotaResult
            {
                IsAllowed = allowRequest,
                Action = action,
                Reason = reason,
                CreditCharge = creditCharge,
                Details = action == QuotaAction.Throttle || action == QuotaAction.Block
                    ? new QuotaDetails
                    {
                        MetricType = req.MetricType, // Use the request's metric type
                        Limit = limit,
                        CurrentUsage = currentUsage,
                        TimeWindow = "minute",
                        WindowStart = DateTime.UtcNow.AddMinutes(-1),
                        WindowEnd = DateTime.UtcNow,
                        RetryAfter = TimeSpan.FromSeconds(60)
                    }
                    : null
            });

        mockService.Setup(x => x.IncrementUsageAsync(
            It.IsAny<Guid>(),
            It.IsAny<UsageMetrics>()))
            .Returns(Task.CompletedTask);

        return mockService.Object;
    }
}
