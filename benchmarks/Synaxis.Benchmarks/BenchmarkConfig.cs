using BenchmarkDotNet.Configs;

namespace Synaxis.Benchmarks;

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
