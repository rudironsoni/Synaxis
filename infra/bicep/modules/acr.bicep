// Azure Container Registry (ACR) Module
// Hansjoerg Scherer Pattern: Ephemeral Scale Unit Architecture

@description('Azure Container Registry name (globally unique)')
param registryName string

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('ACR SKU')
@allowed(['Basic', 'Standard', 'Premium'])
param sku string = environment == 'prod' ? 'Premium' : 'Standard'

@description('Enable geo-replication for multi-region deployments')
param geoReplicationLocations array = environment == 'prod' ? ['westeurope', 'southeastasia'] : []

@description('Enable content trust for image signing')
param enableContentTrust bool = environment == 'prod'

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Subnet ID for private endpoint')
param subnetId string = ''

@description('Private DNS Zone ID for ACR')
param privateDnsZoneId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags')
param tags object = {}

// ACR Resource
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  tags: union(tags, {
    environment: environment
    purpose: 'synaxis-container-registry'
  })
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
    anonymousPullEnabled: false
    dataEndpointEnabled: sku == 'Premium'
    encryption: sku == 'Premium' ? {
      status: 'enabled'
    } : null
    networkRuleBypassOptions: 'AzureServices'
    networkRuleSet: sku == 'Premium' && enablePrivateEndpoint ? {
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    } : null
    policies: {
      azureADAuthenticationAsArmPolicy: {
        status: 'enabled'
      }
      exportPolicy: {
        status: 'disabled'
      }
      quarantinePolicy: {
        status: environment == 'prod' ? 'enabled' : 'disabled'
      }
      retentionPolicy: sku == 'Premium' ? {
        days: 30
        status: 'enabled'
      } : null
      trustPolicy: enableContentTrust ? {
        type: 'Notary'
        status: 'enabled'
      } : null
      softDeletePolicy: sku == 'Premium' ? {
        retentionDays: 7
        status: 'enabled'
      } : null
    }
    publicNetworkAccess: (sku == 'Premium' && enablePrivateEndpoint) ? 'Disabled' : 'Enabled'
    zoneRedundancy: sku == 'Premium' ? 'Enabled' : 'Disabled'
  }
}

// Geo-replication for Premium SKU
resource geoReplication 'Microsoft.ContainerRegistry/registries/replications@2023-07-01' = [for replicaLocation in geoReplicationLocations: if (sku == 'Premium' && !empty(geoReplicationLocations)) {
  parent: containerRegistry
  name: replicaLocation
  location: replicaLocation
  tags: tags
  properties: {
    regionEndpointEnabled: true
    zoneRedundancy: 'Enabled'
  }
}]

// Private Endpoint for ACR (Premium SKU only)
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (sku == 'Premium' && enablePrivateEndpoint && !empty(subnetId)) {
  name: '${registryName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${registryName}-plsc'
        properties: {
          privateLinkServiceId: containerRegistry.id
          groupIds: [
            'registry'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone Group for ACR
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (sku == 'Premium' && enablePrivateEndpoint && !empty(subnetId) && !empty(privateDnsZoneId)) {
  parent: privateEndpoint
  name: 'acr-private-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'acr-private-dns-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${registryName}-diagnostics'
  scope: containerRegistry
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'ContainerRegistryRepositoryEvents'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'ContainerRegistryLoginEvents'
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
@description('Azure Container Registry ID')
output registryId string = containerRegistry.id

@description('Azure Container Registry Name')
output registryName string = containerRegistry.name

@description('Azure Container Registry Login Server')
output loginServer string = containerRegistry.properties.loginServer

@description('Azure Container Registry resource group')
output resourceGroup string = resourceGroup().name
