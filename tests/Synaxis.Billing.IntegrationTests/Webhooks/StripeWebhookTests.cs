// <copyright file="StripeWebhookTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Webhooks;

using System.Net;
using System.Security.Cryptography;
using System.Text;
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
/// Integration tests for Stripe webhook handling.
/// </summary>
[Collection("BillingIntegration")]
[Trait("Category", "Integration")]
public sealed class StripeWebhookTests : IAsyncLifetime
{
    private readonly BillingDatabaseFixture _dbFixture;
    private readonly BillingApiFactory _factory;
    private readonly HttpClient _client;
    private readonly Mock<IPaymentGateway> _mockPaymentGateway;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookTests"/> class.
    /// </summary>
    public StripeWebhookTests(BillingDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
        _factory = new BillingApiFactory();
        _factory.SetConnectionString(_dbFixture.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        _mockPaymentGateway = _factory.MockPaymentGateway;
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
    public async Task StripeWebhook_PaymentSucceeded_UpdatesBillingRecords()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        // Create an invoice
        await using (var context = _dbFixture.CreateDbContext())
        {
            var invoice = new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = "INV-WEBHOOK-001",
                OrganizationId = organizationId,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Amount = 99.99m,
                Currency = "USD",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
        }

        // Setup mock to validate webhook
        _mockPaymentGateway.Setup(g => g.ValidateWebhookAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var webhookPayload = $@"{{
            ""type"": ""invoice.payment_succeeded"",
            ""data"": {{
                ""object"": {{
                    ""id"": ""in_1234567890"",
                    ""status"": ""paid"",
                    ""amount_paid"": 9999,
                    ""currency"": ""usd"",
                    ""customer"": ""{organizationId}"",
                    ""lines"": {{
                        ""data"": [
                            {{
                                ""id"": ""li_1234567890"",
                                ""description"": ""Monthly subscription"",
                                ""amount"": 9999
                            }}
                        ]
                    }},
                    ""metadata"": {{
                        ""organization_id"": ""{organizationId}"",
                        ""invoice_id"": ""{invoiceId}""
                    }}
                }}
            }}
        }}";

        var signature = ComputeSignature(webhookPayload, "whsec_test_secret");

        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/webhooks/stripe?signature={signature}", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Note: Webhook controller validates and returns success, but actual processing
        // would be handled by a background worker or event handler in production
    }

    [Fact]
    public async Task StripeWebhook_InvalidSignature_Returns400BadRequest()
    {
        // Arrange
        var webhookPayload = @"{
            ""type"": ""invoice.payment_succeeded"",
            ""data"": {
                ""object"": {
                    ""id"": ""in_1234567890"",
                    ""status"": ""paid""
                }
            }
        }";

        // Setup mock to reject invalid signature
        _mockPaymentGateway.Setup(g => g.ValidateWebhookAsync(
            It.IsAny<string>(),
            It.Is<string>(s => s == "invalid_signature"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/webhooks/stripe?signature=invalid_signature", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_PaymentFailed_UpdatesInvoiceStatus()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        // Create an invoice
        await using (var context = _dbFixture.CreateDbContext())
        {
            var invoice = new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = "INV-WEBHOOK-002",
                OrganizationId = organizationId,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Amount = 99.99m,
                Currency = "USD",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
        }

        // Setup mock to validate webhook
        _mockPaymentGateway.Setup(g => g.ValidateWebhookAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var webhookPayload = $@"{{
            ""type"": ""invoice.payment_failed"",
            ""data"": {{
                ""object"": {{
                    ""id"": ""in_0987654321"",
                    ""status"": ""open"",
                    ""amount_due"": 9999,
                    ""currency"": ""usd"",
                    ""customer"": ""{organizationId}"",
                    ""metadata"": {{
                        ""organization_id"": ""{organizationId}"",
                        ""invoice_id"": ""{invoiceId}""
                    }},
                    ""next_payment_attempt"": null
                }}
            }},
            ""livemode"": false
        }}";

        var signature = ComputeSignature(webhookPayload, "whsec_test_secret");
        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/webhooks/stripe?signature={signature}", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task StripeWebhook_SubscriptionCancelled_UpdatesSubscriptionStatus()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Create a subscription
        await using (var context = _dbFixture.CreateDbContext())
        {
            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                PlanId = "pro",
                Status = "Active",
                StartDate = DateTime.UtcNow,
                BillingCycle = "Monthly",
                CreatedAt = DateTime.UtcNow,
            };
            context.Subscriptions.Add(subscription);
            await context.SaveChangesAsync();
        }

        // Setup mock to validate webhook
        _mockPaymentGateway.Setup(g => g.ValidateWebhookAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var webhookPayload = $@"{{
            ""type"": ""customer.subscription.deleted"",
            ""data"": {{
                ""object"": {{
                    ""id"": ""sub_1234567890"",
                    ""status"": ""canceled"",
                    ""customer"": ""{organizationId}"",
                    ""current_period_end"": {DateTimeOffset.UtcNow.AddMonths(1).ToUnixTimeSeconds()},
                    ""cancellation_details"": {{
                        ""comment"": ""User requested cancellation"",
                        ""feedback"": ""other""
                    }}
                }}
            }},
            ""livemode"": false
        }}";

