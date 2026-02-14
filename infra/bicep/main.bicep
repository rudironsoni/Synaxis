targetScope = 'subscription'

// =============================================================================
// Synaxis Mission-Critical Infrastructure
// Main Bicep Deployment - Network Foundation
// =============================================================================
// This is the entry point for deploying Synaxis infrastructure following
// Azure Mission-Critical patterns (Heyko Oelrichs, Hansjoerg Scherer)
// =============================================================================

@description('Environment name (dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string = 'dev'

@description('Azure region for deployment')
@allowed(['eastus', 'westeurope', 'southeastasia'])
param location string = 'eastus'

@description('Enable DDoS Protection Standard')
param enableDdosProtection bool = true

@description('Tags to apply to all resources')
param tags object = {
  project: 'synaxis'
  managedBy: 'bicep'
  missionCritical: 'true'
}

@description('SSH public key for AKS nodes')
@secure()
param sshPublicKey string = 'ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQCplaceholder synaxis-aks-key'

// =============================================================================
// Resource Group for Network Resources
// =============================================================================

resource networkResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'synaxis-network-${environment}-${location}'
  location: location
  tags: union(tags, {
    layer: 'network'
    environment: environment
  })
}

// =============================================================================
// Deploy DDoS Protection Plan Module
// =============================================================================

module ddosProtection './modules/ddos-protection.bicep' = {
  name: 'ddos-protection-deployment'
  scope: networkResourceGroup
  params: {
    environment: environment
    location: location
    enableDdosProtection: enableDdosProtection
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    tags: tags
  }
}

// =============================================================================
// Deploy Virtual Network Module
// =============================================================================

module vnet './modules/vnet.bicep' = {
  name: 'vnet-deployment'
  scope: networkResourceGroup
  params: {
    environment: environment
    location: location
    tags: tags
    enableDdosProtection: enableDdosProtection
    ddosProtectionPlanId: ddosProtection.outputs.ddosProtectionPlanId
  }
  dependsOn: [
    ddosProtection
  ]
}

// =============================================================================
// Deploy Network Security Groups Module
// =============================================================================

module nsg './modules/nsg.bicep' = {
  name: 'nsg-deployment'
  scope: networkResourceGroup
  params: {
    environment: environment
    location: location
    tags: tags
  }
}

// =============================================================================
// Deploy Route Table Module
// =============================================================================

module routeTable './modules/route-table.bicep' = {
  name: 'route-table-deployment'
  scope: networkResourceGroup
  params: {
    environment: environment
    location: location
    tags: tags
  }
}

// =============================================================================
// Associate NSGs with Subnets (requires VNet and NSG to be deployed first)
// =============================================================================

resource aksSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-05-01' = {
  name: '${vnet.outputs.vnetName}/aks-subnet'
  properties: {
    addressPrefix: '10.0.0.0/20'
    networkSecurityGroup: {
      id: nsg.outputs.aksNsgId
    }
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
      {
        service: 'Microsoft.Storage'
      }
    ]
    delegations: [
      {
        name: 'aks-delegation'
        properties: {
          serviceName: 'Microsoft.ContainerService/managedClusters'
        }
      }
    ]
  }
  dependsOn: [
    vnet
    nsg
  ]
}

resource peSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-05-01' = {
  name: '${vnet.outputs.vnetName}/private-endpoints'
  properties: {
    addressPrefix: '10.0.16.0/24'
    networkSecurityGroup: {
      id: nsg.outputs.peNsgId
    }
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
  dependsOn: [
    vnet
    nsg
  ]
}

// =============================================================================
// Associate Route Table with AKS Subnet
// =============================================================================

resource aksSubnetWithRouteTable 'Microsoft.Network/virtualNetworks/subnets@2023-05-01' = {
  name: '${vnet.outputs.vnetName}/aks-subnet'
  properties: {
    addressPrefix: '10.0.0.0/20'
    routeTable: {
      id: routeTable.outputs.routeTableId
    }
  }
  dependsOn: [
    aksSubnet
    routeTable
  ]
}

