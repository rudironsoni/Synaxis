// <copyright file="PaymentService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;
    using Billing.Domain.Entities;
    using Billing.Infrastructure.Repositories;
    using Billing.Infrastructure.Services;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Service implementation for payment operations.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<PaymentService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentService"/> class.
        /// </summary>
        /// <param name="paymentRepository">The payment repository.</param>
        /// <param name="invoiceRepository">The invoice repository.</param>
        /// <param name="paymentGateway">The payment gateway.</param>
        /// <param name="logger">The logger.</param>
        public PaymentService(
            IPaymentRepository paymentRepository,
            IInvoiceRepository invoiceRepository,
            IPaymentGateway paymentGateway,
            ILogger<PaymentService> logger)
        {
            _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _paymentGateway = paymentGateway ?? throw new ArgumentNullException(nameof(paymentGateway));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "Processing payment of {Amount} {Currency} for organization {OrganizationId}",
                request.Amount,
                request.Currency,
                request.OrganizationId);

            // Validate invoice if provided
            if (request.InvoiceId.HasValue)
            {
                var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId.Value, cancellationToken).ConfigureAwait(false);
                if (invoice == null)
                {
                    return new PaymentResultDto(false, null, string.Empty, "Failed", "Invoice not found");
                }
            }

            // Create payment record
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                InvoiceId = request.InvoiceId,
                Amount = request.Amount,
                Currency = request.Currency,
                PaymentMethod = request.PaymentMethod,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment, cancellationToken).ConfigureAwait(false);

            // Process through gateway
            var gatewayRequest = new PaymentRequest(
                request.Amount,
                request.Currency,
                request.PaymentMethod,
                request.PaymentToken,
                $"Payment for organization {request.OrganizationId}",
                request.OrganizationId.ToString());

            var gatewayResult = await _paymentGateway.ProcessPaymentAsync(gatewayRequest, cancellationToken).ConfigureAwait(false);

            // Update payment record
            payment.TransactionId = gatewayResult.TransactionId;
            payment.Status = gatewayResult.Success ? "Completed" : "Failed";
            payment.GatewayResponse = gatewayResult.GatewayResponse;
            payment.CompletedAt = gatewayResult.Success ? DateTime.UtcNow : null;

            await _paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);

            // If successful and has invoice, record payment on invoice
            if (gatewayResult.Success && request.InvoiceId.HasValue)
            {
                var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId.Value, cancellationToken).ConfigureAwait(false);
                if (invoice != null)
                {
                    invoice.Status = "Paid";
                    invoice.PaidAt = DateTime.UtcNow;
                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation(
                "Payment {PaymentId} processed with status {Status}",
                payment.Id,
                payment.Status);

            return new PaymentResultDto(
                gatewayResult.Success,
                payment.Id,
                gatewayResult.TransactionId,
                payment.Status,
                gatewayResult.ErrorMessage);
        }

        /// <inheritdoc />
        public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
            return payment == null ? null : MapToDto(payment);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<PaymentDto>> GetOrganizationPaymentsAsync(Guid organizationId, CancellationToken cancellationToken = default)
        {
            var payments = await _paymentRepository.GetByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            return payments.Select(MapToDto).ToList();
        }

        /// <inheritdoc />
        public async Task<bool> RefundPaymentAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken = default)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Refund amount must be greater than zero", nameof(amount));
            }

            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken).ConfigureAwait(false);
            if (payment == null || payment.Status != "Completed")
            {
                return false;
            }

            var result = await _paymentGateway.RefundPaymentAsync(payment.TransactionId, amount, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                payment.Status = "Refunded";
                await _paymentRepository.UpdateAsync(payment, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Refund processed for payment {PaymentId}: {Success}",
                paymentId,
                result.Success);

            return result.Success;
        }

        private static PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto(
                payment.Id,
                payment.OrganizationId,
                payment.InvoiceId,
                payment.TransactionId,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.PaymentMethod,
                payment.CreatedAt);
        }
    }
}
