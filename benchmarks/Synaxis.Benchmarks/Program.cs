using BenchmarkDotNet.Running;
using Synaxis.Benchmarks;

// Run all benchmarks
BenchmarkRunner.Run<ChatCompletionBenchmarks>();
BenchmarkRunner.Run<ProviderRoutingBenchmarks>();
BenchmarkRunner.Run<ConfigurationLoadingBenchmarks>();
