# Troubleshooting Guide

> **ULTRA MISER MODE™ Debugging**: Why pay for observability tools when you can read logs? This guide helps you diagnose and fix issues without spending a single token on support contracts.

Synaxis is designed to be resilient, but even the most miserly gateway occasionally needs a gentle nudge. This guide covers common issues, their symptoms, and solutions that won't cost you anything but time.

---

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Common Issues](#common-issues)
  - [All Providers Failing](#all-providers-failing)
  - [Authentication Failures](#authentication-failures)
  - [Rate Limiting Errors](#rate-limiting-errors)
  - [Database Connection Issues](#database-connection-issues)
  - [Redis Connection Issues](#redis-connection-issues)
  - [Streaming Not Working](#streaming-not-working)
  - [High Latency](#high-latency)
- [Provider-Specific Issues](#provider-specific-issues)
- [Debugging Techniques](#debugging-techniques)
- [Log Analysis](#log-analysis)
- [Getting Help](#getting-help)

---

## Quick Diagnostics

Before diving deep, run these quick checks:

```bash
# 1. Check if the API is alive
curl http://localhost:8080/health/liveness

# 2. Check if all dependencies are ready
curl http://localhost:8080/health/readiness

# 3. Check detailed health status
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# 4. Test a simple chat completion
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.3-70b-versatile",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

---

## Common Issues

### All Providers Failing

**Symptoms:**
- Every request returns `503 Service Unavailable`
- Health dashboard shows all providers as "offline"
- Error message: "No healthy providers available"

**Possible Causes & Solutions:**

1. **Network Connectivity**
   ```bash
   # Test external connectivity
   curl -I https://api.groq.com
   curl -I https://api.together.xyz
   ```
   If these fail, check your network/firewall settings.

2. **API Keys Expired or Invalid**
   ```bash
   # Check provider configuration
   curl http://localhost:8080/admin/providers \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```
   Verify keys are correct and not expired. Regenerate if necessary.

3. **Provider Rate Limits Exhausted**
   - Wait for rate limit reset (usually 1 minute to 1 hour)
   - Check provider dashboards for quota status
   - Consider adding more providers to rotation

4. **Configuration Issues**
   ```bash
   # Check config health
   curl http://localhost:8080/health/readiness
   ```
   Look for "config" check failures in the response.

---

### Authentication Failures

**Symptoms:**
- `401 Unauthorized` responses
- JWT validation errors in logs
- Cannot access admin endpoints

**Solutions:**

1. **JWT Token Expired**
   ```bash
   # Get a new token
   curl -X POST http://localhost:8080/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "admin@example.com", "password": "your-password"}'
   ```

2. **Invalid JWT Secret**
   - Verify `JwtSecret` in configuration is at least 32 characters
   - Ensure same secret is used across all instances
   - Check for trailing whitespace in environment variables

3. **Missing Authorization Header**
   ```bash
   # Correct format
   curl http://localhost:8080/admin/providers \
     -H "Authorization: Bearer YOUR_TOKEN_HERE"
   ```

---

### Rate Limiting Errors

**Symptoms:**
- `429 Too Many Requests` responses
- Headers show `X-RateLimit-Remaining: 0`
- Requests blocked after high volume

**Solutions:**

1. **Check Current Limits**
   ```bash
   curl -I http://localhost:8080/v1/chat/completions
   # Look for X-RateLimit-* headers
   ```

2. **Wait for Reset**
   - Default window: 1 minute
   - Check `X-RateLimit-Reset` header for exact time

3. **Adjust Rate Limits** (if self-hosted)
   ```csharp
   // In Program.cs or configuration
   builder.Services.AddRateLimiter(options =>
   {
       options.AddSlidingWindowLimiter("default", opt =>
       {
           opt.PermitLimit = 200;  // Increase from default 100
           opt.Window = TimeSpan.FromMinutes(1);
       });
   });
   ```

4. **Distribute Load**
   - Use multiple API keys for same provider
   - Add more providers to rotation
   - Implement client-side request batching

---

### Database Connection Issues

**Symptoms:**
- `500 Internal Server Error` on admin endpoints
- Health check shows "database: unhealthy"
- PostgreSQL connection errors in logs

**Solutions:**

1. **Check PostgreSQL Status**
   ```bash
   # If using Docker
   docker compose ps postgres
   docker compose logs postgres
   ```

2. **Verify Connection String**
   ```json
   {
     "Synaxis": {
       "ControlPlane": {
         "ConnectionString": "Host=localhost;Database=synaxis;Username=postgres;Password=postgres"
       }
     }
   }
   ```

3. **Test Connection**
   ```bash
   psql "Host=localhost;Database=synaxis;Username=postgres;Password=postgres" \
     -c "SELECT 1;"
   ```

4. **Migration Issues**
   ```bash
   # Apply pending migrations
   dotnet ef database update \
     --project src/InferenceGateway/Infrastructure \
     --startup-project src/InferenceGateway/WebApi
   ```

---

### Redis Connection Issues

**Symptoms:**
- Health check shows "redis: unhealthy"
- Rate limiting not working (falls back to in-memory)
- Provider health tracking inconsistent

**Solutions:**

1. **Check Redis Status**
   ```bash
   # If using Docker
   docker compose ps redis
   docker compose logs redis
   
   # Test Redis connection
   redis-cli ping
   # Should return: PONG
   ```

2. **Verify Connection String**
   ```json
   {
     "ConnectionStrings": {
       "Redis": "localhost:6379,abortConnect=false"
     }
   }
   ```

3. **Redis Memory Issues**
   ```bash
   # Check memory usage
   redis-cli info memory
   
   # If maxed out, clear old keys
   redis-cli --eval "return redis.call('del', unpack(redis.call('keys', 'health:*')))"
   ```

4. **Fallback Behavior**
   - Synaxis works without Redis (in-memory fallback)
   - Restarting loses health state and rate limit counters
   - Non-critical but recommended for production

---

### Streaming Not Working

**Symptoms:**
- `stream: true` requests return complete response at once
- No Server-Sent Events (SSE) data frames
- Client timeouts on streaming requests

**Solutions:**

1. **Verify Provider Supports Streaming**
   ```bash
   # Check model capabilities
   curl http://localhost:8080/v1/models
   ```
   Look for `streaming: true` in model metadata.

2. **Check Client Implementation**
   ```bash
   # Correct streaming request
   curl http://localhost:8080/v1/chat/completions \
     -H "Content-Type: application/json" \
     -d '{
       "model": "llama-3.3-70b-versatile",
       "stream": true,
       "messages": [{"role": "user", "content": "Hello"}]
     }'
   ```
   Should see `data: {...}` lines followed by `data: [DONE]`.

3. **Proxy/Load Balancer Issues**
   - Ensure proxy supports SSE (no buffering)
   - Nginx: `proxy_buffering off;`
   - Cloudflare: Disable buffering in Page Rules

4. **Timeout Settings**
   - Streaming requests take longer
   - Increase client timeout to 120+ seconds

---

### High Latency

**Symptoms:**
- Responses take 10+ seconds
- Timeouts on normal requests
- Degraded user experience

**Solutions:**

1. **Check Provider Latency**
   ```bash
   curl http://localhost:8080/admin/health \
     -H "Authorization: Bearer YOUR_TOKEN"
   ```
   Review `latency` values for each provider.

2. **Switch to Lower-Latency Provider**
   - Groq typically has lowest latency
   - Consider tier configuration (lower tier = higher priority)

3. **Enable Response Caching** (if applicable)
   ```csharp
   // For identical repeated prompts
   builder.Services.AddOutputCache(options =>
   {
       options.AddPolicy("chat", builder =>
           builder.Expire(TimeSpan.FromMinutes(5)));
   });
   ```

4. **Check Network Path**
   ```bash
   # Trace route to provider
   traceroute api.groq.com
   
   # Check DNS resolution time
   dig api.groq.com +stats
   ```

---

## Provider-Specific Issues

### Groq

**Issue:** `429 Rate Limit Exceeded` even with low usage
- **Cause:** Groq has aggressive rate limits on free tier
- **Solution:** Add multiple Groq accounts or fallback to other providers

**Issue:** Model not found errors
- **Cause:** Model names change frequently
- **Solution:** Check Groq documentation for current model IDs

### Together AI

**Issue:** Intermittent timeouts
- **Cause:** Cold start on less popular models
- **Solution:** Use popular models or implement retry logic

### Cloudflare Workers AI

**Issue:** Authentication failures
- **Cause:** Requires Cloudflare account ID + API token
- **Solution:** Verify both credentials are configured correctly

### DeepInfra

**Issue:** Model availability varies
- **Cause:** Not all models available in all regions
- **Solution:** Check DeepInfra dashboard for available models

---

## Debugging Techniques

### Enable Detailed Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Synaxis": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Trace Request Flow

```bash
# Add correlation ID to track requests
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "X-Request-ID: debug-123" \
  -d '{...}'

# Then grep logs for that ID
docker compose logs webapi | grep "debug-123"
```

### Check Provider Health Manually

```bash
# Test specific provider
curl http://localhost:8080/admin/providers/groq/status \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Inspect Redis State

```bash
# List all provider health keys
redis-cli keys "health:*"

# Check specific provider penalty
redis-cli ttl "health:groq:penalty"

# View rate limit counters
redis-cli keys "ratelimit:*"
```

---

## Log Analysis

### Common Log Patterns

**Healthy Request Flow:**
```
[INF] Routing request to provider: groq
[INF] Provider 'groq' health check passed
[INF] Request completed in 245ms
```

**Provider Failure:**
```
[WRN] Provider 'groq' returned 429, marking unhealthy for 60s
[INF] Falling back to next provider: together
```

**Authentication Error:**
```
[ERR] JWT validation failed: token expired
[WRN] Authentication failed for request to /admin/providers
```

**Database Error:**
```
[ERR] Database connection failed: connection refused
[WRN] Using in-memory fallback for health store
```

### Log Levels Guide

| Level | Use For |
|-------|---------|
| `Debug` | Request routing decisions, provider selection |
| `Information` | Successful operations, health check results |
| `Warning` | Recoverable errors, fallbacks, rate limits |
| `Error` | Failed requests, authentication failures |
| `Critical` | System-wide outages, data corruption |

---

## Getting Help

### Before Asking

1. **Check this guide** (you're here, good start!)
2. **Review logs** with `Debug` level enabled
3. **Test health endpoints** to isolate the issue
4. **Try the minimal reproduction** below

### Minimal Reproduction Test

```bash
#!/bin/bash
# save as test-synaxis.sh

echo "=== Synaxis Diagnostic Test ==="

echo -n "1. Liveness: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/health/liveness

echo -e "\n2. Readiness: "
curl -s http://localhost:8080/health/readiness | jq -r '.status'

echo "3. Chat completion: "
curl -s http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"llama-3.3-70b-versatile","messages":[{"role":"user","content":"Hi"}]}' \
  | jq -r '.choices[0].message.content // .error.message'

echo "=== Test Complete ==="
```

### Information to Provide

When reporting issues, include:

1. **Synaxis version** (from `git describe` or docker image tag)
2. **Deployment method** (Docker, bare metal, cloud)
3. **Provider configuration** (redact API keys)
4. **Relevant log excerpts** (with `Debug` level)
5. **Health check output** (`/admin/health`)
6. **Steps to reproduce**

---

> **Remember**: In ULTRA MISER MODE™, every problem is just an opportunity to learn something new without paying for a course. Logs are your free textbook, and debugging is your practical exam. Master both, and you'll never need expensive support again.

---

## Quick Reference Card

| Issue | Quick Fix | Check |
|-------|-----------|-------|
| 503 Errors | Check provider keys | `/admin/health` |
| 401 Errors | Refresh JWT token | `/auth/login` |
| 429 Errors | Wait 1 minute | `X-RateLimit-Reset` header |
| 500 Errors | Check database/Redis | `/health/readiness` |
| High Latency | Switch provider tier | `/admin/health` latency |
| Streaming Fails | Verify model supports it | `/v1/models` |
| All Providers Down | Check network connectivity | `curl api.groq.com` |

---

*Last updated: 2026-02-02*
