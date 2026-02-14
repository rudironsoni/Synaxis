// =============================================================================
// Azure Web Application Firewall (WAF) Policy Module
// Heyko Oelrichs Pattern - Application-layer security with OWASP 3.2
// =============================================================================

// =============================================================================
// Parameters
// =============================================================================

@description('Location for WAF Policy (global service)')
param location string = 'global'

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('WAF Policy name')
param wafPolicyName string = 'synaxis-waf-${environment}'

@description('Front Door SKU (determines WAF capabilities)')
@allowed(['Standard_AzureFrontDoor', 'Premium_AzureFrontDoor'])
param frontDoorSku string = environment == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor'

@description('WAF mode (Prevention or Detection)')
@allowed(['Prevention', 'Detection'])
param wafMode string = 'Prevention'

@description('Enable OWASP Core Rule Set 3.2')
param enableOwaspRules bool = true

@description('Enable Bot Protection')
param enableBotProtection bool = true

@description('Enable Rate Limiting')
param enableRateLimiting bool = true

@description('Rate limit threshold (requests per 5 minutes)')
param rateLimitThreshold int = 1000

@description('Enable Geo-filtering')
param enableGeoFiltering bool = false

@description('Allowed countries (ISO 3166-1 alpha-2 codes)')
param allowedCountries array = []

@description('Blocked countries (ISO 3166-1 alpha-2 codes)')
param blockedCountries array = []

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to all resources')
param tags object = {}

// =============================================================================
// Variables
// =============================================================================

var wafPolicyTags = union(tags, {
  environment: environment
  managedBy: 'bicep'
  purpose: 'waf-protection'
  tier: 'application-security'
})

var customBlockResponseStatusCode = 403
var customBlockResponseBody = '{"code": 403, "message": "Request blocked by WAF"}'

// =============================================================================
// WAF Policy
// =============================================================================

