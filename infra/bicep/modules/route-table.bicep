// Route Table Module for Synaxis Mission-Critical Infrastructure
// Forces all traffic through Azure Firewall for inspection

@description('The name of the route table')
param routeTableName string = 'synaxis-aks-routetable'

@description('The Azure region')
param location string = resourceGroup().location

@description('Environment tag')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Azure Firewall private IP (if deployed)')
param firewallPrivateIp string = ''

@description('Tags for the route table')
param tags object = {
  project: 'synaxis'
  managedBy: 'bicep'
}

// Common tags
var commonTags = union(tags, {
  environment: environment
  component: 'network-routing'
})

// Route Table resource
resource routeTable 'Microsoft.Network/routeTables@2023-09-01' = {
  name: routeTableName
  location: location
  tags: commonTags
  properties: {
    disableBgpRoutePropagation: true  // Don't learn BGP routes
    routes: empty(firewallPrivateIp) ? [] : [
      {
        name: 'DefaultRoute-to-Firewall'
        properties: {
          addressPrefix: '0.0.0.0/0'
          nextHopType: 'VirtualAppliance'
          nextHopIpAddress: firewallPrivateIp
        }
      }
    ]
  }
}

// Outputs
output routeTableId string = routeTable.id
output routeTableName string = routeTable.name
