#!/bin/bash
# <copyright file="terraform-rollback.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# terraform-rollback.sh
# Rolls back Terraform infrastructure to a previous state.
#
# Usage:
#   ./terraform-rollback.sh <environment> [state_version_id]
#
# Arguments:
#   environment       Target environment (e.g., production, staging)
#   state_version_id  Specific state version to restore (optional)
#
# Environment Variables:
#   TF_DIR            Terraform directory (default: infrastructure/terraform/us)
#   S3_BUCKET         S3 bucket for state storage
#   AWS_REGION        AWS region (default: us-east-1)

set -euo pipefail

# Colors for output
readonly COLOR_INFO='\033[0;36m'
readonly COLOR_SUCCESS='\033[0;32m'
readonly COLOR_WARNING='\033[1;33m'
readonly COLOR_ERROR='\033[0;31m'
readonly COLOR_RESET='\033[0m'

# Logging functions
log_info() {
    echo -e "${COLOR_INFO}[INFO]${COLOR_RESET} $1"
}

log_success() {
    echo -e "${COLOR_SUCCESS}[SUCCESS]${COLOR_RESET} $1"
}

log_warning() {
    echo -e "${COLOR_WARNING}[WARNING]${COLOR_RESET} $1"
}

log_error() {
    echo -e "${COLOR_ERROR}[ERROR]${COLOR_RESET} $1"
}

# Parse arguments
ENVIRONMENT="${1:-}"
TARGET_VERSION="${2:-}"

if [[ -z "$ENVIRONMENT" ]]; then
    log_error "Environment argument required"
    echo "Usage: $0 <environment> [state_version_id]"
    echo "Example: $0 production"
    echo "Example: $0 staging 1234567890abcdef"
    exit 1
fi

# Configuration
TF_DIR="${TF_DIR:-infrastructure/terraform/us}"
S3_BUCKET="${S3_BUCKET:-synaxis-terraform-state}"
AWS_REGION="${AWS_REGION:-us-east-1}"
STATE_KEY="${ENVIRONMENT}/terraform.tfstate"

# Check prerequisites
if ! command -v terraform &> /dev/null; then
    log_error "terraform not found in PATH"
    exit 1
fi

if ! command -v aws &> /dev/null; then
    log_error "aws CLI not found in PATH"
    exit 1
fi

# Verify Terraform directory exists
if [[ ! -d "$TF_DIR" ]]; then
    log_error "Terraform directory not found: $TF_DIR"
    exit 1
fi

cd "$TF_DIR"

log_info "Terraform rollback for environment: $ENVIRONMENT"
log_info "Working directory: $(pwd)"

# Initialize Terraform
log_info "Initializing Terraform..."
terraform init -backend-config="bucket=$S3_BUCKET" \
    -backend-config="key=$STATE_KEY" \
    -backend-config="region=$AWS_REGION" \
    -backend-config="dynamodb_table=synaxis-terraform-locks"

# Select workspace
terraform workspace select "$ENVIRONMENT" 2>/dev/null || {
    log_error "Workspace '$ENVIRONMENT' not found"
    log_info "Available workspaces:"
    terraform workspace list
    exit 1
}

# Show current state
log_info "Current Terraform state summary:"
terraform show -no-color | head -50 || true

# If specific version provided, restore it
if [[ -n "$TARGET_VERSION" ]]; then
    log_info "Restoring state version: $TARGET_VERSION"
    
    # Backup current state
    BACKUP_KEY="${STATE_KEY}.backup-$(date +%Y%m%d-%H%M%S)"
    log_info "Backing up current state to: $BACKUP_KEY"
    
    aws s3 cp "s3://${S3_BUCKET}/${STATE_KEY}" "s3://${S3_BUCKET}/${BACKUP_KEY}" || {
        log_error "Failed to backup current state"
        exit 1
    }
    
    # Download target version
    log_info "Downloading target state version..."
    aws s3api get-object \
        --bucket "$S3_BUCKET" \
        --key "$STATE_KEY" \
        --version-id "$TARGET_VERSION" \
        terraform.tfstate || {
        log_error "Failed to download state version $TARGET_VERSION"
        exit 1
    }
    
    # Re-upload as current
    log_info "Restoring state..."
    aws s3 cp terraform.tfstate "s3://${S3_BUCKET}/${STATE_KEY}"
    
    # Re-initialize to refresh
    terraform init -reconfigure
fi

# Show state versions
log_info "Available state versions:"
aws s3api list-object-versions \
    --bucket "$S3_BUCKET" \
    --prefix "$STATE_KEY" \
    --query 'Versions[*].{VersionId:VersionId,LastModified:LastModified,IsLatest:IsLatest}' \
    --output table | head -20

# Plan rollback
log_info "Planning rollback..."
terraform plan -out=rollback.plan -input=false

log_success "Rollback plan created: rollback.plan"
echo ""
log_info "Review the plan above carefully before applying"
log_info "To apply rollback: terraform apply rollback.plan"
log_info "To cancel: rm rollback.plan"
