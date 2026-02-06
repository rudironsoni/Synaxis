Feature: Multi-Tenant Isolation
  As a platform operator
  I want complete data isolation between organizations
  So that customer data remains secure and private

  Background:
    Given the system is running in multi-tenant mode
    And audit logging is enabled

  # Core Isolation Scenarios
  Scenario: Organization A cannot see Organization B's data
    Given Organization "Acme Corp" exists with ID "org-acme-001"
    And Organization "Globex Industries" exists with ID "org-globex-002"
    And User "alice@acme.com" belongs to "org-acme-001"
    And User "bob@globex.com" belongs to "org-globex-002"
    When User "alice@acme.com" tries to access Organization "org-globex-002" data
    Then Access should be denied with 403 Forbidden
    And Audit log should record the unauthorized access attempt
    And Response should include error code "TENANT_ISOLATION_VIOLATION"

  Scenario: API keys are scoped to organization
    Given Organization "Acme Corp" exists with ID "org-acme-001"
    And API key "sk-acme-12345" belongs to "org-acme-001"
    When API key "sk-acme-12345" is used to authenticate
    Then Requests should be charged to "org-acme-001" organization
    And Usage metrics should be tracked under "org-acme-001"
    And API key cannot access data from other organizations

  Scenario: Database queries are automatically filtered by tenant context
    Given Organization "Acme Corp" has tenant ID "org-acme-001"
    And Database contains 1000 inference requests across 10 organizations
    And 50 requests belong to "org-acme-001"
    When System executes query for user in "org-acme-001"
    Then Exactly 50 requests should be returned
    And All returned requests should have tenant_id = "org-acme-001"
    And Query should include automatic tenant filter in WHERE clause

  Scenario: Shared resources are isolated per tenant
    Given Organization "Acme Corp" has Redis namespace "acme:cache"
    And Organization "Globex Industries" has Redis namespace "globex:cache"
    When "Acme Corp" stores cache entry "model-config"
    Then Cache entry should be stored with key "acme:cache:model-config"
    And "Globex Industries" cannot access "acme:cache:*" keys
    And Cache isolation should be enforced at infrastructure layer

  # API Key Management
  Scenario: Creating API key associates it with user's organization
    Given User "alice@acme.com" belongs to organization "org-acme-001"
    And User is authenticated with valid JWT token
    When User creates a new API key named "Production Key"
    Then API key should be automatically associated with "org-acme-001"
    And API key prefix should be "sk-acme-"
    And Attempting to assign key to different organization should fail

  Scenario: Listing API keys only shows keys from user's organization
    Given Organization "Acme Corp" has 5 API keys
    And Organization "Globex Industries" has 3 API keys
    And User "alice@acme.com" belongs to "Acme Corp"
    When User "alice@acme.com" lists API keys
    Then Exactly 5 API keys should be returned
    And All keys should belong to "Acme Corp"
    And No keys from "Globex Industries" should be visible

  Scenario: Revoking API key requires ownership
    Given Organization "Acme Corp" owns API key "sk-acme-12345"
    And User "bob@globex.com" belongs to different organization
    When User "bob@globex.com" tries to revoke "sk-acme-12345"
    Then Operation should fail with 403 Forbidden
    And API key should remain active
    And Audit log should record the unauthorized attempt

  # Cross-Tenant Attacks
  Scenario: SQL injection cannot bypass tenant isolation
    Given Organization "Acme Corp" has tenant ID "org-acme-001"
    And Attacker attempts to inject "' OR tenant_id != 'org-acme-001' --"
    When System processes the malicious query
    Then Query should be parameterized and sanitized
    And No data from other tenants should be returned
    And Security alert should be triggered

  Scenario: JWT token tampering is detected
    Given User "alice@acme.com" has valid JWT for "org-acme-001"
    When Attacker modifies token to claim membership in "org-globex-002"
    And Modified token is used for authentication
    Then Token signature validation should fail
    And Request should be rejected with 401 Unauthorized
    And Security incident should be logged with user IP and timestamp

  # Resource Quotas and Billing
  Scenario: Each organization has independent quota limits
    Given Organization "Startup Inc" is on Free tier with 1000 req/day limit
    And Organization "Enterprise Corp" is on Enterprise tier with unlimited requests
    And Both organizations make 1500 requests in one day
    When System evaluates quota compliance
    Then "Startup Inc" should be throttled after 1000 requests
    And "Enterprise Corp" should have all requests succeed
    And Each organization should have separate quota counters

  Scenario: Billing is isolated per organization
    Given Organization "Acme Corp" uses 1,000,000 tokens
    And Organization "Globex Industries" uses 500,000 tokens
    When Monthly invoices are generated
    Then "Acme Corp" should be billed for exactly 1,000,000 tokens
    And "Globex Industries" should be billed for exactly 500,000 tokens
    And Usage should not be shared or aggregated across organizations

  # Admin Operations
  Scenario: Super admin can access all organizations
    Given User "admin@synaxis.io" has super_admin role
    And Platform has 100 organizations
    When Super admin lists all organizations
    Then All 100 organizations should be returned
    And Admin can view data from any organization
    And Admin actions should be logged separately in audit trail

  Scenario: Organization admin can only manage their own organization
    Given User "alice@acme.com" has org_admin role in "org-acme-001"
    And User "bob@globex.com" is member of "org-globex-002"
    When "alice@acme.com" tries to modify user "bob@globex.com"
    Then Operation should fail with 403 Forbidden
    And Admin can only manage users in "org-acme-001"

  # Edge Cases
  Scenario: User belongs to multiple organizations
    Given User "consultant@example.com" belongs to both "org-acme-001" and "org-globex-002"
    And User authenticates with JWT containing org_id = "org-acme-001"
    When User makes API request
    Then Request should use "org-acme-001" tenant context
    And User cannot access "org-globex-002" data in same session
    And Switching organizations requires new authentication token

  Scenario: Organization deletion cleans up all associated data
    Given Organization "TestCorp" has ID "org-test-999"
    And Organization has 10 users, 5 API keys, and 1000 requests
    When Organization "org-test-999" is deleted
    Then All users should be removed or reassigned
    And All API keys should be revoked immediately
    And All cached data should be purged
    And Deletion should be logged in audit trail
    And Historical data should remain for billing/compliance (90 days)

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Unit/TenantServiceTests.cs
  # - tests/Synaxis.Tests/Integration/ComplianceValidationTests.cs
  # - tests/Synaxis.Tests/Services/AuditServiceTests.cs
