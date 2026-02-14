// <copyright file="ErrorResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Samples.SaaSClient;

public record ErrorResponse
{
    public ErrorDetail? Error { get; init; }
}
