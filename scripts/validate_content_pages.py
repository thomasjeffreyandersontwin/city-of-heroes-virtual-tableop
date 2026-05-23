#!/usr/bin/env python3
"""Validate content pages in increments 4-6 without rebuilding."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as builder
from drawio_tools import get_page, load_drawio, validate_layout

CONTENT = {
    4: ["Character Movement", "Memory Interface", "Movement Execution", "Camera Rig"],
    5: ["Roster", "Desktop Overlay", "Context Menu", "Pop-Up Menu", "Game State Query"],
    6: [
        "Crowd Move",
        "Attack Configuration",
        "Combat Execution",
        "Combat Geometry",
        "HCS Integration",
    ],
}


def main() -> int:
    all_ok = True
    for inc, names in CONTENT.items():
        path = ROOT / "docs" / f"increment-{inc}" / f"class-diagram-increment-{inc}.drawio"
        _, mx = load_drawio(path)
        print(f"=== Increment {inc} ===")
        for diagram in mx.findall("diagram"):
            name = diagram.get("name", "")
            if name not in names:
                continue
            _, root = get_page(mx, name)
            violations = validate_layout(root)
            if not violations:
                print(f"  {name}: PASS")
                continue
            all_ok = False
            counts = Counter(rule for rule, _ in violations)
            edge_ov = counts.get("edge_on_edge_overlap", 0)
            critical = sum(counts.get(r, 0) for r in builder.CRITICAL_RULES)
            print(
                f"  {name}: total={len(violations)} edge_ov={edge_ov} critical={critical}"
            )
    print("ALL PASS" if all_ok else "FAILURES REMAIN")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
