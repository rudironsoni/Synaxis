// Azure Cache for Redis with Private Endpoint
// Heyko Oelrichs pattern: Private Link, clustering, persistence

@description('Environment name')
param environment string

@description('Azure region')
param location string = resourceGroup().location

@description('Virtual Network ID')
param virtualNetworkId string

@description('Private Endpoints subnet ID')
param privateEndpointSubnetId string

@description('Redis SKU')
@allowed([
  'Premium'
  'Enterprise'
])
param sku string = 'Premium'

// Azure Cache for Redis
resource redis 'Microsoft.Cache/redis@2023-08-01' = {
  name: 'synaxis-redis-${environment}'
  location: location
  sku: {
    name: sku
    family: 'P'
    capacity: 1  // 6 GB for Premium P1
  }
  properties: {
    redisVersion: '6.0'
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    sku: {
      name: sku
      family: 'P'
      capacity: 1
    }
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
      'maxmemory-reserved': '50'
      'maxfragmentationmemory-reserved': '50'
      'maxmemory-delta': '50'
      'aof-backup-enabled': 'true'
      'aof-storage-connection-string-0': storageConnectionString
      'aof-storage-connection-string-1': storageConnectionString
    }
    shardCount: 2  // Clustering enabled
    replicasPerMaster: 1
    replicasPerPrimary: 1
    zones: [
      '1'
      '2'
      '3'
    ]
  }
  zones: [
    '1'
    '2'
    '3'
  ]
  tags: {
    environment: environment
    managedBy: 'bicep'
    purpose: 'synaxis-cache'
  }
}

// Storage Account for Redis Persistence (AOF)
resource redisStorage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: 'synaxisredis${environment}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
      virtualNetworkRules: [
        {
          id: privateEndpointSubnetId
          action: 'Allow'
        }
      ]
    }
  }
}

// Storage Container for Redis AOF
resource redisContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  name: '${redisStorage.name}/default/redis-aof'
  properties: {
    publicAccess: 'None'
  }
}

// Storage Connection String for Redis
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${redisStorage.name};AccountKey=${redisStorage.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

// Private DNS Zone for Redis
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.redis.cache.windows.net'
  location: 'global'
  properties: {}
}

// Link Private DNS Zone to VNet
resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'synaxis-redis-dns-link'
  parent: privateDnsZone
  location: 'global'
  properties: {
    virtualNetwork: {
      id: virtualNetworkId
    }
    registrationEnabled: false
  }
}

// Private Endpoint for Redis
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: 'synaxis-redis-pe'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'synaxis-redis-connection'
        properties: {
          privateLinkServiceId: redis.id
          groupIds: [
            'redisCache'
          ]
        }
      }
    ]
  }
  tags: {
    environment: environment
    managedBy: 'bicep'
  }
}

// A Record in Private DNS Zone
resource aRecord 'Microsoft.Network/privateDnsZones/A@2020-06-01' = {
  name: 'synaxis-redis-${environment}'
  parent: privateDnsZone
  properties: {
    ttl: 300
    aRecords: [
      {
        ipv4Address: privateEndpoint.properties.networkInterfaces[0].properties.ipConfigurations[0].properties.privateIPAddress
      }
    ]
  }
}

// Diagnostic Settings
resource diagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'synaxis-redis-diagnostics'
  scope: redis
  properties: {
    logs: [
      {
        category: 'ConnectedClientList'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// Outputs
output redisId string = redis.id
output redisName string = redis.name
output redisHostName string = redis.properties.hostName
output redisSslPort int = redis.properties.sslPort
output redisPrimaryKey string = redis.listKeys().primaryKey
output privateEndpointIp string = privateEndpoint.properties.networkInterfaces[0].properties.ipConfigurations[0].properties.privateIPAddress
output connectionString string = '${redis.properties.hostName}:${redis.properties.sslPort},password=${redis.listKeys().primaryKey},ssl=True,abortConnect=False'
