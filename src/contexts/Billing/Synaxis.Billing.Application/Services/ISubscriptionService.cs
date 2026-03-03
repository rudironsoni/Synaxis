// <copyright file="ISubscriptionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;

    /// <summary>
    /// Service interface for subscription operations.
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>
        /// Gets the active subscription for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The subscription DTO or null if not found.</returns>
        Task<SubscriptionDto?> GetSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new subscription for an organization.
        /// </summary>
        /// <param name="request">The subscription creation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The created subscription DTO.</returns>
        Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an organization's subscription.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="reason">The cancellation reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if cancelled; false if no active subscription found.</returns>
        Task<bool> CancelSubscriptionAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upgrades an organization's subscription to a new plan.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="newPlanId">The new plan ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if upgraded; false if no active subscription found.</returns>
        Task<bool> UpgradeSubscriptionAsync(Guid organizationId, string newPlanId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request to create a new subscription.
    /// </summary>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="PlanId">The plan ID.</param>
    /// <param name="BillingCycle">The billing cycle (Monthly or Yearly).</param>
    public record CreateSubscriptionRequest(
        Guid OrganizationId,
        string PlanId,
        string BillingCycle = "Monthly");

    /// <summary>
    /// Data transfer object for a subscription.
    /// </summary>
    /// <param name="Id">The subscription ID.</param>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="PlanId">The plan ID.</param>
    /// <param name="Status">The subscription status.</param>
    /// <param name="StartDate">The start date.</param>
    /// <param name="EndDate">Optional end date.</param>
    /// <param name="BillingCycle">The billing cycle.</param>
    public record SubscriptionDto(
        Guid Id,
        Guid OrganizationId,
        string PlanId,
        string Status,
        DateTime StartDate,
        DateTime? EndDate,
        string BillingCycle);
}
