#!/usr/bin/env python3
"""Fix validator-reported layout issues on specific class diagram pages."""

from __future__ import annotations

import re
import xml.etree.ElementTree as ET
from pathlib import Path

W = 260
GAP = 80
ROSTER_GAP = 100


def row_x(count: int, start: int = 60) -> list[int]:
    return [start + i * (W + GAP) for i in range(count)]


def set_geom(cell: ET.Element, x: float, y: float, width: float | None = None, height: float | None = None) -> None:
    geom = cell.find("mxGeometry")
    if geom is None:
        return
    geom.set("x", str(int(x)))
    geom.set("y", str(int(y)))
    if width is not None:
        geom.set("width", str(int(width)))
    if height is not None:
        geom.set("height", str(int(height)))


def class_name(cell: ET.Element) -> str:
    value = cell.get("value") or ""
    m = re.search(r"<b>([^<]+)</b>", value)
    return m.group(1) if m else ""


def find_by_name(cells: dict[str, ET.Element], name: str) -> ET.Element:
    for cell in cells.values():
        if cell.get("vertex") == "1" and name in class_name(cell):
            return cell
    raise KeyError(name)


def strip_waypoints(root: ET.Element) -> None:
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        geom = cell.find("mxGeometry")
        if geom is None:
            continue
        for arr in geom.findall("Array"):
            geom.remove(arr)


def set_style_entry(cell: ET.Element, entry_x: float) -> None:
    style = cell.get("style") or ""
    if "entryX=" in style:
        style = re.sub(r"entryX=[^;]+", f"entryX={entry_x}", style)
    else:
        style = style.rstrip(";") + f";entryX={entry_x}"
    cell.set("style", style)


def fix_edge_style(cell: ET.Element, **attrs: float) -> None:
    style = cell.get("style") or ""
    style = re.sub(r"elbow=[^;]*;?", "", style)
    for key, val in attrs.items():
        token = f"{key}={val}"
        if f"{key}=" in style:
            style = re.sub(rf"{key}=[^;]+", token, style)
        else:
            style = style.rstrip(";") + f";{token}"
    cell.set("style", style)


def set_waypoints(cell: ET.Element, points: list[tuple[int, int]]) -> None:
    geom = cell.find("mxGeometry")
    if geom is None:
        return
    for arr in list(geom.findall("Array")):
        geom.remove(arr)
    if not points:
        return
    arr = ET.SubElement(geom, "Array", {"as": "points"})
    for x, y in points:
        ET.SubElement(arr, "mxPoint", {"x": str(x), "y": str(y)})


def _anchor(style: str, geo: tuple[float, float, float, float], end: str) -> tuple[float, float]:
    x, y, w, h = geo
    ax = _parse_style_float(style, f"{end}X")
    ay = _parse_style_float(style, f"{end}Y")
    if ax is None:
        ax = 0.5
    if ay is None:
        ay = 0.5
    return x + w * ax, y + h * ay


def _parse_style_float(style: str, key: str) -> float | None:
    m = re.search(rf"{key}=([0-9.]+)", style)
    return float(m.group(1)) if m else None


def _cell_geo(cell: ET.Element) -> tuple[float, float, float, float]:
    geom = cell.find("mxGeometry")
    assert geom is not None
    return (
        float(geom.get("x", "0")),
        float(geom.get("y", "0")),
        float(geom.get("width", str(W))),
        float(geom.get("height", "80")),
    )


LANE_LEFT = 370
LANE_MID = 730
LANE_RIGHT = 1450


def _lane_for_target(tx: float, tw: float = W) -> int:
    center = tx + tw / 2
    if center < 420:
        return LANE_LEFT
    if center < 680:
        return LANE_MID
    return LANE_RIGHT


