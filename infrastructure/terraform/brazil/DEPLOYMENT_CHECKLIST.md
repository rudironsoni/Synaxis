# Brazil Region Deployment Checklist

## Pre-Deployment

### AWS Setup
- [ ] AWS Account configured with proper credentials
- [ ] S3 bucket created: `synaxis-terraform-state`
- [ ] DynamoDB table created: `synaxis-terraform-locks`
- [ ] IAM permissions verified for Terraform
- [ ] Route53 hosted zone configured for `.com.br` domain
- [ ] ACM certificate requested/imported for `*.synaxis.com.br`

### Tools
- [ ] Terraform >= 1.6.0 installed
- [ ] AWS CLI v2 installed and configured
- [ ] kubectl >= 1.28 installed
- [ ] helm >= 3.11 installed

### Configuration
- [ ] Update `variables.tf` with environment-specific values
- [ ] Review VPC CIDR blocks (avoid conflicts with US/EU)
- [ ] Configure VPC peering variables if connecting regions
- [ ] Update ALB certificate ARN in `ingress.yaml`
- [ ] Update container image in `deployment.yaml`

## Terraform Deployment

### Initialize
```bash
cd infrastructure/terraform/brazil
terraform init
```

### Plan
```bash
terraform plan -out=tfplan
```
**Review:**
- [ ] VPC and subnet configuration
- [ ] RDS instance class and storage
- [ ] ElastiCache node type and count
- [ ] EKS cluster version and node configuration
- [ ] Security group rules
- [ ] KMS key configuration
- [ ] Estimated costs

### Apply
```bash
terraform apply tfplan
```
**Duration:** ~30-45 minutes

### Verify
```bash
terraform output
```
**Save these outputs:**
- [ ] `vpc_id`
- [ ] `eks_cluster_name`
- [ ] `rds_endpoint`
- [ ] `redis_endpoint`
- [ ] `qdrant_private_ip`
- [ ] `alb_dns_name`
- [ ] `db_password_secret_arn`

## EKS Configuration

### Connect to Cluster
```bash
aws eks update-kubeconfig \
  --region sa-east-1 \
  --name synaxis-brazil-production
```

### Verify Connection
```bash
kubectl get nodes
kubectl get ns
```
**Expected:** 3+ nodes in Ready state

### Install AWS Load Balancer Controller
```bash
helm repo add eks https://aws.github.io/eks-charts
helm repo update

helm install aws-load-balancer-controller eks/aws-load-balancer-controller \
  -n kube-system \
  --set clusterName=synaxis-brazil-production \
  --set serviceAccount.create=false \
  --set serviceAccount.name=aws-load-balancer-controller
```

## Kubernetes Deployment

### 1. Create Namespace
```bash
kubectl apply -f infrastructure/kubernetes/brazil/namespace.yaml
```
- [ ] Namespace created: `synaxis`

### 2. Update and Apply Secrets
```bash
# Get values from Terraform
DB_HOST=$(terraform output -raw rds_address)
DB_PASSWORD=$(aws secretsmanager get-secret-value \
  --secret-id $(terraform output -raw db_password_secret_arn) \
  --query SecretString --output text)
REDIS_HOST=$(terraform output -raw redis_endpoint)
REDIS_AUTH=$(aws secretsmanager get-secret-value \
  --secret-id synaxis-brazil-redis-auth-token \
  --query SecretString --output text)
QDRANT_HOST=$(terraform output -raw qdrant_private_ip)

# Update secrets.yaml with actual values
# Then apply
kubectl apply -f infrastructure/kubernetes/brazil/secrets.yaml
```
- [ ] Database credentials configured
- [ ] Redis credentials configured
- [ ] Qdrant credentials configured

### 3. Deploy Application
```bash
kubectl apply -f infrastructure/kubernetes/brazil/deployment.yaml
```
- [ ] Wait for pods to be Running: `kubectl get pods -n synaxis -w`

### 4. Create Service
```bash
kubectl apply -f infrastructure/kubernetes/brazil/service.yaml
```
- [ ] Service created: `kubectl get svc -n synaxis`

### 5. Configure HPA
```bash
kubectl apply -f infrastructure/kubernetes/brazil/hpa.yaml
```
- [ ] HPA active: `kubectl get hpa -n synaxis`

