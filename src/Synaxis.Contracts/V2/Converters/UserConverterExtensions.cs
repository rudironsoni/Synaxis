// <copyright file="UserConverterExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Converters;

using Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Extension methods for user conversion.
/// </summary>
public static class UserConverterExtensions
{
    /// <summary>
    /// Converts a V1 UserDto to V2 UserDto.
    /// </summary>
    /// <param name="v1User">The V1 user DTO.</param>
    /// <returns>The V2 user DTO.</returns>
    public static UserDto ToV2(this V1.DTOs.UserDto v1User)
    {
        return new UserConverter().Convert(v1User);
    }

    /// <summary>
    /// Converts a collection of V1 UserDto to V2 UserDto.
    /// </summary>
    /// <param name="v1Users">The V1 user DTOs.</param>
    /// <returns>The V2 user DTOs.</returns>
    public static IEnumerable<UserDto> ToV2(this IEnumerable<V1.DTOs.UserDto> v1Users)
    {
        var converter = new UserConverter();
        return v1Users.Select(converter.Convert);
    }
}
