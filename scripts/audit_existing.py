#!/usr/bin/env python3
"""Audit existing drawio files without rebuild."""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"))

from drawio_tools import get_page, load_drawio, validate_layout  # noqa: E402

CRITICAL = frozenset({"class_overlap", "edge_crosses_class", "hierarchy_flow"})


def main() -> int:
    rc = 0
    for inc in (4, 5, 6):
        path = ROOT / f"docs/increment-{inc}/class-diagram-increment-{inc}.drawio"
        if not path.exists():
            print(f"Inc {inc}: missing {path}")
            rc = 1
            continue
        _, mx = load_drawio(path)
        print(f"=== Increment {inc} (existing) ===")
        for diagram in mx.findall("diagram"):
            name = diagram.get("name", "")
            _, root = get_page(mx, name)
            violations = validate_layout(root)
            if not violations:
                print(f"  {name}: PASS")
                continue
            rc = 1
            counts = Counter(rule for rule, _ in violations)
            sa = counts.get("shared_anchor", 0)
            eo = counts.get("edge_on_edge_overlap", 0)
            crit = sum(counts.get(r, 0) for r in CRITICAL)
            print(f"  {name}: sa={sa} eo={eo} crit={crit} tot={len(violations)}")
    return rc


if __name__ == "__main__":
    sys.exit(main())
