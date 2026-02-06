# Synaxis US-East-1 Infrastructure

This directory contains Terraform configurations for deploying the Synaxis inference gateway infrastructure in the US-East-1 (Virginia) region.

## Overview

The infrastructure includes:
- **VPC**: Multi-AZ VPC with public, private, and database subnets across 3 availability zones
- **EKS**: Managed Kubernetes cluster with autoscaling node groups
- **RDS**: PostgreSQL 16 Multi-AZ instance with automated backups
- **ElastiCache**: Redis 7 cluster with replication
- **Qdrant**: Vector database on EC2 with persistent EBS storage
- **ALB**: Application Load Balancer with WAF protection
- **VPC Peering**: Connections to EU and Brazil regions for cross-region communication

## Prerequisites

- AWS CLI configured with appropriate credentials
- Terraform >= 1.6.0
- kubectl >= 1.28
- Helm >= 3.11

## Directory Structure

```
.
├── main.tf                    # Main Terraform configuration
├── variables.tf               # Input variables
├── outputs.tf                 # Output values
├── vpc.tf                     # VPC and networking resources
├── rds.tf                     # PostgreSQL database
├── elasticache.tf            # Redis cluster
├── eks.tf                     # EKS cluster and node groups
├── security.tf               # Security groups, IAM roles, ALB, Qdrant
├── peering.tf                # VPC peering connections
├── policies/                 # IAM policy documents
│   └── aws-load-balancer-controller-policy.json
└── user-data/                # EC2 user data scripts
    └── qdrant-init.sh
```

## Usage

### 1. Initialize Terraform

```bash
terraform init
```

### 2. Create terraform.tfvars

Copy the example file and customize:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with your specific values.

### 3. Plan

```bash
terraform plan -out=tfplan
```

### 4. Apply

```bash
terraform apply tfplan
```

### 5. Configure kubectl

After deployment, configure kubectl to access the EKS cluster:

```bash
aws eks update-kubeconfig --region us-east-1 --name synaxis-production
```

### 6. Deploy Kubernetes Resources

```bash
cd ../../kubernetes/us
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml
kubectl apply -f network-policy.yaml
```

## Configuration

### Required Variables

- `environment`: Environment name (e.g., production, staging)
- `aws_region`: AWS region (default: us-east-1)

### VPC Peering Variables

To enable VPC peering with other regions, set:

```hcl
eu_vpc_id      = "vpc-xxxxx"
eu_vpc_cidr    = "10.1.0.0/16"
brazil_vpc_id  = "vpc-yyyyy"
brazil_vpc_cidr = "10.2.0.0/16"
```

### Database Configuration

Customize RDS instance:

```hcl
db_instance_class         = "db.r6g.xlarge"
db_allocated_storage      = 100
db_max_allocated_storage  = 500
```

### EKS Configuration

Customize EKS cluster:

```hcl
eks_cluster_version     = "1.28"
eks_node_instance_types = ["m6i.xlarge", "m6i.2xlarge"]
eks_node_desired_size   = 6
eks_node_min_size       = 3
eks_node_max_size       = 15
```

## Outputs

Key outputs after deployment:

- `vpc_id`: VPC ID
- `eks_cluster_name`: EKS cluster name
- `rds_endpoint`: PostgreSQL endpoint
- `redis_endpoint`: Redis endpoint
- `qdrant_endpoint`: Qdrant endpoint
- `alb_dns_name`: Load balancer DNS name

## Security

### Encryption

- All data at rest is encrypted using KMS
- RDS has encryption enabled with automated key rotation
- Redis has in-transit and at-rest encryption
- EBS volumes are encrypted

### Network Security

- Private subnets for workloads
- Security groups with least privilege access
- Network policies in Kubernetes
- WAF protection on ALB

### IAM

- IRSA (IAM Roles for Service Accounts) enabled
- Least privilege IAM policies
- Service account per component

## High Availability

- Multi-AZ deployment across 3 availability zones
- RDS Multi-AZ with automated failover
- Redis cluster with automatic failover
- EKS node groups with autoscaling
- Pod anti-affinity rules

## Monitoring

### CloudWatch

- VPC Flow Logs
- RDS Enhanced Monitoring
- EKS Control Plane Logs
- ElastiCache logs
- ALB access logs

### Metrics

- Prometheus metrics exposed on pods
- CloudWatch Container Insights
- Custom application metrics

### Alarms

- RDS CPU, storage, and connections
- Redis CPU, memory, and evictions
- EKS node health

## Backup and Recovery

### RDS

- Automated daily backups (30-day retention)
- Point-in-time recovery enabled
- Automated snapshots before major changes

### Qdrant

- EBS snapshots scheduled
- Data volume separate from root volume

## Disaster Recovery

### Cross-Region

- VPC peering to EU and Brazil regions
- Database replication can be configured
- Multi-region failover architecture

## Cost Optimization

- Spot instances for non-critical workloads
- Autoscaling based on load
- S3 lifecycle policies for logs
- Reserved instances recommended for production

## Maintenance

### Updates

- Automated minor version upgrades enabled
- Maintenance windows configured
- Rolling updates for EKS nodes

### Scaling

- Horizontal Pod Autoscaler configured
- Cluster Autoscaler deployed
- Database autoscaling enabled

## Troubleshooting

### EKS Access Issues

```bash
aws eks update-kubeconfig --region us-east-1 --name synaxis-production
kubectl get nodes
```

### Database Connection

```bash
# Get RDS endpoint
terraform output rds_endpoint

# Test connection (from within VPC)
psql -h <rds-endpoint> -U synaxis_admin -d synaxis
```

### Redis Connection

```bash
# Get Redis endpoint
terraform output redis_endpoint

# Test connection (from within VPC)
redis-cli -h <redis-endpoint> --tls --askpass
```

## Clean Up

To destroy all resources:

```bash
terraform destroy
```

**Warning**: This will delete all resources including databases. Ensure backups are taken before destroying production infrastructure.

## Support

For issues or questions, please contact the infrastructure team or refer to the main Synaxis documentation.
