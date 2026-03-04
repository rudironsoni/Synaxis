// <copyright file="CreateTemplateCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Templates;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to create a new chat template.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="Name">The template name.</param>
/// <param name="Description">The template description.</param>
/// <param name="TemplateContent">The template content.</param>
/// <param name="Parameters">The template parameters.</param>
/// <param name="Category">The template category.</param>
/// <param name="IsSystemTemplate">Whether this is a system template.</param>
public record CreateTemplateCommand(
    Guid TenantId,
    string Name,
    string? Description,
    string TemplateContent,
    IReadOnlyList<TemplateParameterDto> Parameters,
    string Category,
    bool IsSystemTemplate = false)
    : IRequest<CreateTemplateResult>;

/// <summary>
/// Result of creating a chat template.
/// </summary>
/// <param name="TemplateId">The unique template identifier.</param>
/// <param name="Template">The created template DTO.</param>
public record CreateTemplateResult(Guid TemplateId, TemplateDto Template);

/// <summary>
/// Handler for the <see cref="CreateTemplateCommand"/>.
/// </summary>
public class CreateTemplateCommandHandler : IRequestHandler<CreateTemplateCommand, CreateTemplateResult>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public CreateTemplateCommandHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<CreateTemplateResult> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
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

        // Check for duplicate name
        bool nameExists = await _repository.NameExistsAsync(request.TenantId, request.Name, cancellationToken: cancellationToken);
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

        // Create the template
        var template = ChatTemplate.Create(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Description?.Trim(),
            request.TemplateContent.Trim(),
            parameters,
            request.Category,
            request.TenantId,
            request.IsSystemTemplate);

        // Persist the template
        await _repository.AddAsync(template, cancellationToken);

        // Map to DTO and return
        var templateDto = MapToDto(template);
        return new CreateTemplateResult(template.Id, templateDto);
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
