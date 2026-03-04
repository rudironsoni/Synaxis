# Synaxis Staging Environment

This directory contains the infrastructure-as-code and deployment configurations for the Synaxis Staging Environment, which mirrors production for comprehensive testing.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     AWS Cloud (us-east-1)                   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌───────────────────────────────────────────────────────┐│
│  │                     EKS Cluster                        ││
│  │  ┌─────────────────────────────────────────────────┐  ││
│  │  │              synaxis-staging Namespace          │  ││
│  │  │                                                  │  ││
│  │  │  ┌──────────┬──────────┬──────────┬──────────┐  │  ││
│  │  │  │ Gateway  │ Identity │ Inference│ Billing  │  │  ││
│  │  │  │  2 pods  │  2 pods  │  2 pods  │  2 pods  │  │  ││
│  │  │  └──────────┴──────────┴──────────┴──────────┘  │  ││
│  │  │  ┌──────────┬──────────┐                        │  ││
│  │  │  │ Agents   │Orchestr. │                        │  ││
│  │  │  │  2 pods  │  2 pods  │                        │  ││
│  │  │  └──────────┴──────────┘                        │  ││
│  │  │                                                  │  ││
│  │  │  Istio Service Mesh (mTLS)                      │  ││
│  │  │  Network Policies                               │  ││
│  │  └─────────────────────────────────────────────────┘  ││
│  │                                                        ││
│  │  ┌─────────────┬─────────────┬─────────────┐          ││
│  │  │ Prometheus  │   Grafana   │    Loki     │          ││
│  │  └─────────────┴─────────────┴─────────────┘          ││
│  └───────────────────────────────────────────────────────┘│
│                                                            │
│  ┌───────────────┬───────────────┬───────────────┐      │
│  │   RDS         │   ElastiCache   │     S3        │      │
│  │ PostgreSQL 16 │    Redis 7     │  Storage      │      │
│  └───────────────┴───────────────┴───────────────┘      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## Directory Structure

```
infrastructure/staging/
├── helm/
│   └── synaxis-staging/
│       ├── Chart.yaml          # Helm chart metadata
│       ├── values.yaml         # Default configuration values
│       └── templates/          # Kubernetes resource templates
│           ├── namespace.yaml
│           ├── secrets.yaml
│           ├── configmap.yaml
│           ├── deployments.yaml
│           ├── services.yaml
│           ├── hpa.yaml
│           ├── network-policies.yaml
│           ├── istio.yaml
│           └── rbac.yaml
├── terraform/
│   ├── main.tf                 # Main Terraform configuration
│   ├── variables.tf            # Variable definitions
│   └── providers.tf            # Provider configuration
├── monitoring/
│   ├── prometheus/
│   │   ├── prometheus.yml        # Prometheus configuration
│   │   └── alerts.yml            # Alert rules
│   ├── grafana/                 # Grafana dashboards (JSON)
│   └── loki/
│       └── loki.yml             # Loki configuration
└── scripts/
    ├── deploy.sh                # Main deployment script
    └── database-setup.sh        # Database migration script
```

## Prerequisites

- AWS CLI configured with appropriate credentials
- kubectl configured
- Helm 3.x
- Terraform >= 1.5.0
- Docker (for local testing)

## Quick Start

### 1. Deploy Infrastructure

```bash
# Deploy AWS infrastructure (EKS, RDS, ElastiCache, S3)
cd infrastructure/staging/terraform
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

### 2. Deploy Application

```bash
# Run the main deployment script
cd infrastructure/staging
./scripts/deploy.sh
```

Or run individual steps:

```bash
# Deploy infrastructure only
./scripts/deploy.sh infrastructure

# Deploy Kubernetes resources
./scripts/deploy.sh kubernetes

# Run health checks
./scripts/deploy.sh health-check

# Run smoke tests
./scripts/deploy.sh smoke-tests
```

### 3. Verify Deployment

```bash
# Check pod status
kubectl get pods -n synaxis-staging

# Check services
kubectl get svc -n synaxis-staging

