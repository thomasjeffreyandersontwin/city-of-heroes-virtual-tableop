import json
with open('data/crowds/rebuilt/Armageddons.data') as f:
    data = json.load(f)
top = data[0] if isinstance(data, list) else data
print('Top name:', top.get('Name'))
t = top.get('$type', '').split(',')[0].split('.')[-1]
print('Type:', t)
members = top.get('CrowdMemberCollection', [])
print('Member count:', len(members))
for m in members:
    mtype = m.get('$type', '').split('.')[-1][:40]
    print('  -', repr(m.get('Name')), '| type:', mtype, '| OptionGroups:', 'OptionGroups' in m, '| id:', m.get('$id'))
