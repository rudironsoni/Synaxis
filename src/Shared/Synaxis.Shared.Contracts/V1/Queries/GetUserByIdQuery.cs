// <copyright file="GetUserByIdQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Queries;

/// <summary>
/// Query to get a user by their identifier.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetUserByIdQuery), "get_user_by_id")]
public record GetUserByIdQuery : QueryBase
{
    /// <summary>
    /// Gets the identifier of the user to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetUserId")]
    public required Guid TargetUserId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; init; }
}
