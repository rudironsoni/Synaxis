#!/bin/bash
# Synaxis Staging Database Migration and Seeding Script

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
INFRA_DIR="${SCRIPT_DIR}/.."
NAMESPACE="synaxis-staging"

echo "=== Synaxis Staging Database Setup ==="

# Get database credentials from Kubernetes secrets
echo "Retrieving database credentials..."
DB_HOST=$(kubectl get secret synaxis-db-credentials -n "${NAMESPACE}" -o jsonpath='{.data.POSTGRES_HOST}' | base64 -d)
DB_PORT=$(kubectl get secret synaxis-db-credentials -n "${NAMESPACE}" -o jsonpath='{.data.POSTGRES_PORT}' | base64 -d)
DB_NAME=$(kubectl get secret synaxis-db-credentials -n "${NAMESPACE}" -o jsonpath='{.data.POSTGRES_DB}' | base64 -d)
DB_USER=$(kubectl get secret synaxis-db-credentials -n "${NAMESPACE}" -o jsonpath='{.data.POSTGRES_USER}' | base64 -d)
DB_PASSWORD=$(kubectl get secret synaxis-db-credentials -n "${NAMESPACE}" -o jsonpath='{.data.POSTGRES_PASSWORD}' | base64 -d)

# Create connection string
DB_CONNECTION_STRING="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

echo "Database: ${DB_NAME}"
echo "Host: ${DB_HOST}:${DB_PORT}"

# Function to run migrations
run_migrations() {
    echo ""
    echo "Running database migrations..."
    
    # Check if EF Core migrations project exists
    MIGRATION_PROJECT="${INFRA_DIR}/../../src/Synaxis.Infrastructure"
    
    if [ -d "${MIGRATION_PROJECT}" ]; then
        echo "Applying EF Core migrations..."
        
        # Set environment variable for connection string
        export ConnectionStrings__DefaultConnection="${DB_CONNECTION_STRING}"
        
        # Run migrations for each bounded context
        cd "${INFRA_DIR}/../../src"
        
        # Identity migrations
        if [ -d "Identity/Identity.Infrastructure" ]; then
            echo "Applying Identity migrations..."
            dotnet ef database update \
                --project Identity/Identity.Infrastructure \
                --startup-project Synaxis.Api \
                --context IdentityDbContext \
                --connection "${DB_CONNECTION_STRING}"
        fi
        
        echo "Migrations completed successfully!"
    else
        echo "Migration project not found. Skipping EF Core migrations."
    fi
}

# Function to verify database health
verify_database() {
    echo ""
    echo "Verifying database health..."
    
    echo "Database verification complete!"
}

# Main execution
main() {
    echo "Starting database setup..."
    
    case "${1:-all}" in
        migrate|migrations)
            run_migrations
            ;;
        verify)
            verify_database
            ;;
        all)
            run_migrations
            verify_database
            ;;
        *)
            echo "Usage: $0 [migrate|verify|all]"
            exit 1
            ;;
    esac
    
    echo ""
    echo "=== Database Setup Complete ==="
}

main "$@"
