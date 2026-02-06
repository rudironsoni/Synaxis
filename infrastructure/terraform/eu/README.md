# Synaxis EU Region Infrastructure

Production-ready Terraform and Kubernetes configurations for deploying Synaxis in the EU-West-1 (Frankfurt) region with full GDPR compliance.

## Overview

This infrastructure provides:

- **Multi-AZ High Availability**: Resources distributed across 3 availability zones
- **GDPR Compliance**: Data residency in EU, encryption at rest (AES-256), encryption in transit (TLS 1.3)
- **Security**: Least privilege access, network isolation, VPC endpoints
- **Scalability**: Auto-scaling (3-20 pods), multi-AZ redundancy
- **Observability**: CloudWatch logs, metrics, VPC flow logs

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Internet                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                  Application Load Balancer                   │
│                    (TLS 1.3, HTTPS)                         │
└──────────────────────┬──────────────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        ▼              ▼              ▼
    ┌───────┐      ┌───────┐      ┌───────┐
    │  AZ A │      │  AZ B │      │  AZ C │
    └───┬───┘      └───┬───┘      └───┬───┘
        │              │              │
        ▼              ▼              ▼
    ┌───────────────────────────────────┐
    │         EKS Cluster               │
    │   (Synaxis API - 3+ replicas)    │
    └───┬───────────────────────────┬───┘
        │                           │
        ▼                           ▼
    ┌─────────┐                 ┌─────────┐
    │   RDS   │                 │  Redis  │
    │Postgres │                 │ Cluster │
    │Multi-AZ │                 │Multi-AZ │
    └─────────┘                 └─────────┘
```

## Prerequisites

1. **AWS Account** with appropriate permissions
2. **Terraform** >= 1.6.0
3. **kubectl** >= 1.28
4. **AWS CLI** v2
5. **Docker** (for building images)

## Quick Start

### 1. Configure AWS Credentials

```bash
aws configure --profile synaxis-eu
export AWS_PROFILE=synaxis-eu
```

### 2. Initialize Terraform

```bash
cd infrastructure/terraform/eu
terraform init
```

### 3. Create terraform.tfvars

```hcl
# terraform.tfvars
environment = "prod"
aws_region  = "eu-west-1"

# Database configuration
db_password = "YourSecurePassword123!" # Min 16 characters

# Node configuration
eks_node_desired_size = 3
eks_node_min_size     = 3
eks_node_max_size     = 10
```

### 4. Deploy Infrastructure

```bash
# Review plan
terraform plan

# Apply configuration
terraform apply
```

This will create:
- VPC with 12 subnets across 3 AZs
- RDS PostgreSQL 16 (Multi-AZ)
- ElastiCache Redis 7 (Multi-AZ)
- EKS cluster with managed node groups
- Application Load Balancer
- Security groups and KMS keys

### 5. Configure kubectl

```bash
aws eks update-kubeconfig --region eu-west-1 --name synaxis-prod
kubectl get nodes
```

### 6. Deploy Kubernetes Resources

```bash
cd infrastructure/kubernetes/eu

# Update secrets with actual values
# Edit secrets.yaml and replace base64-encoded placeholders

# Apply resources
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml
kubectl apply -f network-policy.yaml
```

### 7. Verify Deployment

```bash
# Check pods
kubectl get pods -n synaxis-eu

# Check services
kubectl get svc -n synaxis-eu

# Check HPA
kubectl get hpa -n synaxis-eu

# Get ALB endpoint
kubectl get svc synaxis-api -n synaxis-eu -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'
```

## Configuration

### Database Credentials

Get RDS endpoint from Terraform output:

```bash
terraform output rds_endpoint
```

Update `secrets.yaml`:

```bash
echo -n "synaxis-prod.xxxxx.eu-west-1.rds.amazonaws.com" | base64
```

### Redis Credentials

Get Redis endpoint:

```bash
terraform output redis_endpoint
```

### Container Images

Build and push to ECR:

```bash
# Get ECR login
aws ecr get-login-password --region eu-west-1 | docker login --username AWS --password-stdin <account-id>.dkr.ecr.eu-west-1.amazonaws.com

