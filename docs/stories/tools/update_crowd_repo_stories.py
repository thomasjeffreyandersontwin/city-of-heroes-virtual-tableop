"""One-shot update of the Manage Crowd Repository sub-epic to add the
browse/activate + per-source-file save stories.

Run from the workspace root:

    python docs/stories/tools/update_crowd_repo_stories.py

Then validate via the story-graph CLI:

    python <skills-repo>/skills/story-driven-delivery/story-graph-ops/scripts/story_graph_cli.py \\
        read --file docs/stories/story-graph.json
"""

from __future__ import annotations

import json
from pathlib import Path

GRAPH = Path(__file__).resolve().parents[1] / "story-graph.json"

SUB_EPIC_NAME = "Manage Crowd Repository"

NEW_STORY_ORDER = [
    ("Create Crowd", "GM"),
    ("Rename Crowd", "GM"),
    ("Delete Crowd", "GM"),
    ("Nest Crowd inside Crowd", "GM"),
    ("Browse and Activate Crowd Files", "GM"),
    ("Load Active Crowd Files on Startup", "System"),
    ("Track Source File per Crowd", "System"),
    ("Save Dirty Crowds to Source Files", "GM"),
    ("Save Crowd to New File", "GM"),
    ("Back Up Repository on Load", "System"),
    ("Load Default Crowd Members from Embedded Resource", "System"),
]

# Stories whose existing AC must be carried forward when the name changes.
RENAMES = {
    "Load Crowd Collection from Repository": "Load Active Crowd Files on Startup",
}

# Stories present in the old graph but removed from the new map.
REMOVED = {"Save Crowd Collection to Repository"}


def find_sub_epic(graph: dict) -> dict:
    for epic in graph["epics"]:
        for sub in epic.get("sub_epics", []):
            if sub["name"] == SUB_EPIC_NAME:
                return sub
    raise SystemExit(f"Sub-epic not found: {SUB_EPIC_NAME!r}")


def main() -> int:
    graph = json.loads(GRAPH.read_text(encoding="utf-8"))
    sub = find_sub_epic(graph)
    group = sub["story_groups"][0]
    existing = {s["name"]: s for s in group["stories"]}

    for old, new in RENAMES.items():
        if old in existing:
            existing[old]["name"] = new
            existing[new] = existing.pop(old)

    new_stories = []
    for name, story_type in NEW_STORY_ORDER:
        if name in existing:
            story = existing[name]
            story["story_type"] = story_type
            new_stories.append(story)
        else:
            new_stories.append(
                {
                    "name": name,
                    "story_type": story_type,
                    "acceptance_criteria": [],
                    "scenarios": [],
                }
            )

    kept_names = {n for n, _ in NEW_STORY_ORDER}
    dropped = [name for name in existing if name not in kept_names]
    for name in dropped:
        if name not in REMOVED:
            raise SystemExit(
                f"Unexpected story would be dropped: {name!r}. Add to NEW_STORY_ORDER or REMOVED."
            )

    group["stories"] = new_stories
    GRAPH.write_text(json.dumps(graph, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Updated {GRAPH} — sub-epic now has {len(new_stories)} stories.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
