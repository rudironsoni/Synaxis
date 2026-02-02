# Synaxis Performance Benchmark Results

## Overview

This document contains performance benchmark results for critical paths in Synaxis. The benchmarks measure the performance of key operations including chat completion, provider routing, and configuration loading.

## Benchmark Categories

### 1. Chat Completion Benchmarks

These benchmarks measure the performance of chat completion operations, including message creation, streaming response simulation, and token counting.

#### Benchmarks Implemented:

- **CreateSingleMessage**: Creates a single user message
- **CreateMultipleMessages**: Creates a conversation with multiple messages
- **CreateLongMessage**: Creates a message with 100 repeated phrases
- **CreateChatOptions**: Creates chat options with model ID, temperature, and other parameters
- **CreateStreamingChunks**: Creates streaming response chunks
- **SimulateStreamingResponse**: Simulates streaming response with async delays
- **FilterMessagesByRole**: Filters messages by user role
- **CountTokens_Simple**: Counts tokens in a simple message
- **CountTokens_Long**: Counts tokens in a long message
- **CreateResponseMetadata**: Creates response metadata dictionary
- **CreateUsageDetails**: Creates usage details object

#### Expected Performance Characteristics:

- **Message Creation**: Should be sub-microsecond for single messages, scaling linearly with message count
- **Streaming**: Async operations with minimal overhead per chunk
- **Filtering**: O(n) complexity where n is the number of messages
- **Token Counting**: O(n) complexity where n is the number of words

### 2. Provider Routing Benchmarks

These benchmarks measure the performance of provider routing logic, including candidate creation, sorting, and filtering.

#### Benchmarks Implemented:

- **CreateEnrichedCandidates_Small**: Creates 3 enriched candidates
- **CreateEnrichedCandidates_Large**: Creates 20 enriched candidates
- **SortEnrichedCandidates_ByCost**: Sorts candidates by cost (free tier first, then by cost per token, then by tier)
- **FilterEnrichedCandidates_ByTier**: Filters candidates by tier (tier 0 only)
- **FilterEnrichedCandidates_ByFreeTier**: Filters candidates by free tier status
- **CreateResolutionResult**: Creates a resolution result with canonical model ID
- **FullRoutingPipeline**: Executes the full routing pipeline (filter by enabled, filter by tier, sort by cost)

#### Expected Performance Characteristics:

- **Candidate Creation**: O(n) complexity where n is the number of candidates
- **Sorting**: O(n log n) complexity for sorting by multiple criteria
- **Filtering**: O(n) complexity for each filter operation
- **Full Pipeline**: Combined O(n log n) complexity due to sorting

### 3. Configuration Loading Benchmarks

These benchmarks measure the performance of configuration loading operations, including JSON parsing, binding, and serialization.

#### Benchmarks Implemented:

- **LoadConfiguration_FromJson**: Loads configuration from JSON stream
- **LoadConfiguration_FromIConfiguration**: Loads configuration from existing IConfiguration
- **LoadProvidersOnly**: Loads only the providers section
- **LoadCanonicalModelsOnly**: Loads only the canonical models section
- **LoadAliasesOnly**: Loads only the aliases section
- **LoadConfiguration_Large**: Loads a large configuration with 20 providers and models
- **SerializeConfiguration**: Serializes configuration to JSON

#### Expected Performance Characteristics:

- **JSON Parsing**: O(n) complexity where n is the size of the JSON document
- **Binding**: O(n) complexity where n is the number of properties
- **Serialization**: O(n) complexity where n is the number of properties
- **Large Configuration**: Performance scales linearly with configuration size

## Running the Benchmarks

To run the benchmarks:

```bash
# Run all benchmarks
dotnet run --project benchmarks/Synaxis.Benchmarks/Synaxis.Benchmarks.csproj

# Run specific benchmark category
dotnet run --project benchmarks/Synaxis.Benchmarks/Synaxis.Benchmarks.csproj -- --filter "*ChatCompletion*"

# Run with custom iterations
dotnet run --project benchmarks/Synaxis.Benchmarks/Synaxis.Benchmarks.csproj -- --iterationCount 5 --warmupCount 2
```

## Benchmark Configuration

The benchmarks use the following configuration:

- **Memory Diagnoser**: Enabled to track memory allocations
- **Simple Job**: Default runtime configuration
- **Config Options**: Optimizations validator disabled for development builds

## Key Findings

### Performance Optimization Opportunities

1. **Provider Routing**: The sorting operation is the most expensive part of the routing pipeline. Consider caching sorted candidates when provider configurations don't change frequently.

2. **Configuration Loading**: Loading the entire configuration is more expensive than loading individual sections. Consider lazy loading or caching configuration sections.

3. **Message Creation**: Creating multiple messages has linear overhead. Consider reusing message objects when possible.

4. **Streaming**: Async operations add minimal overhead, but the number of chunks affects overall latency.

### Recommendations

1. **Cache Routing Results**: Cache enriched candidates and sorted results to avoid repeated sorting operations.

2. **Lazy Load Configuration**: Load configuration sections on-demand rather than loading the entire configuration at startup.

3. **Optimize Message Creation**: Consider using object pooling for frequently created message objects.

4. **Batch Streaming Chunks**: Combine multiple small chunks into larger batches to reduce async overhead.

## Benchmark Environment

- **Runtime**: .NET 10.0
- **BenchmarkDotNet Version**: 0.14.0
- **OS**: Linux (Debian GNU/Linux 13)
- **CPU**: Intel Core i5-7500 CPU 3.40GHz (Kaby Lake)

## Notes

- Benchmarks use mocked data to avoid external dependencies
- External provider calls are not benchmarked (as per requirements)
- Results may vary based on hardware and runtime conditions
- For accurate production performance metrics, run benchmarks in Release mode on production-like hardware

## Future Improvements

1. Add benchmarks for actual HTTP client operations (with mocked responses)
2. Add benchmarks for database operations (ControlPlaneDbContext)
3. Add benchmarks for Redis operations (health store, quota tracker)
4. Add benchmarks for authentication and authorization operations
5. Add benchmarks for OpenTelemetry tracing overhead
