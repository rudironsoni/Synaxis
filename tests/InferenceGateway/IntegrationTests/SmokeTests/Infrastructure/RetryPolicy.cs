// <copyright file="RetryPolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    public class RetryPolicy
    {
        private readonly int _maxRetries;
        private readonly int _initialDelayMs;
        private readonly double _backoffMultiplier;
        private readonly Random _rng = new Random();

        public RetryPolicy(int maxRetries, int initialDelayMs, double backoffMultiplier)
        {
            this._maxRetries = maxRetries;
            this._initialDelayMs = initialDelayMs;
            this._backoffMultiplier = backoffMultiplier;
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, Func<Exception, bool> shouldRetry)
        {
            int attempt = 0;
            int delay = this._initialDelayMs;

            while (true)
            {
                try
                {
                    return await action().ConfigureAwait(false);
                }
                catch (Exception ex) when (shouldRetry(ex) && attempt < this._maxRetries)
                {
                    attempt++;

                    // Exponential backoff with multiplier and 10% jitter
                    double jitter = 1.0 + ((this._rng.NextDouble() * 0.2) - 0.1); // between 0.9 and 1.1
                    int delayWithJitter = Math.Max(0, (int)(delay * jitter));
                    await Task.Delay(delayWithJitter).ConfigureAwait(false);
                    delay = (int)(delay * this._backoffMultiplier);
                }
            }
        }
    }
}
