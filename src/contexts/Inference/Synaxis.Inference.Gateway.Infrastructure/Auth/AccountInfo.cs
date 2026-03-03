// <copyright file="AccountInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    /// <summary>
    /// Represents account information for listing purposes.
    /// </summary>
    /// <param name="Email">The account email.</param>
    /// <param name="IsActive">Whether the account is active.</param>
    public record AccountInfo(string Email, bool IsActive);
}