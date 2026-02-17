// <copyright file="ParameterType.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents parameter types.
/// </summary>
public enum ParameterType
{
    /// <summary>
    /// String parameter.
    /// </summary>
    String,

    /// <summary>
    /// Integer parameter.
    /// </summary>
    Integer,

    /// <summary>
    /// Decimal parameter.
    /// </summary>
    Decimal,

    /// <summary>
    /// Boolean parameter.
    /// </summary>
    Boolean,

    /// <summary>
    /// Enum parameter with allowed values.
    /// </summary>
    Enum,

    /// <summary>
    /// DateTime parameter.
    /// </summary>
    DateTime,

    /// <summary>
    /// JSON parameter.
    /// </summary>
    Json,
}
