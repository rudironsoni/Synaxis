# ADR-007: Qdrant Vector Database Integration

## Status
**Accepted** | 2026-02-03

## Context

The Token Optimization system requires semantic caching to avoid redundant LLM API calls. Semantic caching needs to:
- Store embeddings of past requests
- Find similar requests using vector similarity search
- Scale to millions of cached responses
- Provide sub-100ms lookup latency

We evaluated vector database options and needed a solution that integrates cleanly with our Docker-based deployment.

## Decision

Adopt **Qdrant** as our vector database for semantic caching.

### Selection Criteria

| Criteria | Qdrant | Pinecone | Weaviate |
|----------|--------|----------|----------|
| Self-hosted | Yes | No | Yes |
| Docker Support | Native | N/A | Complex |
| gRPC API | Yes | No | Yes |
| Metadata Filtering | Yes | Yes | Yes |
| Performance | Excellent | Excellent | Good |
| License | Apache 2.0 | Commercial | BSD |

### Architecture

**Collection Design:**
```
Collection: synaxis_semantic_cache
- Vector dimension: 1536 (OpenAI embeddings)
- Distance metric: Cosine similarity
- Payload fields:
  - model: string (provider:model identifier)
  - response: string (cached completion)
  - timestamp: datetime
  - tenant_id: string
  - user_id: string
  - hit_count: integer
```

**Embedding Pipeline:**
1. Normalize and hash request content
2. Generate embedding via configured provider
3. Query Qdrant with similarity threshold (0.85)
4. Cache miss → call LLM → store in Qdrant
5. Cache hit → return cached response + increment hit_count

### Deployment

**Docker Compose Service:**
```yaml
qdrant:
  image: qdrant/qdrant:latest
  ports:
    - "6333:6333"  # REST API
    - "6334:6334"  # gRPC
    - "9091:9091"  # Metrics
  volumes:
    - qdrant_storage:/qdrant/storage
```

**Storage Configuration:**
- Persistent volume for data durability
- Snapshot support for backups
- Configurable retention policies

## Operational Tooling

### Backup and Restore Scripts

**qdrant-backup.sh:**
- Creates point-in-time snapshots
- Downloads snapshots to backup directory
- Implements retention policy (default 24h)
- Configurable via environment variables

**qdrant-restore.sh:**
- Restores from snapshot files
- Validates collection integrity
- Supports point-in-time recovery

**qdrant-health-check.sh:**
- REST API connectivity check
- Collection existence verification
- Disk space monitoring
- Custom metric collection

### Monitoring

**Prometheus Metrics (port 9091):**
- `qdrant_collection_vectors` - Vector count per collection
- `qdrant_collection_fragments` - Storage fragmentation
- Request latency and throughput

**Grafana Dashboard:**
- Vector count trends
- Query performance (p50, p95, p99)
- Storage utilization
- Cache hit rate correlation

## Consequences

### Positive
- **Performance**: Sub-50ms similarity search for millions of vectors
- **Cost Savings**: Semantic cache reduces API calls by 20-40%
- **Privacy**: Self-hosted, data never leaves our infrastructure
- **Flexibility**: Rich metadata filtering for multi-tenant queries

### Negative
- **Operational Complexity**: Additional service to monitor and maintain
- **Storage Growth**: Unbounded growth without retention policies
- **Embedding Costs**: Cache misses still require embedding generation
- **Cold Start**: Empty cache provides no benefit initially

## Implementation Notes

- Qdrant client uses gRPC for performance (fallback to REST)
- Collection created on application startup if missing
- Batch upserts for improved throughput
- Async eviction of stale entries

## Related ADRs
- ADR-004: Token Optimization Architecture (semantic caching)
- ADR-005: Observability and Monitoring (Qdrant metrics)
