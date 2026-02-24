// <copyright file="PerformanceIndexTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Tests.Migrations;

using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Xunit;

/// <summary>
/// Tests for performance indexes on tenant queries.
/// </summary>
public class PerformanceIndexTests
{
    /// <summary>
    /// Verifies that organizations.slug has a unique index.
    /// </summary>
    [Fact]
    public void Organizations_Slug_HasUniqueIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(Organization));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var slugIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            string.Equals(i.Properties[0].Name, "Slug", StringComparison.Ordinal));
        slugIndex.Should().NotBeNull();
        slugIndex?.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that teams has a composite index on organization_id + name.
    /// </summary>
    [Fact]
    public void Teams_HasOrganizationIdNameIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(Team));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var compositeIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => string.Equals(p.Name, "OrganizationId", StringComparison.Ordinal)) &&
            i.Properties.Any(p => string.Equals(p.Name, "Name", StringComparison.Ordinal)));
        compositeIndex.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that users.email has a unique index.
    /// </summary>
    [Fact]
    public void Users_Email_HasUniqueIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(User));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var emailIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            string.Equals(i.Properties[0].Name, "Email", StringComparison.Ordinal));
        emailIndex.Should().NotBeNull();
        emailIndex?.IsUnique.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that team_memberships has a composite index on team_id + user_id.
    /// </summary>
    [Fact]
    public void TeamMemberships_HasTeamIdUserIdIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(TeamMembership));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var compositeIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => string.Equals(p.Name, "TeamId", StringComparison.Ordinal)) &&
            i.Properties.Any(p => string.Equals(p.Name, "UserId", StringComparison.Ordinal)));
        compositeIndex.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that virtual_keys has a composite index on organization_id + name.
    /// </summary>
    [Fact]
    public void VirtualKeys_HasOrganizationIdNameIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(VirtualKey));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var compositeIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => string.Equals(p.Name, "OrganizationId", StringComparison.Ordinal)) &&
            i.Properties.Any(p => string.Equals(p.Name, "Name", StringComparison.Ordinal)));
        compositeIndex.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that audit_logs has a composite index on organization_id + timestamp.
    /// </summary>
    [Fact]
    public void AuditLogs_HasOrganizationIdTimestampIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(AuditLog));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var compositeIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => string.Equals(p.Name, "OrganizationId", StringComparison.Ordinal)) &&
            i.Properties.Any(p => string.Equals(p.Name, "Timestamp", StringComparison.Ordinal)));
        compositeIndex.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that requests has a composite index on organization_id + timestamp (created_at).
    /// </summary>
    [Fact]
    public void Requests_HasOrganizationIdCreatedAtIndex()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SynaxisDbContext>()
            .UseInMemoryDatabase(databaseName: "test_perf_indexes")
            .Options;

        using var context = new SynaxisDbContext(options);

        // Act
        var entity = context.Model.FindEntityType(typeof(Request));
        var indexes = entity?.GetIndexes().ToList();

        // Assert
        var compositeIndex = indexes?.FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            i.Properties.Any(p => string.Equals(p.Name, "OrganizationId", StringComparison.Ordinal)) &&
            i.Properties.Any(p => string.Equals(p.Name, "CreatedAt", StringComparison.Ordinal)));
        compositeIndex.Should().NotBeNull();
    }
}