// =============================================================================
// Resource Group for Security Resources
// =============================================================================

resource securityResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'synaxis-security-${environment}-${location}'
  location: location
  tags: union(tags, {
    layer: 'security'
    environment: environment
  })
}

// =============================================================================
// Private DNS Zone for Key Vault
// =============================================================================

resource keyVaultPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.vaultcore.azure.net'
  location: 'global'
  tags: tags
}

resource keyVaultPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: keyVaultPrivateDnsZone
  name: '${vnet.outputs.vnetName}-link'
  location: 'global'
  tags: tags
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

// =============================================================================
// Deploy Key Vault Module
// =============================================================================

module keyVault './modules/keyvault.bicep' = {
  name: 'keyvault-deployment'
  scope: securityResourceGroup
  params: {
    keyVaultName: 'synaxis-kv-${environment}-${location}'
    location: location
    environment: environment
    tags: tags
    enableHsm: environment == 'prod'
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
  }
}

// =============================================================================
// Deploy Private Endpoint for Key Vault
// =============================================================================

module keyVaultPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'keyvault-pe-deployment'
  scope: securityResourceGroup
  params: {
    privateEndpointName: 'synaxis-kv-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: keyVault.outputs.keyVaultId
    groupIds: ['vault']
    privateDnsZoneId: keyVaultPrivateDnsZone.id
  }
}

// =============================================================================
// Create Data Resource Group
// =============================================================================

resource dataResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'synaxis-data-${environment}-${location}'
  location: location
  tags: tags
}

// =============================================================================
// Create Private DNS Zone for PostgreSQL
// =============================================================================

resource postgresPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource postgresPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: postgresPrivateDnsZone
  name: '${postgresPrivateDnsZone.name}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

// =============================================================================
// Create Private DNS Zone for Redis
// =============================================================================

resource redisPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.redis.cache.windows.net'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource redisPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: redisPrivateDnsZone
  name: '${redisPrivateDnsZone.name}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

// =============================================================================
// Deploy Firewall Policy
// =============================================================================

module firewallPolicy './modules/firewall-policy.bicep' = {
  name: 'firewall-policy-deployment'
  scope: networkResourceGroup
  params: {
    environment: environment
    location: location
    aksSubnetCidr: '10.0.0.0/20'
    privateEndpointSubnetCidr: '10.0.16.0/24'
    vnetCidr: '10.0.0.0/16'
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    tags: tags
  }
}

// =============================================================================
// Deploy Azure Firewall
// =============================================================================

module firewall './modules/firewall.bicep' = {
  name: 'firewall-deployment'
  scope: networkResourceGroup
  params: {
    firewallName: 'synaxis-firewall-${environment}-${location}'
    location: location
    tags: tags
    vnetId: vnet.outputs.vnetId
    firewallSubnetId: vnet.outputs.firewallSubnetId
    firewallPolicyId: firewallPolicy.outputs.firewallPolicyId
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
  }
  dependsOn: [
    firewallPolicy
  ]
}

// =============================================================================
// Update Route Table to Use Firewall
// =============================================================================

module routeTableWithFirewall './modules/route-table.bicep' = {
  name: 'route-table-firewall-update'
  scope: networkResourceGroup
  params: {
    routeTableName: 'synaxis-aks-rt-${environment}'
    location: location
    tags: tags
    firewallPrivateIp: firewall.outputs.firewallPrivateIp
  }
  dependsOn: [
    firewall
  ]
}

// =============================================================================
// Re-associate Route Table with Firewall Routes
// =============================================================================

