#!/usr/bin/env python3
"""Fix SA1413, SA1503, and SA1516 style errors in C# files."""

import re
import sys
from pathlib import Path


def fix_sa1503_single_line_if(content):
    """Add braces to single-line if statements without braces."""
    lines = content.split('\n')
    result = []
    i = 0
    
    while i < len(lines):
        line = lines[i]
        stripped = line.lstrip()
        indent = line[:len(line) - len(stripped)]
        
        # Check for single-line if/else/foreach/for/while without braces
        if_match = re.match(r'^(if|else if|else|foreach|for|while|using)\s*(\(.*?\))?\s+(.+)$', stripped)
        
        if if_match and not stripped.endswith('{') and not stripped.endswith(';'):
            keyword = if_match.group(1)
            condition = if_match.group(2) or ''
            statement = if_match.group(3)
            
            # Special handling for else
            if keyword == 'else' and not statement.startswith('if'):
                result.append(f"{indent}{keyword}")
                result.append(f"{indent}{{")
                result.append(f"{indent}    {statement}")
                result.append(f"{indent}}}")
            else:
                result.append(f"{indent}{keyword}{condition}")
                result.append(f"{indent}{{")
                result.append(f"{indent}    {statement}")
                result.append(f"{indent}}}")
        else:
            result.append(line)
        
        i += 1
    
    return '\n'.join(result)


def fix_sa1413_trailing_comma(content):
    """Add trailing commas to multi-line initializers."""
    lines = content.split('\n')
    result = []
    
    for i, line in enumerate(lines):
        # Check if this line ends an initializer element without a trailing comma
        stripped = line.rstrip()
        
        # Look for patterns like: "value" followed by newline and then } or )
        if i < len(lines) - 1:
            next_line = lines[i + 1].lstrip()
            
            # If current line ends with ", }, or ) but not with comma, and next line starts with } or )
            if (next_line.startswith('}') or next_line.startswith(')')) and \
               stripped and not stripped.endswith(',') and not stripped.endswith('{') and \
               not stripped.endswith('(') and not stripped.endswith('['):
                # Add trailing comma
                result.append(stripped + ',')
            else:
                result.append(line)
        else:
            result.append(line)
    
    return '\n'.join(result)


def fix_sa1516_blank_lines(content):
    """Add blank lines between elements."""
    lines = content.split('\n')
    result = []
    
    for i, line in enumerate(lines):
        result.append(line)
        
        if i < len(lines) - 1:
            current = line.strip()
            next_line = lines[i + 1].strip()
            
            # Check if we need a blank line between elements
            # Rules: Add blank line after closing brace if next line is not closing brace, namespace, or blank
            if current == '}' and next_line and not next_line.startswith('}') and \
               not next_line.startswith('namespace') and not next_line.startswith('//'):
                result.append('')
            
            # Add blank line before method/property/class declarations
            elif next_line and (
                next_line.startswith('public ') or 
                next_line.startswith('private ') or 
                next_line.startswith('protected ') or
                next_line.startswith('internal ')
            ) and current and current != '{' and not current.startswith('//') and current != '':
                if i > 0 and lines[i].strip() != '':
                    result.append('')
    
    return '\n'.join(result)


def process_file(file_path):
    """Process a single C# file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Apply fixes in order
        # content = fix_sa1503_single_line_if(content)  # This is complex, skip for now
        content = fix_sa1413_trailing_comma(content)
        # content = fix_sa1516_blank_lines(content)  # This is complex, skip for now
        
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        return True
    except Exception as e:
        print(f"Error processing {file_path}: {e}", file=sys.stderr)
        return False


def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: fix_style_errors.py <path>")
        sys.exit(1)
    
    path = Path(sys.argv[1])
    
    if path.is_file():
        process_file(path)
    elif path.is_dir():
        for cs_file in path.rglob('*.cs'):
            print(f"Processing {cs_file}")
            process_file(cs_file)


if __name__ == '__main__':
    main()