# Build and push
docker build -t synaxis-api:latest .
docker tag synaxis-api:latest <account-id>.dkr.ecr.eu-west-1.amazonaws.com/synaxis-api:latest
docker push <account-id>.dkr.ecr.eu-west-1.amazonaws.com/synaxis-api:latest
```

Update image in `deployment.yaml`.

## Monitoring

### CloudWatch Logs

Application logs are sent to:
- Log Group: `/aws/synaxis/eu-west-1`
- Retention: 90 days (GDPR compliant)

### Metrics

Access via CloudWatch:
- EKS cluster metrics
- RDS performance insights
- Redis metrics
- ALB metrics

### Alarms

Pre-configured alarms for:
- RDS CPU utilization (>80%)
- RDS storage space (<10GB)
- Redis CPU utilization (>75%)
- Redis memory utilization (>80%)

## Security

### Encryption

- **At Rest**: AES-256 via KMS
  - RDS data and backups
  - ElastiCache data
  - EBS volumes
  - Kubernetes secrets

- **In Transit**: TLS 1.3
  - ALB to clients
  - PostgreSQL connections
  - Redis connections
  - Inter-pod communication

### Network Security

- Private subnets for workloads
- NAT Gateways for outbound traffic
- Security groups with least privilege
- Network policies for pod isolation
- VPC flow logs for auditing

### IAM

- IRSA (IAM Roles for Service Accounts) for pod-level permissions
- Separate roles for cluster, nodes, and applications
- KMS key policies for encryption

## Compliance

### GDPR

- ✅ Data residency in EU (eu-west-1)
- ✅ Encryption at rest and in transit
- ✅ Audit logging enabled
- ✅ 90-day log retention
- ✅ Network isolation
- ✅ Multi-AZ backup retention (30 days)

### Audit Trail

- VPC Flow Logs
- EKS control plane logs
- RDS audit logs
- CloudWatch application logs

## Scaling

### Horizontal Pod Autoscaling

- Min replicas: 3
- Max replicas: 20
- Scale up: CPU >70% or Memory >80%
- Scale down: After 5-minute stabilization

### Vertical Scaling

- RDS: Scale instance class as needed
- Redis: Add more nodes to cluster
- EKS nodes: Adjust node count/type

## Disaster Recovery

### Backups

- **RDS**: Automated backups (30 days retention), point-in-time recovery
- **Redis**: Daily snapshots (7 days retention)
- **Terraform State**: S3 with versioning (uncomment backend)

### Recovery Procedures

1. **Database Restore**:
   ```bash
   aws rds restore-db-instance-from-db-snapshot \
     --db-instance-identifier synaxis-restored \
     --db-snapshot-identifier <snapshot-id>
   ```

2. **Redis Restore**:
   ```bash
   aws elasticache create-replication-group \
     --replication-group-id synaxis-restored \
     --snapshot-name <snapshot-name>
   ```

## Cost Optimization

### Current Estimated Costs (Monthly)

- RDS (db.r6g.xlarge Multi-AZ): ~$600
- ElastiCache (3x cache.r6g.large): ~$450
- EKS (cluster + nodes): ~$300
- NAT Gateways (3x): ~$100
- ALB: ~$25
- Data transfer: ~$50

**Total**: ~$1,525/month

### Optimization Tips

1. Use Reserved Instances for RDS and ElastiCache (40% savings)
2. Use EC2 Savings Plans for EKS nodes (up to 72% savings)
3. Enable S3 VPC Gateway Endpoint (free)
4. Use Aurora Serverless v2 for variable workloads
5. Reduce NAT Gateways to 1 in non-prod

## Troubleshooting

### Pods Not Starting

```bash
kubectl describe pod <pod-name> -n synaxis-eu
kubectl logs <pod-name> -n synaxis-eu
```

### Database Connection Issues

```bash
# Test from pod
kubectl run -it --rm debug --image=postgres:16 --restart=Never -- \
  psql -h <rds-endpoint> -U synaxis_admin -d synaxis
```

### Redis Connection Issues

```bash
# Test from pod
kubectl run -it --rm debug --image=redis:7 --restart=Never -- \
  redis-cli -h <redis-endpoint> -p 6379 --tls --askpass
```

### HPA Not Scaling

```bash
# Check metrics server
kubectl top nodes
kubectl top pods -n synaxis-eu

# Check HPA status
kubectl describe hpa synaxis-api-hpa -n synaxis-eu
```

## Maintenance

### Updating EKS Version

```bash
# Update cluster
terraform apply -var="eks_cluster_version=1.29"

# Update add-ons
kubectl get daemonset -n kube-system
```

### Updating Application

```bash
# Update image
kubectl set image deployment/synaxis-api api=<new-image> -n synaxis-eu

# Rollback if needed
kubectl rollout undo deployment/synaxis-api -n synaxis-eu
```

### Database Maintenance

- Maintenance window: Sunday 04:00-05:00 UTC
- Auto minor version upgrades: Enabled
- Backup window: 03:00-04:00 UTC

## Support

For issues or questions:
- GitHub Issues: https://github.com/rudironsoni/Synaxis/issues
- Documentation: https://github.com/rudironsoni/Synaxis/tree/main/docs

## License

MIT License - See main repository for details.