resource aksSubnetWithFirewallRouteTable 'Microsoft.Network/virtualNetworks/subnets@2023-05-01' = {
  name: '${vnet.outputs.vnetName}/aks-subnet'
  properties: {
    addressPrefix: '10.0.0.0/20'
    routeTable: {
      id: routeTableWithFirewall.outputs.routeTableId
    }
    delegations: [
      {
        name: 'aks-delegation'
        properties: {
          serviceName: 'Microsoft.ContainerService/managedClusters'
        }
      }
    ]
    serviceEndpoints: [
      {
        service: 'Microsoft.Sql'
      }
      {
        service: 'Microsoft.Storage'
      }
    ]
  }
  dependsOn: [
    aksSubnet
    routeTableWithFirewall
  ]
}

// =============================================================================
// Deploy PostgreSQL with Private Endpoint
// =============================================================================

module postgresql './modules/postgresql.bicep' = {
  name: 'postgresql-deployment'
  scope: dataResourceGroup
  params: {
    serverName: 'synaxis-pg-${environment}-${location}'
    location: location
    tags: tags
    administratorLogin: 'synaxisadmin'
    administratorLoginPassword: keyVault.getSecret('postgres-admin-password')
    skuTier: 'GeneralPurpose'
    skuName: 'Standard_D4s_v3'
    storageSizeGB: 256
    storageAutoGrow: 'Enabled'
    backupRetentionDays: 35
    geoRedundantBackup: 'Disabled'
    highAvailabilityMode: 'ZoneRedundant'
    availabilityZone: '1'
    standbyAvailabilityZone: '2'
    subnetId: peSubnet.id
    privateDnsZoneId: postgresPrivateDnsZone.id
  }
  dependsOn: [
    dataResourceGroup
    keyVault
  ]
}

// =============================================================================
// Deploy Private Endpoint for PostgreSQL
// =============================================================================

module postgresqlPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'postgresql-pe-deployment'
  scope: dataResourceGroup
  params: {
    privateEndpointName: 'synaxis-pg-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: postgresql.outputs.serverId
    groupIds: ['postgresqlServer']
    privateDnsZoneId: postgresPrivateDnsZone.id
  }
  dependsOn: [
    postgresql
  ]
}

// =============================================================================
// Deploy Redis Cache with Private Endpoint
// =============================================================================

module redis './modules/redis.bicep' = {
  name: 'redis-deployment'
  scope: dataResourceGroup
  params: {
    cacheName: 'synaxis-redis-${environment}-${location}'
    location: location
    tags: tags
    sku: 'Premium'
    capacity: 1
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
    subnetId: peSubnet.id
    privateDnsZoneId: redisPrivateDnsZone.id
  }
  dependsOn: [
    dataResourceGroup
  ]
}

// =============================================================================
// Deploy Private Endpoint for Redis
// =============================================================================

module redisPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'redis-pe-deployment'
  scope: dataResourceGroup
  params: {
    privateEndpointName: 'synaxis-redis-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: redis.outputs.cacheId
    groupIds: ['redisCache']
    privateDnsZoneId: redisPrivateDnsZone.id
  }
  dependsOn: [
    redis
  ]
}

// =============================================================================
// Create Private DNS Zones for Cosmos DB and Service Bus
// =============================================================================

resource cosmosPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.documents.azure.com'
  location: 'global'
  tags: tags
}

resource cosmosPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: cosmosPrivateDnsZone
  name: '${cosmosPrivateDnsZone.name}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

resource serviceBusPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.servicebus.windows.net'
  location: 'global'
  tags: tags
}

resource serviceBusPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: serviceBusPrivateDnsZone
  name: '${serviceBusPrivateDnsZone.name}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

// =============================================================================
// Deploy Cosmos DB (Multi-Region)
// =============================================================================

