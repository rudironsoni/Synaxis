// <copyright file="PaymentRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Services
{
    /// <summary>
    /// Request to process a payment.
    /// </summary>
    public record PaymentRequest(
        decimal Amount,
        string Currency,
        string PaymentMethod,
        string? Token,
        string Description,
        string? CustomerId);
}