resource wafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2022-05-01' = {
  name: wafPolicyName
  location: location
  tags: wafPolicyTags
  sku: {
    name: frontDoorSku
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: wafMode
      requestBodyCheck: true
      requestBodyEnforce: true
      defaultCustomBlockResponseStatusCode: customBlockResponseStatusCode
      defaultCustomBlockResponseBody: customBlockResponseBody
    }
    managedRules: {
      managedRuleSets: [
        // OWASP Core Rule Set 3.2
        {
          ruleSetType: 'OWASP'
          ruleSetVersion: '3.2'
          ruleSetAction: 'Block'
          ruleGroupOverrides: [
            // Fine-tune OWASP rules for Synaxis API
            {
              ruleGroupName: 'REQUEST-942-APPLICATION-ATTACK-SQLI'
              rules: [
                {
                  ruleId: 942100
                  enabledState: 'Disabled' // Allow common SQL patterns in API
                  action: 'Block'
                }
                {
                  ruleId: 942200
                  enabledState: 'Disabled' // Allow JSON payloads
                  action: 'Block'
                }
              ]
            }
            {
              ruleGroupName: 'REQUEST-941-APPLICATION-ATTACK-XSS'
              rules: [
                {
                  ruleId: 941100
                  enabledState: 'Enabled'
                  action: 'Block'
                }
                {
                  ruleId: 941101
                  enabledState: 'Enabled'
                  action: 'Block'
                }
              ]
            }
            {
              ruleGroupName: 'REQUEST-920-PROTOCOL-ENFORCEMENT'
              rules: [
                {
                  ruleId: 920300
                  enabledState: 'Enabled'
                  action: 'Block'
                }
                {
                  ruleId: 920310
                  enabledState: 'Enabled'
                  action: 'Block'
                }
              ]
            }
          ]
          exclusions: [
            // Exclude specific headers from inspection
            {
              matchVariable: 'RequestHeaderNames'
              selector: 'Authorization'
              operator: 'Equals'
              exclusionMatchValue: []
            }
            {
              matchVariable: 'RequestHeaderNames'
              selector: 'X-API-Key'
              operator: 'Equals'
              exclusionMatchValue: []
            }
          ]
        }
        // Bot Manager Rule Set
        {
          ruleSetType: 'Microsoft_BotManagerRuleSet'
          ruleSetVersion: '1.0'
          ruleSetAction: 'Block'
          ruleGroupOverrides: []
          exclusions: []
        }
      ]
    }
    customRules: {
      rules: concat(
        // Rate Limiting Rules
        enableRateLimiting ? [
          {
            name: 'RateLimit-Global'
            enabledState: 'Enabled'
            priority: 1
            ruleType: 'RateLimitRule'
            rateLimitDurationInMinutes: 5
            rateLimitThreshold: rateLimitThreshold
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RemoteAddr'
                selector: null
                operator: 'IPMatch'
                negateCondition: false
                matchValue: ['0.0.0.0/0']
                transforms: []
              }
            ]
          }
          {
            name: 'RateLimit-API-Endpoints'
            enabledState: 'Enabled'
            priority: 2
            ruleType: 'RateLimitRule'
            rateLimitDurationInMinutes: 1
            rateLimitThreshold: 100
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RemoteAddr'
                selector: null
                operator: 'IPMatch'
                negateCondition: false
                matchValue: ['0.0.0.0/0']
                transforms: []
              }
              {
                matchVariable: 'RequestUri'
                selector: null
                operator: 'BeginsWith'
                negateCondition: false
                matchValue: ['/api/', '/v1/', '/inference/']
                transforms: ['Lowercase']
              }
            ]
          }
          {
            name: 'RateLimit-Auth-Endpoints'
            enabledState: 'Enabled'
            priority: 3
            ruleType: 'RateLimitRule'
            rateLimitDurationInMinutes: 5
            rateLimitThreshold: 20
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RemoteAddr'
                selector: null
                operator: 'IPMatch'
                negateCondition: false
                matchValue: ['0.0.0.0/0']
                transforms: []
              }
              {
                matchVariable: 'RequestUri'
                selector: null
                operator: 'BeginsWith'
                negateCondition: false
                matchValue: ['/auth/', '/login/', '/token/']
                transforms: ['Lowercase']
              }
            ]
          }
        ] : [],
        // Geo-filtering Rules
        enableGeoFiltering ? concat(
          // Allow specific countries
          length(allowedCountries) > 0 ? [
            {
              name: 'GeoFilter-Allowed-Countries'
              enabledState: 'Enabled'
              priority: 10
              ruleType: 'MatchRule'
              action: 'Allow'
              matchConditions: [
                {
                  matchVariable: 'RemoteAddr'
                  selector: null
                  operator: 'GeoMatch'
                  negateCondition: false
                  matchValue: allowedCountries
                  transforms: []
                }
              ]
            }
          ] : [],
          // Block specific countries
          length(blockedCountries) > 0 ? [
            {
              name: 'GeoFilter-Blocked-Countries'
              enabledState: 'Enabled'
              priority: 11
              ruleType: 'MatchRule'
              action: 'Block'
              matchConditions: [
                {
                  matchVariable: 'RemoteAddr'
                  selector: null
                  operator: 'GeoMatch'
                  negateCondition: false
                  matchValue: blockedCountries
                  transforms: []
                }
              ]
            }
          ] : []
        ) : [],
        // Custom API Protection Rules
        [
          {
            name: 'Block-SQL-Injection-Patterns'
            enabledState: 'Enabled'
            priority: 20
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'QueryString'
                selector: null
                operator: 'Contains'
                negateCondition: false
                matchValue: [
                  'UNION SELECT'
                  'OR 1=1'
                  'DROP TABLE'
                  'EXEC('
                  'xp_cmdshell'
                ]
                transforms: ['Lowercase', 'Trim']
              }
            ]
          }
          {
            name: 'Block-XSS-Patterns'
            enabledState: 'Enabled'
            priority: 21
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'QueryString'
                selector: null
                operator: 'Contains'
                negateCondition: false
                matchValue: [
                  '<script>'
                  'javascript:'
                  'onerror='
                  'onload='
                  'eval('
                  'document.cookie'
                ]
                transforms: ['Lowercase', 'Trim']
              }
            ]
          }
          {
            name: 'Block-Path-Traversal'
            enabledState: 'Enabled'
            priority: 22
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RequestUri'
                selector: null
                operator: 'Contains'
                negateCondition: false
                matchValue: [
                  '../'
                  '..\\'
                  '%2e%2e%2f'
                  '%2e%2e%5c'
                ]
                transforms: ['Lowercase', 'UrlDecode']
              }
            ]
          }
          {
            name: 'Block-User-Agent-Anomalies'
            enabledState: 'Enabled'
            priority: 23
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RequestHeader'
                selector: 'User-Agent'
                operator: 'Contains'
                negateCondition: false
                matchValue: [
                  'sqlmap'
                  'nmap'
                  'nikto'
                  'w3af'
                  'acunetix'
                  'burpsuite'
                  'metasploit'
                  'havij'
                ]
                transforms: ['Lowercase']
              }
            ]
          }
          {
            name: 'Block-Empty-User-Agent'
            enabledState: 'Enabled'
            priority: 24
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RequestHeader'
                selector: 'User-Agent'
                operator: 'Contains'
                negateCondition: true
                matchValue: ['Mozilla', 'curl', 'wget', 'python', 'java', 'go-http']
                transforms: ['Lowercase']
              }
            ]
          }
          {
            name: 'Block-Large-Request-Body'
            enabledState: 'Enabled'
            priority: 25
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RequestBody'
                selector: null
                operator: 'SizeGreaterThan'
                negateCondition: false
                matchValue: ['10485760'] // 10MB
                transforms: []
              }
            ]
          }
          {
            name: 'Block-Invalid-Content-Type'
            enabledState: 'Enabled'
            priority: 26
            ruleType: 'MatchRule'
            action: 'Block'
            matchConditions: [
              {
                matchVariable: 'RequestHeader'
                selector: 'Content-Type'
                operator: 'Contains'
                negateCondition: false
                matchValue: [
                  'application/x-www-form-urlencoded'
                  'multipart/form-data'
                  'application/json'
                  'text/plain'
                  'application/xml'
                ]
                transforms: ['Lowercase']
              }
            ]
          }
        ]
      )
    }
  }
}

