#!/bin/bash
# <copyright file="purge-cloudfront.sh" company="Synaxis">
# Copyright (c) Synaxis. All rights reserved.
# </copyright>

# purge-cloudfront.sh
# Invalidates CloudFront cache for specified paths.
#
# Usage:
#   ./purge-cloudfront.sh [paths]
#
# Arguments:
#   paths    Space-separated paths to invalidate (default: /*)
#
# Environment Variables:
#   CLOUDFRONT_DISTRIBUTION_ID    CloudFront distribution ID (required)
#   AWS_REGION                    AWS region (default: us-east-1)

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

# Configuration
DISTRIBUTION_ID="${CLOUDFRONT_DISTRIBUTION_ID:-}"
AWS_REGION="${AWS_REGION:-us-east-1}"

# Parse paths (default to /*)
if [[ $# -eq 0 ]]; then
    PATHS="/*"
else
    PATHS=$(printf '"%s" ' "$@")
fi

# Validate configuration
if [[ -z "$DISTRIBUTION_ID" ]]; then
    log_error "CLOUDFRONT_DISTRIBUTION_ID environment variable not set"
    exit 1
fi

# Check prerequisites
if ! command -v aws &> /dev/null; then
    log_error "AWS CLI not found in PATH"
    exit 1
fi

# Verify AWS credentials
if ! aws sts get-caller-identity &> /dev/null; then
    log_error "AWS credentials not configured or invalid"
    exit 1
fi

log_info "Creating CloudFront invalidation..."
log_info "Distribution ID: $DISTRIBUTION_ID"
log_info "Paths: $PATHS"

# Create invalidation
INVALIDATION_RESULT=$(aws cloudfront create-invalidation \
    --distribution-id "$DISTRIBUTION_ID" \
    --paths $PATHS \
    --query 'Invalidation.{Id:Id,Status:Status,CreateTime:CreateTime}' \
    --output json 2>&1) || {
    log_error "Failed to create invalidation: $INVALIDATION_RESULT"
    exit 1
}

INVALIDATION_ID=$(echo "$INVALIDATION_RESULT" | grep -o '"Id": "[^"]*"' | cut -d'"' -f4)

log_info "Invalidation created: $INVALIDATION_ID"
log_info "Waiting for completion..."

# Wait for invalidation to complete
if aws cloudfront wait invalidation-completed \
    --distribution-id "$DISTRIBUTION_ID" \
    --id "$INVALIDATION_ID" 2>/dev/null; then
    log_success "Cache purge completed successfully!"
else
    log_warning "Timeout waiting for invalidation. Check status manually:"
    log_info "aws cloudfront get-invalidation --distribution-id $DISTRIBUTION_ID --id $INVALIDATION_ID"
fi

echo ""
log_info "Invalidation details:"
echo "$INVALIDATION_RESULT"
