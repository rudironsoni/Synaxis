Feature: Regional Failover
  As a platform reliability engineer
  I want automatic failover to healthy regions during outages
  So that users experience minimal service disruption

  Background:
    Given system has regions: "eu-west-1", "eu-central-1", "us-east-1", "ap-southeast-1"
    And health monitoring runs every 30 seconds
    And failover policies are configured per region

  # Health Monitoring
  Scenario: Region health score calculation
    Given Region "eu-west-1" has following metrics:
      | Metric              | Value  | Weight |
      | API success rate    | 99%    | 40%    |
      | Average latency     | 150ms  | 30%    |
      | Error rate          | 0.5%   | 20%    |
      | Provider capacity   | 95%    | 10%    |
    When Health score is calculated
    Then Overall health score should be 98.5%
    And Region status should be "healthy"
    And Region should remain in active rotation

  Scenario: Region degradation detection
    Given Region "eu-west-1" health score is 95% (healthy)
    When API success rate drops to 85%
    Then Health score should drop to ~80%
    And Region status should change to "degraded"
    And New requests should prefer other regions
    And Existing requests should continue to completion
    And Alert should be sent to operations team

  Scenario: Region becomes unhealthy
    Given Region "eu-west-1" health score is 80% (degraded)
    When Multiple services fail and success rate drops to 60%
    Then Health score should drop below 70% threshold
    And Region status should change to "unhealthy"
    And Region should be removed from routing pool immediately
    And All new requests should route to healthy regions
    And Alert should escalate to incident management

  # Automatic Failover
  Scenario: Automatic failover to healthy region
    Given User "bob@example.com" is assigned to "eu-west-1"
    And "eu-west-1" becomes unhealthy (health score 50%)
    And "eu-central-1" is healthy (health score 98%)
    When User makes inference request
    Then Request should be automatically routed to "eu-central-1"
    And User should not experience request failure
    And Response should include header "X-Served-From: eu-central-1"
    And Response should include header "X-Failover: true"
    And Failover should be logged in audit trail

  Scenario: Failover respects data residency requirements
    Given User "hans@example.de" is assigned to "eu-west-1"
    And User has data_residency_requirement = "EU_GDPR"
    And "eu-west-1" becomes unhealthy
    And Available regions: "eu-central-1" (healthy), "us-east-1" (healthy)
    When Failover is triggered
    Then Request should route to "eu-central-1" (EU region)
    And Request should NOT route to "us-east-1" (violates GDPR)
    And System should prefer regional compliance over latency

  Scenario: All EU regions unhealthy requires consent for US failover
    Given User "marie@example.fr" is assigned to "eu-west-1"
    And User has data_residency_requirement = "EU_GDPR"
    And Both "eu-west-1" and "eu-central-1" are unhealthy
    And "us-east-1" is healthy
    When User makes inference request
    Then Request should be queued (not immediately processed)
    And User should receive prompt: "EU regions unavailable. Route to US?"
    When User grants consent for cross-border transfer
    Then Request should proceed to "us-east-1"
    And Transfer should be logged with legal basis "Consent - Emergency"
    And User should receive email notification within 24 hours

  Scenario: Failover to nearest healthy region by latency
    Given User "john@example.com" is in "us-east-1"
    And "us-east-1" becomes unhealthy
    And Available regions:
      | Region           | Health | Latency from user |
      | us-west-2        | 98%    | 50ms              |
      | eu-west-1        | 99%    | 120ms             |
      | ap-southeast-1   | 97%    | 180ms             |
    When Failover is triggered
    Then Request should route to "us-west-2" (lowest latency)
    And Latency should be prioritized when health scores are similar

  # Failover Notifications
  Scenario: User notification of temporary failover
    Given User "bob@example.com" normally uses "eu-west-1"
    When Request fails over to "eu-central-1"
    Then User should see notification in response:
      """
      Your request was served from eu-central-1 due to temporary issues 
      with eu-west-1. Service will return to your primary region automatically.
      """
    And Notification should be informational only (not alarming)
    And User should be able to dismiss notification

  Scenario: Operations team receives failover alert
    Given Region "eu-west-1" fails over at "2026-02-05T10:00:00Z"
    Then Alert should be sent via:
      | Channel     | Recipients           | Urgency  |
      | PagerDuty   | On-call engineer     | High     |
      | Slack       | #incidents channel   | High     |
      | Email       | ops@synaxis.io       | Medium   |
    And Alert should include:
      - Failed region: eu-west-1
      - Health score: 45%
      - Affected users: ~5,000
      - Failover target: eu-central-1
      - Time of failure: 2026-02-05T10:00:00Z

  # Return to Primary Region
  Scenario: Automatic return when primary region recovers
    Given User "bob@example.com" failed over from "eu-west-1" to "eu-central-1"
    And "eu-west-1" recovers to health score 95%
    And "eu-west-1" maintains 95%+ health for 10 minutes (stabilization period)
    When User makes new inference request
    Then Request should return to "eu-west-1" (primary region)
    And User should receive notification: "Service restored to primary region"
    And Failover status should be cleared

  Scenario: Gradual traffic shift during recovery
    Given Region "eu-west-1" is recovering from outage
    And Health score improves from 60% → 95%
    When Region reaches 75% health
    Then 25% of traffic should route to "eu-west-1" (canary)
    When Region reaches 85% health
    Then 50% of traffic should route to "eu-west-1"
    When Region reaches 95% health and stabilizes for 10 minutes
    Then 100% of traffic should return to "eu-west-1"
    And Gradual rollback prevents thundering herd

  Scenario: Rollback if region degrades during recovery
    Given Region "eu-west-1" is receiving 50% traffic during recovery
    And Health score is 85%
    When Health score suddenly drops to 70%
    Then Traffic should immediately shift back to "eu-central-1"
    And Recovery attempt should be marked as "failed"
    And System should wait 30 minutes before retrying

  # Circuit Breaker Pattern
  Scenario: Circuit breaker opens after repeated failures
    Given Region "eu-west-1" experiences intermittent failures
    When 5 consecutive requests to "eu-west-1" fail
    Then Circuit breaker should open
    And All subsequent requests should immediately route to "eu-central-1"
    And No requests should be attempted to "eu-west-1" for 60 seconds
    And Circuit breaker status should be "OPEN"

  Scenario: Circuit breaker half-open state for testing
    Given Circuit breaker is open for "eu-west-1"
    And 60 seconds have elapsed
    When Circuit breaker enters half-open state
    Then Next request should be sent to "eu-west-1" as test
    If request succeeds
    Then Circuit breaker should close (region recovered)
    If request fails
    Then Circuit breaker should reopen for another 60 seconds

  # Cascading Failure Prevention
  Scenario: Prevent cascading failures to backup region
    Given "eu-west-1" is unhealthy (100% traffic on "eu-central-1")
    When "eu-central-1" reaches 90% capacity due to extra load
    Then System should shed load rather than overload backup region
    And Low-priority requests should be queued or rejected
    And High-priority requests (paid tiers) should be prioritized
    And Additional regions should be brought online if available

  Scenario: Multi-region outage falls back to degraded service
    Given All EU regions are unhealthy
    And All US regions are unhealthy
    And Only "ap-southeast-1" (Asia) is healthy
    When European user makes request
    Then Request should route to "ap-southeast-1" (high latency but functional)
    And User should be warned of degraded performance
    And Status page should display: "Major Incident: Limited service availability"

  # Split-Brain Prevention
  Scenario: Network partition between regions
    Given Region "eu-west-1" and "eu-central-1" lose connectivity
    And Health monitor in "eu-west-1" cannot reach "eu-central-1"
    When Failover decision is made
    Then Global coordinator should make centralized decision
    And Both regions should not simultaneously activate failover
    And Distributed consensus (Raft/Paxos) should prevent split-brain

  # Historical Failover Tracking
  Scenario: Failover history is tracked for SLO reporting
    Given System has had 3 failover events in past 30 days:
      | Date       | From       | To           | Duration | Reason          |
      | 2026-01-10 | eu-west-1  | eu-central-1 | 45min    | Database crash  |
      | 2026-01-20 | us-east-1  | us-west-2    | 2h 15min | Network outage  |
      | 2026-02-03 | eu-west-1  | eu-central-1 | 30min    | Deployment bug  |
    When SRE team generates availability report
    Then Report should calculate:
      - Total failover time: 3h 30min
      - Percentage of time in failover: 0.5%
      - SLO target: 99.9% (allowing 43min downtime/month)
      - Actual availability: 99.5% (exceeds budget by 127min)
    And Report should identify "eu-west-1" as reliability concern

  # Load Balancing During Failover
  Scenario: Geographic load balancing across multiple healthy regions
    Given User is equidistant from "us-east-1" and "us-west-2"
    And Both regions are healthy (98% and 97% health)
    When User makes request
    Then Request should be load balanced between regions
    And Distribution should be weighted by health scores
    And "us-east-1" should receive ~51% of traffic
    And "us-west-2" should receive ~49% of traffic

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Unit/FailoverServiceTests.cs
  # - tests/Synaxis.Tests/Unit/HealthMonitorTests.cs
  # - tests/Synaxis.Tests/Integration/CrossRegionRoutingTests.cs
  # - tests/Synaxis.Tests/Unit/RegionRouterTests.cs