def _route_corridor(
    src_geo: tuple[float, float, float, float],
    tgt_geo: tuple[float, float, float, float],
    style: str,
    *,
    band: float,
) -> list[tuple[int, int]]:
    ex, ey = _anchor(style, src_geo, "exit")
    nx, ny = _anchor(style, tgt_geo, "entry")
    tx, ty, tw, th = tgt_geo
    lane = _lane_for_target(tx, tw)
    if ny > band + 40:
        return [
            (lane, int(ey)),
            (lane, int(band)),
            (lane, int(ny)),
            (int(nx), int(ny)),
        ]
    if abs(ex - nx) < 2:
        return [(lane, int(band))]
    return [(lane, int(ey)), (lane, int(band)), (int(nx), int(band))]


def fix_animation_element_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    imports = [
        "Animated Ability",
        "FX Resource",
        "Game Bridge",
        "Identity",
        "Movement Resource",
        "Sound Resource",
        "Spawned NPC",
    ]
    xs_imp = row_x(len(imports))
    for name, x in zip(imports, xs_imp):
        set_geom(find_by_name(cells, name), x, 60)

    derived = [
        ("FX Element : Animation Element", 0.1),
        ("Load-Identity Element : Animation Element", 0.2),
        ("Movement Element : Animation Element", 0.35),
        ("Pause Element : Animation Element", 0.5),
        ("Reference Element : Animation Element", 0.65),
        ("Sequence Element : Animation Element", 0.8),
        ("Sound Element : Animation Element", 0.9),
    ]
    xs = row_x(len(derived))
    parent = find_by_name(cells, "Animation Element")
    center_x = xs[0] + (xs[-1] + W - xs[0]) / 2 - W / 2
    set_geom(parent, center_x, 300)

    find_by_name(cells, "Animation Sequence")
    set_geom(find_by_name(cells, "Animation Sequence"), center_x + W + GAP, 300)

    for (name, entry_x), x in zip(derived, xs):
        cell = find_by_name(cells, name.split(":")[0].strip())
        set_geom(cell, x, 540)

    parent_id = parent.get("id")
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1" or cell.get("target") != parent_id:
            continue
        style = cell.get("style") or ""
        if "endArrow=block" not in style:
            continue
        source = cells.get(cell.get("source", ""))
        if source is None:
            continue
        for name, entry_x in derived:
            if name.split(":")[0].strip() in class_name(source):
                set_style_entry(cell, entry_x)
                break

    strip_waypoints(root)
    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", str(xs_imp[-1] + W + 60))
        model.set("pageHeight", "900")


def fix_keybind_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    derived = [
        ("Delete NPC Command : Game Command", 0.25),
        ("Load Costume Command : Game Command", 0.4),
        ("Spawn NPC Command : Game Command", 0.6),
        ("Target by Name Command : Game Command", 0.75),
    ]
    xs = row_x(len(derived))
    parent = find_by_name(cells, "Game Command")
    center_x = xs[0] + (xs[-1] + W - xs[0]) / 2 - W / 2
    # Drop Game Command below KeyBind row so KeyBind File→KeyBind clears the box.
    set_geom(parent, center_x, 430)

    set_geom(find_by_name(cells, "KeyBind"), center_x - W - GAP, 300)
    set_geom(find_by_name(cells, "KeyBind File"), center_x + W + GAP, 300)

    for (name, entry_x), x in zip(derived, xs):
        cell = find_by_name(cells, name.split(":")[0].strip())
        set_geom(cell, x, 540)

    parent_id = parent.get("id")
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1" or cell.get("target") != parent_id:
            continue
        style = cell.get("style") or ""
        if "endArrow=block" not in style:
            continue
        source = cells.get(cell.get("source", ""))
        if source is None:
            continue
        for name, entry_x in derived:
            if name.split(":")[0].strip() in class_name(source):
                set_style_entry(cell, entry_x)
                break

    kb = class_name(find_by_name(cells, "KeyBind"))
    kbf = class_name(find_by_name(cells, "KeyBind File"))
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn, tn = class_name(src), class_name(tgt)
        if sn.startswith("KeyBind File") and tn == "KeyBind":
            fix_edge_style(cell, exitX=0, exitY=0.75, entryX=1, entryY=0.75)
        elif sn == "KeyBind" and tn.startswith("Slash Command"):
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.25, entryY=1)
        elif sn.startswith("KeyBind File") and tn.startswith("COH Game Directory"):
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.75, entryY=1)

    strip_waypoints(root)
    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", "1500")
        model.set("pageHeight", "900")


