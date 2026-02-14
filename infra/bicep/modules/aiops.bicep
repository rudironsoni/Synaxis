@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the Log Analytics workspace')
param logAnalyticsWorkspaceName string

@description('Name of the Application Insights resource')
param applicationInsightsName string

@description('Resource ID of the Application Insights')
param applicationInsightsId string

@description('Resource ID of the Log Analytics workspace')
param logAnalyticsWorkspaceId string

@description('Environment name for tagging')
param environmentName string

@description('Enable anomaly detection alerts')
param enableAnomalyDetection bool = true

@description('Enable performance anomaly detection')
param enablePerformanceAnomalyDetection bool = true

@description('Enable error rate anomaly detection')
param enableErrorRateAnomalyDetection bool = true

@description('Enable availability anomaly detection')
param enableAvailabilityAnomalyDetection bool = true

@description('Email addresses for alert notifications')
param alertEmails array = []

@description('Severity for critical alerts')
param criticalAlertSeverity int = 0

@description('Severity for warning alerts')
param warningAlertSeverity int = 2

var alertActionGroupName = '${applicationInsightsName}-aiops-alerts'
var smartDetectorRuleName = '${applicationInsightsName}-smart-detector'
var anomalyDetectionRuleName = '${applicationInsightsName}-anomaly-detection'

resource alertActionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: alertActionGroupName
  location: 'Global'
  properties: {
    groupShortName: 'AIOps'
    enabled: true
    emailReceivers: [for email in alertEmails: {
      name: replace(email, '@', '-')
      emailAddress: email
      useCommonAlertSchema: true
    }]
  }
}

// Smart Detector for Performance Anomalies
resource performanceSmartDetector 'Microsoft.AlertsManagement/smartDetectorAlertRules@2021-04-01' = if (enablePerformanceAnomalyDetection) {
  name: '${smartDetectorRuleName}-performance'
  location: 'Global'
  properties: {
    description: 'Detects performance anomalies in application response time'
    state: 'Enabled'
    severity: warningAlertSeverity
    frequency: 'PT5M'
    scope: [applicationInsightsId]
    detector: {
      id: 'FailureAnomaliesDetector'
      name: 'Failure Anomalies'
      description: 'Detects performance anomalies'
      supportedResourceTypes: [
        'microsoft.insights/components'
      ]
    }
    actionGroups: [alertActionGroup.id]
    throttle: {
      duration: 'PT1H'
    }
  }
}

// Smart Detector for Failure Anomalies
resource failureSmartDetector 'Microsoft.AlertsManagement/smartDetectorAlertRules@2021-04-01' = if (enableErrorRateAnomalyDetection) {
  name: '${smartDetectorRuleName}-failure'
  location: 'Global'
  properties: {
    description: 'Detects failure anomalies in application error rate'
    state: 'Enabled'
    severity: criticalAlertSeverity
    frequency: 'PT5M'
    scope: [applicationInsightsId]
    detector: {
      id: 'FailureAnomaliesDetector'
      name: 'Failure Anomalies'
      description: 'Detects failure anomalies'
      supportedResourceTypes: [
        'microsoft.insights/components'
      ]
    }
    actionGroups: [alertActionGroup.id]
    throttle: {
      duration: 'PT30M'
    }
  }
}

// Metric Alert for Response Time
resource responseTimeAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enablePerformanceAnomalyDetection) {
  name: '${anomalyDetectionRuleName}-response-time'
  location: 'Global'
  properties: {
    description: 'Alert when response time exceeds threshold'
    severity: warningAlertSeverity
    enabled: true
    scopes: [applicationInsightsId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 1.0
          name: 'Metric1'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'dependency duration/average'
          dimensions: []
          operator: 'GreaterThan'
          timeAggregation: 'Average'
          skipMetricValidation: false
          criterionType: 'StaticThresholdCriterion'
        }
      ]
      additionalProperties: {}
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}

// Metric Alert for Error Rate
resource errorRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableErrorRateAnomalyDetection) {
  name: '${anomalyDetectionRuleName}-error-rate'
  location: 'Global'
  properties: {
    description: 'Alert when error rate exceeds threshold'
    severity: criticalAlertSeverity
    enabled: true
    scopes: [applicationInsightsId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 5.0
          name: 'Metric1'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'exceptions/count'
          dimensions: []
          operator: 'GreaterThan'
          timeAggregation: 'Total'
          skipMetricValidation: false
          criterionType: 'StaticThresholdCriterion'
        }
      ]
      additionalProperties: {}
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}

// Metric Alert for Availability
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (enableAvailabilityAnomalyDetection) {
  name: '${anomalyDetectionRuleName}-availability'
  location: 'Global'
  properties: {
    description: 'Alert when availability drops below threshold'
    severity: criticalAlertSeverity
    enabled: true
    scopes: [applicationInsightsId]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      allOf: [
        {
          threshold: 95.0
          name: 'Metric1'
          metricNamespace: 'microsoft.insights/components'
          metricName: 'availabilityResults/availabilityPercentage'
          dimensions: []
          operator: 'LessThan'
          timeAggregation: 'Average'
          skipMetricValidation: false
          criterionType: 'StaticThresholdCriterion'
        }
      ]
      additionalProperties: {}
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
    }
    autoMitigate: true
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}

