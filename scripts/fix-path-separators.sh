#!/usr/bin/env bash
#
# Fix Path Separators in MSBuild Files
# Automatically replaces backslashes with forward slashes in .csproj, .props, .targets, .sln files
#
# Usage:
#   ./scripts/fix-path-separators.sh              # Fix all files in git working tree
#   ./scripts/fix-path-separators.sh --check    # Check only (exit 1 if issues found)
#   ./scripts/fix-path-separators.sh <file>       # Fix specific file

set -euo pipefail

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# File extensions to check
FILE_EXTENSIONS="csproj props targets sln"

# Files to exclude (these have legitimate backslash usage)
EXCLUDED_FILES="Directory.Build.targets"

# Function to fix a single file
fix_file() {
    local file="$1"
    local fixed=0
    
    if [[ ! -f "$file" ]]; then
        return 0
    fi
    
    # Read file content
    local content
    content=$(cat "$file")
    
    # Replace backslashes with forward slashes
    local new_content
    new_content=$(echo "$content" | sed 's/\\\\/\//g')
    
    if [[ "$content" != "$new_content" ]]; then
        echo "$new_content" > "$file"
        echo -e "${GREEN}Fixed:${NC} $file"
        return 1
    fi
    
    return 0
}

# Function to check if any files need fixing
check_files() {
    local found_issues=0
    
    for ext in $FILE_EXTENSIONS; do
        while IFS= read -r -d '' file; do
            # Skip excluded files
            local basename=$(basename "$file")
            if [[ "$EXCLUDED_FILES" == *"$basename"* ]]; then
                continue
            fi
            
            if grep -q '\\' "$file" 2>/dev/null; then
                echo -e "${RED}Found backslash:${NC} $file"
                found_issues=1
            fi
        done < <(find . -type f -name "*.$ext" -not -path "./.git/*" -not -path "./node_modules/*" -not -path "./apps/studio-mobile/node_modules/*" -print0)
    done
    
    return $found_issues
}

# Function to fix all files
fix_all_files() {
    local fixed_count=0
    
    for ext in $FILE_EXTENSIONS; do
        while IFS= read -r -d '' file; do
            # Skip excluded files
            local basename=$(basename "$file")
            if [[ "$EXCLUDED_FILES" == *"$basename"* ]]; then
                continue
            fi
            
            if fix_file "$file"; then
                : # No changes needed
            else
                fixed_count=$((fixed_count + 1))
            fi
        done < <(find . -type f -name "*.$ext" -not -path "./.git/*" -not -path "./node_modules/*" -not -path "./apps/studio-mobile/node_modules/*" -print0)
    done
    
    if [[ $fixed_count -eq 0 ]]; then
        echo -e "${GREEN}✓ All MSBuild files already use forward slashes${NC}"
    else
        echo -e "${GREEN}✓ Fixed $fixed_count file(s)${NC}"
    fi
    
    return 0
}

# Function to fix all files
fix_all_files() {
    local fixed_count=0
    
    for ext in $FILE_EXTENSIONS; do
        while IFS= read -r -d '' file; do
            if fix_file "$file"; then
                : # No changes needed
            else
                fixed_count=$((fixed_count + 1))
            fi
        done < <(find . -type f -name "*.$ext" -not -path "./.git/*" -not -path "./node_modules/*" -not -path "./apps/studio-mobile/node_modules/*" -print0)
    done
    
    if [[ $fixed_count -eq 0 ]]; then
        echo -e "${GREEN}✓ All MSBuild files already use forward slashes${NC}"
    else
        echo -e "${GREEN}✓ Fixed $fixed_count file(s)${NC}"
    fi
    
    return 0
}

# Main logic
case "${1:-}" in
    --check)
        echo "Checking for backslashes in MSBuild files..."
        if check_files; then
            echo -e "${GREEN}✓ All MSBuild files use forward slashes${NC}"
            exit 0
        else
            echo -e "${RED}✗ Backslashes found! Run without --check to autofix.${NC}"
            exit 1
        fi
        ;;
    --help|-h)
        echo "Fix Path Separators in MSBuild Files"
        echo ""
        echo "Usage:"
        echo "  $0              Fix all MSBuild files"
        echo "  $0 --check      Check only (exit 1 if backslashes found)"
        echo "  $0 <file>       Fix specific file"
        echo "  $0 --help       Show this help message"
        exit 0
        ;;
    "")
        # Fix all files
        echo "Checking and fixing MSBuild files..."
        fix_all_files
        ;;
    *)
        # Fix specific file
        if [[ -f "$1" ]]; then
            if fix_file "$1"; then
                echo -e "${GREEN}✓ No changes needed:${NC} $1"
            fi
        else
            echo -e "${RED}Error: File not found: $1${NC}"
            exit 1
        fi
        ;;
esac
