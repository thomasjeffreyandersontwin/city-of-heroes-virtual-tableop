# Lo-fi Wireframes — Increment 1: Character and Crowd Library

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 1: Character and Crowd Library |
| Increment outcome | GM can create, organize, browse, and persist *characters* and *crowds* |
| UL file | `docs/domain/ubiquitous-language-increment-1.md` |
| AC file | `docs/stories/acceptance-criteria-increment-1.md` |
| State JSON | `docs/ux/lo-fi/increment-1-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-1.drawio` |
| IA reference | `docs/ux/initial-ia.md` |
| Design references | `Design/1) Character and Crowds/*.png` (12 images) |
| Date | 2026-05-17 |
| Generator | hand-crafted mxGraph XML |

---

## Screen 1: Game Directory Prompt

**Layout:** `modal` — centered dialog, single column
**Context:** pre-session — appears on startup when *COH game directory* is absent or invalid
**Grid position:** col=0, row=0

### ASCII sketch

```
[Game Directory Prompt]
┌───────────────────────────────────────────┐
│                                           │
│  COH Game Directory  [___________________]│
│  Validation feedback [___________________]│
│                                           │
│                      [Browse...] [Continue]│
│                                           │
└───────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| directory entry form | form | body | COH Game Directory (text input), Validation feedback (read-only label), Browse button, Continue button | Continue is disabled until path validates; Browse opens OS folder picker; Validation feedback shows error text on invalid path |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| COH Game Directory | text | Path entry; validated on every keystroke; auto-populated by Browse picker |
| Validation feedback | text (read-only) | Blank when valid; shows inline error when invalid |
| Browse... | button | Opens OS folder picker; populates path field; triggers re-validation |
| Continue | button (primary) | Disabled while path is invalid; enabled only when validation passes; dismisses modal |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Validate City of Heroes Game Directory | directory entry form | validation on field change |
| Prompt for Game Directory if Invalid | directory entry form | modal opens when path absent or invalid |

---

## Screen 2: Character Explorer

**Layout:** `panel-with-collapsed-tabs` — Character Explorer expanded (left), Active Roster and Edit Character collapsed as vertical side tabs (right)
**Context:** pre-session — primary workspace after game directory validated
**Grid position:** col=1, row=0

### ASCII sketch

```
[Character Explorer]                          +  +
┌──────────────────────────────────────────┐  │  │
│ Character Explorer                    ─ □│  │  │
│ [New][Cut][Find][Clone][Paste][+Crwd]    │  │  │
│ [Del][Edit]                              │  A  E
│ ┌─Spyder─────────────────────────────┐   │  c  d
│ └────────────────────────────────────┘   │  t  i
│                                          │  i  t
│ -  All Characters                        │  v  
│   +  Crowd 1                             │  e  C
│   -  Crowd 2                             │     h
│       Character 1                        │  R  a
│       Character 2                        │  o  r
│       Character 3                        │  s  a
│       Spyder           ← selected        │  t  c
│   +  Crowd 3                             │  e  t
│   +  Crowd 4                             │  r  e
│   +  Crowd 5                             │     r
│   +  Crowd 6                             │     
│   +  Crowd 7                             │     
│   +  Crowd 8                             │     
│   +  Crowd 9                        ■    │     
│                                          │     
└──────────────────────────────────────────┘     
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| toolbar | toolbar | panel | New, Cut, Find, Clone, Paste, +Crowd, Del, Edit, Save (9 icon buttons) | Matches production toolbar icons; Save triggers Save Dirty Crowds (Ctrl+S) |
| filter bar | form | panel | Filter by Name (text input) | Live substring match; collapses non-matching nodes |
| crowd tree | tree | panel | Hierarchical crowd/character tree with expand/collapse (`+`/`-`); dirty indicator (`*` prefix) on modified crowds; source-file indicator (`●` suffix) on crowds with a saved path | `*` = *dirty flag* set (unsaved changes); `●` = *crowd source file* set; `* ●` = dirty and has a file (will save on Ctrl+S); no prefix = clean and no file. Selected item highlighted blue. |
| save status bar | chrome | footer | Status message e.g. "Saved 2, failed 0, skipped 1 ● Crowd 1 has no source file — use Save to New File" | Appears after every Save Dirty Crowds invocation; shows per-crowd save summary |
| collapsed side tabs | collapsed-tabs | side | Active Roster tab, Edit Character tab | Vertical text with `+` icon; click expands the panel |

### Controls detail — crowd tree

| Control | Type | State / behavior |
| --- | --- | --- |
| crowd node (`+`/`-`) | tree node | Click `+` to expand, `-` to collapse; bold when expanded |
| `*` dirty prefix | indicator | Present when *dirty flag* is true; cleared after successful save |
| `●` source-file suffix | indicator | Present when *crowd source file* is non-null; set by Save to New File |
| character node | tree leaf | Click to select (blue highlight); double-click or F2 for inline rename |
| drag-drop | gesture | Drag character between crowds; drag crowd onto crowd to nest |
| right-click | gesture | Opens context menu with full command set |

### Context menu commands

