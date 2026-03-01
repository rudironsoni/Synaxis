// <copyright file="IntegrityVerificationResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers;

#nullable enable

using System;

/// <summary>
/// Result of an integrity verification operation.
/// </summary>
/// <param name="LogId">The audit log identifier.</param>
/// <param name="IsValid">Whether the audit log integrity is valid.</param>
/// <param name="VerifiedAt">When the verification was performed.</param>
/// <param name="Message">A descriptive message about the verification result.</param>
public sealed record IntegrityVerificationResult(
    Guid LogId,
    bool IsValid,
    DateTime VerifiedAt,
    string Message);
