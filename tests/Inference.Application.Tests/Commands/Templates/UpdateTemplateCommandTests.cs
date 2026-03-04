// <copyright file="UpdateTemplateCommandTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Commands.Templates;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Commands.Templates;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="UpdateTemplateCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateTemplateCommandTests
{
    private readonly Mock<IChatTemplateRepository> _repositoryMock;
    private readonly UpdateTemplateCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateTemplateCommandTests"/> class.
    /// </summary>
    public UpdateTemplateCommandTests()
    {
        _repositoryMock = new Mock<IChatTemplateRepository>();
        _handler = new UpdateTemplateCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command updates a template successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_UpdatesTemplate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, tenantId, "Old Name");

        var command = new UpdateTemplateCommand(
            templateId,
            tenantId,
            "New Name",
            "New Description",
            "New Content",
            new List<TemplateParameterDto>(),
            "New Category");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        _repositoryMock
            .Setup(r => r.NameExistsAsync(tenantId, "New Name", templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Template.Should().NotBeNull();
        result.Template.Name.Should().Be("New Name");
        result.Template.Description.Should().Be("New Description");
        result.Template.TemplateContent.Should().Be("New Content");
        result.Template.Category.Should().Be("New Category");

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that updating a non-existent template throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_NonExistentTemplate_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new UpdateTemplateCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Name",
            null,
            "Content",
            Array.Empty<TemplateParameterDto>(),
            "Category");

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
    /// Tests that updating another tenant's template throws an UnauthorizedAccessException.
    /// </summary>
    [Fact]
    public async Task Handle_DifferentTenant_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var templateTenantId = Guid.NewGuid();
        var requestTenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, templateTenantId, "Name");

        var command = new UpdateTemplateCommand(
            templateId,
            requestTenantId,
            "New Name",
            null,
            "New Content",
            Array.Empty<TemplateParameterDto>(),
            "Category");

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
    /// Tests that an empty name throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyName_ThrowsArgumentException(string name)
    {
        // Arrange
        var command = new UpdateTemplateCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            name,
            null,
            "Content",
            Array.Empty<TemplateParameterDto>(),
            "Category");

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*name*");
    }

    /// <summary>
    /// Tests that an empty content throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyContent_ThrowsArgumentException(string content)
    {
        // Arrange
        var command = new UpdateTemplateCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Name",
            null,
            content,
            Array.Empty<TemplateParameterDto>(),
            "Category");

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*content*");
    }

    /// <summary>
    /// Tests that a duplicate name throws an InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task Handle_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var existingTemplate = CreateTemplate(templateId, tenantId, "Old Name");

        var command = new UpdateTemplateCommand(
            templateId,
            tenantId,
            "Existing Name",
            null,
            "Content",
            Array.Empty<TemplateParameterDto>(),
            "Category");

        _repositoryMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTemplate);

        _repositoryMock
            .Setup(r => r.NameExistsAsync(tenantId, "Existing Name", templateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    private static ChatTemplate CreateTemplate(Guid id, Guid tenantId, string name)
    {
        var template = ChatTemplate.Create(
            id,
            name,
            null,
            "Content",
            new List<TemplateParameter>(),
            "Category",
            tenantId,
            false);
        return template;
    }
}