### 6. Apply Network Policies
```bash
kubectl apply -f infrastructure/kubernetes/brazil/network-policy.yaml
```
- [ ] Network policies applied: `kubectl get networkpolicies -n synaxis`

### 7. Configure Ingress
**Update ingress.yaml with:**
- ACM certificate ARN
- Subnet IDs
- WAF ACL ARN (if applicable)

```bash
kubectl apply -f infrastructure/kubernetes/brazil/ingress.yaml
```
- [ ] Ingress created: `kubectl get ingress -n synaxis`
- [ ] ALB provisioning (wait 2-5 minutes)
- [ ] Get ALB DNS: `kubectl get ingress synaxis-api-ingress -n synaxis -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'`

### 8. Apply Resource Limits
```bash
kubectl apply -f infrastructure/kubernetes/brazil/resource-limits.yaml
```
- [ ] PodDisruptionBudget created
- [ ] LimitRange configured
- [ ] ResourceQuota set

### 9. Enable Monitoring
```bash
kubectl apply -f infrastructure/kubernetes/brazil/monitoring.yaml
```
- [ ] ServiceMonitor created (if Prometheus Operator installed)
- [ ] Grafana dashboard configmap created

## Post-Deployment Verification

### Health Checks
```bash
# Check pod status
kubectl get pods -n synaxis

# Check logs
kubectl logs -f deployment/synaxis-api -n synaxis

# Check HPA
kubectl get hpa -n synaxis

# Check resource usage
kubectl top pods -n synaxis
kubectl top nodes
```

### Application Testing
```bash
# Get ALB DNS name
ALB_DNS=$(kubectl get ingress synaxis-api-ingress -n synaxis -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')

# Test health endpoint
curl -k https://$ALB_DNS/health

# Test API endpoint
curl -k https://$ALB_DNS/api/v1/inference
```

### Database Connectivity
```bash
# Connect to a pod
kubectl exec -it deployment/synaxis-api -n synaxis -- /bin/sh

# Test database connection (from pod)
psql -h $DB_HOST -U synaxis_admin -d synaxis -c "SELECT 1;"

# Test Redis connection (from pod)
redis-cli -h $REDIS_HOST -p 6379 --tls PING
```

### Monitoring
```bash
# CloudWatch Logs
aws logs tail /aws/synaxis/sa-east-1 --follow

# EKS Cluster Logs
aws logs tail /aws/eks/synaxis-brazil-production/cluster --follow

# RDS Metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/RDS \
  --metric-name CPUUtilization \
  --dimensions Name=DBInstanceIdentifier,Value=synaxis-brazil-production \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average
```

## DNS Configuration

### Route53 Setup
```bash
# Get ALB Zone ID and DNS name
ALB_ZONE_ID=$(terraform output -raw alb_zone_id)
ALB_DNS=$(terraform output -raw alb_dns_name)

# Create A record (alias)
aws route53 change-resource-record-sets \
  --hosted-zone-id YOUR_HOSTED_ZONE_ID \
  --change-batch '{
    "Changes": [{
      "Action": "CREATE",
      "ResourceRecordSet": {
        "Name": "api.synaxis.com.br",
        "Type": "A",
        "AliasTarget": {
          "HostedZoneId": "'$ALB_ZONE_ID'",
          "DNSName": "'$ALB_DNS'",
          "EvaluateTargetHealth": true
        }
      }
    }]
  }'
```
- [ ] DNS record created: `api.synaxis.com.br`
- [ ] DNS propagation verified: `dig api.synaxis.com.br`
- [ ] HTTPS working: `curl https://api.synaxis.com.br/health`

## VPC Peering (Optional)

### Connect to US Region
```bash
# Get US VPC ID
US_VPC_ID=$(aws ec2 describe-vpcs --region us-east-1 --filters "Name=tag:Name,Values=synaxis-us-vpc-production" --query 'Vpcs[0].VpcId' --output text)

# Update Terraform variables
export TF_VAR_us_vpc_id=$US_VPC_ID
export TF_VAR_us_vpc_cidr="10.0.0.0/16"

# Apply changes
terraform apply -auto-approve
```
- [ ] VPC peering created
- [ ] Routes configured
- [ ] Security groups updated
- [ ] Connectivity tested

