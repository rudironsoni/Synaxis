// <copyright file="IJwtService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Security
{
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Provides JWT token generation services.
    /// </summary>
    public interface IJwtService
    {
        /// <summary>
        /// Generates a JWT token for a user.
        /// </summary>
        /// <param name="user">The user to generate a token for.</param>
        /// <returns>The generated JWT token.</returns>
        string GenerateToken(User user);
    }
}