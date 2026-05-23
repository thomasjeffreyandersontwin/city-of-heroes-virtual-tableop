#!/usr/bin/env python3
"""
Build Draw.io class diagrams from object-model or CRC markdown sources.

Usage:
  python scripts/domain_diagram_builder.py --increment 1
  python scripts/domain_diagram_builder.py --source docs/increment-1/object-model-increment-1.md
  python scripts/domain_diagram_builder.py --all-increments
"""

from __future__ import annotations

import argparse
import math
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

REPO_ROOT = Path(__file__).resolve().parent.parent
SKILL_SCRIPTS = REPO_ROOT / ".cursor" / "skills" / "drawio-domain-sync" / "scripts"
if str(SKILL_SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SKILL_SCRIPTS))

from drawio_tools import (  # noqa: E402
    CELL_MIN_HEIGHT,
    CELL_WIDTH,
    add_page,
    audit_diagram_report,
    calc_cell_height,
    create_class_cell,
    create_edge,
    create_empty_mxfile,
    find_cell_by_name,
    get_page,
    load_drawio,
    save_drawio,
    set_edge_anchors,
    validate_layout,
)

COL_GAP = 200
ROW_GAP = 240
START_X = 60
START_Y = 60
OVERLAP_PAD = 60
ROUTE_CORRIDOR_GAP = 90
ANCHOR_STEP = 0.08
ANCHOR_START = 0.08
LANE_STEP = 44
IMPORT_BAND_MAX_Y = 200
IMPORT_BAND_MAX_COLS = 4
MAX_OUTER_MARGIN = 240
_OUTER_SLOTS = 32
CRITICAL_RULES = frozenset({"class_overlap", "edge_crosses_class", "hierarchy_flow"})
SECONDARY_RULES = frozenset({"shared_anchor", "edge_on_edge_overlap"})
EDGE_SEPARATION = 14
LANE_MODULO = 64
_OUTER_FIRST_KINDS = ("outer", "outer_bottom", "gutter", "between", "local")


def _lane_y_offset(lane_idx: int) -> float:
    """Vertical separation between parallel horizontal edge corridors (>= 16px)."""
    return float((lane_idx % 96) + 1) * (EDGE_SEPARATION + 2)