def _row_x_wide(count: int, start: int = 60, gap: int = ROSTER_GAP) -> list[int]:
    return [start + i * (W + gap) for i in range(count)]


def _cell_x(cells: dict[str, ET.Element], name: str) -> float:
    geom = find_by_name(cells, name).find("mxGeometry")
    assert geom is not None
    return float(geom.get("x", "0"))


def fix_roster_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    top = [
        ("Character", 0),
        ("Crowd", 1),
        ("Spawned NPC", 2),
        ("Game Bridge", 3),
        ("Game Done State", 4),
    ]
    xs_top = _row_x_wide(len(top))
    for name, idx in top:
        set_geom(find_by_name(cells, name), xs_top[idx], 60)

    # Middle tier in inner columns — vertical corridors at cols 0 and 4 stay clear.
    set_geom(find_by_name(cells, "Character Overlay"), xs_top[1], 210)
    set_geom(find_by_name(cells, "Context Menu"), xs_top[2], 210)

    set_geom(find_by_name(cells, "Roster"), xs_top[0], 500)

    bottom = [
        ("Gang Leader", 0),
        ("Roster Entry", 1),
        ("Gang Mode", 2),
        ("Spawned State", 3),
        ("Active Character", 4),
    ]
    xs_bottom = _row_x_wide(len(bottom))
    for name, idx in bottom:
        set_geom(find_by_name(cells, name), xs_bottom[idx], 780)

    entry_top = [0.08, 0.16, 0.24, 0.32, 0.4, 0.48, 0.56, 0.64, 0.72]
    entry_i = 0
    left_exit_i = 0
    right_exit_i = 0
    roster_exit_i = 0

    def next_entry() -> float:
        nonlocal entry_i
        v = entry_top[entry_i % len(entry_top)]
        entry_i += 1
        return v

    def side_exit_y(left: bool) -> float:
        nonlocal left_exit_i, right_exit_i
        if left:
            y = 0.1 + (left_exit_i % 6) * 0.12
            left_exit_i += 1
        else:
            y = 0.1 + (right_exit_i % 6) * 0.12
            right_exit_i += 1
        return y

    middle_tier = {"Character Overlay", "Context Menu"}
    overlay_entry = [0.15, 0.35, 0.55, 0.75]
    overlay_i = 0

    def next_overlay_entry() -> float:
        nonlocal overlay_i
        v = overlay_entry[overlay_i % len(overlay_entry)]
        overlay_i += 1
        return v

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn, tn = class_name(src), class_name(tgt)
        sx = _cell_x(cells, sn)
        tx = _cell_x(cells, tn)

        if sn == "Roster" and tn == "Roster Entry":
            fix_edge_style(cell, exitX=1, exitY=0.5, entryX=0, entryY=0.5)
        elif sn == "Roster":
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=next_entry(), entryY=1)
            if tn == "Gang Mode":
                fix_edge_style(cell, exitX=0.5, exitY=0, entryY=0)
        elif tn in middle_tier:
            left_route = tx <= sx
            ey = side_exit_y(left_route)
            ex = next_overlay_entry()
            if left_route:
                fix_edge_style(cell, exitX=0, exitY=ey, entryX=ex, entryY=0)
            else:
                fix_edge_style(cell, exitX=1, exitY=ey, entryX=ex, entryY=0)
        elif sn in ("Roster Entry", "Gang Leader", "Gang Mode", "Spawned State", "Active Character"):
            left_route = tx < sx - 20
            ey = side_exit_y(left_route)
            if left_route:
                fix_edge_style(cell, exitX=0, exitY=ey, entryX=next_entry(), entryY=1)
            else:
                fix_edge_style(cell, exitX=1, exitY=ey, entryX=next_entry(), entryY=1)

    strip_waypoints(root)

    top_band = 185
    mid_band = 700

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn, tn = class_name(src), class_name(tgt)
        style = cell.get("style") or ""
        sgeo, tgeo = _cell_geo(src), _cell_geo(tgt)
        if sn == "Roster" and tn != "Roster Entry":
            set_waypoints(cell, _route_corridor(sgeo, tgeo, style, band=top_band))
        elif sn in ("Roster Entry", "Gang Leader", "Gang Mode", "Spawned State", "Active Character"):
            _, sy, _, _ = sgeo
            _, ty, _, _ = tgeo
            band = top_band if sy > ty else mid_band
            set_waypoints(cell, _route_corridor(sgeo, tgeo, style, band=band))
        elif tn in middle_tier:
            set_waypoints(cell, _route_corridor(sgeo, tgeo, style, band=mid_band))

    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", str(xs_top[-1] + W + 60))
        model.set("pageHeight", "1050")


