# Error Reference

> **ULTRA MISER MODE™ Pro Tip**: Errors are just providers telling you they've run out of generosity. The solution? Rotate to the next free tier. Rinse. Repeat. Never pay.

Complete reference for all error codes, troubleshooting steps, and debugging strategies in Synaxis.

---

## Table of Contents

- [Error Code Reference](#error-code-reference)
- [HTTP Status Codes](#http-status-codes)
- [Error Types](#error-types)
- [Common Errors](#common-errors)
- [Provider-Specific Errors](#provider-specific-errors)
- [Troubleshooting Guide](#troubleshooting-guide)
- [Debugging Strategies](#debugging-strategies)
- [ULTRA MISER MODE™ Error Recovery](#ultra-miser-mode-error-recovery)

---

## Error Code Reference

### Client Errors (4xx)

#### `invalid_request_error` (400)

The request was invalid or cannot be served.

**Causes:**
- Malformed JSON in request body
- Missing required fields
- Invalid parameter types
- Invalid parameter values

**Example:**
```json
{
  "error": {
    "message": "The request was invalid or cannot be served.",
    "type": "invalid_request_error",
    "param": null,
    "code": "invalid_request_error"
  }
}
```

**Resolution:**
- Validate your JSON syntax
- Check required fields in request
- Verify parameter types match schema

---

#### `invalid_value` (400)

A specific parameter has an invalid value.

**Causes:**
- Model ID doesn't exist
- Temperature out of range (0-2)
- Max tokens exceeds limit
- Invalid message format

**Example:**
```json
{
  "error": {
    "message": "A parameter value is invalid.",
    "type": "invalid_request_error",
    "param": "temperature",
    "code": "invalid_value"
  }
}
```

**Resolution:**
- Check model ID exists in `/v1/models`
- Ensure temperature is between 0 and 2
- Verify max_tokens is within model limits

---

#### `missing_required_field` (400)

A required field is missing from the request.

**Causes:**
- Missing `messages` array
- Missing `model` parameter
- Missing `content` in message

**Resolution:**
- Include all required fields
- Check API documentation for required parameters

---

#### `invalid_parameter_type` (400)

A parameter has the wrong data type.

**Causes:**
- String where number expected
- Array where object expected
- Boolean where string expected

**Resolution:**
- Verify parameter types match the schema
- Check API documentation for correct types

---

#### `invalid_json` (400)

The request body contains invalid JSON.

**Causes:**
- Syntax errors in JSON
- Trailing commas
- Unquoted keys
- Invalid escape sequences

**Resolution:**
- Validate JSON with a linter
- Remove trailing commas
- Quote all object keys

---

#### `authentication_error` (401)

Authentication failed. Invalid or missing credentials.

**Causes:**
- Missing API key
- Invalid API key format
- Expired API key
- Wrong authentication header

**Example:**
```json
{
  "error": {
    "message": "Authentication failed. Please check your credentials.",
    "type": "authentication_error",
    "param": null,
    "code": "authentication_error"
  }
}
```

**Resolution:**
- Include `Authorization: Bearer YOUR_API_KEY` header
- Verify API key is valid and not expired
- Check API key has necessary permissions

---

#### `invalid_api_key` (401)

The API key provided is invalid.

**Resolution:**
- Generate a new API key from the admin panel
- Ensure key is copied correctly (no extra spaces)

---

#### `expired_api_key` (401)

The API key has expired.

**Resolution:**
- Generate a new API key
- Check key expiration settings

---

#### `permission_error` (403)

You do not have permission to access this resource.

**Causes:**
- Insufficient permissions for the operation
- Trying to access admin endpoints without admin role
- Rate limit exceeded for your tier

**Example:**
```json
{
  "error": {
    "message": "You do not have permission to access this resource.",
    "type": "permission_error",
    "param": null,
    "code": "authorization_error"
  }
}
```

**Resolution:**
- Verify your API key has required permissions
- Use admin key for admin endpoints
- Upgrade your access tier if needed

---

#### `forbidden` (403)

Access to the resource is forbidden.

**Resolution:**
- Check resource access controls
- Verify you're using the correct endpoint

---

#### `insufficient_permissions` (403)

Your API key lacks the required permissions.

**Resolution:**
- Request additional permissions from admin
- Use a key with broader scope

---

#### `not_found` (404)

The requested resource was not found.

**Causes:**
- Model doesn't exist
- Endpoint doesn't exist
- Provider not configured

**Example:**
```json
{
  "error": {
    "message": "The requested resource was not found.",
    "type": "not_found_error",
    "param": null,
    "code": "not_found"
  }
}
```

**Resolution:**
- Check model ID in `/v1/models` list
- Verify endpoint URL is correct
- Ensure provider is configured in settings

---

#### `model_not_found` (404)

The specified model does not exist.

**Resolution:**
- Use a valid model ID from the models list
- Check for typos in model name
- Verify model is available for your provider

---

#### `provider_not_found` (404)

The specified provider is not configured.

**Resolution:**
- Configure the provider in `appsettings.json`
- Check provider name spelling
- Verify provider is enabled

---

#### `rate_limit_error` (429)

Rate limit exceeded. Too many requests.

**Causes:**
- Exceeded requests per minute (RPM)
- Exceeded tokens per minute (TPM)
- Provider quota exhausted

**Example:**
```json
{
  "error": {
    "message": "Rate limit exceeded. Please try again later.",
    "type": "rate_limit_error",
    "param": null,
    "code": "rate_limit_exceeded"
  }
}
```

**Resolution:**
- Implement exponential backoff
- Reduce request frequency
- Check rate limits in provider dashboard
- Wait before retrying

**ULTRA MISER MODE™ Tip**: This is your cue to rotate to the next provider. Don't wait—route!

---

#### `quota_exceeded` (429)

Your quota for this billing period has been exceeded.

**Resolution:**
- Wait for quota reset
- Upgrade your plan
- Switch to a different provider
- Use free tier providers

---

### Server Errors (5xx)

#### `api_error` (502)

An error occurred while communicating with an upstream provider.

**Causes:**
- Provider service is down
- Network connectivity issues
- Provider returned an error
- All providers in tier failed

**Example:**
```json
{
  "error": {
    "message": "An error occurred while processing your request.",
    "type": "api_error",
    "param": null,
    "code": "provider_error"
  }
}
```

**Resolution:**
- Check provider status page
- Retry with exponential backoff
- Synaxis will automatically try next provider
- Verify provider configuration

---

#### `upstream_routing_failure` (502)

Unable to route request to any provider.

**Causes:**
- All providers in all tiers failed
- No providers configured for requested model
- Circuit breakers open for all providers

**Example:**
```json
{
  "error": {
    "message": "Unable to route request to any provider. Please try again later.",
    "type": "api_error",
    "param": null,
    "code": "upstream_routing_failure"
  }
}
```

**Resolution:**
- Check provider health: `GET /admin/health`
- Verify providers are configured
- Check circuit breaker status
- Wait and retry

**ULTRA MISER MODE™ Alert**: This means you've exhausted ALL providers. Time to sign up for more free tiers!

---

#### `provider_error` (502)

A specific provider returned an error.

**Resolution:**
- Check provider-specific error details in logs
- Verify provider API key is valid
- Check provider status page

---

#### `bad_gateway` (502)

The gateway received an invalid response from upstream.

**Resolution:**
- Provider may be experiencing issues
- Retry after a short delay
- Check provider status

---

#### `api_error` (503)

The service is temporarily unavailable.

**Causes:**
- Server maintenance
- Overload
- Dependency unavailable (Redis, PostgreSQL)

**Example:**
```json
{
  "error": {
    "message": "The service is temporarily unavailable. Please try again later.",
    "type": "api_error",
    "param": null,
    "code": "service_unavailable"
  }
}
```

**Resolution:**
- Check service health: `GET /health/liveness`
- Verify dependencies are running
- Wait and retry

---

#### `gateway_timeout` (504)

The gateway timed out waiting for upstream.

**Causes:**
- Provider response too slow
- Network latency
- Complex request taking too long

**Resolution:**
- Increase timeout if possible
- Simplify request
- Try a different provider
- Use streaming for long responses

---

#### `server_error` (500)

An internal server error occurred.

**Causes:**
- Unexpected exception
- Bug in the code
- Configuration error

**Example:**
```json
{
  "error": {
    "message": "An internal server error occurred. Please try again later.",
    "type": "server_error",
    "param": null,
    "code": "internal_error"
  }
}
```

**Resolution:**
- Check server logs for details
- Report the issue if persistent
- Retry the request

---

## HTTP Status Codes

| Status | Code | Meaning |
|--------|------|---------|
| 200 | OK | Success |
| 400 | Bad Request | Invalid request |
| 401 | Unauthorized | Authentication failed |
| 403 | Forbidden | Permission denied |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limited |
| 500 | Internal Server Error | Server error |
| 502 | Bad Gateway | Upstream error |
| 503 | Service Unavailable | Service down |
| 504 | Gateway Timeout | Timeout waiting for upstream |

---

## Error Types

OpenAI-compatible error types used in responses:

| Type | Description | HTTP Status |
|------|-------------|-------------|
| `invalid_request_error` | Client-side validation error | 400 |
| `authentication_error` | Authentication failed | 401 |
| `permission_error` | Authorization failed | 403 |
| `not_found_error` | Resource not found | 404 |
| `rate_limit_error` | Rate limit exceeded | 429 |
| `api_error` | Upstream provider error | 502, 503, 504 |
| `server_error` | Internal server error | 500 |

---

## Common Errors

### "No providers available for model"

**Cause**: The requested model isn't configured in any provider.

**Resolution:**
```bash
# Check available models
curl http://localhost:8080/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"

# Configure the model in appsettings.json
```

---

### "All providers failed"

**Cause**: Every provider in the routing chain returned an error.

**Resolution:**
```bash
# Check provider health
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Verify provider configurations
# Check provider API keys are valid
```

---

### "Circuit breaker is open"

**Cause**: A provider has failed too many times and is temporarily disabled.

**Resolution:**
- Wait for circuit breaker to close (default: 60 seconds)
- Check provider status
- Verify provider configuration
- Check provider rate limits

---

### "Streaming not supported"

**Cause**: The selected provider doesn't support streaming for this model.

**Resolution:**
- Set `stream: false` in request
- Use a different model that supports streaming
- Check model capabilities in documentation

---

## Provider-Specific Errors

### Groq Errors

| Error | Cause | Resolution |
|-------|-------|------------|
| `RateLimitError` | Exceeded 60 RPM or 100K TPM | Wait and retry |
| `InvalidModelError` | Model not available | Check available models |
| `AuthenticationError` | Invalid API key | Verify GROQ_API_KEY |

### OpenAI Errors

| Error | Cause | Resolution |
|-------|-------|------------|
| `insufficient_quota` | Out of credits | Add credits or switch provider |
| `model_not_found` | Model doesn't exist | Check model ID |
| `context_length_exceeded` | Input too long | Reduce message size |

### Cloudflare Errors

| Error | Cause | Resolution |
|-------|-------|------------|
| `AccountIdRequired` | Missing account ID | Add AccountId to config |
| `WorkersAIRateLimit` | Workers AI limit hit | Wait or upgrade plan |

### Gemini Errors

| Error | Cause | Resolution |
|-------|-------|------------|
| `QuotaExceeded` | Free tier quota exhausted | Wait for reset |
| `InvalidApiKey` | API key invalid | Check GEMINI_API_KEY |

---

## Troubleshooting Guide

### Step 1: Check Authentication

```bash
# Test authentication
curl http://localhost:8080/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"
```

If you get 401:
- Verify API key is correct
- Check key hasn't expired
- Ensure proper header format: `Bearer YOUR_API_KEY`

---

### Step 2: Check Provider Health

```bash
# Check all providers
curl http://localhost:8080/admin/health \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Look for:
- Provider status (online/offline)
- Last check timestamp
- Error messages
- Latency metrics

---

### Step 3: Verify Model Availability

```bash
# List all available models
curl http://localhost:8080/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"
```

Ensure:
- Model ID exists
- Provider is configured for that model
- Model is enabled

---

### Step 4: Check Configuration

```bash
# View provider configuration
curl http://localhost:8080/admin/providers \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

Verify:
- Provider is enabled
- API keys are configured
- Models are listed
- Tier is set correctly

---

### Step 5: Review Logs

Check Synaxis logs for detailed error information:

```bash
# Docker logs
docker logs synaxis-inferencegateway

# Or if running directly
dotnet run --project src/InferenceGateway/WebApi 2>&1 | tee synaxis.log
```

Look for:
- Exception stack traces
- Provider error responses
- Request/response details

---

## Debugging Strategies

### Enable Debug Logging

Set environment variable:

```bash
export LOG_LEVEL=Debug
# or
export LOG_LEVEL=Trace
```

---

### Test Individual Providers

```bash
# Test specific provider
curl http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -d '{
    "model": "groq/llama-3.3-70b",
    "messages": [{"role": "user", "content": "test"}]
  }'
```

Use canonical model IDs to test specific providers.

---

### Monitor Circuit Breakers

Circuit breakers protect against cascading failures:

| State | Meaning | Action |
|-------|---------|--------|
| Closed | Normal operation | None |
| Open | Provider failing | Wait for reset |
| Half-Open | Testing recovery | Monitor closely |

---

### Check Rate Limits

```bash
# Response headers include rate limit info
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1640995200
```

---

## ULTRA MISER MODE™ Error Recovery

### The Philosophy

> Errors are not failures—they're opportunities to rotate to the next free provider.

### Automatic Recovery

Synaxis automatically handles many errors:

1. **Rate limit (429)** → Try next provider in tier
2. **Provider error (502)** → Try next provider in tier
3. **Timeout (504)** → Try next provider in tier
4. **All tier providers fail** → Try next tier

### Manual Recovery Strategies

#### Strategy 1: The Rapid Rotate

When you hit a rate limit, immediately switch aliases:

```bash
# First try - might rate limit
curl -s http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "groq/llama-3.3-70b", "messages": [...]}'

# Second try - different provider
curl -s http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "nvidia/llama-3.3-70b", "messages": [...]}'
```

#### Strategy 2: The Miser Alias

Use aliases that prioritize free providers:

```bash
# This will try Pollinations → DuckDuckGo → Groq → etc.
curl -s http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model": "miser-fast", "messages": [...]}'
```

#### Strategy 3: The Tier Cascade

Configure multiple tiers for automatic fallback:

```json
{
  "Aliases": {
    "bulletproof": {
      "Candidates": [
        "pollinations/openai",    // Tier 0 - always free
        "ddg/gpt-4o-mini",        // Tier 0 - always free
        "groq/llama-3.3-70b",     // Tier 1 - generous free
        "gemini/flash",           // Tier 1 - generous free
        "openrouter/llama-free"   // Tier 2 - fallback
      ]
    }
  }
}
```

### Error Recovery Flowchart

```
Request Made
     ↓
Provider Available?
     ↓ No → Try Next Provider in Tier
     ↓ Yes
Request Sent
     ↓
Success? 
     ↓ Yes → Return Response
     ↓ No
Rate Limited?
     ↓ Yes → Try Next Provider
     ↓ No
Server Error?
     ↓ Yes → Try Next Provider
     ↓ No
All Providers Failed?
     ↓ Yes → Try Next Tier
     ↓ No → Return Error
```

### The Golden Rule

> Every error is just a provider saying "I'm tired." Your job is to say "That's fine, I'll ask someone else" and keep the free inference flowing.

---

## Error Code Quick Reference

| Code | Status | Type | Quick Fix |
|------|--------|------|-----------|
| `invalid_request_error` | 400 | invalid_request_error | Check request format |
| `invalid_value` | 400 | invalid_request_error | Fix parameter values |
| `authentication_error` | 401 | authentication_error | Check API key |
| `authorization_error` | 403 | permission_error | Check permissions |
| `not_found` | 404 | not_found_error | Verify model/endpoint |
| `rate_limit_exceeded` | 429 | rate_limit_error | Wait or rotate provider |
| `upstream_routing_failure` | 502 | api_error | Check provider health |
| `provider_error` | 502 | api_error | Try different provider |
| `service_unavailable` | 503 | api_error | Wait and retry |
| `internal_error` | 500 | server_error | Check logs, retry |

---

**Remember**: Errors are just temporary obstacles on the road to free inference. Rotate, retry, and refuse to pay.

*ULTRA MISER MODE™ — Because paying for AI is for people with self-respect.*
