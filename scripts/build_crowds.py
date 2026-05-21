"""
Build new crowd .data files from the "All Characters" pool.

Each rebuilt file:
  - Contains only the characters the crowd needs (looked up by name)
  - Every $ref is satisfied: objects are inlined at first use, subsequent
    references use renumbered $ref
  - Back-refs (Owner, Character) are kept as $ref so the C# model graph
    stays intact after deserialization
  - Runtime fields stripped (SavedPosition, IsSpawned, etc.)

Usage:
    python scripts/build_crowds.py
"""

import json, glob, os, sys

CROWDS_DIR = "data/crowds"
OUT_DIR    = "data/crowds/rebuilt"

# Keys whose $ref values point BACK UP to a parent — keep as $ref always
# (RosterCrowd/RosterCrowdMember are runtime state, stripped below)
BACK_REF_KEYS = frozenset({"Owner", "Character"})

RUNTIME_KEYS = frozenset({
    "SavedPosition", "SavedPositions", "IsSpawned",
    "Position", "IsInCombat", "IsTargeted", "IsDead",
    # Runtime roster assignments — dangling $refs if preserved across files
    "RosterCrowd", "RosterCrowdMember",
})

ID_KEY  = "$id"
REF_KEY = "$ref"
VAL_KEY = "$values"

CROWD_MODEL_TYPE = "Module.HeroVirtualTabletop.Crowds.CrowdModel, Module.HeroVirtualTabletop"

NAMED_CROWDS = [
    "Armageddons.data",
    "Campaigns.data",
    "Scenarios.data",
    "Assets By COH Structure.data",
    "Assets By Concept.data",
    "Custom Power Sets.data",
    "Mob Flattened.data",
    "System Characters.data",
]


# ── ref map ───────────────────────────────────────────────────────────────────

def build_ref_map(root):
    ref_map = {}
    stack = [root]
    while stack:
        obj = stack.pop()
        if isinstance(obj, dict):
            oid = obj.get(ID_KEY)
            if oid is not None:
                ref_map[str(oid)] = obj
            stack.extend(obj.values())
        elif isinstance(obj, list):
            stack.extend(obj)
    return ref_map


# ── per-character converter ───────────────────────────────────────────────────

class CharacterConverter:
    """
    Converts ONE character using its own source-file ref_map.
    $id namespace is scoped to this character by prefixing old ids with a
    unique file key, so characters from different files never collide.

    Uses a shared global counter so output $ids are unique across the
    whole crowd file.
    """

    def __init__(self, file_ref_map, file_key, global_counter):
        self._ref_map    = file_ref_map
        self._prefix     = file_key + ":"       # e.g. "001:"
        self._counter    = global_counter       # list [next_id], shared
        self._old_to_new = {}                   # namespaced old id → new int
        self._written    = set()                # namespaced old ids emitted inline

    def _ns(self, old_id):
        return self._prefix + str(old_id)

    def _new_id(self, old_id):
        key = self._ns(old_id)
        if key not in self._old_to_new:
            self._counter[0] += 1
            self._old_to_new[key] = str(self._counter[0])  # Newtonsoft needs string $id/$ref
        return self._old_to_new[key]

    def convert(self, obj, parent_key=None):
        if isinstance(obj, dict):
            ref = obj.get(REF_KEY)
            if ref is not None:
                rid = str(ref)
                ns  = self._ns(rid)
                # Back-ref: always keep as $ref
                if parent_key in BACK_REF_KEYS:
                    return {REF_KEY: self._new_id(rid)}
                # Already written inline: emit ref
                if ns in self._written:
                    return {REF_KEY: self._new_id(rid)}
                # Not yet written: inline it
                target = self._ref_map.get(rid)
                if target is None:
                    return None   # genuinely dangling — drop
                return self.convert(target, parent_key)

            if VAL_KEY in obj:
                result = []
                for item in obj[VAL_KEY]:
                    c = self.convert(item, parent_key)
                    if c is not None:
                        result.append(c)
                return result

            out  = {}
            oid  = obj.get(ID_KEY)
            if oid is not None:
                ns = self._ns(str(oid))
                if ns in self._written:
                    return {REF_KEY: self._new_id(str(oid))}
                self._written.add(ns)
                out[ID_KEY] = self._new_id(str(oid))

            for k, v in obj.items():
                if k in (ID_KEY, REF_KEY, VAL_KEY):
                    continue
                if k in RUNTIME_KEYS:
                    continue
                c = self.convert(v, parent_key=k)
                if c is None:
                    continue
                out[k] = c

            return out if out else None

        elif isinstance(obj, list):
            result = []
            for item in obj:
                c = self.convert(item, parent_key)
                if c is not None:
                    result.append(c)
            return result

        return obj


