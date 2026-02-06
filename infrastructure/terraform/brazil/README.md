# Synaxis Brazil Region Infrastructure

This directory contains the Terraform configuration and Kubernetes manifests for deploying Synaxis in the Brazil (sa-east-1 - São Paulo) region.

## Architecture Overview

### AWS Resources
- **VPC**: 10.2.0.0/16 across 3 Availability Zones
  - Public subnets: 10.2.101.0/24, 10.2.102.0/24, 10.2.103.0/24
  - Private subnets: 10.2.1.0/24, 10.2.2.0/24, 10.2.3.0/24
  - Database subnets: 10.2.201.0/24, 10.2.202.0/24, 10.2.203.0/24
- **RDS PostgreSQL 16**: Multi-AZ, encrypted with KMS
- **ElastiCache Redis 7**: 3-node cluster, encrypted at rest and in transit
- **EKS Cluster**: Kubernetes 1.28 with managed node groups
- **Qdrant**: Vector database on EC2 instance
- **ALB**: Application Load Balancer with HTTPS
- **VPC Peering**: Connections to US and EU regions

### Compliance
- **LGPD**: Brazilian General Data Protection Law
- **Data Residency**: All data stays in sa-east-1
- **Encryption**: KMS encryption at rest for all data stores
- **Audit Logging**: CloudWatch logs for all services
- **ANPD Notification**: Capability enabled for data breach notification

## Prerequisites

### Required Tools
- Terraform >= 1.6.0
- AWS CLI v2
- kubectl >= 1.28
- helm >= 3.11

### AWS Credentials
```bash
export AWS_PROFILE=synaxis-brazil
export AWS_REGION=sa-east-1
```

### Terraform Backend
Ensure the S3 bucket and DynamoDB table exist:
```bash
aws s3 mb s3://synaxis-terraform-state --region sa-east-1
aws dynamodb create-table \
  --table-name synaxis-terraform-locks \
  --attribute-definitions AttributeName=LockID,AttributeType=S \
  --key-schema AttributeName=LockID,KeyType=HASH \
  --billing-mode PAY_PER_REQUEST \
  --region sa-east-1
```

## Deployment

### 1. Terraform Infrastructure

Initialize Terraform:
```bash
cd infrastructure/terraform/brazil
terraform init
```

Review the plan:
```bash
terraform plan -out=tfplan
```

Apply the infrastructure:
```bash
terraform apply tfplan
```

**Important Outputs:**
- `eks_cluster_name`: Name of the EKS cluster
- `rds_endpoint`: PostgreSQL endpoint
- `redis_endpoint`: Redis cluster endpoint
- `alb_dns_name`: Load balancer DNS name
- `qdrant_private_ip`: Qdrant instance IP

### 2. Configure kubectl

```bash
aws eks update-kubeconfig \
  --region sa-east-1 \
  --name $(terraform output -raw eks_cluster_name)
```

Verify connection:
```bash
kubectl get nodes
```

### 3. Deploy Kubernetes Resources

Deploy in order:
```bash
# Create namespace
kubectl apply -f infrastructure/kubernetes/brazil/namespace.yaml

# Create secrets (update values first)
kubectl apply -f infrastructure/kubernetes/brazil/secrets.yaml

# Deploy application
kubectl apply -f infrastructure/kubernetes/brazil/deployment.yaml
kubectl apply -f infrastructure/kubernetes/brazil/service.yaml
kubectl apply -f infrastructure/kubernetes/brazil/hpa.yaml

# Apply network policies
kubectl apply -f infrastructure/kubernetes/brazil/network-policy.yaml

# Configure ingress
kubectl apply -f infrastructure/kubernetes/brazil/ingress.yaml

# Set resource limits
kubectl apply -f infrastructure/kubernetes/brazil/resource-limits.yaml

# Enable monitoring
kubectl apply -f infrastructure/kubernetes/brazil/monitoring.yaml
```

Verify deployment:
```bash
kubectl get pods -n synaxis
kubectl get svc -n synaxis
kubectl get hpa -n synaxis
```

## Configuration

### Database Connection
Update secrets with actual values from Terraform outputs:
```bash
DB_HOST=$(terraform output -raw rds_address)
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id $(terraform output -raw db_password_secret_arn) \
  --query SecretString --output text)

kubectl create secret generic synaxis-db-credentials \
  --from-literal=DB_HOST=$DB_HOST \
  --from-literal=DB_PORT=5432 \
  --from-literal=DB_NAME=synaxis \
  --from-literal=DB_USERNAME=synaxis_admin \
  --from-literal=DB_PASSWORD=$DB_PASSWORD \
  --namespace synaxis \
  --dry-run=client -o yaml | kubectl apply -f -
```

