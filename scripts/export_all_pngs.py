"""Export all pages from all fixed drawio files as PNGs."""
import subprocess
import xml.etree.ElementTree as ET
import os

DRAWIO_EXE = r"C:\Program Files\draw.io\draw.io.exe"
BASE_DIR = r"c:\hero-desktop\city-of-heroes-virtual-tabletop\docs"

files = [
    os.path.join(BASE_DIR, "increment-2", "class-diagram-increment-2.drawio"),
    os.path.join(BASE_DIR, "increment-3", "class-diagram-increment-3.drawio"),
    os.path.join(BASE_DIR, "increment-4", "class-diagram-increment-4.drawio"),
    os.path.join(BASE_DIR, "increment-5", "class-diagram-increment-5.drawio"),
    os.path.join(BASE_DIR, "increment-6", "class-diagram-increment-6.drawio"),
]

all_pngs = []

for filepath in files:
    inc_dir = os.path.dirname(filepath)
    tree = ET.parse(filepath)
    root = tree.getroot()
    diagrams = root.findall('diagram')
    
    print(f"\nExporting: {os.path.basename(filepath)} ({len(diagrams)} pages)")
    
    for i, diagram in enumerate(diagrams):
        page_name = diagram.get('name', f'page_{i}')
        safe_name = page_name.lower().replace(' ', '-').replace(':', '').replace('/', '-')
        png_path = os.path.join(inc_dir, f"{safe_name}.png")
        
        # draw.io uses 1-based page index
        page_index = i + 1
        
        cmd = [
            DRAWIO_EXE,
            "--export",
            "--format", "png",
            "--page-index", str(page_index),
            "--output", png_path,
            filepath
        ]
        
        try:
            result = subprocess.run(cmd, capture_output=True, text=True, timeout=30)
            if result.returncode == 0:
                print(f"  [{page_index}] {page_name} -> {os.path.basename(png_path)}")
                all_pngs.append(png_path)
            else:
                print(f"  [{page_index}] {page_name} FAILED: {result.stderr}")
        except subprocess.TimeoutExpired:
            print(f"  [{page_index}] {page_name} TIMEOUT")
        except Exception as e:
            print(f"  [{page_index}] {page_name} ERROR: {e}")

print(f"\n{'='*60}")
print(f"Total PNGs exported: {len(all_pngs)}")
print(f"{'='*60}")
for png in all_pngs:
    print(f"  {png}")