// =============================================================================
// Diagnostic Settings for WAF Policy
// =============================================================================

resource wafDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${wafPolicyName}-diagnostics'
  scope: wafPolicy
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'FrontDoorWebApplicationFirewallLog'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
  }
}

// =============================================================================
// Alert Rules for WAF
// =============================================================================

resource wafBlockedRequestsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${wafPolicyName}-blocked-requests-alert'
  location: 'global'
  tags: wafPolicyTags
  properties: {
    description: 'Alert when WAF blocks significant number of requests'
    severity: 3 // Warning
    enabled: true
    scopes: [
      wafPolicy.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 100
          metricName: 'WebApplicationFirewallRequestCount'
          metricNamespace: 'Microsoft.Network/frontdoorWebApplicationFirewallPolicies'
          operator: 'GreaterThan'
          timeAggregation: 'Total'
          dimensions: [
            {
              name: 'Action'
              operator: 'Include'
              values: ['Block']
            }
          ]
        }
      ]
    }
    actions: []
  }
}

resource wafRateLimitAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${wafPolicyName}-rate-limit-alert'
  location: 'global'
  tags: wafPolicyTags
  properties: {
    description: 'Alert when rate limiting is triggered frequently'
    severity: 2 // Error
    enabled: true
    scopes: [
      wafPolicy.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 50
          metricName: 'WebApplicationFirewallRequestCount'
          metricNamespace: 'Microsoft.Network/frontdoorWebApplicationFirewallPolicies'
          operator: 'GreaterThan'
          timeAggregation: 'Total'
          dimensions: [
            {
              name: 'RuleName'
              operator: 'Include'
              values: ['RateLimit-Global', 'RateLimit-API-Endpoints', 'RateLimit-Auth-Endpoints']
            }
            {
              name: 'Action'
              operator: 'Include'
              values: ['Block']
            }
          ]
        }
      ]
    }
    actions: []
  }
}

// =============================================================================
// Outputs
// =============================================================================

@description('WAF Policy ID')
output wafPolicyId string = wafPolicy.id

@description('WAF Policy Name')
output wafPolicyName string = wafPolicy.name

@description('WAF Mode')
output wafMode string = wafMode

@description('OWASP Rules Enabled')
output owaspRulesEnabled bool = enableOwaspRules

@description('Bot Protection Enabled')
output botProtectionEnabled bool = enableBotProtection

@description('Rate Limiting Enabled')
output rateLimitingEnabled bool = enableRateLimiting

@description('Geo-filtering Enabled')
output geoFilteringEnabled bool = enableGeoFiltering
