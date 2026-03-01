using Billing.Application.DTOs;
using Billing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var payments = await _paymentService.GetOrganizationPaymentsAsync(organizationId, cancellationToken);
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayment(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _paymentService.GetPaymentAsync(id, cancellationToken);
        if (payment == null)
            return NotFound();
        
        if (payment.OrganizationId != GetOrganizationId())
            return Forbid();
        
        return Ok(payment);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var paymentRequest = request with { OrganizationId = organizationId };
        
        var result = await _paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);
        
        if (!result.Success)
            return BadRequest(new { error = result.ErrorMessage });
        
        return Ok(result);
    }

    [HttpPost("{id:guid}/refund")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefundPayment(
        Guid id,
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.RefundPaymentAsync(id, request.Amount, cancellationToken);
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

public record RefundRequest(decimal Amount);
