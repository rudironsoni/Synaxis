#!/usr/bin/env python3
import xml.etree.ElementTree as ET
import sys

def main():
    trx_file = 'tests/InferenceGateway/IntegrationTests/TestResults/TestResults.trx'
    try:
        tree = ET.parse(trx_file)
    except ET.ParseError as e:
        print(f"Error parsing XML: {e}")
        sys.exit(1)
    
    root = tree.getroot()
    # Namespace handling
    ns = {'ns': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    
    failures = []
    for unit_test_result in root.findall('.//ns:UnitTestResult', ns):
        outcome = unit_test_result.get('outcome')
        if outcome == 'Failed':
            test_name = unit_test_result.get('testName')
            # Extract error message
            error_info = unit_test_result.find('.//ns:ErrorInfo', ns)
            message = ''
            if error_info is not None:
                msg_elem = error_info.find('ns:Message', ns)
                if msg_elem is not None:
                    message = msg_elem.text.strip()
            # Extract stack trace if needed
            stack_trace = ''
            if error_info is not None:
                stack_elem = error_info.find('ns:StackTrace', ns)
                if stack_elem is not None:
                    stack_trace = stack_elem.text.strip()
            
            failures.append({
                'test_name': test_name,
                'message': message,
                'stack_trace': stack_trace
            })
    
    print(f"Total failures: {len(failures)}")
    for i, f in enumerate(failures, 1):
        print(f"\n{i}. {f['test_name']}")
        print(f"   Error: {f['message']}")
        # Optionally print first line of stack trace
        if f['stack_trace']:
            first_line = f['stack_trace'].split('\n')[0]
            print(f"   Stack: {first_line}")

if __name__ == '__main__':
    main()