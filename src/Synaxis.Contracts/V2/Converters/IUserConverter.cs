// <copyright file="IUserConverter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Converters;

using Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Converts V1 UserDto to V2 UserDto.
/// </summary>
public interface IUserConverter
{
    /// <summary>
    /// Converts a V1 UserDto to V2 UserDto.
    /// </summary>
    /// <param name="v1User">The V1 user DTO.</param>
    /// <returns>The V2 user DTO.</returns>
    UserDto Convert(V1.DTOs.UserDto v1User);
}
