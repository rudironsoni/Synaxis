// <copyright file="GetTemplatesQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Queries;

using MediatR;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Query to get all templates for a tenant.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="IncludeInactive">Whether to include inactive templates.</param>
/// <param name="Category">Optional category filter.</param>
public record GetTemplatesQuery(
    Guid TenantId,
    bool IncludeInactive = false,
    string? Category = null)
    : IRequest<IReadOnlyList<TemplateDto>>;

/// <summary>
/// Handler for the <see cref="GetTemplatesQuery"/>.
/// </summary>
public class GetTemplatesQueryHandler : IRequestHandler<GetTemplatesQuery, IReadOnlyList<TemplateDto>>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTemplatesQueryHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public GetTemplatesQueryHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.GetByTenantAsync(
            request.TenantId,
            request.IncludeInactive,
            cancellationToken);

        if (!string.IsNullOrEmpty(request.Category))
        {
            templates = templates.Where(t => t.Category == request.Category).ToList();
        }

        return templates.Select(MapToDto).ToList().AsReadOnly();
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
