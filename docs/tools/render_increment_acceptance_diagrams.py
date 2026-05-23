#!/usr/bin/env python3
"""Render one exploration (acceptance-criteria) Draw.io per increment from story-graph.json.

Slices the graph per thin-slice lane with **StoryGraphFilter** (story-graph-ops), then
**DrawIOSynchronizer** render-exploration — same renderer as aggregate ``acceptance-criteria.drawio``,
but one file per priority under ``docs/increment-N/``.

Run from repo root or any cwd:
  python docs/stories/tools/render_increment_acceptance_diagrams.py

Requires **story-graph-ops** ``scripts`` and **drawio-story-sync** ``scripts`` on the import path;
the script adds the usual agilebydesign-skills locations when present.
"""
from __future__ import annotations

import copy
import json
import sys
import tempfile
from pathlib import Path

_DOCS = Path(__file__).resolve().parents[1]
_PROJECT = _DOCS.parent
_STORIES = _DOCS / "stories"
_GRAPH = _STORIES / "story-graph.json"

_CANDIDATE_OPS = [
    _PROJECT.parent / "agilebydesign-skills" / "skills" / "story-driven-delivery" / "story-graph-ops" / "scripts",
    Path(r"C:\dev\agilebydesign-skills\skills\story-driven-delivery\story-graph-ops\scripts"),
]
_CANDIDATE_DRAWIO = [
    _PROJECT.parent / "agilebydesign-skills" / "skills" / "story-driven-delivery" / "drawio-story-sync" / "scripts",
    Path(r"C:\dev\agilebydesign-skills\skills\story-driven-delivery\drawio-story-sync\scripts"),
]


def _prepend_scripts_dir(candidates: list[Path], proof: Path) -> None:
    """Insert the first candidate ``scripts`` root that contains ``proof`` relative to it."""
    for root in candidates:
        if proof.is_absolute():
            raise ValueError("proof must be relative to candidate root")
        if (root / proof).exists() and root.is_dir():
            s = str(root)
            if s not in sys.path:
                sys.path.insert(0, s)
            return


def main() -> int:
    if not _GRAPH.is_file():
        print(f"[ERR] Missing {_GRAPH}", file=sys.stderr)
        return 2

    _prepend_scripts_dir(_CANDIDATE_OPS, Path("story_graph_file.py"))
    _prepend_scripts_dir(_CANDIDATE_DRAWIO, Path("drawio_story_sync/story_io_synchronizer.py"))

    from story_graph_file import load_story_graph_dict
    from story_graph_ops.story_graph_scope import StoryGraphFilter
    from drawio_story_sync.story_io_synchronizer import DrawIOSynchronizer

    full_graph = load_story_graph_dict(_GRAPH)
    increments = sorted(
        full_graph.get("increments") or [],
        key=lambda inc: int(inc.get("priority", 0) or 0),
    )
    if not increments:
        print("[ERR] story-graph.json has no increments[] entries.", file=sys.stderr)
        return 2

    sync = DrawIOSynchronizer()

    for inc in increments:
        priority = int(inc.get("priority", 0) or 0)
        name = (inc.get("name") or "").strip()
        if not name or priority < 1:
            print(f"[SKIP] Increment missing name or priority: {inc!r}", file=sys.stderr)
            continue

        out_dir = _DOCS / f"increment-{priority}"
        out_path = out_dir / f"acceptance-criteria-increment-{priority}.drawio"

        filt = StoryGraphFilter(increments=[name])
        slice_graph = filt.filter_story_graph(copy.deepcopy(full_graph))

        with tempfile.NamedTemporaryFile(
            mode="w",
            suffix=".json",
            delete=False,
            encoding="utf-8",
        ) as tmp:
            json.dump(slice_graph, tmp, ensure_ascii=False)
            tmp_path = Path(tmp.name)

        try:
            load_story_graph_dict(tmp_path)
        except Exception as e:
            print(f"[ERR] Sliced graph failed validation for {name!r}: {e}", file=sys.stderr)
            tmp_path.unlink(missing_ok=True)
            return 3

        try:
            out_dir.mkdir(parents=True, exist_ok=True)
            sync.render(tmp_path, out_path, renderer_command="render-exploration")
            print(f"[OK] increment-{priority} -> {out_path.relative_to(_PROJECT)}")
        finally:
            tmp_path.unlink(missing_ok=True)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