def dedup_gsq_inherit_edges(diagram: ET.Element) -> None:
    """Remove duplicate gsq_inherit_* mxCells without changing layout or routing."""
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    seen: set[str] = set()
    to_remove: list[ET.Element] = []
    for cell in root.findall("mxCell"):
        cid = cell.get("id") or ""
        if not cid.startswith("gsq_inherit_"):
            continue
        if cid in seen:
            to_remove.append(cell)
        else:
            seen.add(cid)
    for cell in to_remove:
        root.remove(cell)


def fix_game_state_query_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    top = [
        ("Desktop Overlay", 0),
        ("Game Bridge", 1),
        ("Roster Entry", 2),
        ("Spawned NPC", 3),
        ("Spawned State", 4),
    ]
    xs_top = row_x(len(top))
    for name, idx in top:
        set_geom(find_by_name(cells, name), xs_top[idx], 60)

    set_geom(find_by_name(cells, "Game State Query"), xs_top[1], 280)

    bottom = [
        ("Command Chain", 0),
        ("Oversized Command Chain", 1),
        ("Game Done State", 2),
        ("Hovered NPC Info", 3),
        ("Mouse XYZ Position", 4),
    ]
    xs_bottom = row_x(len(bottom))
    for name, idx in bottom:
        set_geom(find_by_name(cells, name), xs_bottom[idx], 520)

    gsq = find_by_name(cells, "Game State Query")
    gsq_id = gsq.get("id")
    bottom_names = [n for n, _ in bottom]
    inherit_entry = [0.1, 0.25, 0.4, 0.55, 0.7]

    # Replace downward GSQ→result associations with upward inheritance from bottom row.
    to_remove = []
    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        cid = cell.get("id") or ""
        if cid.startswith("gsq_inherit_"):
            to_remove.append(cell)
            continue
        if cell.get("source") != gsq_id:
            continue
        tgt = cells.get(cell.get("target", ""))
        if tgt is not None and class_name(tgt).split(":")[0].strip() in bottom_names:
            to_remove.append(cell)
    for cell in to_remove:
        root.remove(cell)

    for (name, _), entry_x in zip(bottom, inherit_entry):
        child = find_by_name(cells, name)
        edge = ET.Element(
            "mxCell",
            {
                "id": f"gsq_inherit_{name.replace(' ', '_')}",
                "edge": "1",
                "parent": "1",
                "source": child.get("id"),
                "target": gsq_id,
                "style": (
                    "edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;"
                    "endArrow=block;endFill=0;endSize=12;"
                    f"exitX=0.5;exitY=0;exitDx=0;exitDy=0;"
                    f"entryX={entry_x};entryY=1;entryDx=0;entryDy=0;"
                ),
                "value": "",
            },
        )
        geo = ET.SubElement(edge, "mxGeometry", {"relative": "1", "as": "geometry"})
        root.append(edge)

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn, tn = class_name(src), class_name(tgt)
        if sn.startswith("Game State Query") and tn == "Game Bridge":
            fix_edge_style(cell, exitX=1, exitY=0.5, entryX=0, entryY=0.75)
        elif sn.startswith("Game Done State") and tn.startswith("Desktop Overlay"):
            fix_edge_style(cell, exitX=0, exitY=0.5, entryX=1, entryY=0.5)
        elif sn.startswith("Game Done State"):
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.5, entryY=1)
        elif sn.startswith("Command Chain") and tn == "Game Bridge":
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.25, entryY=0)
        elif sn.startswith("Oversized Command Chain") and tn == "Game Bridge":
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.75, entryY=0)
        elif sn.startswith("Hovered NPC Info") and tn.startswith("Spawned NPC"):
            fix_edge_style(cell, exitX=0.5, exitY=0, entryX=0.5, entryY=1)

    strip_waypoints(root)
    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", "1740")
        model.set("pageHeight", "900")


