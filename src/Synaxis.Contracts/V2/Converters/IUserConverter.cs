using Synaxis.Contracts.V2.DTOs;

namespace Synaxis.Contracts.V2.Converters;

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
            LastActiveAt = v1User.LastActiveAt
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
            _ => Common.UserStatus.PendingVerification
        };
    }
}

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
