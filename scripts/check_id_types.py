import json
with open('data/crowds/All Characters 001.data') as f:
    data = json.load(f)

ID_KEY, REF_KEY = '$id', '$ref'

def find_first(obj, path='root'):
    if isinstance(obj, dict):
        if ID_KEY in obj:
            v = obj[ID_KEY]
            print(f'  $id={repr(v)} ({type(v).__name__}) at {path}')
            return True
        if REF_KEY in obj:
            v = obj[REF_KEY]
            print(f'  $ref={repr(v)} ({type(v).__name__}) at {path}')
            return True
        for k, v in obj.items():
            if find_first(v, path+'.'+k): return True
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            if find_first(v, path+f'[{i}]'): return True
    return False

print("First $id/$ref in All Characters 001.data:")
find_first(data)

# Also check our rebuilt file
print()
with open('data/crowds/rebuilt/Armageddons.data') as f:
    rebuilt = json.load(f)
print("First $id/$ref in rebuilt Armageddons.data:")
find_first(rebuilt)
