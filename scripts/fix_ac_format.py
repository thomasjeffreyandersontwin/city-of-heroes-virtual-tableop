"""Normalize all acceptance-criteria*.md files to the abd-acceptance-criteria format:
  - ### Story Name  ->  ## Story: Story Name
  - WHEN / THEN / AND / BUT (unbolded) -> **WHEN** / **THEN** / **AND** / **BUT**
"""
import re
from pathlib import Path

files = list(Path("docs").rglob("acceptance-criteria*.md"))
for src in sorted(files):
    text = src.read_text(encoding="utf-8")
    original = text

    # ### heading -> ## Story: heading
    text = re.sub(r"^### (.+)$", r"## Story: \1", text, flags=re.MULTILINE)

    # Bold WHEN / THEN / AND / BUT — only unbolded occurrences
    for kw in ("WHEN", "THEN", "AND", "BUT"):
        text = re.sub(rf"(?<!\*\*){kw}(?!\*\*)", f"**{kw}**", text)

    if text != original:
        src.write_text(text, encoding="utf-8")
        print(f"[FIXED] {src}")
    else:
        print(f"[OK]    {src}")