### Connect to EU Region
```bash
# Get EU VPC ID
EU_VPC_ID=$(aws ec2 describe-vpcs --region eu-west-1 --filters "Name=tag:Name,Values=synaxis-eu-vpc-production" --query 'Vpcs[0].VpcId' --output text)

# Update Terraform variables
export TF_VAR_eu_vpc_id=$EU_VPC_ID
export TF_VAR_eu_vpc_cidr="10.1.0.0/16"

# Apply changes
terraform apply -auto-approve
```
- [ ] VPC peering created
- [ ] Routes configured
- [ ] Security groups updated
- [ ] Connectivity tested

## Security Hardening

### Enable GuardDuty
```bash
aws guardduty create-detector --enable --region sa-east-1
```
- [ ] GuardDuty enabled

### Enable Security Hub
```bash
aws securityhub enable-security-hub --region sa-east-1
```
- [ ] Security Hub enabled

### Enable AWS Config
```bash
aws configservice put-configuration-recorder \
  --configuration-recorder name=synaxis-brazil,roleARN=arn:aws:iam::ACCOUNT_ID:role/aws-config-role \
  --recording-group allSupported=true,includeGlobalResourceTypes=true

aws configservice start-configuration-recorder --configuration-recorder-name synaxis-brazil
```
- [ ] AWS Config enabled

### Enable CloudTrail
- [ ] CloudTrail logging enabled for all regions
- [ ] Logs encrypted with KMS
- [ ] Log file validation enabled

## Backup Configuration

### RDS Automated Backups
- [ ] Backup retention: 30 days (configured)
- [ ] Backup window: 03:00-04:00 UTC (configured)
- [ ] Point-in-time recovery enabled

### Manual Snapshot
```bash
aws rds create-db-snapshot \
  --db-instance-identifier synaxis-brazil-production \
  --db-snapshot-identifier synaxis-brazil-manual-$(date +%Y%m%d) \
  --region sa-east-1
```
- [ ] Manual snapshot created

### Cross-Region Backup (Optional)
```bash
aws rds copy-db-snapshot \
  --source-db-snapshot-identifier arn:aws:rds:sa-east-1:ACCOUNT_ID:snapshot:SNAPSHOT_NAME \
  --target-db-snapshot-identifier synaxis-brazil-backup-$(date +%Y%m%d) \
  --source-region sa-east-1 \
  --region us-east-1
```
- [ ] Cross-region backup configured

## Monitoring Setup

### CloudWatch Alarms
- [ ] RDS CPU alarm configured
- [ ] RDS storage alarm configured
- [ ] Redis CPU alarm configured
- [ ] Redis memory alarm configured
- [ ] EKS node CPU alarm
- [ ] EKS node memory alarm
- [ ] ALB 5xx errors alarm

### SNS Topics
```bash
aws sns create-topic --name synaxis-brazil-alerts --region sa-east-1
aws sns subscribe \
  --topic-arn arn:aws:sns:sa-east-1:ACCOUNT_ID:synaxis-brazil-alerts \
  --protocol email \
  --notification-endpoint ops@synaxis.com
```
- [ ] SNS topic created
- [ ] Email subscriptions confirmed

## Documentation

- [ ] Update runbook with Brazil-specific procedures
- [ ] Document all resource ARNs and IDs
- [ ] Create incident response plan for Brazil region
- [ ] Document LGPD compliance procedures
- [ ] Create disaster recovery plan
- [ ] Document scaling procedures

## Sign-off

### Infrastructure Team
- [ ] Terraform deployment verified
- [ ] All resources created successfully
- [ ] Cost estimation reviewed and approved

### Platform Team
- [ ] Kubernetes deployment verified
- [ ] All pods running successfully
- [ ] HPA and scaling tested

### Security Team
- [ ] Security groups reviewed
- [ ] Encryption verified
- [ ] Network policies tested
- [ ] LGPD compliance verified

### Operations Team
- [ ] Monitoring configured
- [ ] Alerts set up
- [ ] Runbook updated
- [ ] On-call rotation updated

---

**Deployment Date:** _________________
**Deployed By:** _________________
**Reviewed By:** _________________
**Approved By:** _________________
