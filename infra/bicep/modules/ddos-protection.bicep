// =============================================================================
// Azure DDoS Protection Plan Module
// Heyko Oelrichs Pattern - Network-level DDoS protection for mission-critical workloads
// =============================================================================

// =============================================================================
// Parameters
// =============================================================================

@description('Azure region for the DDoS Protection Plan')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('DDoS Protection Plan name')
param ddosPlanName string = 'synaxis-ddos-${environment}'

@description('Virtual Network ID to protect')
param virtualNetworkId string

@description('Enable DDoS Protection Standard')
param enableDdosProtection bool = true

@description('Log Analytics Workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags to apply to all resources')
param tags object = {}

// =============================================================================
// Variables
// =============================================================================

var ddosPlanSku = 'Standard'
var ddosPlanTags = union(tags, {
  environment: environment
  managedBy: 'bicep'
  purpose: 'ddos-protection'
  tier: 'network-security'
})

// =============================================================================
// DDoS Protection Plan
// =============================================================================

resource ddosProtectionPlan 'Microsoft.Network/ddosProtectionPlans@2023-09-01' = if (enableDdosProtection) {
  name: ddosPlanName
  location: location
  sku: {
    name: ddosPlanSku
  }
  tags: ddosPlanTags
  properties: {
    // DDoS Protection Plan properties
  }
}

// =============================================================================
// DDoS Protection Policy with Custom Rules
// =============================================================================

resource ddosProtectionPolicy 'Microsoft.Network/ddosProtectionPolicies@2023-09-01' = if (enableDdosProtection) {
  name: '${ddosPlanName}-policy'
  location: location
  tags: ddosPlanTags
  properties: {
    ddosProtectionPlan: {
      id: ddosProtectionPlan.id
    }
    rateLimitThresholds: {
      // Custom rate limiting thresholds per protocol
      tcpThreshold: 1000000 // 1M packets per second
      udpThreshold: 400000 // 400K packets per second
      icmpThreshold: 10000 // 10K packets per second
      synThreshold: 100000 // 100K SYN packets per second
    }
    protocolCustomSettings: [
      {
        protocol: 'Tcp'
        triggerRateLimitOverride: true
        rateLimitThreshold: 1000000
        triggerSensitivity: 'Low'
      }
      {
        protocol: 'Udp'
        triggerRateLimitOverride: true
        rateLimitThreshold: 400000
        triggerSensitivity: 'Low'
      }
      {
        protocol: 'Icmp'
        triggerRateLimitOverride: true
        rateLimitThreshold: 10000
        triggerSensitivity: 'Low'
      }
      {
        protocol: 'Syn'
        triggerRateLimitOverride: true
        rateLimitThreshold: 100000
        triggerSensitivity: 'Low'
      }
    ]
    ddosCustomPolicy: {
      // Custom policy for advanced protection
      protocolCustomSettings: [
        {
          protocol: 'Tcp'
          triggerRateLimitOverride: true
          rateLimitThreshold: 1000000
          triggerSensitivity: 'Low'
          tcpParameters: {
            enableRateLimiting: true
            rateLimitThreshold: 1000000
            synAuthTimeout: 60
            synRetransmissionTimeout: 5
            minRstRateThreshold: 10
            abnormalSequenceThreshold: 100
          }
        }
        {
          protocol: 'Udp'
          triggerRateLimitOverride: true
          rateLimitThreshold: 400000
          triggerSensitivity: 'Low'
          udpParameters: {
            enableRateLimiting: true
            rateLimitThreshold: 400000
            udpDropTimeout: 30
            udpDropInvalid: true
          }
        }
        {
          protocol: 'Icmp'
          triggerRateLimitOverride: true
          rateLimitThreshold: 10000
          triggerSensitivity: 'Low'
          icmpParameters: {
            enableRateLimiting: true
            rateLimitThreshold: 10000
            icmpDropTimeout: 30
          }
        }
      ]
    }
  }
}

// =============================================================================
// Diagnostic Settings for DDoS Protection Plan
// =============================================================================

resource ddosDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDdosProtection && !empty(logAnalyticsWorkspaceId)) {
  name: '${ddosPlanName}-diagnostics'
  scope: ddosProtectionPlan
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'DDoSProtectionNotifications'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'DDoSMitigationFlowLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'DDoSMitigationReports'
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
// Alert Rules for DDoS Protection
// =============================================================================

resource ddosAlertRule 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableDdosProtection) {
  name: '${ddosPlanName}-ddos-attack-alert'
  location: 'global'
  tags: ddosPlanTags
  properties: {
    description: 'Alert when DDoS attack is detected'
    severity: 3 // Warning
    enabled: true
    scopes: [
      ddosProtectionPlan.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 1
          metricName: 'IfUnderDDoSAttack'
          metricNamespace: 'Microsoft.Network/ddosProtectionPlans'
          operator: 'GreaterThan'
          timeAggregation: 'Average'
          dimensions: []
        }
      ]
    }
    actions: []
  }
}

resource ddosMitigationAlertRule 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableDdosProtection) {
  name: '${ddosPlanName}-ddos-mitigation-alert'
  location: 'global'
  tags: ddosPlanTags
  properties: {
    description: 'Alert when DDoS mitigation is active'
    severity: 2 // Error
    enabled: true
    scopes: [
      ddosProtectionPlan.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 1
          metricName: 'DDoSMitigation'
          metricNamespace: 'Microsoft.Network/ddosProtectionPlans'
          operator: 'GreaterThan'
          timeAggregation: 'Average'
          dimensions: []
        }
      ]
    }
    actions: []
  }
}

// =============================================================================
// Outputs
// =============================================================================

@description('DDoS Protection Plan ID')
output ddosProtectionPlanId string = enableDdosProtection ? ddosProtectionPlan.id : ''

@description('DDoS Protection Plan Name')
output ddosProtectionPlanName string = enableDdosProtection ? ddosProtectionPlan.name : ''

@description('DDoS Protection Policy ID')
output ddosProtectionPolicyId string = enableDdosProtection ? ddosProtectionPolicy.id : ''

@description('DDoS Protection Policy Name')
output ddosProtectionPolicyName string = enableDdosProtection ? ddosProtectionPolicy.name : ''

@description('DDoS Protection Enabled')
output ddosProtectionEnabled bool = enableDdosProtection