module cosmos './modules/cosmos.bicep' = {
  name: 'cosmos-deployment'
  scope: dataResourceGroup
  params: {
    accountName: 'synaxis-cosmos-${environment}-${location}'
    location: location
    environment: environment
    enableMultiRegionWrites: environment == 'prod'
    replicaRegions: environment == 'prod' ? [location, 'westeurope', 'eastus', 'southeastasia'] : [location]
    enablePrivateEndpoint: true
    subnetId: peSubnet.id
    privateDnsZoneId: cosmosPrivateDnsZone.id
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    tags: tags
  }
  dependsOn: [
    dataResourceGroup
    logAnalyticsWorkspace
  ]
}

// =============================================================================
// Deploy Private Endpoint for Cosmos DB
// =============================================================================

module cosmosPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'cosmos-pe-deployment'
  scope: dataResourceGroup
  params: {
    privateEndpointName: 'synaxis-cosmos-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: cosmos.outputs.accountId
    groupIds: ['Sql']
    privateDnsZoneId: cosmosPrivateDnsZone.id
  }
  dependsOn: [
    cosmos
  ]
}

// =============================================================================
// Deploy Redis Enterprise Cluster
// =============================================================================

module redisEnterprise './modules/redis-enterprise.bicep' = {
  name: 'redis-enterprise-deployment'
  scope: dataResourceGroup
  params: {
    clusterName: 'synaxis-redis-ent-${environment}-${location}'
    location: location
    environment: environment
    sku: environment == 'prod' ? 'Enterprise_E10' : 'Enterprise_E10'
    shardCount: 6
    zoneRedundant: true
    persistenceMode: 'AOF'
    enablePrivateEndpoint: true
    subnetId: peSubnet.id
    privateDnsZoneId: redisPrivateDnsZone.id
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    keyVaultId: keyVault.outputs.keyVaultId
    tags: tags
  }
  dependsOn: [
    dataResourceGroup
    keyVault
    logAnalyticsWorkspace
  ]
}

// =============================================================================
// Deploy Private Endpoint for Redis Enterprise
// =============================================================================

module redisEnterprisePrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'redis-enterprise-pe-deployment'
  scope: dataResourceGroup
  params: {
    privateEndpointName: 'synaxis-redis-ent-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: redisEnterprise.outputs.clusterId
    groupIds: ['redisCache']
    privateDnsZoneId: redisPrivateDnsZone.id
  }
  dependsOn: [
    redisEnterprise
  ]
}

// =============================================================================
// Deploy Service Bus Premium
// =============================================================================

module serviceBus './modules/servicebus.bicep' = {
  name: 'servicebus-deployment'
  scope: dataResourceGroup
  params: {
    namespaceName: 'synaxis-sb-${environment}-${location}'
    location: location
    environment: environment
    messagingUnits: environment == 'prod' ? 4 : 1
    zoneRedundant: true
    enableGeoDR: environment == 'prod'
    geoDRPairedLocation: 'eastus'
    enablePrivateEndpoint: true
    subnetId: peSubnet.id
    privateDnsZoneId: serviceBusPrivateDnsZone.id
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    tags: tags
  }
  dependsOn: [
    dataResourceGroup
    logAnalyticsWorkspace
  ]
}

// =============================================================================
// Deploy Private Endpoint for Service Bus
// =============================================================================

module serviceBusPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'servicebus-pe-deployment'
  scope: dataResourceGroup
  params: {
    privateEndpointName: 'synaxis-sb-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: serviceBus.outputs.namespaceId
    groupIds: ['namespace']
    privateDnsZoneId: serviceBusPrivateDnsZone.id
  }
  dependsOn: [
    serviceBus
  ]
}

// =============================================================================
// Create Compute Resource Group (for AKS)
// =============================================================================

resource computeResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'synaxis-compute-${environment}-${location}'
  location: location
  tags: union(tags, {
    layer: 'compute'
    environment: environment
  })
}

// =============================================================================
// Create Private DNS Zone for Azure Container Registry
// =============================================================================

resource acrPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: 'privatelink.azurecr.io'
  location: 'global'
  tags: tags
}

