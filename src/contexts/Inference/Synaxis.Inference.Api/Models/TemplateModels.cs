// <copyright file="PromptTemplate.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Models;

/// <summary>
/// Represents a prompt template.
/// </summary>
public class PromptTemplate
{
    /// <summary>
    /// Gets or sets the template ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of variable names.
    /// </summary>
    public List<string> Variables { get; set; } = new();

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user who created the template.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the template is public.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Request to create a template.
/// </summary>
public class CreateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of variable names.
    /// </summary>
    public List<string>? Variables { get; set; }
}

/// <summary>
/// Request to update a template.
/// </summary>
public class UpdateTemplateRequest
{
    /// <summary>
    /// Gets or sets the template name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the template content.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the list of variable names.
    /// </summary>
    public List<string>? Variables { get; set; }
}

/// <summary>
/// Request to share a template.
/// </summary>
public class ShareTemplateRequest
{
    /// <summary>
    /// Gets or sets the list of user IDs to share with.
    /// </summary>
    public List<string> UserIds { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to make the template public.
    /// </summary>
    public bool? MakePublic { get; set; }
}

/// <summary>
/// Request to unshare a template.
/// </summary>
public class UnshareTemplateRequest
{
    /// <summary>
    /// Gets or sets the list of user IDs to remove sharing from.
    /// </summary>
    public List<string> UserIds { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to make the template private.
    /// </summary>
    public bool? MakePrivate { get; set; }
}
