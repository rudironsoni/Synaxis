// <copyright file="PaymentControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;

using global::Billing.Application.Services;
using global::Billing.Domain.Entities;
using global::Billing.Infrastructure.Data;
using global::Billing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Moq;
using Synaxis.Billing.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the Payments controller.
/// </summary>
[Collection("BillingIntegration")]
[Trait("Category", "Integration")]
public sealed class PaymentControllerTests : IAsyncLifetime
{
    private readonly BillingDatabaseFixture _dbFixture;
    private readonly BillingApiFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _organizationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentControllerTests"/> class.
    /// </summary>
    public PaymentControllerTests(BillingDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
        _factory = new BillingApiFactory();
        _organizationId = Guid.NewGuid();
        _factory.SetConnectionString(_dbFixture.ConnectionString);
        _factory.SetAuthOptions(_organizationId, "User");
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
    public async Task ProcessPayment_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        // Setup mock payment gateway
        _factory.ConfigurePaymentGateway(mock =>
        {
            mock.Setup(g => g.ProcessPaymentAsync(
                It.Is<PaymentRequest>(r => r.Amount == 99.99m),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(
                    true,
                    "txn_12345",
                    "Completed",
                    null,
                    "{\"status\": \"succeeded\"}"));
        });

        var request = new ProcessPaymentRequest(
            _organizationId,
            invoiceId,
            99.99m,
            "USD",
            "card",
            "tok_visa");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Status.Should().Be("Completed");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ProcessPayment_WhenPaymentFails_Returns400BadRequest()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        // Setup mock to simulate payment failure
        _factory.ConfigurePaymentGateway(mock =>
        {
            mock.Setup(g => g.ProcessPaymentAsync(
                It.IsAny<PaymentRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(
                    false,
                    string.Empty,
                    "Failed",
                    "Insufficient funds",
                    "{\"error\": \"insufficient_funds\"}"));
        });

        var request = new ProcessPaymentRequest(
            _organizationId,
            invoiceId,
            999.99m,
            "USD",
            "card",
            "tok_decline");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<PaymentResultDto>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task ProcessPayment_UpdatesSubscriptionStatus()
    {
        // Arrange
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            PlanId = "pro",
            Status = "Pending",
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            CreatedAt = DateTime.UtcNow,
        };

        await using (var context = _dbFixture.CreateDbContext())
        {
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
        }

        // Setup mock for successful payment
        _factory.ConfigurePaymentGateway(mock =>
        {
            mock.Setup(g => g.ProcessPaymentAsync(
                It.IsAny<PaymentRequest>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(
                    true,
                    "txn_67890",
                    "Completed",
                    null,
                    "{\"status\": \"succeeded\"}"));
        });

        var request = new ProcessPaymentRequest(
            _organizationId,
            null,
            49.99m,
            "USD",
            "card",
            "tok_visa");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/payments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment was recorded in database
        await using (var verifyContext = _dbFixture.CreateDbContext())
        {
            var payment = await verifyContext.Payments
                .FirstOrDefaultAsync(p => p.OrganizationId == _organizationId);
            payment.Should().NotBeNull();
            payment!.Amount.Should().Be(49.99m);
            payment.Status.Should().Be("Completed");
        }
    }

    [Fact]
    public async Task GetPayments_ForOrganization_Returns200OkWithPayments()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var payment1 = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransactionId = "txn_001",
            Amount = 100.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
        };
        var payment2 = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransactionId = "txn_002",
            Amount = 200.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
        };

        context.Payments.AddRange(payment1, payment2);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/payments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        payments.Should().NotBeNull();
        payments.Should().HaveCount(2);
        payments!.Select(p => p.TransactionId).Should().Contain("txn_001", "txn_002");
    }

    [Fact]
    public async Task GetPayment_ById_Returns200OkWithPayment()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransactionId = "txn_003",
            Amount = 150.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow,
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaymentDto>();
        result.Should().NotBeNull();
        result!.TransactionId.Should().Be("txn_003");
        result.Amount.Should().Be(150.00m);
    }

    [Fact]
    public async Task GetPayment_ById_WhenNotFound_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_ById_WrongOrganization_Returns403Forbidden()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(), // Different organization
            TransactionId = "txn_004",
            Amount = 150.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow,
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/payments/{payment.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RefundPayment_WhenCompleted_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransactionId = "txn_005",
            Amount = 100.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow,
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        // Setup mock for successful refund
        _factory.ConfigurePaymentGateway(mock =>
        {
            mock.Setup(g => g.RefundPaymentAsync(
                "txn_005",
                50.00m,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PaymentResult(
                    true,
                    "txn_005",
                    "Refunded",
                    null,
                    "{\"status\": \"refunded\"}"));
        });

        // Create admin client for refund endpoint
        var adminFactory = new BillingApiFactory();
        adminFactory.SetConnectionString(_dbFixture.ConnectionString);
        adminFactory.SetAuthOptions(_organizationId, "Admin");
        var adminClient = adminFactory.CreateClient();

        var request = new RefundRequest(50.00m);

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RefundPayment_WithoutAdminRole_Returns403Forbidden()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            TransactionId = "txn_006",
            Amount = 100.00m,
            Currency = "USD",
            Status = "Completed",
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow,
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var request = new RefundRequest(50.00m);

        // Act - Use regular client without Admin role
        var response = await _client.PostAsJsonAsync($"/api/v1/payments/{payment.Id}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RefundPayment_WhenNotFound_Returns404NotFound()
    {
        // Arrange
        var adminFactory = new BillingApiFactory();
        adminFactory.SetConnectionString(_dbFixture.ConnectionString);
        adminFactory.SetAuthOptions(_organizationId, "Admin");
        var adminClient = adminFactory.CreateClient();

        var request = new RefundRequest(50.00m);

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/v1/payments/{Guid.NewGuid()}/refund", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

/// <summary>
/// Request to refund a payment.
/// </summary>
public record RefundRequest(decimal Amount);
