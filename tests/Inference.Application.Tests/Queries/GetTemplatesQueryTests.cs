// <copyright file="GetTemplatesQueryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Inference.Application.Tests.Queries;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Synaxis.Inference.Application.Dtos;
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Application.Queries;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="GetTemplatesQueryHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GetTemplatesQueryTests
{
    private readonly Mock<IChatTemplateRepository> _repositoryMock;
    private readonly GetTemplatesQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTemplatesQueryTests"/> class.
    /// </summary>
    public GetTemplatesQueryTests()
    {
        _repositoryMock = new Mock<IChatTemplateRepository>();
        _handler = new GetTemplatesQueryHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that query returns templates for tenant.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTenant_ReturnsTemplates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templates = new List<ChatTemplate>
        {
            CreateTemplate(Guid.NewGuid(), tenantId, "Template 1", "Category 1"),
            CreateTemplate(Guid.NewGuid(), tenantId, "Template 2", "Category 2"),
        };

        var command = new GetTemplatesQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(t => t.TenantId == tenantId).Should().BeTrue();
    }

    /// <summary>
    /// Tests that query returns empty list when no templates exist.
    /// </summary>
    [Fact]
    public async Task Handle_NoTemplates_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new GetTemplatesQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatTemplate>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that category filter filters templates correctly.
    /// </summary>
    [Fact]
    public async Task Handle_WithCategoryFilter_FiltersTemplates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templates = new List<ChatTemplate>
        {
            CreateTemplate(Guid.NewGuid(), tenantId, "Template 1", "General"),
            CreateTemplate(Guid.NewGuid(), tenantId, "Template 2", "Code"),
            CreateTemplate(Guid.NewGuid(), tenantId, "Template 3", "General"),
        };

        var command = new GetTemplatesQuery(tenantId, Category: "General");

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.All(t => t.Category == "General").Should().BeTrue();
    }

    /// <summary>
    /// Tests that include inactive returns inactive templates.
    /// </summary>
    [Fact]
    public async Task Handle_IncludeInactive_CallsRepositoryWithIncludeInactive()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new GetTemplatesQuery(tenantId, IncludeInactive: true);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatTemplate>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.GetByTenantAsync(tenantId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that templates are correctly mapped to DTOs.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTemplates_MapsToDtoCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var template = ChatTemplate.Create(
            templateId,
            "Template",
            "Description",
            "Content",
            new List<TemplateParameter>(),
            "Category",
            tenantId,
            false);

        var templates = new List<ChatTemplate> { template };

        var command = new GetTemplatesQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(templateId);
        dto.Name.Should().Be("Template");
        dto.Description.Should().Be("Description");
        dto.TemplateContent.Should().Be("Content");
        dto.Category.Should().Be("Category");
        dto.TenantId.Should().Be(tenantId);
        dto.IsSystemTemplate.Should().BeFalse();
        dto.IsActive.Should().BeTrue();
    }

    private static ChatTemplate CreateTemplate(Guid id, Guid tenantId, string name, string category)
    {
        return ChatTemplate.Create(
            id,
            name,
            null,
            "Content",
            new List<TemplateParameter>(),
            category,
            tenantId,
            false);
    }
}
