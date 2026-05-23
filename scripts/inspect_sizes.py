import json, glob, os

# Check System Characters crowd structure
with open('data/crowds/System Characters.data', encoding='utf-8') as f:
    sc = json.load(f)
top = sc[0] if isinstance(sc, list) else sc
members = top.get('CrowdMemberCollection', [])
print('System Characters members:')
for m in members:
    print(' ', m.get('Name'), '- has OptionGroups:', 'OptionGroups' in m)

print()

# Check raw size of a few named characters
targets = ['Pre-Emptive Strike', 'Spyder', 'Suzerain']
found = set()
for path in sorted(glob.glob('data/crowds/All Characters *.data')):
    with open(path, encoding='utf-8') as f:
        raw = json.load(f)
    for char in raw[0].get('CrowdMemberCollection', []):
        name = char.get('Name', '').strip()
        if name in targets and name not in found:
            found.add(name)
            s = json.dumps(char)
            print(name, 'raw size:', len(s) // 1024, 'KB, inline $id count:', s.count('"$id"'))
    if found == set(targets):
        break

# Also check system chars
with open('data/crowds/System Characters.data', encoding='utf-8') as f:
    raw = json.load(f)
ref_map = {}
stack = [raw]
while stack:
    obj = stack.pop()
    if isinstance(obj, dict):
        oid = obj.get('$id')
        if oid:
            ref_map[str(oid)] = obj
        stack.extend(obj.values())
    elif isinstance(obj, list):
        stack.extend(obj)

top = raw[0] if isinstance(raw, list) else raw
for m in top.get('CrowdMemberCollection', []):
    actual = ref_map.get(str(m.get('$ref', '')), m)
    name = actual.get('Name', '?')
    s = json.dumps(actual)
    print('SystemChar', name, 'raw size:', len(s) // 1024, 'KB, inline $id count:', s.count('"$id"'))
