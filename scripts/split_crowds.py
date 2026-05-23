"""
Split CrowdRepo.data.full into per-crowd files in data/crowds/.

Uses a full-tree $id lookup so cross-crowd $refs resolve correctly,
but only replaces the SHALLOW CrowdMemberCollection entries — internal
character $refs (back-links like Character/Owner) stay intact for
Newtonsoft to resolve within each file.
"""

import json, os, re
from pathlib import Path

GAME_DIR    = (Path(__file__).resolve().parent.parent / 'city-of-heroes').resolve()
FULL_BACKUP = str(GAME_DIR / 'data' / 'CrowdRepo.data.full')
CROWDS_DIR  = str(GAME_DIR / 'data' / 'crowds')
CHUNK_SIZE  = 50

os.makedirs(CROWDS_DIR, exist_ok=True)

print("Loading full backup...")
with open(FULL_BACKUP, 'r', encoding='utf-8') as f:
    raw = json.load(f)

# Build $id lookup from the ENTIRE tree — needed because character $ids
# are assigned sequentially including all nested objects, so a character
# in crowd #3 might have $id "541" even though it looks "shallow"
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
print("  Indexed {} objects total".format(len(id_table)))

def safe_name(name):
    return re.sub(r'[<>:"/\\|?*]', '_', name)

def resolve_members(members):
    """Shallow: replace {$ref: N} entries in a member list with their
    full target object. Internal $refs inside characters are left alone."""
    result = []
    for m in members:
        if isinstance(m, dict) and set(m.keys()) == {'$ref'}:
            target = id_table.get(m['$ref'])
            if target is not None:
                result.append(target)
            # dangling ref — skip
        else:
            result.append(m)
    return result

written = []

for crowd in raw:
    name     = crowd.get('Name', 'Unknown')
    members  = crowd.get('CrowdMemberCollection', [])
    resolved = resolve_members(members)

    if name == 'All Characters':
        for i in range(0, len(resolved), CHUNK_SIZE):
            chunk = resolved[i:i + CHUNK_SIZE]
            chunk_num = (i // CHUNK_SIZE) + 1
            crowd_copy = {k: v for k, v in crowd.items() if k != 'CrowdMemberCollection'}
            crowd_copy['CrowdMemberCollection'] = chunk
            fname = 'All Characters {:03d}.data'.format(chunk_num)
            fpath = os.path.join(CROWDS_DIR, fname)
            with open(fpath, 'w', encoding='utf-8') as f:
                json.dump([crowd_copy], f, indent=2)
            size_kb = round(os.path.getsize(fpath) / 1024, 0)
            print("  {} -> {} members, {} KB".format(fname, len(chunk), size_kb))
            written.append(fname)
    else:
        crowd_copy = {k: v for k, v in crowd.items() if k != 'CrowdMemberCollection'}
        crowd_copy['CrowdMemberCollection'] = resolved
        fname = safe_name(name) + '.data'
        fpath = os.path.join(CROWDS_DIR, fname)
        with open(fpath, 'w', encoding='utf-8') as f:
            json.dump([crowd_copy], f, indent=2)
        size_kb = round(os.path.getsize(fpath) / 1024, 0)
        print("  {} -> {} members, {} KB".format(fname, len(resolved), size_kb))
        written.append(fname)

# Remove old cache
cache = str(GAME_DIR / 'data' / 'CrowdRepo.data.cache')
if os.path.exists(cache):
    os.remove(cache)

print('\nDone. {} files in {}'.format(len(written), CROWDS_DIR))