def fix_popup_menu_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    # Remove ghost vertices (empty non-edge cells)
    to_remove = []
    for cell in list(root.findall("mxCell")):
        if cell.get("edge") == "1":
            continue
        if cell.get("vertex") != "1":
            continue
        if (cell.get("value") or "").strip():
            continue
        style = cell.get("style") or ""
        if "swimlane" in style or "group" in style or "rounded=0" in style:
            to_remove.append(cell)
    for cell in to_remove:
        root.remove(cell)

    set_geom(find_by_name(cells, "Game Bridge"), 420, 60)
    set_geom(find_by_name(cells, "Pop-Up Menu"), 60, 280)
    set_geom(find_by_name(cells, "Area Attack Pop-Up Menu"), 400, 280)
    set_geom(find_by_name(cells, "COH Menus Directory"), 230, 480)

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn = class_name(src)
        tn = class_name(tgt)
        if sn.startswith("Pop-Up Menu") and tn == "Game Bridge":
            fix_edge_style(cell, exitX=0.25, exitY=0, entryX=0.5, entryY=1)
        elif sn.startswith("Area Attack") and tn == "Game Bridge":
            fix_edge_style(cell, exitX=0.75, exitY=0, entryX=0.75, entryY=1)
        elif sn.startswith("Area Attack") and tn.startswith("Pop-Up Menu"):
            fix_edge_style(cell, exitX=0, exitY=0.5, entryX=1, entryY=0.5)
        elif sn.startswith("Area Attack") and tn.startswith("COH Menus"):
            fix_edge_style(cell, exitX=0.25, exitY=1, entryX=0.75, entryY=0)
        elif sn.startswith("Pop-Up Menu") and tn.startswith("COH Menus"):
            fix_edge_style(cell, exitX=0.5, exitY=1, entryX=0.25, entryY=0)

    strip_waypoints(root)
    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", "1100")
        model.set("pageHeight", "700")


