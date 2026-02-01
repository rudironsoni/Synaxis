using BenchmarkDotNet.Running;
using Synaxis.Benchmarks;

BenchmarkRunner.Run<ProviderRoutingBenchmarks>();
BenchmarkRunner.Run<ConfigurationLoadingBenchmarks>();
BenchmarkRunner.Run<JsonSerializationBenchmarks>();
