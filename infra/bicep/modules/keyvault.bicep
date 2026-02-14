// Key Vault module with HSM support (Heyko Oelrichs pattern)
// Requires Premium SKU for HSM-backed keys

@description('Key Vault name')
param keyVaultName string

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Enable HSM-backed keys (required for production)')
param enableHsm bool = environment == 'prod'

@description('Soft delete retention in days')
param softDeleteRetentionInDays int = 90

@description('Enable purge protection')
param enablePurgeProtection bool = true

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags')
param tags object = {}

// Key Vault resource
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: union(tags, {
    environment: environment
    managedBy: 'bicep'
    purpose: 'synaxis-secrets'
  })
  properties: {
    sku: {
      family: 'A'
      name: enableHsm ? 'premium' : 'standard'
    }
    tenantId: subscription().tenantId
    
    // Security settings
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection
    enableRbacAuthorization: true
    
    // Network access - private only
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass: 'None'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: []
    }
    
    // Access policies (empty - using RBAC)
    accessPolicies: []
  }
}

// Tenant data encryption key (HSM-backed)
resource tenantDataKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = if (enableHsm) {
  parent: keyVault
  name: 'tenant-data-encryption'
  tags: union(tags, {
    purpose: 'tenant-data-encryption'
    autoRotate: 'true'
  })
  properties: {
    kty: 'RSA-HSM'
    keySize: 4096
    keyOps: [
      'encrypt'
      'decrypt'
      'wrapKey'
      'unwrapKey'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: {
      lifetimeActions: [
        {
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
          action: {
            type: 'Rotate'
          }
        }
      ]
      attributes: {
        expiryTime: 'P90D'
      }
    }
  }
}

// API key encryption key (HSM-backed)
resource apiKeyEncryptionKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = if (enableHsm) {
  parent: keyVault
  name: 'api-key-encryption'
  tags: union(tags, {
    purpose: 'api-key-encryption'
    autoRotate: 'true'
  })
  properties: {
    kty: 'RSA-HSM'
    keySize: 2048
    keyOps: [
      'encrypt'
      'decrypt'
    ]
    attributes: {
      enabled: true
      exportable: false
    }
    rotationPolicy: {
      lifetimeActions: [
        {
          trigger: {
            timeBeforeExpiry: 'P30D'
          }
          action: {
            type: 'Rotate'
          }
        }
      ]
      attributes: {
        expiryTime: 'P90D'
      }
    }
  }
}

// Placeholder secrets for provider API keys
resource openAiSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'openai-api-key'
  tags: union(tags, {
    provider: 'openai'
    status: 'placeholder'
  })
  properties: {
    value: 'PLACEHOLDER-UPDATE-MANUALLY'
    contentType: 'text/plain'
    attributes: {
      enabled: false  // Disabled until manually set
    }
  }
}

