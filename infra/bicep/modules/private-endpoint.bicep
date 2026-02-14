// =============================================================================
// Private Endpoint Module
// Creates private endpoints for PaaS services with DNS integration
// Supports: PostgreSQL, Redis, Cosmos DB, Service Bus, ACR, Key Vault
// =============================================================================

@description('Private endpoint name')
param privateEndpointName string

@description('Azure region')
param location string = resourceGroup().location

@description('Tags to apply to resources')
param tags object = {}

@description('Subnet ID for private endpoint deployment')
param subnetId string

@description('Private Link Service ID (target resource ID)')
param privateLinkServiceId string

@description('Group IDs for the private link service (e.g., vault, postgresqlServer, redisCache)')
@minLength(1)
param groupIds array

@description('Private DNS Zone ID for DNS integration')
param privateDnsZoneId string

@description('Enable manual approval (default: auto-approval)')
param manualApproval bool = false

@description('Private DNS zone group name (default: private-dns-zone-group)')
param privateDnsZoneGroupName string = 'private-dns-zone-group'

@description('Private link service connection name (default: private-link-service-connection)')
param privateLinkServiceConnectionName string = 'private-link-service-connection'

// =============================================================================
// Private Endpoint Resource
// =============================================================================

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: privateEndpointName
  location: location
  tags: union(tags, {
    managedBy: 'bicep'
    purpose: 'private-link'
  })
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: privateLinkServiceConnectionName
        properties: {
          privateLinkServiceId: privateLinkServiceId
          groupIds: groupIds
          requestMessage: manualApproval ? 'Private endpoint connection request for ${privateEndpointName}' : null
        }
      }
    ]
    ipConfigurations: empty(groupIds) ? [] : [
      for (groupId, index) in groupIds: {
        name: 'ipconfig-${index}'
        properties: {
          groupId: groupId
          memberName: 'default'
        }
      }
    ]
  }
}

// =============================================================================
// Private DNS Zone Group Configuration
// Links private endpoint to private DNS zone for automatic DNS registration
// =============================================================================

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-11-01' = {
  name: privateDnsZoneGroupName
  parent: privateEndpoint
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'dns-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// =============================================================================
// DNS A Record (Optional - for services that don't support zone groups)
// Some services require explicit A record creation in the private DNS zone
// =============================================================================

var resourceType = split(privateLinkServiceId, '/')[7]
var resourceName = last(split(privateLinkServiceId, '/'))

// Extract the DNS name from the resource based on type
var dnsName = resourceType == 'vaults' ? resourceName :
              resourceType == 'flexibleServers' ? resourceName :
              resourceType == 'redis' ? resourceName :
              resourceType == 'databaseAccounts' ? resourceName :
              resourceType == 'namespaces' ? resourceName :
              resourceType == 'registries' ? resourceName :
              resourceName

// Create A record for services that need explicit DNS configuration
resource dnsARecord 'Microsoft.Network/privateDnsZones/A@2020-06-01' = if (!manualApproval) {
  name: dnsName
  parent: privateDnsZoneId
  properties: {
    ttl: 300
    aRecords: [
      {
        ipv4Address: privateEndpoint.properties.networkInterfaces[0].properties.ipConfigurations[0].properties.privateIPAddress
      }
    ]
  }
  dependsOn: [
    privateEndpoint
  ]
}

// =============================================================================
// Network Interface Reference (for diagnostics and monitoring)
// =============================================================================

var networkInterfaceId = privateEndpoint.properties.networkInterfaces[0].id

// =============================================================================
// Diagnostic Settings (Optional)
// Uncomment and configure if diagnostic settings are needed
// =============================================================================

// resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(diagnosticSettings)) {
//   name: '${privateEndpointName}-diagnostics'
//   scope: privateEndpoint
//   properties: {
//     workspaceId: contains(diagnosticSettings, 'workspaceId') ? diagnosticSettings.workspaceId : ''
//     logs: [
//       {
//         category: 'PrivateEndpointNetworkInterface'
//         enabled: true
//       }
//     ]
//     metrics: [
//       {
//         category: 'AllMetrics'
//         enabled: true
//       }
//     ]
//   }
// }

// =============================================================================
// Outputs
// =============================================================================

@description('Private Endpoint ID')
output privateEndpointId string = privateEndpoint.id

@description('Private Endpoint Name')
output privateEndpointName string = privateEndpoint.name

@description('Private Endpoint IP Address')
output privateEndpointIpAddress string = privateEndpoint.properties.networkInterfaces[0].properties.ipConfigurations[0].properties.privateIPAddress

@description('Network Interface ID')
output networkInterfaceId string = networkInterfaceId

@description('Private DNS Zone Group ID')
output privateDnsZoneGroupId string = privateDnsZoneGroup.id

@description('Private Link Service Connection Status')
output connectionStatus string = privateEndpoint.properties.privateLinkServiceConnections[0].properties.connectionStatus ?? 'Approved'

@description('DNS Name')
output dnsName string = dnsName

@description('Resource Type')
output resourceType string = resourceType
