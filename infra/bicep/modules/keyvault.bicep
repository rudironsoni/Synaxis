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

@description('Tags')
param tags object = {}

@description('Diagnostic settings')
param diagnosticSettings object = {}

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

// Diagnostic settings
resource diagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(diagnosticSettings)) {
  name: '${keyVaultName}-diagnostics'
  scope: keyVault
  properties: {
    workspaceId: contains(diagnosticSettings, 'workspaceId') ? diagnosticSettings.workspaceId : ''
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
