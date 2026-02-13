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
    public class CollectionsControllerTests : IDisposable
    {
        private readonly SynaxisDbContext _dbContext;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly CollectionsController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private readonly Guid _testCollectionId = Guid.NewGuid();
        private readonly Guid _testTeamId = Guid.NewGuid();
        private bool _disposed;

        public CollectionsControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _mockAuditService = new Mock<IAuditService>();

            _controller = new CollectionsController(_dbContext, _mockAuditService.Object);
            SetupControllerContext();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _dbContext.Dispose();
                _disposed = true;
            }
        }

        [Fact]
        public async Task CreateCollection_WhenUserNotInOrganization_ReturnsForbid()
        {
            // Arrange
            var request = new CreateCollectionRequest { Name = "Test Collection" };

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
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

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
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

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
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

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
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

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
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

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
            var org = CreateTestOrganization();

            _dbContext.Users.Add(user);
            _dbContext.Organizations.Add(org);
            await _dbContext.SaveChangesAsync();

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

            // Verify collection was created
            var collections = await _dbContext.Collections.ToListAsync();
            collections.Should().HaveCount(1);
            collections[0].Name.Should().Be("Test Collection");

            // Verify membership was created
            var memberships = await _dbContext.CollectionMemberships.ToListAsync();
            memberships.Should().HaveCount(1);
            memberships[0].UserId.Should().Be(_testUserId);
            memberships[0].CollectionId.Should().Be(collections[0].Id);

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

            _dbContext.Users.Add(user);
            _dbContext.Collections.AddRange(collections);
            await _dbContext.SaveChangesAsync();

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

            _dbContext.CollectionMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

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

            _dbContext.CollectionMemberships.Add(membership);
            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();

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

            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();

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

            // Refresh from database
            await _dbContext.Entry(collection).ReloadAsync();
            collection.Name.Should().Be("Updated Name");
            collection.Description.Should().Be("Updated Description");
            collection.Type.Should().Be("prompts");
            collection.Visibility.Should().Be("public");

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

            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();

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

            // Verify collection was deleted
            var collections = await _dbContext.Collections.ToListAsync();
            collections.Should().BeEmpty();

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

            _dbContext.CollectionMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

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

            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();

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

            _dbContext.Collections.Add(collection);
            _dbContext.Users.Add(otherUser);
            await _dbContext.SaveChangesAsync();

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

            var existingMembership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = otherUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Collections.Add(collection);
            _dbContext.Users.Add(otherUser);
            _dbContext.CollectionMemberships.Add(existingMembership);
            await _dbContext.SaveChangesAsync();

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
            var otherUser = CreateTestUser(otherUserId);

            _dbContext.Collections.Add(collection);
            _dbContext.Users.Add(otherUser);
            await _dbContext.SaveChangesAsync();

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

            // Verify membership was created
            var memberships = await _dbContext.CollectionMemberships.ToListAsync();
            memberships.Should().HaveCount(2); // 1 from SetupCollectionAdminPermission + 1 new
            memberships.Should().Contain(m => m.UserId == otherUserId && m.CollectionId == _testCollectionId);

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
            // Create an organization membership for the current user so they have permission
            var orgMembership = new OrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OrganizationId = _testOrganizationId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow,
            };
            _dbContext.OrganizationMemberships.Add(orgMembership);

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

            _dbContext.Collections.Add(collection);
            await _dbContext.SaveChangesAsync();

            // Act
            var nonMemberUserId = Guid.NewGuid();
            var result = await _controller.RemoveMember(_testOrganizationId, _testCollectionId, nonMemberUserId, CancellationToken.None);

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

            var membership = new CollectionMembership
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                CollectionId = _testCollectionId,
                OrganizationId = _testOrganizationId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow,
            };

            _dbContext.Collections.Add(collection);
            _dbContext.CollectionMemberships.Add(membership);
            await _dbContext.SaveChangesAsync();

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

            // Verify membership was removed
            var memberships = await _dbContext.CollectionMemberships.ToListAsync();
            memberships.Should().HaveCount(1); // Only the admin membership from SetupCollectionAdminPermission remains

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

            _dbContext.CollectionMemberships.Add(membership);
            _dbContext.SaveChanges();
        }
    }
}