# ── build character pool ──────────────────────────────────────────────────────

def build_char_pool():
    """
    Returns {name: (raw_char_dict, full_file_ref_map, file_key)}.
    file_key is the basename stem (e.g. "All Characters 001") used to
    namespace $ids so characters from different files never collide.
    """
    pool = {}
    files = sorted(glob.glob(os.path.join(CROWDS_DIR, "All Characters *.data")))
    print("Loading from {} All Characters files...".format(len(files)))

    for path in files:
        file_key = os.path.splitext(os.path.basename(path))[0]
        with open(path, encoding="utf-8") as f:
            raw = json.load(f)
        file_ref_map = build_ref_map(raw)
        top = raw[0] if isinstance(raw, list) else raw
        for member in top.get("CrowdMemberCollection", []):
            if REF_KEY in member:
                member = file_ref_map.get(str(member[REF_KEY]), member)
            name = member.get("Name", "").strip()
            if name and name not in pool:
                pool[name] = (member, file_ref_map, file_key)

    print("  {} unique characters.".format(len(pool)))
    return pool


# ── crowd structure extraction ────────────────────────────────────────────────

def extract_structure(obj, ref_map=None):
    """Walk crowd/character tree, resolving $refs via ref_map."""
    if not isinstance(obj, dict):
        return None
    # Resolve $ref
    if REF_KEY in obj:
        if ref_map is None:
            return None
        target = ref_map.get(str(obj[REF_KEY]))
        if target is None:
            return None
        return extract_structure(target, ref_map)

    name    = obj.get("Name", "").strip()
    is_char = "OptionGroups" in obj
    members = obj.get("CrowdMemberCollection", [])
    if isinstance(members, dict):
        members = members.get(VAL_KEY, [])
    children = [c for c in (extract_structure(m, ref_map) for m in members) if c]
    # Skip empty-name nodes that are just artifacts of $ref resolution
    if not name and not children and not is_char:
        return None
    return {"name": name, "is_char": is_char, "children": children}


# ── crowd assembly ────────────────────────────────────────────────────────────

def _next_id(global_counter):
    global_counter[0] += 1
    return str(global_counter[0])


def assemble(node, pool, global_counter, missing):
    name     = node["name"]
    children = node["children"]

    if node["is_char"] or (not children):
        entry = pool.get(name)
        if entry is None:
            missing.add(name)
            return None   # skip missing characters entirely
        raw_char, file_ref_map, file_key = entry
        conv   = CharacterConverter(file_ref_map, file_key, global_counter)
        result = conv.convert(raw_char)
        if result:
            result["Name"] = name
        return result

    crowd_id = _next_id(global_counter)
    members = []
    for child in children:
        a = assemble(child, pool, global_counter, missing)
        if a:
            members.append(a)
    return {
        ID_KEY:  crowd_id,
        "$type": CROWD_MODEL_TYPE,
        "Name":  name,
        "CrowdMemberCollection": members,
    }


# ── main ──────────────────────────────────────────────────────────────────────

def main():
    sys.setrecursionlimit(5000)
    pool = build_char_pool()

    os.makedirs(OUT_DIR, exist_ok=True)

    for fname in NAMED_CROWDS:
        src = os.path.join(CROWDS_DIR, fname)
        if not os.path.exists(src):
            print("SKIP: {}".format(fname))
            continue

        print("Building: {}".format(fname))
        with open(src, encoding="utf-8") as f:
            raw = json.load(f)

        top_raw        = raw[0] if isinstance(raw, list) else raw
        src_ref_map    = build_ref_map(raw)
        structure      = extract_structure(top_raw, src_ref_map)
        global_counter = [0]   # fresh $id numbering per output file
        missing        = set()
        crowd          = assemble(structure, pool, global_counter, missing)

        if missing:
            print("  {} chars not found: {}".format(
                len(missing), ", ".join(sorted(missing)[:5])))

        out_path = os.path.join(OUT_DIR, fname)
        with open(out_path, "w", encoding="utf-8") as f:
            json.dump([crowd], f, indent=2, ensure_ascii=False)

        kb = os.path.getsize(out_path) // 1024
        print("  -> {} KB  ({} missing)".format(kb, len(missing)))


if __name__ == "__main__":
    main()
