"""Export story-graph.json to story-map.md tree format.

Usage:
    python export_story_map.py <story-graph.json> <story-map.md>
"""
import json
import sys
from pathlib import Path


def render_node(node: dict, depth: int, lines: list[str]):
    indent = "    " * depth
    for sg in node.get("story_groups", []):
        for story in sg.get("stories", []):
            actor = story.get("story_type", "System")
            lines.append(f"{indent}(S) {actor} --> {story['name']}")
    for se in node.get("sub_epics", []):
        lines.append(f"{indent}(E) {se['name']}")
        render_node(se, depth + 1, lines)


def export(graph_path: Path, md_path: Path):
    g = json.loads(graph_path.read_text(encoding="utf-8"))

    # Preserve preamble from existing file if present
    preamble = []
    if md_path.exists():
        for line in md_path.read_text(encoding="utf-8").splitlines():
            if line.startswith("(E) "):
                break
            preamble.append(line)
        # trim trailing blank lines from preamble
        while preamble and not preamble[-1].strip():
            preamble.pop()

    lines = preamble + [""]
    for epic in g.get("epics", []):
        lines.append(f"(E) {epic['name']}")
        render_node(epic, 1, lines)
        lines.append("")

    md_path.write_text("\n".join(lines), encoding="utf-8")
    print(f"Written {len(g['epics'])} epics to {md_path}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: export_story_map.py <story-graph.json> <story-map.md>", file=sys.stderr)
        sys.exit(1)
    export(Path(sys.argv[1]), Path(sys.argv[2]))
