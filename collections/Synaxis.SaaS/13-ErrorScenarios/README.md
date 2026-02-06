# Error Scenarios Test Suite

Comprehensive test coverage for ALL HTTP error status codes and error conditions in the Synaxis SaaS API.

## Overview

This test suite contains **54 error scenario tests** organized by HTTP status code to validate proper error handling, security, and user experience.

## Test Categories

### 400 Bad Request (10 tests)
Validates request validation and input sanitization:
- ✅ Missing required fields
- ✅ Invalid email format
- ✅ Invalid region
- ✅ Invalid currency
- ✅ Empty organization name
- ✅ Invalid slug format
- ✅ Invalid model name
- ✅ Malformed JSON body
- ✅ Invalid date format
- ✅ Invalid tier value

### 401 Unauthorized (8 tests)
Validates authentication requirements and token handling:
- ✅ Missing auth token
- ✅ Invalid token format
- ✅ Expired token
- ✅ Revoked API key
- ✅ Invalid API key
- ✅ Missing API key
- ✅ Invalid MFA code
- ✅ Expired MFA session

### 403 Forbidden (8 tests)
Validates authorization, permissions, and access control:
- ✅ Insufficient permissions
- ✅ Tier feature not available
- ✅ Region not allowed
- ✅ Cross-border without consent
- ✅ Admin action without approval
- ✅ Read-only user trying to modify
- ✅ Suspended organization
- ✅ Revoked key still used

### 404 Not Found (6 tests)
Validates resource existence checking:
- ✅ Non-existent organization
- ✅ Non-existent user
- ✅ Non-existent team
- ✅ Non-existent API key
- ✅ Non-existent invoice
- ✅ Invalid endpoint

### 422 Unprocessable Entity (8 tests)
Validates business logic and data integrity:
- ✅ Duplicate email
- ✅ Duplicate organization slug
- ✅ Duplicate team name
- ✅ Invalid budget amount
- ✅ Invalid rate limit values
- ✅ Conflicting settings
- ✅ Validation errors
- ✅ Business rule violations

### 429 Too Many Requests (6 tests)
Validates rate limiting and quota enforcement:
- ✅ Rate limit exceeded (RPM)
- ✅ Rate limit exceeded (TPM)
- ✅ Quota exceeded (daily)
- ✅ Quota exceeded (monthly)
- ✅ Concurrent limit exceeded
- ✅ Burst traffic detected

### 500 Internal Server Error (4 tests)
Validates graceful failure handling and security:
- ✅ Database unavailable
- ✅ Redis connection failure
- ✅ External provider error
- ✅ Unexpected exception

### 503 Service Unavailable (4 tests)
Validates service availability and resilience:
- ✅ Regional outage
- ✅ Failover in progress
- ✅ Maintenance mode
- ✅ Circuit breaker open

## Test Standards

Each test validates:
1. **Correct HTTP status code** - Proper status code for error type
2. **Error code consistency** - Machine-readable error codes
3. **Error message clarity** - User-friendly error messages
4. **Error details structure** - Actionable error details
5. **Security** - No sensitive data leakage (credentials, stack traces, internal details)
6. **Headers** - Proper response headers (Content-Type, Retry-After, WWW-Authenticate)
7. **Tracking** - Incident IDs for support escalation
8. **Guidance** - Helpful suggestions and documentation links

## Security Validations

All tests verify:
- ❌ No stack traces exposed
- ❌ No file paths leaked
- ❌ No database details revealed
- ❌ No API keys or credentials in responses
- ❌ No timing attack vectors
- ❌ No user enumeration possible
- ✅ Generic safe error messages for 500 errors
- ✅ Incident tracking for support

## Usage

### Run All Error Scenario Tests
```bash
bruno run collections/Synaxis.SaaS/13-ErrorScenarios
```

### Run Specific Status Code Tests
```bash
# Run all 400 tests
bruno run collections/Synaxis.SaaS/13-ErrorScenarios --filter "400-*"

# Run all 401 tests
bruno run collections/Synaxis.SaaS/13-ErrorScenarios --filter "401-*"

# Run all 429 rate limit tests
bruno run collections/Synaxis.SaaS/13-ErrorScenarios --filter "429-*"

# Run all 500 server error tests
bruno run collections/Synaxis.SaaS/13-ErrorScenarios --filter "500-*"
```

### Run Individual Test
```bash
bruno run "collections/Synaxis.SaaS/13-ErrorScenarios/400-01-Missing Required Fields.bru"
```

## Error Response Format

All errors follow this standard format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "timestamp": "2025-02-05T23:30:00Z",
    "details": {
      // Error-specific details
      "field": "fieldName",
      "constraint": "validation rule",
      "suggestion": "How to fix"
    },
    "incidentId": "inc_123abc" // For 500 errors
  }
}
```

## Test Simulation

Many tests use the `X-Simulate-Error` header to trigger specific error conditions in the API:
- `database-unavailable` - Simulates database failure
- `redis-connection-failure` - Simulates cache failure
- `provider-error` - Simulates upstream provider error
- `regional-outage` - Simulates regional service outage
- `failover-in-progress` - Simulates active failover
- `maintenance-mode` - Simulates maintenance window
- `circuit-breaker-open` - Simulates circuit breaker activation

## Documentation

Each test includes comprehensive documentation:
- **Trigger Condition** - How the error is triggered
- **Expected Response** - What the API should return
- **Validation Points** - What the test verifies

Access documentation in Bruno UI or view the `docs` section in each `.bru` file.

## Contributing

When adding new error scenarios:
1. Follow the naming convention: `{status}-{number}-{description}.bru`
2. Include all standard assertions (status, code, message, details)
3. Add security checks (no leakage)
4. Document trigger conditions and expected behavior
5. Add appropriate test assertions for error-specific details

## Test Coverage Summary

| Status Code | Tests | Coverage |
|-------------|-------|----------|
| 400 | 10 | Validation & Input |
| 401 | 8 | Authentication |
| 403 | 8 | Authorization |
| 404 | 6 | Resource Existence |
| 422 | 8 | Business Logic |
| 429 | 6 | Rate Limiting |
| 500 | 4 | Server Errors |
| 503 | 4 | Service Availability |
| **Total** | **54** | **Complete** |

---

**Last Updated**: 2026-02-05  
**Test Suite Version**: 1.0.0  
**API Version**: v1
