// <copyright file="CollectionsControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using Synaxis.Core.Models;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;

    [Trait("Category", "Unit")]
    public class CollectionsControllerTests
    {
        private readonly Mock<SynaxisDbContext> _mockDbContext;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly CollectionsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testCollectionId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();

        public CollectionsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _mockDbContext = new Mock<SynaxisDbContext>(options);
            _mockAuditService = new Mock<IAuditService>();

            _controller = new CollectionsController(_mockDbContext.Object, _mockAuditService.Object);
            SetupControllerContext();
        }

        [Fact]
        public async Task CreateCollection_WhenUserNotInOrganization_ReturnsForbid()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection" };
            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task CreateCollection_WhenOrganizationDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection" };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var mockOrgSet = CreateMockDbSet(Array.Empty<Organization>());
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Organization not found");
        }

        [Fact]
        public async Task CreateCollection_WhenNameIsEmpty_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = string.Empty };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Name is required");
        }

        [Fact]
        public async Task CreateCollection_WhenTypeIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection", Type = "invalid_type" };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Type must be one of: general, models, prompts, datasets, workflows");
        }

        [Fact]
        public async Task CreateCollection_WhenVisibilityIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection", Visibility = "invalid_visibility" };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Visibility must be one of: public, private, team");
        }

        [Fact]
        public async Task CreateCollection_WhenTeamDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection", TeamId = _testTeamId };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var mockTeamSet = CreateMockDbSet(Array.Empty<Team>());
            _mockDbContext.Setup(x => x.Teams).Returns(mockTeamSet.Object);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Team not found");
        }

        [Fact]
        public async Task CreateCollection_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateCollectionRequest
            {
                Name = "Test Collection",
                Description = "Test Description",
                Type = "models",
                Visibility = "private",
            };
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var org = CreateTestOrganization();
            var mockOrgSet = CreateMockDbSet(new[] { org });
            _mockDbContext.Setup(x => x.Organizations).Returns(mockOrgSet.Object);

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);
            _mockDbContext.Setup(x => x.Collections.Add(It.IsAny<Collection>()));

            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);
            _mockDbContext.Setup(x => x.CollectionMemberships.Add(It.IsAny<CollectionMembership>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.CreateCollection(_testOrganizationId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;
            createdResult!.ActionName.Should().Be(nameof(CollectionsController.GetCollection));

            var value = createdResult.Value;
            value.Should().NotBeNull();

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.Collections.Add(It.IsAny<Collection>()), Times.Once);
            _mockDbContext.Verify(x => x.CollectionMemberships.Add(It.IsAny<CollectionMembership>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "CreateCollection",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ListCollections_WhenUserNotInOrganization_ReturnsForbid()
        {
            // Arrange
            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.ListCollections(_testOrganizationId, null, null, null, 0, 20, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ListCollections_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var user = CreateTestUser();
            var mockUserSet = CreateMockDbSet(new[] { user });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var collections = new[]
            {
                new Collection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _testOrganizationId,
                    Name = "Collection 1",
                    Description = "Description 1",
                    Slug = "collection-1",
                    Type = "models",
                    Visibility = "private",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = _testUserId,
                    CollectionMemberships = new List<CollectionMembership>(),
                },
                new Collection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _testOrganizationId,
                    Name = "Collection 2",
                    Description = "Description 2",
                    Slug = "collection-2",
                    Type = "prompts",
                    Visibility = "public",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = _testUserId,
                    CollectionMemberships = new List<CollectionMembership>(),
                },
            };

            var mockCollectionSet = CreateMockDbSet(collections);
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.ListCollections(_testOrganizationId, null, null, null, 0, 20, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCollection_WhenUserNotMember_ReturnsForbid()
        {
            // Arrange
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.GetCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task GetCollection_WhenCollectionNotFound_ReturnsNotFound()
        {
            // Arrange
            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.GetCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Collection not found");
        }

        [Fact]
        public async Task GetCollection_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                Tags = new List<string> { "tag1", "tag2" },
                Metadata = new Dictionary<string, object>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership> { membership },
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.GetCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateCollection_WhenUserNotAdmin_ReturnsForbid()
        {
            // Arrange
            var request = new UpdateCollectionRequest { Name = "Updated Name" };
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.UpdateCollection(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateCollection_WhenCollectionNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateCollectionRequest { Name = "Updated Name" };
            SetupCollectionAdminPermission();

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.UpdateCollection(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Collection not found");
        }

        [Fact]
        public async Task UpdateCollection_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new UpdateCollectionRequest
            {
                Name = "Updated Name",
                Description = "Updated Description",
                Type = "prompts",
                Visibility = "public",
            };
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Original Name",
                Description = "Original Description",
                Slug = "original-slug",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateCollection(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "UpdateCollection",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteCollection_WhenUserNotAdmin_ReturnsForbid()
        {
            // Arrange
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.DeleteCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DeleteCollection_WhenCollectionNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupCollectionAdminPermission();

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.DeleteCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Collection not found");
        }

        [Fact]
        public async Task DeleteCollection_WithValidRequest_ReturnsNoContent()
        {
            // Arrange
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);
            _mockDbContext.Setup(x => x.Collections.Remove(It.IsAny<Collection>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteCollection(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.Collections.Remove(It.IsAny<Collection>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "DeleteCollection",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ListMembers_WhenUserNotMember_ReturnsForbid()
        {
            // Arrange
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ListMembers_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.ListMembers(_testOrganizationId, _testCollectionId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task AddMember_WhenUserNotAdmin_ReturnsForbid()
        {
            // Arrange
            var request = new AddCollectionMemberRequest { UserId = Guid.NewGuid(), Role = "member" };
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task AddMember_WhenRoleIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddCollectionMemberRequest { UserId = Guid.NewGuid(), Role = "invalid_role" };
            SetupCollectionAdminPermission();

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Role must be 'admin', 'member', or 'viewer'");
        }

        [Fact]
        public async Task AddMember_WhenCollectionNotFound_ReturnsNotFound()
        {
            // Arrange
            var request = new AddCollectionMemberRequest { UserId = Guid.NewGuid(), Role = "member" };
            SetupCollectionAdminPermission();

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Collection not found");
        }

        [Fact]
        public async Task AddMember_WhenUserNotFound_ReturnsBadRequest()
        {
            // Arrange
            var request = new AddCollectionMemberRequest { UserId = Guid.NewGuid(), Role = "member" };
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User not found");
        }

        [Fact]
        public async Task AddMember_WhenUserNotInOrganization_ReturnsBadRequest()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var request = new AddCollectionMemberRequest { UserId = otherUserId, Role = "member" };
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var otherOrgId = Guid.NewGuid();
            var otherUser = new User
            {
                Id = otherUserId,
                OrganizationId = otherOrgId,
                Email = "other@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                Role = "member",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var mockUserSet = CreateMockDbSet(new[] { otherUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is not a member of the organization");
        }

        [Fact]
        public async Task AddMember_WhenUserAlreadyMember_ReturnsBadRequest()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var request = new AddCollectionMemberRequest { UserId = otherUserId, Role = "member" };
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var otherUser = new User
            {
                Id = otherUserId,
                OrganizationId = _testOrganizationId,
                Email = "other@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                Role = "member",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            var mockUserSet = CreateMockDbSet(new[] { otherUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var existingMembership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { existingMembership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("User is already a member of the collection");
        }

        [Fact]
        public async Task AddMember_WithValidRequest_ReturnsCreated()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var request = new AddCollectionMemberRequest { UserId = otherUserId, Role = "member" };
            SetupCollectionAdminPermission();

            var collection = CreateTestCollection();
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var otherUser = CreateTestUser(otherUserId);
            var mockUserSet = CreateMockDbSet(new[] { otherUser });
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);
            _mockDbContext.Setup(x => x.CollectionMemberships.Add(It.IsAny<CollectionMembership>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.AddMember(_testOrganizationId, _testCollectionId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<StatusCodeResult>();
            var statusCodeResult = result as StatusCodeResult;
            statusCodeResult!.StatusCode.Should().Be(201);

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.CollectionMemberships.Add(It.IsAny<CollectionMembership>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "AddCollectionMember",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RemoveMember_WhenUserNotAdminAndNotSelf_ReturnsForbid()
        {
            // Arrange
            var otherUserId = Guid.NewGuid();
            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            var mockUserSet = CreateMockDbSet(Array.Empty<User>());
            _mockDbContext.Setup(x => x.Users).Returns(mockUserSet.Object);

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testCollectionId, otherUserId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task RemoveMember_WhenCollectionNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupCollectionAdminPermission();

            var mockCollectionSet = CreateMockDbSet(Array.Empty<Collection>());
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testCollectionId, _testUserId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Collection not found");
        }

        [Fact]
        public async Task RemoveMember_WhenMemberNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupCollectionAdminPermission();

            var collection = new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var mockMembershipSet = CreateMockDbSet(Array.Empty<CollectionMembership>());
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testCollectionId, _testUserId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Member not found in collection");
        }

        [Fact]
        public async Task RemoveMember_WithValidRequest_ReturnsNoContent()
        {
            // Arrange
            SetupCollectionAdminPermission();

            var collection = CreateTestCollection();
            var mockCollectionSet = CreateMockDbSet(new[] { collection });
            _mockDbContext.Setup(x => x.Collections).Returns(mockCollectionSet.Object);

            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);
            _mockDbContext.Setup(x => x.CollectionMemberships.Remove(It.IsAny<CollectionMembership>()));

            _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockAuditService.Setup(x => x.LogAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RemoveMember(_testOrganizationId, _testCollectionId, _testUserId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify DbContext interactions
            _mockDbContext.Verify(x => x.CollectionMemberships.Remove(It.IsAny<CollectionMembership>()), Times.Once);
            _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            // Verify audit log
            _mockAuditService.Verify(x => x.LogAsync(
                _testOrganizationId,
                _testUserId,
                "RemoveCollectionMember",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim("sub", _testUserId.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal },
            };
        }

        private User CreateTestUser(Guid? userId = null)
        {
            return new User
            {
                Id = userId ?? _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hash",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                Role = "member",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private Collection CreateTestCollection()
        {
            return new Collection
            {
                Id = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Name = "Test Collection",
                Description = "Test Description",
                Slug = "test-collection",
                Type = "models",
                Visibility = "private",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = _testUserId,
                CollectionMemberships = new List<CollectionMembership>(),
            };
        }

        private Organization CreateTestOrganization()
        {
            return new Organization
            {
                Id = _testOrganizationId,
                Slug = "test-org",
                Name = "Test Organization",
                Description = "Test Description",
                PrimaryRegion = "us-east-1",
                Tier = "free",
                BillingCurrency = "USD",
                CreditBalance = 0.00m,
                IsActive = true,
                IsVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
        }

        private void SetupCollectionAdminPermission()
        {
            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
            };
            var mockMembershipSet = CreateMockDbSet(new[] { membership });
            _mockDbContext.Setup(x => x.CollectionMemberships).Returns(mockMembershipSet.Object);
        }

        private static Mock<DbSet<T>> CreateMockDbSet<T>(IEnumerable<T> data)
            where T : class
        {
            var queryable = data.AsQueryable();
            var mockSet = new Mock<DbSet<T>>();

            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            return mockSet;
        }
    }
}
