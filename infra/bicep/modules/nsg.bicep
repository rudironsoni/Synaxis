// Network Security Groups Module for Synaxis Mission-Critical Infrastructure
// Implements Zero Trust network segmentation

@description('The name prefix for NSGs')
param nsgNamePrefix string = 'synaxis'

@description('The Azure region')
param location string = resourceGroup().location

@description('Environment tag')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Tags for NSGs')
param tags object = {
  project: 'synaxis'
  managedBy: 'bicep'
}

// Common tags
var commonTags = union(tags, {
  environment: environment
  component: 'network-security'
})

// AKS NSG - Allows inbound from ALB and Azure Bastion
resource aksNsg 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: '${nsgNamePrefix}-aks-nsg'
  location: location
  tags: commonTags
  properties: {
    securityRules: [
      {
        name: 'Allow-AzureLoadBalancer'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '443'
          sourceAddressPrefix: 'AzureLoadBalancer'
          destinationAddressPrefix: 'VirtualNetwork'
          access: 'Allow'
          priority: 100
          direction: 'Inbound'
        }
      }
      {
        name: 'Allow-Bastion-SSH'
        properties: {
          protocol: 'Tcp'
          sourcePortRange: '*'
          destinationPortRange: '22'
          sourceAddressPrefix: '10.0.17.0/24'  // Bastion subnet
          destinationAddressPrefix: 'VirtualNetwork'
          access: 'Allow'
          priority: 110
          direction: 'Inbound'
        }
      }
      {
        name: 'Deny-All-Inbound'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Deny'
          priority: 4096
          direction: 'Inbound'
        }
      }
    ]
  }
}

// Private Endpoint NSG - Deny all inbound
resource peNsg 'Microsoft.Network/networkSecurityGroups@2023-09-01' = {
  name: '${nsgNamePrefix}-pe-nsg'
  location: location
  tags: commonTags
  properties: {
    securityRules: [
      {
        name: 'Deny-All-Inbound'
        properties: {
          protocol: '*'
          sourcePortRange: '*'
          destinationPortRange: '*'
          sourceAddressPrefix: '*'
          destinationAddressPrefix: '*'
          access: 'Deny'
          priority: 4096
          direction: 'Inbound'
        }
      }
    ]
  }
}

// Diagnostic settings for NSG flow logs
resource aksNsgDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${aksNsg.name}-flowlogs'
  scope: aksNsg
  properties: {
    logs: [
      {
        category: 'NetworkSecurityGroupEvent'
        enabled: true
      }
      {
        category: 'NetworkSecurityGroupRuleCounter'
        enabled: true
      }
    ]
  }
}

// Outputs
output aksNsgId string = aksNsg.id
output aksNsgName string = aksNsg.name
output peNsgId string = peNsg.id
output peNsgName string = peNsg.name
