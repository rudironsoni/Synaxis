// <copyright file="TemplatesController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Controllers;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Synaxis.Inference.Api.Models;

/// <summary>
/// Controller for prompt templates management.
/// </summary>
[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> logger;
    private static readonly Dictionary<string, PromptTemplate> Templates = new();
    private static readonly Dictionary<string, HashSet<string>> SharedWith = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TemplatesController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TemplatesController(ILogger<TemplatesController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Lists all templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of templates.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<PromptTemplate>), StatusCodes.Status200OK)]
    public Task<IActionResult> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Listing all templates");
        var tenantId = this.GetTenantContext();

        var templates = Templates.Values
            .Where(t => t.TenantId == tenantId || t.IsPublic)
            .ToList();

        return Task.FromResult<IActionResult>(this.Ok(templates));
    }

    /// <summary>
    /// Gets a specific template.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The template.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetTemplateAsync(
        [FromRoute, Required] string id,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Getting template {TemplateId}", id);

        if (!Templates.TryGetValue(id, out var template))
        {
            return Task.FromResult<IActionResult>(this.NotFound());
        }

        return Task.FromResult<IActionResult>(this.Ok(template));
    }

    /// <summary>
    /// Creates a new template.
    /// </summary>
    /// <param name="request">The template creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created template.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> CreateTemplateAsync(
        [FromBody] CreateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Creating template {TemplateName}", request.Name);

        var tenantId = this.GetTenantContext();
        var userId = this.GetCurrentUserId();

        var id = Guid.NewGuid().ToString("N");
        var template = new PromptTemplate
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            Variables = request.Variables ?? new List<string>(),
            TenantId = tenantId,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        Templates[id] = template;
        SharedWith[id] = new HashSet<string>();

        return Task.FromResult<IActionResult>(this.Created($"/api/templates/{id}", template));
    }

    /// <summary>
    /// Updates a template.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="request">The template update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> UpdateTemplateAsync(
        [FromRoute, Required] string id,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Updating template {TemplateId}", id);

        if (!Templates.TryGetValue(id, out var existing))
        {
            return Task.FromResult<IActionResult>(this.NotFound());
        }

        if (!this.CanModifyTemplate(existing))
        {
            return Task.FromResult<IActionResult>(this.Forbid());
        }

        existing.Name = request.Name ?? existing.Name;
        existing.Description = request.Description ?? existing.Description;
        existing.Content = request.Content ?? existing.Content;
        existing.Variables = request.Variables ?? existing.Variables;
        existing.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<IActionResult>(this.Ok(existing));
    }

    /// <summary>
    /// Deletes a template.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> DeleteTemplateAsync(
        [FromRoute, Required] string id,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Deleting template {TemplateId}", id);

        if (!Templates.TryGetValue(id, out var template))
        {
            return Task.FromResult<IActionResult>(this.NotFound());
        }

        if (!this.CanModifyTemplate(template))
        {
            return Task.FromResult<IActionResult>(this.Forbid());
        }

        Templates.Remove(id);
        SharedWith.Remove(id);
        return Task.FromResult<IActionResult>(this.NoContent());
    }

    /// <summary>
    /// Shares a template with other users.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="request">The share request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    [HttpPost("{id}/share")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> ShareTemplateAsync(
        [FromRoute, Required] string id,
        [FromBody] ShareTemplateRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Sharing template {TemplateId}", id);

        if (!Templates.TryGetValue(id, out var template))
        {
            return Task.FromResult<IActionResult>(this.NotFound());
        }

        if (!this.CanModifyTemplate(template))
        {
            return Task.FromResult<IActionResult>(this.Forbid());
        }

        if (!SharedWith.TryGetValue(id, out var sharedUsers))
        {
            sharedUsers = new HashSet<string>();
            SharedWith[id] = sharedUsers;
        }

        foreach (var userId in request.UserIds)
        {
            sharedUsers.Add(userId);
        }

        template.IsPublic = request.MakePublic ?? template.IsPublic;
        template.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<IActionResult>(this.Ok(template));
    }

    /// <summary>
    /// Unshares a template from specific users.
    /// </summary>
    /// <param name="id">The template ID.</param>
    /// <param name="request">The unshare request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated template.</returns>
    [HttpPost("{id}/unshare")]
    [ProducesResponseType(typeof(PromptTemplate), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> UnshareTemplateAsync(
        [FromRoute, Required] string id,
        [FromBody] UnshareTemplateRequest request,
        CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Unsharing template {TemplateId}", id);

        if (!Templates.TryGetValue(id, out var template))
        {
            return Task.FromResult<IActionResult>(this.NotFound());
        }

        if (!this.CanModifyTemplate(template))
        {
            return Task.FromResult<IActionResult>(this.Forbid());
        }

        if (SharedWith.TryGetValue(id, out var sharedUsers))
        {
            foreach (var userId in request.UserIds)
            {
                sharedUsers.Remove(userId);
            }
        }

        if (request.MakePrivate == true)
        {
            template.IsPublic = false;
        }

        template.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult<IActionResult>(this.Ok(template));
    }

    private string? GetTenantContext()
    {
        return this.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId)
            ? tenantId.ToString()
            : this.User?.FindFirst("tenant_id")?.Value;
    }

    private string GetCurrentUserId()
    {
        return this.User?.Identity?.Name ?? "anonymous";
    }

    private bool CanModifyTemplate(PromptTemplate template)
    {
        var currentUserId = this.GetCurrentUserId();
        return template.CreatedBy == currentUserId;
    }
}
