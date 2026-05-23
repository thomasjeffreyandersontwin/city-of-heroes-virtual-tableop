import json, os
from pathlib import Path

GAME_DIR = (Path(__file__).resolve().parent.parent / 'city-of-heroes').resolve()
data_path = str(GAME_DIR / 'data' / 'CrowdRepo.data')

with open(data_path, 'r', encoding='utf-8') as f:
    crowds = json.load(f)

all_chars = next(c for c in crowds if c['Name'] == 'All Characters')
system = next(c for c in crowds if c['Name'] == 'System Characters')

original_count = len(all_chars['CrowdMemberCollection'])
print('Original All Characters:', original_count, 'members')

all_chars['CrowdMemberCollection'] = all_chars['CrowdMemberCollection'][:10]
print('Trimmed to:', len(all_chars['CrowdMemberCollection']), 'members')

result = [all_chars, system]
with open(data_path, 'w', encoding='utf-8') as f:
    json.dump(result, f, indent=2)

cache = data_path + '.cache'
if os.path.exists(cache):
    os.remove(cache)
    print('Cache deleted')

size = os.path.getsize(data_path)
print('New file size:', round(size/1024, 0), 'KB')
