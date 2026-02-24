namespace Synaxis.TestUtilities.Collections;

using Synaxis.TestUtilities.Factories;
using Xunit;

/// <summary>
/// Test collection definitions for organizing tests into isolated groups.
/// Each collection gets its own isolated Docker container.
/// </summary>
public static class TestCollections
{
    /// <summary>
    /// Collection for routing-related tests.
    /// </summary>
    public const string Routing = "routing-tests";

    /// <summary>
    /// Collection for health check tests.
    /// </summary>
    public const string HealthCheck = "health-check-tests";

    /// <summary>
    /// Collection for quota tracking tests.
    /// </summary>
    public const string Quota = "quota-tests";

    /// <summary>
    /// Collection for configuration tests.
    /// </summary>
    public const string Configuration = "configuration-tests";

    /// <summary>
    /// Collection for integration tests.
    /// </summary>
    public const string Integration = "integration-tests";

    /// <summary>
    /// Gets a unique collection ID for the specified collection name.
    /// </summary>
    /// <param name="collectionName">The collection name</param>
    /// <returns>Unique collection ID</returns>
    public static string GetCollectionId(string collectionName)
    {
        return $"{collectionName}-{Guid.NewGuid():N}";
    }
}

/// <summary>
/// Collection attribute for routing tests.
/// </summary>
[CollectionDefinition(TestCollections.Routing)]
public class RoutingCollection : ICollectionFixture<TestContainerFactory>
{
}

/// <summary>
/// Collection attribute for health check tests.
/// </summary>
[CollectionDefinition(TestCollections.HealthCheck)]
public class HealthCheckCollection : ICollectionFixture<TestContainerFactory>
{
}

/// <summary>
/// Collection attribute for quota tests.
/// </summary>
[CollectionDefinition(TestCollections.Quota)]
public class QuotaCollection : ICollectionFixture<TestContainerFactory>
{
}

/// <summary>
/// Collection attribute for configuration tests.
/// </summary>
[CollectionDefinition(TestCollections.Configuration)]
public class ConfigurationCollection : ICollectionFixture<TestContainerFactory>
{
}

/// <summary>
/// Collection attribute for integration tests.
/// </summary>
[CollectionDefinition(TestCollections.Integration)]
public class IntegrationCollection : ICollectionFixture<TestContainerFactory>
{
}
