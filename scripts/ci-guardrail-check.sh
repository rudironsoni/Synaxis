#!/bin/bash
# CI Guardrail: Prevent container builders in test classes (except fixtures)
# This script checks for direct container builder usage in test files

set -e

echo "Checking for container builders in test classes..."

# Find all test files except fixture files
TEST_FILES=$(find tests -name "*.cs" -type f ! -path "*/Fixtures/*")

# Patterns to check for
PATTERNS=(
    "new PostgreSqlBuilder"
    "new RedisBuilder"
    "new QdrantBuilder"
)

VIOLATIONS=0

for pattern in "${PATTERNS[@]}"; do
    echo "Checking for pattern: $pattern"
    
    # Search for the pattern in test files
    MATCHES=$(grep -r "$pattern" $TEST_FILES 2>/dev/null || true)
    
    if [ -n "$MATCHES" ]; then
        echo "❌ Found $pattern in test files:"
        echo "$MATCHES"
        VIOLATIONS=$((VIOLATIONS + 1))
    else
        echo "✓ No $pattern found in test files"
    fi
done

if [ $VIOLATIONS -gt 0 ]; then
    echo ""
    echo "❌ CI Guardrail Failed: Found $VIOLATIONS violation(s)"
    echo ""
    echo "Container builders should only be used in fixture files (tests/Common/Fixtures/*.cs)"
    echo "Test classes should use shared fixtures via constructor injection."
    echo ""
    echo "Example of correct usage:"
    echo "  [Collection(\"PostgresIntegration\")]"
    echo "  public class MyTests : IAsyncLifetime"
    echo "  {"
    echo "      private readonly PostgresFixture _postgresFixture;"
    echo ""
    echo "      public MyTests(PostgresFixture postgresFixture)"
    echo "      {"
    echo "          _postgresFixture = postgresFixture;"
    echo "      }"
    echo "  }"
    exit 1
else
    echo ""
    echo "✓ CI Guardrail Passed: No container builders found in test classes"
    exit 0
fi
