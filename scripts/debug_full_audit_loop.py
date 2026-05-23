#!/usr/bin/env python3
"""Replicate full audit loop until any page fails."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as builder
from drawio_tools import get_page, load_drawio, validate_layout


def audit_once() -> list[tuple[int, str, int]]:
    fails = []
    for inc in (1, 2, 3):
        out = builder.build_diagram(builder.resolve_increment_source(inc))
        _, mx = load_drawio(out)
        for diagram in mx.findall("diagram"):
            name = diagram.get("name", "")
            _, root = get_page(mx, name)
            v = validate_layout(root)
            if v:
                fails.append((inc, name, len(v)))
    return fails


def main() -> None:
    for run in range(1, 31):
        fails = audit_once()
        if fails:
            print(f"Run {run} FAIL:")
            for inc, name, n in fails:
                print(f"  inc{inc} {name}: {n} violations")
            src = builder.resolve_increment_source(fails[0][0])
            out = builder.build_diagram(src)
            _, mx = load_drawio(out)
            _, root = get_page(mx, fails[0][1])
            for rule, msg in validate_layout(root)[:8]:
                print(f"    [{rule}] {msg}")
            return
    print("30 consecutive full audits PASS")


if __name__ == "__main__":
    main()
