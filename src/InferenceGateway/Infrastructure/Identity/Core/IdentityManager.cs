// <copyright file="IdentityManager.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Manages identity accounts, authentication strategies, and token refresh operations.
    /// </summary>
    public class IdentityManager : IHostedService, IDisposable
    {
        private readonly IEnumerable<IAuthStrategy> _strategies;
        private readonly ISecureTokenStore _store;
        private readonly ILogger<IdentityManager> _logger;
        private readonly Lock _lock = new Lock();
        private readonly TaskCompletionSource<bool> _initialLoadComplete = new TaskCompletionSource<bool>();
        private readonly List<IdentityAccount> _accounts = new List<IdentityAccount>();
        private Timer? _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityManager"/> class.
        /// </summary>
        /// <param name="strategies">The collection of authentication strategies.</param>
        /// <param name="store">The secure token store.</param>
        /// <param name="logger">The logger instance.</param>
        public IdentityManager(IEnumerable<IAuthStrategy> strategies, ISecureTokenStore store, ILogger<IdentityManager> logger)
        {
            this._strategies = strategies ?? Array.Empty<IAuthStrategy>();
            this._store = store ?? throw new ArgumentNullException(nameof(store));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to account authenticated events from strategies
            try
            {
                foreach (var s in this._strategies)
                {
                    s.AccountAuthenticated += async (sender, eventArgs) =>
                    {
                        try
                        {
                            await this.AddOrUpdateAccountAsync(eventArgs.Account).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, "Error adding/updating account from strategy event");
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to subscribe to auth strategy events");
            }

            // Load existing accounts from store synchronously at startup
            _ = Task.Run(async () =>
            {
                try
                {
                    var loaded = await this._store.LoadAsync().ConfigureAwait(false);
                    lock (this._lock)
                    {
                        this._accounts.Clear();
                        if (loaded != null)
                        {
                            this._accounts.AddRange(loaded);
                    }
                }

                    this._initialLoadComplete.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed to load identity accounts from store");
                    this._initialLoadComplete.TrySetException(ex);
                }
            });
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start a timer to refresh tokens periodically
            this._timer = new Timer(
                _ =>
                {
#pragma warning disable S1854 // Intentional fire-and-forget async operation
                    _ = Task.Run(async () =>
#pragma warning restore S1854
                    {
                        try
                        {
                            await this.RefreshLoopAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogError(ex, "Error in refresh loop");
                        }
                    });
                },
                null,
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits for the initial account loading to complete.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task WaitForInitialLoadAsync(CancellationToken ct = default)
        {
            return this._initialLoadComplete.Task.WaitAsync(ct);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources used by the IdentityManager.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), false if called from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._timer?.Dispose();
            }
        }

        private async Task RefreshLoopAsync()
        {
            List<IdentityAccount> toRefresh;
            lock (this._lock)
            {
                var now = DateTimeOffset.UtcNow;
                toRefresh = this._accounts.Where(a => a.ExpiresAt.HasValue && a.RefreshToken != null && a.ExpiresAt.Value <= now.AddMinutes(5)).ToList();
            }

            foreach (var acc in toRefresh)
            {
                try
                {
                    var strat = this.FindStrategyForProvider(acc.Provider);
                    if (strat == null)
                    {
                        this._logger.LogWarning("No auth strategy found for provider {Provider}", acc.Provider);
                        continue;
                    }

                    var tokenResp = await strat.RefreshTokenAsync(acc, CancellationToken.None).ConfigureAwait(false);
                    if (tokenResp != null)
                    {
                        lock (this._lock)
                        {
                            acc.AccessToken = tokenResp.AccessToken ?? acc.AccessToken;
                            if (!string.IsNullOrEmpty(tokenResp.RefreshToken))
                            {
                                acc.RefreshToken = tokenResp.RefreshToken;
                            }

                            if (tokenResp.ExpiresInSeconds.HasValue)
                            {
                                acc.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResp.ExpiresInSeconds.Value);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error refreshing token for provider {Provider}", acc.Provider);
                }
            }

            // Save updated accounts
            try
            {
                List<IdentityAccount> snapshot;
                lock (this._lock)
                {
                    snapshot = this._accounts.Select(a => a).ToList();
                }

                await this._store.SaveAsync(snapshot).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to save accounts after refresh loop");
            }
        }

        private IAuthStrategy? FindStrategyForProvider(string provider)
        {
            if (string.IsNullOrEmpty(provider))
            {
                return null;
            }

            var prov = provider.Trim();

            // Try exact match on type name or substring match
            foreach (var s in this._strategies)
            {
                var name = s.GetType().Name;
                if (string.Equals(name, prov, StringComparison.OrdinalIgnoreCase) || name.Contains(prov, StringComparison.OrdinalIgnoreCase))
                {
                    return s;
                }
            }

            // Fallback: return first strategy
            return this._strategies.FirstOrDefault();
        }

        /// <summary>
        /// Initiates an authentication flow for the specified provider.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The authentication result.</returns>
        public async Task<AuthResult?> StartAuth(string provider, CancellationToken ct = default)
        {
            var strat = this.FindStrategyForProvider(provider);
            if (strat == null)
            {
                this._logger.LogWarning("No strategy found for provider {Provider}", provider);
                return new AuthResult { Status = "Error", Message = "No strategy found" };
            }

            return await strat.InitiateFlowAsync(ct).ConfigureAwait(false);
        }

        // New helper to complete auth from external callers by provider

        /// <summary>
        /// Completes an authentication flow using the provided authorization code and state.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="code">The authorization code.</param>
        /// <param name="state">The state parameter.</param>
        /// <returns>The authentication result.</returns>
        public Task<AuthResult> CompleteAuth(string provider, string code, string state)
        {
            var strat = this.FindStrategyForProvider(provider);
            if (strat == null)
            {
                this._logger.LogWarning("No strategy found for provider {Provider}", provider);
                throw new InvalidOperationException("No strategy found");
            }

            return strat.CompleteFlowAsync(code, state, CancellationToken.None);
        }

        /// <summary>
        /// Retrieves an access token for the specified provider.
        /// </summary>
        /// <param name="provider">The provider name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The access token or null if not found.</returns>
        public async Task<string?> GetToken(string provider, CancellationToken ct = default)
        {
            IdentityAccount? acc;
            lock (this._lock)
            {
                acc = this._accounts.FirstOrDefault(a => string.Equals(a.Provider, provider, StringComparison.OrdinalIgnoreCase));
            }

            if (acc == null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            if (acc.ExpiresAt.HasValue && acc.ExpiresAt.Value <= now && !string.IsNullOrEmpty(acc.RefreshToken))
            {
                try
                {
                    var strat = this.FindStrategyForProvider(provider);
                    if (strat != null)
                    {
                        var tokenResp = await strat.RefreshTokenAsync(acc, ct).ConfigureAwait(false);
                        if (tokenResp != null)
                        {
                            lock (this._lock)
                            {
                                acc.AccessToken = tokenResp.AccessToken ?? acc.AccessToken;
                                if (!string.IsNullOrEmpty(tokenResp.RefreshToken))
                                {
                                    acc.RefreshToken = tokenResp.RefreshToken;
                                }

                                if (tokenResp.ExpiresInSeconds.HasValue)
                                {
                                    acc.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResp.ExpiresInSeconds.Value);
                                }
                            }

                            await this._store.SaveAsync(this._accounts.Select(a => a).ToList()).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed refreshing token for provider {Provider}", provider);
                }
            }

            return acc.AccessToken;
        }

        /// <summary>
        /// Adds or updates an identity account in the manager.
        /// </summary>
        /// <param name="account">The account to add or update.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task AddOrUpdateAccountAsync(IdentityAccount account)
        {
            lock (this._lock)
            {
                var existing = this._accounts.FirstOrDefault(a => string.Equals(a.Provider, account.Provider, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Id, account.Id, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.AccessToken = account.AccessToken;
                    existing.RefreshToken = account.RefreshToken;
                    existing.ExpiresAt = account.ExpiresAt;
                    existing.Email = account.Email;
                    existing.Properties = account.Properties;
                }
                else
                {
                    this._accounts.Add(account);
                }
            }

            try
            {
                await this._store.SaveAsync(this._accounts.Select(a => a).ToList()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to save accounts after add/update");
            }
        }
    }
}
