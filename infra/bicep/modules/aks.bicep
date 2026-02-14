// Azure Kubernetes Service (AKS) Module
// Hansjoerg Scherer Pattern: Ephemeral Scale Unit Architecture

@description('AKS cluster name')
param clusterName string

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Kubernetes version')
param kubernetesVersion string = '1.29'

@description('Availability zones for the cluster')
param availabilityZones array = ['1', '2', '3']

@description('Virtual Network ID')
param vnetId string

@description('AKS subnet ID')
param aksSubnetId string

@description('Log Analytics workspace ID for Container Insights')
param logAnalyticsWorkspaceId string

@description('Key Vault ID for secrets access')
param keyVaultId string

@description('Azure Container Registry ID')
param acrId string = ''

@description('Enable private cluster')
param enablePrivateCluster bool = true

@description('Private DNS zone mode for private cluster')
@allowed(['system', 'none'])
param privateDnsZone string = 'system'

@description('Tags')
param tags object = {}

@description('SSH public key for Linux nodes')
@secure()
param sshPublicKey string

// System Node Pool Configuration
var systemNodePool = {
  name: 'system'
  count: 3
  vmSize: 'Standard_D4s_v5'
  osDiskSizeGB: 128
  maxPods: 110
  minCount: 3
  maxCount: 5
  enableAutoScaling: true
  taints: [
    'CriticalAddonsOnly=true:NoSchedule'
  ]
  labels: {
    'node-type': 'system'
    'environment': environment
  }
  mode: 'System'
}

// General Workload Node Pool
var generalWorkloadNodePool = {
  name: 'general'
  count: 3
  vmSize: 'Standard_D8s_v5'
  osDiskSizeGB: 256
  maxPods: 110
  minCount: 3
  maxCount: 50
  enableAutoScaling: true
  taints: []
  labels: {
    'node-type': 'general'
    'workload': 'general'
    'environment': environment
  }
  mode: 'User'
}

// GPU Node Pool (NVIDIA GPU workloads)
var gpuNodePool = {
  name: 'gpu'
  count: 0
  vmSize: 'Standard_NC24s_v3'
  osDiskSizeGB: 1024
  maxPods: 110
  minCount: 0
  maxCount: 20
  enableAutoScaling: true
  taints: [
    'nvidia.com/gpu=true:NoSchedule'
  ]
  labels: {
    'node-type': 'gpu'
    'nvidia.com/gpu': 'true'
    'workload': 'gpu-inference'
    'environment': environment
  }
  mode: 'User'
}

// Network configuration
var networkProfile = {
  networkPlugin: 'azure'
  networkPolicy: 'calico'
  loadBalancerSku: 'standard'
  outboundType: 'userDefinedRouting'
  dnsServiceIP: '10.0.32.10'
  serviceCidr: '10.0.32.0/20'
  dockerBridgeCidr: '172.17.0.1/16'
}

// Managed Identity for AKS
resource aksManagedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: '${clusterName}-mi'
  location: location
  tags: union(tags, {
    purpose: 'aks-cluster-identity'
    clusterName: clusterName
  })
}

// Role Assignment: Network Contributor (for Load Balancer management)
resource networkContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aksManagedIdentity.id, 'NetworkContributor')
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4d97b98b-1d4f-4787-a291-c67834d212e7')
    principalId: aksManagedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment: Key Vault Secrets User
