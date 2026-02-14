// =============================================================================
// Azure Front Door - Global Load Balancing Module
// Hansjoerg Scherer Pattern - Multi-region traffic management
// =============================================================================

// =============================================================================
// Parameters
// =============================================================================

@description('Location for Front Door (global service)')
param location string = 'global'

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Common tags to apply to all resources')
param tags object = {}

@description('Front Door SKU')
@allowed(['Standard_AzureFrontDoor', 'Premium_AzureFrontDoor'])
param frontDoorSku string = environment == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor'

@description('Stamp origins configuration')
param stampOrigins array = []

@description('Health probe path')
param healthProbePath string = '/health'

@description('Health probe interval in seconds')
param healthProbeIntervalSeconds int = 30

@description('Custom domains for Front Door')
param customDomains array = []

@description('Enable WAF (requires Premium SKU)')
param enableWaf bool = environment == 'prod'

@description('WAF mode (Prevention or Detection)')
@allowed(['Prevention', 'Detection'])
param wafMode string = 'Prevention'

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

// =============================================================================
// Variables
// =============================================================================

var frontDoorName = 'synaxis-fd-${environment}'
var wafPolicyName = 'synaxis-waf-${environment}'

var defaultTags = union(tags, {
  environment: environment
  managedBy: 'bicep'
  purpose: 'global-load-balancing'
})

// =============================================================================
// Azure Front Door Profile
// =============================================================================

resource frontDoorProfile 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: frontDoorName
  location: location
  tags: defaultTags
  sku: {
    name: frontDoorSku
  }
  properties: {
    originResponseTimeoutSeconds: 60
  }
}

// =============================================================================
// WAF Policy (Premium SKU only)
// =============================================================================

resource wafPolicy 'Microsoft.Network/FrontDoorWebApplicationFirewallPolicies@2022-05-01' = if (enableWaf) {
  name: wafPolicyName
  location: location
  tags: defaultTags
  sku: {
    name: frontDoorSku
  }
  properties: {
    policySettings: {
      enabledState: 'Enabled'
      mode: wafMode
      requestBodyCheck: true
      customBlockResponseStatusCode: 403
    }
    managedRules: {
      managedRuleSets: [
        {
          ruleSetType: 'Microsoft_DefaultRuleSet'
          ruleSetVersion: '2.1'
          ruleSetAction: 'Block'
          ruleGroupOverrides: []
          exclusions: []
        }
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
      rules: [
        {
          name: 'RateLimitRule'
          enabledState: 'Enabled'
          priority: 1
          ruleType: 'RateLimitRule'
          rateLimitDurationInMinutes: 5
          rateLimitThreshold: 1000
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
      ]
    }
  }
}

// =============================================================================
// Origin Group - Stamp Origins
// =============================================================================

resource stampOriginGroup 'Microsoft.Cdn/profiles/originGroups@2023-05-01' = {
  parent: frontDoorProfile
  name: 'stamp-origins'
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
      additionalLatencyInMilliseconds: 50
    }
    healthProbeSettings: {
      probePath: healthProbePath
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: healthProbeIntervalSeconds
    }
    sessionAffinityState: 'Disabled'
  }
}

// =============================================================================
// Origins (Stamps)
// =============================================================================

@batchSize(1)
resource stampOriginsResource 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = [for (origin, i) in stampOrigins: {
  parent: stampOriginGroup
  name: origin.name
  properties: {
    hostName: origin.hostName
    httpPort: 80
    httpsPort: 443
    originHostHeader: origin.hostName
    priority: origin.priority ?? 1
    weight: origin.weight ?? 1000
    enabledState: 'Enabled'
    enforceCertificateNameCheck: true
  }
}]

// =============================================================================
// Custom Domains
// =============================================================================

@batchSize(1)
resource customDomainResources 'Microsoft.Cdn/profiles/customDomains@2023-05-01' = [for domain in customDomains: {
  parent: frontDoorProfile
  name: replace(replace(domain.hostName, '.', '-'), '*', 'wildcard')
  properties: {
    hostName: domain.hostName
    tlsSettings: {
      certificateType: domain.certificateType ?? 'ManagedCertificate'
      minimumTlsVersion: 'TLS12'
      secret: domain.certificateType == 'CustomerCertificate' ? {
        id: domain.certificateSecretId
      } : null
    }
  }
}]

