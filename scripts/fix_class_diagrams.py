"""
Fix Draw.io class diagrams: remove waypoints, apply semantic column layout,
add missing inheritance edges, and use clean anchors.
"""
import xml.etree.ElementTree as ET
import re
import html
import os
import sys
from collections import defaultdict

CLASS_WIDTH = 260
COL_SPACING = 340
ROW_SPACING = 200
IMPORT_Y = 60
FIRST_ROW_Y = 300


def extract_class_name(value_attr):
    """Extract the bold class name from the HTML value."""
    if not value_attr:
        return None, None
    match = re.search(r'<b>([^<]+)</b>', value_attr)
    if not match:
        return None, None
    full_name = html.unescape(match.group(1)).strip()
    # Check for inheritance: "ClassName : BaseClass"
    if ' : ' in full_name:
        parts = full_name.split(' : ', 1)
        return parts[0].strip(), parts[1].strip()
    return full_name, None


def is_import_class(style):
    """Check if a class has the dashed import style."""
    if not style:
        return False
    return 'dashed=1' in style or 'dashPattern=' in style


def get_class_height(value_attr):
    """Estimate class height based on content lines."""
    if not value_attr:
        return 80
    lines = value_attr.count('<br/>')
    if lines <= 3:
        return 80
    elif lines <= 5:
        return 96
    elif lines <= 7:
        return 110
    elif lines <= 9:
        return 126
    elif lines <= 11:
        return 142
    elif lines <= 13:
        return 174
    elif lines <= 15:
        return 190
    else:
        return 210 + (lines - 15) * 14


def parse_page_classes(root_elem):
    """Parse all vertex (class) cells from a page."""
    classes = {}
    for cell in root_elem.iter('mxCell'):
        if cell.get('vertex') == '1' and cell.get('parent') == '1':
            cell_id = cell.get('id')
            value = cell.get('value', '')
            style = cell.get('style', '')
            name, base_class = extract_class_name(value)
            if name:
                geom = cell.find('mxGeometry')
                height = int(float(geom.get('height', '80'))) if geom is not None else 80
                classes[cell_id] = {
                    'name': name,
                    'base_class': base_class,
                    'is_import': is_import_class(style),
                    'cell': cell,
                    'height': height,
                    'value': value,
                    'style': style,
                }
    return classes


def parse_page_edges(root_elem):
    """Parse all edge cells from a page."""
    edges = []
    for cell in root_elem.iter('mxCell'):
        if cell.get('edge') == '1' and cell.get('parent') == '1':
            edges.append({
                'cell': cell,
                'source': cell.get('source'),
                'target': cell.get('target'),
                'style': cell.get('style', ''),
            })
    return edges


def remove_waypoints(root_elem):
    """Remove all <Array as='points'> elements from edge geometries."""
    for geom in root_elem.iter('mxGeometry'):
        arrays_to_remove = []
        for child in geom:
            if child.tag == 'Array' and child.get('as') == 'points':
                arrays_to_remove.append(child)
        for arr in arrays_to_remove:
            geom.remove(arr)
        # Also remove sourcePoint/targetPoint that are explicit
        points_to_remove = []
        for child in geom:
            if child.tag == 'mxPoint' and child.get('as') in ('sourcePoint', 'targetPoint'):
                points_to_remove.append(child)
        for pt in points_to_remove:
            geom.remove(pt)


def find_inheritance_edges(edges, classes):
    """Find existing inheritance edges (block arrow)."""
    inheritance_edges = set()
    for edge in edges:
        style = edge['style']
        if 'endArrow=block' in style and 'endFill=0' in style:
            source = edge['source']
            target = edge['target']
            if source and target:
                inheritance_edges.add((source, target))
    return inheritance_edges


def find_missing_inheritance(classes, existing_inheritance_edges):
    """Find classes that declare inheritance but lack an edge."""
    missing = []
    for cell_id, cls in classes.items():
        if cls['base_class']:
            base_name = cls['base_class']
            # Find the base class cell ID
            base_id = None
            for other_id, other_cls in classes.items():
                if other_cls['name'] == base_name:
                    base_id = other_id
                    break
            if base_id:
                # Check if edge exists (source=derived, target=base)
                if (cell_id, base_id) not in existing_inheritance_edges:
                    missing.append((cell_id, base_id))
    return missing


def build_relationship_graph(classes, edges):
    """Build adjacency for non-import classes based on edges."""
    graph = defaultdict(set)
    for edge in edges:
        src = edge['source']
        tgt = edge['target']
        if src and tgt and src in classes and tgt in classes:
            graph[src].add(tgt)
            graph[tgt].add(src)
    return graph