resource keyVaultSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aksManagedIdentity.id, keyVaultId, 'KeyVaultSecretsUser')
  scope: resourceId('Microsoft.KeyVault/vaults', last(split(keyVaultId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: aksManagedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment: Key Vault Crypto User (for encryption at rest)
resource keyVaultCryptoUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aksManagedIdentity.id, keyVaultId, 'KeyVaultCryptoUser')
  scope: resourceId('Microsoft.KeyVault/vaults', last(split(keyVaultId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '14b46e9e-c2b7-41b4-b07b-48a6ebf60603')
    principalId: aksManagedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Role Assignment: ACR Pull (if ACR is provided)
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(acrId)) {
  name: guid(aksManagedIdentity.id, acrId, 'AcrPull')
  scope: resourceId('Microsoft.ContainerRegistry/registries', last(split(acrId, '/')))
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: aksManagedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// AKS Cluster
resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-01-02-preview' = {
  name: clusterName
  location: location
  tags: union(tags, {
    environment: environment
    clusterType: 'scale-unit'
    kubernetesVersion: kubernetesVersion
  })
  sku: {
    name: 'Base'
    tier: environment == 'prod' ? 'Premium' : 'Standard'
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${aksManagedIdentity.id}': {}
    }
  }
  properties: {
    kubernetesVersion: kubernetesVersion
    dnsPrefix: clusterName
    enableRBAC: true
    
    // Node Resource Group (auto-created by AKS)
    nodeResourceGroup: '${resourceGroup().name}-nodes'
    
    // System Node Pool
    agentPoolProfiles: [
      {
        name: systemNodePool.name
        count: systemNodePool.count
        vmSize: systemNodePool.vmSize
        osDiskSizeGB: systemNodePool.osDiskSizeGB
        osDiskType: 'Managed'
        maxPods: systemNodePool.maxPods
        minCount: systemNodePool.minCount
        maxCount: systemNodePool.maxCount
        enableAutoScaling: systemNodePool.enableAutoScaling
        nodeTaints: systemNodePool.taints
        nodeLabels: systemNodePool.labels
        mode: systemNodePool.mode
        type: 'VirtualMachineScaleSets'
        availabilityZones: availabilityZones
        vnetSubnetID: aksSubnetId
        osType: 'Linux'
        osSKU: 'AzureLinux'
        upgradeSettings: {
          maxSurge: '33%'
        }
        securityProfile: {
          sshAccess: 'Disabled'
        }
      }
    ]
    
    // Network Profile
    networkProfile: {
      networkPlugin: networkProfile.networkPlugin
      networkPolicy: networkProfile.networkPolicy
      loadBalancerSku: networkProfile.loadBalancerSku
      outboundType: networkProfile.outboundType
      dnsServiceIP: networkProfile.dnsServiceIP
      serviceCidr: networkProfile.serviceCidr
      dockerBridgeCidr: networkProfile.dockerBridgeCidr
    }
    
    // Private Cluster Configuration
    apiServerAccessProfile: {
      enablePrivateCluster: enablePrivateCluster
      privateDNSZone: enablePrivateCluster ? privateDnsZone : null
      enablePrivateClusterPublicFQDN: false
    }
    
    // Security Profile
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
      azureKeyVaultKms: {
        enabled: true
        keyId: '${keyVaultId}/keys/cluster-secrets'
        keyVaultNetworkAccess: 'Private'
      }
      defender: {
        logAnalyticsWorkspaceResourceId: logAnalyticsWorkspaceId
        securityMonitoring: {
          enabled: true
        }
      }
      imageCleaner: {
        enabled: true
        intervalHours: 24
      }
    }
    
    // Add-ons
    addonProfiles: {
      azureKeyvaultSecretsProvider: {
        enabled: true
        config: {
          enableSecretRotation: 'true'
          rotationPollInterval: '2m'
        }
      }
      azurePolicy: {
        enabled: true
      }
      httpApplicationRouting: {
        enabled: false
      }
      omsAgent: {
        enabled: true
        config: {
          logAnalyticsWorkspaceResourceID: logAnalyticsWorkspaceId
        }
      }
    }
    
    // Maintenance Windows
    maintenanceProfiles: {
      default: {
        maintenanceWindow: {
          schedule: {
            absoluteMonthly: {
              dayOfMonth: 1
              intervalMonths: 1
            }
            durationHours: 4
            startTime: '2024-01-01T02:00:00Z'
            utcOffset: '+00:00'
          }
        }
      }
    }
    
    // Auto-upgrade Profile
    autoUpgradeProfile: {
      upgradeChannel: 'stable'
      nodeOSUpgradeChannel: 'NodeImage'
    }
    
    // Service Mesh (Istio) - Optional, can be enabled later
    serviceMeshProfile: {
      mode: 'Disabled'
    }
    
    // Linux Profile (SSH key required but SSH access disabled)
    linuxProfile: {
      adminUsername: 'azureuser'
      ssh: {
        publicKeys: [
          {
            keyData: sshPublicKey
          }
        ]
      }
    }
    
    // OIDC Issuer Profile (for Workload Identity)
    oidcIssuerProfile: {
      enabled: true
    }
  }
  dependsOn: [
    networkContributorRole
  ]
}

// General Workload Node Pool
resource generalWorkloadNodePoolResource 'Microsoft.ContainerService/managedClusters/agentPools@2024-01-02-preview' = {
  parent: aksCluster
  name: generalWorkloadNodePool.name
  properties: {
    count: generalWorkloadNodePool.count
    vmSize: generalWorkloadNodePool.vmSize
    osDiskSizeGB: generalWorkloadNodePool.osDiskSizeGB
    osDiskType: 'Managed'
    maxPods: generalWorkloadNodePool.maxPods
    minCount: generalWorkloadNodePool.minCount
    maxCount: generalWorkloadNodePool.maxCount
    enableAutoScaling: generalWorkloadNodePool.enableAutoScaling
    nodeTaints: generalWorkloadNodePool.taints
    nodeLabels: generalWorkloadNodePool.labels
    mode: generalWorkloadNodePool.mode
    type: 'VirtualMachineScaleSets'
    availabilityZones: availabilityZones
    vnetSubnetID: aksSubnetId
    osType: 'Linux'
    osSKU: 'AzureLinux'
    upgradeSettings: {
      maxSurge: '33%'
    }
    securityProfile: {
      sshAccess: 'Disabled'
    }
  }
}

// GPU Node Pool (NVIDIA GPU)
resource gpuNodePoolResource 'Microsoft.ContainerService/managedClusters/agentPools@2024-01-02-preview' = {
  parent: aksCluster
  name: gpuNodePool.name
  properties: {
    count: gpuNodePool.count
    vmSize: gpuNodePool.vmSize
    osDiskSizeGB: gpuNodePool.osDiskSizeGB
    osDiskType: 'Managed'
    maxPods: gpuNodePool.maxPods
    minCount: gpuNodePool.minCount
    maxCount: gpuNodePool.maxCount
    enableAutoScaling: gpuNodePool.enableAutoScaling
    nodeTaints: gpuNodePool.taints
    nodeLabels: gpuNodePool.labels
    mode: gpuNodePool.mode
    type: 'VirtualMachineScaleSets'
    availabilityZones: availabilityZones
    vnetSubnetID: aksSubnetId
    osType: 'Linux'
    osSKU: 'Ubuntu'
    upgradeSettings: {
      maxSurge: '33%'
    }
    securityProfile: {
      sshAccess: 'Disabled'
    }
  }
}

// Diagnostic Settings for AKS
resource aksDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${clusterName}-diagnostics'
  scope: aksCluster
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'kube-apiserver'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'kube-audit'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'kube-controller-manager'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'kube-scheduler'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'cluster-autoscaler'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'guard'
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
@description('AKS Cluster ID')
output clusterId string = aksCluster.id

@description('AKS Cluster Name')
output clusterName string = aksCluster.name

@description('AKS FQDN')
output clusterFqdn string = aksCluster.properties.fqdn

@description('AKS Private FQDN')
output privateFqdn string = enablePrivateCluster ? aksCluster.properties.privateFQDN : ''

@description('AKS Managed Identity Principal ID')
output managedIdentityPrincipalId string = aksManagedIdentity.properties.principalId

@description('AKS Managed Identity Client ID')
output managedIdentityClientId string = aksManagedIdentity.properties.clientId

@description('AKS Managed Identity ID')
output managedIdentityId string = aksManagedIdentity.id

@description('Node Resource Group')
output nodeResourceGroup string = aksCluster.properties.nodeResourceGroup

@description('OIDC Issuer URL')
output oidcIssuerUrl string = aksCluster.properties.oidcIssuerProfile.issuerURL

@description('Kubernetes Version')
output kubernetesVersion string = aksCluster.properties.kubernetesVersion
