// Azure Database for PostgreSQL Flexible Server with Private Endpoint
// Heyko Oelrichs pattern: Private Link, CMK encryption, no public access

@description('Environment name')
param environment string

@description('Azure region')
param location string = resourceGroup().location

@description('Virtual Network ID')
param virtualNetworkId string

@description('Private Endpoints subnet ID')
param privateEndpointSubnetId string

@description('Key Vault ID for CMK')
param keyVaultId string

@description('Key name for CMK')
param keyName string

@description('Administrator login')
@secure()
param administratorLogin string

@description('Administrator password')
@secure()
param administratorPassword string

// PostgreSQL Flexible Server
resource postgresql 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: 'synaxis-postgres-${environment}'
  location: location
  sku: {
    name: 'Standard_D4s_v3'
    tier: 'GeneralPurpose'
  }
  properties: {
    version: '16'
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: 256
      autoGrow: 'Enabled'
    }
    highAvailability: {
      mode: 'ZoneRedundant'
      standbyAvailabilityZone: '2'
    }
    backup: {
      backupRetentionDays: 35
      geoRedundantBackup: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Disabled'
      delegatedSubnetResourceId: privateEndpointSubnetId
      privateDnsZoneArmResourceId: privateDnsZone.id
    }
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Enabled'
    }
    dataEncryption: {
      type: 'AzureKeyVault'
      geoBackupEncryptionKeyStatus: 'Disabled'
      primaryKeyURI: '${keyVault.properties.vaultUri}keys/${keyName}'
      primaryUserAssignedIdentityId: userAssignedIdentity.id
    }
    availabilityZone: '1'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  tags: {
    environment: environment
    managedBy: 'bicep'
    purpose: 'synaxis-database'
  }
}

// User Assigned Identity for CMK
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'synaxis-postgres-identity-${environment}'
  location: location
}

// Key Vault Access Policy for PostgreSQL
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  name: '${last(split(keyVaultId, '/'))}/add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: userAssignedIdentity.properties.principalId
        permissions: {
          keys: [
            'get'
            'wrapKey'
            'unwrapKey'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone for PostgreSQL
resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  properties: {}
}

// Link Private DNS Zone to VNet
resource privateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  name: 'synaxis-postgres-dns-link'
  parent: privateDnsZone
  location: 'global'
  properties: {
    virtualNetwork: {
      id: virtualNetworkId
    }
    registrationEnabled: false
  }
}

// Private Endpoint for PostgreSQL
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-11-01' = {
  name: 'synaxis-postgres-pe'
  location: location
  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'synaxis-postgres-connection'
        properties: {
          privateLinkServiceId: postgresql.id
          groupIds: [
            'postgresqlServer'
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
  name: 'synaxis-postgres-${environment}'
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
  name: 'synaxis-postgres-diagnostics'
  scope: postgresql
  properties: {
    logs: [
      {
        category: 'PostgreSQLLogs'
        enabled: true
      }
      {
        category: 'PostgreSQLFlexSessions'
        enabled: true
      }
      {
        category: 'PostgreSQLFlexQueryStoreRuntime'
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
output postgresqlId string = postgresql.id
output postgresqlName string = postgresql.name
output postgresqlFqdn string = postgresql.properties.fullyQualifiedDomainName
output privateEndpointIp string = privateEndpoint.properties.networkInterfaces[0].properties.ipConfigurations[0].properties.privateIPAddress
output connectionString string = 'postgresql://${administratorLogin}@${postgresql.name}.postgres.database.azure.com:5432/synaxis?sslmode=require'