resource azureOpenAiSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'azure-openai-api-key'
  tags: union(tags, {
    provider: 'azure-openai'
    status: 'placeholder'
  })
  properties: {
    value: 'PLACEHOLDER-UPDATE-MANUALLY'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

resource anthropicSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'anthropic-api-key'
  tags: union(tags, {
    provider: 'anthropic'
    status: 'placeholder'
  })
  properties: {
    value: 'PLACEHOLDER-UPDATE-MANUALLY'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

resource googleSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'google-api-key'
  tags: union(tags, {
    provider: 'google'
    status: 'placeholder'
  })
  properties: {
    value: 'PLACEHOLDER-UPDATE-MANUALLY'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

// =============================================================================
// Connection String Secrets
// =============================================================================

resource postgresConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'postgres-connection-string'
  tags: union(tags, {
    purpose: 'database-connection'
    database: 'postgresql'
    status: 'placeholder'
  })
  properties: {
    value: 'postgresql://synaxisadmin:PLACEHOLDER-PASSWORD@synaxis-pg-${environment}-${location}.postgres.database.azure.com:5432/synaxis?sslmode=require'
    contentType: 'text/plain'
    attributes: {
      enabled: false  // Disabled until manually set with actual connection string
    }
  }
}

resource cosmosConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'cosmos-connection-string'
  tags: union(tags, {
    purpose: 'database-connection'
    database: 'cosmosdb'
    status: 'placeholder'
  })
  properties: {
    value: 'AccountEndpoint=https://synaxis-cosmos-${environment}-${location}.documents.azure.com:443/;AccountKey=PLACEHOLDER-KEY;'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

resource redisConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'redis-connection-string'
  tags: union(tags, {
    purpose: 'cache-connection'
    cache: 'redis'
    status: 'placeholder'
  })
  properties: {
    value: 'synaxis-redis-${environment}-${location}.redis.cache.windows.net:6380,password=PLACEHOLDER-PASSWORD,ssl=True,abortConnect=False'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

resource redisEnterpriseConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'redis-enterprise-connection-string'
  tags: union(tags, {
    purpose: 'cache-connection'
    cache: 'redis-enterprise'
    status: 'placeholder'
  })
  properties: {
    value: 'synaxis-redis-ent-${environment}-${location}.redisenterprise.cache.azure.net:10000,password=PLACEHOLDER-PASSWORD,ssl=True,abortConnect=False'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

resource serviceBusConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'servicebus-connection-string'
  tags: union(tags, {
    purpose: 'messaging-connection'
    messaging: 'servicebus'
    status: 'placeholder'
  })
  properties: {
    value: 'Endpoint=sb://synaxis-sb-${environment}-${location}.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=PLACEHOLDER-KEY;'
    contentType: 'text/plain'
    attributes: {
      enabled: false
    }
  }
}

// =============================================================================
// TLS Certificates (Placeholders - to be updated with actual certificates)
// =============================================================================

resource tlsCertificate 'Microsoft.KeyVault/vaults/certificates@2023-07-01' = {
  parent: keyVault
  name: 'synaxis-tls-cert'
  tags: union(tags, {
    purpose: 'tls-certificate'
    status: 'placeholder'
  })
  properties: {
    certificatePolicy: {
      issuerParameters: {
        name: 'Self'
      }
      keyProperties: {
        keyType: 'RSA'
        keySize: 4096
        exportable: true
        reuseKey: true
      }
      secretProperties: {
        contentType: 'application/x-pkcs12'
      }
      x509CertificateProperties: {
        subject: 'CN=synaxis-${environment}.synaxis.io'
        subjectAlternativeNames: {
          dnsNames: [
            'synaxis-${environment}.synaxis.io'
            '*.synaxis-${environment}.synaxis.io'
          ]
        }
        validityInMonths: 12
        keyUsage: [
          'cRLSign'
          'dataEncipherment'
          'digitalSignature'
          'keyEncipherment'
          'keyAgreement'
          'keyCertSign'
        ]
        enhancedKeyUsage: [
          '1.3.6.1.5.5.7.3.1'  # Server Authentication
          '1.3.6.1.5.5.7.3.2'  # Client Authentication
        ]
      }
      lifetimeActions: [
        {
          trigger: {
            percentageThreshold: 80
          }
          action: {
            actionType: 'AutoRenew'
          }
        }
      ]
    }
    attributes: {
      enabled: false  // Disabled until manually configured with proper CA
    }
  }
}

resource ingressCertificate 'Microsoft.KeyVault/vaults/certificates@2023-07-01' = {
  parent: keyVault
  name: 'synaxis-ingress-cert'
  tags: union(tags, {
    purpose: 'ingress-tls-certificate'
    status: 'placeholder'
  })
  properties: {
    certificatePolicy: {
      issuerParameters: {
        name: 'Self'
      }
      keyProperties: {
        keyType: 'RSA'
        keySize: 2048
        exportable: true
        reuseKey: true
      }
      secretProperties: {
        contentType: 'application/x-pkcs12'
      }
      x509CertificateProperties: {
        subject: 'CN=*.synaxis-ingress-${environment}.synaxis.io'
        subjectAlternativeNames: {
          dnsNames: [
            '*.synaxis-ingress-${environment}.synaxis.io'
            'synaxis-ingress-${environment}.synaxis.io'
          ]
        }
        validityInMonths: 12
        keyUsage: [
          'digitalSignature'
          'keyEncipherment'
        ]
        enhancedKeyUsage: [
          '1.3.6.1.5.5.7.3.1'  # Server Authentication
        ]
      }
      lifetimeActions: [
        {
          trigger: {
            percentageThreshold: 80
          }
          action: {
            actionType: 'AutoRenew'
          }
        }
      ]
    }
    attributes: {
      enabled: false  // Disabled until manually configured with proper CA
    }
  }
}

// Diagnostic settings
resource diagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${keyVaultName}-diagnostics'
  scope: keyVault
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
      {
        category: 'AzurePolicyEvaluationDetails'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
    ]
  }
}

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output tenantDataKeyId string = enableHsm ? tenantDataKey.id : ''
output tenantDataKeyName string = enableHsm ? tenantDataKey.name : ''
output apiKeyEncryptionKeyId string = enableHsm ? apiKeyEncryptionKey.id : ''
output apiKeyEncryptionKeyName string = enableHsm ? apiKeyEncryptionKey.name : ''
