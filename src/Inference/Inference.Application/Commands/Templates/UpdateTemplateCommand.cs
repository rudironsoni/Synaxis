// <copyright file="UpdateTemplateCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Templates;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to update a chat template.
/// </summary>
/// <param name="TemplateId">The template identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="TemplateContent">The template content.</param>
/// <param name="Parameters">The template parameters.</param>
/// <param name="Category">The template category.</param>
public record UpdateTemplateCommand(
    Guid TemplateId,
    Guid TenantId,
    string Name,
    string? Description,
    string TemplateContent,
    IReadOnlyList<TemplateParameterDto> Parameters,
    string Category)
    : IRequest<UpdateTemplateResult>;

/// <summary>
/// Result of updating a chat template.
/// </summary>
/// <param name="Template">The updated template DTO.</param>
public record UpdateTemplateResult(TemplateDto Template);

/// <summary>
/// Handler for the <see cref="UpdateTemplateCommand"/>.
/// </summary>
public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand, UpdateTemplateResult>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public UpdateTemplateCommandHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<UpdateTemplateResult> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Template name is required.", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(request.TemplateContent))
        {
            throw new ArgumentException("Template content is required.", nameof(request.TemplateContent));
        }

        // Get existing template
        var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
        {
            throw new InvalidOperationException($"Template with ID '{request.TemplateId}' was not found.");
        }

        // Verify tenant ownership
        if (template.TenantId != request.TenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to update this template.");
        }

        // Check for duplicate name
        bool nameExists = await _repository.NameExistsAsync(
            request.TenantId,
            request.Name,
            request.TemplateId,
            cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"A template with the name '{request.Name}' already exists.");
        }

        // Map DTO parameters to domain parameters
        var parameters = request.Parameters.Select(p => new TemplateParameter
        {
            Name = p.Name,
            Description = p.Description,
            Type = p.Type,
            IsRequired = p.IsRequired,
            DefaultValue = p.DefaultValue,
            ValidationPattern = p.ValidationPattern,
            AllowedValues = p.AllowedValues.ToList(),
        }).ToList();

        // Update the template
        template.Update(
            request.Name.Trim(),
            request.Description?.Trim(),
            request.TemplateContent.Trim(),
            parameters,
            request.Category);

        // Persist changes
        await _repository.UpdateAsync(template, cancellationToken);

        // Map to DTO and return
        var templateDto = MapToDto(template);
        return new UpdateTemplateResult(templateDto);
    }

    private static TemplateDto MapToDto(ChatTemplate template)
    {
        var parameters = template.Parameters.Select(p => new TemplateParameterDto(
            p.Name,
            p.Description,
            p.Type,
            p.IsRequired,
            p.DefaultValue,
            p.ValidationPattern,
            p.AllowedValues.ToList().AsReadOnly())).ToList().AsReadOnly();

        return new TemplateDto(
            template.Id,
            template.Name,
            template.Description,
            template.TemplateContent,
            parameters,
            template.Category,
            template.TenantId,
            template.IsSystemTemplate,
            template.IsActive,
            template.UsageCount,
            template.CreatedAt,
            template.UpdatedAt);
    }
}
