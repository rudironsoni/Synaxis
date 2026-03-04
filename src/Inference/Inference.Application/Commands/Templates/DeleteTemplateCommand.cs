// <copyright file="DeleteTemplateCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Templates;

using MediatR;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Command to delete a chat template.
/// </summary>
/// <param name="TemplateId">The template identifier.</param>
/// <param name="TenantId">The tenant identifier.</param>
public record DeleteTemplateCommand(
    Guid TemplateId,
    Guid TenantId)
    : IRequest<DeleteTemplateResult>;

/// <summary>
/// Result of deleting a chat template.
/// </summary>
/// <param name="Success">Whether the deletion was successful.</param>
public record DeleteTemplateResult(bool Success);

/// <summary>
/// Handler for the <see cref="DeleteTemplateCommand"/>.
/// </summary>
public class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand, DeleteTemplateResult>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public DeleteTemplateCommandHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<DeleteTemplateResult> Handle(DeleteTemplateCommand request, CancellationToken cancellationToken)
    {
        // Get existing template
        var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
        {
            throw new InvalidOperationException($"Template with ID '{request.TemplateId}' was not found.");
        }

        // Verify tenant ownership
        if (template.TenantId != request.TenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete this template.");
        }

        // Delete the template (domain will validate if system template)
        template.Delete();

        // Persist changes
        await _repository.UpdateAsync(template, cancellationToken);

        return new DeleteTemplateResult(true);
    }
}
