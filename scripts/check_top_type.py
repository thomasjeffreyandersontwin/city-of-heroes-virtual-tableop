import json, os, glob
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

files = [
    os.path.join(root, 'data', 'crowds', 'All Characters 001.data'),
    os.path.join(root, 'data', 'crowds', 'rebuilt', 'Armageddons.data'),
]

for path in files:
    with open(path) as f:
        data = json.load(f)
    top = data[0] if isinstance(data, list) else data
    print(f"{os.path.basename(path)}")
    print(f"  is_list: {isinstance(data, list)}, len: {len(data) if isinstance(data, list) else 1}")
    print(f"  top keys: {list(top.keys())[:6]}")
    print(f"  $type: {top.get('$type', 'MISSING')[:80]}")
    print(f"  $id: {top.get('$id', 'MISSING')}")
    print()
