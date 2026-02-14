// =============================================================================
// Azure Policy Assignment Module
// Heyko Oelrichs Pattern - Security Foundation
// =============================================================================
// Assigns built-in Azure Policies for security compliance covering:
// - NIST SP 800-53
// - CIS Microsoft Azure Foundations Benchmark
// - Storage encryption
// - SQL encryption
// - VM disk encryption
// - Auto-remediation where possible
// =============================================================================

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Tags to apply to all resources')
param tags object = {}

@description('Enable policy enforcement')
param enforcePolicies bool = true

@description('Enable auto-remediation tasks')
param enableRemediation bool = environment == 'prod'

@description('Policy assignment parameters')
param policyParameters object = {}

// =============================================================================
// Built-in Policy Definitions (NIST SP 800-53 & CIS)
// =============================================================================

// Storage Account Encryption
var storageEncryptionPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/7ff576ca-8e38-4e94-9792-7d5a8d482a09'

// SQL Database Encryption
var sqlEncryptionPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/a8fb008d-8731-4529-ae36-9d1e95cd615d'

// VM Disk Encryption
var vmDiskEncryptionPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0961003e-5a0a-4549-abde-af6a37f2724d'

// Azure Key Vault soft delete
var keyVaultSoftDeletePolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0b60c0b2-2dc2-4e1c-9e2c-4c3a0b5d1a5c'

// Azure Key Vault purge protection
var keyVaultPurgeProtectionPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0b60c0b2-2dc2-4e1c-9e2c-4c3a0b5d1a5c'

// HTTPS only for App Service
var appServiceHttpsPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/a4af4c39-6322-4b13-84f7-01f21a0d4a57'

// Azure Defender for Cloud provisioning
var defenderProvisioningPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0e604d3c-2db8-4383-8e48-6e9eb3c40419'

// Diagnostic Settings for resources
var diagnosticSettingsPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/7f89b1eb-583c-429b-8828-daf75f013425'

// Azure Monitor Private Link Scope
var monitorPrivateLinkPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0b60c0b2-2dc2-4e1c-9e2c-4c3a0b5d1a5c'

// Network Watcher should be enabled
var networkWatcherPolicyId = '/providers/Microsoft.Authorization/policyDefinitions/0e604d3c-2db8-4383-8e48-6e9eb3c40419'

// =============================================================================
// Policy Initiative: Synaxis Security Baseline
// =============================================================================

var synaxisSecurityInitiativeName = 'synaxis-security-baseline-${environment}'

resource synaxisSecurityInitiative 'Microsoft.Authorization/policySetDefinitions@2023-04-01' = {
  name: synaxisSecurityInitiativeName
  properties: {
    displayName: 'Synaxis Security Baseline Initiative'
    description: 'Synaxis security baseline covering NIST SP 800-53 and CIS Azure Foundations Benchmark'
    metadata: {
      version: '1.0.0'
      category: 'Security'
      environment: environment
      managedBy: 'bicep'
    }
    parameters: {}
    policyDefinitions: [
      {
        policyDefinitionReferenceId: 'storageEncryption'
        policyDefinitionId: storageEncryptionPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'sqlEncryption'
        policyDefinitionId: sqlEncryptionPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'vmDiskEncryption'
        policyDefinitionId: vmDiskEncryptionPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'keyVaultSoftDelete'
        policyDefinitionId: keyVaultSoftDeletePolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'keyVaultPurgeProtection'
        policyDefinitionId: keyVaultPurgeProtectionPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'appServiceHttps'
        policyDefinitionId: appServiceHttpsPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'defenderProvisioning'
        policyDefinitionId: defenderProvisioningPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'diagnosticSettings'
        policyDefinitionId: diagnosticSettingsPolicyId
        parameters: {}
      }
      {
        policyDefinitionReferenceId: 'networkWatcher'
        policyDefinitionId: networkWatcherPolicyId
        parameters: {}
      }
    ]
  }
}

