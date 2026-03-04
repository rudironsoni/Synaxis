// <copyright file="GetTemplateQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Queries;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Query to get a specific template by ID.
/// </summary>
/// <param name="TemplateId">The template identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
public record GetTemplateQuery(
    Guid TemplateId,
    Guid TenantId)
    : IRequest<TemplateDto?>;

/// <summary>
/// Handler for the <see cref="GetTemplateQuery"/>.
/// </summary>
public class GetTemplateQueryHandler : IRequestHandler<GetTemplateQuery, TemplateDto?>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTemplateQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public GetTemplateQueryHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<TemplateDto?> Handle(GetTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);

        if (template is null || template.TenantId != request.TenantId)
        {
            return null;
        }

        return MapToDto(template);
    }

    private static TemplateDto MapToDto(Synaxis.Inference.Domain.Aggregates.ChatTemplate template)
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
