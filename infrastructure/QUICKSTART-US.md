# Synaxis US Region - Quick Start Guide

## Prerequisites

- AWS Account with appropriate permissions
- AWS CLI installed and configured
- Terraform >= 1.6.0
- kubectl >= 1.28
- Helm >= 3.11

## Step 1: Deploy Infrastructure with Terraform

```bash
cd infrastructure/terraform/us

# Copy and edit configuration
cp terraform.tfvars.example terraform.tfvars
# Edit terraform.tfvars with your values

# Initialize Terraform
terraform init

# Review plan
terraform plan -out=tfplan

# Apply configuration (takes ~20-30 minutes)
terraform apply tfplan

# Save outputs
terraform output > outputs.txt
```

## Step 2: Configure kubectl for EKS

```bash
# Update kubeconfig
aws eks update-kubeconfig --region us-east-1 --name synaxis-production

# Verify connection
kubectl get nodes
```

## Step 3: Prepare Kubernetes Secrets

```bash
cd ../../kubernetes/us

# Get database credentials from Terraform
DB_ENDPOINT=$(cd ../../terraform/us && terraform output -raw rds_address)
DB_SECRET_ARN=$(cd ../../terraform/us && terraform output -raw rds_secret_arn)

# Get Redis credentials
REDIS_ENDPOINT=$(cd ../../terraform/us && terraform output -raw redis_endpoint)
REDIS_SECRET_ARN=$(cd ../../terraform/us && terraform output -raw redis_auth_token_arn)

# Get Qdrant endpoint
QDRANT_IP=$(cd ../../terraform/us && terraform output -raw qdrant_private_ip)

# Get IAM role ARN for service account
API_ROLE_ARN=$(cd ../../terraform/us && terraform output -raw api_service_account_role_arn)

# Get ALB security group and certificate ARN
ALB_SG=$(cd ../../terraform/us && terraform output -raw alb_security_group_id)
CERT_ARN=$(cd ../../terraform/us && terraform output -raw acm_certificate_arn)
WAF_ARN=$(cd ../../terraform/us && terraform output -raw waf_acl_arn)
```

## Step 4: Update Kubernetes Manifests

### Update secrets.yaml

```bash
# Option 1: Manually edit secrets.yaml
vim secrets.yaml

# Option 2: Use sed to update (example)
sed -i "s|DATABASE_HOST: \"\"|DATABASE_HOST: \"$DB_ENDPOINT\"|" secrets.yaml
sed -i "s|REDIS_HOST: \"\"|REDIS_HOST: \"$REDIS_ENDPOINT\"|" secrets.yaml
sed -i "s|QDRANT_HOST: \"\"|QDRANT_HOST: \"$QDRANT_IP\"|" secrets.yaml
```

### Update deployment.yaml

```bash
# Update ServiceAccount IAM role
sed -i "s|eks.amazonaws.com/role-arn: \"\"|eks.amazonaws.com/role-arn: \"$API_ROLE_ARN\"|" deployment.yaml
```

### Update service.yaml

```bash
# Update Ingress annotations
sed -i "s|alb.ingress.kubernetes.io/certificate-arn: \"\"|alb.ingress.kubernetes.io/certificate-arn: \"$CERT_ARN\"|" service.yaml
sed -i "s|alb.ingress.kubernetes.io/security-groups: \"\"|alb.ingress.kubernetes.io/security-groups: \"$ALB_SG\"|" service.yaml
sed -i "s|alb.ingress.kubernetes.io/wafv2-acl-arn: \"\"|alb.ingress.kubernetes.io/wafv2-acl-arn: \"$WAF_ARN\"|" service.yaml

# Update domain name
sed -i "s|api.synaxis.example.com|api.synaxis.yourdomain.com|" service.yaml
```

## Step 5: Deploy Kubernetes Resources

```bash
# Deploy in order
kubectl apply -f namespace.yaml
kubectl apply -f secrets.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml
kubectl apply -f network-policy.yaml

# Wait for deployment
kubectl rollout status deployment/synaxis-api -n synaxis
```

## Step 6: Verify Deployment

