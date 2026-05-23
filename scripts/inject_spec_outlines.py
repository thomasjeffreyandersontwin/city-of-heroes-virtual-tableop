#!/usr/bin/env python3
"""Parse Scenario Outlines from specification-by-example-increment-1.md
and inject them as scenario_outlines into docs/stories/story-graph.json.

Stories are matched by name (case-insensitive, with a few known aliases).
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
REPO_ROOT = Path(__file__).resolve().parent.parent
SPEC_FILE = REPO_ROOT / "docs" / "increment-1" / "specification-by-example-increment-1.md"
GRAPH_FILE = REPO_ROOT / "docs" / "stories" / "story-graph.json"

# ---------------------------------------------------------------------------
# Known name aliases: spec name -> graph name
# ---------------------------------------------------------------------------
NAME_ALIASES = {
    "browse and activate crowd files": "browse and activate crowd files",
    "load active crowd files on startup": "load crowd collection from repository",
    "track source file per crowd": "save crowd collection to repository",
    "save dirty crowds to source files": "save crowd collection to repository",
    "save crowd to new file": "save crowd collection to repository",
    "browse crowds by concept": "browse crowds by concept (animals, armed forces, civilians, vehicles, etc.)",
    "browse crowds by gangs, crews, and squads": "browse crowds by gangs, crews, and squads",
    "browse crowds by coh structure": "browse crowds by coh structure",
    "browse all characters crowd": "browse all characters crowd",
    "serialize crowd collection to json": "serialize crowd collection to json",
    "deserialize crowd collection from json": "deserialize crowd collection from json",
    "create daily backup of crowd repository": "create daily backup of crowd repository",
    "store crowd repository in coh data directory": "store crowd repository in coh data directory",
    "filter characters by name": "filter characters by name",
    "create crowd": "create crowd",
    "rename crowd": "rename crowd",
    "delete crowd": "delete crowd",
    "nest crowd inside crowd": "nest crowd inside crowd",
    "create character in crowd": "create character in crowd",
    "rename character": "rename character",
    "delete character from crowd": "delete character from crowd",
    "clone character": "clone character",
    "cut character to clipboard": "cut character to clipboard",
    "link character across crowds": "link character across crowds",
    "clone-link character": "clone-link character",
    "flatten-copy crowd into numbered characters": "flatten-copy crowd into numbered characters",
    "clone memberships to another crowd": "clone memberships to another crowd",
}


# ---------------------------------------------------------------------------
# Parse tables from markdown text
# ---------------------------------------------------------------------------
def parse_table(block: str) -> tuple[list[str], list[list[str]]]:
    """Parse a markdown pipe table. Returns (columns, rows)."""
    lines = [ln.strip() for ln in block.strip().splitlines() if ln.strip()]
    table_lines = [ln for ln in lines if ln.startswith("|")]
    if len(table_lines) < 2:
        return [], []

    def split_row(ln: str) -> list[str]:
        parts = [c.strip() for c in ln.strip("|").split("|")]
        return parts

    header = split_row(table_lines[0])
    rows = []
    for ln in table_lines[2:]:  # skip separator row
        row = split_row(ln)
        # Pad / trim to header width
        while len(row) < len(header):
            row.append("")
        rows.append(row[: len(header)])
    return header, rows


# ---------------------------------------------------------------------------
# Parse spec markdown
# ---------------------------------------------------------------------------
def parse_spec(text: str) -> dict[str, list[dict]]:
    """Return {story_name_lower: [outline_dict, ...]}."""
    result: dict[str, list[dict]] = {}
    current_story: str | None = None
    lines = text.splitlines()
    i = 0

    while i < len(lines):
        line = lines[i]

        # Story section header
        m = re.match(r"^## Story:\s+(.+)$", line)
        if m:
            current_story = m.group(1).strip().lower()
            if current_story not in result:
                result[current_story] = []
            i += 1
            continue

        # Scenario Outline header
        m = re.match(r"^### Scenario Outline:\s+(.+)$", line)
        if m and current_story is not None:
            outline_name = m.group(1).strip()
            i += 1

            # Collect everything until next ## Story or ### Scenario (outline or plain) or EOF
            segment_lines: list[str] = []
            while i < len(lines):
                peek = lines[i]
                if re.match(r"^## Story:", peek) or re.match(r"^### Scenario", peek):
                    break
                segment_lines.append(peek)
                i += 1

            segment = "\n".join(segment_lines)

            # Extract gherkin steps (inside ```gherkin ... ```)
            gherkin_match = re.search(r"```gherkin\s*\n(.*?)```", segment, re.DOTALL)
            steps: list[str] = []
            if gherkin_match:
                for step_line in gherkin_match.group(1).splitlines():
                    stripped = step_line.strip()
                    if stripped:
                        steps.append(stripped)

            # Extract named tables: **TableName** (... label ...): followed by pipe rows
            tables: list[dict] = []
            table_pattern = re.compile(
                r"\*\*([A-Za-z][A-Za-z0-9]*)\*\*\s*\([^)]*\)[^:\n]*:?\s*\n((?:\s*\|[^\n]+\n?)+)",
                re.MULTILINE,
            )
            for tm in table_pattern.finditer(segment):
                tname = tm.group(1)
                table_block = tm.group(2)
                cols, rows = parse_table(table_block)
                if cols:
                    tables.append({"name": tname, "columns": cols, "rows": rows})

            if tables or steps:
                result[current_story].append(
                    {
                        "name": outline_name,
                        "type": "outline",
                        "steps": steps,
                        "examples": tables,
                    }
                )
            continue

        i += 1

    return result


# ---------------------------------------------------------------------------
# Match spec story name to graph story name
# ---------------------------------------------------------------------------
def resolve_graph_name(spec_name: str) -> str:
    return NAME_ALIASES.get(spec_name.lower(), spec_name.lower())


def find_story_in_graph(graph: dict, target_lower: str) -> dict | None:
    """Walk the graph and return the story dict whose name matches target_lower."""

    def _walk(node: dict) -> dict | None:
        for epic in node.get("epics", []):
            hit = _search_epic(epic)
            if hit:
                return hit
        return None

    def _search_epic(epic: dict) -> dict | None:
        for story in _stories_in(epic):
            if story.get("name", "").lower() == target_lower:
                return story
        return None

    def _stories_in(node: dict) -> list[dict]:
        stories: list[dict] = []
        for sg in node.get("story_groups", []):
            stories.extend(sg.get("stories", []))
        for se in node.get("sub_epics", []):
            stories.extend(_stories_in(se))
        stories.extend(node.get("stories", []))
        return stories

    return _walk(graph)


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
def main() -> int:
    if not SPEC_FILE.is_file():
        print(f"ERROR: spec not found: {SPEC_FILE}", file=sys.stderr)
        return 1
    if not GRAPH_FILE.is_file():
        print(f"ERROR: graph not found: {GRAPH_FILE}", file=sys.stderr)
        return 1

    spec_text = SPEC_FILE.read_text(encoding="utf-8")
    graph = json.loads(GRAPH_FILE.read_text(encoding="utf-8"))

    outlines_by_story = parse_spec(spec_text)

    matched = 0
    unmatched: list[str] = []

    for spec_name, outlines in outlines_by_story.items():
        if not outlines:
            continue
        graph_name = resolve_graph_name(spec_name)
        story_node = find_story_in_graph(graph, graph_name)
        if story_node is None:
            # Try direct match
            story_node = find_story_in_graph(graph, spec_name)
        if story_node is None:
            unmatched.append(spec_name)
            print(f"  [SKIP] '{spec_name}' — no graph story found for '{graph_name}'")
            continue

        story_node["scenario_outlines"] = outlines
        matched += 1
        print(f"  [OK]   '{story_node['name']}' <- {len(outlines)} outline(s) from spec '{spec_name}'")
        for o in outlines:
            tables_desc = ", ".join(t["name"] for t in o["examples"])
            print(f"           outline: '{o['name']}' — tables: [{tables_desc}]")

    GRAPH_FILE.write_text(json.dumps(graph, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"\nUpdated {matched} stories. Unmatched: {len(unmatched)}")
    if unmatched:
        print("Unmatched spec stories:")
        for n in unmatched:
            print(f"  - {n}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
