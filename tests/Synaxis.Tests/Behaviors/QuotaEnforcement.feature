Feature: Quota Enforcement
  As a platform operator
  I want to enforce usage quotas based on customer tiers
  So that system resources are fairly distributed and abuse is prevented

  Background:
    Given quota enforcement is enabled
    And quota counters are tracked in Redis with sliding windows
    And rate limiting uses token bucket algorithm

  # Tier-Based Quotas
  Scenario: Free tier daily request limit
    Given Organization "StartupCo" is on "free" tier
    And Free tier allows 1000 requests per day
    And Organization has used 999 requests today
    When Organization makes request 1000
    Then Request should succeed with 200 OK
    And Response header "X-RateLimit-Remaining" should be "0"
    When Organization makes request 1001
    Then Request should be rejected with 429 Too Many Requests
    And Response should include "Daily request limit exceeded for free tier"
    And Response header "Retry-After" should indicate seconds until midnight UTC

  Scenario: Pro tier has higher limits
    Given Organization "GrowthCo" is on "pro" tier
    And Pro tier allows 100,000 requests per day
    And Organization has used 99,999 requests today
    When Organization makes request 100,000
    Then Request should succeed
    When Organization makes request 100,001
    Then Request should be rejected with 429 Too Many Requests
    And Response should suggest "Upgrade to Enterprise for unlimited requests"

  Scenario: Enterprise tier has no hard limits
    Given Organization "BigCorp" is on "enterprise" tier
    And Enterprise tier has "unlimited" requests
    When Organization makes 1,000,000 requests in one day
    Then All requests should succeed
    And Usage should still be tracked for billing
    And No throttling should occur

  # Token-Based Quotas
  Scenario: Token quota with sliding window
    Given Organization "StartupCo" has 10,000 tokens per minute limit
    And Current time is "2026-02-05T12:00:00Z"
    When Organization uses 5,000 tokens at "12:00:00"
    And Organization uses 3,000 tokens at "12:00:30"
    And Organization uses 3,000 tokens at "12:00:45"
    Then Request at "12:00:45" should succeed (total 11,000 tokens)
    And Sliding window should allow burst usage
    When Organization tries to use 1,000 tokens at "12:00:50"
    Then Request should be throttled with 429 Too Many Requests
    And Response should include "Token rate limit exceeded: 11000/10000 in last 60s"
    When time advances to "12:01:01"
    Then Request should succeed (5000 tokens from 12:00:00 expired)

  Scenario: Cost-based quota enforcement
    Given Organization "StartupCo" has monthly budget limit of $100
    And Organization has spent $95.50 this month
    When Organization makes request estimated to cost $3.00
    Then Request should succeed (total $98.50)
    When Organization makes request estimated to cost $5.00
    Then Request should be rejected with 402 Payment Required
    And Response should include "Monthly budget limit would be exceeded"
    And Organization owner should receive email notification

  # Rate Limiting
  Scenario: Per-second rate limiting
    Given Organization "StartupCo" has 10 requests per second limit
    When Organization makes 10 requests within 1 second
    Then All 10 requests should succeed
    When Organization makes 11th request within same second
    Then Request should be rejected with 429 Too Many Requests
    And Response header "X-RateLimit-Limit" should be "10"
    And Response header "X-RateLimit-Window" should be "1s"

  Scenario: Burst allowance with token bucket
    Given Organization "GrowthCo" has rate limit of 100 req/min
    And Token bucket allows burst of 120 requests
    When Organization is idle for 5 minutes (bucket fills to max)
    And Organization suddenly makes 120 requests in 10 seconds
    Then All 120 requests should succeed (burst allowed)
    And Bucket should be empty
    When Organization makes 121st request immediately
    Then Request should be throttled
    And Bucket refills at 100 tokens per minute

  # Concurrent Request Limits
  Scenario: Maximum concurrent requests per organization
    Given Organization "StartupCo" has limit of 5 concurrent requests
    When Organization starts 5 long-running inference requests
    And All 5 requests are still processing
    And Organization attempts 6th request
    Then 6th request should be rejected with 429 Too Many Requests
    And Response should include "Maximum concurrent requests (5) exceeded"
    When 1 of the 5 requests completes
    Then 6th request should now be allowed

  Scenario: Per-user concurrent request limits
    Given Organization "GrowthCo" allows 3 concurrent requests per user
    And User "alice@growthco.com" has 3 active requests
    When User "alice@growthco.com" makes 4th request
    Then Request should be rejected
    When User "bob@growthco.com" makes request
    Then Request should succeed (different user)

  # Model-Specific Quotas
  Scenario: Different limits for premium models
    Given Organization "StartupCo" is on free tier
    And Free tier allows access to "gpt-4o-mini" but not "gpt-4o"
    When User requests model "gpt-4o-mini"
    Then Request should succeed
    When User requests model "gpt-4o"
    Then Request should be rejected with 403 Forbidden
    And Response should include "Model gpt-4o requires Pro tier or higher"

  Scenario: Token limits vary by model
    Given Organization "GrowthCo" has 100k tokens/day for standard models
    And Premium models (GPT-4) count as 2x tokens for quota
    When Organization uses 50,000 tokens on "gpt-4o-mini"
    Then Quota consumed should be 50,000 tokens
    When Organization uses 25,000 tokens on "gpt-4o"
    Then Quota consumed should be 50,000 additional tokens (2x multiplier)
    And Total quota consumed should be 100,000 tokens

  # Quota Resets
  Scenario: Daily quota resets at midnight UTC
    Given Organization "StartupCo" has 1000 requests per day
    And Organization used 1000 requests on "2026-02-05"
    And Organization is throttled
    When System time reaches "2026-02-06T00:00:00Z"
    Then Quota counter should reset to 0
    And Next request should succeed
    And Response header "X-RateLimit-Reset" should show next midnight

  Scenario: Monthly quota resets on billing cycle date
    Given Organization "GrowthCo" has billing cycle starting on 1st of month
    And Organization has monthly limit of 1M tokens
    And Organization used 1M tokens in January
    When System time reaches "2026-02-01T00:00:00Z"
    Then Token quota should reset to 0
    And Billing cycle should advance to February

  # Grace Periods and Overages
  Scenario: Grace period for quota exceeded
    Given Organization "StartupCo" exceeds daily limit by small margin
    And Organization has good payment history
    When Organization makes request 1010 (1% over limit)
    Then Request should succeed with warning
    And Response header "X-RateLimit-Exceeded" should be "true"
    And Organization should receive email warning
    And Grace period should allow up to 5% overage

  Scenario: Hard limit after grace period exhausted
    Given Organization "StartupCo" has used 5% grace period
    When Organization attempts 6% overage
    Then Request should be hard-rejected with 429
    And No further grace should be allowed
    And Organization must upgrade tier or wait for reset

  # Quota Management
  Scenario: Organization admin can view quota usage
    Given Organization "GrowthCo" has admin "admin@growthco.com"
    When Admin requests quota dashboard
    Then Dashboard should show:
      | Metric                  | Used    | Limit   | Percentage |
      | Requests (daily)        | 45,231  | 100,000 | 45%        |
      | Tokens (monthly)        | 2.5M    | 10M     | 25%        |
      | Cost (monthly)          | $125.50 | $500    | 25%        |
      | Concurrent requests     | 3       | 20      | 15%        |

  Scenario: Quota can be temporarily increased for special events
    Given Organization "GrowthCo" is running Black Friday promotion
    And Support team grants temporary quota increase
    When Temporary limit is set to 500,000 requests for 48 hours
    Then Quota should increase to 500,000
    And After 48 hours, quota should revert to normal 100,000
    And Temporary increase should be logged in audit trail

  # Edge Cases
  Scenario: Quota counter race condition
    Given Organization "StartupCo" is at 999/1000 daily requests
    When 5 concurrent requests arrive simultaneously
    Then Only 1 request should succeed
    And Other 4 should be rejected with 429
    And Quota counter should correctly show 1000 (no over-counting)
    And Redis atomic operations should prevent race conditions

  Scenario: Quota bypass attempt via API key rotation
    Given Organization "StartupCo" has reached quota limit
    When Organization creates new API key
    And Tries to make request with new key
    Then Quota should still be enforced at organization level
    And Request should be rejected with 429
    And Multiple API keys should share same quota pool

  Scenario: Downgrading tier mid-cycle
    Given Organization "GrowthCo" is on Pro tier (100k req/day)
    And Organization has used 50k requests today
    When Organization downgrades to Free tier (1k req/day)
    Then Quota should immediately drop to 1k req/day
    And Organization should be instantly over quota (50k > 1k)
    And All further requests should be throttled until daily reset
    And System should show "50,000/1,000 used (downgraded mid-cycle)"

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Unit/QuotaServiceTests.cs
  # - tests/Synaxis.Tests/Integration/QuotaEnforcementTests.cs
  # - tests/Synaxis.Tests/Services/BillingServiceTests.cs
