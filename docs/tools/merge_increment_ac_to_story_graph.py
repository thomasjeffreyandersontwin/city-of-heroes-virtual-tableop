#!/usr/bin/env python3
"""Merge docs/increment-N/acceptance-criteria-increment-N.md into story-graph.json.

Uses **story-graph-ops** validated save (same as story_graph_cli). Parses CoH format:
  ## Activity
  ---
  ### Story Name
  (optional **Domain terms** block)
  1. WHEN ... / 1. **WHEN** ...

Run from repo root or any cwd:
  python docs/stories/tools/merge_increment_ac_to_story_graph.py

After merging AC, regenerate **one Draw.io per increment** (thin-slice lane):
  python docs/stories/tools/render_increment_acceptance_diagrams.py

Optional aggregate ``docs/stories/acceptance-criteria.drawio`` (all lanes): use drawio-story-sync
``render --mode acceptance-criteria`` on the full ``story-graph.json``.

Requires story-graph-ops on PYTHONPATH (script adds sibling paths when agilebydesign-skills is checked out).
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

# Project docs root: .../docs
_DOCS = Path(__file__).resolve().parents[2]
_PROJECT = _DOCS.parent
_STORIES = _DOCS / "stories"
_GRAPH = _STORIES / "story-graph.json"

# Prefer bundled story_graph_file from a checkout of agilebydesign-skills
_CANDIDATE_OPS = [
    _PROJECT.parent / "agilebydesign-skills" / "skills" / "story-driven-delivery" / "story-graph-ops" / "scripts",
    Path(r"C:\dev\agilebydesign-skills\skills\story-driven-delivery\story-graph-ops\scripts"),
]


def _ensure_ops_path() -> Path | None:
    for p in _CANDIDATE_OPS:
        if (p / "story_graph_file.py").is_file():
            if str(p) not in sys.path:
                sys.path.insert(0, str(p))
            return p
    return None


_STORY_SECTION = re.compile(r"^###\s+(.+?)\s*$", re.MULTILINE)
_H2_SECTION = re.compile(r"^##\s+(.+?)\s*$", re.MULTILINE)
_NUMBERED_START = re.compile(r"^\d+\.\s", re.MULTILINE)


# Markdown headings sometimes shorten names that differ from story-graph.json.
_TITLE_ALIASES: dict[str, str] = {
    "Add Default Abilities to Character": (
        "Add Default Abilities to Character (Recovery, Stun Recovery, Pass Turn, Half Phase Action, "
        "Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, "
        "Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power)"
    ),
}


def _canonical_story_title(raw: str) -> str:
    """Strip optional ``Story:`` prefix from a heading line."""
    t = raw.strip()
    if t.lower().startswith("story:"):
        t = t[6:].strip()
    return t


def _resolve_graph_story_name(raw: str) -> str:
    """Map markdown heading to exact story ``name`` in ``story-graph.json``."""
    t = _canonical_story_title(raw)
    return _TITLE_ALIASES.get(t, t)


def _split_numbered_items(block: str) -> list[str]:
    boundaries = [m.start() for m in _NUMBERED_START.finditer(block)]
    if not boundaries:
        return []
    items: list[str] = []
    for i, start in enumerate(boundaries):
        end = boundaries[i + 1] if i + 1 < len(boundaries) else len(block)
        raw = block[start:end].strip()
        # Normalize internal newlines to spaces for compact diagram cells (optional)
        inner = " ".join(line.strip() for line in raw.splitlines() if line.strip())
        if inner:
            items.append(inner)
    return items


def _next_h2_or_h3(rest: str) -> re.Match | None:
    """First of next ### or ## heading in *rest* (same file order)."""
    matches: list[re.Match[str]] = []
    a = re.search(r"^###\s+", rest, re.MULTILINE)
    b = re.search(r"^##\s+", rest, re.MULTILINE)
    if a:
        matches.append(a)
    if b:
        matches.append(b)
    if not matches:
        return None
    return min(matches, key=lambda m: m.start())


def _parse_h3_stories(text: str) -> dict[str, list[str]]:
    """Increment 1, 2, 4, 6 style: ### Story title (optional 'Story:' prefix)."""
    result: dict[str, list[str]] = {}
    for m in _STORY_SECTION.finditer(text):
        title = _resolve_graph_story_name(m.group(1))
        if not title:
            continue
        start = m.end()
        rest = text[start:]
        end_match = _next_h2_or_h3(rest)
        if end_match:
            block = rest[: end_match.start()]
        else:
            block = rest
        items = _split_numbered_items(block)
        if items:
            result[title] = items
    return result



