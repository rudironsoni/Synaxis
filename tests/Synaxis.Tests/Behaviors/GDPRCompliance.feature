Feature: GDPR Compliance
  As a Data Protection Officer
  I want to ensure GDPR compliance for EU users
  So that we meet legal obligations and protect user privacy

  Background:
    Given GDPR compliance module is enabled
    And data subject rights are enforced
    And all EU data processing has legal basis

  # Right to Access (Article 15)
  Scenario: Data subject requests data export
    Given User "anna@example.de" exists with data in EU region
    And User has made 500 inference requests over 6 months
    When User submits data access request via portal
    Then System should verify user identity via email confirmation
    And System should compile complete data export within 30 days
    And Export should include:
      | Data Category         | Format | Included                          |
      | Profile Information   | JSON   | Name, email, phone, address       |
      | Inference Requests    | JSON   | Prompts, responses, timestamps    |
      | API Keys              | JSON   | Key names, creation dates         |
      | Billing Records       | JSON   | Invoices, payments, usage         |
      | Consent History       | JSON   | Consents given/withdrawn, dates   |
      | Access Logs           | JSON   | Login events, IP addresses        |
    And Export should be downloadable as encrypted ZIP
    And User should receive email notification when ready

  Scenario: Data portability in machine-readable format
    Given User "hans@example.de" requests data portability
    When Export is generated
    Then Data should be in JSON format (machine-readable)
    And JSON should follow structured schema
    And Schema should include metadata for interoperability
    And Export should be importable to competing service

  Scenario: Third-party data processors are included in export
    Given User "marie@example.fr" data was processed by OpenAI and Anthropic
    When User requests data access
    Then Export should include list of all processors:
      | Processor          | Purpose              | Data Shared                    |
      | OpenAI             | Inference processing | Prompts, context               |
      | Anthropic          | Inference processing | Prompts, context               |
      | Stripe             | Payment processing   | Email, payment method          |
      | AWS S3 (eu-west-1) | Data storage         | All application data           |
    And User should be informed of each processor's role

  # Right to Erasure / Right to be Forgotten (Article 17)
  Scenario: Data subject requests account deletion
    Given User "anna@example.de" exists with account data
    And User has 1000 inference requests in database
    And User has 3 active API keys
    When User submits deletion request
    Then System should verify user identity
    And System should display deletion confirmation dialog
    And Dialog should warn: "This action is permanent and cannot be undone"
    When User confirms deletion
    Then Personal data should be deleted within 30 days
    And Deletion should include:
      | Data Type              | Action                                  |
      | User profile           | Permanently deleted                     |
      | Inference requests     | Anonymized (PII removed)                |
      | API keys               | Revoked and deleted                     |
      | Cached data            | Purged from Redis                       |
      | Billing records        | Retained for 7 years (legal obligation) |
      | Audit logs             | Anonymized (user_id → "deleted_user")   |
    And User should receive confirmation email
    And DPO should be notified of deletion

  Scenario: Right to erasure with exceptions
    Given User "pierre@example.fr" requests deletion
    And User has outstanding invoice of €500 (unpaid)
    When Deletion request is processed
    Then Most data should be deleted
    But Billing records should be retained due to "legal obligation" exception
    And User should be informed of retention with legal basis
    And Records should include: invoice, payment history, company name
    And All other personal data should be deleted

  Scenario: Cascade deletion of related data
    Given User "hans@example.de" requests deletion
    And User owns Organization "HansCorp" as sole admin
    And Organization has 5 other users
    When Deletion is requested
    Then System should detect organization ownership conflict
    And System should require ownership transfer or organization deletion
    And Other users should be notified of pending deletion
    And Deletion should not complete until conflict resolved

  # Right to Rectification (Article 16)
  Scenario: Data subject corrects inaccurate data
    Given User "anna@example.de" has incorrect phone number "+49 123 456"
    When User updates phone to "+49 987 654321"
    Then Updated data should be saved immediately
    And All systems should use corrected data
    And Historical records should note correction
    And Audit log should record: "User corrected phone number on 2026-02-05"

  Scenario: User disputes inaccurate inference data
    Given User "marie@example.fr" claims stored inference response is incorrect
    When User submits dispute
    Then User can add annotation to disputed record
    And Record should be flagged as "disputed by data subject"
    And DPO should review dispute within 7 days
    And If upheld, record should be corrected or deleted

  # Right to Restriction of Processing (Article 18)
  Scenario: Data subject requests processing restriction
    Given User "hans@example.de" is disputing data accuracy
    When User requests processing restriction during dispute
    Then Account should be marked as "processing_restricted"
    And User can no longer make inference requests
    And Existing data should not be processed or analyzed
    And Data should be stored but not used for any purpose
    And Restriction should remain until dispute resolved

  # Right to Object (Article 21)
  Scenario: Data subject objects to direct marketing
    Given User "anna@example.de" receives marketing emails
    When User objects to marketing communications
    Then User should be removed from all marketing lists immediately
    And No further marketing emails should be sent
    And User should still receive transactional emails (invoices, security alerts)
    And Objection should be recorded in consent management system

  Scenario: User objects to automated decision-making
    Given System uses ML to recommend models to users
    When User "pierre@example.fr" objects to automated recommendations
    Then Automated model selection should be disabled
    And User should manually select models
    And No profiling should be performed on user data

  # Consent Management (Article 7)
  Scenario: Granular consent for different processing purposes
    Given New user "marie@example.fr" creates account
    When User completes registration
    Then User should be presented with consent options:
      | Purpose                          | Required | Default |
      | Essential service operation      | Yes      | Yes     |
      | Analytics and usage statistics   | No       | No      |
      | Marketing communications         | No       | No      |
      | Personalized model recommendations | No     | No      |
      | Cross-border data transfers      | No       | No      |
    And Each consent should be separately toggleable
    And User can continue without optional consents

  Scenario: Withdrawing consent stops processing
    Given User "anna@example.de" previously consented to analytics
    And Analytics system has been collecting usage data
    When User withdraws analytics consent
    Then Analytics collection should stop immediately
    And Historical analytics data should be anonymized or deleted
    And User should receive confirmation of withdrawal

  Scenario: Consent must be freely given (no forced bundling)
    Given User wants to use basic inference service
    When User opts out of marketing consent
    Then Service should function normally
    And No features should be restricted due to marketing opt-out
    And Only essential consents can be required

  # Data Breach Notification (Article 33 & 34)
  Scenario: Data breach detected and reported to supervisory authority
    Given Data breach occurs affecting 1,000 EU users
    And Breach involves exposure of email addresses and inference prompts
    And Breach is detected at "2026-02-05T10:00:00Z"
    When Security team confirms breach at "2026-02-05T11:00:00Z"
    Then DPO should be notified immediately
    And Supervisory authority (e.g., CNIL, BfDI) should be notified within 72 hours
    And Notification should include:
      | Field                          | Value                                |
      | Nature of breach               | Unauthorized database access         |
      | Categories of data affected    | Email, prompts, timestamps           |
      | Number of data subjects        | 1,000                                |
      | Likely consequences            | Phishing risk, privacy violation     |
      | Measures taken                 | Credentials rotated, systems patched |
      | DPO contact                    | dpo@synaxis.io                       |
    And Breach should be logged in breach register

  Scenario: High-risk breach requires user notification
    Given Data breach exposes sensitive personal data (health information in prompts)
    And Risk assessment determines "high risk to rights and freedoms"
    When Breach is confirmed
    Then All affected users should be notified without undue delay
    And Notification should be in clear and plain language
    And Email should include:
      - Description of breach
      - Likely consequences
      - Measures taken by company
      - Measures user should take (change password, monitor account)
      - Contact information for questions
    And Users should be notified within 72 hours of breach discovery

  Scenario: Low-risk breach does not require user notification
    Given Data breach exposes non-sensitive data (request timestamps)
    And Risk assessment determines "unlikely to result in risk"
    When Breach is confirmed
    Then Supervisory authority should still be notified
    But Individual user notification is not required
    And Decision should be documented and justified

  # Privacy by Design (Article 25)
  Scenario: Data minimization in inference requests
    Given User "anna@example.de" makes inference request
    When Request is processed
    Then Only necessary data should be collected:
      - User ID (for authentication)
      - Prompt (for inference)
      - Timestamp (for audit)
    And Unnecessary data should not be collected:
      - IP address beyond authentication
      - Browser fingerprints
      - Detailed system information
    And Data retention should be minimal (30 days for prompts)

  Scenario: Pseudonymization of stored data
    Given User "hans@example.de" has user_id "usr_12345"
    When Inference requests are stored in analytics database
    Then user_id should be pseudonymized to "anon_abc789"
    And Mapping should be stored separately with restricted access
    And Analytics team should only see pseudonymized data

  # Data Protection Impact Assessment (Article 35)
  Scenario: DPIA required for high-risk processing
    Given Company launches new feature: "AI-powered content moderation"
    And Feature processes user prompts for safety detection
    And Feature involves automated decision-making affecting user access
    When Feature is designed
    Then DPIA should be mandatory before launch
    And DPIA should assess:
      - Nature, scope, context, purposes of processing
      - Necessity and proportionality
      - Risks to data subjects
      - Measures to mitigate risks
    And If high risk remains, supervisory authority should be consulted

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Compliance/GdprComplianceProviderTests.cs
  # - tests/Synaxis.Tests/Integration/ComplianceValidationTests.cs
  # - tests/Synaxis.Tests/Services/AuditServiceTests.cs
  # - tests/Synaxis.Tests/Unit/UserServiceTests.cs
