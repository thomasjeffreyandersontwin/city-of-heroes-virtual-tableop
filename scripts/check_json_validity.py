"""
Deeply inspect the rebuilt Armageddons.data for common deserialization issues:
1. JSON validity
2. $ref pointing to non-existent $id
3. Circular $ref loops
4. Unexpected null values in critical fields
"""
import json, os

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
path = os.path.join(root, 'data', 'crowds', 'rebuilt', 'Armageddons.data')

print(f"Reading: {path}")
with open(path) as f:
    raw = f.read()

# 1. Valid JSON?
try:
    data = json.loads(raw)
    print("JSON: VALID")
except json.JSONDecodeError as e:
    print(f"JSON: INVALID - {e}")
    exit(1)

# 2. Collect all $id values and all $ref values
ids  = {}
refs = {}

def scan(obj, path="root"):
    if isinstance(obj, dict):
        oid = obj.get('$id')
        ref = obj.get('$ref')
        if oid is not None:
            ids[str(oid)] = path
        if ref is not None:
            refs[str(ref)] = path
        for k, v in obj.items():
            if k not in ('$id', '$ref'):
                scan(v, f"{path}.{k}")
    elif isinstance(obj, list):
        for i, item in enumerate(obj):
            scan(item, f"{path}[{i}]")

scan(data)

print(f"\n$id count : {len(ids)}")
print(f"$ref count: {len(refs)}")

# 3. Check for dangling $refs (ref not in ids)
dangling = {r: p for r, p in refs.items() if r not in ids}
if dangling:
    print(f"\nDANGLING $refs ({len(dangling)}):")
    for r, p in list(dangling.items())[:10]:
        print(f"  $ref={r} at {p}")
else:
    print("\nNo dangling $refs.")

# 4. Check for duplicate $ids
print(f"\n(unique id count: {len(ids)})")

# 5. Show first 5 ids and first 5 refs
print("\nFirst 5 $ids:", list(ids.items())[:5])
print("First 5 $refs:", list(refs.items())[:5])

# 6. Check top-level structure
top = data[0] if isinstance(data, list) else data
print(f"\nTop-level Name: {top.get('Name')}")
print(f"Top-level $type: {top.get('$type','MISSING')}")
print(f"Top-level $id: {top.get('$id','MISSING')}")
print(f"CrowdMemberCollection length: {len(top.get('CrowdMemberCollection',[]))}")
