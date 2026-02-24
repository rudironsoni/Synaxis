namespace Synaxis.TestUtilities.Factories;

using Microsoft.Extensions.Logging;

/// <summary>
/// Factory for creating and managing isolated test containers.
/// Ensures each test collection gets its own isolated Docker container.
/// </summary>
public class TestContainerFactory : IAsyncDisposable
{
    private static readonly Lazy<TestContainerFactory> _instance = new (() => new TestContainerFactory());
    private readonly Dictionary<string, object> _containers = new ();
    private readonly ILogger<TestContainerFactory> _logger;
    private bool _disposed;

    private TestContainerFactory()
    {
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<TestContainerFactory>();
    }

    public static TestContainerFactory Instance => _instance.Value;

    /// <summary>
    /// Gets or creates an isolated container for the specified test collection.
    /// </summary>
    /// <typeparam name="TContainer">The container type</typeparam>
    /// <param name="collectionId">The test collection ID</param>
    /// <param name="factory">The factory function to create the container</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The container instance</returns>
    public async Task<TContainer> GetContainerAsync<TContainer>(
        string collectionId,
        Func<Task<TContainer>> factory,
        CancellationToken cancellationToken = default)
    {
        var containerKey = $"{typeof(TContainer).Name}_{collectionId}";

        if (_containers.TryGetValue(containerKey, out var existingContainer))
        {
            _logger.LogDebug("Reusing existing container '{ContainerKey}'", containerKey);
            return (TContainer)existingContainer;
        }

        _logger.LogInformation("Creating new container '{ContainerKey}'", containerKey);
        var container = await factory();
        _containers[containerKey] = container!;

        return container;
    }

    /// <summary>
    /// Gets a unique container name for the specified test collection.
    /// </summary>
    /// <param name="collectionId">The test collection ID</param>
    /// <param name="containerType">The container type</param>
    /// <returns>Unique container name</returns>
    public static string GetContainerName(string collectionId, string containerType)
    {
        return $"synaxis-test-{containerType.ToLower()}-{collectionId}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing TestContainerFactory, cleaning up {Count} containers", _containers.Count);

        foreach (var container in _containers.Values)
        {
            if (container is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (container is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _containers.Clear();
        _disposed = true;
    }
}
