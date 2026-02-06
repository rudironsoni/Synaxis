# Quota Permutation Tests

Comprehensive Bruno API test suite for all quota enforcement scenarios.

## Overview

This collection tests **all permutations** of quota enforcement:

- **Metric Types**: requests, tokens, both
- **Time Granularities**: minute, hour, day, week, month
- **Window Types**: fixed, sliding
- **Actions**: allow, throttle, block, credit_charge
- **Usage Levels**: 0%, 50%, 75%, 80%, 90%, 95%, 99%, 100%, 101%, 105%, 110%, 120%

## Test Files (30 tests)

### Basic Request Quotas (Fixed Window)
1. `01-Requests-Minute-Fixed-Allow-0%.bru` - First request in window
2. `02-Requests-Minute-Fixed-Allow-50%.bru` - Mid-range usage
3. `03-Requests-Minute-Fixed-Allow-90%.bru` - High usage with warning
4. `04-Requests-Minute-Fixed-Allow-99%.bru` - Last request before limit
5. `05-Requests-Minute-Fixed-Throttle-100%.bru` - At limit, throttled
6. `06-Requests-Minute-Fixed-Block-101%.bru` - Over limit, blocked

### Sliding Window Tests
7. `07-Requests-Minute-Sliding-Allow-50%.bru` - Sliding window behavior
8. `08-Requests-Minute-Sliding-Throttle-100%.bru` - Sliding window throttle
21. `21-Requests-Hour-Sliding-Allow-95%.bru` - Critical sliding hour usage
22. `22-Tokens-Minute-Sliding-Block-120%.bru` - Sliding window severe overage
23. `23-Requests-Day-Sliding-Throttle-100%.bru` - Daily sliding throttle

### Hourly Quotas
9. `09-Requests-Hour-Fixed-Allow-50%.bru` - Hourly quota mid-usage
10. `10-Requests-Hour-Fixed-Throttle-100%.bru` - Hourly quota exhausted

### Daily Quotas
11. `11-Requests-Day-Fixed-Allow-50%.bru` - Daily quota mid-usage
12. `12-Requests-Day-Fixed-Block-101%.bru` - Daily quota exceeded, blocked

### Token-Based Quotas
13. `13-Tokens-Minute-Fixed-Allow-50%.bru` - Token quota basics
14. `14-Tokens-Minute-Fixed-Throttle-100%.bru` - Token quota exhausted
15. `15-Tokens-Hour-Fixed-Allow-75%.bru` - Hourly token quota high usage
16. `16-Tokens-Day-Fixed-CreditCharge-110%.bru` - Credit charging for overage
26. `26-Tokens-Day-CreditCharge-Insufficient-Credits.bru` - Insufficient credits
19. `19-Tokens-Month-Fixed-Block-105%.bru` - Monthly token block

### Weekly and Monthly Quotas
17. `17-Requests-Week-Fixed-Allow-60%.bru` - Weekly quota
18. `18-Requests-Month-Fixed-Allow-80%.bru` - Monthly quota high usage

### Mixed Quotas (Requests + Tokens)
20. `20-Mixed-Requests-And-Tokens-Both-90%.bru` - Both quotas high
24. `24-Mixed-Request-OK-Token-Throttled.bru` - Request OK, token exhausted
25. `25-Mixed-Token-Throttled-Request-OK.bru` - Token OK, request exhausted

### Special Scenarios
27. `27-Window-Reset-Behavior-Fixed.bru` - Fixed window reset
28. `28-Burst-Protection-Rate-Limit.bru` - Nested burst limits
29. `29-Zero-Quota-Block-Immediate.bru` - Zero quota (no access)
30. `30-Unlimited-Quota-No-Limits.bru` - Unlimited quota (enterprise)

## Test Configuration

### Environment Variables Required
```
baseUrl=https://api.synaxis.example.com
apiKey=your-api-key
orgId=your-org-id
```

### Test Headers
Each test uses custom headers to simulate quota states:
- `X-Test-Quota-Type`: requests | tokens | both | unlimited
- `X-Test-Quota-Limit`: Numeric limit
- `X-Test-Quota-Window`: minute | hour | day | week | month
- `X-Test-Quota-Window-Type`: fixed | sliding
- `X-Test-Quota-Action`: allow | throttle | block | credit_charge
- `X-Test-Simulate-Usage`: Current usage count
- `X-Test-Credit-Balance`: Available credits (for credit_charge tests)

