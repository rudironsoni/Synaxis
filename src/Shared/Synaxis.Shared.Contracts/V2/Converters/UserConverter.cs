// <copyright file="UserConverter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Converters;

using Synaxis.Shared.Contracts.V2.DTOs;

/// <summary>
/// Default implementation of the user converter.
/// </summary>
public class UserConverter : IUserConverter
{
    /// <inheritdoc />
    public UserDto Convert(V1.DTOs.UserDto v1User)
    {
        ArgumentNullException.ThrowIfNull(v1User);

        return new UserDto
        {
            Id = v1User.Id,
            TenantId = null,
            Email = v1User.Email,
            DisplayName = v1User.DisplayName,
            Status = ConvertUserStatus(v1User.Status),
            IsAdmin = v1User.Roles.Contains("admin", StringComparer.OrdinalIgnoreCase),
            Metadata = null,
            CreatedAt = v1User.CreatedAt,
            UpdatedAt = v1User.UpdatedAt,
            LastActiveAt = v1User.LastActiveAt,
        };
    }

    private static Common.UserStatus ConvertUserStatus(V1.Common.UserStatus status)
    {
        return status switch
        {
            V1.Common.UserStatus.Pending => Common.UserStatus.PendingVerification,
            V1.Common.UserStatus.Active => Common.UserStatus.Active,
            V1.Common.UserStatus.Suspended => Common.UserStatus.Suspended,
            V1.Common.UserStatus.Deactivated => Common.UserStatus.Inactive,
            _ => Common.UserStatus.PendingVerification,
        };
    }
}
