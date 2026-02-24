Feature: Data Residency Compliance
  As a compliance officer
  I want user data to remain in assigned geographic regions
  So that we comply with data sovereignty laws (GDPR, LGPD, CCPA)

  Background:
    Given the system has regions: "eu-west-1", "us-east-1", "ap-southeast-1", "br-south-1"
    And data residency rules are enforced at API gateway level
    And compliance audit logging is enabled

  # Core Data Residency
  Scenario: EU user data stays in EU region
    Given User "hans@example.de" is assigned to region "eu-west-1"
    And User has data_residency_requirement = "EU_GDPR"
    When User makes an inference request
    Then Request data should be processed in "eu-west-1"
    And Response data should be stored in "eu-west-1"
    And No cross-border data transfer should occur
    And Processing should use EU-based LLM providers

  Scenario: Brazil user data stays in Brazil
    Given User "maria@example.com.br" is assigned to region "br-south-1"
    And User has data_residency_requirement = "BR_LGPD"
    When User makes an inference request with PII data
    Then All data should remain in "br-south-1"
    And Logs should be stored in Brazilian data centers
    And Data should not transit through non-Brazilian networks

  Scenario: US user data can be processed in US
    Given User "john@example.com" is assigned to region "us-east-1"
    And User has data_residency_requirement = "US_CCPA"
    When User makes an inference request
    Then Request can be processed in "us-east-1" or "us-west-2"
    And Data can transit between US regions
    But Data should not leave US jurisdiction without consent

  # Automatic Region Assignment
  Scenario: User region is auto-assigned based on IP geolocation
    Given New user "user@example.fr" signs up from IP "212.27.48.10" (Paris, France)
    When Account is created
    Then User should be assigned to nearest EU region "eu-west-1"
    And data_residency_requirement should be set to "EU_GDPR"
    And User should be notified of data residency location

  Scenario: User can manually override region assignment
    Given User "hans@example.de" is assigned to "eu-west-1"
    When User changes preferred region to "us-east-1"
    Then System should display data residency warning
    And User must acknowledge data transfer implications
    And Change should only proceed after explicit consent
    And Old consent should be archived for compliance

  # Cross-Border Transfers
  Scenario: Cross-border transfer requires explicit consent
    Given User "marie@example.fr" is assigned to region "eu-west-1"
    And EU region "eu-west-1" is unhealthy (0% availability)
    When Request needs to be routed to "us-east-1"
    Then System should prompt user for cross-border consent
    And Request should be queued until consent is granted
    And Transfer should be logged with legal basis "Emergency Operation"
    And User should receive notification within 24 hours

  Scenario: Standard Contractual Clauses enable cross-border transfer
    Given Organization "GlobalCorp" has signed SCCs with US providers
    And User "anna@example.de" belongs to "GlobalCorp"
    And EU region is at 50% capacity
    When Load balancer considers US failover
    Then Transfer to "us-east-1" is permitted under SCC framework
    And Transfer should be logged with legal basis "SCC Article 46"
    And Privacy notice should reference SCC agreement

  Scenario: Emergency failover with retroactive notification
    Given User "pierre@example.fr" is in "eu-west-1"
    And Major incident causes "eu-west-1" complete outage
    When Emergency failover routes requests to "us-east-1"
    Then Transfer should proceed immediately under "vital interest" exemption
    And User should be notified within 72 hours
    And Supervisory authority (CNIL) should be notified within 72 hours
    And Return to EU region should happen automatically when recovered

  # Data Localization
  Scenario: Inference request data is stored in correct region
    Given User "hans@example.de" is assigned to "eu-west-1"
    When User sends request "What is the capital of France?"
    Then Request payload should be stored in EU PostgreSQL cluster
    And Response should be stored in EU PostgreSQL cluster
    And Embedding vectors should be stored in EU Redis instance
    And All database shards should be physically located in EU

  Scenario: Cached data respects regional boundaries
    Given User "maria@example.com.br" is in "br-south-1"
    When User request is cached
    Then Cache entry should be stored in Brazil Redis cluster
    And Cache should use namespace "br-south-1:cache:*"
    And Cache should not replicate to non-Brazilian regions
    And Cache TTL should comply with LGPD retention limits

  Scenario: Logs are stored in user's region
    Given User "john@example.com" is in "us-east-1"
    When System logs user activity
    Then Logs should be written to "us-east-1" log storage
    And Logs should not be centralized in different jurisdiction
    And Log retention should follow regional compliance rules

  # Provider Selection
  Scenario: EU users only use GDPR-compliant providers
    Given User "anna@example.de" is assigned to "eu-west-1"
    And OpenAI has EU data center in Dublin
    And Anthropic processes EU requests in US
    When User makes inference request
    Then System should prefer OpenAI (EU-based)
    And System should avoid Anthropic unless user consents
    And Provider selection should prioritize data residency

  Scenario: China user data must stay in China
    Given User "li@example.cn" is in "cn-north-1"
    And China has strict data localization laws
    When User makes inference request
    Then Only Chinese-based providers should be used
    And Data should never leave Chinese territory
    And Non-compliant providers should be blocked at firewall level

  # Compliance Validation
  Scenario: System validates data residency before processing
    Given User "hans@example.de" has residency requirement "EU_GDPR"
    And Request routing targets "ap-southeast-1" (Singapore)
    When System validates compliance before forwarding
    Then Validation should fail with "RESIDENCY_VIOLATION"
    And Request should not be forwarded
    And Compliance team should be alerted
    And User should receive error "Data residency requirements prevent processing"

  Scenario: Audit trail tracks all data transfers
    Given User "marie@example.fr" makes 100 requests over 30 days
    And 98 requests processed in "eu-west-1"
    And 2 requests failed over to "us-east-1" (emergency)
    When Compliance officer requests audit report
    Then Report should show all 100 requests with locations
    And Report should highlight 2 cross-border transfers
    And Report should include legal basis for each transfer
    And Report should be exportable in JSON format

  # Edge Cases
  Scenario: User travels to different region
    Given User "hans@example.de" is assigned to "eu-west-1"
    And User account settings specify "data_residency = EU"
    When User logs in from US IP address "54.23.45.67"
    Then System should detect geographic mismatch
    And System should warn "You are accessing from outside EU"
    But Data should still be processed in "eu-west-1"
    And Access should be logged as "cross-border access"

  Scenario: VPN masking user location
    Given User "suspicious@example.com" signs up from "us-east-1" IP
    And Account metadata suggests EU phone number and address
    When System detects location inconsistency
    Then System should request additional verification
    And Admin should review for potential VPN/proxy use
    And User should verify actual residency location

  Scenario: Multi-region organization
    Given Organization "GlobalCorp" operates in EU and US
    And EU employees should use "eu-west-1"
    And US employees should use "us-east-1"
    When Employee from EU office makes request
    Then System should automatically route to "eu-west-1"
    And Organization-level settings should define regional routing rules

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Integration/CrossRegionRoutingTests.cs
  # - tests/Synaxis.Tests/Unit/RegionRouterTests.cs
  # - tests/Synaxis.Tests/Unit/GeoIPServiceTests.cs
  # - tests/Synaxis.Tests/Compliance/GdprComplianceProviderTests.cs
  # - tests/Synaxis.Tests/Compliance/LgpdComplianceProviderTests.cs
