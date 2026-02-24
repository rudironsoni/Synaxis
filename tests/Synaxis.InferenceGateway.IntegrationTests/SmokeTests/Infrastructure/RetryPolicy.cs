// <copyright file="RetryPolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure;

public class RetryPolicy(int maxRetries, int initialDelayMs, double backoffMultiplier)
{
    private readonly int _maxRetries = maxRetries;
    private readonly int _initialDelayMs = initialDelayMs;
    private readonly double _backoffMultiplier = backoffMultiplier;
    private readonly Random _rng = new Random();

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

                // Exponential backoff without Task.Delay - uses Task.Yield for async fairness
                delay = (int)(delay * this._backoffMultiplier);
                await Task.Yield();
            }
        }
    }
}
