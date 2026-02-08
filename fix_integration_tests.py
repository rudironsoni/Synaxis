#!/usr/bin/env python3
"""
Fix IntegrationTests build errors
"""
import os
import re
from pathlib import Path

def fix_file(filepath):
    """Fix common errors in a C# test file"""
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original = content
    
    # Fix AccountInfo property: Email -> email
    content = re.sub(r'\.Email\b', '.email', content)
    
    # Fix AuditLog properties
    content = re.sub(r'\.TenantId\b', '.OrganizationId', content)
    content = re.sub(r'\.PayloadJson\b', '.NewValues', content)
    
    # Fix DateTime.Offset -> use DateTimeOffset
    content = re.sub(r'\.Offset\b', '.Offset', content)  # This is actually OK, just checking
    
    # Add ConfigureAwait(false) to await calls that don't have it
    # Match: await xxx) but not await xxx).ConfigureAwait
    content = re.sub(
        r'(await\s+[^;]+?\))(\s*;)',
        lambda m: m.group(1) + '.ConfigureAwait(false)' + m.group(2) if '.ConfigureAwait' not in m.group(0) else m.group(0),
        content
    )
    
    # Add StringComparison.Ordinal to Assert.Equal string comparisons
    # Match: Assert.Equal("string", variable) without StringComparison
    content = re.sub(
        r'(Assert\.Equal\([^,]+,\s*[^,)]+)(\))',
        lambda m: m.group(1) + ', StringComparison.Ordinal' + m.group(2) if 'StringComparison' not in m.group(0) and '"' in m.group(1) else m.group(0),
        content
    )
    
    # Fix using directives order - System.* should come before others
    using_pattern = r'(using\s+[^;]+;)'
    usings = re.findall(using_pattern, content[:2000])  # Check first 2000 chars
    
    if usings and len(usings) > 2:
        # Extract and sort usings
        system_usings = sorted([u for u in usings if 'System' in u])
        other_usings = sorted([u for u in usings if 'System' not in u and u])
        
        if system_usings and other_usings:
            # Replace all usings with sorted version
            all_usings_str = '\n'.join(usings)
            sorted_usings_str = '\n'.join(system_usings + other_usings)
            content = content.replace(all_usings_str, sorted_usings_str, 1)
    
    # Remove trailing whitespace
    lines = content.split('\n')
    lines = [line.rstrip() for line in lines]
    content = '\n'.join(lines)
    
    # Save if changed
    if content != original:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        return True
    return False

def main():
    test_dir = Path('tests/InferenceGateway/IntegrationTests')
    
    fixed_count = 0
    for cs_file in test_dir.rglob('*.cs'):
        if fix_file(cs_file):
            fixed_count += 1
            print(f"Fixed: {cs_file}")
    
    print(f"\nFixed {fixed_count} files")

if __name__ == '__main__':
    main()