def _parse_h2_stories(text: str) -> dict[str, list[str]]:
    """Increment 3, 5 style: each story is ## Title or ## Story: Title (no ###)."""
    result: dict[str, list[str]] = {}
    matches = list(_H2_SECTION.finditer(text))
    for i, m in enumerate(matches):
        title = _resolve_graph_story_name(m.group(1))
        if not title:
            continue
        start = m.end()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(text)
        block = text[start:end]
        items = _split_numbered_items(block)
        if items:
            result[title] = items
    return result


def parse_increment_ac_md(text: str) -> dict[str, list[str]]:
    """Return {story_title: [ac_string, ...]}.

    If the file uses any ### headings, only ### blocks are treated as stories
    (## are activity groupings). Otherwise only ## blocks are stories.
    """
    if re.search(r"^###\s+", text, re.MULTILINE):
        return _parse_h3_stories(text)
    return _parse_h2_stories(text)


def _all_stories(graph: dict) -> list[dict]:
    stories: list[dict] = []
    for epic in graph.get("epics", []):
        stories.extend(_stories_from_node(epic))
    return stories


def _stories_from_node(node: dict) -> list[dict]:
    stories: list[dict] = []
    for sg in node.get("story_groups", []):
        stories.extend(sg.get("stories", []))
    for sub in node.get("sub_epics", []):
        stories.extend(_stories_from_node(sub))
    return stories


def inject_ac(graph: dict, ac_by_story: dict[str, list[str]]) -> tuple[int, list[str], list[str]]:
    """Set acceptance_criteria (list of strings) on matching stories."""
    by_name = {s["name"]: s for s in _all_stories(graph) if s.get("name")}
    updated = 0
    for name, ac_list in ac_by_story.items():
        if name in by_name:
            by_name[name]["acceptance_criteria"] = list(ac_list)
            updated += 1
    matched = set(ac_by_story) & set(by_name.keys())
    unmatched_md = [n for n in ac_by_story if n not in by_name]
    unmatched_graph = []  # reserved
    return updated, unmatched_md, list(matched)


def main() -> int:
    ops = _ensure_ops_path()
    if not ops:
        print(
            "[ERROR] Could not find story-graph-ops scripts (story_graph_file.py). "
            "Set PYTHONPATH or clone agilebydesign-skills next to the project.",
            file=sys.stderr,
        )
        return 1

    from story_graph_file import load_story_graph_dict, save_story_graph_dict

    inc_dirs = sorted(_DOCS.glob("increment-*"))
    ac_files: list[Path] = []
    for d in inc_dirs:
        if not d.is_dir():
            continue
        m = re.match(r"increment-(\d+)$", d.name)
        if not m:
            continue
        n = m.group(1)
        f = d / f"acceptance-criteria-increment-{n}.md"
        if f.is_file():
            ac_files.append(f)

    if not ac_files:
        print(f"[ERROR] No acceptance-criteria-increment-*.md under {_DOCS / 'increment-*'}", file=sys.stderr)
        return 1

    combined: dict[str, list[str]] = {}
    for md_path in ac_files:
        text = md_path.read_text(encoding="utf-8")
        part = parse_increment_ac_md(text)
        print(f"[PARSE] {md_path.relative_to(_PROJECT)}: {len(part)} stories")
        for k, v in part.items():
            combined[k] = v  # later files override if duplicate

    graph = load_story_graph_dict(_GRAPH)
    updated, unmatched, _ = inject_ac(graph, combined)
    print(f"[MERGE] Injected AC for {updated} stories ({len(combined)} story blocks in markdown)")

    save_story_graph_dict(_GRAPH, graph)
    print(f"[WRITE] {_GRAPH}")

    if unmatched:
        print(f"\n[WARN] {len(unmatched)} markdown story title(s) not found in graph:", file=sys.stderr)
        for n in unmatched[:40]:
            print(f"  - {n}", file=sys.stderr)
        if len(unmatched) > 40:
            print(f"  ... and {len(unmatched) - 40} more", file=sys.stderr)
        return 1

    print("[OK] All merged story names matched the graph.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
