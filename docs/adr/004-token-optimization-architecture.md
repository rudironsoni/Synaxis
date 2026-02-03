# ADR-004: Token Optimization Architecture

## Status
**Accepted** | 2026-02-03

## Context

The Inference Gateway processes thousands of LLM requests daily, with token consumption being the primary cost driver. Token usage has three dimensions:
- **Input tokens**: Prompt + context sent to the model
- **Output tokens**: Generated completions
- **Wasted tokens**: Duplicated prompts, redundant context, unnecessary API calls

We needed a comprehensive strategy to optimize token usage across all dimensions while maintaining response quality and minimizing latency impact.

## Decision

Implement a **multi-layered Token Optimization System** with four complementary strategies:

### 1. Semantic Caching
- **Vector-based cache** using Qdrant for semantic similarity matching
- Cache responses when similarity > 0.85 threshold
- Hierarchical cache keys: tenant + user + model + content hash
- TTL-based eviction with configurable retention periods

### 2. Prompt Compression
- **Semantic compression** to reduce prompt length while preserving meaning
- Remove redundant whitespace, comments, and boilerplate
- Summarize long context windows when possible
- Compression ratio target: 20-40% reduction without quality loss

### 3. Session Affinity
- **Conversation continuity** by routing similar requests to same model/provider
- Track conversation patterns and model preferences per user/session
- Pre-warm connections to reduce cold-start latency
- Session stickiness with fallback to load balancing

### 4. Request Deduplication
- **Exact-match deduplication** for concurrent identical requests
- Coalesce in-flight requests to the same prompt
- Return cached result to all waiting clients
- Automatic retry with fresh request on cache miss

## Configuration Hierarchy

```
System Defaults
    ↓
Tenant Configuration (override)
    ↓
User Configuration (override)
    ↓
Request-level Flags (override)
```

All configuration levels support:
- Enable/disable per feature
- Threshold tuning (similarity, compression ratio)
- TTL and retention settings
- Provider-specific overrides

## Consequences

### Positive
- **Cost Reduction**: 30-50% reduction in token consumption
- **Latency Improvement**: Cache hits served in <50ms
- **Better UX**: Faster responses for common queries
- **Scalability**: Reduced load on downstream providers

### Negative
- **Added Complexity**: Four interdependent subsystems to maintain
- **Infrastructure Cost**: Qdrant cluster for vector storage
- **Cache Invalidation**: Complex invalidation logic for semantic matches
- **Observability**: Need detailed metrics to debug cache behavior

## Implementation Notes

- TokenOptimizationConfigurationResolver handles hierarchical config resolution
- TokenOptimizingChatClient decorates IChatClient with optimization logic
- Redis used for conversation state and session tracking
- Qdrant for vector-based semantic caching

## Related ADRs
- ADR-001: Stream-Native CQRS (request/response flow)
- ADR-002: Tiered Routing Strategy (provider selection)
- ADR-007: Qdrant Integration (vector storage)