```bash
# Check pods
kubectl get pods -n synaxis

# Check HPA
kubectl get hpa -n synaxis

# Check ingress
kubectl get ingress -n synaxis

# Get ALB DNS name
kubectl get ingress synaxis-api -n synaxis -o jsonpath='{.status.loadBalancer.ingress[0].hostname}'

# Check logs
kubectl logs -n synaxis -l app.kubernetes.io/component=api --tail=50
```

## Step 7: Configure DNS

```bash
# Get ALB DNS name
ALB_DNS=$(kubectl get ingress synaxis-api -n synaxis -o jsonpath='{.status.loadBalancer.ingress[0].hostname}')

echo "Create a CNAME record:"
echo "api.synaxis.yourdomain.com -> $ALB_DNS"
```

## Step 8: Test the API

```bash
# Health check
curl http://$ALB_DNS/health

# If DNS is configured
curl https://api.synaxis.yourdomain.com/health
```

## Optional: Set up External Secrets Operator

```bash
# Install External Secrets Operator
helm repo add external-secrets https://charts.external-secrets.io
helm repo update

helm install external-secrets \
  external-secrets/external-secrets \
  -n external-secrets-system \
  --create-namespace

# Create SecretStore
kubectl apply -f - <<EOF
apiVersion: external-secrets.io/v1beta1
kind: SecretStore
metadata:
  name: aws-secrets
  namespace: synaxis
spec:
  provider:
    aws:
      service: SecretsManager
      region: us-east-1
      auth:
        jwt:
          serviceAccountRef:
            name: synaxis-api
EOF

# Create ExternalSecret for RDS
kubectl apply -f - <<EOF
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: postgres-credentials
  namespace: synaxis
spec:
  refreshInterval: 1h
  secretStoreRef:
    name: aws-secrets
    kind: SecretStore
  target:
    name: postgres-credentials
    creationPolicy: Owner
  data:
    - secretKey: DATABASE_HOST
      remoteRef:
        key: $DB_SECRET_ARN
        property: host
    - secretKey: DATABASE_PASSWORD
      remoteRef:
        key: $DB_SECRET_ARN
        property: password
    - secretKey: DATABASE_USER
      remoteRef:
        key: $DB_SECRET_ARN
        property: username
EOF
```

## Monitoring

```bash
# Install Prometheus (optional)
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

helm install prometheus prometheus-community/kube-prometheus-stack \
  -n monitoring \
  --create-namespace

# Access Grafana
kubectl port-forward -n monitoring svc/prometheus-grafana 3000:80
# Open http://localhost:3000 (admin/prom-operator)
```

## Troubleshooting

### Pods not starting
```bash
kubectl describe pod -n synaxis <pod-name>
kubectl logs -n synaxis <pod-name>
```

### ALB not created
```bash
kubectl logs -n kube-system -l app.kubernetes.io/name=aws-load-balancer-controller
```

### Database connection issues
```bash
# Test from a pod
kubectl run -it --rm debug --image=postgres:16 --restart=Never -n synaxis -- \
  psql -h $DB_ENDPOINT -U synaxis_admin -d synaxis
```

### HPA not scaling
```bash
kubectl describe hpa synaxis-api -n synaxis
kubectl top pods -n synaxis
```

## Cleanup

```bash
# Delete Kubernetes resources
kubectl delete namespace synaxis

# Destroy Terraform infrastructure
cd infrastructure/terraform/us
terraform destroy
```

## Next Steps

1. Configure ACM certificate for your domain
2. Set up DNS records
3. Configure External Secrets Operator
4. Set up monitoring and alerting
5. Configure VPC peering with other regions
6. Implement CI/CD pipeline
7. Perform load testing
8. Configure backup schedules
9. Set up log aggregation
10. Implement disaster recovery procedures

## Support

- Terraform documentation: See `infrastructure/terraform/us/README.md`
- Kubernetes documentation: See `infrastructure/kubernetes/us/README.md`
- AWS documentation: https://docs.aws.amazon.com/

## Security Notes

- Never commit secrets to version control
- Use AWS Secrets Manager or External Secrets Operator
- Rotate credentials regularly
- Enable MFA for AWS accounts
- Review security groups regularly
- Monitor CloudWatch Logs for anomalies
- Keep software up to date
