# Performance Guide

> **ULTRA MISER MODE™ Optimization**: Why pay for bigger instances when you can optimize? This guide covers squeezing every last drop of performance from your free-tier infrastructure. Remember: a well-optimized cheap server beats an expensive lazy one.

Synaxis is built for speed and efficiency. This guide helps you maximize throughput, minimize latency, and make the most of your hardware—whether it's a Raspberry Pi or a cloud VM you're barely paying for.

---

## Table of Contents

- [Performance Overview](#performance-overview)
- [Benchmarks](#benchmarks)
- [Optimization Strategies](#optimization-strategies)
- [Provider Selection](#provider-selection)
- [Caching](#caching)
- [Connection Tuning](#connection-tuning)
- [Profiling](#profiling)
- [Load Testing](#load-testing)
- [Production Tuning](#production-tuning)

---

## Performance Overview

### Key Performance Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| **P50 Latency** | < 500ms | Time to first token |
| **P95 Latency** | < 2000ms | Time to first token |
| **Throughput** | > 100 req/min | Successful requests |
| **Error Rate** | < 1% | Failed requests |
| **Provider Failover** | < 100ms | Time to switch providers |

### Factors Affecting Performance

1. **Provider Latency** - The biggest factor (varies by provider)
2. **Network Distance** - Geographic proximity to providers
3. **Request Size** - Larger prompts = longer processing
4. **Streaming vs Non-Streaming** - Streaming improves perceived latency
5. **Concurrent Requests** - Affects connection pooling

---

## Benchmarks

### Baseline Performance

Tested on modest hardware (2 vCPU, 4GB RAM):

| Provider | Avg Latency | P95 Latency | Success Rate |
|----------|-------------|-------------|--------------|
| Groq | 120ms | 250ms | 99.5% |
| Together AI | 450ms | 1200ms | 98.0% |
| Cloudflare | 200ms | 500ms | 99.0% |
| DeepInfra | 800ms | 2500ms | 95.0% |
| Fireworks | 350ms | 900ms | 97.5% |

*Note: Actual performance varies based on model, time of day, and your geographic location.*

### Running Your Own Benchmarks

```bash
#!/bin/bash
# benchmark.sh - Simple load test

API_URL="http://localhost:8080/v1/chat/completions"
MODEL="llama-3.3-70b-versatile"
CONCURRENT=10
REQUESTS=100

echo "Running benchmark: ${CONCURRENT} concurrent, ${REQUESTS} total"

# Generate test payload
cat > /tmp/payload.json << 'EOF'
{
  "model": "llama-3.3-70b-versatile",
  "messages": [{"role": "user", "content": "Say hello in 5 words"}],
  "max_tokens": 20
}
EOF

# Run with Apache Bench (apt install apache2-utils)
ab -n $REQUESTS -c $CONCURRENT \
  -T 'application/json' \
  -p /tmp/payload.json \
  -g /tmp/benchmark.tsv \
  $API_URL

# Analyze results
echo "=== Results ==="
echo "Requests per second: $(grep 'Requests per second' /tmp/benchmark.tsv | awk '{print $4}')"
echo "Mean latency: $(grep 'Time per request' /tmp/benchmark.tsv | head -1 | awk '{print $4}') ms"
echo "Failed requests: $(grep 'Failed requests' /tmp/benchmark.tsv | awk '{print $3}')"
```

### Using k6 for Advanced Testing

```javascript
// benchmark.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 10 },   // Ramp up
    { duration: '3m', target: 10 },   // Steady state
    { duration: '1m', target: 20 },   // Spike
    { duration: '1m', target: 0 },    // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000'], // 95% under 2s
    http_req_failed: ['rate<0.01'],     // <1% errors
  },
};

export default function () {
  const payload = JSON.stringify({
    model: 'llama-3.3-70b-versatile',
    messages: [{ role: 'user', content: 'Hello' }],
    max_tokens: 50,
  });

  const res = http.post('http://localhost:8080/v1/chat/completions', payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 2s': (r) => r.timings.duration < 2000,
  });

  sleep(1);
}
```

Run with: `k6 run benchmark.js`

---

## Optimization Strategies

### 1. Provider Tier Configuration

Configure providers by latency and reliability:

```json
{
  "Synaxis": {
    "InferenceGateway": {
      "Providers": {
        "Groq": {
          "Enabled": true,
          "Type": "groq",
          "Key": "GROQ_API_KEY",
          "Tier": 0,
          "Models": ["llama-3.3-70b-versatile"]
        },
        "Cloudflare": {
          "Enabled": true,
          "Type": "cloudflare",
          "Key": "CF_API_TOKEN",
          "Tier": 1,
          "Models": ["@cf/meta/llama-3.1-8b-instruct"]
        },
        "DeepInfra": {
          "Enabled": true,
          "Type": "openai",
          "Endpoint": "https://api.deepinfra.com/v1/openai",
          "Key": "DEEPINFRA_KEY",
          "Tier": 2,
          "Models": ["meta-llama/Llama-3.3-70B-Instruct"]
        }
      }
    }
  }
}
```

**Tier Strategy:**
- **Tier 0**: Fastest providers (Groq, Cloudflare)
- **Tier 1**: Reliable mid-latency providers
- **Tier 2**: Slower but higher quota providers
- **Tier 3+**: Emergency fallbacks

### 2. Connection Pooling

Optimize HTTP client settings:

```csharp
// In Program.cs
builder.Services.AddHttpClient("ProviderClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Connection pooling
    MaxConnectionsPerServer = 100,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    
    // Keep-alive
    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
    EnableMultipleHttp2Connections = true,
    
    // Performance
    UseProxy = false,  // If no proxy needed
});
```

### 3. Request Batching

For high-volume scenarios, batch similar requests:

```csharp
// Batch multiple prompts into single request when possible
var batchedRequest = new ChatCompletionRequest
{
    Model = "llama-3.3-70b-versatile",
    Messages = new[]
    {
        new Message { Role = "system", Content = "Process these items:" },
        new Message { Role = "user", Content = string.Join("\n", items) }
    }
};
```

### 4. Streaming Optimization

Always use streaming for better perceived performance:

```bash
# Non-streaming (waits for full response)
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"llama-3.3-70b-versatile","messages":[{"role":"user","content":"Write a story"}]}'
# Time: 5-10 seconds before any output

# Streaming (first token in ~200ms)
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"llama-3.3-70b-versatile","stream":true,"messages":[{"role":"user","content":"Write a story"}]}'
# Time: First token in 200ms, complete in 5-10s
```

---

## Provider Selection

### Latency-Based Routing

Synaxis automatically routes to the lowest-latency healthy provider:

```csharp
// SmartRouter prioritizes by:
// 1. Health status (healthy first)
// 2. Tier (lower tier = higher priority)
// 3. Latency (lower latency preferred)
// 4. Cost (if cost data available)
```

### Geographic Optimization

Choose providers closest to your users:

| Region | Recommended Providers |
|--------|----------------------|
| US East | Groq, Together AI, Fireworks |
| US West | Groq, Cloudflare |
| Europe | DeepInfra, Cohere |
| Asia | Cloudflare, Together AI |

### Provider Health Awareness

The system automatically excludes unhealthy providers:

```bash
# Check which providers are currently healthy
curl -s http://localhost:8080/admin/health \
  -H "Authorization: Bearer $TOKEN" \
  | jq '.providers[] | select(.status == "online") | .name'
```

---

## Caching

### Response Caching

Enable caching for identical requests:

```csharp
// In Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("chat", builder => builder
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByQuery("model")
        .SetVaryByHeader("Authorization"));
});

// Apply to endpoints
app.MapPost("/v1/chat/completions", HandleChatCompletion)
   .CacheOutput("chat");
```

**When to Cache:**
- Identical prompts (e.g., system initialization)
- Non-personalized content
- Expensive operations with stable outputs

**When NOT to Cache:**
- User-specific data
- Real-time information requests
- Large context windows

### Provider Response Caching

Cache provider metadata to reduce API calls:

```csharp
// Cache model lists (rarely change)
builder.Services.AddMemoryCache();

// In provider service
var models = await _cache.GetOrCreateAsync(
    $"models:{providerKey}",
    async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        return await FetchModelsFromProvider(providerKey);
    });
```

---

## Connection Tuning

### Database Optimization

**PostgreSQL Connection Pool:**
```json
{
  "Synaxis": {
    "ControlPlane": {
      "ConnectionString": "Host=localhost;Database=synaxis;Username=postgres;Password=postgres;Maximum Pool Size=50;Connection Lifetime=300;"
    }
  }
}
```

**Recommended Pool Sizes:**
- Small deployment (< 100 req/min): 10-20 connections
- Medium deployment (100-1000 req/min): 20-50 connections
- Large deployment (> 1000 req/min): 50-100 connections

### Redis Optimization

**Connection Multiplexing:**
```csharp
// In Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(new ConfigurationOptions
    {
        EndPoints = { "localhost:6379" },
        AbortOnConnectFail = false,
        ConnectTimeout = 5000,
        SyncTimeout = 5000,
        AsyncTimeout = 5000,
        ReconnectRetryPolicy = new LinearRetry(5000),
    }));
```

### HTTP/2 Enablement

Enable HTTP/2 for better multiplexing:

```csharp
// In Program.cs
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

---

## Profiling

### Built-in Diagnostics

Enable request timing logs:

```json
{
  "Logging": {
    "LogLevel": {
      "Synaxis": "Debug"
    }
  }
}
```

### Using dotnet-trace

```bash
# Install dotnet-trace
dotnet tool install --global dotnet-trace

# Collect trace
dotnet-trace collect --process-id $(pgrep -f "Synaxis.WebApi") --duration 00:01:00

# Analyze with PerfView or Visual Studio
```

### Memory Profiling

```bash
# Check memory usage
dotnet-counters monitor --process-id $(pgrep -f "Synaxis.WebApi")

# Key counters to watch:
# - Working Set: Should be stable
# - GC Heap Size: Should not grow unbounded
# - ThreadPool Queue Length: Should be near 0
```

### Request Tracing

Add correlation IDs to trace request flow:

```bash
# Request with trace ID
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Request-ID: perf-test-001" \
  -d '{...}'

# Find in logs
docker compose logs webapi | grep "perf-test-001"
```

---

## Load Testing

### Gradual Load Increase

```bash
#!/bin/bash
# gradual-load-test.sh

for concurrent in 1 5 10 20 50; do
  echo "Testing with $concurrent concurrent requests..."
  
  ab -n 100 -c $concurrent \
    -T 'application/json' \
    -p /tmp/payload.json \
    http://localhost:8080/v1/chat/completions 2>&1 \
    | grep -E "(Requests per second|Time per request|Failed)"
  
  sleep 5
done
```

### Sustained Load Test

```bash
# 10 minutes of sustained load
ab -n 6000 -c 20 -t 600 \
  -T 'application/json' \
  -p /tmp/payload.json \
  http://localhost:8080/v1/chat/completions
```

### Provider Failover Test

```bash
# Test failover by blocking a provider
iptables -A OUTPUT -p tcp --dport 443 -d api.groq.com -j DROP

# Run requests - should failover to other providers
for i in {1..10}; do
  curl -s -w "%{http_code}\n" \
    http://localhost:8080/v1/chat/completions \
    -H "Content-Type: application/json" \
    -d '{"model":"llama-3.3-70b-versatile","messages":[{"role":"user","content":"Hi"}]}'
  sleep 1
done

# Restore connectivity
iptables -D OUTPUT -p tcp --dport 443 -d api.groq.com -j DROP
```

---

## Production Tuning

### Environment-Specific Settings

**Development:**
```json
{
  "Logging": { "LogLevel": { "Default": "Debug" } },
  "Synaxis": {
    "InferenceGateway": {
      "RequestTimeout": 120
    }
  }
}
```

**Production:**
```json
{
  "Logging": { "LogLevel": { "Default": "Warning" } },
  "Synaxis": {
    "InferenceGateway": {
      "RequestTimeout": 60,
      "MaxConcurrentRequests": 100,
      "EnableResponseCaching": true
    }
  }
}
```

### Container Resource Limits

```yaml
# docker-compose.yml
services:
  webapi:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
```

### Reverse Proxy Optimization

**Nginx Configuration:**
```nginx
upstream synaxis {
    least_conn;  # Load balance by least connections
    server localhost:8080 max_fails=3 fail_timeout=30s;
    keepalive 100;  # Keep connections open
}

server {
    location /v1/ {
        proxy_pass http://synaxis;
        proxy_http_version 1.1;
        proxy_set_header Connection "";
        proxy_buffering off;  # Required for SSE streaming
        proxy_read_timeout 120s;
    }
}
```

---

> **Remember**: In ULTRA MISER MODE™, performance isn't about throwing money at bigger servers—it's about understanding your bottlenecks and optimizing ruthlessly. A $5 VPS running optimized Synaxis can outperform a $500 unoptimized one. The best performance optimization is the one that costs nothing but thought.

---

## Performance Checklist

- [ ] Provider tiers configured by latency
- [ ] Connection pooling optimized
- [ ] HTTP/2 enabled
- [ ] Database connection pool sized correctly
- [ ] Redis connection multiplexing configured
- [ ] Response caching enabled for appropriate endpoints
- [ ] Streaming used for all chat completions
- [ ] Load tested with expected traffic patterns
- [ ] Memory usage profiled and stable
- [ ] Failover tested and < 100ms

---

*Last updated: 2026-02-02*
