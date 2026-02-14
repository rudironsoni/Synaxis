// Azure Service Bus Premium Module
// Epic E2-T5: Messaging and Cross-Stamp Communication

@description('Service Bus namespace name (globally unique)')
param namespaceName string

@description('Azure region')
param location string = resourceGroup().location

@description('Environment')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Messaging units (Premium tier only)')
@minValue(1)
@maxValue(16)
param messagingUnits int = 1

@description('Enable zone redundancy')
param zoneRedundant bool = true

@description('Enable geo-disaster recovery')
param enableGeoDR bool = environment == 'prod'

@description('Paired namespace location for geo-DR')
param geoDRPairedLocation string = 'eastus'

@description('Enable private endpoint')
param enablePrivateEndpoint bool = true

@description('Subnet ID for private endpoint')
param subnetId string = ''

@description('Private DNS Zone ID for Service Bus')
param privateDnsZoneId string = ''

@description('Log Analytics workspace ID for diagnostics')
param logAnalyticsWorkspaceId string = ''

@description('Tags')
param tags object = {}

// Topic definitions
var topics = [
  {
    name: 'inference-requests'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 5120
    supportOrdering: true
    enablePartitioning: true
    enableExpress: false
  }
  {
    name: 'batch-processing'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT5M'
    defaultMessageTimeToLive: 'P7D'
    maxSizeInMegabytes: 5120
    supportOrdering: false
    enablePartitioning: true
    enableExpress: false
  }
  {
    name: 'audit-events'
    requiresDuplicateDetection: false
    defaultMessageTimeToLive: 'P31D'
    maxSizeInMegabytes: 10240
    supportOrdering: true
    enablePartitioning: true
    enableExpress: false
  }
]

// Queue definitions
var queues = [
  {
    name: 'webhooks-delivery'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    defaultMessageTimeToLive: 'P14D'
    maxSizeInMegabytes: 5120
    lockDuration: 'PT5M'
    maxDeliveryCount: 10
    enablePartitioning: true
    enableDeadLetteringOnMessageExpiration: true
    deadLetteringOnFilterEvaluationExceptions: true
  }
  {
    name: 'email-notifications'
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT5M'
    defaultMessageTimeToLive: 'P3D'
    maxSizeInMegabytes: 2048
    lockDuration: 'PT5M'
    maxDeliveryCount: 5
    enablePartitioning: true
    enableDeadLetteringOnMessageExpiration: true
    deadLetteringOnFilterEvaluationExceptions: true
  }
]

// Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: union(tags, {
    environment: environment
    purpose: 'synaxis-messaging'
  })
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: messagingUnits
  }
  properties: {
    zoneRedundant: zoneRedundant
    disableLocalAuth: true  // Enforce Azure AD only
    encryption: {
      keySource: 'Microsoft.ServiceBus'
    }
  }
}

// Geo-DR paired namespace (for prod)
resource geoDRNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = if (enableGeoDR) {
  name: '${namespaceName}-dr'
  location: geoDRPairedLocation
  tags: union(tags, {
    environment: environment
    purpose: 'synaxis-messaging-dr'
  })
  sku: {
    name: 'Premium'
    tier: 'Premium'
    capacity: messagingUnits
  }
  properties: {
    zoneRedundant: zoneRedundant
    disableLocalAuth: true
  }
}

// Geo-DR pairing
resource geoDRPairing 'Microsoft.ServiceBus/namespaces/disasterRecoveryConfigs@2022-10-01-preview' = if (enableGeoDR) {
  parent: serviceBusNamespace
  name: '${namespaceName}-geo-dr'
  properties: {
    partnerNamespace: geoDRNamespace.id
  }
}

// Create Topics
resource serviceBusTopic 'Microsoft.ServiceBus/namespaces/topics@2022-10-01-preview' = [for topic in topics: {
  parent: serviceBusNamespace
  name: topic.name
  properties: {
    requiresDuplicateDetection: topic.requiresDuplicateDetection
    duplicateDetectionHistoryTimeWindow: topic.duplicateDetectionHistoryTimeWindow
    defaultMessageTimeToLive: topic.defaultMessageTimeToLive
    maxSizeInMegabytes: topic.maxSizeInMegabytes
    supportOrdering: topic.supportOrdering
    enablePartitioning: topic.enablePartitioning
    enableExpress: topic.enableExpress
  }
}]

// Create Queues
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = [for queue in queues: {
  parent: serviceBusNamespace
  name: queue.name
  properties: {
    requiresDuplicateDetection: queue.requiresDuplicateDetection
    duplicateDetectionHistoryTimeWindow: queue.duplicateDetectionHistoryTimeWindow
    defaultMessageTimeToLive: queue.defaultMessageTimeToLive
    maxSizeInMegabytes: queue.maxSizeInMegabytes
    lockDuration: queue.lockDuration
    maxDeliveryCount: queue.maxDeliveryCount
    enablePartitioning: queue.enablePartitioning
    enableDeadLetteringOnMessageExpiration: queue.enableDeadLetteringOnMessageExpiration
    deadLetteringOnFilterEvaluationExceptions: queue.deadLetteringOnFilterEvaluationExceptions
  }
}]

// Topic Subscriptions
resource inferenceSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2022-10-01-preview' = {
  parent: serviceBusTopic[0]  // inference-requests topic
  name: 'inference-workers'
  properties: {
    lockDuration: 'PT5M'
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnFilterEvaluationExceptions: true
    maxDeliveryCount: 10
    enableBatchedOperations: true
  }
}

// Private Endpoint
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId)) {
  name: '${namespaceName}-pe'
  location: location
  tags: tags
  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${namespaceName}-plsc'
        properties: {
          privateLinkServiceId: serviceBusNamespace.id
          groupIds: [
            'namespace'
          ]
        }
      }
    ]
  }
}

// Private DNS Zone Group
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = if (enablePrivateEndpoint && !empty(subnetId) && !empty(privateDnsZoneId)) {
  parent: privateEndpoint
  name: 'servicebus-private-dns-zone-group'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'servicebus-private-dns-zone-config'
        properties: {
          privateDnsZoneId: privateDnsZoneId
        }
      }
    ]
  }
}

// Diagnostic Settings
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${namespaceName}-diagnostics'
  scope: serviceBusNamespace
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        category: 'OperationalLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 30
        }
      }
      {
        category: 'VNetAndIPFilteringLogs'
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
@description('Service Bus Namespace ID')
output namespaceId string = serviceBusNamespace.id

@description('Service Bus Namespace Name')
output namespaceName string = serviceBusNamespace.name

@description('Service Bus Endpoint')
output endpoint string = '${serviceBusNamespace.name}.servicebus.windows.net'

@description('Topic Names')
output topicNames array = [for topic in topics: topic.name]

@description('Queue Names')
output queueNames array = [for queue in queues: queue.name]

@description('Geo-DR Namespace ID (if enabled)')
output geoDRNamespaceId string = enableGeoDR ? geoDRNamespace.id : ''

@description('Primary Connection String (for reference only, use Managed Identity in production)')
#disable-next-line outputs-should-not-contain-secrets
output primaryConnectionString string = listKeys('${serviceBusNamespace.id}/authorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