def _outer_margin(lane_idx: int) -> int:
    """Bounded distance outside the diagram for outer bus / side corridors."""
    idx = lane_idx % _OUTER_SLOTS
    if _OUTER_SLOTS <= 1:
        return 36
    step = max(EDGE_SEPARATION + 2, (MAX_OUTER_MARGIN - 36) // max(1, _OUTER_SLOTS - 1))
    return min(36 + idx * step, MAX_OUTER_MARGIN)


def _corridor_offset(lane_idx: int) -> int:
    """Unique bounded corridor offset (legacy alias for outer margin)."""
    return _outer_margin(lane_idx)


def _lane_spacing(lane_idx: int) -> Tuple[int, int]:
    """Tuple for between-column offsets; both values stay bounded."""
    idx = lane_idx % _OUTER_SLOTS
    n = (idx % LANE_MODULO) + 1
    extra = _outer_margin(lane_idx)
    return n, extra


def _bounded_lane(lane_idx: int) -> int:
    return lane_idx % _OUTER_SLOTS


def _is_straight_inheritance(style: str) -> bool:
    """True for plain inheritance edges (no orthogonal router)."""
    return (
        "endFill=0" in style
        and "endArrow=block" in style
        and "orthogonalEdgeStyle" not in style
    )
PRIMITIVE_COLLAB = re.compile(r"^\([^)]+\)$", re.I)

KA_HEADING = re.compile(r"^## \*\*(.+?)\*\*\s*$")
OM_CLASS = re.compile(r"^### \*\*(.+?)\*\*(?:\s*<<\s*(.+?)\s*>>)?\s*$")
CRC_CLASS = re.compile(r"^### \*\*(.+?)\*\*\s*$")
CRC_ROW = re.compile(r"^(.+?)\s*\|\s*(.+?)\s*$")

TYPE_IN_PROP = re.compile(
    r"(?:<<\s*(?:composition|aggregation)\s*>>\s*)?"
    r"(?:\w+\s*:\s*)?"
    r"(?:List<|Dictionary<[^,]+,\s*)?([A-Z][A-Za-z0-9 ]+?)(?:>|,|\)|$|\s)"
)


@dataclass
class Concept:
    name: str
    ka: str
    base: Optional[str] = None
    stereotype: Optional[str] = None
    properties: List[str] = field(default_factory=list)
    operations: List[str] = field(default_factory=list)
    invariants: List[str] = field(default_factory=list)
    collaborators: Set[str] = field(default_factory=set)


@dataclass
class KeyAbstraction:
    name: str
    concepts: List[Concept] = field(default_factory=list)


def _normalize_name(name: str) -> str:
    return name.strip()


def _parse_class_heading(raw: str) -> Tuple[str, Optional[str], Optional[str]]:
    """Return (display_name, base, stereotype) from 'Nested Crowd : Crowd' etc."""
    stereotype = None
    if " : " in raw and "<<" not in raw.split(" : ", 1)[0]:
        name_part, base = raw.split(" : ", 1)
        return _normalize_name(name_part), _normalize_name(base), stereotype
    return _normalize_name(raw), None, stereotype


def _extract_types_from_line(line: str) -> Set[str]:
    types: Set[str] = set()
    for m in re.finditer(r":\s*([A-Z][A-Za-z0-9 ]+?)(?:\(|<|,|\)|$|\s)", line):
        t = m.group(1).strip()
        if t not in ("String", "Boolean", "Integer", "Path", "Date", "Void", "Dictionary"):
            types.add(t)
    for m in re.finditer(r"(?:List|Dictionary)<[^>]*?([A-Z][A-Za-z0-9 ]+)", line):
        types.add(m.group(1).strip())
    for m in re.finditer(r"new\s+([A-Z][A-Za-z0-9 ]+)", line):
        types.add(m.group(1).strip())
    return types


def _is_skip_section(line: str) -> bool:
    s = line.strip()
    return s.startswith("### references") or s.startswith("### decisions") or s.startswith("```")


def parse_object_model(text: str) -> List[KeyAbstraction]:
    lines = text.split("\n")
    kas: List[KeyAbstraction] = []
    current_ka: Optional[KeyAbstraction] = None
    current: Optional[Concept] = None
    section = "preamble"  # preamble | ctor | props | ops

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        ka_m = KA_HEADING.match(stripped)
        if ka_m:
            if current and current_ka:
                current_ka.concepts.append(current)
                current = None
            current_ka = KeyAbstraction(name=ka_m.group(1).strip())
            kas.append(current_ka)
            section = "preamble"
            i += 1
            continue

        if current_ka is None:
            i += 1
            continue

        if _is_skip_section(stripped):
            while i < len(lines) and not (lines[i].startswith("## ") or lines[i].startswith("### **")):
                i += 1
            continue

        om_m = OM_CLASS.match(stripped)
        if om_m:
            if current:
                current_ka.concepts.append(current)
            raw_name = om_m.group(1)
            stereotype = om_m.group(2)
            name, base, _ = _parse_class_heading(raw_name)
            if stereotype is None:
                pass  # already set from heading if needed
            current = Concept(name=name, ka=current_ka.name, base=base, stereotype=stereotype)
            section = "ctor"
            i += 1
            continue

        if current is None or section == "preamble":
            i += 1
            continue

        if stripped == "------":
            section = "props"
            i += 1
            continue
        if stripped == "----":
            section = "ops"
            i += 1
            continue

        if stripped.startswith("### "):
            if current:
                current_ka.concepts.append(current)
                current = None
            i += 1
            continue

        if stripped.startswith("Invariant:") or stripped.startswith("\tInvariant:"):
            inv = stripped.replace("Invariant:", "").strip()
            if inv:
                current.invariants.append(inv)
            i += 1
            continue

        if stripped.startswith("Interaction:") or stripped.startswith("\t") and section == "ops":
            # Collect collaborator types from interaction block
            while i < len(lines):
                il = lines[i]
                if il.strip().startswith("+ ") or il.strip().startswith("- ") and ": " in il.strip():
                    if il.strip().startswith("+ ") or (il.strip().startswith("- ") and "(" in il.strip()):
                        break
                if il.strip() == "----" or OM_CLASS.match(il.strip()) or KA_HEADING.match(il.strip()):
                    break
                for t in _extract_types_from_line(il):
                    current.collaborators.add(t)
                i += 1
            continue

        if section == "props" and stripped.startswith("+ "):
            prop = stripped[2:].strip()
            current.properties.append(prop)
            for t in _extract_types_from_line(prop):
                current.collaborators.add(t)
        elif section == "ops" and (stripped.startswith("+ ") or stripped.startswith("- ")):
            op = stripped[2:].strip()
            if op and not op.startswith("Invariant"):
                current.operations.append(op)
                for t in _extract_types_from_line(op):
                    current.collaborators.add(t)

        i += 1

    if current and current_ka:
        current_ka.concepts.append(current)

    return kas


def parse_crc(text: str) -> List[KeyAbstraction]:
    lines = text.split("\n")
    kas: List[KeyAbstraction] = []
    current_ka: Optional[KeyAbstraction] = None
    current: Optional[Concept] = None

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        ka_m = KA_HEADING.match(stripped)
        if ka_m:
            if current and current_ka:
                current_ka.concepts.append(current)
                current = None
            current_ka = KeyAbstraction(name=ka_m.group(1).strip())
            kas.append(current_ka)
            i += 1
            continue

        if current_ka is None:
            i += 1
            continue

        if _is_skip_section(stripped):
            while i < len(lines) and not (lines[i].startswith("## ") or CRC_CLASS.match(lines[i].strip())):
                i += 1
            continue

        crc_m = CRC_CLASS.match(stripped)
        if crc_m:
            if current:
                current_ka.concepts.append(current)
            name = _normalize_name(crc_m.group(1))
            current = Concept(name=name, ka=current_ka.name)
            i += 1
            continue

        if current is None:
            i += 1
            continue

        row_m = CRC_ROW.match(stripped)
        if row_m and not stripped.startswith("|") and "|" in stripped:
            resp = row_m.group(1).strip()
            collab_raw = row_m.group(2).strip()
            if collab_raw.startswith("invariant:"):
                inv = collab_raw.replace("invariant:", "").strip()
                current.invariants.append(inv)
            else:
                row_label = resp
                collabs = []
                for part in re.split(r",(?![^(]*\))", collab_raw):
                    c = part.strip()
                    if c and not PRIMITIVE_COLLAB.match(c):
                        collabs.append(c)
                        current.collaborators.add(c)
                if collabs:
                    current.properties.append(f"{row_label} : {', '.join(collabs)}")
                elif resp:
                    current.properties.append(resp)
            i += 1
            continue

        if stripped.startswith("### "):
            if current:
                current_ka.concepts.append(current)
                current = None
            i += 1
            continue

        i += 1

    if current and current_ka:
        current_ka.concepts.append(current)

    return kas


def parse_source(path: Path) -> List[KeyAbstraction]:
    text = path.read_text(encoding="utf-8")
    if "object-model" in path.name or text.lstrip().startswith("---") and "state: domain-model" in text[:500]:
        return parse_object_model(text)
    return parse_crc(text)


def _concept_index(kas: List[KeyAbstraction]) -> Dict[str, Concept]:
    idx: Dict[str, Concept] = {}
    for ka in kas:
        for c in ka.concepts:
            idx[c.name] = c
    return idx


def _match_concept(name: str, index: Dict[str, Concept]) -> Optional[str]:
    if name in index:
        return name
    lower = name.lower()
    for k in index:
        if k.lower() == lower:
            return k
    # Crowd Tree vs CrowdTree etc.
    compact = name.replace(" ", "")
    for k in index:
        if k.replace(" ", "") == compact:
            return k
    return None


def _inheritance_depth(concept: Concept, index: Dict[str, Concept], memo: Dict[str, int]) -> int:
    if concept.name in memo:
        return memo[concept.name]
    if not concept.base:
        memo[concept.name] = 0
        return 0
    base_key = _match_concept(concept.base, index)
    if base_key is None:
        memo[concept.name] = 0
        return 0
    d = 1 + _inheritance_depth(index[base_key], index, memo)
    memo[concept.name] = d
    return d


def _estimate_height(c: Concept) -> int:
    return calc_cell_height(
        len(c.properties),
        min(len(c.operations), 12),
        min(len(c.invariants), 6),
    )


def _local_inheritance_layer(
    c: Concept,
    index: Dict[str, Concept],
    imported_names: Set[str],
    local_names: Set[str],
    memo: Dict[str, int],
) -> int:
    """Depth among local classes: 0 = root, +1 per local/imported base."""
    if c.name in memo:
        return memo[c.name]
    if not c.base:
        layer = 0
    else:
        bk = _match_concept(c.base, index)
        if bk is None:
            layer = 0
        elif bk in imported_names:
            layer = 1
        elif bk in local_names:
            layer = _local_inheritance_layer(index[bk], index, imported_names, local_names, memo) + 1
        else:
            layer = 0
    memo[c.name] = layer
    return layer


def _grid_columns(n: int) -> int:
    """Pick column count so dense pages stay readable left-to-right."""
    if n <= 1:
        return 1
    if n <= 4:
        return n
    if n <= 9:
        return 3
    if n <= 16:
        return 4
    if n <= 25:
        return 5
    if n <= 36:
        return 6
    if n <= 49:
        return 7
    return 8


def _spacing_for_page(n_classes: int) -> Tuple[int, int]:
    """Wider gaps on dense pages so edges can route in margins."""
    if n_classes >= 24:
        return 360, 400
    if n_classes >= 16:
        return 280, 320
    if n_classes >= 10:
        return 220, 280
    return COL_GAP, ROW_GAP


def _order_concepts_by_connectivity(
    concepts: List[Concept],
    index: Dict[str, Concept],
) -> List[Concept]:
    """Place highly-connected concepts adjacent in the grid."""
    if len(concepts) <= 1:
        return concepts
    names = {c.name for c in concepts}
    adj: Dict[str, Set[str]] = {c.name: set() for c in concepts}
    for c in concepts:
        for collab in c.collaborators:
            ck = _match_concept(collab, index)
            if ck and ck in names:
                adj[c.name].add(ck)
                adj[ck].add(c.name)
        if c.base:
            bk = _match_concept(c.base, index)
            if bk and bk in names:
                adj[c.name].add(bk)
                adj[bk].add(c.name)

    start = max(concepts, key=lambda c: len(adj[c.name])).name
    ordered: List[Concept] = []
    seen: Set[str] = set()
    queue = [start]
    name_to_concept = {c.name: c for c in concepts}
    while queue:
        name = queue.pop(0)
        if name in seen:
            continue
        seen.add(name)
        ordered.append(name_to_concept[name])
        for nb in sorted(adj[name], key=lambda x: -len(adj[x])):
            if nb not in seen:
                queue.append(nb)
    for c in sorted(concepts, key=lambda x: x.name):
        if c.name not in seen:
            ordered.append(c)
    return ordered


def _place_import_band(
    imported_concepts: List[Concept],
    positions: Dict[str, Tuple[int, int]],
    start_x: int = START_X,
) -> int:
    """Compact multi-row import band at page top (avoids one ultra-wide row)."""
    if not imported_concepts:
        return START_Y
    col_w = CELL_WIDTH + (COL_GAP if len(imported_concepts) > 6 else COL_GAP // 2)
    row_gap = OVERLAP_PAD + 36 + max(0, (len(imported_concepts) - 6) // 2) * 24
    cols = min(6, max(IMPORT_BAND_MAX_COLS, _grid_columns(len(imported_concepts))))
    y = START_Y
    col = 0
    row_max_h = 0
    for c in imported_concepts:
        h = _estimate_height(c)
        positions[c.name] = (start_x + col * col_w, y)
        row_max_h = max(row_max_h, h)
        col += 1
        if col >= cols:
            col = 0
            y += row_max_h + row_gap
            row_max_h = 0
    if col > 0:
        y += row_max_h
    return y + row_gap


def _place_grid(
    concepts: List[Concept],
    positions: Dict[str, Tuple[int, int]],
    start_y: int,
    col_w: int,
    row_gap: int,
    start_x: int = START_X,
) -> int:
    """Place concepts in a multi-row grid; return y after last row."""
    if not concepts:
        return start_y
    cols = _grid_columns(len(concepts))
    y = start_y
    col = 0
    row_max_h = 0
    for c in concepts:
        positions[c.name] = (start_x + col * col_w, y)
        row_max_h = max(row_max_h, _estimate_height(c))
        col += 1
        if col >= cols:
            col = 0
            y += row_max_h + row_gap
            row_max_h = 0
    if col > 0:
        y += row_max_h
    return y + row_gap


def _eliminate_bbox_overlaps(
    positions: Dict[str, Tuple[int, int]],
    concepts: List[Concept],
) -> None:
    """Separate overlapping bounding boxes using estimated cell sizes."""
    heights = {c.name: _estimate_height(c) for c in concepts}
    names = sorted(positions.keys())
    for _ in range(120):
        moved = False
        for i, a in enumerate(names):
            if a not in positions:
                continue
            ax, ay = positions[a]
            ah = heights.get(a, CELL_MIN_HEIGHT)
            for b in names[i + 1 :]:
                if b not in positions:
                    continue
                bx, by = positions[b]
                bh = heights.get(b, CELL_MIN_HEIGHT)
                if ax >= bx + CELL_WIDTH or ax + CELL_WIDTH <= bx:
                    continue
                if ay >= by + bh or ay + ah <= by:
                    continue
                if abs(ax - bx) < CELL_WIDTH // 2:
                    positions[b] = (bx, ay + ah + OVERLAP_PAD)
                else:
                    positions[b] = (ax + CELL_WIDTH + OVERLAP_PAD, by)
                moved = True
        if not moved:
            break


def _concept_degree(
    concept: Concept,
    local_names: Set[str],
    index: Dict[str, Concept],
    imported_names: Set[str],
) -> int:
    partners: Set[str] = set()
    for collab in concept.collaborators:
        ck = _match_concept(collab, index)
        if ck and (ck in local_names or ck in imported_names):
            partners.add(ck)
    if concept.base:
        bk = _match_concept(concept.base, index)
        if bk and (bk in local_names or bk in imported_names):
            partners.add(bk)
    return len(partners)


def _should_use_hub_layout(
    ka_name: str,
    local: List[Concept],
    index: Dict[str, Concept],
    imported_names: Set[str],
) -> bool:
    local_names = {c.name for c in local}
    for c in local:
        deg = _concept_degree(c, local_names, index, imported_names)
        if c.name == ka_name and deg >= 4:
            return True
        if deg >= 7:
            return True
    return False


def _layout_hub_spoke(
    ka_name: str,
    local: List[Concept],
    imported: List[Tuple[Concept, str]],
    index: Dict[str, Concept],
) -> Dict[str, Tuple[int, int]]:
    """Hub on the left; spoke targets in staggered columns to the right."""
    positions: Dict[str, Tuple[int, int]] = {}
    imported_concepts = sorted([c for c, _ in imported], key=lambda x: x.name)
    local_names = {c.name for c in local}
    imported_names = {c.name for c, _ in imported}
    col_w = CELL_WIDTH + COL_GAP
    row_gap = ROW_GAP // 2

    y = START_Y
    if imported_concepts:
        y = _place_import_band(imported_concepts, positions, START_X)

    hub = next((c for c in local if c.name == ka_name), None)
    if hub is None:
        hub = max(
            local,
            key=lambda c: _concept_degree(c, local_names, index, imported_names),
        )

    hub_y = y + ROW_GAP // 2
    positions[hub.name] = (START_X, hub_y)
    placed: Set[str] = {hub.name}

    child_y = hub_y + _estimate_height(hub) + row_gap
    for child in _children_of_local(hub.name, local, index):
        positions[child.name] = (START_X, child_y)
        placed.add(child.name)
        child_y += _estimate_height(child) + row_gap

    others = [c for c in local if c.name not in placed]
    others = _order_concepts_by_connectivity(others, index)
    spoke_x_base = START_X + col_w * 4
    if len(others) > 14:
        spoke_cols = 2
    else:
        spoke_cols = 1
    for col_idx in range(spoke_cols):
        cx = spoke_x_base + col_idx * col_w
        col_concepts = others[col_idx::spoke_cols] if spoke_cols > 1 else others
        cy = hub_y
        for c in col_concepts:
            positions[c.name] = (cx, cy)
            cy += _estimate_height(c) + row_gap

    return positions


def _layout_positions_strip(
    local: List[Concept],
    imported: List[Tuple[Concept, str]],
    index: Dict[str, Concept],
    row_gap: int,
) -> Dict[str, Tuple[int, int]]:
    """Single-column strip for dense pages — minimizes edge crossings through classes."""
    positions: Dict[str, Tuple[int, int]] = {}
    imported_concepts = sorted([c for c, _ in imported], key=lambda x: x.name)
    ordered_local = _order_concepts_by_connectivity(local, index)
    all_ordered = imported_concepts + ordered_local
    y = START_Y
    for c in all_ordered:
        positions[c.name] = (START_X, y)
        y += _estimate_height(c) + row_gap
    return positions


def _children_of_local(
    parent: str,
    local: List[Concept],
    index: Dict[str, Concept],
) -> List[Concept]:
    kids: List[Concept] = []
    for c in local:
        if not c.base:
            continue
        bk = _match_concept(c.base, index)
        if bk == parent:
            kids.append(c)
    return sorted(kids, key=lambda x: x.name)


def _place_chain_column(
    root: Concept,
    col: int,
    y: int,
    local: List[Concept],
    index: Dict[str, Concept],
    positions: Dict[str, Tuple[int, int]],
    col_w: int,
    row_gap: int,
    start_x: int,
    placed: Set[str],
    chain_step: Optional[int] = None,
) -> int:
    """Place an inheritance chain in one column; return max y used."""
    step = chain_step if chain_step is not None else col_w
    positions[root.name] = (start_x + col * step, y)
    placed.add(root.name)
    h = _estimate_height(root)
    child_y = y + h + row_gap // 2
    max_y = y + h
    children = _children_of_local(root.name, local, index)
    if len(children) == 1:
        end = _place_chain_column(
            children[0], col, child_y, local, index, positions, col_w, row_gap, start_x, placed, chain_step
        )
        max_y = max(max_y, end)
    elif len(children) > 1:
        for i, child in enumerate(children):
            end = _place_chain_column(
                child, col + i, child_y, local, index, positions, col_w, row_gap, start_x, placed, chain_step
            )
            max_y = max(max_y, end)
    return max_y


def _layout_positions(
    local: List[Concept],
    imported: List[Tuple[Concept, str]],
    index: Dict[str, Concept],
    ka_name: Optional[str] = None,
) -> Dict[str, Tuple[int, int]]:
    """Grid layout: imported rows at top, local layers in grids below."""
    positions: Dict[str, Tuple[int, int]] = {}
    imported_names = {c.name for c, _ in imported}
    local_names = {c.name for c in local}
    n_total = len(local) + len(imported)

    if ka_name and _should_use_hub_layout(ka_name, local, index, imported_names):
        return _layout_hub_spoke(ka_name, local, imported, index)

    col_gap, row_gap = _spacing_for_page(n_total)

    col_w = CELL_WIDTH + col_gap

    imported_concepts = sorted([c for c, _ in imported], key=lambda x: x.name)
    local_start_x = START_X
    y = START_Y
    if imported_concepts:
        y = _place_import_band(imported_concepts, positions, START_X)
        local_start_x = START_X

    memo: Dict[str, int] = {}
    layers: Dict[int, List[Concept]] = {}
    for c in local:
        layer = _local_inheritance_layer(c, index, imported_names, local_names, memo)
        layers.setdefault(layer, []).append(c)

    use_chain_columns = len(local) >= 10

    if imported_concepts and 8 <= len(local) < 10:
        cy = y
        memo_pre: Dict[str, int] = {}
        for c in sorted(
            local,
            key=lambda x: (
                _local_inheritance_layer(x, index, imported_names, local_names, memo_pre),
                x.name,
            ),
        ):
            positions[c.name] = (local_start_x, cy)
            cy += _estimate_height(c) + row_gap // 2
    elif use_chain_columns:
        placed: Set[str] = set()
        roots = sorted(layers.get(0, []), key=lambda x: x.name)
        chain_col = 0
        max_chain_bottom = y
        chain_step = col_w + CELL_WIDTH
        for root in roots:
            if root.name in placed:
                continue
            end_y = _place_chain_column(
                root, chain_col, y, local, index, positions, col_w, row_gap, local_start_x, placed, chain_step
            )
            max_chain_bottom = max(max_chain_bottom, end_y)
            chain_col += 1

        orphans = [c for c in local if c.name not in placed]
        if orphans:
            orphan_y = max_chain_bottom + row_gap
            _place_grid(
                _order_concepts_by_connectivity(orphans, index),
                positions,
                orphan_y,
                col_w,
                row_gap,
                local_start_x,
            )
    else:
        placed_layer: Set[str] = set()
        for layer_num in sorted(layers.keys()):
            concepts = _order_concepts_by_connectivity(layers[layer_num], index)
            if layer_num == 0:
                if imported_concepts and len(concepts) <= 8:
                    cy = y
                    for c in concepts:
                        positions[c.name] = (local_start_x, cy)
                        cy += _estimate_height(c) + row_gap // 2
                    y = cy + row_gap
                else:
                    y = _place_grid(concepts, positions, y, col_w, row_gap, local_start_x)
                continue

            max_bottom = y
            by_parent: Dict[str, List[Concept]] = {}
            for c in concepts:
                if not c.base:
                    continue
                bk = _match_concept(c.base, index)
                if bk is None or bk not in positions:
                    continue
                by_parent.setdefault(bk, []).append(c)

            for parent_name, children in sorted(by_parent.items()):
                bx, by = positions[parent_name]
                parent = index.get(parent_name)
                bh = _estimate_height(parent) if parent else CELL_MIN_HEIGHT
                cy = by + bh + row_gap // 2
                for i, child in enumerate(sorted(children, key=lambda x: x.name)):
                    positions[child.name] = (bx + i * col_w, cy)
                    placed_layer.add(child.name)
                    max_bottom = max(max_bottom, cy + _estimate_height(child))

            orphans = [c for c in concepts if c.name not in placed_layer]
            if orphans:
                orphan_y = max(y, max_bottom + OVERLAP_PAD)
                y = _place_grid(orphans, positions, orphan_y, col_w, row_gap, local_start_x)
            else:
                y = max_bottom + row_gap

    if imported_concepts and 8 <= len(local) < 10:
        pass
    elif not use_chain_columns:
        _eliminate_bbox_overlaps(positions, local + imported_concepts)
    if imported_concepts:
        _hoist_imported_to_top_band(positions, imported_concepts)
    return positions


def _hoist_imported_to_top_band(
    positions: Dict[str, Tuple[int, int]],
    imported: List[Concept],
) -> None:
    """Keep cross-KA imports in a compact top band."""
    if not imported:
        return
    band: Dict[str, Tuple[int, int]] = {}
    _place_import_band(sorted(imported, key=lambda x: x.name), band, START_X)
    positions.update(band)


def _hoist_imports_on_canvas(
    root,
    positions: Dict[str, Tuple[int, int]],
    imported_names: Set[str],
) -> None:
    from drawio_tools import get_all_classes, set_geometry

    if not imported_names:
        return
    placeholders = [Concept(name=n, ka="") for n in sorted(imported_names)]
    band: Dict[str, Tuple[int, int]] = {}
    _place_import_band(placeholders, band, START_X)
    for _ in range(40):
        _eliminate_bbox_overlaps(band, placeholders)
        for name, (new_x, new_y) in band.items():
            cell = find_cell_by_name(root, name)
            if cell is None:
                continue
            set_geometry(cell, x=new_x, y=new_y)
            positions[name] = (new_x, new_y)
        from drawio_tools import check_overlaps, get_all_classes

        if not check_overlaps(get_all_classes(root)):
            break
        if not _fix_overlaps_on_canvas(root, positions):
            break


def _fan_slot(slot: int) -> float:
    return min(0.92, ANCHOR_START + slot * ANCHOR_STEP)


def _side_anchor(side: str, slot: int, total_on_side: int = 1) -> Tuple[float, float]:
    step = min(ANCHOR_STEP, 0.84 / max(total_on_side, 1))
    v = min(0.92, ANCHOR_START + slot * step)
    if side == "top":
        return (v, 0.0)
    if side == "bottom":
        return (v, 1.0)
    if side == "left":
        return (0.0, v)
    return (1.0, v)


def _infer_exit_entry_sides(
    sx: float,
    sy: float,
    tx: float,
    ty: float,
    rel: str,
    sh: float = CELL_MIN_HEIGHT,
    th: float = CELL_MIN_HEIGHT,
) -> Tuple[str, str]:
    if rel == "inheritance":
        return "top", "bottom"
    if sy < ty - 50:
        return "bottom", "top"
    if sy > ty + 50:
        return "left", "top"
    if sx < tx:
        return "right", "left"
    return "left", "right"


def _pick_edge_type_for_rel(
    rel: str,
    sx: float,
    tx: float,
    sw: float = CELL_WIDTH,
    tw: float = CELL_WIDTH,
) -> str:
    if rel == "inheritance":
        if abs((sx + sw / 2) - (tx + tw / 2)) <= CELL_WIDTH * 0.55:
            return "inheritance"
        return "inheritance-orthogonal"
    if rel == "composition":
        return "composition"
    if rel == "aggregation":
        return "aggregation"
    if rel == "dependency":
        return "dependency-orthogonal"
    return "association"


def _assign_distinct_anchors(root) -> None:
    """Reassign exit/entry anchors so no two edges share the same side anchor."""
    from drawio_tools import get_all_classes, set_edge_anchors

    classes = get_all_classes(root)
    id_to_geo = {cid: (x, y, w, h) for cid, name, x, y, w, h in classes}
    id_to_name = {cid: name for cid, name, *_ in classes}
    edge_rows: List[Tuple[ET.Element, str, str, str, str, str]] = []
    side_counts: Dict[Tuple[str, str], int] = {}

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src_id = cell.get("source", "")
        tgt_id = cell.get("target", "")
        if src_id not in id_to_geo or tgt_id not in id_to_geo:
            continue
        src_name = id_to_name[src_id]
        tgt_name = id_to_name[tgt_id]
        sx, sy, sw, sh = id_to_geo[src_id]
        tx, ty, tw, th = id_to_geo[tgt_id]
        style = cell.get("style", "")
        rel = "inheritance" if "endFill=0" in style and "endArrow=block" in style else "association"
        exit_side, entry_side = _infer_exit_entry_sides(sx, sy, tx, ty, rel, sh, th)
        edge_rows.append((cell, src_name, tgt_name, exit_side, entry_side, rel))
        for cls, side in ((src_name, exit_side), (tgt_name, entry_side)):
            key = (cls, side)
            side_counts[key] = side_counts.get(key, 0) + 1

    edge_rows.sort(key=lambda row: (row[1], row[2], row[5]))
    side_slots: Dict[Tuple[str, str], int] = {}

    for cell, src_name, tgt_name, exit_side, entry_side, _rel in edge_rows:
        src_key = (src_name, exit_side)
        tgt_key = (tgt_name, entry_side)
        src_slot = side_slots.get(src_key, 0)
        tgt_slot = side_slots.get(tgt_key, 0)
        side_slots[src_key] = src_slot + 1
        side_slots[tgt_key] = tgt_slot + 1
        ex, ey = _side_anchor(exit_side, src_slot, side_counts.get(src_key, 1))
        nx, ny = _side_anchor(entry_side, tgt_slot, side_counts.get(tgt_key, 1))
        set_edge_anchors(cell, exit_x=ex, exit_y=ey, entry_x=nx, entry_y=ny)


def _refresh_routes_after_anchors(
    root,
    imported_names: Set[str],
    lane_offset: int = 0,
) -> None:
    """Recompute waypoints whenever exit/entry anchors change."""
    from drawio_tools import check_shared_anchors

    if check_shared_anchors(root):
        _assign_distinct_anchors(root)
    _assign_orthogonal_lanes(root, lane_offset=lane_offset, imported_names=imported_names)


def _is_orthogonal_edge_style(style: str) -> bool:
    return "orthogonalEdgeStyle" in style or "edgeStyle=orthogonal" in style


def _force_unique_edge_route(
    root,
    src_name: str,
    tgt_name: str,
    route_index: int,
    imported_names: Optional[Set[str]] = None,
) -> bool:
    """Assign explicit waypoints on a corridor lane unique to route_index."""
    from drawio_tools import get_all_classes

    edge = _find_edge_cell(root, src_name, tgt_name)
    if edge is None:
        return False
    style = edge.get("style", "")
    if _is_straight_inheritance(style):
        return False

    classes = get_all_classes(root)
    name_to_geo = {name: (x, y, w, h) for _, name, x, y, w, h in classes}
    name_to_id = {name: cid for cid, name, *_ in classes}
    if src_name not in name_to_geo or tgt_name not in name_to_geo:
        return False

    src_id = name_to_id[src_name]
    tgt_id = name_to_id[tgt_name]
    sg = name_to_geo[src_name]
    tg = name_to_geo[tgt_name]
    min_x, min_y, max_x, max_y = _diagram_bounds(classes)

    for bump in range(_OUTER_SLOTS):
        idx = _bounded_lane(route_index + bump)
        if _try_lane_route(
            root,
            edge,
            sg,
            tg,
            style,
            idx,
            min_x,
            min_y,
            max_x,
            max_y,
            src_id,
            tgt_id,
            classes,
            None,
            check_overlaps=True,
            imported_names=imported_names,
            route_kind=_ROUTE_KINDS[bump % len(_ROUTE_KINDS)],
        ):
            return True
    return False


def _edge_has_waypoints(cell: ET.Element) -> bool:
    geo = cell.find("mxGeometry")
    if geo is None:
        return False
    arr = geo.find("Array")
    if arr is None:
        return False
    return bool(arr.findall("mxPoint"))


def _edge_polyline(
    edge: ET.Element,
    id_to_geo: Dict[str, Tuple[float, float, float, float]],
) -> List[Tuple[float, float]]:
    from drawio_tools import _compute_edge_segments

    return [p for seg in _compute_edge_segments(edge, id_to_geo) for p in (seg[0], seg[1])][::2] or []


def _edge_path_points(
    edge: ET.Element,
    id_to_geo: Dict[str, Tuple[float, float, float, float]],
) -> List[Tuple[float, float]]:
    from drawio_tools import _compute_edge_segments

    segs = _compute_edge_segments(edge, id_to_geo)
    if not segs:
        return []
    pts = [segs[0][0]]
    for _, p2 in segs:
        pts.append(p2)
    return pts


def _edge_overlaps_any(
    root,
    edge: ET.Element,
    skip_ids: Optional[Set[str]] = None,
    only_edges: Optional[List[ET.Element]] = None,
) -> bool:
    from drawio_tools import (
        _compute_edge_segments,
        _edge_segments_overlap,
        get_all_classes,
    )

    skip_ids = skip_ids or set()
    classes = get_all_classes(root)
    id_to_geo = {cid: (x, y, w, h) for cid, name, x, y, w, h in classes}
    my_segs = _compute_edge_segments(edge, id_to_geo)
    if not my_segs:
        return False
    others = only_edges if only_edges is not None else [
        c for c in root.findall("mxCell") if c.get("edge") == "1" and c is not edge
    ]
    for cell in others:
        oid = cell.get("id", "")
        if oid in skip_ids:
            continue
        other_segs = _compute_edge_segments(cell, id_to_geo)
        for sa in my_segs:
            for sb in other_segs:
                if _edge_segments_overlap(sa, sb, proximity=12):
                    return True
    return False


def _route_is_clear(
    root,
    edge: ET.Element,
    src_id: str,
    tgt_id: str,
    path: List[Tuple[float, float]],
    classes,
    prior_edges: Optional[List[ET.Element]] = None,
) -> bool:
    if _path_crosses_obstacles(path, classes, src_id, tgt_id):
        return False
    return not _edge_overlaps_any(root, edge, only_edges=prior_edges)


def _stub_length(lane_idx: int) -> float:
    """Per-lane stub length; consecutive lanes differ by >= 13px (overlap proximity)."""
    return 14.0 + (lane_idx % 24) * 13


def _entry_approach(
    style: str,
    x2: float,
    y2: float,
    lane_idx: int,
) -> Tuple[float, float]:
    from drawio_tools import _parse_style_float

    entry_stub = _stub_length(lane_idx)
    nx = _parse_style_float(style, "entryX")
    ny = _parse_style_float(style, "entryY")
    nx = 0.5 if nx is None else nx
    ny = 0.5 if ny is None else ny
    if ny <= 0.05:
        return (x2, y2 - entry_stub)
    if ny >= 0.95:
        return (x2, y2 + entry_stub)
    if nx <= 0.05:
        return (x2 - entry_stub, y2)
    return (x2 + entry_stub, y2)


def _exit_hop(
    style: str,
    x1: float,
    y1: float,
    lane_idx: int,
) -> Tuple[float, float]:
    from drawio_tools import _parse_style_float

    stub = _stub_length(lane_idx)
    ex = _parse_style_float(style, "exitX")
    ey = _parse_style_float(style, "exitY")
    ex = 0.5 if ex is None else ex
    ey = 0.5 if ey is None else ey
    if ey <= 0.05:
        return (x1, y1 - stub)
    if ey >= 0.95:
        return (x1, y1 + stub)
    if ex <= 0.05:
        return (x1 - stub, y1)
    return (x1 + stub, y1)


def _is_local_edge(
    sg: Tuple[float, float, float, float],
    tg: Tuple[float, float, float, float],
) -> bool:
    sx, sy, sw, sh = sg
    tx, ty, tw, th = tg
    cx_dist = abs((sx + sw / 2) - (tx + tw / 2))
    cy_dist = abs((sy + sh / 2) - (ty + th / 2))
    return cx_dist < COL_GAP * 2.5 and cy_dist < ROW_GAP * 1.5


def _routing_gutter_y(
    classes: List[Tuple],
    imported_names: Set[str],
    lane_idx: int,
) -> Optional[float]:
    """Horizontal channel between import band and local classes."""
    id_to_name = {cid: name for cid, name, *_ in classes}
    imp_bottom = 0.0
    loc_top = float("inf")
    for cid, _name, x, y, w, h in classes:
        name = id_to_name.get(cid, "")
        if name in imported_names:
            imp_bottom = max(imp_bottom, y + h)
        else:
            loc_top = min(loc_top, y)
    if loc_top > imp_bottom + 48:
        return (imp_bottom + loc_top) / 2 + ((lane_idx % 160) + 1) * (EDGE_SEPARATION + 2)
    return None


def _force_lane_waypoints(
    edge: ET.Element,
    sg: Tuple[float, float, float, float],
    tg: Tuple[float, float, float, float],
    style: str,
    lane_idx: int,
    min_x: float,
    min_y: float,
    max_x: float,
    max_y: float,
    classes: Optional[List[Tuple]] = None,
    imported_names: Optional[Set[str]] = None,
    route_kind: str = "outer",
) -> None:
    """Assign explicit orthogonal waypoints; route_kind selects outer wrap, gutter, or local L."""
    p1 = _edge_anchor(style, sg, "exit")
    p2 = _edge_anchor(style, tg, "entry")
    x1, y1 = p1
    x2, y2 = p2
    sx, sy, sw, sh = sg
    tx, ty, tw, th = tg
    n, extra = _lane_spacing(lane_idx)
    hop = _exit_hop(style, x1, y1, lane_idx)
    approach = _entry_approach(style, x2, y2, lane_idx)

    if route_kind == "between":
        if sx + sw + 40 < tx:
            cx = sx + sw + 40 + n * 10 + (extra % 40)
            wps = [hop, (cx, hop[1]), (cx, approach[1]), approach]
            _set_edge_waypoints(edge, wps)
            return
        if tx + tw + 40 < sx:
            cx = tx + tw + 40 + n * 10 + (extra % 40)
            wps = [hop, (cx, hop[1]), (cx, approach[1]), approach]
            _set_edge_waypoints(edge, wps)
            return

    if route_kind == "local":
        mid_y = (hop[1] + approach[1]) / 2 + _lane_y_offset(lane_idx)
        wps = [hop, (hop[0], mid_y), (approach[0], mid_y), approach]
        _set_edge_waypoints(edge, wps)
        return

    if route_kind not in ("outer", "outer_bottom", "between") and _is_local_edge(sg, tg):
        wps = [(hop[0], approach[1]), approach]
        _set_edge_waypoints(edge, wps)
        return

    if route_kind == "gutter" and classes is not None and imported_names:
        gutter_y = _routing_gutter_y(classes, imported_names, lane_idx)
        if gutter_y is not None:
            wps = [hop, (hop[0], gutter_y), (approach[0], gutter_y), approach]
            _set_edge_waypoints(edge, wps)
            return

    use_top = route_kind != "outer_bottom"
    off = _outer_margin(lane_idx)
    bus = min_y - off if use_top else max_y + off
    run_left = min_x - off
    run_right = max_x + off
    src_side = run_left if lane_idx % 2 == 0 else run_right
    tgt_side = run_left if approach[0] <= (min_x + max_x) / 2 else run_right
    if abs(src_side - tgt_side) < LANE_STEP:
        tgt_side = run_right if src_side == run_left else run_left
    wps = [
        hop,
        (src_side, hop[1]),
        (src_side, bus),
        (tgt_side, bus),
        (tgt_side, approach[1]),
        approach,
    ]
    _set_edge_waypoints(edge, wps)


_ROUTE_KINDS = ("between", "outer", "outer_bottom", "gutter", "local")


def _route_kinds_for_edge(
    src_name: str,
    tgt_name: str,
    sg: Tuple[float, float, float, float],
    tg: Tuple[float, float, float, float],
    imported_names: Set[str],
) -> Tuple[str, ...]:
    """Pick route templates; long vertical spans and import targets prefer outer wrap."""
    if tgt_name in imported_names and src_name not in imported_names:
        return _OUTER_FIRST_KINDS
    sx, sy, _, _ = sg
    tx, ty, _, _ = tg
    if abs(sy - ty) > ROW_GAP * 1.2:
        return ("outer", "outer_bottom", "between", "gutter", "local")
    if tgt_name in imported_names:
        return _OUTER_FIRST_KINDS
    if _is_local_edge(sg, tg):
        return ("local", "between", "gutter", "outer", "outer_bottom")
    return _ROUTE_KINDS


def _try_lane_route(
    root,
    edge: ET.Element,
    sg: Tuple[float, float, float, float],
    tg: Tuple[float, float, float, float],
    style: str,
    lane_idx: int,
    min_x: float,
    min_y: float,
    max_x: float,
    max_y: float,
    src_id: str,
    tgt_id: str,
    classes,
    prior_edges: Optional[List[ET.Element]] = None,
    check_overlaps: bool = True,
    imported_names: Optional[Set[str]] = None,
    route_kind: str = "outer",
) -> bool:
    _force_lane_waypoints(
        edge,
        sg,
        tg,
        style,
        lane_idx,
        min_x,
        min_y,
        max_x,
        max_y,
        classes=classes,
        imported_names=imported_names,
        route_kind=route_kind,
    )
    style = edge.get("style", "")
    p1 = _edge_anchor(style, sg, "exit")
    p2 = _edge_anchor(style, tg, "entry")
    geo = edge.find("mxGeometry")
    wps: List[Tuple[float, float]] = []
    if geo is not None:
        arr = geo.find("Array")
        if arr is not None:
            for pt in arr.findall("mxPoint"):
                wps.append((float(pt.get("x", 0)), float(pt.get("y", 0))))
    path = [p1] + wps + [p2]
    if _path_crosses_obstacles(path, classes, src_id, tgt_id):
        return False
    if not check_overlaps:
        return True
    return not _edge_overlaps_any(root, edge, only_edges=prior_edges)


def _assign_orthogonal_lanes(
    root,
    lane_offset: int = 0,
    imported_names: Optional[Set[str]] = None,
) -> None:
    """Assign a unique outer corridor to every non-straight inheritance orthogonal edge."""
    from drawio_tools import get_all_classes

    classes = get_all_classes(root)
    id_to_geo = {cid: (x, y, w, h) for cid, name, x, y, w, h in classes}
    if not id_to_geo:
        return
    min_x, min_y, max_x, max_y = _diagram_bounds(classes)
    imported_names = imported_names or set()

    edges: List[ET.Element] = []
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        style = cell.get("style", "")
        if not _is_orthogonal_edge_style(style) or _is_straight_inheritance(style):
            continue
        src_id = cell.get("source", "")
        tgt_id = cell.get("target", "")
        if src_id in id_to_geo and tgt_id in id_to_geo:
            edges.append(cell)

    edges.sort(key=lambda c: (c.get("source", ""), c.get("target", "")))
    id_to_name = {cid: name for cid, name, *_ in classes}
    from collections import Counter

    out_degree: Counter = Counter(c.get("source", "") for c in edges)
    src_edge_idx: Dict[str, int] = {}
    placed: List[ET.Element] = []
    for idx, cell in enumerate(edges):
        src_id = cell.get("source", "")
        tgt_id = cell.get("target", "")
        style = cell.get("style", "")
        sg = id_to_geo[src_id]
        tg = id_to_geo[tgt_id]
        tgt_name = id_to_name.get(tgt_id, "")
        kinds: Tuple[str, ...] = (
            ("gutter", "between", "outer", "outer_bottom", "local")
            if tgt_name in imported_names
            else _ROUTE_KINDS
        )
        out_idx = src_edge_idx.get(src_id, 0)
        src_edge_idx[src_id] = out_idx + 1
        if out_degree.get(src_id, 0) >= 4:
            base = _bounded_lane(lane_offset + idx * 2 + out_idx * 6)
        else:
            base = _bounded_lane(lane_offset + idx)
        placed_ok = False
        for bump in range(_OUTER_SLOTS):
            lane_idx = _bounded_lane(base + bump)
            route_kind = kinds[bump % len(kinds)]
            if _try_lane_route(
                root,
                cell,
                sg,
                tg,
                style,
                lane_idx,
                min_x,
                min_y,
                max_x,
                max_y,
                src_id,
                tgt_id,
                classes,
                placed,
                check_overlaps=True,
                imported_names=imported_names,
                route_kind=route_kind,
            ):
                placed_ok = True
                break
        if not placed_ok:
            for bump in range(_OUTER_SLOTS * len(_ROUTE_KINDS)):
                lane_idx = _bounded_lane(base + bump)
                route_kind = kinds[bump % len(kinds)]
                if _try_lane_route(
                    root,
                    cell,
                    sg,
                    tg,
                    style,
                    lane_idx,
                    min_x,
                    min_y,
                    max_x,
                    max_y,
                    src_id,
                    tgt_id,
                    classes,
                    placed,
                    check_overlaps=True,
                    imported_names=imported_names,
                    route_kind=route_kind,
                ):
                    placed_ok = True
                    break
            if not placed_ok:
                for bump in range(_OUTER_SLOTS):
                    lane_idx = _bounded_lane(base + bump)
                    route_kind = kinds[bump % len(kinds)]
                    if _try_lane_route(
                        root,
                        cell,
                        sg,
                        tg,
                        style,
                        lane_idx,
                        min_x,
                        min_y,
                        max_x,
                        max_y,
                        src_id,
                        tgt_id,
                        classes,
                        placed,
                        check_overlaps=False,
                        imported_names=imported_names,
                        route_kind=route_kind,
                    ):
                        break
        placed.append(cell)


def _ensure_orthogonal_waypoints(
    root,
    imported_names: Optional[Set[str]] = None,
) -> None:
    """Every orthogonal edge without waypoints gets explicit routing."""
    from drawio_tools import get_all_classes

    classes = get_all_classes(root)
    id_to_geo = {cid: (x, y, w, h) for cid, name, x, y, w, h in classes}
    if not id_to_geo:
        return
    min_x, min_y, max_x, max_y = _diagram_bounds(classes)
    imported_names = imported_names or set()
    lane_idx = 0
    for cell in sorted(
        [c for c in root.findall("mxCell") if c.get("edge") == "1"],
        key=lambda c: (c.get("source", ""), c.get("target", "")),
    ):
        style = cell.get("style", "")
        if not _is_orthogonal_edge_style(style) or _is_straight_inheritance(style):
            continue
        if _edge_has_waypoints(cell):
            continue
        src_id = cell.get("source", "")
        tgt_id = cell.get("target", "")
        if src_id not in id_to_geo or tgt_id not in id_to_geo:
            continue
        _force_lane_waypoints(
            cell,
            id_to_geo[src_id],
            id_to_geo[tgt_id],
            style,
            lane_idx,
            min_x,
            min_y,
            max_x,
            max_y,
            classes=classes,
            imported_names=imported_names,
            route_kind="outer",
        )
        lane_idx = _bounded_lane(lane_idx + 1)


def _route_all_orthogonal_edges(root, corridor_offset: int = 0) -> int:
    """Assign unique waypoint corridors to every orthogonal edge."""
    _assign_orthogonal_lanes(root)
    return len([c for c in root.findall("mxCell") if c.get("edge") == "1"])


def _fix_class_crossing_edges(
    root,
    max_rounds: int = 40,
    imported_names: Optional[Set[str]] = None,
) -> None:
    """Reroute only edges that still cross unrelated class boxes."""
    from drawio_tools import check_edges_crossing_classes

    for round_idx in range(max_rounds):
        crossings = check_edges_crossing_classes(root)
        if not crossings:
            return
        seen: Set[Tuple[str, str]] = set()
        for desc, _obstacle in crossings[:8]:
            m = re.match(r"(.+?)->(.+?) \(", desc)
            if not m:
                continue
            pair = (m.group(1).strip(), m.group(2).strip())
            if pair in seen:
                continue
            seen.add(pair)
            edge = _find_edge_cell(root, pair[0], pair[1])
            if edge is not None and _is_straight_inheritance(edge.get("style", "")):
                _apply_inheritance_edge_type(
                    edge,
                    orthogonal=True,
                    entry_x=_fan_slot(round_idx % 8),
                    clear_route=True,
                )
            _force_unique_edge_route(
                root,
                pair[0],
                pair[1],
                round_idx * 6 + len(seen) + 20,
                imported_names,
            )


def _reroute_single_edge(
    root,
    src_name: str,
    tgt_name: str,
    lane_base: int,
    imported_names: Set[str],
    check_overlaps: bool = True,
) -> bool:
    from drawio_tools import get_all_classes

    edge = _find_edge_cell(root, src_name, tgt_name)
    if edge is None:
        return False
    style = edge.get("style", "")
    if _is_straight_inheritance(style):
        return False

    classes = get_all_classes(root)
    name_to_geo = {name: (x, y, w, h) for _, name, x, y, w, h in classes}
    name_to_id = {name: cid for cid, name, *_ in classes}
    if src_name not in name_to_geo or tgt_name not in name_to_geo:
        return False

    src_id = name_to_id[src_name]
    tgt_id = name_to_id[tgt_name]
    sg = name_to_geo[src_name]
    tg = name_to_geo[tgt_name]
    min_x, min_y, max_x, max_y = _diagram_bounds(classes)
    placed = [
        c for c in root.findall("mxCell") if c.get("edge") == "1" and c is not edge
    ]
    if tgt_name in imported_names:
        kinds: Tuple[str, ...] = ("gutter", "between", "outer", "outer_bottom", "local")
    elif _is_local_edge(sg, tg):
        kinds = ("local", "between", "gutter", "outer", "outer_bottom")
    else:
        kinds = _ROUTE_KINDS

    for bump in range(_OUTER_SLOTS):
        lane_idx = _bounded_lane(lane_base + bump)
        route_kind = kinds[bump % len(kinds)]
        if _try_lane_route(
            root,
            edge,
            sg,
            tg,
            style,
            lane_idx,
            min_x,
            min_y,
            max_x,
            max_y,
            src_id,
            tgt_id,
            classes,
            placed,
            check_overlaps=check_overlaps,
            imported_names=imported_names,
            route_kind=route_kind,
        ):
            return True
    return False


def _eliminate_edge_overlaps(
    root,
    imported_names: Set[str],
    max_rounds: int = 150,
) -> None:
    """Dedicated pass until edge_on_edge_overlap is zero."""
    from drawio_tools import check_edge_on_edge_overlaps

    lane = 0
    for round_idx in range(max_rounds):
        overlaps = check_edge_on_edge_overlaps(root)
        if not overlaps:
            return
        rerouted = False
        for desc_a, desc_b, _detail in overlaps:
            for desc in (desc_a, desc_b):
                parsed = _parse_edge_desc(desc)
                if parsed is None:
                    continue
                src, tgt = parsed
                lane += 1
                if _reroute_single_edge(
                    root, src, tgt, lane, imported_names, check_overlaps=True
                ):
                    rerouted = True
        if rerouted:
            continue
        _assign_orthogonal_lanes(
            root,
            lane_offset=(round_idx + 1) % _OUTER_SLOTS,
            imported_names=imported_names,
        )


def _polish_page_layout(
    root,
    positions: Dict[str, Tuple[int, int]],
    imported_names: Set[str],
) -> None:
    """Fix shared_anchor and edge_on_edge_overlap without rerouting every edge."""
    from drawio_tools import (
        check_edge_on_edge_overlaps,
        check_shared_anchors,
        validate_layout,
    )

    _hoist_imports_on_canvas(root, positions, imported_names)
    if imported_names:
        _fix_class_crossing_edges(root, max_rounds=10, imported_names=imported_names)
    _ensure_orthogonal_waypoints(root, imported_names)
    _assign_distinct_anchors(root)
    _assign_orthogonal_lanes(root, lane_offset=0, imported_names=imported_names)

    lane_base = 0
    for round_idx in range(80):
        if not check_edge_on_edge_overlaps(root):
            break
        overlaps = check_edge_on_edge_overlaps(root)
        rerouted = False
        for desc_a, desc_b, _detail in overlaps[:6]:
            for desc in (desc_a, desc_b):
                m = re.match(r"(.+?)->(.+?) \(", desc)
                if not m:
                    continue
                if _reroute_single_edge(
                    root,
                    m.group(1).strip(),
                    m.group(2).strip(),
                    lane_base + round_idx,
                    imported_names,
                    check_overlaps=True,
                ):
                    rerouted = True
        if not rerouted:
            _assign_orthogonal_lanes(
                root,
                lane_offset=(round_idx + 1) % _OUTER_SLOTS,
                imported_names=imported_names,
            )

    if [v for v in validate_layout(root) if v[0] in CRITICAL_RULES]:
        _fix_class_crossing_edges(root, max_rounds=15, imported_names=imported_names)


def _refine_page_violations(
    root,
    imported_names: Set[str],
    positions: Dict[str, Tuple[int, int]],
    max_rounds: int = 40,
) -> None:
    """Final targeted pass for remaining violations."""
    from drawio_tools import (
        check_edge_on_edge_overlaps,
        check_edges_crossing_classes,
        check_shared_anchors,
        validate_layout,
    )

    lane_base = 0
    stall = 0
    for round_idx in range(max_rounds):
        violations = validate_layout(root)
        if not violations:
            return
        if check_shared_anchors(root):
            _refresh_routes_after_anchors(
                root, imported_names, lane_offset=round_idx % _OUTER_SLOTS
            )
            stall = 0
            continue

        crossings = check_edges_crossing_classes(root)
        overlaps = check_edge_on_edge_overlaps(root)
        candidates: List[str] = []
        if crossings:
            candidates.append(crossings[0][0])
        if overlaps:
            candidates.extend(overlaps[i][0] for i in range(min(4, len(overlaps))))
        if not candidates:
            stall += 1
        else:
            progress = False
            check_ov = not bool(crossings)
            for desc in candidates:
                m = re.match(r"(.+?)->(.+?) \(", desc)
                if not m:
                    continue
                src_name = m.group(1).strip()
                tgt_name = m.group(2).strip()
                edge = _find_edge_cell(root, src_name, tgt_name)
                if edge is not None and crossings and _is_straight_inheritance(edge.get("style", "")):
                    _apply_inheritance_edge_type(
                        edge,
                        orthogonal=True,
                        entry_x=_fan_slot(round_idx % 9),
                        clear_route=True,
                    )
                if _reroute_single_edge(
                    root,
                    src_name,
                    tgt_name,
                    lane_base + round_idx,
                    imported_names,
                    check_overlaps=check_ov,
                ):
                    progress = True

            if progress:
                stall = 0
                continue

        rule, msg = violations[0]
        if rule == "edge_crosses_class" and _fix_edge_cross_on_canvas(
            root, positions, msg, round_idx
        ):
            _sync_positions_from_root(root, positions)
            stall = 0
            continue
        if rule == "class_overlap" and _fix_overlaps_on_canvas(root, positions):
            _sync_positions_from_root(root, positions)
            stall = 0
            continue

        stall += 1
        _assign_orthogonal_lanes(
            root, lane_offset=round_idx % _OUTER_SLOTS, imported_names=imported_names
        )
        _fix_class_crossing_edges(root, max_rounds=6, imported_names=imported_names)
        if stall >= 30:
            return


def _parse_edge_desc(desc: str) -> Optional[Tuple[str, str]]:
    m = re.match(r"(.+?)->(.+?) \(", desc)
    if not m:
        return None
    return m.group(1).strip(), m.group(2).strip()


def _achieve_zero_violations(
    root,
    positions: Dict[str, Tuple[int, int]],
    imported_names: Set[str],
    max_rounds: int = 800,
) -> None:
    """Iterate layout fixes until validate_layout is clean."""
    from drawio_tools import (
        check_edge_on_edge_overlaps,
        check_overlaps,
        check_shared_anchors,
        get_all_classes,
        validate_layout,
    )

    lane_seq = 0
    stall = 0
    for round_idx in range(max_rounds):
        violations = validate_layout(root)
        if not violations:
            return

        if check_overlaps(get_all_classes(root)):
            if _fix_overlaps_on_canvas(root, positions):
                _refresh_routes_after_anchors(
                    root, imported_names, lane_offset=round_idx % _OUTER_SLOTS
                )
                stall = 0
                continue

        if check_shared_anchors(root):
            _refresh_routes_after_anchors(
                root, imported_names, lane_offset=round_idx % _OUTER_SLOTS
            )
            stall = 0
            continue

        progress = False
        for rule, msg in violations[:10]:
            if rule == "hierarchy_flow" and _fix_hierarchy_on_canvas(root, positions, msg):
                progress = True
                break
            if rule == "class_overlap":
                if _fix_overlaps_on_canvas(root, positions):
                    progress = True
                    break
            if rule == "edge_crosses_class":
                parsed = _parse_edge_desc(msg.replace("Edge ", "", 1))
                if parsed is None:
                    continue
                src, tgt = parsed
                lane_seq += 1
                edge = _find_edge_cell(root, src, tgt)
                if edge is not None and _is_straight_inheritance(edge.get("style", "")):
                    _apply_inheritance_edge_type(
                        edge, orthogonal=True, entry_x=_fan_slot(lane_seq % 9), clear_route=True
                    )
                if _reroute_single_edge(
                    root, src, tgt, lane_seq, imported_names, check_overlaps=False
                ):
                    progress = True
                    break
                if _route_edge_clear(root, src, tgt, lane_seq):
                    progress = True
                    break
                if _fix_edge_cross_on_canvas(root, positions, msg, lane_seq):
                    progress = True
                    break
            if rule == "edge_on_edge_overlap":
                overlaps = check_edge_on_edge_overlaps(root)
                if not overlaps:
                    continue
                desc_a, desc_b, _ = overlaps[0]
                for desc in (desc_a, desc_b):
                    parsed = _parse_edge_desc(desc)
                    if parsed is None:
                        continue
                    src, tgt = parsed
                    lane_seq += 1
                    if _reroute_single_edge(
                        root, src, tgt, lane_seq, imported_names, check_overlaps=True
                    ):
                        progress = True
                        break
                    if _reroute_single_edge(
                        root, src, tgt, lane_seq + 8, imported_names, check_overlaps=False
                    ):
                        progress = True
                        break
                if progress:
                    break

        if progress:
            stall = 0
            continue

        stall += 1
        if stall >= 24:
            return
        if stall % 4 == 0:
            lane_seq += 8
            _assign_orthogonal_lanes(
                root, lane_offset=(round_idx % 48) + stall, imported_names=imported_names
            )
            _fix_class_crossing_edges(root, max_rounds=8, imported_names=imported_names)


def _fix_secondary_violations(
    root, positions: Dict[str, Tuple[int, int]], imported_names: Set[str]
) -> None:
    _polish_page_layout(root, positions, imported_names)


def _snap_inheritance_columns(
    local: List[Concept],
    index: Dict[str, Concept],
    positions: Dict[str, Tuple[int, int]],
) -> None:
    """Align derived classes with base when they are the only child (vertical inheritance)."""
    children_by_parent: Dict[str, List[Concept]] = {}
    for c in local:
        if not c.base:
            continue
        bk = _match_concept(c.base, index)
        if bk:
            children_by_parent.setdefault(bk, []).append(c)

    for c in local:
        if not c.base:
            continue
        bk = _match_concept(c.base, index)
        if bk is None or bk not in positions or c.name not in positions:
            continue
        if len(children_by_parent.get(bk, [])) > 1:
            continue
        px, _ = positions[bk]
        _, cy = positions[c.name]
        positions[c.name] = (px, cy)


def _pick_edge_type(from_c: Concept, to_name: str, rel: str) -> str:
    if rel == "inheritance":
        return "inheritance-orthogonal"
    if rel == "composition":
        return "composition"
    if rel == "aggregation":
        return "aggregation"
    if rel == "dependency":
        return "dependency-orthogonal"
    return "association"


def _build_page(
    mxfile,
    ka: KeyAbstraction,
    all_kas: List[KeyAbstraction],
    index: Dict[str, Concept],
) -> None:
    local_names = {c.name for c in ka.concepts}
    imported: List[Tuple[Concept, str]] = []
    imported_set: Set[str] = set()

    def _add_import(name: str, chain: Set[str]) -> None:
        key = _match_concept(name, index)
        if key is None or key in local_names or key in imported_set:
            return
        if key in chain:
            return
        c = index[key]
        if c.ka == ka.name:
            return
        imported.append((c, c.ka))
        imported_set.add(key)
        if c.base:
            _add_import(c.base, chain | {key})

    for c in ka.concepts:
        if c.base:
            _add_import(c.base, set())
        for collab in c.collaborators:
            _add_import(collab, set())

    _, root = get_page(mxfile, ka.name)
    if root is None:
        _, root = add_page(mxfile, ka.name, page_width=2400, page_height=1800)

    # Clear existing cells except root ids 0 and 1
    for cell in list(root.findall("mxCell")):
        if cell.get("id") not in ("0", "1"):
            root.remove(cell)

    positions = _layout_positions(ka.concepts, imported, index, ka_name=ka.name)
    _snap_inheritance_columns(ka.concepts, index, positions)
    name_to_id: Dict[str, str] = {}

    def _add_concept(c: Concept, imported_from: Optional[str] = None) -> None:
        if c.name in name_to_id:
            return
        x, y = positions.get(c.name, (START_X, START_Y))
        props = list(c.properties)
        ops = list(c.operations)
        invs = [inv[:120] for inv in c.invariants[:6]]
        cell = create_class_cell(
            root,
            name=c.name,
            base=c.base,
            properties=props,
            operations=ops[:12],
            invariants=invs,
            x=x,
            y=y,
            imported_from=imported_from,
        )
        name_to_id[c.name] = cell.get("id")

    for c, src_ka in imported:
        _add_concept(c, imported_from=src_ka)

    for c in ka.concepts:
        _add_concept(c)

    edges_added: Set[Tuple[str, str, str]] = set()
    undirected_assoc: Set[Tuple[str, str]] = set()
    edge_specs: List[Tuple[str, str, str]] = []

    def _queue_rel(src: str, tgt: str, rel: str) -> None:
        src_key = _match_concept(src, index)
        tgt_key = _match_concept(tgt, index)
        if src_key is None or tgt_key is None:
            return
        if src_key not in name_to_id or tgt_key not in name_to_id:
            return
        key = (src_key, tgt_key, rel)
        if key in edges_added:
            return
        if rel == "inheritance" and src_key == tgt_key:
            return
        if rel == "association":
            pair = tuple(sorted((src_key, tgt_key)))
            if pair in undirected_assoc:
                return
            undirected_assoc.add(pair)
        edges_added.add(key)
        edge_specs.append((src_key, tgt_key, rel))

    for c in ka.concepts:
        if c.base:
            _queue_rel(c.name, c.base, "inheritance")

    for c in ka.concepts:
        for collab in c.collaborators:
            ck = _match_concept(collab, index)
            if ck is None:
                continue
            if ck == c.name:
                continue
            if index[ck].ka != ka.name and ck not in imported_set:
                continue
            rel = "association"
            for p in c.properties:
                if collab in p and "composition" in p.lower():
                    rel = "composition"
                elif collab in p and "aggregation" in p.lower():
                    rel = "aggregation"
            if c.base and ck == _match_concept(c.base, index):
                continue
            _queue_rel(c.name, ck, rel)

    side_slots: Dict[Tuple[str, str], int] = {}

    def _take_anchor(class_name: str, side: str) -> Tuple[float, float]:
        key = (class_name, side)
        slot = side_slots.get(key, 0)
        side_slots[key] = slot + 1
        return _side_anchor(side, slot)

    for src_key, tgt_key, rel in sorted(edge_specs, key=lambda t: (t[0], t[1], t[2])):
        sx, sy = positions.get(src_key, (0, 0))
        tx, ty = positions.get(tgt_key, (0, 0))
        src_c = index.get(src_key)
        sh = _estimate_height(src_c) if src_c else CELL_MIN_HEIGHT
        tgt_c = index.get(tgt_key)
        th = _estimate_height(tgt_c) if tgt_c else CELL_MIN_HEIGHT
        etype = _pick_edge_type_for_rel(rel, sx, tx)
        exit_side, entry_side = _infer_exit_entry_sides(sx, sy, tx, ty, rel, sh, th)
        exit_x, exit_y = _take_anchor(src_key, exit_side)
        entry_x, entry_y = _take_anchor(tgt_key, entry_side)
        create_edge(
            root,
            name_to_id[src_key],
            name_to_id[tgt_key],
            etype,
            exit_x=exit_x,
            exit_y=exit_y,
            entry_x=entry_x,
            entry_y=entry_y,
        )

    # Post-layout: optimize until critical violations cleared
    _refresh_inheritance_edge_styles(root)
    for _pass in range(3):
        _optimize_layout(root, positions)
        _finalize_page_routes(root, positions)
        from drawio_tools import validate_layout

        if not [v for v in validate_layout(root) if v[0] in CRITICAL_RULES]:
            break

    _snap_inheritance_columns(ka.concepts, index, positions)
    from drawio_tools import set_geometry

    for c in ka.concepts:
        if c.name not in positions:
            continue
        cell = find_cell_by_name(root, c.name)
        if cell is None:
            continue
        x, y = positions[c.name]
        set_geometry(cell, x=x, y=y)
    _refresh_inheritance_edge_styles(root)
    _polish_page_layout(root, positions, imported_set)
    n_edges = len(edge_specs)
    refine_a = min(100, 30 + n_edges)
    achieve_a = min(150, 40 + n_edges * 2)
    _refine_page_violations(root, imported_set, positions, max_rounds=refine_a)
    _achieve_zero_violations(root, positions, imported_set, max_rounds=achieve_a)
    _eliminate_edge_overlaps(root, imported_set, max_rounds=min(120, 40 + n_edges))
    _fix_class_crossing_edges(root, max_rounds=30, imported_names=imported_set)
    if validate_layout(root):
        _refine_page_violations(root, imported_set, positions, max_rounds=refine_a // 2)
        _achieve_zero_violations(root, positions, imported_set, max_rounds=achieve_a * 2)
        _eliminate_edge_overlaps(root, imported_set, max_rounds=min(100, 30 + n_edges))
        _fix_class_crossing_edges(root, max_rounds=40, imported_names=imported_set)


def _edge_anchor(style: str, geo: Tuple[float, float, float, float], end: str) -> Tuple[float, float]:
    x, y, w, h = geo
    ax_m = re.search(rf"{end}X=([0-9.]+)", style)
    ay_m = re.search(rf"{end}Y=([0-9.]+)", style)
    if ax_m and ay_m:
        return (x + w * float(ax_m.group(1)), y + h * float(ay_m.group(1)))
    return (x + w / 2, y + h / 2)


def _set_edge_waypoints(edge_cell: ET.Element, points: List[Tuple[float, float]]) -> None:
    geo = edge_cell.find("mxGeometry")
    if geo is None:
        return
    for arr in list(geo.findall("Array")):
        geo.remove(arr)
    if not points:
        return
    arr = ET.SubElement(geo, "Array")
    arr.set("as", "points")
    for px, py in points:
        pt = ET.SubElement(arr, "mxPoint")
        pt.set("x", str(int(px)))
        pt.set("y", str(int(py)))


def _find_edge_cell(root, src_name: str, tgt_name: str) -> Optional[ET.Element]:
    from drawio_tools import get_all_classes

    classes = get_all_classes(root)
    name_to_id = {name: cid for cid, name, *_ in classes}
    src_id = name_to_id.get(src_name)
    tgt_id = name_to_id.get(tgt_name)
    if not src_id or not tgt_id:
        return None
    for cell in root.findall("mxCell"):
        if cell.get("edge") == "1" and cell.get("source") == src_id and cell.get("target") == tgt_id:
            return cell
    return None


def _clear_edge_waypoints(edge_cell: ET.Element) -> None:
    geo = edge_cell.find("mxGeometry")
    if geo is None:
        return
    for arr in list(geo.findall("Array")):
        geo.remove(arr)


def _apply_inheritance_edge_type(
    edge: ET.Element,
    *,
    orthogonal: bool,
    entry_x: float = 0.5,
    clear_route: bool = True,
) -> None:
    from drawio_tools import EDGE_STYLES, _parse_style_float

    style = edge.get("style", "")
    ex = _parse_style_float(style, "exitX")
    ey = _parse_style_float(style, "exitY")
    nx = _parse_style_float(style, "entryX")
    ny = _parse_style_float(style, "entryY")

    etype = "inheritance-orthogonal" if orthogonal else "inheritance"
    base = EDGE_STYLES[etype].rstrip(";") + ";"
    if orthogonal:
        ex = ex if ex is not None else 0.5
        ey = ey if ey is not None else 0.0
        nx = nx if nx is not None else entry_x
        ny = ny if ny is not None else 1.0
    else:
        ex = ex if ex is not None else 0.5
        ey = ey if ey is not None else 0.0
        nx = nx if nx is not None else 0.5
        ny = ny if ny is not None else 1.0
    edge.set(
        "style",
        f"{base}exitX={ex};exitY={ey};entryX={nx};entryY={ny};"
        "exitDx=0;exitDy=0;entryDx=0;entryDy=0;",
    )
    if clear_route:
        _clear_edge_waypoints(edge)


def _refresh_inheritance_edge_styles(root) -> None:
    """Reconcile inheritance edge styles with final class geometry."""
    from drawio_tools import get_all_classes

    classes = get_all_classes(root)
    id_to_geo = {cid: (x, y, w, h) for cid, name, x, y, w, h in classes}
    id_to_name = {cid: name for cid, name, *_ in classes}
    parent_entry_slots: Dict[str, int] = {}

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        style = cell.get("style", "")
        if "endFill=0" not in style or "endArrow=block" not in style:
            continue
        src_id = cell.get("source", "")
        tgt_id = cell.get("target", "")
        if src_id not in id_to_geo or tgt_id not in id_to_geo:
            continue
        sx, sy, sw, sh = id_to_geo[src_id]
        tx, ty, tw, th = id_to_geo[tgt_id]
        tgt_name = id_to_name[tgt_id]
        same_col = abs((sx + sw / 2) - (tx + tw / 2)) <= CELL_WIDTH * 0.55
        if same_col:
            if not _is_straight_inheritance(style):
                _apply_inheritance_edge_type(cell, orthogonal=False, clear_route=True)
            continue
        slot = parent_entry_slots.get(tgt_name, 0)
        parent_entry_slots[tgt_name] = slot + 1
        from drawio_tools import _parse_style_float

        existing_nx = _parse_style_float(style, "entryX")
        entry_x = existing_nx if existing_nx is not None else _fan_slot(slot)
        was_straight = _is_straight_inheritance(style)
        _apply_inheritance_edge_type(
            cell, orthogonal=True, entry_x=entry_x, clear_route=was_straight
        )


def _path_crosses_obstacles(
    points: List[Tuple[float, float]],
    classes: List[Tuple],
    src_id: str,
    tgt_id: str,
    margin: int = 3,
) -> bool:
    from drawio_tools import _line_intersects_rect

    for i in range(len(points) - 1):
        x1, y1 = points[i]
        x2, y2 = points[i + 1]
        for cid, _cname, cx, cy, cw, ch in classes:
            if cid in (src_id, tgt_id):
                continue
            if _line_intersects_rect(x1, y1, x2, y2, cx, cy, cw, ch, margin):
                return True
    return False


def _diagram_bounds(classes: List[Tuple]) -> Tuple[float, float, float, float]:
    min_x = min(c[2] for c in classes)
    min_y = min(c[3] for c in classes)
    max_x = max(c[2] + c[4] for c in classes)
    max_y = max(c[3] + c[5] for c in classes)
    return min_x, min_y, max_x, max_y


def _route_edge_with_waypoints(
    root,
    src_name: str,
    tgt_name: str,
    obstacle_name: str,
    corridor_idx: int = 0,
) -> bool:
    """Add orthogonal waypoints verified clear of obstructing classes and other edges."""
    return _force_unique_edge_route(root, src_name, tgt_name, corridor_idx)


def _classify_waypoint_route(
    wps: List[Tuple[float, float]],
    p1: Tuple[float, float],
    p2: Tuple[float, float],
    min_y: float,
    max_y: float,
    min_x: float,
    max_x: float,
) -> str:
    if not wps:
        return "right"
    wy = wps[0][1]
    wx = wps[0][0]
    if wy < min(p1[1], p2[1]) - 20:
        return "top"
    if wy > max(p1[1], p2[1]) + 20:
        return "bottom"
    if wx < min(p1[0], p2[0]) - 20:
        return "left"
    return "right"


def _apply_route_anchors(edge: ET.Element, route: str) -> None:
    from drawio_tools import _parse_style_float

    style = edge.get("style", "")
    ex = _parse_style_float(style, "exitX")
    ey = _parse_style_float(style, "exitY")
    nx = _parse_style_float(style, "entryX")
    ny = _parse_style_float(style, "entryY")
    if route == "top":
        set_edge_anchors(
            edge,
            exit_x=ex if ex is not None else 0.5,
            exit_y=0,
            entry_x=nx if nx is not None else 0.5,
            entry_y=0,
        )
    elif route == "bottom":
        set_edge_anchors(
            edge,
            exit_x=ex if ex is not None else 0.5,
            exit_y=1,
            entry_x=nx if nx is not None else 0.5,
            entry_y=1,
        )
    elif route == "left":
        set_edge_anchors(
            edge,
            exit_x=0,
            exit_y=ey if ey is not None else 0.5,
            entry_x=0,
            entry_y=ny if ny is not None else 0.5,
        )
    else:
        set_edge_anchors(
            edge,
            exit_x=1,
            exit_y=ey if ey is not None else 0.5,
            entry_x=1,
            entry_y=ny if ny is not None else 0.5,
        )


def _edge_still_crosses(root, src_name: str, tgt_name: str) -> bool:
    from drawio_tools import check_edges_crossing_classes

    prefix = f"{src_name}->{tgt_name} "
    return any(desc.startswith(prefix) for desc, _ in check_edges_crossing_classes(root))


def _route_edge_clear(root, src_name: str, tgt_name: str, corridor_idx: int) -> bool:
    """Try multiple corridor offsets until this edge is clear of classes and other edges."""
    for attempt in range(24):
        if _force_unique_edge_route(root, src_name, tgt_name, corridor_idx + attempt * 2):
            return True
    return False


def _route_edge_crossings(root, corridor_start: int = 0) -> int:
    """Add waypoints for every edge-crosses-class violation. Returns count routed."""
    from drawio_tools import check_edges_crossing_classes

    routed = 0
    crossings = check_edges_crossing_classes(root)
    seen: Set[Tuple[str, str]] = set()
    for edge_desc, _crossed in crossings:
        m = re.match(r"(.+?)->(.+?) \(", edge_desc)
        if not m:
            continue
        src, tgt = m.group(1).strip(), m.group(2).strip()
        key = (src, tgt)
        if key in seen:
            continue
        seen.add(key)
        edge = _find_edge_cell(root, src, tgt)
        if edge is not None and _is_straight_inheritance(edge.get("style", "")):
            _apply_inheritance_edge_type(edge, orthogonal=True, entry_x=0.5)
        if _route_edge_clear(root, src, tgt, corridor_start + routed):
            routed += 1
    return routed


def _sync_positions_from_root(root, positions: Dict[str, Tuple[int, int]]) -> None:
    from drawio_tools import get_all_classes

    for _, name, x, y, _, _ in get_all_classes(root):
        positions[name] = (x, y)


def _fix_overlaps_on_canvas(root, positions: Dict[str, Tuple[int, int]]) -> bool:
    from drawio_tools import check_overlaps, get_all_classes, set_geometry

    classes = get_all_classes(root)
    overlaps = check_overlaps(classes)
    if not overlaps:
        return False
    name_to_geo = {name: (x, y, w, h) for _, name, x, y, w, h in classes}
    for name_a, name_b in overlaps:
        xa, ya, wa, ha = name_to_geo[name_a]
        xb, yb, wb, hb = name_to_geo[name_b]
        cell_b = find_cell_by_name(root, name_b)
        if cell_b is None:
            continue
        if abs(xa - xb) < wa * 0.6:
            new_y = int(ya + ha + OVERLAP_PAD)
            set_geometry(cell_b, y=new_y)
            positions[name_b] = (xb, new_y)
        else:
            new_x = int(xa + wa + OVERLAP_PAD)
            set_geometry(cell_b, x=new_x)
            positions[name_b] = (new_x, yb)
    return True


def _fix_hierarchy_on_canvas(root, positions: Dict[str, Tuple[int, int]], msg: str) -> bool:
    from drawio_tools import get_all_classes, set_geometry

    m = re.search(
        r"Inheritance: (.+?) \(y=(\d+)\) should be below parent (.+?) \(y=(\d+)\)",
        msg,
    )
    if not m:
        return False
    child, _, parent, _ = m.group(1), m.group(2), m.group(3), m.group(4)
    classes = get_all_classes(root)
    name_to_geo = {name: (x, y, w, h) for _, name, x, y, w, h in classes}
    if child not in name_to_geo or parent not in name_to_geo:
        return False
    cell = find_cell_by_name(root, child)
    if cell is None:
        return False
    _, py, _, ph = name_to_geo[parent]
    cx, _, _, _ = name_to_geo[child]
    new_y = int(py + ph + ROW_GAP)
    set_geometry(cell, y=new_y)
    positions[child] = (cx, new_y)
    return True


def _fix_edge_cross_on_canvas(
    root,
    positions: Dict[str, Tuple[int, int]],
    msg: str,
    corridor_idx: int = 0,
) -> bool:
    m = re.search(r"Edge (.+?)->(.+?) \(", msg)
    cross_m = re.search(r"crosses through (.+?)(?: \(approx\))?$", msg)
    if not m or not cross_m:
        return False
    src, tgt = m.group(1).strip(), m.group(2).strip()
    crossed = cross_m.group(1).strip()
    if crossed in (src, tgt):
        return False

    if _route_edge_clear(root, src, tgt, corridor_idx):
        return True

    edge = _find_edge_cell(root, src, tgt)
    if edge is not None and _is_straight_inheritance(edge.get("style", "")):
        _apply_inheritance_edge_type(edge, orthogonal=True, entry_x=0.5)
        if _route_edge_clear(root, src, tgt, corridor_idx):
            return True

    from drawio_tools import get_all_classes, set_geometry

    classes = get_all_classes(root)
    name_to_geo = {name: (x, y, w, h) for _, name, x, y, w, h in classes}
    if crossed not in name_to_geo or src not in name_to_geo or tgt not in name_to_geo:
        return False

    cell = find_cell_by_name(root, crossed)
    if cell is None:
        return False

    sx, sy, sw, sh = name_to_geo[src]
    tx, ty, tw, th = name_to_geo[tgt]
    cx, cy, cw, ch = name_to_geo[crossed]

    src_cy = sy + sh / 2
    tgt_cy = ty + th / 2
    src_cx = sx + sw / 2
    tgt_cx = tx + tw / 2

    if abs(src_cy - tgt_cy) < 80:
        edge_y = (src_cy + tgt_cy) / 2
        if cy + ch / 2 >= edge_y:
            new_y = int(max(START_Y, edge_y + 120))
        else:
            new_y = int(max(START_Y, edge_y - ch - 120))
        set_geometry(cell, y=new_y)
        positions[crossed] = (cx, new_y)
    else:
        edge_x = (src_cx + tgt_cx) / 2
        if cx + cw / 2 >= edge_x:
            new_x = int(edge_x + 120)
        else:
            new_x = int(max(START_X, edge_x - cw - 120))
        set_geometry(cell, x=new_x)
        positions[crossed] = (new_x, cy)
    return True


def _optimize_layout(root, positions: Dict[str, Tuple[int, int]]) -> None:
    """Iteratively fix overlaps, hierarchy, and edge crossings."""
    from drawio_tools import validate_layout

    corridor_idx = 0
    for _ in range(40):
        violations = validate_layout(root)
        critical = [v for v in violations if v[0] in CRITICAL_RULES]
        if not critical:
            return

        if _fix_overlaps_on_canvas(root, positions):
            _sync_positions_from_root(root, positions)
            continue

        routed = _route_edge_crossings(root, corridor_idx)
        if routed:
            corridor_idx += routed
            continue

        changed = False
        for rule, msg in critical:
            if rule == "hierarchy_flow" and _fix_hierarchy_on_canvas(root, positions, msg):
                changed = True
                break
            if rule == "edge_crosses_class" and _fix_edge_cross_on_canvas(
                root, positions, msg, corridor_idx
            ):
                corridor_idx += 1
                changed = True
                break

        if changed:
            _sync_positions_from_root(root, positions)
            continue

        break


def _route_edge_outer_desperate(
    root, src_name: str, tgt_name: str, route_index: int = 0
) -> bool:
    """Route around the entire diagram with large outer margins."""
    for bump in range(32):
        if _force_unique_edge_route(root, src_name, tgt_name, route_index + bump * 3 + 80):
            return True
    return False


def _finalize_page_routes(root, positions: Optional[Dict[str, Tuple[int, int]]] = None) -> None:
    """Route every edge that still crosses a class until clear or no progress."""
    _refresh_inheritance_edge_styles(root)
    _fix_class_crossing_edges(root, max_rounds=40)


def _deoverlap(
    root,
    positions: Dict[str, Tuple[int, int]],
    name_to_id: Dict[str, str],
    index: Dict[str, Concept],
) -> None:
    """Legacy shim — optimization handled by _optimize_layout."""
    _optimize_layout(root, positions)


def build_diagram(source_path: Path, output_path: Optional[Path] = None) -> Path:
    if output_path is None:
        if "object-model" in source_path.name:
            output_path = source_path.parent / source_path.name.replace(
                "object-model", "class-diagram"
            ).replace(".md", ".drawio")
        elif "crc-" in source_path.name:
            output_path = source_path.parent / source_path.name.replace(
                "crc-", "class-diagram-"
            ).replace(".md", ".drawio")
        else:
            output_path = source_path.parent / f"class-diagram-{source_path.stem}.drawio"

    kas = parse_source(source_path)
    index = _concept_index(kas)

    if output_path.exists():
        _, mxfile = load_drawio(output_path)
    else:
        mxfile = create_empty_mxfile()

    existing_pages = {d.get("name") for d in mxfile.findall("diagram")}
    for ka in kas:
        if ka.name not in existing_pages:
            add_page(mxfile, ka.name, page_width=2400, page_height=1800)
        _build_page(mxfile, ka, kas, index)

    save_drawio(output_path, mxfile)
    return output_path


def audit_diagram(path: Path) -> bool:
    report = audit_diagram_report(str(path))
    print(report)
    _, mxfile = load_drawio(path)
    all_pass = True
    for d in mxfile.findall("diagram"):
        pname = d.get("name")
        _, root = get_page(mxfile, pname)
        if root is None:
            continue
        violations = validate_layout(root)
        critical = [v for v in violations if v[0] in ("class_overlap", "edge_crosses_class", "hierarchy_flow")]
        if critical:
            all_pass = False
            print(f"\n=== CRITICAL on page '{pname}' ===")
            for rule, msg in critical:
                print(f"  [{rule}] {msg}")
    return all_pass


def resolve_increment_source(inc: int) -> Path:
    folder = REPO_ROOT / "docs" / f"increment-{inc}"
    om = folder / f"object-model-increment-{inc}.md"
    if om.exists():
        return om
    crc = folder / f"crc-increment-{inc}.md"
    if crc.exists():
        return crc
    raise FileNotFoundError(f"No object model or CRC for increment {inc} in {folder}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Build domain class diagrams from OM/CRC sources")
    parser.add_argument("--increment", type=int, help="Increment number (1-6)")
    parser.add_argument("--source", type=Path, help="Path to object-model or CRC markdown")
    parser.add_argument("--output", type=Path, help="Output .drawio path")
    parser.add_argument("--all-increments", action="store_true", help="Build diagrams for increments 1-6")
    parser.add_argument("--audit-only", action="store_true", help="Only run audit on existing diagram")
    args = parser.parse_args()

    paths: List[Path] = []
    if args.all_increments:
        for n in range(1, 7):
            try:
                paths.append(resolve_increment_source(n))
            except FileNotFoundError as e:
                print(f"Skip: {e}")
    elif args.source:
        paths = [args.source.resolve()]
    elif args.increment:
        paths = [resolve_increment_source(args.increment)]
    else:
        parser.error("Specify --increment, --source, or --all-increments")

    rc = 0
    for src in paths:
        print(f"\n{'='*60}\nSource: {src}")
        if args.audit_only:
            out = args.output or src.parent / src.name.replace("object-model", "class-diagram").replace("crc-", "class-diagram-")
            if not out.exists():
                out = src.parent / f"class-diagram-increment-{src.parent.name.split('-')[-1]}.drawio"
        else:
            out = build_diagram(src, args.output)
            print(f"Wrote: {out}")
        if not audit_diagram(out):
            rc = 1
    return rc


if __name__ == "__main__":
    sys.exit(main())
