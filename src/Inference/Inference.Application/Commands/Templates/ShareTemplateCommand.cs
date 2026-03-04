// <copyright file="ShareTemplateCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands.Templates;

using MediatR;
using Synaxis.Inference.Application.Interfaces;

/// <summary>
/// Command to share a chat template with another tenant.
/// </summary>
/// <param name="TemplateId">The template identifier.</param>
/// <param name="SourceTenantId">The source tenant identifier.</param>
/// <param name="TargetTenantId">The target tenant identifier.</param>
/// <param name="SharePermissions">The share permissions.</param>
public record ShareTemplateCommand(
    Guid TemplateId,
    Guid SourceTenantId,
    Guid TargetTenantId,
    string SharePermissions)
    : IRequest<ShareTemplateResult>;

/// <summary>
/// Result of sharing a chat template.
/// </summary>
/// <param name="Success">Whether the share was successful.</param>
/// <param name="ShareId">The unique share identifier.</param>
public record ShareTemplateResult(bool Success, Guid? ShareId);

/// <summary>
/// Handler for the <see cref="ShareTemplateCommand"/>.
/// </summary>
public class ShareTemplateCommandHandler : IRequestHandler<ShareTemplateCommand, ShareTemplateResult>
{
    private readonly IChatTemplateRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareTemplateCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The chat template repository.</param>
    public ShareTemplateCommandHandler(IChatTemplateRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ShareTemplateResult> Handle(ShareTemplateCommand request, CancellationToken cancellationToken)
    {
        // Validate input
        if (request.SourceTenantId == request.TargetTenantId)
        {
            throw new ArgumentException("Source and target tenants cannot be the same.", nameof(request.TargetTenantId));
        }

        // Get existing template
        var template = await _repository.GetByIdAsync(request.TemplateId, cancellationToken);
        if (template is null)
        {
            throw new InvalidOperationException($"Template with ID '{request.TemplateId}' was not found.");
        }

        // Verify source tenant ownership
        if (template.TenantId != request.SourceTenantId)
        {
            throw new UnauthorizedAccessException("You do not have permission to share this template.");
        }

        // Verify template is not a system template
        if (template.IsSystemTemplate)
        {
            throw new InvalidOperationException("System templates cannot be shared.");
        }

        // Note: In a real implementation, this would create a share record
        // For now, we return a success with a generated share ID
        var shareId = Guid.NewGuid();

        return new ShareTemplateResult(true, shareId);
    }
}
