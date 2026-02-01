```

BenchmarkDotNet v0.14.0, Debian GNU/Linux 13 (trixie)
Intel Core i5-7500 CPU 3.40GHz (Kaby Lake), 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.102
  [Host]     : .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2
  Job-VTZOQI : .NET 10.0.2 (10.0.225.61305), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method                                                 | canonicalModelCount | providerCount | Mean      | Error      | StdDev     | Gen0    | Gen1    | Allocated |
|------------------------------------------------------- |-------------------- |-------------- |----------:|-----------:|-----------:|--------:|--------:|----------:|
| **SmartRouter_GetCandidatesAsync_AliasResolution**         | **?**                   | **?**             |  **11.99 μs** |   **1.154 μs** |   **0.687 μs** |  **0.6104** |  **0.1221** |   **4.74 KB** |
| SmartRouter_GetCandidatesAsync_WithStreamingCapability | ?                   | ?             |  11.62 μs |   1.167 μs |   0.694 μs |  0.5493 |  0.1221 |   4.61 KB |
| **ModelResolver_ResolveAsync_MultipleCanonicalModels**     | **1**                   | **?**             | **880.04 μs** | **113.699 μs** |  **75.205 μs** | **15.6250** | **13.6719** |  **53.31 KB** |
| **ModelResolver_ResolveAsync_SingleCanonicalModel**        | **?**                   | **1**             | **893.24 μs** | **134.029 μs** |  **88.652 μs** | **13.6719** | **11.7188** |  **47.74 KB** |
| SmartRouter_GetCandidatesAsync_SingleProvider          | ?                   | 1             | 894.67 μs | 120.298 μs |  79.570 μs | 15.6250 | 13.6719 |  49.78 KB |
| SmartRouter_GetCandidatesAsync_MultipleProviders       | ?                   | 1             | 924.17 μs | 156.525 μs | 103.532 μs | 15.6250 | 13.6719 |  52.94 KB |
| **ModelResolver_ResolveAsync_MultipleCanonicalModels**     | **5**                   | **?**             | **879.84 μs** | **103.857 μs** |  **68.695 μs** | **17.5781** | **15.6250** |  **54.07 KB** |
| **ModelResolver_ResolveAsync_SingleCanonicalModel**        | **?**                   | **5**             | **867.44 μs** |  **87.506 μs** |  **57.880 μs** | **15.6250** | **13.6719** |  **48.66 KB** |
| SmartRouter_GetCandidatesAsync_SingleProvider          | ?                   | 5             | 926.17 μs |  98.153 μs |  64.922 μs | 15.6250 | 13.6719 |  51.25 KB |
| SmartRouter_GetCandidatesAsync_MultipleProviders       | ?                   | 5             | 921.16 μs |  86.538 μs |  51.497 μs | 17.5781 | 15.6250 |  58.52 KB |
| **ModelResolver_ResolveAsync_MultipleCanonicalModels**     | **10**                  | **?**             | **858.94 μs** | **124.881 μs** |  **74.314 μs** | **17.5781** | **15.6250** |   **54.8 KB** |
| **ModelResolver_ResolveAsync_SingleCanonicalModel**        | **?**                   | **10**            | **773.13 μs** |  **31.038 μs** |  **16.233 μs** | **15.6250** | **13.6719** |  **50.81 KB** |
| SmartRouter_GetCandidatesAsync_SingleProvider          | ?                   | 10            | 850.58 μs |  88.830 μs |  58.756 μs | 15.6250 | 13.6719 |  53.05 KB |
| SmartRouter_GetCandidatesAsync_MultipleProviders       | ?                   | 10            | 851.31 μs |  61.682 μs |  36.706 μs |  9.7656 |  1.9531 |  66.46 KB |
| **ModelResolver_ResolveAsync_SingleCanonicalModel**        | **?**                   | **13**            | **828.97 μs** |  **88.072 μs** |  **58.254 μs** | **15.6250** | **13.6719** |  **51.56 KB** |
| SmartRouter_GetCandidatesAsync_SingleProvider          | ?                   | 13            | 857.45 μs | 103.155 μs |  68.231 μs | 15.6250 | 13.6719 |  54.14 KB |
| SmartRouter_GetCandidatesAsync_MultipleProviders       | ?                   | 13            | 881.12 μs |  68.782 μs |  40.931 μs |  9.7656 |  1.9531 |  71.01 KB |
