// <copyright file="SubscriptionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Repositories;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service implementation for subscription operations.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly ILogger<SubscriptionService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionService"/> class.
        /// </summary>
        /// <param name="subscriptionRepository">The subscription repository.</param>
        /// <param name="logger">The logger.</param>
        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository ?? throw new ArgumentNullException(nameof(subscriptionRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SubscriptionDto?> GetSubscriptionAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return subscription == null ? null : MapToDto(subscription);
        }

        /// <inheritdoc />
        public async Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.PlanId))
            {
                throw new ArgumentException("Plan ID is required", nameof(request));
            }

            _logger.LogInformation(
                "Creating subscription for organization {OrganizationId} with plan {PlanId}",
                request.OrganizationId,
                request.PlanId);

            // Check for existing active subscription
            var existing = await _subscriptionRepository.GetActiveByOrganizationAsync(request.OrganizationId, cancellationToken).ConfigureAwait(false);
            if (existing != null)
            {
                throw new InvalidOperationException("Organization already has an active subscription");
            }

            var startDate = DateTime.UtcNow;
            var endDate = request.BillingCycle.Equals("Yearly", StringComparison.OrdinalIgnoreCase)
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);

            var subscription = new Subscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                PlanId = request.PlanId,
                Status = "Active",
                StartDate = startDate,
                EndDate = endDate,
                BillingCycle = request.BillingCycle,
                CreatedAt = DateTime.UtcNow
            };

            await _subscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Created subscription {SubscriptionId} for organization {OrganizationId}",
                subscription.Id,
                request.OrganizationId);

            return MapToDto(subscription);
        }

        /// <inheritdoc />
        public async Task<bool> CancelSubscriptionAsync(Guid organizationId, string reason, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Cancellation reason is required", nameof(reason));
            }

            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (subscription == null)
            {
                return false;
            }

            subscription.Status = "Cancelled";
            subscription.EndDate = DateTime.UtcNow;
            subscription.CancelledAt = DateTime.UtcNow;

            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Cancelled subscription for organization {OrganizationId}: {Reason}",
                organizationId,
                reason);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> UpgradeSubscriptionAsync(Guid organizationId, string newPlanId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(newPlanId))
            {
                throw new ArgumentException("New plan ID is required", nameof(newPlanId));
            }

            var subscription = await _subscriptionRepository.GetActiveByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (subscription == null)
            {
                return false;
            }

            subscription.PlanId = newPlanId;
            await _subscriptionRepository.UpdateAsync(subscription, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Upgraded subscription for organization {OrganizationId} to plan {PlanId}",
                organizationId,
                newPlanId);

            return true;
        }

        private static SubscriptionDto MapToDto(Subscription subscription)
        {
            return new SubscriptionDto(
                subscription.Id,
                subscription.OrganizationId,
                subscription.PlanId,
                subscription.Status,
                subscription.StartDate,
                subscription.EndDate,
                subscription.BillingCycle);
        }
    }
}
