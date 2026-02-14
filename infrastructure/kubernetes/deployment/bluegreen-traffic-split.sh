#!/bin/bash
# Blue/Green Traffic Split Script
# Usage: ./bluegreen-traffic-split.sh <blue-percentage>

set -e

BLUE_PERCENTAGE=${1:-100}
GREEN_PERCENTAGE=$((100 - BLUE_PERCENTAGE))

echo "Setting traffic split: Blue=${BLUE_PERCENTAGE}%, Green=${GREEN_PERCENTAGE}%"

# Update VirtualService to adjust traffic weights
kubectl patch virtualservice synaxis-vs -n synaxis --type='json' \
  -p="[
    {
      \"op\": \"replace\",
      \"path\": \"/spec/http/1/route/0/weight\",
      \"value\": ${BLUE_PERCENTAGE}
    },
    {
      \"op\": \"replace\",
      \"path\": \"/spec/http/1/route/1/weight\",
      \"value\": ${GREEN_PERCENTAGE}
    }
  ]"

echo "Traffic split updated successfully"
echo "Current status:"
kubectl get virtualservice synaxis-vs -n synaxis -o jsonpath='{.spec.http[1].route}' | jq
