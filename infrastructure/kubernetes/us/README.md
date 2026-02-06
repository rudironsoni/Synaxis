# Synaxis Kubernetes Manifests - US Region

This directory contains Kubernetes manifests for deploying the Synaxis API application in the US-East-1 region.

## Overview

The manifests deploy:
- **Namespace**: Isolated namespace for Synaxis resources
- **Deployment**: Multi-replica API deployment with health checks
- **Service**: ClusterIP service for internal traffic
- **Ingress**: ALB ingress for external traffic
- **HPA**: Horizontal Pod Autoscaler for dynamic scaling
- **Network Policies**: Secure pod-to-pod and external communication
- **Secrets**: Configuration for database, cache, and vector database
- **PDB**: Pod Disruption Budget for high availability

## Prerequisites

- EKS cluster deployed (via Terraform)
- kubectl configured for the cluster
- AWS Load Balancer Controller installed
- Metrics Server installed

## File Structure

```
.
├── namespace.yaml         # Namespace definition
├── secrets.yaml          # Secrets and ConfigMap
├── deployment.yaml       # API deployment and ServiceAccount
├── service.yaml          # Service and Ingress
├── hpa.yaml             # HorizontalPodAutoscaler and PDB
└── network-policy.yaml  # NetworkPolicy rules
```

## Deployment

### 1. Configure Secrets

Before deploying, update the secrets in `secrets.yaml` with actual values from Terraform outputs:

```bash
# Get Terraform outputs
cd ../../terraform/us
terraform output

# Update secrets.yaml with:
# - DATABASE_HOST: RDS endpoint
# - DATABASE_PASSWORD: From AWS Secrets Manager
# - REDIS_HOST: ElastiCache endpoint
# - REDIS_PASSWORD: From AWS Secrets Manager
# - QDRANT_HOST: Qdrant instance IP
```

Alternatively, use a secrets management solution like:
- AWS Secrets Manager with External Secrets Operator
- Sealed Secrets
- HashiCorp Vault

### 2. Update Service Account

Update the ServiceAccount annotation in `deployment.yaml`:

```yaml
annotations:
  eks.amazonaws.com/role-arn: "arn:aws:iam::ACCOUNT_ID:role/synaxis-api-sa-production"
```

Get the role ARN from Terraform:

```bash
terraform output api_service_account_role_arn
```

### 3. Update Ingress

Update the Ingress annotations in `service.yaml`:

```yaml
alb.ingress.kubernetes.io/certificate-arn: "<ACM_CERT_ARN>"
alb.ingress.kubernetes.io/security-groups: "<ALB_SG_ID>"
alb.ingress.kubernetes.io/wafv2-acl-arn: "<WAF_ACL_ARN>"
```

Update the host in the Ingress spec:

```yaml
rules:
  - host: api.synaxis.yourdomain.com  # Replace with your domain
```

### 4. Deploy

Deploy the manifests in order:

```bash
# Create namespace
kubectl apply -f namespace.yaml

# Deploy secrets and config
kubectl apply -f secrets.yaml

# Deploy application
kubectl apply -f deployment.yaml

# Deploy service and ingress
kubectl apply -f service.yaml

# Deploy autoscaling
kubectl apply -f hpa.yaml

# Deploy network policies
kubectl apply -f network-policy.yaml
```

Or deploy all at once:

```bash
kubectl apply -f .
```

## Verification

### Check Deployment Status

```bash
# Check pods
kubectl get pods -n synaxis

# Check deployment
kubectl get deployment synaxis-api -n synaxis

# Check HPA
kubectl get hpa synaxis-api -n synaxis

# Check ingress
kubectl get ingress synaxis-api -n synaxis
```

### Check Pod Logs

```bash
kubectl logs -n synaxis -l app.kubernetes.io/name=synaxis --tail=100 -f
```

### Check Pod Health

```bash
# Describe pod
kubectl describe pod -n synaxis <pod-name>

# Check readiness
kubectl get pods -n synaxis -o wide

# Test health endpoint
kubectl exec -n synaxis <pod-name> -- curl localhost:8080/health
```

### Check Network Policy

```bash
kubectl get networkpolicy -n synaxis
kubectl describe networkpolicy -n synaxis synaxis-api-network-policy
```

## Configuration

### Environment Variables

The application configuration is split between:
- **ConfigMap** (`synaxis-api-config`): Non-sensitive configuration
- **Secrets**: Sensitive credentials and keys

### Resource Limits

Current resource configuration per pod:

```yaml
resources:
  requests:
    cpu: "500m"
    memory: "512Mi"
  limits:
    cpu: "2000m"
    memory: "2Gi"
```

Adjust based on your workload requirements.

### Autoscaling

The HPA is configured with:
- **Min replicas**: 3
- **Max replicas**: 20
- **CPU target**: 70%
- **Memory target**: 80%

Custom metrics (requires Prometheus Adapter):
- HTTP requests per second: 1000 req/s per pod
- P95 latency: 500ms

### High Availability

