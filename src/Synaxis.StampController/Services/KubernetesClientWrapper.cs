// <copyright file="KubernetesClientWrapper.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Services;

using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Wrapper around KubernetesClient for basic ConfigMap operations.
/// </summary>
public sealed class KubernetesClientWrapper : IDisposable
{
    private readonly IKubernetes _client;
    private readonly ILogger<KubernetesClientWrapper> _logger;
    private readonly string _namespace;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KubernetesClientWrapper"/> class.
    /// </summary>
    /// <param name="options">The Kubernetes client options.</param>
    /// <param name="logger">The logger instance.</param>
    public KubernetesClientWrapper(
        IOptions<KubernetesClientOptions> options,
        ILogger<KubernetesClientWrapper> logger)
    {
        var optionsValue = options.Value;
        this._logger = logger;
        this._namespace = optionsValue.Namespace ?? "default";

        var config = KubernetesClientConfiguration.IsInCluster()
            ? KubernetesClientConfiguration.InClusterConfig()
            : KubernetesClientConfiguration.BuildConfigFromConfigFile();

        this._client = new Kubernetes(config);
        this._logger.LogInformation("Kubernetes client initialized for namespace: {Namespace}", this._namespace);
    }

    /// <summary>
    /// Lists all ConfigMaps with the managed-by label.
    /// </summary>
    /// <returns>A list of ConfigMaps.</returns>
    public async Task<IList<V1ConfigMap>> ListConfigMapsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configMaps = await this._client.CoreV1.ListNamespacedConfigMapAsync(
                this._namespace,
                labelSelector: "app.kubernetes.io/managed-by=synaxis-stamp-controller",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return configMaps.Items;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to list ConfigMaps in namespace: {Namespace}", this._namespace);
            throw;
        }
    }

    /// <summary>
    /// Gets a specific ConfigMap by name.
    /// </summary>
    /// <param name="name">The name of the ConfigMap.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The ConfigMap if found; otherwise, null.</returns>
    public async Task<V1ConfigMap?> GetConfigMapAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            return await this._client.CoreV1.ReadNamespacedConfigMapAsync(name, this._namespace, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to get ConfigMap: {Name} in namespace: {Namespace}", name, this._namespace);
            throw;
        }
    }

    /// <summary>
    /// Creates a new ConfigMap.
    /// </summary>
    /// <param name="configMap">The ConfigMap to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created ConfigMap.</returns>
    public async Task<V1ConfigMap> CreateConfigMapAsync(V1ConfigMap configMap, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._client.CoreV1.CreateNamespacedConfigMapAsync(configMap, this._namespace, cancellationToken: cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Created ConfigMap: {Name} in namespace: {Namespace}", result.Metadata.Name, this._namespace);
            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to create ConfigMap: {Name} in namespace: {Namespace}", configMap.Metadata.Name, this._namespace);
            throw;
        }
    }

    /// <summary>
    /// Updates an existing ConfigMap.
    /// </summary>
    /// <param name="configMap">The ConfigMap to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated ConfigMap.</returns>
    public async Task<V1ConfigMap> UpdateConfigMapAsync(V1ConfigMap configMap, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await this._client.CoreV1.ReplaceNamespacedConfigMapAsync(
                configMap,
                configMap.Metadata.Name,
                this._namespace,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            this._logger.LogInformation("Updated ConfigMap: {Name} in namespace: {Namespace}", result.Metadata.Name, this._namespace);
            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to update ConfigMap: {Name} in namespace: {Namespace}", configMap.Metadata.Name, this._namespace);
            throw;
        }
    }

    /// <summary>
    /// Deletes a ConfigMap.
    /// </summary>
    /// <param name="name">The name of the ConfigMap to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    public async Task DeleteConfigMapAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            await this._client.CoreV1.DeleteNamespacedConfigMapAsync(name, this._namespace, cancellationToken: cancellationToken).ConfigureAwait(false);
            this._logger.LogInformation("Deleted ConfigMap: {Name} in namespace: {Namespace}", name, this._namespace);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            this._logger.LogWarning(ex, "ConfigMap not found for deletion: {Name} in namespace: {Namespace}", name, this._namespace);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to delete ConfigMap: {Name} in namespace: {Namespace}", name, this._namespace);
            throw;
        }
    }

    /// <summary>
    /// Disposes the Kubernetes client.
    /// </summary>
    public void Dispose()
    {
        if (!this._disposed)
        {
            this._client.Dispose();
            this._disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
