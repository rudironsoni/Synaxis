namespace Synaxis.Common.Tests;

/// <summary>
/// Factory for creating test containers for integration testing.
/// Provides standardized container configurations for common services.
/// NOTE: This requires the Testcontainers NuGet packages to be installed:
/// - Testcontainers.Qdrant
/// - Testcontainers.Redis  
/// - Testcontainers.PostgreSql
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
    public static ITestContainer CreateQdrant()
    {
        // Requires: Testcontainers.Qdrant package
        // return new QdrantBuilder()
        //     .WithImage("qdrant/qdrant:latest")
        //     .WithPortBinding(6333, true)
        //     .Build();

        throw new NotImplementedException(
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
    public static ITestContainer CreateRedis()
    {
        // Requires: Testcontainers.Redis package
        // return new RedisBuilder()
        //     .WithImage("redis:7-alpine")
        //     .WithPortBinding(6379, true)
        //     .Build();

        throw new NotImplementedException(
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
    public static ITestContainer CreatePostgreSql()
    {
        // Requires: Testcontainers.PostgreSql package
        // return new PostgreSqlBuilder()
        //     .WithImage("postgres:15-alpine")
        //     .WithDatabase("synaxis_test")
        //     .WithUsername("test")
        //     .WithPassword("test")
        //     .Build();

        throw new NotImplementedException(
            "Testcontainers.PostgreSql package is required. " +
            "Install via: dotnet add package Testcontainers.PostgreSql");
    }
}

/// <summary>
/// Stub interface for test containers.
/// Replace with actual Testcontainers.IContainer when packages are installed.
/// </summary>
public interface ITestContainer : IAsyncDisposable
{
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    string GetConnectionString();
}
