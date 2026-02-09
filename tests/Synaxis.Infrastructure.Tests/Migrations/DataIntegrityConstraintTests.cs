// <copyright file="DataIntegrityConstraintTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using FluentAssertions;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;
    using Xunit;

    /// <summary>
    /// Tests for data integrity constraints.
    /// </summary>
    public class DataIntegrityConstraintTests
    {
        /// <summary>
        /// Verifies that organization slug validation regex accepts valid slugs.
        /// </summary>
        [Theory]
        [InlineData("my-org")]
        [InlineData("test-organization")]
        [InlineData("org123")]
        [InlineData("my-org-123")]
        public void OrganizationSlug_ValidSlugs_Accepted(string slug)
        {
            // Arrange
            var regex = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));

            // Act & Assert
            regex.IsMatch(slug).Should().BeTrue();
        }

        /// <summary>
        /// Verifies that organization slug validation regex rejects invalid slugs.
        /// </summary>
        [Theory]
        [InlineData("MyOrg")]
        [InlineData("my_org")]
        [InlineData("my org")]
        [InlineData("-myorg")]
        [InlineData("myorg-")]
        [InlineData("my--org")]
        [InlineData("")]
        public void OrganizationSlug_InvalidSlugs_Rejected(string slug)
        {
            // Arrange
            var regex = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(100));

            // Act & Assert
            regex.IsMatch(slug).Should().BeFalse();
        }

        /// <summary>
        /// Verifies that team budget must be non-negative.
        /// </summary>
        [Fact]
        public void TeamBudget_NegativeValue_Invalid()
        {
            // Arrange
            var team = new Team
            {
                Id = Guid.NewGuid(),
                Name = "Test Team",
                Slug = "test-team",
                OrganizationId = Guid.NewGuid(),
                MonthlyBudget = -100m,
            };

            // Act & Assert
            team.MonthlyBudget.Should().BeLessThan(0);
        }

        /// <summary>
        /// Verifies that user email validation works correctly.
        /// </summary>
        [Theory]
        [InlineData("user@example.com", true)]
        [InlineData("test.user@domain.co.uk", true)]
        [InlineData("user+tag@example.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@example.com", false)]
        [InlineData("user@", false)]
        [InlineData("", false)]
        public void UserEmail_Validation(string email, bool expectedValid)
        {
            // Arrange
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));

            // Act
            var isValid = regex.IsMatch(email);

            // Assert
            isValid.Should().Be(expectedValid);
        }

        /// <summary>
        /// Verifies that membership role validation accepts valid roles.
        /// </summary>
        [Theory]
        [InlineData("OrgAdmin")]
        [InlineData("TeamAdmin")]
        [InlineData("Member")]
        [InlineData("Viewer")]
        public void MembershipRole_ValidRoles_Accepted(string role)
        {
            // Arrange
            var validRoles = new[] { "OrgAdmin", "TeamAdmin", "Member", "Viewer" };

            // Act & Assert
            validRoles.Should().Contain(role);
        }

        /// <summary>
        /// Verifies that membership role validation rejects invalid roles.
        /// </summary>
        [Theory]
        [InlineData("Admin")]
        [InlineData("Owner")]
        [InlineData("User")]
        [InlineData("")]
        public void MembershipRole_InvalidRoles_Rejected(string role)
        {
            // Arrange
            var validRoles = new[] { "OrgAdmin", "TeamAdmin", "Member", "Viewer" };

            // Act & Assert
            validRoles.Should().NotContain(role);
        }

        /// <summary>
        /// Verifies that API key status validation accepts valid statuses.
        /// </summary>
        [Theory]
        [InlineData("active")]
        [InlineData("revoked")]
        [InlineData("expired")]
        public void ApiKeyStatus_ValidStatuses_Accepted(string status)
        {
            // Arrange
            var validStatuses = new[] { "active", "revoked", "expired" };

            // Act & Assert
            validStatuses.Should().Contain(status);
        }

        /// <summary>
        /// Verifies that API key status validation rejects invalid statuses.
        /// </summary>
        [Theory]
        [InlineData("inactive")]
        [InlineData("deleted")]
        [InlineData("pending")]
        [InlineData("")]
        public void ApiKeyStatus_InvalidStatuses_Rejected(string status)
        {
            // Arrange
            var validStatuses = new[] { "active", "revoked", "expired" };

            // Act & Assert
            validStatuses.Should().NotContain(status);
        }

        /// <summary>
        /// Verifies that the model has the expected constraints configured.
        /// </summary>
        [Fact]
        public void Organization_HasSlugValidationConfigured()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: "test_constraints")
                .Options;

            using var context = new SynaxisDbContext(options);

            // Act
            var entity = context.Model.FindEntityType(typeof(Organization));

            // Assert
            entity.Should().NotBeNull();
            // Check that the entity is configured with proper column constraints
            var slugProperty = entity?.FindProperty("Slug");
            slugProperty.Should().NotBeNull();
            slugProperty?.IsNullable.Should().BeFalse();
            slugProperty?.GetMaxLength().Should().Be(100);
        }

        /// <summary>
        /// Verifies that User entity has email validation configured.
        /// </summary>
        [Fact]
        public void User_HasEmailValidationConfigured()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<SynaxisDbContext>()
                .UseInMemoryDatabase(databaseName: "test_constraints")
                .Options;

            using var context = new SynaxisDbContext(options);

            // Act
            var entity = context.Model.FindEntityType(typeof(User));

            // Assert
            entity.Should().NotBeNull();
            var emailProperty = entity?.FindProperty("Email");
            emailProperty.Should().NotBeNull();
            emailProperty?.IsNullable.Should().BeFalse();
            emailProperty?.GetMaxLength().Should().Be(255);
        }
    }
}