### Redis Connection
```bash
REDIS_HOST=$(terraform output -raw redis_endpoint)
REDIS_AUTH_TOKEN=$(aws secretsmanager get-secret-value \
  --secret-id synaxis-brazil-redis-auth-token \
  --query SecretString --output text)

kubectl create secret generic synaxis-redis-credentials \
  --from-literal=REDIS_HOST=$REDIS_HOST \
  --from-literal=REDIS_PORT=6379 \
  --from-literal=REDIS_TLS=true \
  --from-literal=REDIS_AUTH_TOKEN=$REDIS_AUTH_TOKEN \
  --namespace synaxis \
  --dry-run=client -o yaml | kubectl apply -f -
```

### Service Account IAM Role
Update the deployment with the correct IAM role ARN:
```bash
ROLE_ARN=$(terraform output -raw synaxis_api_sa_role_arn)
kubectl annotate serviceaccount synaxis-api \
  eks.amazonaws.com/role-arn=$ROLE_ARN \
  --namespace synaxis \
  --overwrite
```

## Monitoring

### CloudWatch Logs
```bash
aws logs tail /aws/eks/synaxis-brazil-production/cluster --follow
aws logs tail /aws/synaxis/sa-east-1 --follow
```

### Kubernetes Logs
```bash
kubectl logs -f deployment/synaxis-api -n synaxis
```

### Metrics
Access Grafana dashboard:
```bash
kubectl port-forward -n monitoring svc/grafana 3000:3000
```

## VPC Peering

### Connect to US Region
```bash
# In US region Terraform
terraform output vpc_id  # Use this value

# Update variables
export TF_VAR_us_vpc_id="vpc-xxxxx"
export TF_VAR_us_vpc_cidr="10.0.0.0/16"
terraform apply
```

### Connect to EU Region
```bash
# In EU region Terraform
terraform output vpc_id  # Use this value

# Update variables
export TF_VAR_eu_vpc_id="vpc-yyyyy"
export TF_VAR_eu_vpc_cidr="10.1.0.0/16"
terraform apply
```

## Scaling

### Manual Scaling
```bash
kubectl scale deployment synaxis-api --replicas=10 -n synaxis
```

### Horizontal Pod Autoscaler
The HPA automatically scales based on:
- CPU utilization: 70% target
- Memory utilization: 80% target
- HTTP requests per second: 1000 average

### EKS Node Group Scaling
Modify node group size:
```bash
terraform apply -var="eks_node_desired_size=10"
```

## Security

### LGPD Compliance Checklist
- [x] Data encryption at rest (KMS)
- [x] Data encryption in transit (TLS)
- [x] Network isolation (VPC, Security Groups)
- [x] Audit logging (CloudWatch)
- [x] Access control (IAM, RBAC)
- [x] Data residency (sa-east-1 only)
- [x] Backup and recovery (RDS automated backups)
- [x] Secrets management (AWS Secrets Manager)

### Security Scanning
```bash
# Scan Docker images
trivy image synaxis/api:latest

# Scan Kubernetes manifests
kubesec scan infrastructure/kubernetes/brazil/deployment.yaml

# Check for vulnerabilities
kubectl get vulnerabilityreports -n synaxis
```

## Disaster Recovery

### RDS Backups
- Automated daily backups with 30-day retention
- Manual snapshots before major changes
- Cross-region backup replication (optional)

### Restore from Backup
```bash
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier synaxis-brazil-restored \
  --db-snapshot-identifier snapshot-id \
  --region sa-east-1
```

## Troubleshooting

### Pod Not Starting
```bash
kubectl describe pod <pod-name> -n synaxis
kubectl logs <pod-name> -n synaxis --previous
```

### Database Connection Issues
```bash
# Test connectivity from pod
kubectl run -it --rm debug --image=postgres:16 --restart=Never -- \
  psql -h $DB_HOST -U synaxis_admin -d synaxis
```

### Redis Connection Issues
```bash
# Test Redis connectivity
kubectl run -it --rm redis-cli --image=redis:7 --restart=Never -- \
  redis-cli -h $REDIS_HOST -p 6379 --tls PING
```

### ALB Not Routing Traffic
```bash
# Check target group health
aws elbv2 describe-target-health \
  --target-group-arn $(terraform output -raw alb_target_group_arn)
```

## Cost Optimization

### Estimated Monthly Costs (USD)
- EKS Cluster: $73
- EC2 Instances (6x m6i.xlarge): ~$750
- RDS (db.r6g.xlarge Multi-AZ): ~$560
- ElastiCache (3x cache.r7g.large): ~$510
- ALB: ~$25
- NAT Gateways (3): ~$100
- Data Transfer: Variable
- **Total: ~$2,018/month**

### Cost Reduction Options
1. Use Reserved Instances (30-40% savings)
2. Reduce node count during off-peak hours
3. Use Spot Instances for non-critical workloads
4. Enable S3 lifecycle policies for logs

## Support

For issues or questions:
- Internal Slack: #synaxis-brazil
- Email: ops@synaxis.com
- Oncall: PagerDuty

## License

Proprietary - Synaxis Inc. 2026
