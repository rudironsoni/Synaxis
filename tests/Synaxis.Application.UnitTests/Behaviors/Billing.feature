Feature: Multi-Currency Billing
  As a finance manager
  I want to bill customers in their preferred currency
  So that we can operate globally and avoid conversion friction

  Background:
    Given billing system supports 25+ currencies
    And exchange rates are updated hourly from ECB and other providers
    And invoices are generated on the 1st of each month

  # Basic Currency Billing
  Scenario: USD billing for US customer
    Given Organization "Acme Corp" has billing currency "USD"
    And Organization uses 1,000,000 tokens in January
    And Token pricing is $0.0001 per token
    When Invoice is generated for January
    Then Invoice total should be $100.00 USD
    And Invoice should display:
      | Line Item                | Quantity    | Unit Price | Total      |
      | GPT-4o inference tokens  | 1,000,000   | $0.0001    | $100.00    |
      | Subtotal                 |             |            | $100.00    |
      | Tax (0%)                 |             |            | $0.00      |
      | Total                    |             |            | $100.00    |
    And Invoice currency symbol should be "$"

  Scenario: EUR billing with exchange rate
    Given Organization "EuroCorp" has billing currency "EUR"
    And Exchange rate on 2026-02-01 is 1 USD = 0.92 EUR
    And Organization uses 1,000,000 tokens at $0.0001 per token
    When Invoice is generated
    Then Base cost should be calculated as $100.00 USD
    And Cost should be converted to €92.00 EUR
    And Invoice should display:
      | Line Item                | Quantity    | Unit Price (USD) | Total (EUR) |
      | GPT-4o inference tokens  | 1,000,000   | $0.0001          | €92.00      |
      | Subtotal                 |             |                  | €92.00      |
      | Exchange rate            | 1 USD = 0.92 EUR (2026-02-01)   | €92.00      |
      | Total                    |             |                  | €92.00      |
    And Invoice should show both USD base cost and EUR converted cost

  Scenario: GBP billing with real-time rate
    Given Organization "UKCorp" has billing currency "GBP"
    And Exchange rate on 2026-02-01 is 1 USD = 0.79 GBP
    And Organization uses 500,000 tokens at $0.0001 per token
    When Invoice is generated
    Then Invoice total should be £39.50 GBP
    And Exchange rate should be locked at time of invoice generation
    And Rate should not change if GBP fluctuates after invoice sent

  # Multi-Model Pricing
  Scenario: Different models have different pricing
    Given Organization "TechStart" has billing currency "USD"
    And Pricing structure is:
      | Model         | Price per 1K tokens |
      | gpt-4o        | $0.10               |
      | gpt-4o-mini   | $0.01               |
      | claude-3-opus | $0.15               |
    And Organization uses:
      | Model         | Tokens    |
      | gpt-4o        | 100,000   |
      | gpt-4o-mini   | 500,000   |
      | claude-3-opus | 50,000    |
    When Invoice is generated
    Then Invoice should itemize:
      | Model         | Tokens  | Unit Price | Subtotal |
      | gpt-4o        | 100,000 | $0.10/1K   | $10.00   |
      | gpt-4o-mini   | 500,000 | $0.01/1K   | $5.00    |
      | claude-3-opus | 50,000  | $0.15/1K   | $7.50    |
      | Total         |         |            | $22.50   |

  # Tax Handling
  Scenario: EU VAT for European customers
    Given Organization "EuroCorp" is located in Germany
    And German VAT rate is 19%
    And Organization uses 1,000,000 tokens worth €100.00
    When Invoice is generated
    Then Invoice should include:
      | Line Item              | Amount   |
      | Subtotal               | €100.00  |
      | VAT (19%)              | €19.00   |
      | Total                  | €119.00  |
    And Invoice should display VAT ID: "DE123456789"
    And Invoice should comply with EU VAT invoice requirements

  Scenario: Reverse charge for B2B in different EU countries
    Given Organization "FrenchCorp" is in France with valid VAT ID
    And Synaxis is registered in Ireland
    And French VAT would normally be 20%
    When Invoice is generated for B2B transaction
    Then VAT should be 0% (reverse charge mechanism)
    And Invoice should state: "VAT reverse charge - Customer to account for VAT"
    And Invoice should show both VAT IDs (seller and buyer)

  Scenario: No VAT for non-EU customers
    Given Organization "USCorp" is located in United States
    And US does not have VAT system
    When Invoice is generated
    Then VAT line should not appear on invoice
    And Total should equal subtotal (no tax)
    And Invoice should show US business address

  # Payment Methods
  Scenario: Credit card payment in customer currency
    Given Organization "EuroCorp" has billing currency "EUR"
    And Invoice total is €119.00
    And Payment method is Stripe credit card
    When Customer pays invoice
    Then Stripe should charge €119.00 directly
    And No currency conversion should occur
    And Customer's bank should see charge in EUR

  Scenario: ACH payment for US customers
    Given Organization "USCorp" has billing currency "USD"
    And Payment method is ACH bank transfer
    And Invoice total is $250.00
    When Payment is processed
    Then ACH transfer should be initiated for $250.00 USD
    And Transfer should take 3-5 business days
    And Customer should receive email when payment clears

  Scenario: SEPA payment for EU customers
    Given Organization "GermanCorp" has billing currency "EUR"
    And Payment method is SEPA Direct Debit
    And Invoice total is €500.00
    When Payment is processed
    Then SEPA transfer should be initiated for €500.00
    And Transfer should complete within 1-2 business days
    And SEPA mandate should be on file

  # Subscription Tiers with Currency
  Scenario: Free tier has no invoice
    Given Organization "Startup" is on free tier
    And Organization uses 500 requests (under free limit)
    When Billing cycle ends
    Then No invoice should be generated
    And Usage should be tracked but not billed

  Scenario: Pro tier monthly subscription
    Given Organization "GrowthCo" is on Pro tier
    And Pro tier costs $99/month base fee + usage
    And Billing currency is USD
    And Organization uses 1,000,000 tokens worth $100
    When Invoice is generated
    Then Invoice should include:
      | Line Item            | Amount  |
      | Pro Plan Base Fee    | $99.00  |
      | Usage (1M tokens)    | $100.00 |
      | Total                | $199.00 |

  Scenario: Enterprise tier with volume discounts
    Given Organization "BigCorp" is on Enterprise tier
    And Enterprise tier has volume pricing:
      | Volume                | Price per 1K tokens |
      | 0 - 10M tokens        | $0.10               |
      | 10M - 100M tokens     | $0.08               |
      | 100M+ tokens          | $0.06               |
    And Organization uses 150,000,000 tokens
    When Invoice is calculated
    Then Tiered pricing should apply:
      | Tier          | Tokens      | Rate   | Subtotal   |
      | Tier 1        | 10,000,000  | $0.10  | $1,000.00  |
      | Tier 2        | 90,000,000  | $0.08  | $7,200.00  |
      | Tier 3        | 50,000,000  | $0.06  | $3,000.00  |
      | Total         | 150,000,000 |        | $11,200.00 |

  # Credits and Refunds
  Scenario: Promotional credits applied to invoice
    Given Organization "NewCustomer" has $50 promotional credit
    And Invoice total is $120.00
    When Invoice is generated
    Then Invoice should show:
      | Line Item            | Amount   |
      | Subtotal             | $120.00  |
      | Promotional Credit   | -$50.00  |
      | Total Due            | $70.00   |
    And Remaining credit should be $0.00

  Scenario: Partial refund for service disruption
    Given Organization "Customer" paid $500 for January
    And Service had 5% downtime due to outage
    When Refund is issued for downtime
    Then Refund should be calculated as $25.00 (5% of $500)
    And Credit note should be issued
    And Credit should be applied to next invoice
    And Customer should receive email explaining refund

  # Exchange Rate Management
  Scenario: Exchange rates cached to prevent fluctuations during billing
    Given Exchange rate is 1 USD = 0.92 EUR at start of month
    And Organization "EuroCorp" uses services throughout the month
    And Exchange rate changes to 1 USD = 0.95 EUR by end of month
    When Invoice is generated on 1st of next month
    Then System should use exchange rate from start of billing period (0.92)
    And Rate should be locked for consistency
    And Invoice should note: "Exchange rate locked on 2026-02-01"

  Scenario: Exchange rate update failure fallback
    Given Primary exchange rate provider (ECB) is unavailable
    When System attempts to update rates
    Then System should fall back to secondary provider (OpenExchangeRates)
    And If all providers fail, use cached rates from last successful update
    And Billing should continue with last known good rates
    And Alert should be sent to finance team

  # Historical Billing
  Scenario: View historical invoices in original currency
    Given Organization "EuroCorp" has invoices from past 12 months
    And Currency was EUR for all periods
    When Customer views invoice history
    Then All invoices should display in EUR
    And Invoices should show exchange rates at time of generation
    And Customer can download PDF invoices for accounting

  Scenario: Currency change affects only future invoices
    Given Organization "UKCorp" has billing currency "GBP"
    And Organization has invoices from Jan, Feb, Mar in GBP
    When Organization changes billing currency to "USD" on Apr 1
    Then Historical invoices (Jan-Mar) should remain in GBP
    And Future invoices (Apr onward) should be in USD
    And Change should be noted in billing history

  # Budget Alerts in Multiple Currencies
  Scenario: Budget alert in customer's currency
    Given Organization "EuroCorp" has billing currency "EUR"
    And Organization sets budget limit of €500/month
    And Organization has spent €450 so far
    When Organization uses additional services worth €60
    Then Alert should be triggered in EUR
    And Email should state: "Budget limit exceeded: €510/€500"
    And Alert threshold should respect customer's currency

  # Compliance and Regulations
  Scenario: Invoice meets local accounting standards
    Given Organization "GermanCorp" requires German-compliant invoices
    When Invoice is generated
    Then Invoice must include:
      - Sequential invoice number (no gaps)
      - Seller's VAT ID and tax number
      - Buyer's VAT ID (if B2B)
      - Date of supply
      - Detailed line items
      - Correct VAT breakdown
    And Invoice should be archivable for 10 years (German law)

  # Reference Implementation
  # These scenarios link to actual test classes:
  # - tests/Synaxis.Tests/Services/BillingServiceTests.cs
  # - tests/Synaxis.Tests/Services/ExchangeRateProviderTests.cs
  # - tests/Synaxis.Tests/Integration/BillingCalculationTests.cs
  # - tests/Synaxis.Tests/Integration/BillingCacheIntegrationTests.cs
