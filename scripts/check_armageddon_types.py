"""Check $type values in rebuilt Armageddons.data vs original source."""
import json, os

def collect_types(obj, types=None):
    if types is None:
        types = set()
    if isinstance(obj, dict):
        t = obj.get('$type')
        if t:
            types.add(t)
        for v in obj.values():
            collect_types(v, types)
    elif isinstance(obj, list):
        for item in obj:
            collect_types(item, types)
    return types

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Check rebuilt file
rebuilt_path = os.path.join(root, 'data', 'crowds', 'rebuilt', 'Armageddons.data')
with open(rebuilt_path) as f:
    rebuilt = json.load(f)
rebuilt_types = collect_types(rebuilt)
print('=== Rebuilt Armageddons.data types ===')
for t in sorted(rebuilt_types):
    print(' ', t[:100])

# Check a working source file (All Characters)
src_path = os.path.join(root, 'data', 'crowds', 'All Characters 001.data')
with open(src_path) as f:
    src = json.load(f)
src_types = collect_types(src)
print('\n=== All Characters 001.data types ===')
for t in sorted(src_types)[:10]:
    print(' ', t[:100])

# Check top-level object
top = rebuilt[0] if isinstance(rebuilt, list) else rebuilt
print('\n=== Top-level object keys ===')
for k in list(top.keys())[:10]:
    print(' ', k, '=', repr(str(top[k])[:80]))
