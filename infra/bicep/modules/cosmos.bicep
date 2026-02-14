// Azure Cosmos DB Module
// Epic E2-T3: Multi-Region Scale Unit Data Tier

@description('Cosmos DB account name (globally unique)')
param accountName string

@description('Azure region (primary)')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Replica regions for multi-region writes')
param replicaRegions array = environment == 'prod' ? ['westeurope', 'eastus', 'southeastasia'] : ['westeurope']

@description('Enable multi-region writes')
param enableMultiRegionWrites bool = true

@description('Default consistency level')
@allowed(['Eventual', 'Session', 'BoundedStaleness', 'Strong'])
param defaultConsistencyLevel string = 'Session'

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Subnet ID for private endpoint')
param subnetId string = ''

@description('Private DNS Zone ID for Cosmos DB')
param privateDnsZoneId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags')
param tags object = {}

// Database and container definitions
var databases = [
  {
    name: 'synaxis-tenant-db'
    containers: [
      {
        name: 'tenants'
        partitionKey: '/tenantId'
        throughput: 1000
        indexingPolicy: {
          automatic: true
          indexingMode: 'consistent'
          includedPaths: [
            { path: '/*' }
          ]
          excludedPaths: [
            { path: '/"_etag"/?' }
          ]
        }
      }
      {
        name: 'conversations'
        partitionKey: '/tenantId'
        throughput: 2000
        indexingPolicy: {
          automatic: true
          indexingMode: 'consistent'
          includedPaths: [
            { path: '/*' }
          ]
          excludedPaths: [
            { path: '/content/*' }
            { path: '/"_etag"/?' }
          ]
        }
        defaultTtl: 7776000  // 90 days in seconds
      }
    ]
  }
  {
    name: 'synaxis-audit-db'
    containers: [
      {
        name: 'request-logs'
        partitionKey: '/timestamp'
        throughput: 1000
        indexingPolicy: {
          automatic: true
          indexingMode: 'consistent'
          includedPaths: [
            { path: '/*' }
          ]
          excludedPaths: [
            { path: '/"_etag"/?' }
          ]
        }
        defaultTtl: 2592000  // 30 days in seconds
      }
    ]
  }
]

// Cosmos DB Account
resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: accountName
  location: location
  tags: union(tags, {
    environment: environment
    purpose: 'synaxis-multi-tenant-data'
  })
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [for region in replicaRegions: {
      locationName: region
      failoverPriority: region == location ? 0 : 1
      isZoneRedundant: true
    }]
    enableMultipleWriteLocations: enableMultiRegionWrites
    defaultConsistencyLevel: defaultConsistencyLevel
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    backupPolicy: {
      type: 'Continuous'
      continuousModeProperties: {
        tier: 'Continuous30Days'
      }
    }
    publicNetworkAccess: enablePrivateEndpoint ? 'Disabled' : 'Enabled'
    networkAclBypass: 'AzureServices'
    disableLocalAuth: true  // Enforce Entra ID only
    enablePartitionMerge: true
    enablePerRegionPerPartitionAutoscale: true
    analyticalStorageConfiguration: {
      schemaType: 'FullFidelity'
    }
  }
}

// Create databases and containers
resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = [for db in databases: {
  parent: cosmosAccount
  name: db.name
  properties: {
    resource: {
      id: db.name
    }
    options: {}
  }
}]

// Create containers
resource cosmosContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = [for (db, dbIndex) in databases: {
  parent: cosmosDatabase[dbIndex]
  name: db.containers[0].name
  properties: {
    resource: {
      id: db.containers[0].name
      partitionKey: {
        paths: [
          db.containers[0].partitionKey
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: db.containers[0].indexingPolicy
      defaultTtl: contains(db.containers[0], 'defaultTtl') ? db.containers[0].defaultTtl : null
    }
    options: {
      autoscaleSettings: {
        maxThroughput: db.containers[0].throughput * 10
      }
    }
  }
}]

// Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId)) {
  name: '${accountName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${accountName}-plsc'
        properties: {
          privateLinkServiceId: cosmosAccount.id
          groupIds: [
            'Sql'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId) && !empty(privateDnsZoneId)) {
  parent: privateEndpoint
  name: 'cosmos-private-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'cosmos-private-dns-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${accountName}-diagnostics'
  scope: cosmosAccount
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'DataPlaneRequests'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'ControlPlaneRequests'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'QueryRuntimeStatistics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
    ]
    metrics: [
      {
        category: 'Requests'
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
@description('Cosmos DB Account ID')
output accountId string = cosmosAccount.id

@description('Cosmos DB Account Name')
output accountName string = cosmosAccount.name

@description('Cosmos DB Endpoint')
output endpoint string = cosmosAccount.properties.documentEndpoint

@description('Primary Read-Write Connection String (for reference only, use Managed Identity in production)')
#disable-next-line outputs-should-not-contain-secrets
output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

@description('Database Names')
output databaseNames array = [for db in databases: db.name]