## Expected HTTP Status Codes

| Status | Meaning | When It Occurs |
|--------|---------|----------------|
| 200 | OK | Within quota limits |
| 402 | Payment Required | Insufficient credits for overage |
| 403 | Forbidden | Blocked (exceeded quota, needs intervention) |
| 429 | Too Many Requests | Throttled (temporary, retry after window) |

## Expected Response Headers

### Rate Limit Headers
- `X-RateLimit-Limit`: Total quota
- `X-RateLimit-Remaining`: Remaining quota
- `X-RateLimit-Reset`: Unix timestamp when quota resets
- `X-RateLimit-Window`: Time window (minute/hour/day/week/month)
- `X-RateLimit-Window-Type`: fixed | sliding
- `X-RateLimit-Warning`: Warning message (at high usage)

### Request-Specific Headers
- `X-RateLimit-Limit-Requests`: Request count limit
- `X-RateLimit-Remaining-Requests`: Remaining requests

### Token-Specific Headers
- `X-RateLimit-Limit-Tokens`: Token count limit
- `X-RateLimit-Remaining-Tokens`: Remaining tokens

### Throttle Headers
- `Retry-After`: Seconds to wait before retrying

### Credit Headers
- `X-Credit-Balance`: Current credit balance
- `X-Credit-Charged`: Amount charged for this request
- `X-Quota-Overage`: Amount over quota

### Burst Headers
- `X-Burst-Limit`: Burst rate limit
- `X-Burst-Remaining`: Remaining burst capacity

## Error Response Format

### Throttle Error (429)
```json
{
  "error": {
    "code": "rate_limit_exceeded",
    "type": "throttle",
    "message": "Rate limit exceeded. Retry after 45 seconds.",
    "details": {
      "limit": 100,
      "used": 100,
      "window": "minute",
      "window_type": "fixed"
    }
  }
}
```

### Block Error (403)
```json
{
  "error": {
    "code": "quota_exceeded",
    "type": "block",
    "message": "Quota exceeded. Contact support or upgrade plan.",
    "details": {
      "limit": 10000,
      "used": 10100,
      "overage": 100,
      "window": "day"
    },
    "support_url": "https://support.synaxis.com/quota",
    "upgrade_options": [...]
  }
}
```

### Token Error (429)
```json
{
  "error": {
    "code": "token_quota_exceeded",
    "type": "throttle",
    "message": "Token quota exhausted.",
    "details": {
      "limit_type": "tokens",
      "tokens_limit": 150000,
      "tokens_used": 150000,
      "tokens_remaining": 0
    }
  }
}
```

### Insufficient Credits (402)
```json
{
  "error": {
    "code": "insufficient_credits",
    "type": "payment_required",
    "message": "Insufficient credits for overage.",
    "details": {
      "credit_balance": 0.50,
      "credits_required": 2.00,
      "credit_deficit": 1.50
    },
    "payment_url": "https://billing.synaxis.com/topup",
    "topup_options": [...]
  }
}
```

## Test Assertions

Each test verifies:
1. ✅ Correct HTTP status code
2. ✅ Proper rate limit headers
3. ✅ Accurate remaining quota
4. ✅ Valid reset timestamps
5. ✅ Appropriate error messages
6. ✅ Retry-After when throttled
7. ✅ Warning headers at high usage
8. ✅ Usage tracking accuracy

## Running the Tests

### Run All Tests
```bash
bruno run collections/Synaxis.SaaS/12-QuotaPermutations/
```

### Run Specific Test
```bash
bruno run collections/Synaxis.SaaS/12-QuotaPermutations/05-Requests-Minute-Fixed-Throttle-100%.bru
```

### Run by Category
```bash
# All token tests
bruno run collections/Synaxis.SaaS/12-QuotaPermutations/ --filter "Tokens-*"

# All sliding window tests
bruno run collections/Synaxis.SaaS/12-QuotaPermutations/ --filter "*-Sliding-*"

# All throttle tests
bruno run collections/Synaxis.SaaS/12-QuotaPermutations/ --filter "*-Throttle-*"
```

## Key Concepts

### Fixed vs Sliding Windows

**Fixed Window:**
- Boundary-aligned (e.g., 14:00:00 - 14:00:59)
- Hard reset at boundaries
- Can cause traffic spikes at reset
- Simpler to implement
- Predictable reset times

