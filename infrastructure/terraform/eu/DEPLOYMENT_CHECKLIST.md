# Synaxis EU Region Deployment Checklist

## Pre-Deployment

- [ ] AWS account with appropriate permissions
- [ ] AWS CLI configured (`aws configure`)
- [ ] Terraform >= 1.6.0 installed
- [ ] kubectl >= 1.28 installed
- [ ] Docker installed for image building

## Infrastructure Deployment

### 1. Prepare Terraform Configuration

- [ ] Copy `terraform.tfvars.example` to `terraform.tfvars`
- [ ] Update `db_password` with secure password (min 16 chars)
- [ ] Review and adjust instance types/sizes
- [ ] Configure S3 backend (optional, recommended for production)

### 2. Deploy Infrastructure

```bash
cd infrastructure/terraform/eu
terraform init
terraform plan -out=tfplan
terraform apply tfplan
```

- [ ] Terraform apply completed successfully
- [ ] Note RDS endpoint from outputs
- [ ] Note Redis endpoint from outputs
- [ ] Note EKS cluster name from outputs

### 3. Configure kubectl

```bash
aws eks update-kubeconfig --region eu-west-1 --name synaxis-prod
kubectl get nodes  # Verify cluster access
```

- [ ] kubectl configured and can access cluster
- [ ] All nodes are Ready

## Application Deployment

### 4. Prepare Container Images

```bash
# Get ECR login
aws ecr get-login-password --region eu-west-1 | \
  docker login --username AWS --password-stdin <account-id>.dkr.ecr.eu-west-1.amazonaws.com

# Build and push
docker build -t synaxis-api:latest .
docker tag synaxis-api:latest <account-id>.dkr.ecr.eu-west-1.amazonaws.com/synaxis-api:latest
docker push <account-id>.dkr.ecr.eu-west-1.amazonaws.com/synaxis-api:latest
```

- [ ] ECR repository created
- [ ] Image built successfully
- [ ] Image pushed to ECR

### 5. Configure Secrets

Get values from Terraform outputs:
```bash
terraform output rds_endpoint
terraform output redis_endpoint
```

Update `kubernetes/eu/secrets.yaml` with base64-encoded values:
```bash
echo -n "value" | base64
```

- [ ] Database credentials encoded
- [ ] Redis credentials encoded
- [ ] JWT secret generated and encoded
- [ ] Encryption key generated and encoded
- [ ] Provider API keys encoded (if needed)

### 6. Update Kubernetes Manifests

- [ ] Update `deployment.yaml` with ECR image URI
- [ ] Update `secrets.yaml` with actual base64-encoded values
- [ ] Update `service.yaml` with ACM certificate ARN (if using HTTPS)
- [ ] Review resource requests/limits in `deployment.yaml`

### 7. Deploy Kubernetes Resources

```bash
cd infrastructure/kubernetes/eu

# Deploy in order
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml
kubectl apply -f network-policy.yaml
```

- [ ] Namespace created
- [ ] Secrets created
- [ ] Deployment created
- [ ] Service created
- [ ] HPA created
- [ ] Network policies applied

### 8. Verify Deployment

```bash
# Check pods
kubectl get pods -n synaxis-eu
kubectl describe pod <pod-name> -n synaxis-eu

# Check services
kubectl get svc -n synaxis-eu

# Check HPA
kubectl get hpa -n synaxis-eu

# Check logs
kubectl logs -f -n synaxis-eu -l app=synaxis-api
```

- [ ] All pods are Running
- [ ] All pods pass readiness checks
- [ ] Service has LoadBalancer IP/hostname
- [ ] HPA shows current metrics
- [ ] No errors in logs

## Post-Deployment

### 9. Configure DNS

- [ ] Get ALB DNS name: `kubectl get svc synaxis-api -n synaxis-eu`
- [ ] Create CNAME record pointing to ALB DNS
- [ ] Wait for DNS propagation

### 10. Test Application

```bash
# Test health endpoint
curl https://your-domain.com/health/readiness

# Test API
curl -X POST https://your-domain.com/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "deepseek-chat",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

- [ ] Health check returns 200 OK
- [ ] API responds correctly
- [ ] Streaming works (if enabled)

### 11. Configure Monitoring

- [ ] Set up CloudWatch dashboards
- [ ] Configure SNS topics for alarms
- [ ] Test alarm notifications
- [ ] Enable Container Insights (optional)
- [ ] Set up Prometheus/Grafana (optional)

### 12. Security Hardening

- [ ] Review security group rules
- [ ] Enable WAF (if not already)
- [ ] Configure rate limiting
- [ ] Review IAM policies
- [ ] Enable CloudTrail (if not already)
- [ ] Enable GuardDuty (recommended)

### 13. Backup Verification

- [ ] Verify RDS automated backups are enabled
- [ ] Verify Redis snapshots are being created
- [ ] Test RDS point-in-time recovery
- [ ] Document backup restoration procedures

## Maintenance

### Regular Tasks

- [ ] Monitor CloudWatch alarms
- [ ] Review CloudWatch logs weekly
- [ ] Check for security updates
- [ ] Review cost optimization opportunities
- [ ] Test disaster recovery procedures quarterly

### Scaling

- [ ] Monitor HPA behavior
- [ ] Adjust resource requests/limits as needed
- [ ] Review EKS node group sizing
- [ ] Consider RDS read replicas if needed
- [ ] Monitor Redis memory usage

## Rollback Plan

If deployment fails:

1. **Kubernetes rollback**:
   ```bash
   kubectl rollout undo deployment/synaxis-api -n synaxis-eu
   ```

2. **Infrastructure rollback**:
   ```bash
   cd infrastructure/terraform/eu
   git checkout <previous-commit>
   terraform apply
   ```

3. **Database rollback**:
   - Restore from automated backup
   - Use point-in-time recovery

## Troubleshooting

### Pods Not Starting

```bash
kubectl describe pod <pod-name> -n synaxis-eu
kubectl logs <pod-name> -n synaxis-eu
```

Common issues:
- Image pull errors (check ECR permissions)
- Secret not found (verify secrets.yaml applied)
- Resource limits too low
- Health checks failing

### Database Connection Issues

- [ ] Verify security group allows traffic from EKS nodes
- [ ] Check connection string in secrets
- [ ] Test connectivity from pod: `kubectl run -it --rm debug --image=postgres:16 --restart=Never -- psql -h <endpoint> -U <user>`

### Service Not Accessible

- [ ] Check ALB creation: `kubectl describe svc synaxis-api -n synaxis-eu`
- [ ] Verify security group allows inbound traffic
- [ ] Check target group health in AWS Console
- [ ] Verify DNS records

## Support

- Documentation: `/infrastructure/terraform/eu/README.md`
- GitHub Issues: https://github.com/rudironsoni/Synaxis/issues
- AWS Support: https://console.aws.amazon.com/support/

## Compliance Verification

- [ ] All data stays in eu-west-1
- [ ] Encryption at rest enabled (RDS, Redis, EBS)
- [ ] Encryption in transit enabled (TLS 1.3)
- [ ] Audit logging enabled (CloudWatch, VPC Flow Logs)
- [ ] 90-day log retention configured
- [ ] Multi-AZ redundancy for all critical services
- [ ] Automated backups enabled (30 days retention)
- [ ] Network isolation via security groups and NACLs
- [ ] GDPR compliance requirements met
