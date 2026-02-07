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

    public sealed class IdentityManager : IHostedService, IDisposable
    {
        private readonly IEnumerable<IAuthStrategy> _strategies;
        private readonly ISecureTokenStore _store;
        private readonly ILogger<IdentityManager> _logger;
        private readonly Lock _lock = new Lock();
        private readonly TaskCompletionSource<bool> _initialLoadComplete = new TaskCompletionSource<bool>();
        private readonly List<IdentityAccount> _accounts = new List<IdentityAccount>();
        private Timer? _timer;

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
            Task.Run(async () =>
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Start a timer to refresh tokens periodically
            this._timer = new Timer(async _ => await this.RefreshLoopAsync(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Waits for the initial account loading to complete.
        /// This is primarily used in tests to ensure background loading is finished.
        /// </summary>
        public Task WaitForInitialLoadAsync(CancellationToken ct = default)
        {
            return this._initialLoadComplete.Task.WaitAsync(ct);
        }

        public void Dispose()
        {
            this._timer?.Dispose();
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

                            if (tokenResp.ExpiresInthis.Seconds.HasValue)
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
        public async Task<AuthResult> CompleteAuth(string provider, string code, string state)
        {
            var strat = this.FindStrategyForProvider(provider);
            if (strat == null)
            {
                this._logger.LogWarning("No strategy found for provider {Provider}", provider);
                throw new InvalidOperationException("No strategy found");
            }

            return await strat.CompleteFlowAsync(code, state, CancellationToken.None).ConfigureAwait(false);
        }

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

                                if (tokenResp.ExpiresInthis.Seconds.HasValue)
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