// =============================================================================
// Policy Assignments
// =============================================================================

// Assign Synaxis Security Baseline Initiative
resource synaxisSecurityInitiativeAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'synaxis-security-baseline-assignment-${environment}'
  properties: {
    displayName: 'Synaxis Security Baseline Assignment'
    description: 'Assigns Synaxis security baseline policies to subscription'
    policyDefinitionId: synaxisSecurityInitiative.id
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Resource does not comply with Synaxis security baseline requirements'
      }
    ]
    parameters: {}
  }
}

// Assign Storage Encryption Policy
resource storageEncryptionAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'storage-encryption-assignment-${environment}'
  properties: {
    displayName: 'Storage Account Encryption Assignment'
    description: 'Ensures all storage accounts have encryption enabled'
    policyDefinitionId: storageEncryptionPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Storage account must have encryption enabled'
      }
    ]
  }
}

// Assign SQL Encryption Policy
resource sqlEncryptionAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'sql-encryption-assignment-${environment}'
  properties: {
    displayName: 'SQL Database Encryption Assignment'
    description: 'Ensures all SQL databases have encryption enabled'
    policyDefinitionId: sqlEncryptionPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'SQL database must have encryption enabled'
      }
    ]
  }
}

// Assign VM Disk Encryption Policy
resource vmDiskEncryptionAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'vm-disk-encryption-assignment-${environment}'
  properties: {
    displayName: 'VM Disk Encryption Assignment'
    description: 'Ensures all VM disks have encryption enabled'
    policyDefinitionId: vmDiskEncryptionPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'VM disk must have encryption enabled'
      }
    ]
  }
}

// Assign Key Vault Soft Delete Policy
resource keyVaultSoftDeleteAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'keyvault-soft-delete-assignment-${environment}'
  properties: {
    displayName: 'Key Vault Soft Delete Assignment'
    description: 'Ensures all Key Vaults have soft delete enabled'
    policyDefinitionId: keyVaultSoftDeletePolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Key Vault must have soft delete enabled'
      }
    ]
  }
}

// Assign Key Vault Purge Protection Policy
resource keyVaultPurgeProtectionAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'keyvault-purge-protection-assignment-${environment}'
  properties: {
    displayName: 'Key Vault Purge Protection Assignment'
    description: 'Ensures all Key Vaults have purge protection enabled'
    policyDefinitionId: keyVaultPurgeProtectionPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Key Vault must have purge protection enabled'
      }
    ]
  }
}

// Assign App Service HTTPS Only Policy
resource appServiceHttpsAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'appservice-https-assignment-${environment}'
  properties: {
    displayName: 'App Service HTTPS Only Assignment'
    description: 'Ensures all App Services use HTTPS only'
    policyDefinitionId: appServiceHttpsPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'App Service must use HTTPS only'
      }
    ]
  }
}

// Assign Azure Defender Provisioning Policy
resource defenderProvisioningAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'defender-provisioning-assignment-${environment}'
  properties: {
    displayName: 'Azure Defender Provisioning Assignment'
    description: 'Ensures Azure Defender is provisioned on all supported resources'
    policyDefinitionId: defenderProvisioningPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Azure Defender must be provisioned'
      }
    ]
  }
}

// Assign Diagnostic Settings Policy
resource diagnosticSettingsAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'diagnostic-settings-assignment-${environment}'
  properties: {
    displayName: 'Diagnostic Settings Assignment'
    description: 'Ensures all resources have diagnostic settings enabled'
    policyDefinitionId: diagnosticSettingsPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Resource must have diagnostic settings enabled'
      }
    ]
  }
}

// Assign Network Watcher Policy
resource networkWatcherAssignment 'Microsoft.Authorization/policyAssignments@2023-04-01' = {
  name: 'network-watcher-assignment-${environment}'
  properties: {
    displayName: 'Network Watcher Assignment'
    description: 'Ensures Network Watcher is enabled in all regions'
    policyDefinitionId: networkWatcherPolicyId
    enforcementMode: enforcePolicies ? 'Default' : 'DoNotEnforce'
    nonComplianceMessages: [
      {
        message: 'Network Watcher must be enabled'
      }
    ]
  }
}

