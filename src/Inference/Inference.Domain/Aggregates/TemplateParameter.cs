// <copyright file="TemplateParameter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents a template parameter.
/// </summary>
public class TemplateParameter
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public ParameterType Type { get; set; } = ParameterType.String;

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the validation regex pattern.
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the allowed values for enum type.
    /// </summary>
    public IList<string> AllowedValues { get; set; } = new List<string>();
}
