import json
from pathlib import Path

GAME_DIR = (Path(__file__).resolve().parent.parent / 'city-of-heroes').resolve()

with open(str(GAME_DIR / 'data' / 'CrowdRepo.data.full'), 'r', encoding='utf-8') as f:
    raw = json.load(f)

# Collect ALL $ids from the whole tree
id_table = {}
def collect(obj):
    if isinstance(obj, dict):
        if '$id' in obj:
            id_table[obj['$id']] = obj
        for v in obj.values():
            collect(v)
    elif isinstance(obj, list):
        for item in obj:
            collect(item)

collect(raw)
print('Total $id objects in whole file:', len(id_table))

# Show what Armageddons actually contains
for crowd in raw:
    if crowd.get('Name') == 'Armageddons':
        members = crowd.get('CrowdMemberCollection', [])
        print('Armageddons members:', len(members))
        for m in members:
            ref = m.get('$ref')
            id_ = m.get('$id')
            name = m.get('Name', '?')
            print('  $id={} $ref={} Name={} keys={}'.format(id_, ref, name, list(m.keys())[:4]))
            if ref:
                target = id_table.get(ref)
                print('    -> target found:', target is not None, '| target Name:', target.get('Name') if target else 'N/A')
        break
