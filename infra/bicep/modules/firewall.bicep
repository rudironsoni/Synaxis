// Azure Firewall Module
// Heyko Oelrichs Pattern: Central network security control with forced tunneling

@description('Azure region for the firewall')
param location string = resourceGroup().location

@description('Environment name')
param environment string

@description('Availability zones for the firewall')
param availabilityZones array = ['1', '2', '3']

@description('Virtual Network ID where firewall will be deployed')
param virtualNetworkId string

@description('Firewall subnet ID (AzureFirewallSubnet)')
param firewallSubnetId string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('Tags for the firewall')
param tags object = {}

// Variables
var firewallName = 'synaxis-firewall-${location}'
var publicIpName = 'synaxis-firewall-pip-${location}'
var firewallPolicyName = 'synaxis-firewall-policy-${environment}'

// Public IP for Azure Firewall
resource firewallPublicIp 'Microsoft.Network/publicIPAddresses@2023-09-01' = {
  name: publicIpName
  location: location
  zones: availabilityZones
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
  tags: tags
}

// Firewall Policy
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
  }
  tags: tags
}

// Rule Collection Group: AKS Required (Priority 100)esource aksRequiredRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'AKS-Required'
  properties: {
    priority: 100
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-Azure-Services'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
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
              '10.0.0.0/20'  // AKS subnet
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
              '10.0.0.0/20'
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
              '10.0.0.0/20'
            ]
          }
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
              '10.0.0.0/20'
            ]
          }
        ]
      }
    ]
  }
}

// Rule Collection Group: External AI Providers (Priority 200)esource externalProvidersRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'External-Providers'
  properties: {
    priority: 200
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-AI-Providers'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-OpenAI'
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
              '10.0.0.0/20'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Anthropic'
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
              '10.0.0.0/20'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Google-Gemini'
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
              '10.0.0.0/20'
            ]
          }
          {
            ruleType: 'ApplicationRule'
            name: 'Allow-Azure-OpenAI'
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
              '10.0.0.0/20'
            ]
          }
        ]
      }
    ]
  }
}

// Rule Collection Group: Private Services (Priority 300)esource privateServicesRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Private-Services'
  properties: {
    priority: 300
    ruleCollections: [
      {
        ruleCollectionType: 'FirewallPolicyFilterRuleCollection'
        name: 'Allow-Intra-VNet'
        priority: 100
        action: {
          type: 'Allow'
        }
        rules: [
          {
            ruleType: 'NetworkRule'
            name: 'Allow-VNet-Internal'
            protocols: [
              'Any'
            ]
            sourceAddresses: [
              '10.0.0.0/16'
            ]
            destinationAddresses: [
              '10.0.0.0/16'
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

// Rule Collection Group: Deny All (Priority 400)esource denyAllRuleCollectionGroup 'Microsoft.Network/firewallPolicies/ruleCollectionGroups@2023-09-01' = {
  parent: firewallPolicy
  name: 'Deny-All'
  properties: {
    priority: 400
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
              '10.0.0.0/20'
            ]
          }
        ]
      }
    ]
  }
}

// Azure Firewall
resource firewall 'Microsoft.Network/azureFirewalls@2023-09-01' = {
  name: firewallName
  location: location
  zones: availabilityZones
  properties: {
    sku: {
      name: 'AZFW_VNet'
      tier: 'Standard'
    }
    firewallPolicy: {
      id: firewallPolicy.id
    }
    ipConfigurations: [
      {
        name: 'AzureFirewallIpConfig'
        properties: {
          subnet: {
            id: firewallSubnetId
          }
          publicIPAddress: {
            id: firewallPublicIp.id
          }
        }
      }
    ]
  }
  tags: tags
  dependsOn: [
    aksRequiredRuleCollectionGroup
    externalProvidersRuleCollectionGroup
    privateServicesRuleCollectionGroup
    denyAllRuleCollectionGroup
  ]
}

// Diagnostic Settings
resource firewallDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'firewall-diagnostics'
  scope: firewall
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AzureFirewallApplicationRule'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'AzureFirewallNetworkRule'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'AzureFirewallDnsProxy'
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

// Outputs
output firewallId string = firewall.id
output firewallName string = firewall.name
output firewallPrivateIp string = firewall.properties.ipConfigurations[0].properties.privateIPAddress
output firewallPublicIpId string = firewallPublicIp.id
output firewallPolicyId string = firewallPolicy.id
output firewallPolicyName string = firewallPolicy.name
