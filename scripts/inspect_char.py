import json

TYPE_KEY = "$type"
REF_KEY = "$ref"
ID_KEY = "$id"
VAL_KEY = "$values"

with open(r"data/crowds/Campaigns.data", encoding="utf-8") as f:
    raw = json.load(f)

# Build ref map
ref_map = {}
stack = [raw]
while stack:
    obj = stack.pop()
    if isinstance(obj, dict):
        oid = obj.get(ID_KEY)
        if oid:
            ref_map[str(oid)] = obj
        stack.extend(obj.values())
    elif isinstance(obj, list):
        stack.extend(obj)

print("Refs:", len(ref_map))

def resolve_one(obj):
    if isinstance(obj, dict) and REF_KEY in obj:
        return ref_map.get(str(obj[REF_KEY]))
    return obj

# Get Campaigns top-level crowd
campaigns = raw[0]
members_raw = campaigns.get("CrowdMemberCollection")
if isinstance(members_raw, dict):
    members_raw = members_raw.get(VAL_KEY, [])

print("Campaigns sub-crowds:", len(members_raw))
for m in members_raw:
    sub = resolve_one(m)
    if sub:
        print(" ", sub.get("Name"), "  sub-members raw type:", type(sub.get("CrowdMemberCollection")))
        sub_members_raw = sub.get("CrowdMemberCollection")
        if isinstance(sub_members_raw, dict):
            sub_members_raw = sub_members_raw.get(VAL_KEY, [])
        elif sub_members_raw is None:
            sub_members_raw = []
        for cm in sub_members_raw[:3]:
            char = resolve_one(cm)
            if char:
                print("    Char:", char.get("Name"), "  keys:", list(char.keys()))
                ai = char.get("ActiveIdentity")
                if isinstance(ai, dict):
                    ai2 = resolve_one(ai)
                    if ai2:
                        print("      ActiveIdentity keys:", list(ai2.keys())[:12])
                        # Check costume
                        costume = ai2.get("Costume")
                        if isinstance(costume, dict):
                            cos2 = resolve_one(costume)
                            if cos2:
                                print("      Costume keys:", list(cos2.keys())[:10])
                                print("      Costume size (keys):", len(cos2))
