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

@description('Firewall Policy ID')
param firewallPolicyId string

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string

@description('Tags for the firewall')
param tags object = {}

// Variables
var firewallName = 'synaxis-firewall-${location}'
var publicIpName = 'synaxis-firewall-pip-${location}'

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
      id: firewallPolicyId
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
output firewallPublicIp string = firewallPublicIp.properties.ipAddress
output firewallPublicIpId string = firewallPublicIp.id
