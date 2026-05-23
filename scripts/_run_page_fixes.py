#!/usr/bin/env python3
"""One-off: fix routing on 4 diagram pages only."""
from __future__ import annotations

import sys
from pathlib import Path

repo = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(repo / "scripts"))

from fix_class_diagram_layouts import (  # noqa: E402
    fix_desktop_overlay_page,
    fix_game_state_query_page,
    fix_keybind_page,
    fix_roster_page,
)
from fix_class_diagrams import process_file  # noqa: E402
import xml.etree.ElementTree as ET  # noqa: E402

INC2 = repo / "docs/increment-2/class-diagram-increment-2.drawio"
INC5 = repo / "docs/increment-5/class-diagram-increment-5.drawio"

INC5_SKIP = {
    "Pop-Up Menu",
    "Context Menu",
    "Character",
    "Crowd",
    "Spawned NPC",
    "Game Bridge",
    "page_pop-up_menu",
    "page_context_menu",
    "page_character",
    "page_crowd",
    "page_spawned_npc",
    "page_game_bridge",
}
INC2_SKIP = {
    "Identity",
    "Game Bridge",
    "Costume File",
    "Ghost Shadow",
    "Model",
    "page_identity",
    "page_game_bridge",
    "page_costume_file",
    "page_ghost_shadow",
    "page_model",
}

LAYOUT_FIXERS = {
    "KeyBind": fix_keybind_page,
    "Roster": fix_roster_page,
    "Desktop Overlay": fix_desktop_overlay_page,
    "Game State Query": fix_game_state_query_page,
}


def apply_layout_fixers(path: Path, page_names: set[str]) -> None:
    tree = ET.parse(path)
    mxfile = tree.getroot()
    for page_name, fixer in LAYOUT_FIXERS.items():
        if page_name not in page_names:
            continue
        for diagram in mxfile.findall("diagram"):
            if diagram.get("name") == page_name:
                fixer(diagram)
                print(f"  layout pass: {page_name}")
                break
    tree.write(path, encoding="unicode", xml_declaration=False)


def main() -> None:
    print("Step 1: semantic layout + waypoint strip")
    for r in process_file(str(INC5), INC5_SKIP):
        print(f"  inc5 {r.get('page_name')}: {'skip' if r.get('skipped') else 'ok'}")
    for r in process_file(str(INC2), INC2_SKIP):
        print(f"  inc2 {r.get('page_name')}: {'skip' if r.get('skipped') else 'ok'}")

    print("Step 2: targeted anchor/layout fixes")
    apply_layout_fixers(INC2, {"KeyBind"})
    apply_layout_fixers(INC5, {"Roster", "Desktop Overlay", "Game State Query"})


if __name__ == "__main__":
    main()
