#!/bin/bash
# Synaxis Staging Environment Deployment Script
# This script deploys the complete staging environment

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="${SCRIPT_DIR}/.."
TERRAFORM_DIR="${INFRA_DIR}/terraform"
HELM_DIR="${INFRA_DIR}/helm/synaxis-staging"
NAMESPACE="synaxis-staging"
ENVIRONMENT="staging"

# Logging functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    command -v aws >/dev/null 2>&1 || { log_error "AWS CLI is required but not installed."; exit 1; }
    command -v kubectl >/dev/null 2>&1 || { log_error "kubectl is required but not installed."; exit 1; }
    command -v helm >/dev/null 2>&1 || { log_error "Helm is required but not installed."; exit 1; }
    command -v terraform >/dev/null 2>&1 || { log_error "Terraform is required but not installed."; exit 1; }
    
    # Check AWS authentication
    if ! aws sts get-caller-identity >/dev/null 2>&1; then
        log_error "AWS authentication failed. Please configure AWS credentials."
        exit 1
    fi
    
    log_info "Prerequisites check passed!"
}

# Deploy infrastructure with Terraform
deploy_infrastructure() {
    log_info "Deploying infrastructure with Terraform..."
    
    cd "${TERRAFORM_DIR}"
    
    log_info "Initializing Terraform..."
    terraform init
    
    log_info "Planning Terraform changes..."
    terraform plan -out=tfplan
    
    log_info "Applying Terraform changes..."
    terraform apply tfplan
    
    log_info "Infrastructure deployment complete!"
}

# Configure kubectl for EKS cluster
configure_kubectl() {
    log_info "Configuring kubectl for EKS cluster..."
    
    cd "${TERRAFORM_DIR}"
    CLUSTER_NAME=$(terraform output -raw cluster_name)
    AWS_REGION=$(terraform output -raw region 2>/dev/null || echo "us-east-1")
    
    aws eks update-kubeconfig --region "${AWS_REGION}" --name "${CLUSTER_NAME}"
    
    log_info "kubectl configured for cluster: ${CLUSTER_NAME}"
}

# Install Istio service mesh
install_istio() {
    log_info "Installing Istio service mesh..."
    
    # Add Istio Helm repository
    helm repo add istio https://istio-release.storage.googleapis.com/charts
    helm repo update
    
    # Install Istio base
    helm upgrade --install istio-base istio/base \
        -n istio-system \
        --create-namespace \
        --set defaultRevision=default \
        --wait
    
    # Install Istiod
    helm upgrade --install istiod istio/istiod \
        -n istio-system \
        --set meshConfig.defaultConfig.tracing.sampling=100.0 \
        --wait
    
    # Install Istio ingress gateway
    helm upgrade --install istio-ingressgateway istio/gateway \
        -n istio-system \
        --wait
    
    log_info "Istio installation complete!"
}

# Deploy Synaxis application
deploy_application() {
    log_info "Deploying Synaxis application..."
    
    cd "${HELM_DIR}"
    
    # Update Helm dependencies
    helm dependency update
    
    # Deploy the application
    helm upgrade --install synaxis-staging . \
        -n "${NAMESPACE}" \
        --create-namespace \
        --values values.yaml \
        --set aws.accountId=$(aws sts get-caller-identity --query Account --output text) \
        --wait \
        --timeout 600s
    
    log_info "Application deployment complete!"
}

# Wait for all pods to be ready
wait_for_pods() {
    log_info "Waiting for all pods to be ready..."
    
    kubectl wait --for=condition=ready pod \
        --all \
        -n "${NAMESPACE}" \
        --timeout=300s
    
    log_info "All pods are ready!"
}

