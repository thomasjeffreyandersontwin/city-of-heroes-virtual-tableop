#!/usr/bin/env python3
"""Run audit until Game Bridge fails, then print violations."""
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
        src = builder.resolve_increment_source(2)
        out = builder.build_diagram(src)
        _, mx = load_drawio(out)
        _, root = get_page(mx, "Game Bridge")
        violations = validate_layout(root)
        if violations:
            print(f"FAIL on run {run}: {len(violations)} violations")
            counts = Counter(r for r, _ in violations)
            print(counts)
            for rule, msg in violations:
                print(f"  [{rule}] {msg}")
            return
        print(f"Run {run}: PASS")
    print("All 10 runs PASS")


if __name__ == "__main__":
    main()
