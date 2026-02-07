// <copyright file="Program.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Benchmarks;

using BenchmarkDotNet.Running;

internal static class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<ProviderRoutingBenchmarks>();
        BenchmarkRunner.Run<ConfigurationLoadingBenchmarks>();
        BenchmarkRunner.Run<JsonSerializationBenchmarks>();
    }
}