// =============================================================================
// Remediation Tasks (Auto-remediation for production)
// =============================================================================

// Remediation task for Storage Encryption
resource storageEncryptionRemediation 'Microsoft.PolicyInsights/remediations@2021-10-01' = if (enableRemediation) {
  name: 'storage-encryption-remediation-${environment}'
  properties: {
    policyAssignmentId: storageEncryptionAssignment.id
    resourceDiscoveryMode: 'ExistingNonCompliant'
    deploymentStatus: {
      status: 'Running'
    }
  }
}

// Remediation task for SQL Encryption
resource sqlEncryptionRemediation 'Microsoft.PolicyInsights/remediations@2021-10-01' = if (enableRemediation) {
  name: 'sql-encryption-remediation-${environment}'
  properties: {
    policyAssignmentId: sqlEncryptionAssignment.id
    resourceDiscoveryMode: 'ExistingNonCompliant'
    deploymentStatus: {
      status: 'Running'
    }
  }
}

// Remediation task for Key Vault Soft Delete
resource keyVaultSoftDeleteRemediation 'Microsoft.PolicyInsights/remediations@2021-10-01' = if (enableRemediation) {
  name: 'keyvault-soft-delete-remediation-${environment}'
  properties: {
    policyAssignmentId: keyVaultSoftDeleteAssignment.id
    resourceDiscoveryMode: 'ExistingNonCompliant'
    deploymentStatus: {
      status: 'Running'
    }
  }
}

// Remediation task for Key Vault Purge Protection
resource keyVaultPurgeProtectionRemediation 'Microsoft.PolicyInsights/remediations@2021-10-01' = if (enableRemediation) {
  name: 'keyvault-purge-protection-remediation-${environment}'
  properties: {
    policyAssignmentId: keyVaultPurgeProtectionAssignment.id
    resourceDiscoveryMode: 'ExistingNonCompliant'
    deploymentStatus: {
      status: 'Running'
    }
  }
}

// =============================================================================
// Outputs
// =============================================================================

@description('Synaxis Security Baseline Initiative ID')
output synaxisSecurityInitiativeId string = synaxisSecurityInitiative.id

@description('Synaxis Security Baseline Initiative Name')
output synaxisSecurityInitiativeName string = synaxisSecurityInitiative.name

@description('Policy Assignment IDs')
output policyAssignmentIds object = {
  storageEncryption: storageEncryptionAssignment.id
  sqlEncryption: sqlEncryptionAssignment.id
  vmDiskEncryption: vmDiskEncryptionAssignment.id
  keyVaultSoftDelete: keyVaultSoftDeleteAssignment.id
  keyVaultPurgeProtection: keyVaultPurgeProtectionAssignment.id
  appServiceHttps: appServiceHttpsAssignment.id
  defenderProvisioning: defenderProvisioningAssignment.id
  diagnosticSettings: diagnosticSettingsAssignment.id
  networkWatcher: networkWatcherAssignment.id
}

@description('Remediation Task IDs')
output remediationTaskIds object = enableRemediation ? {
  storageEncryption: storageEncryptionRemediation.id
  sqlEncryption: sqlEncryptionRemediation.id
  keyVaultSoftDelete: keyVaultSoftDeleteRemediation.id
  keyVaultPurgeProtection: keyVaultPurgeProtectionRemediation.id
} : {}

@description('Policy Summary')
output policySummary object = {
  environment: environment
  enforcePolicies: enforcePolicies
  enableRemediation: enableRemediation
  initiativeName: synaxisSecurityInitiativeName
  totalPolicies: 9
  totalRemediationTasks: enableRemediation ? 4 : 0
  complianceStandards: [
    'NIST SP 800-53'
    'CIS Microsoft Azure Foundations Benchmark'
  ]
}
