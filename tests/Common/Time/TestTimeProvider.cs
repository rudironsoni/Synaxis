// <copyright file="TestTimeProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests.Time;

using Synaxis.Abstractions.Time;

/// <summary>
/// Test implementation that allows manual time manipulation.
/// </summary>
public sealed class TestTimeProvider : ITimeProvider
{
    private DateTime _utcNow = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime UtcNow => _utcNow;

    /// <summary>
    /// Advances time by the specified amount instantly.
    /// </summary>
    /// <param name="amount">The time amount to advance.</param>
    public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);

    /// <inheritdoc />
    public Task Delay(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        // Fast-forward time instantly without actual delay
        Advance(delay);
        return Task.CompletedTask;
    }
}
