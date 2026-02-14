// Virtual Network Module for Synaxis Mission-Critical Infrastructure
// Following Heyko Oelrichs Zero Trust security patterns

@description('The name of the virtual network')
param vnetName string

@description('The Azure region for the virtual network')
param location string = resourceGroup().location

@description('The address space for the virtual network')
param addressSpace string = '10.0.0.0/16'

@description('Environment tag (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Enable DDoS Protection')
param enableDdosProtection bool = false

@description('DDoS Protection Plan ID')
param ddosProtectionPlanId string = ''

@description('Tags for the virtual network')
param tags object = {
  project: 'synaxis'
  managedBy: 'bicep'
}

// Subnet configuration
var subnetConfig = [
  {
    name: 'aks-subnet'
    addressPrefix: '10.0.0.0/20'
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
      {
        service: 'Microsoft.Storage'
      }
    ]
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
    delegations: [
      {
        name: 'aks-delegation'
        properties: {
          serviceName: 'Microsoft.ContainerService/managedClusters'
        }
      }
    ]
  }
  {
    name: 'private-endpoints'
    addressPrefix: '10.0.16.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
    delegations: []
  }
  {
    name: 'AzureBastionSubnet'
    addressPrefix: '10.0.17.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
    delegations: []
  }
  {
    name: 'AzureFirewallSubnet'
    addressPrefix: '10.0.18.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
    delegations: []
  }
]

// Virtual Network resource
resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  tags: union(tags, {
    environment: environment
    component: 'network'
  })
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressSpace
      ]
    }
    subnets: [for subnet in subnetConfig: {
      name: subnet.name
      properties: {
        addressPrefix: subnet.addressPrefix
        serviceEndpoints: [for endpoint in subnet.serviceEndpoints: {
          service: endpoint.service
        }]
        privateEndpointNetworkPolicies: subnet.privateEndpointNetworkPolicies
        privateLinkServiceNetworkPolicies: subnet.privateLinkServiceNetworkPolicies
        delegations: [for delegation in subnet.delegations: {
          name: delegation.name
          properties: {
            serviceName: delegation.properties.serviceName
          }
        }]
      }
    }]
    enableDdosProtection: enableDdosProtection
    ddosProtectionPlan: enableDdosProtection && !empty(ddosProtectionPlanId) ? {
      id: ddosProtectionPlanId
    } : null
  }
}

// Outputs for use by other modules
output vnetId string = vnet.id
output vnetName string = vnet.name
output aksSubnetId string = '${vnet.id}/subnets/aks-subnet'
output privateEndpointSubnetId string = '${vnet.id}/subnets/private-endpoints'
output bastionSubnetId string = '${vnet.id}/subnets/AzureBastionSubnet'
output firewallSubnetId string = '${vnet.id}/subnets/AzureFirewallSubnet'
