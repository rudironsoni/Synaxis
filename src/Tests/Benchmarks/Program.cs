// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using BenchmarkDotNet.Running;

/// <summary>
/// Entry point for the benchmark application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Main entry point.
    /// </summary>
    private static void Main()
    {
        BenchmarkRunner.Run<ProviderRoutingBenchmarks>();
        BenchmarkRunner.Run<ConfigurationLoadingBenchmarks>();
        BenchmarkRunner.Run<JsonSerializationBenchmarks>();
    }
}