// Link Private DNS Zone to VNet
resource acrPrivateDnsZoneLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: acrPrivateDnsZone
  name: '${acrPrivateDnsZone.name}-link'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.outputs.vnetId
    }
  }
}

// =============================================================================
// Create Log Analytics Workspace for Container Insights
// =============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'synaxis-law-${environment}-${location}'
  location: location
  tags: union(tags, {
    purpose: 'container-insights'
    environment: environment
  })
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}

// =============================================================================
// Deploy Azure Container Registry
// =============================================================================

module acr './modules/acr.bicep' = {
  name: 'acr-deployment'
  scope: computeResourceGroup
  params: {
    registryName: 'synaxisacr${environment}${location}'
    location: location
    environment: environment
    tags: tags
    enablePrivateEndpoint: true
    subnetId: peSubnet.id
    privateDnsZoneId: acrPrivateDnsZone.id
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
  }
}

// =============================================================================
// Deploy Private Endpoint for Azure Container Registry
// =============================================================================

module acrPrivateEndpoint './modules/private-endpoint.bicep' = {
  name: 'acr-pe-deployment'
  scope: computeResourceGroup
  params: {
    privateEndpointName: 'synaxis-acr-pe-${environment}'
    location: location
    tags: tags
    subnetId: peSubnet.id
    privateLinkServiceId: acr.outputs.registryId
    groupIds: ['registry']
    privateDnsZoneId: acrPrivateDnsZone.id
  }
  dependsOn: [
    acr
  ]
}

// =============================================================================
// Deploy Azure Kubernetes Service (AKS)
// =============================================================================

module aks './modules/aks.bicep' = {
  name: 'aks-deployment'
  scope: computeResourceGroup
  params: {
    clusterName: 'synaxis-aks-${environment}-${location}'
    location: location
    environment: environment
    kubernetesVersion: '1.29'
    vnetId: vnet.outputs.vnetId
    aksSubnetId: aksSubnet.id
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    keyVaultId: keyVault.outputs.keyVaultId
    acrId: acr.outputs.registryId
    sshPublicKey: sshPublicKey
  }
  dependsOn: [
    computeResourceGroup
    aksSubnetWithFirewallRouteTable
  ]
}

// =============================================================================
// Deploy Azure Policy Assignments (Governance)
// =============================================================================

module policyAssignments './modules/policy-assignment.bicep' = {
  name: 'policy-assignment-deployment'
  params: {
    location: location
    environment: environment
    tags: tags
    enforcePolicies: true
    enableRemediation: environment == 'prod'
  }
  dependsOn: [
    networkResourceGroup
    dataResourceGroup
    securityResourceGroup
    computeResourceGroup
  ]
}

// =============================================================================
// Deploy WAF Policy Module
// =============================================================================

module wafPolicy './modules/waf-policy.bicep' = {
  name: 'waf-policy-deployment'
  params: {
    environment: environment
    location: location
    frontDoorSku: environment == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor'
    wafMode: 'Prevention'
    enableOwaspRules: true
    enableBotProtection: true
    enableRateLimiting: true
    rateLimitThreshold: 1000
    enableGeoFiltering: false
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
    tags: tags
  }
}

// =============================================================================
// Deploy Azure Front Door
// =============================================================================

module frontDoor './modules/frontdoor.bicep' = {
  name: 'frontdoor-deployment'
  params: {
    environment: environment
    tags: tags
    frontDoorSku: environment == 'prod' ? 'Premium_AzureFrontDoor' : 'Standard_AzureFrontDoor'
    stampOrigins: environment == 'prod' ? [
      {
        name: 'stamp-weu-01'
        hostName: 'synaxis-ingress-weu-01.${environment}.synaxis.io'
        priority: 1
        weight: 1000
      }
      {
        name: 'stamp-eus-01'
        hostName: 'synaxis-ingress-eus-01.${environment}.synaxis.io'
        priority: 1
        weight: 1000
      }
      {
        name: 'stamp-sea-01'
        hostName: 'synaxis-ingress-sea-01.${environment}.synaxis.io'
        priority: 1
        weight: 1000
      }
    ] : [
      {
        name: 'stamp-dev-01'
        hostName: aks.outputs.ingressIp
        priority: 1
        weight: 1000
      }
    ]
    enableWaf: true
    wafMode: 'Prevention'
    wafPolicyId: wafPolicy.outputs.wafPolicyId
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.id
  }
  dependsOn: [
    aks
    logAnalyticsWorkspace
    wafPolicy
  ]
}