def fix_desktop_overlay_page(diagram: ET.Element) -> None:
    root = diagram.find("./mxGraphModel/root")
    assert root is not None
    cells = {c.get("id", ""): c for c in root.findall("mxCell") if c.get("id")}

    top = [
        ("Active Character", 0),
        ("Character", 1),
        ("Gang Mode", 2),
        ("Spawned NPC", 3),
    ]
    xs_top = row_x(len(top))
    for name, idx in top:
        set_geom(find_by_name(cells, name), xs_top[idx], 60)

    mid = [
        ("Memory Interface", 0),
        ("Movement Execution", 1),
        ("Roster Entry", 2),
    ]
    xs_mid = row_x(len(mid))
    for name, idx in mid:
        set_geom(find_by_name(cells, name), xs_mid[idx], 260)

    set_geom(find_by_name(cells, "Spawned State"), xs_top[3], 460)
    set_geom(find_by_name(cells, "Context Menu"), xs_mid[2], 446)

    set_geom(find_by_name(cells, "Desktop Overlay"), xs_mid[0], 1014)
    set_geom(find_by_name(cells, "Character Overlay"), xs_mid[1], 1014)
    set_geom(find_by_name(cells, "Multi-Select"), xs_mid[2], 1014)

    co_entry = [0.1, 0.2, 0.35, 0.5, 0.65, 0.8]
    co_i = 0

    def co_top_entry() -> float:
        nonlocal co_i
        v = co_entry[co_i % len(co_entry)]
        co_i += 1
        return v

    do_exit = [0.15, 0.3, 0.45, 0.6, 0.75]
    do_i = 0

    def do_top_exit() -> float:
        nonlocal do_i
        v = do_exit[do_i % len(do_exit)]
        do_i += 1
        return v

    for cell in root.findall("mxCell"):
        if cell.get("edge") != "1":
            continue
        src = cells.get(cell.get("source", ""))
        tgt = cells.get(cell.get("target", ""))
        if src is None or tgt is None:
            continue
        sn, tn = class_name(src), class_name(tgt)
        if sn == "Character Overlay":
            fix_edge_style(
                cell,
                exitX=1,
                exitY=co_top_entry(),
                entryX=0,
                entryY=0.5,
            )
        elif sn == "Desktop Overlay" and tn == "Character Overlay":
            fix_edge_style(cell, exitX=1, exitY=0.5, entryX=0, entryY=0.5)
        elif sn == "Desktop Overlay":
            fix_edge_style(cell, exitX=0, exitY=do_top_exit(), entryX=0.5, entryY=1)
        elif sn == "Multi-Select" and tn == "Character Overlay":
            fix_edge_style(cell, exitX=0, exitY=0.5, entryX=1, entryY=0.5)
        elif sn == "Multi-Select" and tn == "Desktop Overlay":
            fix_edge_style(cell, exitX=0, exitY=0.25, entryX=1, entryY=0.5)

    strip_waypoints(root)
    model = diagram.find("mxGraphModel")
    if model is not None:
        model.set("pageWidth", "2400")
        model.set("pageHeight", "1800")


FIXERS = {
    "Animation Element": fix_animation_element_page,
    "KeyBind": fix_keybind_page,
    "Roster": fix_roster_page,
    "Game State Query": fix_game_state_query_page,
    "Pop-Up Menu": fix_popup_menu_page,
    "Desktop Overlay": fix_desktop_overlay_page,
}

FILES = {
    "Animation Element": Path("docs/increment-3/class-diagram-increment-3.drawio"),
    "KeyBind": Path("docs/increment-2/class-diagram-increment-2.drawio"),
    "Roster": Path("docs/increment-5/class-diagram-increment-5.drawio"),
    "Game State Query": Path("docs/increment-5/class-diagram-increment-5.drawio"),
    "Pop-Up Menu": Path("docs/increment-5/class-diagram-increment-5.drawio"),
    "Desktop Overlay": Path("docs/increment-5/class-diagram-increment-5.drawio"),
}


def main() -> None:
    import sys

    repo = Path(__file__).resolve().parents[1]
    if len(sys.argv) > 1 and sys.argv[1] == "increment-5-round2":
        path = repo / "docs/increment-5/class-diagram-increment-5.drawio"
        tree = ET.parse(path)
        mxfile = tree.getroot()
        for diagram in mxfile.findall("diagram"):
            name = diagram.get("name")
            if name == "Roster":
                fix_roster_page(diagram)
                print(f"Fixed page: Roster in {path.relative_to(repo)}")
            elif name == "Game State Query":
                dedup_gsq_inherit_edges(diagram)
                print(f"Deduped gsq_inherit edges: Game State Query in {path.relative_to(repo)}")
        tree.write(path, encoding="unicode", xml_declaration=False)
        return

    by_file: dict[Path, list[tuple[str, object]]] = {}
    for page_name, fixer in FIXERS.items():
        path = repo / FILES[page_name]
        by_file.setdefault(path, []).append((page_name, fixer))

    for path, pages in by_file.items():
        tree = ET.parse(path)
        mxfile = tree.getroot()
        for page_name, fixer in pages:
            for diagram in mxfile.findall("diagram"):
                if diagram.get("name") == page_name:
                    fixer(diagram)
                    print(f"Fixed page: {page_name} in {path.relative_to(repo)}")
                    break
            else:
                raise RuntimeError(f"Page not found: {page_name} in {path}")
        tree.write(path, encoding="unicode", xml_declaration=False)


if __name__ == "__main__":
    main()
