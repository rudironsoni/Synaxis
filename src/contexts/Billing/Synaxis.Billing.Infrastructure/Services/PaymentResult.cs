// <copyright file="PaymentResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Infrastructure.Services
{
    /// <summary>
    /// Result of a payment operation.
    /// </summary>
    public record PaymentResult(
        bool Success,
        string TransactionId,
        string Status,
        string? ErrorMessage,
        string? GatewayResponse);
}