| Command | Shortcut | Behavior |
| --- | --- | --- |
| New | Ctrl+N | Creates new crowd or character under selected node |
| Edit | Ctrl+E | Opens character editor for selected item |
| Delete | Del | Removes selected item with confirmation |
| Save Dirty Crowds | Ctrl+S | Saves all dirty crowds with a *crowd source file* set; prompts Save to New File for crowds without one |
| Save to New File… | Ctrl+Shift+S | Opens OS save-file dialog; assigns chosen path as *crowd source file*; adds to *active crowd list* |
| Cut | Ctrl+X | Removes from current location, holds on *clipboard* |
| Clone | Ctrl+C | Deep-copies character with "(Copy)" suffix |
| Link | Ctrl+L | Adds as *linked member* in target crowd |
| Paste | Ctrl+V | Places *clipboard* item under selected crowd |
| Spawn | Alt+S | Spawns NPC in game world |
| Place | Alt+P | Places NPC at location in game world |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Create Crowd | toolbar / context menu | New button or Ctrl+N |
| Rename Crowd | toolbar / context menu | Edit button or Ctrl+E |
| Delete Crowd | toolbar / context menu | Del button or Del key |
| Nest Crowd inside Crowd | crowd tree | Drag-drop crowd onto crowd |
| Create Character in Crowd | toolbar / context menu | New button under selected crowd |
| Rename Character | toolbar / context menu | Edit button or F2 inline |
| Delete Character from Crowd | toolbar / context menu | Del button or Del key |
| Clone Character | toolbar / context menu | Clone button or Ctrl+C |
| Cut Character to Clipboard | toolbar / context menu | Cut button or Ctrl+X |
| Link Character across Crowds | toolbar / context menu | Link button or Ctrl+L |
| Filter Characters by Name | filter bar | Text input (live) |
| Track Source File per Crowd | crowd tree | `*`/`●` indicators on crowd nodes; persists via crowd repository |
| Save Dirty Crowds to Source Files | toolbar / context menu | Ctrl+S — writes each dirty crowd with source file; prompts for crowds without one |
| Save Crowd to New File | context menu | Ctrl+Shift+S — OS save-file dialog; assigns *crowd source file*; updates *active crowd list* |

---

## Screen 3: Context Menu

**Layout:** `popup-overlay` — shadow-boxed popup appearing on right-click
**Context:** triggered by right-clicking any node in the crowd tree
**Grid position:** col=2, row=0

### ASCII sketch

```
┌─────────────────────────────┐
│ New                  Ctrl+N │
│ Edit                 Ctrl+E │
│ Delete                  Del │
│ Save Dirty Crowds    Ctrl+S │
│ Save to New File… Ctrl+S+S  │
│─────────────────────────────│
│ Cut                  Ctrl+X │
│ Clone                Ctrl+C │
│ Link                 Ctrl+L │
│ Paste                Ctrl+V │
│─────────────────────────────│
│ Spawn                 Alt+S │
│ Place                 Alt+P │
└─────────────────────────────┘
```

### Regions

| Region | Type | Controls |
| --- | --- | --- |
| structure commands | menu-group | New, Edit, Delete, Save Dirty Crowds, Save to New File… |
| clipboard commands | menu-group | Cut, Clone, Link, Paste |
| game commands | menu-group | Spawn, Place |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| game directory prompt | character explorer | valid path |

---

## Design Reference Notes

Design images reviewed from `Design/1) Character and Crowds/`:

| Image | Key observations |
| --- | --- |
| 0) All Collapsed.png | Shows three collapsed vertical panel tabs: Character Explorer, Active Roster, Edit Character |
| Character & Crowd Explorer - activate all commands from context menu.png | Character Explorer expanded with tree; full context menu visible (10 items in 3 groups); toolbar with 8 icon buttons |
| Character & Crowd Explorer - Add Character.png | Context menu on All Characters root node; shows New command creating character |
| Character & Crowd Explorer - Add Crowd.png | +Crowd toolbar button highlighted; "Unnamed Crowd" added to tree |
| Character & Crowd Explorer - Browse Characters.png | Tree with multiple crowds; context menu visible; navigation tabs at top |
| Character & Crowd Explorer - Clone and Paste Character.png | Clone/Paste icons highlighted; Character (1) clone appears in tree |
| Character & Crowd Explorer - Clone and Paste Crowd.png | Crowd 1 (1) clone with Character 1 (1), Character 2 (1) copies |
| Character & Crowd Explorer - cut and paste character.png | Cut/Paste icons highlighted; character moved between crowds |
| Character & Crowd Explorer - Delete Character.png | Delete icon highlighted; character being removed from crowd |
| Character & Crowd Explorer - Filter.png | Filter "Spy" active; tree shows only matching: Spydera, Spyder |
| Character & Crowd Explorer - Link and Paste Character _ Crowd.png | Link icon highlighted; linked copies with (1) suffix created |
| Character & Crowd Explorer - Rename.png | Inline text edit box around "Renamed Character" in tree |

**Key design decisions captured:**
- Toolbar is a horizontal row of 8 compact icon buttons (not stacked full-width buttons)
- Tree uses `+`/`-` expand/collapse indicators with indented hierarchy
- Character nodes are leaf items without expand/collapse prefix
- Context menu has 10 commands in 3 separated groups with keyboard shortcuts
- Active Roster and Edit Character appear as collapsed vertical side tabs when Character Explorer is expanded
- Filter is a simple text field that live-filters the tree

---

## Change log

| Date | Direction | Summary |
| --- | --- | --- |
| 2026-05-17 | redrawn | Hand-crafted mxGraph XML faithfully reproducing production UI from design images; tree view with hierarchical expand/collapse, toolbar as horizontal icon button row, context menu with 3 command groups, collapsed vertical side tabs |
| 2026-05-18 | updated spec | Added crowd source file tracking (dirty `*` / source-file `●` indicators on crowd tree), Save Dirty Crowds (Ctrl+S), Save to New File… (Ctrl+Shift+S) to context menu; added save status bar region; updated stories covered and domain-term traces to reflect 3 new stories |
