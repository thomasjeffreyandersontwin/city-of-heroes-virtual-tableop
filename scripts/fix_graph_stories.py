#!/usr/bin/env python3
"""Align docs/stories/story-graph.json with the spec story names.

Changes:
  - Rename 'Load Crowd Collection from Repository'
      -> 'Load Active Crowd Files on Startup'
  - Add missing stories to 'Manage Crowd Repository' sub-epic:
      Browse and Activate Crowd Files (GM)
      Track Source File per Crowd      (System)
      Save Dirty Crowds to Source Files (GM)
      Save Crowd to New File           (GM)
  - Rename 'Browse Crowds by Concept (Animals, Armed Forces, Civilians, Vehicles, etc.)'
      -> 'Browse Crowds by Concept'
  - Add 'Browse Crowds by Gangs, Crews, and Squads' and
    'Browse All Characters Crowd' to 'Organize Crowd Collections' sub-epic
    (they are in the graph but not in the story-map sub-epic — confirm they
     already exist to avoid duplication).
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
GRAPH_FILE = REPO_ROOT / "docs" / "stories" / "story-graph.json"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def all_stories_flat(graph: dict) -> list[dict]:
    result: list[dict] = []
    def _walk(node: dict):
        for se in node.get("sub_epics", []): _walk(se)
        for sg in node.get("story_groups", []):
            result.extend(sg.get("stories", []))
        result.extend(node.get("stories", []))
    for e in graph.get("epics", []): _walk(e)
    return result


def story_exists(graph: dict, name: str) -> bool:
    nl = name.lower()
    return any(s.get("name", "").lower() == nl for s in all_stories_flat(graph))


def find_sub_epic(graph: dict, name: str) -> dict | None:
    nl = name.lower()
    def _walk(node: dict) -> dict | None:
        for se in node.get("sub_epics", []):
            if se.get("name", "").lower() == nl:
                return se
            hit = _walk(se)
            if hit: return hit
        return None
    for e in graph.get("epics", []):
        if e.get("name", "").lower() == nl:
            return e
        hit = _walk(e)
        if hit: return hit
    return None


def new_story(name: str, story_type: str) -> dict:
    return {"name": name, "story_type": story_type}


def append_story(sub_epic: dict, story: dict):
    if "stories" not in sub_epic:
        sub_epic["stories"] = []
    sub_epic["stories"].append(story)
    print(f"  [ADD]    '{story['name']}' -> '{sub_epic['name']}'")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> int:
    graph = json.loads(GRAPH_FILE.read_text(encoding="utf-8"))

    # 1. Rename 'Load Crowd Collection from Repository'
    OLD_LOAD = "Load Crowd Collection from Repository"
    NEW_LOAD = "Load Active Crowd Files on Startup"
    for s in all_stories_flat(graph):
        if s.get("name", "").lower() == OLD_LOAD.lower():
            s["name"] = NEW_LOAD
            print(f"  [RENAME] '{OLD_LOAD}' -> '{NEW_LOAD}'")
            break

    # 2. Rename 'Browse Crowds by Concept (Animals...)'
    OLD_BROWSE = "Browse Crowds by Concept (Animals, Armed Forces, Civilians, Vehicles, etc.)"
    NEW_BROWSE = "Browse Crowds by Concept"
    for s in all_stories_flat(graph):
        if s.get("name", "").lower() == OLD_BROWSE.lower():
            s["name"] = NEW_BROWSE
            print(f"  [RENAME] '{OLD_BROWSE}' -> '{NEW_BROWSE}'")
            break

    # 3. Add missing stories to 'Manage Crowd Repository'
    repo_se = find_sub_epic(graph, "Manage Crowd Repository")
    if repo_se is None:
        print("ERROR: 'Manage Crowd Repository' sub-epic not found", file=sys.stderr)
        return 1

    to_add_repo = [
        ("Browse and Activate Crowd Files", "GM"),
        ("Track Source File per Crowd", "System"),
        ("Save Dirty Crowds to Source Files", "GM"),
        ("Save Crowd to New File", "GM"),
    ]
    for name, stype in to_add_repo:
        if not story_exists(graph, name):
            append_story(repo_se, new_story(name, stype))
        else:
            print(f"  [EXISTS] '{name}' already in graph")

    # 4. Ensure 'Browse Crowds by Gangs, Crews, and Squads' and
    #    'Browse All Characters Crowd' are in 'Organize Crowd Collections'
    org_se = find_sub_epic(graph, "Organize Crowd Collections")
    if org_se:
        for name, stype in [
            ("Browse Crowds by Gangs, Crews, and Squads", "GM"),
            ("Browse All Characters Crowd", "GM"),
        ]:
            if not story_exists(graph, name):
                append_story(org_se, new_story(name, stype))
            else:
                print(f"  [EXISTS] '{name}' already in graph")
    else:
        print("  [WARN] 'Organize Crowd Collections' sub-epic not found — skipping browse additions")

    GRAPH_FILE.write_text(json.dumps(graph, indent=2, ensure_ascii=False), encoding="utf-8")
    print("\nGraph updated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
