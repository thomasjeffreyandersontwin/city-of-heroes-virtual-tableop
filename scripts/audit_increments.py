#!/usr/bin/env python3
"""Rebuild increment diagrams and report per-page violation counts."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as builder
from drawio_tools import get_page, load_drawio, validate_layout


def audit_increment(inc: int) -> bool:
    src = builder.resolve_increment_source(inc)
    out = builder.build_diagram(src)
    _, mx = load_drawio(out)
    print(f"=== Increment {inc} ({out.name}) ===", flush=True)
    all_ok = True
    for diagram in mx.findall("diagram"):
        name = diagram.get("name", "")
        _, page_root = get_page(mx, name)
        violations = validate_layout(page_root)
        if not violations:
            print(f"  {name}: PASS")
            continue
        all_ok = False
        counts = Counter(rule for rule, _ in violations)
        shared = counts.get("shared_anchor", 0)
        edge_ov = counts.get("edge_on_edge_overlap", 0)
        critical = sum(counts.get(r, 0) for r in builder.CRITICAL_RULES)
        total = len(violations)
        print(
            f"  {name}: shared={shared} edge_ov={edge_ov} "
            f"critical={critical} total={total}"
        )
    return all_ok


def main() -> int:
    ok = True
    for inc in (4, 5, 6):
        if not audit_increment(inc):
            ok = False
    print("ALL PASS" if ok else "FAILURES REMAIN")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
