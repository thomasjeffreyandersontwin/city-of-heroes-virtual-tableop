#!/usr/bin/env python3
"""Test if building inc 1 before inc 2 causes Game Bridge failures."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as builder
from drawio_tools import get_page, load_drawio, validate_layout


def main() -> None:
    for run in range(1, 11):
        builder.build_diagram(builder.resolve_increment_source(1))
        out = builder.build_diagram(builder.resolve_increment_source(2))
        _, mx = load_drawio(out)
        _, root = get_page(mx, "Game Bridge")
        violations = validate_layout(root)
        if violations:
            counts = Counter(r for r, _ in violations)
            print(
                f"Run {run} FAIL: total={len(violations)} "
                f"edge_ov={counts.get('edge_on_edge_overlap', 0)}"
            )
        else:
            print(f"Run {run}: PASS")


if __name__ == "__main__":
    main()
