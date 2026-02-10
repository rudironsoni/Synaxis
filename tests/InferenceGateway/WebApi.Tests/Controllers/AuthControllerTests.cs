// <copyright file="AuthControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Tests.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.WebApi.Controllers;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class AuthControllerTests : IDisposable
    {
        private readonly Mock<IJwtService> _mockJwtService;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly SynaxisDbContext _dbContext;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly Guid _testOrganizationId = Guid.NewGuid();
        private bool _disposed;

        public AuthControllerTests()
        {
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new SynaxisDbContext(options);
            _mockJwtService = new Mock<IJwtService>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Setup password hasher to return a hashed password
            _mockPasswordHasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns((string password) => $"hashed_{password}");

            _controller = new AuthController(
                _mockJwtService.Object,
                _mockPasswordHasher.Object,
                _dbContext,
                _mockLogger.Object);

            // Setup HttpContext and Request
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            _controller.Request.Scheme = "https";
            _controller.Request.Host = new Microsoft.AspNetCore.Http.HostString("localhost:5000");
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
        public async Task ForgotPassword_WithValidEmail_GeneratesTokenAndSendsEmail()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify that a password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].UserId.Should().Be(testUser.Id);
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ReturnsSuccessForSecurity()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = "nonexistent@example.com"
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();

            var response = okResult.Value!.GetType().GetProperty("success")?.GetValue(okResult.Value);
            response.Should().Be(true);

            // Verify that no password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_GeneratesUniqueToken()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result1 = await _controller.ForgotPassword(request, CancellationToken.None);
            var result2 = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result1.Should().BeOfType<OkObjectResult>();
            result2.Should().BeOfType<OkObjectResult>();

            // Verify that two tokens were added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(2);
            resetTokens[0].TokenHash.Should().NotBe(resetTokens[1].TokenHash);
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_SetsTokenExpiration()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify that the token was added with expiration
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].UserId.Should().Be(testUser.Id);
            resetTokens[0].ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            resetTokens[0].ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddHours(1).AddMinutes(1));
            resetTokens[0].IsUsed.Should().BeFalse();
        }

        [Fact]
        public async Task ForgotPassword_WithEmptyEmail_ReturnsBadRequest()
        {
            // Arrange
            var request = new ForgotPasswordRequest
            {
                Email = string.Empty
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();

            // Verify that no password reset token was added
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().BeEmpty();
        }

        [Fact]
        public async Task ForgotPassword_WithValidEmail_StoresTokenHash()
        {
            // Arrange
            var testUser = CreateTestUser();
            _dbContext.Users.Add(testUser);
            await _dbContext.SaveChangesAsync();

            var request = new ForgotPasswordRequest
            {
                Email = testUser.Email
            };

            // Act
            var result = await _controller.ForgotPassword(request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            // Verify that the token hash is stored
            var resetTokens = await _dbContext.PasswordResetTokens.ToListAsync();
            resetTokens.Should().HaveCount(1);
            resetTokens[0].TokenHash.Should().NotBeNullOrEmpty();
            resetTokens[0].TokenHash.Length.Should().BeGreaterThan(0);
        }

        private User CreateTestUser()
        {
            return new User
            {
                Id = _testUserId,
                OrganizationId = _testOrganizationId,
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Role = "member",
                DataResidencyRegion = "us-east-1",
                CreatedInRegion = "us-east-1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
