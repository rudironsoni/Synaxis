```

BenchmarkDotNet v0.14.0, Debian GNU/Linux 13 (trixie)
Intel Core i5-7500 CPU 3.40GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.102
  [Host] : .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method                  | Mean | Error |
|------------------------ |-----:|------:|
| Bind_SmallConfiguration |   NA |    NA |

Benchmarks with issues:
  ConfigurationLoadingBenchmarks.Bind_SmallConfiguration: Job-LCAGCK(IterationCount=10, WarmupCount=3)
