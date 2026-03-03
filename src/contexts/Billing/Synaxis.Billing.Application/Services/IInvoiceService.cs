// <copyright file="IInvoiceService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services
{
    using Billing.Application.DTOs;

    /// <summary>
    /// Service interface for invoice operations.
    /// </summary>
    public interface IInvoiceService
    {
        /// <summary>
        /// Generates a new invoice for an organization.
        /// </summary>
        /// <param name="request">The invoice generation request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The generated invoice DTO.</returns>
        Task<InvoiceDto> GenerateInvoiceAsync(GenerateInvoiceRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an invoice by its ID.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The invoice DTO or null if not found.</returns>
        Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all invoices for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The list of invoice DTOs.</returns>
        Task<IReadOnlyList<InvoiceDto>> GetOrganizationInvoicesAsync(Guid organizationId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Approves an invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if approved; false if invoice not found.</returns>
        Task<bool> ApproveInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an invoice to the specified email address.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="emailAddress">Optional email address; uses organization's default if null.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if sent; false if invoice not found.</returns>
        Task<bool> SendInvoiceAsync(Guid invoiceId, string? emailAddress = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels an invoice.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="reason">The cancellation reason.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True if cancelled; false if invoice not found.</returns>
        Task<bool> CancelInvoiceAsync(Guid invoiceId, string reason, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request to generate a new invoice.
    /// </summary>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="FromDate">The start date for usage aggregation.</param>
    /// <param name="ToDate">The end date for usage aggregation.</param>
    /// <param name="Description">Optional invoice description.</param>
    public record GenerateInvoiceRequest(
        Guid OrganizationId,
        DateTime FromDate,
        DateTime ToDate,
        string? Description = null);

    /// <summary>
    /// Data transfer object for an invoice.
    /// </summary>
    /// <param name="Id">The invoice ID.</param>
    /// <param name="InvoiceNumber">The invoice number.</param>
    /// <param name="OrganizationId">The organization ID.</param>
    /// <param name="IssueDate">The issue date.</param>
    /// <param name="DueDate">The due date.</param>
    /// <param name="Amount">The total amount.</param>
    /// <param name="Currency">The currency code.</param>
    /// <param name="Status">The invoice status.</param>
    /// <param name="LineItems">The line items.</param>
    public record InvoiceDto(
        Guid Id,
        string InvoiceNumber,
        Guid OrganizationId,
        DateTime IssueDate,
        DateTime DueDate,
        decimal Amount,
        string Currency,
        string Status,
        IReadOnlyList<InvoiceLineItemDto> LineItems);

    /// <summary>
    /// Data transfer object for an invoice line item.
    /// </summary>
    /// <param name="Id">The line item ID.</param>
    /// <param name="Description">The description.</param>
    /// <param name="Quantity">The quantity.</param>
    /// <param name="UnitPrice">The unit price.</param>
    /// <param name="Amount">The total amount.</param>
    /// <param name="ResourceType">The resource type.</param>
    public record InvoiceLineItemDto(
        Guid Id,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal Amount,
        string? ResourceType);
}
