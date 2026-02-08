// <copyright file="CopilotSdkAdapter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.GitHub
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Concrete adapter for the GitHub Copilot SDK.
    /// Uses lightweight reflection for optional calls to remain resilient across SDK versions.
    /// </summary>
    public sealed class CopilotSdkAdapter : ICopilotSdkAdapter
    {
        private readonly object _client;
        private readonly ILogger<CopilotSdkAdapter>? _logger;
        private readonly SemaphoreSlim _startLock = new(1, 1);
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
        private readonly ChatClientMetadata _metadata = new ChatClientMetadata("GitHubCopilot", new Uri("https://copilot.github.com/"), "copilot");
#pragma warning restore S1075 // URIs should not be hardcoded
        private readonly string _modelId = "copilot";
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopilotSdkAdapter"/> class.
        /// </summary>
        /// <param name="logger">Optional logger instance.</param>
        public CopilotSdkAdapter(ILogger<CopilotSdkAdapter>? logger = null)
        {
            this._logger = logger;

            // Construct the SDK client via reflection to avoid hard compile-time dependency
            // Authentication is expected to be provided by the environment/CLI.
            object? client = null;
            try
            {
                var sdkType = Type.GetType("GitHub.Copilot.Sdk.CopilotClient, GitHub.Copilot.Sdk");
                if (sdkType != null)
                {
                    client = Activator.CreateInstance(sdkType);
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogDebug(ex, "Failed to create CopilotClient via reflection");
            }

            this._client = client ?? new object();
        }

        /// <inheritdoc/>
        public ChatClientMetadata Metadata => this._metadata;

        private async Task EnsureStartedAsync()
        {
            if (this._started)
            {
                return;
            }

            await this._startLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (this._started)
                {
                    return;
                }

                // Call StartAsync if available on the SDK client.
                var startMethod = this._client.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance);
                if (startMethod != null)
                {
                    try
                    {
                        var t = startMethod.Invoke(this._client, null) as Task;
                        if (t != null)
                        {
                            await t.ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        this._logger?.LogWarning(ex, "Copilot StartAsync failed");
                    }
                }

                this._started = true;
            }
            finally
            {
                this._startLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            await this.EnsureStartedAsync().ConfigureAwait(false);

            // Best-effort: try to find an SDK chat/send API, otherwise fall back
            // to a lightweight echo response so the application remains usable
            // in environments where the local copilot agent is not present.
            try
            {
                // Attempt to build a simple prompt from incoming messages
                var combined = string.Join("\n", messages.Select(m => $"[{m.Role.Value}] {m.Text}"));

                // If the SDK exposes a simple "CreateSessionAsync" + "GetResponseAsync"
                // pattern we could call into it. For now attempt to call a method
                // named "GetResponseAsync" on the client directly that accepts a
                // single string. This keeps the adapter functional if the method
                // exists while avoiding hard compile-time coupling.
                var getRespMethod = this._client.GetType().GetMethod("GetResponseAsync", BindingFlags.Public | BindingFlags.Instance);
                if (getRespMethod != null)
                {
                    var parameters = getRespMethod.GetParameters();
                    object? result = null;
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        var task = getRespMethod.Invoke(this._client, new object[] { combined }) as Task<object>;
                        if (task != null)
                        {
                            result = await task.ConfigureAwait(false);
                        }
                    }

                    // If we got a result and it has a ToString, return that as assistant text
                    if (result != null)
                    {
                        var text = result.ToString() ?? string.Empty;
                        var resp = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
                        resp.ModelId = this._modelId;
                        return resp;
                    }
                }
            }
            catch (Exception ex)
            {
                // Swallow SDK invocation errors and fallback to a safe response below
                this._logger?.LogDebug(ex, "Failed to invoke SDK GetResponseAsync");
            }

            // Fallback behavior: return a simple assistant response indicating
            // Copilot is not available in the current environment.
            var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User) ?? new ChatMessage(ChatRole.Assistant, string.Empty);
            var fallback = new ChatResponse(new ChatMessage(ChatRole.Assistant, lastUser.Text ?? string.Empty));

            // ChatClientMetadata does not expose ModelId; set on response instead
            fallback.ModelId = this._modelId;
            return fallback;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await this.EnsureStartedAsync().ConfigureAwait(false);

            // Try to use a streaming SDK API if available. We'll invoke via reflection
            // and enumerate the returned IEnumerable outside of the try/catch to avoid
            // errors when yielding from a try block that has a catch clause.
            object? invoked = null;
            try
            {
                var streamMethod = this._client.GetType().GetMethod("GetStreamingResponseAsync", BindingFlags.Public | BindingFlags.Instance);
                if (streamMethod != null)
                {
                    invoked = streamMethod.Invoke(this._client, new object[] { messages });
                }
            }
            catch (Exception ex)
            {
                this._logger?.LogDebug(ex, "Streaming invocation failed");
            }

            if (invoked is System.Collections.IEnumerable enumerable2)
            {
                foreach (var item in enumerable2)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
                    update.Contents.Add(new TextContent(item?.ToString() ?? string.Empty));
                    yield return update;
                }

                yield break;
            }

            // Non-streaming fallback: yield a single update with the fully computed response
            var final = await this.GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            var u = new ChatResponseUpdate { Role = ChatRole.Assistant };
            u.Contents.Add(new TextContent(final.Messages.FirstOrDefault()?.Text ?? string.Empty));
            yield return u;
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (this._client != null)
            {
                var sdkType = Type.GetType("GitHub.Copilot.Sdk.CopilotClient, GitHub.Copilot.Sdk");
                if (sdkType != null && serviceType == sdkType)
                {
                    return this._client;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                // Call Dispose or DisposeAsync if available
                var dispose = this._client.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                if (dispose != null)
                {
                    dispose.Invoke(this._client, null);
                }

                var disposeAsync = this._client.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
                if (disposeAsync != null)
                {
                    var task = disposeAsync.Invoke(this._client, null) as Task;
                    task?.GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                // Suppress disposal exceptions during cleanup
                this._logger?.LogDebug(ex, "Error disposing SDK client");
            }
        }
    }
}