// =============================================================================
// Outputs
// =============================================================================

@description('Virtual Network ID')
output vnetId string = vnet.outputs.vnetId

@description('Virtual Network Name')
output vnetName string = vnet.outputs.vnetName

@description('AKS Subnet ID')
output aksSubnetId string = aksSubnet.id

@description('Private Endpoint Subnet ID')
output privateEndpointSubnetId string = peSubnet.id

@description('Bastion Subnet ID')
output bastionSubnetId string = vnet.outputs.bastionSubnetId

@description('Firewall Subnet ID')
output firewallSubnetId string = vnet.outputs.firewallSubnetId

@description('AKS NSG ID')
output aksNsgId string = nsg.outputs.aksNsgId

@description('Private Endpoint NSG ID')
output peNsgId string = nsg.outputs.peNsgId

@description('Route Table ID')
output routeTableId string = routeTable.outputs.routeTableId

@description('Resource Group Name')
output resourceGroupName string = networkResourceGroup.name

@description('Resource Group Name')
output resourceGroupName string = networkResourceGroup.name

@description('Security Resource Group Name')
output securityResourceGroupName string = securityResourceGroup.name

@description('Key Vault ID')
output keyVaultId string = keyVault.outputs.keyVaultId

@description('Key Vault Name')
output keyVaultName string = keyVault.outputs.keyVaultName

@description('Key Vault URI')
output keyVaultUri string = keyVault.outputs.keyVaultUri

@description('Tenant Data Encryption Key ID')
output tenantDataKeyId string = keyVault.outputs.tenantDataKeyId

@description('API Key Encryption Key ID')
output apiKeyEncryptionKeyId string = keyVault.outputs.apiKeyEncryptionKeyId

@description('Firewall ID')
output firewallId string = firewall.outputs.firewallId

@description('Firewall Name')
output firewallName string = firewall.outputs.firewallName

@description('Firewall Private IP')
output firewallPrivateIp string = firewall.outputs.firewallPrivateIp

@description('Firewall Public IP')
output firewallPublicIp string = firewall.outputs.firewallPublicIp

@description('Firewall Policy ID')
output firewallPolicyId string = firewallPolicy.outputs.firewallPolicyId

@description('Firewall Policy Name')
output firewallPolicyName string = firewallPolicy.outputs.firewallPolicyName

@description('Compute Resource Group Name')
output computeResourceGroupName string = computeResourceGroup.name

@description('ACR ID')
output acrId string = acr.outputs.registryId

@description('ACR Name')
output acrName string = acr.outputs.registryName

@description('ACR Login Server')
output acrLoginServer string = acr.outputs.loginServer

@description('Log Analytics Workspace ID')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id

@description('AKS Cluster ID')
output aksClusterId string = aks.outputs.clusterId

@description('AKS Cluster Name')
output aksClusterName string = aks.outputs.clusterName

@description('AKS Managed Identity Principal ID')
output aksManagedIdentityPrincipalId string = aks.outputs.managedIdentityPrincipalId

@description('AKS Managed Identity Client ID')
output aksManagedIdentityClientId string = aks.outputs.managedIdentityClientId

@description('AKS OIDC Issuer URL')
output aksOidcIssuerUrl string = aks.outputs.oidcIssuerUrl

@description('Cosmos DB Account ID')
output cosmosAccountId string = cosmos.outputs.accountId

