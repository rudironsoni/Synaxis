// <copyright file="InvoiceService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Repositories;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service implementation for invoice operations.
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IUsageRecordRepository _usageRepository;
        private readonly ILogger<InvoiceService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceService"/> class.
        /// </summary>
        /// <param name="invoiceRepository">The invoice repository.</param>
        /// <param name="usageRepository">The usage record repository.</param>
        /// <param name="logger">The logger.</param>
        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IUsageRecordRepository usageRepository,
            ILogger<InvoiceService> logger)
        {
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _usageRepository = usageRepository ?? throw new ArgumentNullException(nameof(usageRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "Generating invoice for organization {OrganizationId} from {FromDate} to {ToDate}",
                request.OrganizationId,
                request.FromDate,
                request.ToDate);

            // Get usage records for the period
            var usageRecords = await _usageRepository.GetByOrganizationAsync(
                request.OrganizationId,
                request.FromDate,
                request.ToDate,
                cancellationToken)
                .ConfigureAwait(false);

            // Create invoice number
            var invoiceNumber = await GenerateInvoiceNumberAsync(cancellationToken).ConfigureAwait(false);

            // Calculate total amount from line items
            var lineItems = new List<InvoiceLineItem>();
            decimal totalAmount = 0;

            foreach (var usage in usageRecords)
            {
                var unitPrice = GetUnitPrice(usage.ResourceType);
                var lineItemAmount = usage.Quantity * unitPrice;

                var lineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    Description = $"{usage.ResourceType} usage",
                    Quantity = usage.Quantity,
                    UnitPrice = unitPrice,
                    Amount = lineItemAmount,
                    ResourceType = usage.ResourceType,
                    ResourceId = usage.ResourceId
                };

                lineItems.Add(lineItem);
                totalAmount += lineItemAmount;
            }

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                InvoiceNumber = invoiceNumber,
                IssueDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // Due in 30 days
                Amount = totalAmount,
                Currency = "USD",
                Status = "Approved",
                Description = request.Description,
                LineItems = lineItems,
                CreatedAt = DateTime.UtcNow
            };

            await _invoiceRepository.AddAsync(invoice, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Generated invoice {InvoiceNumber} with {LineItemCount} line items",
                invoiceNumber,
                lineItems.Count);

            return MapToDto(invoice);
        }

        /// <inheritdoc />
        public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
            return invoice == null ? null : MapToDto(invoice);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<InvoiceDto>> GetOrganizationInvoicesAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var invoices = await _invoiceRepository.GetByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return invoices.Select(MapToDto).ToList();
        }

        /// <inheritdoc />
        public async Task<bool> ApproveInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
            if (invoice == null)
            {
                return false;
            }

            if (invoice.Status == "Pending")
            {
                invoice.Status = "Approved";
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

                _logger.LogInformation("Approved invoice {InvoiceId}", invoiceId);
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> SendInvoiceAsync(Guid invoiceId, string? emailAddress = null, CancellationToken cancellationToken = default)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
            if (invoice == null)
            {
                return false;
            }

            // In a real implementation, this would send an email
            // For now, just log and update status if pending
            if (invoice.Status == "Approved")
            {
                invoice.Status = "Sent";
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Sent invoice {InvoiceId} to {EmailAddress}",
                invoiceId,
                emailAddress ?? "default");

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException("Cancellation reason is required", nameof(reason));
            }

            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken).ConfigureAwait(false);
            if (invoice == null)
            {
                return false;
            }

            invoice.Status = "Cancelled";
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Cancelled invoice {InvoiceId}: {Reason}", invoiceId, reason);
            return true;
        }

        private static Task<string> GenerateInvoiceNumberAsync(CancellationToken cancellationToken)
        {
            var prefix = "INV";
            var date = DateTime.UtcNow.ToString("yyyyMM");
            var number = Random.Shared.Next(1000, 9999);
            var invoiceNumber = $"{prefix}-{date}-{number}";
            return Task.FromResult(invoiceNumber);
        }

        private static decimal GetUnitPrice(string resourceType)
        {
            return resourceType switch
            {
                "API" => 0.01m,      // $0.01 per API call
                "Compute" => 0.50m,  // $0.50 per hour
                "Storage" => 0.02m,  // $0.02 per GB
                _ => 0.01m
            };
        }

        private static InvoiceDto MapToDto(Invoice invoice)
        {
            return new InvoiceDto(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.OrganizationId,
                invoice.IssueDate,
                invoice.DueDate,
                invoice.Amount,
                invoice.Currency,
                invoice.Status,
                invoice.LineItems.Select(li => new InvoiceLineItemDto(
                    li.Id,
                    li.Description,
                    li.Quantity,
                    li.UnitPrice,
                    li.Amount,
                    li.ResourceType)).ToList());
        }
    }
}
