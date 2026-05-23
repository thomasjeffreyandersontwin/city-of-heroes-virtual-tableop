#!/usr/bin/env python3
"""Fix all non-domain column names in specification-by-example-increment-1.md.

Replacements:
  crowd_file_path  (in CrowdFile table / FK context) -> absoluteFilePath
  crowd_file_path  (in Crowd table as FK column)     -> sourceFile
  parent_crowd_name                                  -> parentCrowd
  crowd_name                                         -> crowdName
  character_name                                     -> characterName
  existing_clone_path                                -> absoluteFilePath
  new_clone_path                                     -> absoluteFilePath
  new_top_level_crowd_name                           -> crowdName
  concept_tag                                        -> conceptTag
  category_node                                      -> conceptTag
  group_type                                         -> groupType
  sub_heading                                        -> groupType
  coh_faction_tag                                    -> cohFactionTag
  faction_node                                       -> cohFactionTag
"""
import re
from pathlib import Path

SPEC = Path(__file__).resolve().parent.parent / "docs/increment-1/specification-by-example-increment-1.md"

text = SPEC.read_text(encoding="utf-8")

# Track replacements
changes: list[tuple[str, str]] = []

def rep(old: str, new: str, t: str) -> str:
    if old in t:
        changes.append((old, new))
        return t.replace(old, new)
    return t

# -----------------------------------------------------------------------
# 1. crowd_file_path in CrowdFile table headers
#    These are 2-column tables: | scenario | crowd_file_path |
#    Pattern: pipe-table row containing ONLY scenario + crowd_file_path
# -----------------------------------------------------------------------
# Replace in table header rows where crowd_file_path is the only data column
# (i.e., the table that describes CrowdFile, not Crowd)
text = re.sub(
    r"(\| scenario\s+\|) crowd_file_path(\s+\|)",
    lambda m: m.group(1) + " absoluteFilePath" + m.group(2),
    text,
)
changes.append(("crowd_file_path (CrowdFile header)", "absoluteFilePath"))

# -----------------------------------------------------------------------
# 2. crowd_file_path remaining occurrences (in Crowd table and FK notes)
#    -> sourceFile
# -----------------------------------------------------------------------
text = text.replace("crowd_file_path", "sourceFile")
changes.append(("crowd_file_path (remaining)", "sourceFile"))

# -----------------------------------------------------------------------
# 3. parent_crowd_name -> parentCrowd
# -----------------------------------------------------------------------
text = rep("parent_crowd_name", "parentCrowd", text)

# -----------------------------------------------------------------------
# 4. crowd_name -> crowdName  (columns and FK notes)
# -----------------------------------------------------------------------
text = rep("crowd_name", "crowdName", text)

# -----------------------------------------------------------------------
# 5. character_name -> characterName
# -----------------------------------------------------------------------
text = rep("character_name", "characterName", text)

# -----------------------------------------------------------------------
# 6. existing_clone_path -> absoluteFilePath
# -----------------------------------------------------------------------
text = rep("existing_clone_path", "absoluteFilePath", text)

# -----------------------------------------------------------------------
# 7. new_clone_path -> absoluteFilePath
# -----------------------------------------------------------------------
text = rep("new_clone_path", "absoluteFilePath", text)

# -----------------------------------------------------------------------
# 8. new_top_level_crowd_name -> crowdName
# -----------------------------------------------------------------------
text = rep("new_top_level_crowd_name", "crowdName", text)

# -----------------------------------------------------------------------
# 9. concept_tag -> conceptTag
# -----------------------------------------------------------------------
text = rep("concept_tag", "conceptTag", text)

# -----------------------------------------------------------------------
# 10. category_node -> conceptTag
# -----------------------------------------------------------------------
text = rep("category_node", "conceptTag", text)

# -----------------------------------------------------------------------
# 11. group_type -> groupType
# -----------------------------------------------------------------------
text = rep("group_type", "groupType", text)

# -----------------------------------------------------------------------
# 12. sub_heading -> groupType
# -----------------------------------------------------------------------
text = rep("sub_heading", "groupType", text)

# -----------------------------------------------------------------------
# 13. coh_faction_tag -> cohFactionTag
# -----------------------------------------------------------------------
text = rep("coh_faction_tag", "cohFactionTag", text)

# -----------------------------------------------------------------------
# 14. faction_node -> cohFactionTag
# -----------------------------------------------------------------------
text = rep("faction_node", "cohFactionTag", text)

SPEC.write_text(text, encoding="utf-8")

print(f"Applied {len(changes)} replacement type(s) to {SPEC.name}")
for old, new in changes:
    print(f"  {old!r:45s} -> {new!r}")
