// <copyright file="ChatDelta.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ChatDelta
{
    public string? Role { get; init; }
    public string? Content { get; init; }
}