def compute_semantic_layout(classes, edges):
    """Compute new x,y positions using semantic column grouping."""
    imports = {cid: cls for cid, cls in classes.items() if cls['is_import']}
    locals_ = {cid: cls for cid, cls in classes.items() if not cls['is_import']}

    if not locals_:
        # All imports, just spread them
        positions = {}
        for i, (cid, cls) in enumerate(imports.items()):
            positions[cid] = (60 + i * COL_SPACING, IMPORT_Y)
        return positions

    # Build relationship graph among local classes
    graph = build_relationship_graph(classes, edges)

    # Find which imports each local class connects to
    local_to_imports = defaultdict(set)
    for edge in edges:
        src, tgt = edge['source'], edge['target']
        if src in locals_ and tgt in imports:
            local_to_imports[src].add(tgt)
        elif tgt in locals_ and src in imports:
            local_to_imports[tgt].add(src)

    # Group local classes by their inheritance hierarchy
    # Find root classes (no base or base is an import)
    roots = []
    children_of = defaultdict(list)
    for cid, cls in locals_.items():
        if cls['base_class']:
            base_id = None
            for other_id, other_cls in classes.items():
                if other_cls['name'] == cls['base_class']:
                    base_id = other_id
                    break
            if base_id and base_id in locals_:
                children_of[base_id].append(cid)
            else:
                roots.append(cid)
        else:
            roots.append(cid)

    # Sort roots: those with more children first, then alphabetically
    roots.sort(key=lambda cid: (-len(children_of.get(cid, [])), classes[cid]['name']))

    # Build columns: each root + its descendants form a column
    columns = []
    assigned = set()

    def get_descendants(root_id):
        """Get all descendants in DFS order."""
        result = [root_id]
        for child in sorted(children_of.get(root_id, []),
                           key=lambda c: classes[c]['name']):
            result.extend(get_descendants(child))
        return result

    for root in roots:
        if root not in assigned:
            col = get_descendants(root)
            for c in col:
                assigned.add(c)
            columns.append(col)

    # Add any unassigned locals
    for cid in locals_:
        if cid not in assigned:
            columns.append([cid])
            assigned.add(cid)

    # Determine import ordering based on which columns reference them
    import_to_columns = defaultdict(set)
    for col_idx, col in enumerate(columns):
        for cid in col:
            for imp_id in local_to_imports.get(cid, set()):
                import_to_columns[imp_id].add(col_idx)

    # Sort imports by average column index they serve
    import_order = sorted(imports.keys(),
                         key=lambda imp: (
                             min(import_to_columns.get(imp, {len(columns)})),
                             classes[imp]['name']
                         ))

    # Calculate positions
    positions = {}

    # Place imports across the top row
    num_imports = len(import_order)
    if num_imports > 0:
        import_spacing = max(COL_SPACING, 
                            (max(len(columns), num_imports) * COL_SPACING) // num_imports)
        for i, imp_id in enumerate(import_order):
            positions[imp_id] = (60 + i * COL_SPACING, IMPORT_Y)

    # Place local classes in columns
    for col_idx, col in enumerate(columns):
        x = 60 + col_idx * COL_SPACING
        y = FIRST_ROW_Y
        for i, cid in enumerate(col):
            positions[cid] = (x, y)
            y += classes[cid]['height'] + ROW_SPACING - 40

    # If there are more columns than imports, widen imports
    total_width = max(
        (len(columns)) * COL_SPACING if columns else 0,
        num_imports * COL_SPACING if num_imports > 0 else 0
    )

    # Re-center imports if fewer than columns
    if num_imports > 0 and num_imports < len(columns):
        total_col_width = len(columns) * COL_SPACING
        import_spacing = total_col_width // num_imports
        for i, imp_id in enumerate(import_order):
            positions[imp_id] = (60 + i * import_spacing, IMPORT_Y)

    return positions


def set_edge_style(edge_cell, source_id, target_id, classes, positions, 
                   is_inheritance=False, edge_index=0, total_edges_from_source=1):
    """Set clean edge style with proper anchors."""
    base_style = "edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;"
    
    if is_inheritance:
        base_style += "endArrow=block;endFill=0;endSize=12;"
    else:
        base_style += "endArrow=open;endSize=12;"

    # Calculate anchors based on relative positions
    if source_id in positions and target_id in positions:
        sx, sy = positions[source_id]
        tx, ty = positions[target_id]
        
        src_height = classes[source_id]['height'] if source_id in classes else 80
        tgt_height = classes[target_id]['height'] if target_id in classes else 80

        if sy > ty:  # Source below target: exit top, enter bottom
            exit_x = 0.5
            exit_y = 0
            entry_x = 0.5
            entry_y = 1
            # Offset for multiple edges
            if total_edges_from_source > 1:
                offset = (edge_index + 1) / (total_edges_from_source + 1)
                exit_x = offset
        elif sy < ty:  # Source above target: exit bottom, enter top
            exit_x = 0.5
            exit_y = 1
            entry_x = 0.5
            entry_y = 0
            if total_edges_from_source > 1:
                offset = (edge_index + 1) / (total_edges_from_source + 1)
                exit_x = offset
        elif sx < tx:  # Source left of target: exit right, enter left
            exit_x = 1
            exit_y = 0.5
            entry_x = 0
            entry_y = 0.5
        else:  # Source right of target: exit left, enter right
            exit_x = 0
            exit_y = 0.5
            entry_x = 1
            entry_y = 0.5

        base_style += f"exitX={exit_x};exitY={exit_y};exitDx=0;exitDy=0;"
        base_style += f"entryX={entry_x};entryY={entry_y};entryDx=0;entryDy=0;"

    edge_cell.set('style', base_style)


def add_inheritance_edge(root_elem, parent_cell, source_id, target_id, 
                        positions, classes, next_id):
    """Add a new inheritance edge."""
    edge = ET.SubElement(parent_cell, 'mxCell')
    edge.set('id', str(next_id))
    edge.set('value', '')
    edge.set('edge', '1')
    edge.set('parent', '1')
    edge.set('source', source_id)
    edge.set('target', target_id)

    # Calculate anchors
    sx, sy = positions.get(source_id, (0, 0))
    tx, ty = positions.get(target_id, (0, 0))
    
    # Inheritance: derived below, base above -> exit top, enter bottom
    # Use distinct entry points for multiple children
    children_of_target = [cid for cid, cls in classes.items() 
                         if cls.get('base_class') and 
                         any(oid for oid, ocls in classes.items() 
                             if ocls['name'] == cls['base_class'] and oid == target_id)]
    
    entry_x = 0.5
    if len(children_of_target) > 1:
        idx = children_of_target.index(source_id) if source_id in children_of_target else 0
        entry_x = 0.25 + (idx * 0.5 / max(1, len(children_of_target) - 1))

    style = (f"edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;"
             f"endArrow=block;endFill=0;endSize=12;"
             f"exitX=0.5;exitY=0;exitDx=0;exitDy=0;"
             f"entryX={entry_x};entryY=1;entryDx=0;entryDy=0;")
    edge.set('style', style)

    geom = ET.SubElement(edge, 'mxGeometry')
    geom.set('relative', '1')
    geom.set('as', 'geometry')

    return edge


def fix_page(diagram_elem, skip=False):
    """Fix a single diagram page."""
    if skip:
        return {'skipped': True}

    page_name = diagram_elem.get('name', 'Unknown')
    graph_model = diagram_elem.find('mxGraphModel')
    if graph_model is None:
        return {'error': 'No mxGraphModel found'}

    root_elem = graph_model.find('root')
    if root_elem is None:
        return {'error': 'No root element found'}

    # Parse classes and edges
    classes = parse_page_classes(root_elem)
    edges = parse_page_edges(root_elem)

    if not classes:
        return {'error': 'No classes found'}

    # Remove all waypoints
    remove_waypoints(root_elem)

    # Compute layout
    positions = compute_semantic_layout(classes, edges)

    # Apply new positions to class cells
    for cell_id, pos in positions.items():
        if cell_id in classes:
            geom = classes[cell_id]['cell'].find('mxGeometry')
            if geom is not None:
                geom.set('x', str(pos[0]))
                geom.set('y', str(pos[1]))

    # Fix edge styles
    # Count edges from each source for offset calculation
    edges_from_source = defaultdict(list)
    for edge in edges:
        if edge['source']:
            edges_from_source[edge['source']].append(edge)

    for edge in edges:
        src = edge['source']
        tgt = edge['target']
        if not src or not tgt:
            continue
        
        style = edge['style']
        is_inh = 'endArrow=block' in style and 'endFill=0' in style
        
        edge_list = edges_from_source.get(src, [edge])
        idx = edge_list.index(edge) if edge in edge_list else 0
        total = len(edge_list)
        
        set_edge_style(edge['cell'], src, tgt, classes, positions,
                      is_inheritance=is_inh, edge_index=idx, 
                      total_edges_from_source=total)

    # Check for missing inheritance edges
    existing_inheritance = find_inheritance_edges(edges, classes)
    missing = find_missing_inheritance(classes, existing_inheritance)

    # Find max cell ID for new edges
    max_id = 0
    for cell in root_elem.iter('mxCell'):
        try:
            cell_id = int(cell.get('id', '0'))
            if cell_id > max_id:
                max_id = cell_id
        except ValueError:
            pass

    added_inheritance = []
    for derived_id, base_id in missing:
        max_id += 1
        add_inheritance_edge(root_elem, root_elem, derived_id, base_id,
                           positions, classes, max_id)
        derived_name = classes[derived_id]['name']
        base_name = classes[base_id]['name']
        added_inheritance.append(f"{derived_name} -> {base_name}")

    # Adjust canvas size
    num_classes = len(classes)
    new_width = max(2400, (len(positions) // 2 + 1) * COL_SPACING + 200)
    new_height = max(1800, max(y + 300 for _, y in positions.values()) if positions else 1800)
    graph_model.set('pageWidth', str(new_width))
    graph_model.set('pageHeight', str(new_height))

    return {
        'page_name': page_name,
        'classes': len(classes),
        'edges': len(edges),
        'waypoints_removed': True,
        'added_inheritance': added_inheritance,
    }


def process_file(filepath, skip_pages=None):
    """Process a single .drawio file."""
    skip_pages = skip_pages or set()
    
    tree = ET.parse(filepath)
    root = tree.getroot()

    results = []
    for diagram in root.findall('diagram'):
        page_id = diagram.get('id', '')
        page_name = diagram.get('name', '')
        
        should_skip = page_id in skip_pages or page_name in skip_pages
        result = fix_page(diagram, skip=should_skip)
        result['page_id'] = page_id
        result['page_name'] = page_name
        results.append(result)

    # Write back
    tree.write(filepath, xml_declaration=False, encoding='unicode')
    
    # Re-read and fix formatting (add newlines for readability)
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Ensure proper XML structure
    if not content.startswith('<mxfile'):
        content = content.lstrip()
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

    return results


def main():
    base_dir = r'c:\hero-desktop\city-of-heroes-virtual-tabletop\docs'
    
    files_to_process = [
        {
            'path': os.path.join(base_dir, 'increment-2', 'class-diagram-increment-2.drawio'),
            'skip_pages': {'page_identity', 'Identity'},
        },
        {
            'path': os.path.join(base_dir, 'increment-3', 'class-diagram-increment-3.drawio'),
            'skip_pages': set(),
        },
        {
            'path': os.path.join(base_dir, 'increment-4', 'class-diagram-increment-4.drawio'),
            'skip_pages': set(),
        },
        {
            'path': os.path.join(base_dir, 'increment-5', 'class-diagram-increment-5.drawio'),
            'skip_pages': set(),
        },
        {
            'path': os.path.join(base_dir, 'increment-6', 'class-diagram-increment-6.drawio'),
            'skip_pages': set(),
        },
    ]

    all_results = {}
    for file_info in files_to_process:
        filepath = file_info['path']
        print(f"\n{'='*60}")
        print(f"Processing: {filepath}")
        print(f"{'='*60}")
        
        if not os.path.exists(filepath):
            print(f"  ERROR: File not found!")
            continue

        results = process_file(filepath, file_info['skip_pages'])
        all_results[filepath] = results
        
        for r in results:
            if r.get('skipped'):
                print(f"  Page '{r['page_name']}': SKIPPED (already fixed)")
            elif r.get('error'):
                print(f"  Page '{r['page_name']}': ERROR - {r['error']}")
            else:
                print(f"  Page '{r['page_name']}': Fixed ({r['classes']} classes, {r['edges']} edges)")
                if r.get('added_inheritance'):
                    for inh in r['added_inheritance']:
                        print(f"    + Added inheritance: {inh}")

    # Summary
    print(f"\n{'='*60}")
    print("SUMMARY")
    print(f"{'='*60}")
    
    total_pages_fixed = 0
    total_inheritance_added = 0
    for filepath, results in all_results.items():
        inc_name = os.path.basename(os.path.dirname(filepath))
        pages_fixed = [r for r in results if not r.get('skipped') and not r.get('error')]
        inh_added = sum(len(r.get('added_inheritance', [])) for r in results)
        total_pages_fixed += len(pages_fixed)
        total_inheritance_added += inh_added
        print(f"  {inc_name}: {len(pages_fixed)} pages fixed, {inh_added} inheritance edges added")
        for r in pages_fixed:
            print(f"    - {r['page_name']}")

    print(f"\n  Total: {total_pages_fixed} pages fixed, {total_inheritance_added} inheritance edges added")


if __name__ == '__main__':
    main()
