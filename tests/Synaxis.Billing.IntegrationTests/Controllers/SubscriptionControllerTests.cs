// <copyright file="SubscriptionControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;

using global::Billing.Application.Services;
using global::Billing.Domain.Entities;
using global::Billing.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Synaxis.Billing.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the Subscriptions controller.
/// </summary>
[Collection("BillingIntegration")]
[Trait("Category", "Integration")]
public sealed class SubscriptionControllerTests : IAsyncLifetime
{
    private readonly BillingDatabaseFixture _dbFixture;
    private readonly BillingApiFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _organizationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionControllerTests"/> class.
    /// </summary>
    public SubscriptionControllerTests(BillingDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
        _factory = new BillingApiFactory();
        _organizationId = Guid.NewGuid();
        _factory.SetConnectionString(_dbFixture.ConnectionString);
        _factory.SetAuthOptions(_organizationId, "Admin");
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateSubscription_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateSubscriptionRequest
        {
            OrganizationId = _organizationId,
            PlanId = "pro",
            BillingCycle = "Monthly"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var subscription = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        subscription.Should().NotBeNull();
        subscription!.OrganizationId.Should().Be(_organizationId);
        subscription.PlanId.Should().Be("pro");
        subscription.Status.Should().Be("Active");
        subscription.BillingCycle.Should().Be("Monthly");
        subscription.StartDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetSubscription_WhenExists_Returns200Ok()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            PlanId = "enterprise",
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Yearly",
            CreatedAt = DateTime.UtcNow,
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SubscriptionDto>();
        result.Should().NotBeNull();
        result!.PlanId.Should().Be("enterprise");
        result.BillingCycle.Should().Be("Yearly");
    }

    [Fact]
    public async Task GetSubscription_WhenNotExists_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/subscriptions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelSubscription_WhenExists_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            PlanId = "pro",
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            CreatedAt = DateTime.UtcNow,
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var request = new CancelSubscriptionRequest { Reason = "User requested cancellation" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify subscription was cancelled in database
        await using var verifyContext = _dbFixture.CreateDbContext();
        var cancelledSubscription = await verifyContext.Subscriptions
            .FirstOrDefaultAsync(s => s.OrganizationId == _organizationId);
        cancelledSubscription.Should().NotBeNull();
        cancelledSubscription!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancelSubscription_WhenNotExists_Returns404NotFound()
    {
        // Arrange
        var request = new CancelSubscriptionRequest { Reason = "User requested cancellation" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpgradeSubscription_WhenExists_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            PlanId = "pro",
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            CreatedAt = DateTime.UtcNow,
        };
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var request = new UpgradeSubscriptionRequest { NewPlanId = "enterprise" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions/upgrade", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateSubscription_WithoutAdminRole_Returns403Forbidden()
    {
        // Arrange - create new client without Admin role
        var nonAdminFactory = new BillingApiFactory();
        nonAdminFactory.SetConnectionString(_dbFixture.ConnectionString);
        nonAdminFactory.SetAuthOptions(_organizationId, "User"); // No Admin role
        var nonAdminClient = nonAdminFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var request = new CreateSubscriptionRequest
        {
            OrganizationId = _organizationId,
            PlanId = "pro",
            BillingCycle = "Monthly"
        };

        // Act
        var response = await nonAdminClient.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateSubscription_WithDuplicateOrganization_Returns400BadRequest()
    {
        // Arrange - Create existing subscription
        await using var context = _dbFixture.CreateDbContext();
        var existingSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            PlanId = "pro",
            Status = "Active",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            CreatedAt = DateTime.UtcNow,
        };
        context.Subscriptions.Add(existingSubscription);
        await context.SaveChangesAsync();

        var request = new CreateSubscriptionRequest
        {
            OrganizationId = _organizationId,
            PlanId = "enterprise",
            BillingCycle = "Monthly"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/subscriptions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

/// <summary>
/// Request to cancel a subscription.
/// </summary>
public class CancelSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the cancellation reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Request to upgrade a subscription.
/// </summary>
public class UpgradeSubscriptionRequest
{
    /// <summary>
    /// Gets or sets the new plan ID.
    /// </summary>
    public string NewPlanId { get; set; } = string.Empty;
}
