// <copyright file="TemplateParameterDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Dtos;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Data transfer object for a template parameter.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Description">The parameter description.</param>
/// <param name="Type">The parameter type.</param>
/// <param name="IsRequired">Whether the parameter is required.</param>
/// <param name="DefaultValue">The default value.</param>
/// <param name="ValidationPattern">The validation regex pattern.</param>
/// <param name="AllowedValues">The allowed values for enum type.</param>
public record TemplateParameterDto(
    string Name,
    string? Description,
    ParameterType Type,
    bool IsRequired,
    string? DefaultValue,
    string? ValidationPattern,
    IReadOnlyList<string> AllowedValues);

/// <summary>
/// Data transfer object for a chat template.
/// </summary>
/// <param name="Id">The template identifier.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="TemplateContent">The template content.</param>
/// <param name="Parameters">The template parameters.</param>
/// <param name="Category">The template category.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="IsSystemTemplate">Whether this is a system template.</param>
/// <param name="IsActive">Whether the template is active.</param>
/// <param name="UsageCount">The usage count.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="UpdatedAt">The last updated timestamp.</param>
public record TemplateDto(
    Guid Id,
    string Name,
    string? Description,
    string TemplateContent,
    IReadOnlyList<TemplateParameterDto> Parameters,
    string Category,
    Guid TenantId,
    bool IsSystemTemplate,
    bool IsActive,
    int UsageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