# Access logs
kubectl logs -f deployment/gateway -n synaxis-staging
```

## Services Configuration

| Service | Replicas | CPU | Memory | Endpoint |
|---------|----------|-----|--------|----------|
| Gateway | 2 | 250m-1000m | 512Mi-2Gi | /api/v1/* |
| Identity | 2 | 250m-1000m | 512Mi-2Gi | /api/v1/identity/* |
| Inference | 2 | 500m-2000m | 1Gi-4Gi | /api/v1/inference/* |
| Billing | 2 | 250m-1000m | 512Mi-2Gi | /api/v1/billing/* |
| Agents | 2 | 250m-1000m | 512Mi-2Gi | /api/v1/agents/* |
| Orchestration | 2 | 250m-1000m | 512Mi-2Gi | /api/v1/orchestration/* |

## Health Check Endpoints

All services expose the following health check endpoints:

- `GET /health/liveness` - Liveness probe (returns 200 if service is running)
- `GET /health/readiness` - Readiness probe (returns 200 if service is ready to accept traffic)
- `GET /health/startup` - Startup probe (returns 200 after service initialization)
- `GET /metrics` - Prometheus metrics endpoint

## Monitoring

### Access Grafana

```bash
kubectl port-forward svc/synaxis-staging-grafana 3000:3000 -n synaxis-staging
```

Access at: http://localhost:3000
- Username: `admin`
- Password: Retrieved from secret: `synaxis-grafana-credentials`

### Access Prometheus

```bash
kubectl port-forward svc/synaxis-staging-prometheus-server 9090:9090 -n synaxis-staging
```

Access at: http://localhost:9090

## Database Operations

### Run Migrations

```bash
./scripts/database-setup.sh migrate
```

### Verify Database

```bash
./scripts/database-setup.sh verify
```

## Network Policies

The staging environment includes strict network policies:

- **Default Deny**: All ingress and egress traffic is denied by default
- **Internal Communication**: Services can communicate within the namespace
- **DNS Access**: Pods can access DNS (kube-system namespace)
- **Database Access**: Only specific services can access PostgreSQL (port 5432)
- **Redis Access**: Only specific services can access Redis (port 6379)
- **Gateway External**: Gateway service accepts external traffic

## Service Mesh (Istio)

Istio is configured with:

- **mTLS**: PERMISSIVE mode for staging (allows non-mTLS traffic)
- **Traffic Routing**: VirtualServices route traffic to appropriate services
- **Retries**: Automatic retries with backoff
- **Observability**: Full distributed tracing with 100% sampling

## Scaling Configuration

### Horizontal Pod Autoscaler (HPA)

All services are configured with HPA:

- **Min Replicas**: 2
- **Max Replicas**: 6
- **Target CPU**: 70%
- **Target Memory**: 80%

### Manual Scaling

```bash
# Scale a specific service
kubectl scale deployment inference --replicas=4 -n synaxis-staging
```

## Cleanup

```bash
# Destroy Helm release
helm uninstall synaxis-staging -n synaxis-staging

# Destroy Terraform infrastructure
cd terraform
terraform destroy
```

## Troubleshooting

### Pod Not Starting

```bash
# Check pod events
kubectl describe pod <pod-name> -n synaxis-staging

# Check logs
kubectl logs <pod-name> -n synaxis-staging
```

### Service Unreachable

```bash
# Check service endpoints
kubectl get endpoints <service-name> -n synaxis-staging

# Test from within cluster
kubectl run -it --rm debug --image=curlimages/curl --restart=Never -- curl http://<service-name>:8080/health
```

### Database Connection Issues

```bash
# Test database connectivity
kubectl exec -it deployment/synaxis-staging-postgresql -n synaxis-staging -- pg_isready -U synaxis_staging_user
```

## Security Considerations

- All secrets are stored in Kubernetes Secrets
- Network policies restrict inter-service communication
- Service accounts use IRSA for AWS access
- Container security contexts enforce non-root execution
- Read-only root filesystems
- Resource limits prevent resource exhaustion

## Maintenance

### Update Images

```bash
# Update image tag
helm upgrade synaxis-staging ./helm/synaxis-staging \
  -n synaxis-staging \
  --set global.image.tag=new-version
```

### Backup Database

```bash
# Create database backup
kubectl exec -it deployment/synaxis-staging-postgresql -n synaxis-staging -- \
  pg_dump -U synaxis_staging_user synaxis_staging > backup.sql
```

## Support

For issues or questions regarding the staging environment:

1. Check the [Troubleshooting](#troubleshooting) section
2. Review application logs
3. Consult the main project documentation
