"""Sync acceptance-criteria-increment-1.md AC arrays into story-graph.json
for the five Manage Crowd Repository stories changed in this feature.

Run from the workspace root:

    python docs/stories/tools/sync_crowd_repo_ac.py
"""

from __future__ import annotations

import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
GRAPH = ROOT / "stories" / "story-graph.json"
AC_FILE = ROOT / "increment-1" / "acceptance-criteria-increment-1.md"

TARGET_STORIES = [
    "Browse and Activate Crowd Files",
    "Load Active Crowd Files on Startup",
    "Track Source File per Crowd",
    "Save Dirty Crowds to Source Files",
    "Save Crowd to New File",
]


def extract_ac(ac_text: str, story_name: str) -> list[str]:
    """Pull the numbered AC under '### <story_name>' until the next '### '
    or '---' separator, returning them as flat single-line strings."""
    pattern = re.compile(
        rf"^###\s+{re.escape(story_name)}\s*\n(.*?)(?=^---\s*$|^###\s+)",
        re.DOTALL | re.MULTILINE,
    )
    match = pattern.search(ac_text)
    if not match:
        raise SystemExit(f"AC section not found: {story_name!r}")
    body = match.group(1).strip()

    blocks: list[str] = []
    current: list[str] = []
    for raw in body.splitlines():
        line = raw.rstrip()
        if not line.strip():
            if current:
                blocks.append(" ".join(current).strip())
                current = []
            continue
        if re.match(r"^\d+\.\s", line) and current:
            blocks.append(" ".join(current).strip())
            current = []
        current.append(line.strip())
    if current:
        blocks.append(" ".join(current).strip())

    return [normalize_block(b) for b in blocks if b]


def normalize_block(block: str) -> str:
    return re.sub(r"\s+", " ", block).strip()


def find_story(graph: dict, name: str) -> dict:
    for epic in graph["epics"]:
        for sub in epic.get("sub_epics", []):
            for group in sub.get("story_groups", []):
                for story in group.get("stories", []):
                    if story["name"] == name:
                        return story
    raise SystemExit(f"Story not found in graph: {name!r}")


def main() -> int:
    ac_text = AC_FILE.read_text(encoding="utf-8")
    graph = json.loads(GRAPH.read_text(encoding="utf-8"))

    for story_name in TARGET_STORIES:
        criteria = extract_ac(ac_text, story_name)
        if not criteria:
            raise SystemExit(f"No AC extracted for {story_name!r}")
        story = find_story(graph, story_name)
        story["acceptance_criteria"] = criteria
        print(f"  {story_name}: {len(criteria)} AC")

    GRAPH.write_text(json.dumps(graph, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Updated {GRAPH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
