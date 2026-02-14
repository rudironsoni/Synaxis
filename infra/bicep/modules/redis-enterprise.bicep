// Azure Cache for Redis Enterprise Module
// Epic E2-T4: Distributed Cache and Session Store

@description('Redis Enterprise cluster name')
param clusterName string

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Redis Enterprise SKU')
@allowed(['Enterprise_E10', 'Enterprise_E20', 'Enterprise_E50', 'Enterprise_E100'])
param sku string = environment == 'prod' ? 'Enterprise_E10' : 'Enterprise_E10'

@description('Number of shards for clustering')
@minValue(1)
@maxValue(10)
param shardCount int = 6

@description('Enable zone redundancy')
param zoneRedundant bool = true

@description('Enable persistence')
@allowed(['Disabled', 'RDB', 'AOF'])
param persistenceMode string = 'AOF'

@description('AOF frequency')
@allowed(['always', 'every-second', 'every-write'])
param aofFrequency string = 'every-second'

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Subnet ID for private endpoint')
param subnetId string = ''

@description('Private DNS Zone ID for Redis')
param privateDnsZoneId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Key Vault ID for storing connection strings')
param keyVaultId string = ''

@description('Tags')
param tags object = {}

// Database configurations
var databases = [
  {
    name: 'session-cache'
    evictionPolicy: 'AllKeysLRU'
    persistence: true
    clustering: true
    port: 10000
  }
  {
    name: 'rate-limiter'
    evictionPolicy: 'NoEviction'
    persistence: true
    clustering: true
    port: 10001
  }
  {
    name: 'response-cache'
    evictionPolicy: 'VolatileLRU'
    persistence: false
    clustering: true
    port: 10002
  }
  {
    name: 'routing-state'
    evictionPolicy: 'NoEviction'
    persistence: true
    clustering: true
    port: 10003
  }
]

// Redis Enterprise Cluster
resource redisCluster 'Microsoft.Cache/redisEnterprise@2023-07-01' = {
  name: clusterName
  location: location
  tags: union(tags, {
    environment: environment
    purpose: 'synaxis-distributed-cache'
  })
  sku: {
    name: sku
  }
  properties: {
    minimumTlsVersion: '1.2'
    zones: zoneRedundant ? ['1', '2', '3'] : []
  }
}

// Create Redis databases
resource redisDatabase 'Microsoft.Cache/redisEnterprise/databases@2023-07-01' = [for db in databases: {
  parent: redisCluster
  name: db.name
  properties: {
    clientProtocol: 'Encrypted'
    port: db.port
    clustering: {
      shardCount: shardCount
    }
    evictionPolicy: db.evictionPolicy
    persistence: db.persistence ? {
      aofEnabled: persistenceMode == 'AOF'
      aofFrequency: persistenceMode == 'AOF' ? aofFrequency : null
      rdbEnabled: persistenceMode == 'RDB'
    } : {
      aofEnabled: false
      rdbEnabled: false
    }
    modules: []
  }
}]

// Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId)) {
  name: '${clusterName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${clusterName}-plsc'
        properties: {
          privateLinkServiceId: redisCluster.id
          groupIds: [
            'redisEnterprise'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId) && !empty(privateDnsZoneId)) {
  parent: privateEndpoint
  name: 'redis-private-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'redis-private-dns-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${clusterName}-diagnostics'
  scope: redisCluster
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'redisAuditLogs'
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

// Store connection strings in Key Vault
resource keyVaultSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for (db, index) in databases: if (!empty(keyVaultId)) {
  name: '${last(split(keyVaultId, '/'))}/redis-${db.name}-connection-string'
  properties: {
    value: redisDatabase[index].listKeys().primaryKey
    contentType: 'text/plain'
    attributes: {
      enabled: true
    }
  }
}]

// Outputs
@description('Redis Enterprise Cluster ID')
output clusterId string = redisCluster.id

@description('Redis Enterprise Cluster Name')
output clusterName string = redisCluster.name

@description('Redis Hostname')
output hostname string = redisCluster.properties.hostName

@description('Database Names')
output databaseNames array = [for db in databases: db.name]

@description('Database Ports')
output databasePorts array = [for db in databases: db.port]

@description('Primary Access Key (for reference only, use Managed Identity in production)')
#disable-next-line outputs-should-not-contain-secrets
output primaryKey string = redisDatabase[0].listKeys().primaryKey
