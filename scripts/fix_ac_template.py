#!/usr/bin/env python3
"""Bring all acceptance-criteria-increment-*.md files into full template compliance.

Template structure per story:
    ## Story: Title

    **Story type:** user | technical

    ### Domain terms        ← H3 heading (not bold text); absent for inc-1 (has SbE)

    - *Term* — desc

    ### Acceptance criteria

    1. **WHEN** ...
       **THEN** ...

Fixes applied to EVERY increment:
  1. Story heading normalisation — H2/H3 variants → ## Story: Name
  2. Bold WHEN/THEN/AND/BUT keywords in AC items
  3. Convert **Domain terms** / **Domain terms:** (bold) → ### Domain terms (H3)
  4. Insert **Story type:** after the story heading if absent
  5. Insert ### Acceptance criteria before the first numbered AC item if absent

Fix NOT applied to increment-1:
  - No ### Domain terms injection (SbE file covers that increment)
"""
from __future__ import annotations
import re
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
DOCS = PROJECT_ROOT / "docs"

# ---------------------------------------------------------------------------
# Keyword bolding
# ---------------------------------------------------------------------------
_KW_RE = re.compile(
    r"(?<!\*)\b(WHEN|THEN|AND|BUT|GIVEN)\b(?!\*)",
    re.MULTILINE,
)


def bold_keywords(text: str) -> str:
    """Wrap bare WHEN/THEN/AND/BUT/GIVEN in ** if not already bold."""
    return _KW_RE.sub(r"**\1**", text)


# ---------------------------------------------------------------------------
# Story-type heuristic
# ---------------------------------------------------------------------------
_TECH_RE = re.compile(
    r"\b(memory|pointer|process|dll|hook|native|inject|scan|stale|"
    r"camera rig|deploy script|p/invoke|bridge init|rig|collision|facing vector"
    r"|model matrix|rotation matrix|camera enable|camera disable)\b",
    re.IGNORECASE,
)


def story_type(title: str) -> str:
    return "technical" if _TECH_RE.search(title) else "user"


# ---------------------------------------------------------------------------
# Regexes for template element detection
# ---------------------------------------------------------------------------
_H2_STORY_RE   = re.compile(r"^## Story: ", re.MULTILINE)
_H3_STORY_RE   = re.compile(r"^### Story: ", re.MULTILINE)
_H3_PLAIN_RE   = re.compile(r"^### (?!Story: |Domain terms|Acceptance criteria)(.+)$", re.MULTILINE)
_H2_PLAIN_RE   = re.compile(r"^## (?!Story: |#)(.+)$", re.MULTILINE)
_STORY_TYPE_RE = re.compile(r"^\*\*Story type:", re.MULTILINE)
_DT_BOLD_RE    = re.compile(r"^\*\*Domain terms\*\*.*$", re.MULTILINE)
_DT_H3_RE      = re.compile(r"^### Domain terms", re.MULTILINE)
_AC_H3_RE      = re.compile(r"^### Acceptance criteria", re.MULTILINE)
_FIRST_NUM_RE  = re.compile(r"^(1\. )", re.MULTILINE)

# Section separator — marks that what preceded it was an epic/group heading
_SEP_RE = re.compile(r"^---\s*$", re.MULTILINE)


# ---------------------------------------------------------------------------
# Per-file normalisation
# ---------------------------------------------------------------------------

def is_story_block(block: str) -> bool:
    """True if block contains AC content (numbered list), making it a story block."""
    return bool(_FIRST_NUM_RE.search(block))


def fix_file(path: Path, add_domain_terms: bool = True) -> bool:
    raw = path.read_text(encoding="utf-8")
    text = raw

    # ── Step 1: Normalise story headings ──────────────────────────────────
    # Strategy: split on any H2 or H3 heading; reassemble, promoting story
    # headings to "## Story: " form.

    # Pattern matches H2 or H3 headings — excludes reserved template section headings
    _ANY_H_RE = re.compile(
        r"^(#{2,3}) (?!(?:Domain terms|Acceptance criteria)\s*$)(.+)$",
        re.MULTILINE,
    )

    # Find all heading positions + capture groups
    headings = list(_ANY_H_RE.finditer(text))
    if not headings:
        print(f"[SKIP] {path.name}: no headings found")
        return False

    # Build list of (start, end, original_line, level, title)
    segments = []
    for i, m in enumerate(headings):
        seg_end = headings[i + 1].start() if i + 1 < len(headings) else len(text)
        segments.append({
            "start": m.start(),
            "end": seg_end,
            "line": m.group(0),
            "level": len(m.group(1)),
            "raw_title": m.group(2),
            "body": text[m.end():seg_end],
        })

    preamble = text[: segments[0]["start"]]
    parts: list[str] = [preamble]

    for seg in segments:
        title = seg["raw_title"]
        body  = seg["body"]

        # Remove leading "Story: " so we can re-add it uniformly
        clean_title = re.sub(r"^Story:\s*", "", title).strip()

        # Is this a story block?  (has numbered AC items)
        if is_story_block(body):
            # ── 1a. Heading → ## Story: Title ──────────────────────────────
            new_heading = f"## Story: {clean_title}"

            # ── 1b. Bold bare keywords ──────────────────────────────────────
            body = bold_keywords(body)

            # ── 1c. Domain terms: bold → H3 ────────────────────────────────
            if add_domain_terms:
                body = _DT_BOLD_RE.sub("### Domain terms", body)

            # ── 1d. Story type ──────────────────────────────────────────────
            if not _STORY_TYPE_RE.search(body):
                # Strip any leading blank lines, insert story type, then blank
                body = f"\n\n**Story type:** {story_type(clean_title)}\n" + body.lstrip("\n")

            # ── 1e. ### Acceptance criteria ─────────────────────────────────
            if not _AC_H3_RE.search(body):
                body = _FIRST_NUM_RE.sub("### Acceptance criteria\n\n1. ", body, count=1)

            parts.append(new_heading + body)
        else:
            # Epic / group heading — keep as-is (H2, no "Story:" prefix)
            parts.append(seg["line"] + body)

    out = "".join(parts)

    if out == raw:
        print(f"[OK]    {path.name}")
        return False

    path.write_text(out, encoding="utf-8")
    print(f"[FIXED] {path.name}")
    return True


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    for i in range(1, 7):
        p = DOCS / f"increment-{i}" / f"acceptance-criteria-increment-{i}.md"
        if not p.exists():
            print(f"[MISS]  {p}")
            continue
        # Inc-1 has SbE; skip domain terms injection there
        add_dt = (i != 1)
        fix_file(p, add_domain_terms=add_dt)
    print("\nDone.")


if __name__ == "__main__":
    main()
