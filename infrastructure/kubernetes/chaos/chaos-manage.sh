#!/bin/bash
# Chaos Experiment Management Script
# Usage: ./chaos-manage.sh [start|stop|status] [experiment-name]

set -e

ACTION=${1:-status}
EXPERIMENT=${2:-all}
NAMESPACE="synaxis"

case $ACTION in
  start)
    if [ "$EXPERIMENT" = "all" ]; then
      echo "Starting all chaos experiments..."
      kubectl apply -f infrastructure/kubernetes/chaos/
    else
      echo "Starting experiment: $EXPERIMENT"
      kubectl apply -f "infrastructure/kubernetes/chaos/chaos-${EXPERIMENT}.yaml"
    fi
    ;;

  stop)
    if [ "$EXPERIMENT" = "all" ]; then
      echo "Stopping all chaos experiments..."
      kubectl delete -f infrastructure/kubernetes/chaos/ --ignore-not-found=true
    else
      echo "Stopping experiment: $EXPERIMENT"
      kubectl delete -f "infrastructure/kubernetes/chaos/chaos-${EXPERIMENT}.yaml" --ignore-not-found=true
    fi
    ;;

  status)
    echo "Chaos experiments status:"
    echo "=========================="
    echo ""
    echo "Pod Chaos:"
    kubectl get podchaos -n $NAMESPACE 2>/dev/null || echo "  No pod chaos experiments running"
    echo ""
    echo "Network Chaos:"
    kubectl get networkchaos -n $NAMESPACE 2>/dev/null || echo "  No network chaos experiments running"
    echo ""
    echo "IO Chaos:"
    kubectl get iochaos -n $NAMESPACE 2>/dev/null || echo "  No IO chaos experiments running"
    echo ""
    echo "Stress Chaos:"
    kubectl get stresschaos -n $NAMESPACE 2>/dev/null || echo "  No stress chaos experiments running"
    echo ""
    echo "HTTP Chaos:"
    kubectl get httpchaos -n $NAMESPACE 2>/dev/null || echo "  No HTTP chaos experiments running"
    echo ""
    echo "DNS Chaos:"
    kubectl get dnschaos -n $NAMESPACE 2>/dev/null || echo "  No DNS chaos experiments running"
    echo ""
    echo "Workflows:"
    kubectl get workflow -n $NAMESPACE 2>/dev/null || echo "  No workflows running"
    ;;

  *)
    echo "Usage: $0 [start|stop|status] [experiment-name]"
    echo ""
    echo "Commands:"
    echo "  start [experiment]  - Start chaos experiment (or all if not specified)"
    echo "  stop [experiment]   - Stop chaos experiment (or all if not specified)"
    echo "  status              - Show status of all chaos experiments"
    echo ""
    echo "Available experiments:"
    echo "  pod-failure, pod-kill, network-delay, network-loss, io-delay"
    echo "  cpu-stress, memory-stress, http-abort, dns-failure"
    exit 1
    ;;
esac