@description('Cosmos DB Account Name')
output cosmosAccountName string = cosmos.outputs.accountName

@description('Cosmos DB Endpoint')
output cosmosEndpoint string = cosmos.outputs.endpoint

@description('Redis Enterprise Cluster ID')
output redisEnterpriseId string = redisEnterprise.outputs.clusterId

@description('Redis Enterprise Cluster Name')
output redisEnterpriseName string = redisEnterprise.outputs.clusterName

@description('Redis Enterprise Hostname')
output redisEnterpriseHostname string = redisEnterprise.outputs.hostname

@description('Service Bus Namespace ID')
output serviceBusNamespaceId string = serviceBus.outputs.namespaceId

@description('Service Bus Namespace Name')
output serviceBusNamespaceName string = serviceBus.outputs.namespaceName

@description('Service Bus Endpoint')
output serviceBusEndpoint string = serviceBus.outputs.endpoint

@description('Front Door ID')
output frontDoorId string = frontDoor.outputs.frontDoorId

@description('Front Door Name')
output frontDoorName string = frontDoor.outputs.frontDoorName

@description('Front Door Endpoint Hostname')
output frontDoorEndpoint string = frontDoor.outputs.frontDoorEndpoint

@description('Front Door FQDN')
output frontDoorFqdn string = frontDoor.outputs.frontDoorFqdn

@description('WAF Policy ID')
output wafPolicyId string = frontDoor.outputs.wafPolicyId

@description('DDoS Protection Plan ID')
output ddosProtectionPlanId string = ddosProtection.outputs.ddosProtectionPlanId

@description('DDoS Protection Plan Name')
output ddosProtectionPlanName string = ddosProtection.outputs.ddosProtectionPlanName

@description('DDoS Protection Policy ID')
output ddosProtectionPolicyId string = ddosProtection.outputs.ddosProtectionPolicyId

@description('DDoS Protection Enabled')
output ddosProtectionEnabled bool = ddosProtection.outputs.ddosProtectionEnabled

@description('WAF Policy Mode')
output wafPolicyMode string = wafPolicy.outputs.wafMode

@description('OWASP Rules Enabled')
output owaspRulesEnabled bool = wafPolicy.outputs.owaspRulesEnabled

@description('Bot Protection Enabled')
output botProtectionEnabled bool = wafPolicy.outputs.botProtectionEnabled

@description('Rate Limiting Enabled')
output rateLimitingEnabled bool = wafPolicy.outputs.rateLimitingEnabled

@description('Geo-filtering Enabled')
output geoFilteringEnabled bool = wafPolicy.outputs.geoFilteringEnabled

@description('Deployment Summary')
output deploymentSummary object = {
  environment: environment
  location: location
  vnetName: vnet.outputs.vnetName
  vnetAddressSpace: '10.0.0.0/16'
  subnets: {
    aks: '10.0.0.0/20'
    privateEndpoints: '10.0.16.0/24'
    bastion: '10.0.17.0/24'
    firewall: '10.0.18.0/24'
  }
  security: {
    ddosProtection: enableDdosProtection
    ddosProtectionPlanId: ddosProtection.outputs.ddosProtectionPlanId
    ddosProtectionEnabled: ddosProtection.outputs.ddosProtectionEnabled
    wafEnabled: true
    wafPolicyId: wafPolicy.outputs.wafPolicyId
    wafMode: wafPolicy.outputs.wafMode
    owaspRulesEnabled: wafPolicy.outputs.owaspRulesEnabled
    botProtectionEnabled: wafPolicy.outputs.botProtectionEnabled
    rateLimitingEnabled: wafPolicy.outputs.rateLimitingEnabled
    geoFilteringEnabled: wafPolicy.outputs.geoFilteringEnabled
    nsgAttached: true
    routeTableAttached: true
    keyVaultDeployed: true
    hsmEnabled: environment == 'prod'
    privateEndpointsEnabled: true
  }
}
