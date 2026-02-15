// <copyright file="TestContainerFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Factory for creating test containers for integration testing.
/// Provides standardized container configurations for common services.
/// NOTE: This requires the Testcontainers NuGet packages to be installed:
/// - Testcontainers.Qdrant
/// - Testcontainers.Redis
/// - Testcontainers.PostgreSql.
/// </summary>
public static class TestContainerFactory
{
    /// <summary>
    /// Creates a Qdrant vector database container for testing semantic caching.
    /// Example usage:
    /// <code>
    /// var qdrant = TestContainerFactory.CreateQdrant();
    /// await qdrant.StartAsync();
    /// // Use qdrant.GetConnectionString() to connect
    /// await qdrant.StopAsync();
    /// </code>
    /// </summary>
    /// <returns></returns>
    public static ITestContainer CreateQdrant()
    {
        throw new NotSupportedException(
            "Testcontainers.Qdrant package is required. " +
            "Install via: dotnet add package Testcontainers.Qdrant");
    }

    /// <summary>
    /// Creates a Redis container for testing session storage and in-flight deduplication.
    /// Example usage:
    /// <code>
    /// var redis = TestContainerFactory.CreateRedis();
    /// await redis.StartAsync();
    /// // Use redis.GetConnectionString() to connect
    /// await redis.StopAsync();
    /// </code>
    /// </summary>
    /// <returns></returns>
    public static ITestContainer CreateRedis()
    {
        throw new NotSupportedException(
            "Testcontainers.Redis package is required. " +
            "Install via: dotnet add package Testcontainers.Redis");
    }

    /// <summary>
    /// Creates a PostgreSQL container for testing configuration storage.
    /// Example usage:
    /// <code>
    /// var postgres = TestContainerFactory.CreatePostgreSql();
    /// await postgres.StartAsync();
    /// // Use postgres.GetConnectionString() to connect
    /// await postgres.StopAsync();
    /// </code>
    /// </summary>
    /// <returns></returns>
    public static ITestContainer CreatePostgreSql()
    {
        throw new NotSupportedException(
            "Testcontainers.PostgreSql package is required. " +
            "Install via: dotnet add package Testcontainers.PostgreSql");
    }
}
