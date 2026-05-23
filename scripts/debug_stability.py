#!/usr/bin/env python3
"""Check build stability for failing pages."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as b
from drawio_tools import get_page, load_drawio, validate_layout


def check_page(inc: int, page: str) -> None:
    src = b.resolve_increment_source(inc)
    out = b.build_diagram(src)
    _, mx = load_drawio(out)
    _, root = get_page(mx, page)
    violations = validate_layout(root)
    counts = Counter(rule for rule, _ in violations)
    critical = sum(counts.get(r, 0) for r in b.CRITICAL_RULES)
    print(
        f"  {page}: total={len(violations)} critical={critical} "
        f"edge_ov={counts.get('edge_on_edge_overlap', 0)} shared={counts.get('shared_anchor', 0)}"
    )
    for rule, msg in violations[:5]:
        print(f"    [{rule}] {msg}")


def main() -> None:
    for run in range(1, 4):
        print(f"=== Run {run} ===")
        check_page(2, "Game Bridge")
        check_page(2, "KeyBind")


if __name__ == "__main__":
    main()
