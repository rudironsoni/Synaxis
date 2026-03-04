// <copyright file="GetModelConfigsQueryTests.cs" company="Synaxis">
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
using Synaxis.Inference.Application.Interfaces;
using Synaxis.Inference.Application.Queries;
using Synaxis.Inference.Domain.Aggregates;
using Xunit;

/// <summary>
/// Unit tests for <see cref="GetModelConfigsQueryHandler"/>.
/// </summary>
[Trait("Category", "Unit")]
public class GetModelConfigsQueryTests
{
    private readonly Mock<IModelConfigRepository> _repositoryMock;
    private readonly GetModelConfigsQueryHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetModelConfigsQueryTests"/> class.
    /// </summary>
    public GetModelConfigsQueryTests()
    {
        _repositoryMock = new Mock<IModelConfigRepository>();
        _handler = new GetModelConfigsQueryHandler(_repositoryMock.Object);
    }

    /// <summary>
    /// Tests that query returns configurations for tenant.
    /// </summary>
    [Fact]
    public async Task Handle_ValidTenant_ReturnsConfigs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configs = new List<ModelConfig>
        {
            CreateConfig(Guid.NewGuid(), tenantId, "gpt-4", "openai"),
            CreateConfig(Guid.NewGuid(), tenantId, "claude-3", "anthropic"),
        };

        var command = new GetModelConfigsQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(c => c.TenantId == tenantId).Should().BeTrue();
    }

    /// <summary>
    /// Tests that provider filter filters configurations correctly.
    /// </summary>
    [Fact]
    public async Task Handle_WithProviderFilter_FiltersConfigs()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configs = new List<ModelConfig>
        {
            CreateConfig(Guid.NewGuid(), tenantId, "gpt-4", "openai"),
            CreateConfig(Guid.NewGuid(), tenantId, "claude-3", "anthropic"),
            CreateConfig(Guid.NewGuid(), tenantId, "gpt-3.5", "openai"),
        };

        var command = new GetModelConfigsQuery(tenantId, ProviderId: "openai");

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.All(c => c.ProviderId == "openai").Should().BeTrue();
    }

    /// <summary>
    /// Tests that configurations are correctly mapped to DTOs.
    /// </summary>
    [Fact]
    public async Task Handle_ValidConfigs_MapsToDtoCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var config = ModelConfig.Create(
            configId,
            "gpt-4",
            "openai",
            "GPT-4",
            "Advanced model",
            new ModelSettings(),
            new ModelPricing(),
            new ModelCapabilities(),
            tenantId);

        var configs = new List<ModelConfig> { config };

        var command = new GetModelConfigsQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(configId);
        dto.ModelId.Should().Be("gpt-4");
        dto.ProviderId.Should().Be("openai");
        dto.DisplayName.Should().Be("GPT-4");
        dto.Description.Should().Be("Advanced model");
        dto.TenantId.Should().Be(tenantId);
        dto.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tests that empty list is returned when no configurations exist.
    /// </summary>
    [Fact]
    public async Task Handle_NoConfigs_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new GetModelConfigsQuery(tenantId);

        _repositoryMock
            .Setup(r => r.GetByTenantAsync(tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModelConfig>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    private static ModelConfig CreateConfig(Guid id, Guid tenantId, string modelId, string providerId)
    {
        return ModelConfig.Create(
            id,
            modelId,
            providerId,
            modelId.ToUpper(),
            null,
            new ModelSettings(),
            new ModelPricing(),
            new ModelCapabilities(),
            tenantId);
    }
}
