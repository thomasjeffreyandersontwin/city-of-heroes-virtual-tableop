"""
Convert .data crowd files to .json format for the Hero VTT new crowd system.

Each .data file contains exactly one top-level crowd. The script:
  1. Reads the raw JSON (which uses Newtonsoft $id/$ref for circular refs)
  2. Strips runtime/positional state that doesn't belong in saved files
  3. Writes the same structure as .json (keeping $id/$ref intact so
     Owner/Character back-refs survive deserialization without null crashes)

Usage:
    python scripts/split_to_json.py                     # convert all named crowds
    python scripts/split_to_json.py <file.data> <out>   # convert one file

Named crowds: Armageddons, Campaigns, Scenarios, Assets By COH Structure,
              Assets By Concept, Custom Power Sets, Mob Flattened,
              System Characters
"""

import json, os, sys, re

CROWDS_DIR = "data/crowds"
JSON_DIR   = "data/crowds/converted"

STRIP_KEYS = frozenset({
    "RosterCrowd",
    "IsSpawned",
    "Position",
    "SavedPosition",
    "SavedPositions",
    "IsInCombat",
    "IsTargeted",
    "IsDead",
})

NAMED_CROWDS = {
    "Armageddons.data",
    "Campaigns.data",
    "Scenarios.data",
    "Assets By COH Structure.data",
    "Assets By Concept.data",
    "Custom Power Sets.data",
    "Mob Flattened.data",
    "System Characters.data",
}


def strip_runtime(obj):
    """Recursively remove runtime-state keys; keep $id/$ref/$values intact."""
    if isinstance(obj, dict):
        return {k: strip_runtime(v) for k, v in obj.items() if k not in STRIP_KEYS}
    elif isinstance(obj, list):
        return [strip_runtime(item) for item in obj]
    return obj


def crowd_summary(crowd, indent=0):
    name = crowd.get("Name", "?")
    members = crowd.get("CrowdMemberCollection") or []
    if isinstance(members, dict):
        members = members.get("$values", [])
    sub   = [m for m in members if isinstance(m, dict) and not ("$ref" in m) and "CrowdMemberCollection" in m]
    chars = len([m for m in members if isinstance(m, dict) and "OptionGroups" in m])
    print("  " + "  " * indent + "{} [{} chars, {} sub-crowds]".format(name, chars, len(sub)))
    for s in sub:
        crowd_summary(s, indent + 1)


def convert_file(src_path, dst_path):
    kb_in = os.path.getsize(src_path) // 1024
    print("  {} ({} KB) ...".format(os.path.basename(src_path), kb_in), end=" ", flush=True)

    with open(src_path, encoding="utf-8") as f:
        raw = json.load(f)

    cleaned = strip_runtime(raw)

    os.makedirs(os.path.dirname(dst_path), exist_ok=True)
    with open(dst_path, "w", encoding="utf-8") as f:
        json.dump(cleaned, f, indent=2, ensure_ascii=False)

    kb_out = os.path.getsize(dst_path) // 1024
    print("-> {} ({} KB)".format(os.path.basename(dst_path), kb_out))

    top = cleaned[0] if isinstance(cleaned, list) else cleaned
    crowd_summary(top)


def convert_named(crowds_dir, json_dir):
    print("Converting named crowds from {} ...".format(crowds_dir))
    for fname in sorted(NAMED_CROWDS):
        src = os.path.join(crowds_dir, fname)
        if not os.path.exists(src):
            print("  SKIP (not found): {}".format(fname))
            continue
        dst_name = fname  # keep .data extension so Browse dialog picks them up
        dst = os.path.join(json_dir, dst_name)
        convert_file(src, dst)
    print("Done.")


if __name__ == "__main__":
    if len(sys.argv) == 3:
        convert_file(sys.argv[1], sys.argv[2])
    elif len(sys.argv) == 1:
        convert_named(CROWDS_DIR, JSON_DIR)
    else:
        print(__doc__)
        sys.exit(1)
