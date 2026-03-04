// <copyright file="DeleteTemplateCommandTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Commands.Templates;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Commands.Templates;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DeleteTemplateCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class DeleteTemplateCommandTests
{
    private readonly Mock<IChatTemplateRepository> _repositoryMock;
    private readonly DeleteTemplateCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteTemplateCommandTests"/> class.
    /// </summary>
    public DeleteTemplateCommandTests()
    {
        _repositoryMock = new Mock<IChatTemplateRepository>();
        _handler = new DeleteTemplateCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command deletes a template successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_DeletesTemplate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, tenantId, "Name", false);

        var command = new DeleteTemplateCommand(templateId, tenantId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that deleting a non-existent template throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_NonExistentTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new DeleteTemplateCommand(Guid.NewGuid(), Guid.NewGuid());

        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ChatTemplate?)null);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was not found*");
    }

    /// <summary>
    /// Tests that deleting another tenant's template throws an UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_DifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var templateTenantId = Guid.NewGuid();
        var requestTenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, templateTenantId, "Name", false);

        var command = new DeleteTemplateCommand(templateId, requestTenantId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*permission*");
    }

    /// <summary>
    /// Tests that deleting a system template throws an InvalidOperationException from domain.
    /// </summary>
    [Fact]
    public async Task Handle_SystemTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, tenantId, "System Template", true);

        var command = new DeleteTemplateCommand(templateId, tenantId);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*system template*");
    }

    private static ChatTemplate CreateTemplate(Guid id, Guid tenantId, string name, bool isSystemTemplate)
    {
        return ChatTemplate.Create(
            id,
            name,
            null,
            "Content",
            new List<TemplateParameter>(),
            "Category",
            tenantId,
            isSystemTemplate);
    }
}
