// <copyright file="Subscription.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Domain.Entities;

/// <summary>
/// Represents a subscription for an organization.
/// </summary>
public class Subscription
{
    /// <summary>
    /// Gets or sets the unique identifier for the subscription.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this subscription.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the plan ID.
    /// </summary>
    public string PlanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the status of the subscription (Active, Cancelled, Suspended, Expired).
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Gets or sets the subscription start date.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the subscription end date, if applicable.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Gets or sets the billing cycle (Monthly, Yearly).
    /// </summary>
    public string BillingCycle { get; set; } = "Monthly";

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the cancellation timestamp.
    /// </summary>
    public DateTime? CancelledAt { get; set; }
}