- **Pod Anti-Affinity**: Spreads pods across AZs and nodes
- **Topology Spread**: Ensures even distribution across zones
- **PDB**: Maintains minimum 2 pods during disruptions
- **Rolling Updates**: Zero-downtime deployments (maxUnavailable: 0)

## Network Policies

### Default Deny

All ingress traffic is denied by default unless explicitly allowed.

### Allowed Ingress

- From ALB/Ingress Controller on port 8080
- From same namespace (pod-to-pod)
- From Prometheus for metrics scraping

### Allowed Egress

- DNS resolution (kube-dns)
- PostgreSQL (port 5432)
- Redis (port 6379)
- Qdrant (ports 6333, 6334)
- HTTPS (port 443) for external APIs
- Cross-region traffic (EU and Brazil VPCs)

## Monitoring

### Metrics

Prometheus metrics are exposed on port 8080 at `/metrics`:

```yaml
annotations:
  prometheus.io/scrape: "true"
  prometheus.io/port: "8080"
  prometheus.io/path: "/metrics"
```

### Health Checks

Three types of probes are configured:

1. **Liveness Probe** (`/live`): Restart if unhealthy
2. **Readiness Probe** (`/ready`): Remove from load balancer if unhealthy
3. **Startup Probe** (`/health`): Initial startup check

### Logs

Logs are written to stdout in JSON format and collected by CloudWatch Container Insights.

View logs:

```bash
# Tail logs
kubectl logs -n synaxis -l app.kubernetes.io/component=api -f

# Get logs from specific container
kubectl logs -n synaxis <pod-name> -c api

# Get previous logs (after crash)
kubectl logs -n synaxis <pod-name> --previous
```

## Troubleshooting

### Pod Not Starting

```bash
# Check events
kubectl describe pod -n synaxis <pod-name>

# Check logs
kubectl logs -n synaxis <pod-name>

# Check resource constraints
kubectl top pod -n synaxis <pod-name>
```

### Database Connection Issues

```bash
# Test DNS resolution
kubectl exec -n synaxis <pod-name> -- nslookup <rds-endpoint>

# Test connectivity
kubectl exec -n synaxis <pod-name> -- nc -zv <rds-endpoint> 5432

# Check secrets
kubectl get secret postgres-credentials -n synaxis -o jsonpath='{.data}' | jq
```

### HPA Not Scaling

```bash
# Check metrics server
kubectl top nodes
kubectl top pods -n synaxis

# Check HPA status
kubectl describe hpa synaxis-api -n synaxis

# Check HPA metrics
kubectl get hpa synaxis-api -n synaxis -o yaml
```

### Ingress Issues

```bash
# Check ingress
kubectl describe ingress synaxis-api -n synaxis

# Check ALB controller logs
kubectl logs -n kube-system -l app.kubernetes.io/name=aws-load-balancer-controller

# Check target group health in AWS Console
```

### Network Policy Issues

```bash
# Check policies
kubectl get networkpolicy -n synaxis
kubectl describe networkpolicy -n synaxis

# Test connectivity between pods
kubectl exec -n synaxis <pod-name> -- curl <service-name>

# Temporarily disable network policy for testing
kubectl delete networkpolicy -n synaxis <policy-name>
```

## Security

### RBAC

The ServiceAccount has minimal permissions via IRSA:
- Read secrets from AWS Secrets Manager
- Decrypt with KMS
- Access S3 buckets

### Pod Security

- Non-root user (UID 1000)
- Read-only root filesystem
- Drop all capabilities
- Seccomp profile enabled

### Network Isolation

- Network policies restrict traffic
- Only necessary ports exposed
- Egress limited to required services

## Updates

### Rolling Update

```bash
# Update image
kubectl set image deployment/synaxis-api -n synaxis api=synaxis/api:v1.1.0

# Check rollout status
kubectl rollout status deployment/synaxis-api -n synaxis

# Check rollout history
kubectl rollout history deployment/synaxis-api -n synaxis
```

### Rollback

```bash
# Rollback to previous version
kubectl rollout undo deployment/synaxis-api -n synaxis

# Rollback to specific revision
kubectl rollout undo deployment/synaxis-api -n synaxis --to-revision=2
```

### Update Configuration

```bash
# Edit ConfigMap
kubectl edit configmap synaxis-api-config -n synaxis

# Restart pods to pick up new config
kubectl rollout restart deployment/synaxis-api -n synaxis
```

## Clean Up

```bash
# Delete all resources
kubectl delete -f .

# Or delete namespace (removes everything)
kubectl delete namespace synaxis
```

## Best Practices

1. **Always test in staging** before deploying to production
2. **Use GitOps** (ArgoCD, Flux) for declarative deployments
3. **Version control** all manifests
4. **Use Helm** for templating and easier management
5. **Monitor** resource usage and adjust limits
6. **Regular backups** of persistent data
7. **Security scanning** of container images
8. **Keep secrets encrypted** at rest (use External Secrets Operator)

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [EKS Best Practices](https://aws.github.io/aws-eks-best-practices/)
- [AWS Load Balancer Controller](https://kubernetes-sigs.github.io/aws-load-balancer-controller/)
- [Network Policies](https://kubernetes.io/docs/concepts/services-networking/network-policies/)