// Log Analytics Query for Anomaly Detection
resource anomalyDetectionQuery 'Microsoft.OperationalInsights/workspaces/savedSearches@2020-08-01' = if (enableAnomalyDetection) {
  name: 'anomaly-detection-query'
  parent: logAnalyticsWorkspaceId
  properties: {
    category: 'AIOps'
    displayName: 'Anomaly Detection Query'
    query: |
      let TimeRange = 1h;
      let Baseline = 24h;
      let Threshold = 2.0;
      let Requests = requests
        | where timestamp > ago(TimeRange)
        | summarize Count = count(), SuccessRate = 100.0 * countif(success == true) / count() by bin(timestamp, 5m)
        | order by timestamp asc;
      let BaselineRequests = requests
        | where timestamp > ago(Baseline) and timestamp < ago(TimeRange)
        | summarize Count = count(), SuccessRate = 100.0 * countif(success == true) / count() by bin(timestamp, 5m)
        | order by timestamp asc;
      let MeanCount = toscalar(BaselineRequests | summarize Avg(Count));
      let StdDevCount = toscalar(BaselineRequests | summarize stdev(Count));
      let MeanSuccessRate = toscalar(BaselineRequests | summarize Avg(SuccessRate));
      let StdDevSuccessRate = toscalar(BaselineRequests | summarize stdev(SuccessRate));
      Requests
        | extend CountZScore = (Count - MeanCount) / StdDevCount
        | extend SuccessRateZScore = (SuccessRate - MeanSuccessRate) / StdDevSuccessRate
        | where abs(CountZScore) > Threshold or abs(SuccessRateZScore) > Threshold
        | project timestamp, Count, SuccessRate, CountZScore, SuccessRateZScore, AnomalyType = iff(abs(CountZScore) > Threshold, 'RequestCount', iff(abs(SuccessRateZScore) > Threshold, 'SuccessRate', 'None'))
    version: 1
  }
}

// Log Analytics Alert for Anomalies
resource anomalyAlert 'Microsoft.Insights/scheduledQueryRules@2021-08-01' = if (enableAnomalyDetection) {
  name: '${anomalyDetectionRuleName}-log-query'
  location: location
  properties: {
    description: 'Alert when anomalies are detected in application metrics'
    displayName: 'Anomaly Detection Alert'
    severity: warningAlertSeverity
    enabled: true
    evaluationFrequency: 'PT5M'
    timeWindow: 'PT1H'
    autoMitigate: true
    scopes: [logAnalyticsWorkspaceId]
    criteria: {
      allOf: [
        {
          query: |
            let TimeRange = 1h;
            let Baseline = 24h;
            let Threshold = 2.0;
            let Requests = requests
              | where timestamp > ago(TimeRange)
              | summarize Count = count(), SuccessRate = 100.0 * countif(success == true) / count() by bin(timestamp, 5m)
              | order by timestamp asc;
            let BaselineRequests = requests
              | where timestamp > ago(Baseline) and timestamp < ago(TimeRange)
              | summarize Count = count(), SuccessRate = 100.0 * countif(success == true) / count() by bin(timestamp, 5m)
              | order by timestamp asc;
            let MeanCount = toscalar(BaselineRequests | summarize Avg(Count));
            let StdDevCount = toscalar(BaselineRequests | summarize stdev(Count));
            let MeanSuccessRate = toscalar(BaselineRequests | summarize Avg(SuccessRate));
            let StdDevSuccessRate = toscalar(BaselineRequests | summarize stdev(SuccessRate));
            Requests
              | extend CountZScore = (Count - MeanCount) / StdDevCount
              | extend SuccessRateZScore = (SuccessRate - MeanSuccessRate) / StdDevSuccessRate
              | where abs(CountZScore) > Threshold or abs(SuccessRateZScore) > Threshold
              | project timestamp, Count, SuccessRate, CountZScore, SuccessRateZScore, AnomalyType = iff(abs(CountZScore) > Threshold, 'RequestCount', iff(abs(SuccessRateZScore) > Threshold, 'SuccessRate', 'None'))
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          metricMeasureColumn: 'Count'
          resourceColumn: '_ResourceId'
        }
      ]
    }
    actions: [
      {
        actionGroupId: alertActionGroup.id
      }
    ]
  }
}

output alertActionGroupId string = alertActionGroup.id
output performanceSmartDetectorId string = enablePerformanceAnomalyDetection ? performanceSmartDetector.id : ''
output failureSmartDetectorId string = enableErrorRateAnomalyDetection ? failureSmartDetector.id : ''
output responseTimeAlertId string = enablePerformanceAnomalyDetection ? responseTimeAlert.id : ''
output errorRateAlertId string = enableErrorRateAnomalyDetection ? errorRateAlert.id : ''
output availabilityAlertId string = enableAvailabilityAnomalyDetection ? availabilityAlert.id : ''
output anomalyAlertId string = enableAnomalyDetection ? anomalyAlert.id : ''
