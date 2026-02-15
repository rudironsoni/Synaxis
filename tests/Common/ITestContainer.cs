// <copyright file="ITestContainer.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Common.Tests;

/// <summary>
/// Stub interface for test containers.
/// Replace with actual Testcontainers.IContainer when packages are installed.
/// </summary>
public interface ITestContainer : IAsyncDisposable
{
    Task StartAsync(CancellationToken ct = default);

    Task StopAsync(CancellationToken ct = default);

    string GetConnectionString();
}