**Sliding Window:**
- Moves with time (last N seconds from now)
- Gradual capacity recovery
- No reset spikes
- More complex to implement
- Fairer for users

### Actions

**Allow (200):**
- Request succeeds
- Quota decremented
- May include warnings at high usage

**Throttle (429):**
- Temporary denial
- Auto-recovers after window reset
- Includes Retry-After header
- Client should implement backoff

**Block (403):**
- Permanent/long-term denial
- Requires admin intervention
- Shows support contact
- Suggests plan upgrade

**Credit Charge (200/402):**
- Succeeds if credits available
- Charges credits for overage
- Fails with 402 if insufficient
- Prompts for payment

### Usage Levels

| Level | State | Client Action |
|-------|-------|---------------|
| 0-50% | ✅ Normal | Continue normal operation |
| 50-75% | ⚠️ Elevated | Monitor usage |
| 75-90% | 🟡 High | Start conserving |
| 90-99% | 🟠 Critical | Minimize requests |
| 100% | 🔴 Exhausted | Wait for reset |
| 101%+ | 🚫 Exceeded | Blocked/throttled |

## Best Practices

### Client Implementation
1. **Always read rate limit headers**
2. **Implement exponential backoff**
3. **Honor Retry-After header**
4. **Set up monitoring/alerts at 80%**
5. **Cache responses when possible**
6. **Handle both request and token quotas**
7. **Don't retry 403 blocks**
8. **Show user-friendly error messages**

### System Design
1. **Use sliding windows for fairness**
2. **Implement burst protection**
3. **Provide clear error messages**
4. **Include upgrade options in errors**
5. **Track both requests and tokens**
6. **Consider credit-based overages**
7. **Set appropriate warning thresholds**
8. **Log quota violations for abuse detection**

## Coverage Matrix

| Metric | Window | Type | Action | Usage | Test # |
|--------|--------|------|--------|-------|--------|
| Requests | Minute | Fixed | Allow | 0% | 01 |
| Requests | Minute | Fixed | Allow | 50% | 02 |
| Requests | Minute | Fixed | Allow | 90% | 03 |
| Requests | Minute | Fixed | Allow | 99% | 04 |
| Requests | Minute | Fixed | Throttle | 100% | 05 |
| Requests | Minute | Fixed | Block | 101% | 06 |
| Requests | Minute | Sliding | Allow | 50% | 07 |
| Requests | Minute | Sliding | Throttle | 100% | 08 |
| Requests | Hour | Fixed | Allow | 50% | 09 |
| Requests | Hour | Fixed | Throttle | 100% | 10 |
| Requests | Hour | Sliding | Allow | 95% | 21 |
| Requests | Day | Fixed | Allow | 50% | 11 |
| Requests | Day | Fixed | Block | 101% | 12 |
| Requests | Day | Sliding | Throttle | 100% | 23 |
| Requests | Week | Fixed | Allow | 60% | 17 |
| Requests | Month | Fixed | Allow | 80% | 18 |
| Tokens | Minute | Fixed | Allow | 50% | 13 |
| Tokens | Minute | Fixed | Throttle | 100% | 14 |
| Tokens | Minute | Sliding | Block | 120% | 22 |
| Tokens | Hour | Fixed | Allow | 75% | 15 |
| Tokens | Day | Fixed | Credit | 110% | 16 |
| Tokens | Day | Fixed | Credit | 110%* | 26 |
| Tokens | Month | Fixed | Block | 105% | 19 |
| Both | Hour | Fixed | Allow | 90% | 20 |
| Both | Hour | Fixed | Throttle | Req 100% | 24 |
| Both | Hour | Fixed | Throttle | Tok 100% | 25 |
| Requests | - | - | Special | Reset | 27 |
| Requests | Hour | Fixed | Special | Burst | 28 |
| Requests | Day | Fixed | Block | 0 quota | 29 |
| Requests | - | - | Special | Unlimited | 30 |

\* Test 26: Insufficient credits variant

## Integration

These tests can be:
- ✅ Run in CI/CD pipelines
- ✅ Used for contract testing
- ✅ Executed against staging/production
- ✅ Integrated with monitoring
- ✅ Used for load testing
- ✅ Part of SLA verification

## Support

For questions or issues with these tests:
- Documentation: https://docs.synaxis.com/quotas
- Support: support@synaxis.com
- Issues: https://github.com/synaxis/api/issues
