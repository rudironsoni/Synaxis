// =============================================================================
// Virtual Network Module for Synaxis Mission-Critical Infrastructure
// =============================================================================
// Following Heyko Oelrichs Zero Trust security patterns for Azure
// Mission-Critical workloads
//
// ARCHITECTURE PRINCIPLES:
// - Zero Trust by default: deny all, allow only what's necessary
// - Network segmentation: separate subnets for different workload types
// - Private endpoints only: no public IPs for PaaS services
// - Service endpoints for Azure services: secure, optimized access
// - Hub-spoke ready: designed for future multi-region expansion
//
// SUBNET DESIGN:
// - aks-subnet (10.0.0.0/20): AKS cluster nodes with service endpoints
// - private-endpoints (10.0.16.0/24): Private Link for all PaaS services
// - AzureBastionSubnet (10.0.17.0/24): Azure Bastion for secure access
// - AzureFirewallSubnet (10.0.18.0/24): Azure Firewall for inspection
// - database-subnet (10.0.19.0/24): Direct database access (optional)
// - services-subnet (10.0.20.0/24): Microservices and workloads
// =============================================================================

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

@description('AKS NSG ID for association')
param aksNsgId string = ''

@description('Private Endpoint NSG ID for association')
param peNsgId string = ''

@description('Route Table ID for AKS subnet')
param routeTableId string = ''

@description('Tags for the virtual network')
param tags object = {
  project: 'synaxis'
  managedBy: 'bicep'
}

// Common tags
var commonTags = union(tags, {
  environment: environment
  component: 'network'
  zeroTrust: 'enabled'
})

// =============================================================================
// Subnet Configuration
// =============================================================================
// Each subnet is designed for a specific workload type following
// network segmentation best practices
// =============================================================================

var subnetConfig = [
  // -------------------------------------------------------------------------
  // AKS Subnet - Container workloads
  // -------------------------------------------------------------------------
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
      {
        service: 'Microsoft.KeyVault'
      }
      {
        service: 'Microsoft.ContainerRegistry'
      }
      {
        service: 'Microsoft.EventHub'
      }
      {
        service: 'Microsoft.ServiceBus'
      }
      {
        service: 'Microsoft.AzureActiveDirectory'
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
  // -------------------------------------------------------------------------
  // Private Endpoints Subnet - PaaS services via Private Link
  // -------------------------------------------------------------------------
  {
    name: 'private-endpoints'
    addressPrefix: '10.0.16.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
    delegations: []
  }
  // -------------------------------------------------------------------------
  // Azure Bastion Subnet - Secure RDP/SSH access
  // -------------------------------------------------------------------------
  {
    name: 'AzureBastionSubnet'
    addressPrefix: '10.0.17.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
    delegations: []
  }
  // -------------------------------------------------------------------------
  // Azure Firewall Subnet - Network security inspection
  // -------------------------------------------------------------------------
  {
    name: 'AzureFirewallSubnet'
    addressPrefix: '10.0.18.0/24'
    serviceEndpoints: []
    privateEndpointNetworkPolicies: 'Disabled'
    privateLinkServiceNetworkPolicies: 'Disabled'
    delegations: []
  }
  // -------------------------------------------------------------------------
  // Database Subnet - Direct database access (optional, for specific scenarios)
  // -------------------------------------------------------------------------
  {
    name: 'database-subnet'
    addressPrefix: '10.0.19.0/24'
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
    delegations: []
  }
  // -------------------------------------------------------------------------
  // Services Subnet - Microservices and application workloads
  // -------------------------------------------------------------------------
  {
    name: 'services-subnet'
    addressPrefix: '10.0.20.0/24'
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
      {
        service: 'Microsoft.Storage'
      }
      {
        service: 'Microsoft.KeyVault'
      }
      {
        service: 'Microsoft.ServiceBus'
      }
    ]
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
    delegations: []
  }
]

// =============================================================================
// Virtual Network Resource
// =============================================================================
resource vnet 'Microsoft.Network/virtualNetworks@2023-09-01' = {
  name: vnetName
  location: location
  tags: commonTags
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
        // Associate NSG if provided
        networkSecurityGroup: (subnet.name == 'aks-subnet' && !empty(aksNsgId)) ? {
          id: aksNsgId
        } : (subnet.name == 'private-endpoints' && !empty(peNsgId)) ? {
          id: peNsgId
        } : null
        // Associate route table for AKS subnet
        routeTable: (subnet.name == 'aks-subnet' && !empty(routeTableId)) ? {
          id: routeTableId
        } : null
      }
    }]
    enableDdosProtection: enableDdosProtection
    ddosProtectionPlan: enableDdosProtection && !empty(ddosProtectionPlanId) ? {
      id: ddosProtectionPlanId
    } : null
  }
}

// =============================================================================
// Outputs for use by other modules
// =============================================================================
output vnetId string = vnet.id
output vnetName string = vnet.name
output vnetAddressSpace string = addressSpace

// Subnet IDs
output aksSubnetId string = '${vnet.id}/subnets/aks-subnet'
output aksSubnetCidr string = '10.0.0.0/20'
output privateEndpointSubnetId string = '${vnet.id}/subnets/private-endpoints'
output privateEndpointSubnetCidr string = '10.0.16.0/24'
output bastionSubnetId string = '${vnet.id}/subnets/AzureBastionSubnet'
output bastionSubnetCidr string = '10.0.17.0/24'
output firewallSubnetId string = '${vnet.id}/subnets/AzureFirewallSubnet'
output firewallSubnetCidr string = '10.0.18.0/24'
output databaseSubnetId string = '${vnet.id}/subnets/database-subnet'
output databaseSubnetCidr string = '10.0.19.0/24'
output servicesSubnetId string = '${vnet.id}/subnets/services-subnet'
output servicesSubnetCidr string = '10.0.20.0/24'

// Subnet configuration summary
output subnetConfiguration object = {
  aks: {
    cidr: '10.0.0.0/20'
    purpose: 'AKS cluster nodes'
    serviceEndpoints: [
      'Microsoft.Sql'
      'Microsoft.Storage'
      'Microsoft.KeyVault'
      'Microsoft.ContainerRegistry'
      'Microsoft.EventHub'
      'Microsoft.ServiceBus'
      'Microsoft.AzureActiveDirectory'
    ]
    delegation: 'Microsoft.ContainerService/managedClusters'
  }
  privateEndpoints: {
    cidr: '10.0.16.0/24'
    purpose: 'Private Link for PaaS services'
    serviceEndpoints: []
  }
  bastion: {
    cidr: '10.0.17.0/24'
    purpose: 'Azure Bastion for secure access'
    serviceEndpoints: []
  }
  firewall: {
    cidr: '10.0.18.0/24'
    purpose: 'Azure Firewall for inspection'
    serviceEndpoints: []
  }
  database: {
    cidr: '10.0.19.0/24'
    purpose: 'Direct database access (optional)'
    serviceEndpoints: [
      'Microsoft.Sql'
      'Microsoft.Storage'
    ]
  }
  services: {
    cidr: '10.0.20.0/24'
    purpose: 'Microservices and workloads'
    serviceEndpoints: [
      'Microsoft.Sql'
      'Microsoft.Storage'
      'Microsoft.KeyVault'
      'Microsoft.ServiceBus'
    ]
  }
}
