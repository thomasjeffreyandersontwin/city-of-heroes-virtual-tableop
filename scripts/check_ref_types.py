import json
with open('data/crowds/rebuilt/Armageddons.data') as f:
    data = json.load(f)

count = 0
def find_refs(obj, path='root', depth=0):
    global count
    if depth > 20:
        return
    if isinstance(obj, dict):
        ref = obj.get('$ref')
        if ref is not None and not isinstance(ref, str):
            count += 1
            print(f'  NON-STRING $ref={repr(ref)} ({type(ref).__name__}) at {path}')
            if count > 10:
                return
        for k, v in obj.items():
            find_refs(v, f'{path}.{k}', depth+1)
    elif isinstance(obj, list):
        for i, v in enumerate(obj[:5]):
            find_refs(v, f'{path}[{i}]', depth+1)

find_refs(data)
print(f'Total non-string $refs: {count}')
