using Billing.Application.DTOs;
using Billing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var invoices = await _invoiceService.GetOrganizationInvoicesAsync(organizationId, cancellationToken);
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetInvoiceAsync(id, cancellationToken);
        if (invoice == null)
            return NotFound();
        
        // Verify tenant access
        if (invoice.OrganizationId != GetOrganizationId())
            return Forbid();
        
        return Ok(invoice);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,BillingManager")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var invoiceRequest = request with { OrganizationId = organizationId };
        
        var invoice = await _invoiceService.GenerateInvoiceAsync(invoiceRequest, cancellationToken);
        return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,BillingManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveInvoice(Guid id, CancellationToken cancellationToken)
    {
        var result = await _invoiceService.ApproveInvoiceAsync(id, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = "Admin,BillingManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendInvoice(
        Guid id,
        [FromBody] SendInvoiceRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.SendInvoiceAsync(id, request?.EmailAddress, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,BillingManager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvoice(
        Guid id,
        [FromBody] CancelInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _invoiceService.CancelInvoiceAsync(id, request.Reason, cancellationToken);
        if (!result)
            return NotFound();
        return NoContent();
    }

    private Guid GetOrganizationId()
    {
        var claim = User.FindFirst("organization_id")?.Value;
        return claim != null ? Guid.Parse(claim) : Guid.Empty;
    }
}

public record SendInvoiceRequest(string? EmailAddress);
public record CancelInvoiceRequest(string Reason);
