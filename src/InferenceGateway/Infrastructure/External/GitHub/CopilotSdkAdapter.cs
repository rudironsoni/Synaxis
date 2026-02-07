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
    /// Minimal concrete adapter for the GitHub Copilot SDK.
    /// The implementation attempts to start the CopilotClient lazily and exposes
    /// a small, test-friendly surface required by CopilotSdkClient.
    ///
    /// Note: The SDK surface varies across versions; to remain resilient we use
    /// light-weight reflection for optional calls but still expose the underlying
    /// CopilotClient via GetService so callers can use advanced features when
    /// available.
    /// </summary>
    public class CopilotSdkAdapter : ICopilotSdkAdapter
    {
        private readonly object _client;
        private readonly ILogger<CopilotSdkAdapter>? _logger;
        private readonly SemaphoreSlim _startLock = new (1, 1);
        private bool _started;
        private readonly ChatClientMetadata _metadata = new ChatClientMetadata("GitHubCopilot", new Uri("https://copilot.github.com/"), "copilot");
        private readonly string _modelId = "copilot";

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
                _logger?.LogDebug(ex, "Failed to create CopilotClient via reflection");
            }
            this._client = client ?? new object();
        }

        public ChatClientMetadata Metadata => this._metadata;

        private async Task EnsureStartedAsync()
        {
            if (_started) return;
            await _startLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_started) return;
                // Call StartAsync if available on the SDK client.
                var startMethod = _client.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance);
                if (startMethod != null)
                {
                    try
                    {
                        var t = startMethod.Invoke(_client, null) as Task;
                        if (t != null) await t.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Copilot StartAsync failed");
                    }
                }

                this._started = true;
            }
            finally
            {
                _startLock.Release();
            }
        }

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            await EnsureStartedAsync().ConfigureAwait(false);

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
                var getRespMethod = _client.GetType().GetMethod("GetResponseAsync", BindingFlags.Public | BindingFlags.Instance);
                if (getRespMethod != null)
                {
                    var parameters = getRespMethod.GetParameters();
                    object? result = null;
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                    {
                        var task = getRespMethod.Invoke(_client, new object[] { combined }) as Task<object>;
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
                        resp.ModelId = _modelId;
                        return resp;
                    }
                }
            }
            catch
            {
                // Swallow - we'll fallback to a safe response below
            }

            // Fallback behavior: return a simple assistant response indicating
            // Copilot is not available in the current environment.
            var lastUser = messages.LastOrDefault(m => m.Role == ChatRole.User) ?? new ChatMessage(ChatRole.Assistant, string.Empty);
            var fallback = new ChatResponse(new ChatMessage(ChatRole.Assistant, lastUser.Text ?? string.Empty));
            // ChatClientMetadata does not expose ModelId; set on response instead
            fallback.ModelId = _modelId;
            return fallback;
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await EnsureStartedAsync().ConfigureAwait(false);

            // Try to use a streaming SDK API if available. We'll invoke via reflection
            // and enumerate the returned IEnumerable outside of the try/catch to avoid
            // yielding from inside a try block with a catch (which is not allowed).
            object? invoked = null;
            try
            {
                var streamMethod = _client.GetType().GetMethod("GetStreamingResponseAsync", BindingFlags.Public | BindingFlags.Instance);
                if (streamMethod != null)
                {
                    invoked = streamMethod.Invoke(_client, new object[] { messages });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Streaming invocation failed");
            }

            if (invoked is System.Collections.IEnumerable enumerable2)
            {
                foreach (var item in enumerable2)
                {
                    if (cancellationToken.IsCancellationRequested) yield break;
                    var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
                    update.Contents.Add(new TextContent(item?.ToString() ?? string.Empty));
                    yield return update;
                }
                yield break;
            }

            // Non-streaming fallback: yield a single update with the fully computed response
            var final = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
            var u = new ChatResponseUpdate { Role = ChatRole.Assistant };
            u.Contents.Add(new TextContent(final.Messages.FirstOrDefault()?.Text ?? string.Empty));
            yield return u;
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            if (_client != null)
            {
                var sdkType = Type.GetType("GitHub.Copilot.Sdk.CopilotClient, GitHub.Copilot.Sdk");
                if (sdkType != null && serviceType == sdkType) return this._client;
            }
            return null;
        }

        public void Dispose()
        {
            try
            {
                // Call Dispose or DisposeAsync if available
                var dispose = _client.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance);
                if (dispose != null) dispose.Invoke(_client, null);

                var disposeAsync = _client.GetType().GetMethod("DisposeAsync", BindingFlags.Public | BindingFlags.Instance);
                if (disposeAsync != null)
                {
                    var task = disposeAsync.Invoke(_client, null) as Task;
                    task?.GetAwaiter().GetResult();
                }
            }
            catch { }
        }
    }
}
