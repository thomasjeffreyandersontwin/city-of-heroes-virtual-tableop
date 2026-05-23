#!/usr/bin/env python3
"""Rebuild specific diagram pages and report violations."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

import domain_diagram_builder as builder
from drawio_tools import get_page, load_drawio, save_drawio, validate_layout

TARGETS = {
    4: ["Movement Execution"],
    6: ["Attack Configuration", "HCS Integration"],
}


def main() -> int:
    for inc, pages in TARGETS.items():
        src = builder.resolve_increment_source(inc)
        out = src.parent / f"class-diagram-increment-{inc}.drawio"
        kas = builder.parse_source(src)
        index = builder._concept_index(kas)
        _, mx = load_drawio(out)
        for ka in kas:
            if ka.name in pages:
                print(f"Rebuilding inc {inc}: {ka.name}...", flush=True)
                builder._build_page(mx, ka, kas, index)
        save_drawio(out, mx)
        _, mx = load_drawio(out)
        for name in pages:
            _, root = get_page(mx, name)
            violations = validate_layout(root)
            counts = Counter(rule for rule, _ in violations)
            if not violations:
                print(f"  {name}: PASS", flush=True)
                continue
            edge_ov = counts.get("edge_on_edge_overlap", 0)
            critical = sum(counts.get(r, 0) for r in builder.CRITICAL_RULES)
            print(
                f"  {name}: total={len(violations)} edge_ov={edge_ov} critical={critical}",
                flush=True,
            )
            for rule, msg in violations[:8]:
                print(f"    {rule}: {msg}", flush=True)
    return 0


if __name__ == "__main__":
    sys.exit(main())
