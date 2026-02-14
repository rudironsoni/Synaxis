// =============================================================================
// Azure Firewall Policy Module - E1-T4 Security Foundation
// =============================================================================
// This module implements comprehensive firewall rules for:
// - Application rules for HTTPS traffic (443)
// - DNAT rules for inbound traffic
// - Network rules for internal service communication
// - Threat intelligence-based filtering (Alert/Deny)
// - Custom FQDN filtering
//
// Rules cover:
// - AKS egress (MicrosoftContainerRegistry, AzureActiveDirectory)
// - PostgreSQL private endpoint
// - Redis private endpoint
// - Service Bus private endpoint
// =============================================================================

@description('Azure region for the firewall policy')
param location string = resourceGroup().location

@description('Environment name')
param environment string

@description('AKS subnet CIDR')
param aksSubnetCidr string = '10.0.0.0/20'

@description('Private endpoint subnet CIDR')
param privateEndpointSubnetCidr string = '10.0.16.0/24'

@description('VNet CIDR')
param vnetCidr string = '10.0.0.0/16'

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('Tags for the firewall policy')
param tags object = {}

// Variables
var firewallPolicyName = 'synaxis-firewall-policy-${environment}'

// =============================================================================
// Firewall Policy with Threat Intelligence
// =============================================================================

resource firewallPolicy 'Microsoft.Network/firewallPolicies@2023-09-01' = {
  name: firewallPolicyName
  location: location
  properties: {
    sku: {
      tier: 'Standard'
    }
    threatIntelMode: 'Alert'
    dnsSettings: {
      enableProxy: true
      servers: []
    }
    intrusionDetection: {
      mode: 'Alert'
      configuration: {
        signatureOverrides: []
        trafficBypass: []
      }
    }
  }
  tags: tags
}

// =============================================================================
// Rule Collection Group: AKS Egress (Priority 100)
// =============================================================================

resource aksEgressRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'AKS-Egress'
  properties: {
    priority: 100
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-AKS-Required-Services'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-MicrosoftContainerRegistry'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            fqdnTags: [
              'MicrosoftContainerRegistry'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-AzureActiveDirectory'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            fqdnTags: [
              'AzureActiveDirectory'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-AzureContainerRegistry'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            fqdnTags: [
              'AzureContainerRegistry'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-AzureMonitor'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            fqdnTags: [
              'AzureMonitor'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-AKSAuditLogs'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'audit.loganalytics.azure.com'
              '*.ods.opinsights.azure.com'
              '*.oms.opinsights.azure.com'
              '*.monitoring.azure.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Rule Collection Group: Private Services (Priority 200)
// =============================================================================

resource privateServicesRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Private-Services'
  properties: {
    priority: 200
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-PostgreSQL-Private-Endpoint'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-PostgreSQL-5432'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
            destinationAddresses: [
              privateEndpointSubnetCidr
            ]
            destinationPorts: [
              '5432'
            ]
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-Redis-Private-Endpoint'
        priority: 200
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-Redis-6380'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
            destinationAddresses: [
              privateEndpointSubnetCidr
            ]
            destinationPorts: [
              '6380'
            ]
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-ServiceBus-Private-Endpoint'
        priority: 300
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-ServiceBus-5671'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
            destinationAddresses: [
              privateEndpointSubnetCidr
            ]
            destinationPorts: [
              '5671'
            ]
          }
          {
            ruleType: 'NetworkRule'
            name: 'Allow-ServiceBus-9093'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
            destinationAddresses: [
              privateEndpointSubnetCidr
            ]
            destinationPorts: [
              '9093'
            ]
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-KeyVault-Private-Endpoint'
        priority: 400
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-KeyVault-443'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
            destinationAddresses: [
              privateEndpointSubnetCidr
            ]
            destinationPorts: [
              '443'
            ]
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Rule Collection Group: Internal VNet Communication (Priority 300)
// =============================================================================

resource internalVnetRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Internal-VNet'
  properties: {
    priority: 300
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-Intra-VNet-Communication'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-VNet-Internal-All'
            protocols: [
              'Any'
            ]
            sourceAddresses: [
              vnetCidr
            ]
            destinationAddresses: [
              vnetCidr
            ]
            destinationPorts: [
              '*'
            ]
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Rule Collection Group: DNAT Rules (Priority 400)
// =============================================================================

resource dnatRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'DNAT-Rules'
  properties: {
    priority: 400
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyNatRuleCollection'
        name: 'DNAT-HTTPS-Inbound'
        priority: 100
        action: {
          type: 'DNAT'
        }
        rules: [
          {
            ruleType: 'NatRule'
            name: 'DNAT-HTTPS-to-AKS'
            protocols: [
              'TCP'
            ]
            sourceAddresses: [
              '*'
            ]
            destinationAddresses: [
              // This will be populated with the firewall public IP
              // Reference: firewallPublicIp.properties.ipAddress
            ]
            destinationPorts: [
              '443'
            ]
            translatedAddress: '10.0.0.4' // AKS load balancer IP (example)
            translatedPort: '443'
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Rule Collection Group: Custom FQDN Filtering (Priority 500)
// =============================================================================

resource customFqdnRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Custom-FQDN-Filtering'
  properties: {
    priority: 500
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-External-APIs'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-OpenAI-API'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'api.openai.com'
              '*.openai.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Anthropic-API'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'api.anthropic.com'
              '*.anthropic.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Google-Gemini-API'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              'generativelanguage.googleapis.com'
              '*.googleapis.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Azure-OpenAI-API'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              '*.openai.azure.com'
              '*.cognitiveservices.azure.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
        ]
      }
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Deny-Social-Media'
        priority: 200
        action: {
          type: 'Deny'
        }
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'Deny-Facebook'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              '*.facebook.com'
              '*.fb.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Deny-Twitter'
            protocols: [
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              '*.twitter.com'
              '*.x.com'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Rule Collection Group: Deny All (Priority 1000)
// =============================================================================

resource denyAllRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Deny-All'
  properties: {
    priority: 1000
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Deny-All-Outbound'
        priority: 100
        action: {
          type: 'Deny'
        }
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'Deny-All-Internet'
            protocols: [
              {
                protocolType: 'Http'
                port: 80
              }
              {
                protocolType: 'Https'
                port: 443
              }
            ]
            targetFqdns: [
              '*'
            ]
            sourceAddresses: [
              aksSubnetCidr
            ]
          }
        ]
      }
    ]
  }
}

// =============================================================================
// Outputs
// =============================================================================

@description('Firewall Policy ID')
output firewallPolicyId string = firewallPolicy.id

@description('Firewall Policy Name')
output firewallPolicyName string = firewallPolicy.name