# Run health checks
run_health_checks() {
    log_info "Running health checks..."
    
    # Get gateway endpoint
    GATEWAY_URL=$(kubectl get svc synaxis-gateway-ingress -n "${NAMESPACE}" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || echo "")
    
    if [ -z "$GATEWAY_URL" ]; then
        log_warn "Gateway URL not available yet. Skipping external health checks."
        return 0
    fi
    
    log_info "Gateway URL: ${GATEWAY_URL}"
    
    # Wait for endpoint to be available
    sleep 30
    
    # Check health endpoints
    HEALTH_ENDPOINTS=(
        "http://${GATEWAY_URL}/health/liveness"
        "http://${GATEWAY_URL}/health/readiness"
    )
    
    for endpoint in "${HEALTH_ENDPOINTS[@]}"; do
        log_info "Checking: ${endpoint}"
        if curl -sf "${endpoint}" > /dev/null 2>&1; then
            log_info "✓ ${endpoint} is healthy"
        else
            log_error "✗ ${endpoint} is not responding"
            return 1
        fi
    done
    
    log_info "All health checks passed!"
}

# Print deployment summary
print_summary() {
    log_info "=========================================="
    log_info "Synaxis Staging Deployment Complete!"
    log_info "=========================================="
    
    cd "${TERRAFORM_DIR}"
    
    echo ""
    echo "Cluster Endpoint: $(terraform output -raw cluster_endpoint 2>/dev/null || echo 'N/A')"
    echo "Database Endpoint: $(terraform output -raw db_endpoint 2>/dev/null || echo 'N/A')"
    echo "Redis Endpoint: $(terraform output -raw redis_endpoint 2>/dev/null || echo 'N/A')"
    echo "S3 Bucket: $(terraform output -raw s3_bucket_name 2>/dev/null || echo 'N/A')"
    echo ""
    
    # Get gateway info
    kubectl get svc synaxis-gateway-ingress -n "${NAMESPACE}" 2>/dev/null || true
    
    echo ""
    log_info "Access Grafana: kubectl port-forward svc/synaxis-staging-grafana 3000:3000 -n ${NAMESPACE}"
    log_info "Access Prometheus: kubectl port-forward svc/synaxis-staging-prometheus-server 9090:9090 -n ${NAMESPACE}"
    log_info ""
    log_info "To view logs: kubectl logs -f deployment/<service-name> -n ${NAMESPACE}"
}

# Run smoke tests
run_smoke_tests() {
    log_info "Running smoke tests..."
    
    # Get gateway URL
    GATEWAY_URL=$(kubectl get svc synaxis-gateway-ingress -n "${NAMESPACE}" -o jsonpath='{.status.loadBalancer.ingress[0].hostname}' 2>/dev/null || echo "")
    
    if [ -z "$GATEWAY_URL" ]; then
        log_warn "Cannot run smoke tests - gateway URL not available"
        return 0
    fi
    
    # Wait for services to be fully ready
    sleep 60
    
    # Test each service endpoint
    SERVICES=("identity" "inference" "billing" "agents" "orchestration")
    
    for service in "${SERVICES[@]}"; do
        endpoint="http://${GATEWAY_URL}/api/v1/${service}/health"
        log_info "Testing ${service}..."
        
        if curl -sf "${endpoint}" > /dev/null 2>&1; then
            log_info "✓ ${service} is responding"
        else
            log_warn "✗ ${service} is not responding (may still be starting)"
        fi
    done
    
    log_info "Smoke tests complete!"
}

# Main execution
main() {
    log_info "Starting Synaxis Staging Environment Deployment"
    
    case "${1:-all}" in
        infrastructure|infra)
            check_prerequisites
            deploy_infrastructure
            ;;
        kubernetes|k8s)
            check_prerequisites
            configure_kubectl
            install_istio
            deploy_application
            wait_for_pods
            ;;
        health-check|health)
            run_health_checks
            ;;
        smoke-tests|smoke)
            run_smoke_tests
            ;;
        all)
            check_prerequisites
            deploy_infrastructure
            configure_kubectl
            install_istio
            deploy_application
            wait_for_pods
            run_health_checks
            run_smoke_tests
            print_summary
            ;;
        *)
            echo "Usage: $0 [infrastructure|kubernetes|health-check|smoke-tests|all]"
            exit 1
            ;;
    esac
}

main "$@"