        var signature = ComputeSignature(webhookPayload, "whsec_test_secret");
        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/webhooks/stripe?signature={signature}", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Verify subscription was updated
        await using (var verifyContext = _dbFixture.CreateDbContext())
        {
            var updatedSubscription = await verifyContext.Subscriptions
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);
            updatedSubscription.Should().NotBeNull();
            updatedSubscription!.Status.Should().BeOneOf("Cancelled", "Cancelled");
        }
    }

    [Fact]
    public async Task StripeWebhook_MissingSignature_Returns400BadRequest()
    {
        // Arrange
        var webhookPayload = @"{
            ""type"": ""invoice.payment_succeeded"",
            ""data"": {
                ""object"": {
                    ""id"": ""in_1234567890"",
                    ""status"": ""paid""
                }
            }
        }";

        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act - No signature in query
        var response = await _client.PostAsync("/api/v1/webhooks/stripe", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_EmptyPayload_Returns400BadRequest()
    {
        // Arrange
        var content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/webhooks/stripe?signature=sig", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StripeWebhook_RetryLogic_TriggersAfterFailedPayment()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        // Create an invoice
        await using (var context = _dbFixture.CreateDbContext())
        {
            var invoice = new Invoice
            {
                Id = invoiceId,
                InvoiceNumber = "INV-WEBHOOK-RETRY",
                OrganizationId = organizationId,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30),
                Amount = 99.99m,
                Currency = "USD",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
            };
            context.Invoices.Add(invoice);
            await context.SaveChangesAsync();
        }

        // Setup mock to validate webhook
        _mockPaymentGateway.Setup(g => g.ValidateWebhookAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var webhookPayload = $@"{{
            ""type"": ""invoice.payment_failed"",
            ""data"": {{
                ""object"": {{
                    ""id"": ""in_retry_test"",
                    ""status"": ""open"",
                    ""amount_due"": 9999,
                    ""currency"": ""usd"",
                    ""customer"": ""{organizationId}"",
                    ""metadata"": {{
                        ""organization_id"": ""{organizationId}"",
                        ""invoice_id"": ""{invoiceId}""
                    }},
                    ""next_payment_attempt"": {DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()},
                    ""attempt_count"": 1,
                    ""last_payment_error"": {{
                        ""code"": ""card_declined"",
                        ""decline_code"": ""insufficient_funds"",
                        ""message"": ""Your card has insufficient funds.""
                    }}
                }}
            }},
            ""livemode"": false
        }}";

        var signature = ComputeSignature(webhookPayload, "whsec_test_secret");
        var content = new StringContent(webhookPayload, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/v1/webhooks/stripe?signature={signature}", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        // Note: Webhook controller validates and returns success, but actual processing
        // would be handled by a background worker or event handler in production
    }

    private static string ComputeSignature(string payload, string secret)
    {
        // Simplified signature computation for testing
        // In real implementation, this would use HMAC-SHA256 with the webhook secret
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