// =============================================================================
// Endpoint - Default
// =============================================================================

resource defaultEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoorProfile
  name: 'synaxis-${environment}'
  location: location
  tags: defaultTags
  properties: {
    enabledState: 'Enabled'
  }
}

// =============================================================================
// Routes - Default Route
// =============================================================================

resource defaultRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: defaultEndpoint
  name: 'default-route'
  properties: {
    originGroup: {
      id: stampOriginGroup.id
    }
    originPath: null
    ruleSets: []
    supportedProtocols: ['Http', 'Https']
    patternsToMatch: ['/*']
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
    cacheConfiguration: {
      queryStringCachingBehavior: 'IgnoreQueryString'
      compressionSettings: {
        isCompressionEnabled: true
        contentTypesToCompress: [
          'application/eot'
          'application/font'
          'application/font-sfnt'
          'application/javascript'
          'application/json'
          'application/opentype'
          'application/otf'
          'application/pkcs7-mime'
          'application/truetype'
          'application/ttf'
          'application/vnd.ms-fontobject'
          'application/xhtml+xml'
          'application/xml'
          'application/xml+rss'
          'application/x-font-opentype'
          'application/x-font-truetype'
          'application/x-font-ttf'
          'application/x-httpd-cgi'
          'application/x-javascript'
          'application/x-mpegurl'
          'application/x-opentype'
          'application/x-otf'
          'application/x-perl'
          'application/x-ttf'
          'font/eot'
          'font/ttf'
          'font/otf'
          'font/opentype'
          'image/svg+xml'
          'text/css'
          'text/csv'
          'text/html'
          'text/javascript'
          'text/js'
          'text/markdown'
          'text/plain'
          'text/richtext'
          'text/tab-separated-values'
          'text/xml'
          'text/x-script'
          'text/x-component'
          'text/x-java-source'
        ]
      }
    }
    customDomains: [for (domain, i) in customDomains: {
      id: customDomainResources[i].id
    }]
  }
  dependsOn: [
    stampOriginsResource
  ]
}

// =============================================================================
// Route with WAF Association (Premium)
// =============================================================================

resource wafRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = if (enableWaf) {
  parent: defaultEndpoint
  name: 'waf-route'
  properties: {
    originGroup: {
      id: stampOriginGroup.id
    }
    originPath: null
    ruleSets: []
    supportedProtocols: ['Https']
    patternsToMatch: ['/api/*', '/v1/*', '/inference/*']
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
    enabledState: 'Enabled'
    cacheConfiguration: {
      queryStringCachingBehavior: 'UseQueryString'
      queryParameters: ''
    }
    customDomains: [for (domain, i) in customDomains: {
      id: customDomainResources[i].id
    }]
  }
  dependsOn: [
    stampOriginsResource
    defaultRoute
  ]
}

// =============================================================================
// Security Policy (WAF Association)
// =============================================================================

resource securityPolicy 'Microsoft.Cdn/profiles/securityPolicies@2023-05-01' = if (enableWaf) {
  parent: frontDoorProfile
  name: 'synaxis-security-policy'
  properties: {
    parameters: {
      type: 'WebApplicationFirewall'
      wafPolicy: {
        id: wafPolicy.id
      }
      associations: [
        {
          domains: concat(
            [
              {
                id: defaultEndpoint.id
              }
            ],
            [for (domain, i) in customDomains: {
              id: customDomainResources[i].id
            }]
          )
          patternsToMatch: ['/*']
        }
      ]
    }
  }
}

// =============================================================================
// Diagnostic Settings
// =============================================================================

resource frontDoorDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${frontDoorName}-diagnostics'
  scope: frontDoorProfile
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'FrontDoorAccessLog'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'FrontDoorHealthProbeLog'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
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
// Outputs
// =============================================================================

@description('Front Door Profile ID')
output frontDoorId string = frontDoorProfile.id

@description('Front Door Profile Name')
output frontDoorName string = frontDoorProfile.name

@description('Front Door Endpoint Hostname')
output frontDoorEndpoint string = defaultEndpoint.properties.hostName

@description('Front Door Origin Group ID')
output originGroupId string = stampOriginGroup.id

@description('WAF Policy ID (if enabled)')
output wafPolicyId string = enableWaf ? wafPolicy.id : ''

@description('Front Door FQDN')
output frontDoorFqdn string = '${defaultEndpoint.name}.azurefd.net'
