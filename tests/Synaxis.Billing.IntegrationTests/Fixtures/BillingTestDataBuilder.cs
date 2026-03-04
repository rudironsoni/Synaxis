// <copyright file="BillingTestDataBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Fixtures;

using global::Billing.Domain.Entities;
using global::Billing.Infrastructure.Data;
#nullable enable

/// <summary>
/// Builder for creating test data in Billing integration tests.
/// </summary>
public sealed class BillingTestDataBuilder
{
    private readonly BillingDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="BillingTestDataBuilder"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BillingTestDataBuilder(BillingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates a subscription for testing.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="planId">The plan ID.</param>
    /// <param name="status">The subscription status.</param>
    /// <returns>The created subscription.</returns>
    public Subscription CreateSubscription(
        Guid? organizationId = null,
        string planId = "pro",
        string status = "Active")
    {
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId ?? Guid.NewGuid(),
            PlanId = planId,
            Status = status,
            StartDate = DateTime.UtcNow,
            BillingCycle = "Monthly",
            CreatedAt = DateTime.UtcNow,
        };

        _context.Subscriptions.Add(subscription);
        return subscription;
    }

    /// <summary>
    /// Creates an invoice for testing.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="amount">The invoice amount.</param>
    /// <param name="status">The invoice status.</param>
    /// <param name="lineItems">Optional line items.</param>
    /// <returns>The created invoice.</returns>
    public Invoice CreateInvoice(
        Guid? organizationId = null,
        decimal amount = 100.00m,
        string status = "Pending",
        List<InvoiceLineItem>? lineItems = null)
    {
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            OrganizationId = organizationId ?? Guid.NewGuid(),
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = amount,
            Currency = "USD",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            LineItems = lineItems ?? new List<InvoiceLineItem>(),
        };

        if (lineItems == null || lineItems.Count == 0)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                Description = "Test line item",
                Quantity = 1,
                UnitPrice = amount,
                Amount = amount,
                ResourceType = "test",
            });
        }

        _context.Invoices.Add(invoice);
        return invoice;
    }

    /// <summary>
    /// Creates a payment for testing.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="invoiceId">Optional associated invoice ID.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="status">The payment status.</param>
    /// <returns>The created payment.</returns>
    public Payment CreatePayment(
        Guid? organizationId = null,
        Guid? invoiceId = null,
        decimal amount = 100.00m,
        string status = "Completed")
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId ?? Guid.NewGuid(),
            InvoiceId = invoiceId,
            TransactionId = $"txn_{Guid.NewGuid():N}",
            Amount = amount,
            Currency = "USD",
            Status = status,
            PaymentMethod = "card",
            CreatedAt = DateTime.UtcNow,
        };

        _context.Payments.Add(payment);
        return payment;
    }

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
