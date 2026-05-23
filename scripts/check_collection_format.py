import json, os
root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

for fname in ['All Characters 001.data', os.path.join('rebuilt','Armageddons.data')]:
    path = os.path.join(root, 'data', 'crowds', fname)
    with open(path) as f:
        data = json.load(f)
    top = data[0] if isinstance(data, list) else data
    cmc = top.get('CrowdMemberCollection')
    print(f'{fname}:')
    print(f'  CrowdMemberCollection type: {type(cmc).__name__}')
    if isinstance(cmc, dict):
        print(f'  keys: {list(cmc.keys())}')
        if '$values' in cmc:
            print(f'  $id: {cmc.get("$id")}')
            items = cmc['$values']
            print(f'  items[0] type: {type(items[0]).__name__ if items else "empty"}')
            if items:
                print(f'  items[0] keys: {list(items[0].keys())[:5]}')
    elif isinstance(cmc, list):
        print(f'  length: {len(cmc)}')
        if cmc:
            print(f'  items[0] type: {type(cmc[0]).__name__}')
            print(f'  items[0] keys: {list(cmc[0].keys())[:5]}')
    print()
