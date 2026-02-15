# Synaxis Deployment Guide

This guide covers deploying Synaxis to various platforms, including Azure, AWS, GCP, Kubernetes, and on-premises environments.

## Table of Contents

- [Deployment Options](#deployment-options)
- [Prerequisites](#prerequisites)
- [Azure Deployment](#azure-deployment)
- [AWS Deployment](#aws-deployment)
- [GCP Deployment](#gcp-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Docker Deployment](#docker-deployment)
- [Terraform Deployment](#terraform-deployment)
- [Bicep Deployment](#bicep-deployment)
- [Environment Configuration](#environment-configuration)
- [Monitoring and Logging](#monitoring-and-logging)
- [Troubleshooting](#troubleshooting)

## Deployment Options

Synaxis can be deployed in several ways:

| Option | Best For | Complexity | Cost |
|--------|----------|------------|------|
| **Docker** | Development, testing | Low | Low |
| **Azure Container Apps** | Production on Azure | Medium | Medium |
| **AWS ECS/Fargate** | Production on AWS | Medium | Medium |
| **GCP Cloud Run** | Production on GCP | Medium | Medium |
| **Kubernetes** | Enterprise, multi-cloud | High | High |
| **Bare Metal** | On-premises, high control | High | Variable |

## Prerequisites

### General Prerequisites

- **Docker** 20.10+ (for container-based deployments)
- **kubectl** (for Kubernetes deployments)
- **Terraform** 1.0+ (for IaC deployments)
- **Azure CLI** (for Azure deployments)
- **AWS CLI** (for AWS deployments)
- **gcloud CLI** (for GCP deployments)

### API Keys

You'll need API keys for at least one AI provider:

- **OpenAI**: [Get API Key](https://platform.openai.com/api-keys)
- **Azure OpenAI**: [Create Resource](https://azure.microsoft.com/products/cognitive-services/openai-service)
- **Anthropic**: [Get API Key](https://console.anthropic.com/)
- **Google**: [Get API Key](https://aistudio.google.com/app/apikey)

## Azure Deployment

### Option 1: Azure Container Apps

#### Prerequisites
- Azure CLI installed
- Azure subscription

#### Deployment Steps

1. **Create a resource group**:

```bash
az group create \
  --name synaxis-rg \
  --location eastus
```

2. **Create a Container Apps environment**:

```bash
az containerapp env create \
  --name synaxis-env \
  --resource-group synaxis-rg \
  --location eastus
```

3. **Create the Container App**:

```bash
az containerapp create \
  --name synaxis-gateway \
  --resource-group synaxis-rg \
  --environment synaxis-env \
  --image synaxis/gateway:latest \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    OPENAI_API_KEY=secretref:openai-key \
    AZURE_OPENAI_ENDPOINT=secretref:azure-endpoint \
  --secrets \
    openai-key=sk-your-openai-key \
    azure-endpoint=https://your-resource.openai.azure.com
```

4. **Get the URL**:

```bash
az containerapp show \
  --name synaxis-gateway \
  --resource-group synaxis-rg \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

### Option 2: Azure App Service

#### Deployment Steps

1. **Create an App Service plan**:

```bash
az appservice plan create \
  --name synaxis-plan \
  --resource-group synaxis-rg \
  --sku B1 \
  --is-linux
```

2. **Create a web app**:

```bash
az webapp create \
  --name synaxis-gateway-unique \
  --resource-group synaxis-rg \
  --plan synaxis-plan \
  --deployment-container-image-name synaxis/gateway:latest
```

3. **Configure environment variables**:

```bash
az webapp config appsettings set \
  --resource-group synaxis-rg \
  --name synaxis-gateway-unique \
  --settings \
    OPENAI_API_KEY=sk-your-key \
    AZURE_OPENAI_ENDPOINT=your-endpoint
```

4. **Enable continuous deployment** (optional):

```bash
az webapp deployment container config \
  --name synaxis-gateway-unique \
  --resource-group synaxis-rg \
  --enable-cd true
```

### Option 3: Azure Kubernetes Service (AKS)

#### Deployment Steps

1. **Create an AKS cluster**:

```bash
az aks create \
  --resource-group synaxis-rg \
  --name synaxis-aks \
  --node-count 3 \
  --node-vm-size Standard_DS2_v2 \
  --generate-ssh-keys
```

2. **Get credentials**:

```bash
az aks get-credentials \
  --resource-group synaxis-rg \
  --name synaxis-aks
```

3. **Deploy using Helm**:

```bash
helm repo add synaxis https://charts.synaxis.io
helm repo update

helm install synaxis-gateway synaxis/synaxis \
  --set image.tag=latest \
  --set env.OPENAI_API_KEY=sk-your-key \
  --set env.AZURE_OPENAI_ENDPOINT=your-endpoint
```

## AWS Deployment

### Option 1: AWS ECS (Fargate)

#### Deployment Steps

1. **Create an ECS cluster**:

```bash
aws ecs create-cluster \
  --cluster-name synaxis-cluster \
  --region us-east-1
```

2. **Create a task definition** (`task-definition.json`):

```json
{
  "family": "synaxis-task",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "containerDefinitions": [
    {
      "name": "synaxis",
      "image": "synaxis/gateway:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "OPENAI_API_KEY",
          "value": "sk-your-key"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/synaxis",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]
}
```

3. **Register the task definition**:

```bash
aws ecs register-task-definition \
  --cli-input-json file://task-definition.json \
  --region us-east-1
```

4. **Create a service**:

```bash
aws ecs create-service \
  --cluster synaxis-cluster \
  --service-name synaxis-gateway \
  --task-definition synaxis-task \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345],securityGroups=[sg-12345],assignPublicIp=ENABLED}" \
  --region us-east-1
```

5. **Create a load balancer**:

```bash
aws elbv2 create-load-balancer \
  --name synaxis-lb \
  --subnets subnet-12345 subnet-67890 \
  --security-groups sg-12345 \
  --region us-east-1
```

### Option 2: AWS App Runner

#### Deployment Steps

1. **Create an App Runner service**:

```bash
aws apprunner create-service \
  --service-name synaxis-gateway \
  --source-configuration '{
    "ImageRepository": {
      "ImageIdentifier": "synaxis/gateway:latest",
      "ImageConfiguration": {
        "Port": "8080",
        "RuntimeEnvironmentVariables": [
          {
            "Name": "OPENAI_API_KEY",
            "Value": "sk-your-key"
          }
        ]
      },
      "ImageRepositoryType": "ECR_PUBLIC"
    }
  }' \
  --region us-east-1
```

## GCP Deployment

### Option 1: Cloud Run

#### Deployment Steps

1. **Build and deploy**:

```bash
gcloud run deploy synaxis-gateway \
  --image synaxis/gateway:latest \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars OPENAI_API_KEY=sk-your-key,AZURE_OPENAI_ENDPOINT=your-endpoint
```

2. **Get the URL**:

```bash
gcloud run services describe synaxis-gateway \
  --platform managed \
  --region us-central1 \
  --format 'value(status.url)'
```

### Option 2: Google Kubernetes Engine (GKE)

#### Deployment Steps

1. **Create a GKE cluster**:

```bash
gcloud container clusters create synaxis-cluster \
  --num-nodes 3 \
  --machine-type e2-medium \
  --region us-central1
```

2. **Get credentials**:

```bash
gcloud container clusters get-credentials synaxis-cluster \
  --region us-central1
```

3. **Deploy using kubectl**:

```bash
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster (any provider)
- kubectl configured

### Deployment Manifest

Create `deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: synaxis-gateway
  labels:
    app: synaxis-gateway
spec:
  replicas: 3
  selector:
    matchLabels:
      app: synaxis-gateway
  template:
    metadata:
      labels:
        app: synaxis-gateway
    spec:
      containers:
      - name: synaxis
        image: synaxis/gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: OPENAI_API_KEY
          valueFrom:
            secretKeyRef:
              name: synaxis-secrets
              key: openai-api-key
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: synaxis-secrets
              key: azure-endpoint
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: synaxis-service
spec:
  selector:
    app: synaxis-gateway
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

### Create Secrets

```bash
kubectl create secret generic synaxis-secrets \
  --from-literal=openai-api-key=sk-your-key \
  --from-literal=azure-endpoint=your-endpoint
```

### Deploy

```bash
kubectl apply -f deployment.yaml
```

### Horizontal Pod Autoscaler

Create `hpa.yaml`:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: synaxis-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: synaxis-gateway
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

Apply:

```bash
kubectl apply -f hpa.yaml
```

## Docker Deployment

### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  synaxis:
    image: synaxis/gateway:latest
    ports:
      - "8080:8080"
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
      - AZURE_OPENAI_ENDPOINT=${AZURE_OPENAI_ENDPOINT}
      - POSTGRES_CONNECTION_STRING=Host=postgres;Database=synaxis;Username=synaxis;Password=${POSTGRES_PASSWORD}
      - REDIS_CONNECTION_STRING=redis:6379,password=${REDIS_PASSWORD}
    depends_on:
      - postgres
      - redis
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=synaxis
      - POSTGRES_USER=synaxis
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}
    volumes:
      - redis-data:/data
    restart: unless-stopped

volumes:
  postgres-data:
  redis-data:
```

Run:

```bash
docker-compose up -d
```

## Terraform Deployment

### Azure Example

Create `main.tf`:

```hcl
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "synaxis" {
  name     = "synaxis-rg"
  location = "eastus"
}

resource "azurerm_container_app_environment" "synaxis" {
  name                = "synaxis-env"
  location            = azurerm_resource_group.synaxis.location
  resource_group_name = azurerm_resource_group.synaxis.name
}

resource "azurerm_container_app" "synaxis" {
  name                         = "synaxis-gateway"
  resource_group_name          = azurerm_resource_group.synaxis.name
  container_app_environment_id = azurerm_container_app_environment.synaxis.id

  template {
    container {
      name  = "synaxis"
      image = "synaxis/gateway:latest"
      cpu    = 0.5
      memory = "1.0Gi"

      env {
        name  = "OPENAI_API_KEY"
        value = var.openai_api_key
      }

      env {
        name  = "AZURE_OPENAI_ENDPOINT"
        value = var.azure_openai_endpoint
      }
    }

    min_replicas = 1
    max_replicas = 10
  }

  ingress {
    external_enabled = true
    target_port      = 8080
  }
}

variable "openai_api_key" {
  type      = string
  sensitive = true
}

variable "azure_openai_endpoint" {
  type      = string
  sensitive = true
}

output "url" {
  value = azurerm_container_app.synaxis.ingress[0].fqdn
}
```

Deploy:

```bash
terraform init
terraform plan
terraform apply -var="openai_api_key=sk-your-key" -var="azure_openai_endpoint=your-endpoint"
```

## Bicep Deployment

### Azure Container Apps

Create `main.bicep`:

```bicep
param location string = resourceGroup().location
param openaiApiKey string
@secure()
param azureOpenaiEndpoint string

resource synaxisRg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'synaxis-rg'
  location: location
}

resource synaxisEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: 'synaxis-env'
  location: location
  resourceGroupName: synaxisRg.name
}

resource synaxisApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'synaxis-gateway'
  location: location
  resourceGroupName: synaxisRg.name
  managedEnvironmentId: synaxisEnv.id

  properties: {
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
      }
      secrets: [
        {
          name: 'openai-key'
          value: openaiApiKey
        }
        {
          name: 'azure-endpoint'
          value: azureOpenaiEndpoint
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'synaxis'
          image: 'synaxis/gateway:latest'
          env: [
            {
              name: 'OPENAI_API_KEY'
              secretRef: 'openai-key'
            }
            {
              name: 'AZURE_OPENAI_ENDPOINT'
              secretRef: 'azure-endpoint'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
      }
    }
  }
}

output url string = synaxisApp.properties.configuration.ingress.fqdn
```

Deploy:

```bash
az deployment group create \
  --resource-group synaxis-rg \
  --template-file main.bicep \
  --parameters openaiApiKey=sk-your-key azureOpenaiEndpoint=your-endpoint
```

## Environment Configuration

### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `OPENAI_API_KEY` | OpenAI API key | `sk-...` |
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI endpoint | `https://...` |
| `ANTHROPIC_API_KEY` | Anthropic API key | `sk-ant-...` |

### Optional Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `SERVER_PORT` | Server port | `8080` |
| `SERVER_HOST` | Server host | `0.0.0.0` |
| `POSTGRES_CONNECTION_STRING` | PostgreSQL connection | - |
| `REDIS_CONNECTION_STRING` | Redis connection | - |
| `LOG_LEVEL` | Logging level | `Information` |
| `ENABLE_METRICS` | Enable metrics | `true` |
| `ENABLE_TRACING` | Enable tracing | `false` |

### Configuration File

You can also use a configuration file (`appsettings.json`):

```json
{
  "Synaxis": {
    "Providers": {
      "OpenAI": {
        "ApiKey": "sk-your-key",
        "Endpoint": "https://api.openai.com/v1"
      }
    },
    "Routing": {
      "Strategy": "CostOptimized",
      "EnableFallback": true
    },
    "RateLimiting": {
      "Enabled": true,
      "RequestsPerMinute": 60
    }
  }
}
```

## Monitoring and Logging

### Prometheus Metrics

Synaxis exposes Prometheus metrics on `/metrics`:

- `synaxis_requests_total` - Total requests
- `synaxis_requests_duration_seconds` - Request duration
- `synaxis_tokens_total` - Total tokens used
- `synaxis_errors_total` - Total errors

### Grafana Dashboard

Import the Synaxis dashboard from `monitoring/grafana-dashboard.json`.

### Structured Logging

Logs are in JSON format:

```json
{
  "@timestamp": "2024-02-15T10:30:00Z",
  "level": "Information",
  "message": "Request completed",
  "requestId": "req-123",
  "duration": 1234,
  "tokens": 150
}
```

### Distributed Tracing

Enable OpenTelemetry tracing:

```bash
export ENABLE_TRACING=true
export OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
```

## Troubleshooting

### Common Issues

#### Container Won't Start

**Symptoms**: Container exits immediately

**Solutions**:
1. Check logs: `docker logs synaxis-gateway`
2. Verify environment variables are set
3. Check API keys are valid

#### High Memory Usage

**Symptoms**: OOMKilled errors

**Solutions**:
1. Increase memory limits
2. Reduce concurrent requests
3. Enable caching to reduce repeated calls

#### Slow Response Times

**Symptoms**: Requests take >10 seconds

**Solutions**:
1. Check provider status
2. Enable caching
3. Use streaming for long responses
4. Scale horizontally

#### Rate Limit Errors

**Symptoms**: 429 errors from providers

**Solutions**:
1. Implement retry logic with exponential backoff
2. Configure multiple providers for failover
3. Upgrade your provider plan

### Health Checks

Synaxis provides health check endpoints:

- `/health` - Liveness probe
- `/ready` - Readiness probe
- `/metrics` - Prometheus metrics

### Debug Mode

Enable debug logging:

```bash
export LOG_LEVEL=Debug
```

### Getting Help

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/rudironsoni/Synaxis/issues](https://github.com/rudironsoni/Synaxis/issues)
- **Discord**: [Join our Discord](https://discord.gg/synaxis)

---

**Next**: Learn about [Development](./development.md) or explore [Examples](../examples/).
