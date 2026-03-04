// <copyright file="CreateTemplateCommandTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="CreateTemplateCommandHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class CreateTemplateCommandTests
{
    private readonly Mock<IChatTemplateRepository> _repositoryMock;
    private readonly CreateTemplateCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateTemplateCommandTests"/> class.
    /// </summary>
    public CreateTemplateCommandTests()
    {
        _repositoryMock = new Mock<IChatTemplateRepository>();
        _handler = new CreateTemplateCommandHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that a valid command creates a template successfully.
    /// </summary>
    [Fact]
    public async Task Handle_ValidCommand_CreatesTemplate()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var parameters = new List<TemplateParameterDto>
        {
            new("name", "The name", ParameterType.String, true, null, null, Array.Empty<string>()),
        };

        var command = new CreateTemplateCommand(
            tenantId,
            "Test Template",
            "Test Description",
            "Hello {{name}}!",
            parameters,
            "General",
            false);

        _repositoryMock
            .Setup(r => r.NameExistsAsync(tenantId, "Test Template", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        ChatTemplate? capturedTemplate = null;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<ChatTemplate, CancellationToken>((t, _) => capturedTemplate = t)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TemplateId.Should().NotBe(Guid.Empty);
        result.Template.Should().NotBeNull();
        result.Template.Name.Should().Be("Test Template");
        result.Template.Description.Should().Be("Test Description");
        result.Template.TemplateContent.Should().Be("Hello {{name}}!");
        result.Template.Category.Should().Be("General");
        result.Template.TenantId.Should().Be(tenantId);
        result.Template.IsSystemTemplate.Should().BeFalse();
        result.Template.IsActive.Should().BeTrue();
        result.Template.Parameters.Should().HaveCount(1);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that an empty name throws an ArgumentException.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyName_ThrowsArgumentException(string? name)
    {
        // Arrange
        var command = new CreateTemplateCommand(
            Guid.NewGuid(),
            name!,
            null,
            "Content",
            Array.Empty<TemplateParameterDto>(),
            "General");

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
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyContent_ThrowsArgumentException(string? content)
    {
        // Arrange
        var command = new CreateTemplateCommand(
            Guid.NewGuid(),
            "Name",
            null,
            content!,
            Array.Empty<TemplateParameterDto>(),
            "General");

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
        var command = new CreateTemplateCommand(
            tenantId,
            "Existing Template",
            null,
            "Content",
            Array.Empty<TemplateParameterDto>(),
            "General");

        _repositoryMock
            .Setup(r => r.NameExistsAsync(tenantId, "Existing Template", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    /// <summary>
    /// Tests that parameters are correctly mapped.
    /// </summary>
    [Fact]
    public async Task Handle_WithParameters_MapsParametersCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var parameters = new List<TemplateParameterDto>
        {
            new(
                "param1",
                "Description 1",
                ParameterType.String,
                true,
                "default",
                "^[a-z]+$",
                new[] { "a", "b", "c" }),
        };

        var command = new CreateTemplateCommand(
            tenantId,
            "Template",
            null,
            "Content",
            parameters,
            "Category");

        _repositoryMock
            .Setup(r => r.NameExistsAsync(It.IsAny<Guid>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var param = result.Template.Parameters.Should().ContainSingle().Subject;
        param.Name.Should().Be("param1");
        param.Description.Should().Be("Description 1");
        param.Type.Should().Be(ParameterType.String);
        param.IsRequired.Should().BeTrue();
        param.DefaultValue.Should().Be("default");
        param.ValidationPattern.Should().Be("^[a-z]+$");
        param.AllowedValues.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    /// <summary>
    /// Tests that names and content are trimmed.
    /// </summary>
    [Fact]
    public async Task Handle_InputWithWhitespace_TrimsValues()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new CreateTemplateCommand(
            tenantId,
            "  Template Name  ",
            "  Description  ",
            "  Content  ",
            Array.Empty<TemplateParameterDto>(),
            "  Category  ");

        _repositoryMock
            .Setup(r => r.NameExistsAsync(tenantId, "Template Name", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ChatTemplate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Template.Name.Should().Be("Template Name");
        result.Template.Description.Should().Be("Description");
        result.Template.TemplateContent.Should().Be("Content");
    }
}
