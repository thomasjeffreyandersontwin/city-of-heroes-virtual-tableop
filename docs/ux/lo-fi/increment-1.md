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
| Design references | `Design/1) Character and Crowds/*.png` |
| Date | 2026-05-17 |
| Generator | `drawio-mockup.mjs save` |

---

## Screen 1: game directory prompt

**Layout:** `modal` — centered dialog, single column  
**Context:** pre-session — appears on startup when *COH game directory* is absent or invalid

### ASCII sketch

```
[game directory prompt]
┌──────────────────────────────────────────┐
│ ╔════════════════════════════════════╗   │
│ ║  directory entry form              ║   │
│ ║   COH Game Directory               ║   │
│ ║   [________________________]       ║   │
│ ║   Validation feedback              ║   │
│ ║   [________________________]       ║   │
│ ╠════════════════════════════════════╣   │
│ ║  [Browse]  [Continue (disabled)]   ║   │
│ ╚════════════════════════════════════╝   │
└──────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| directory entry form | form | body | COH Game Directory (text input), Validation feedback (read-only label row), Browse button, Continue button | Continue is disabled until the path validates; Browse opens OS folder picker; Validation feedback is blank on valid path, error text on invalid path |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| COH Game Directory | text | Path entry; validated on every keystroke; auto-populated by Browse picker |
| Validation feedback | text (read-only) | Blank when valid; shows inline error when invalid (e.g. "Directory not found", "Not a valid COH installation") |
| Browse | button | Opens OS folder picker; selected path populates COH Game Directory field; triggers re-validation |
| Continue | button (primary) | Disabled while path is invalid; enabled only when validation passes; dismisses modal and unblocks startup |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Validate City of Heroes Game Directory | directory entry form | validation on field change |
| Prompt for Game Directory if Invalid | directory entry form | modal opens when path absent or invalid |

---

## Screen 2: crowd manager — identities

**Layout:** `sidebar` — crowd tree (panel slot, 33%) · tab content (body slot, 67%)  
**Context:** pre-session — primary workspace, default tab after startup

### ASCII sketch

```
[crowd manager — identities]
┌─────────────────────────┬───────────────────────────────────────────┐
│ [New Crowd][New Char]   │ [Identities]  Abilities  Movements        │
│ [Rename][Delete]        ├───────────────────────────────────────────┤
│ [Cut][Clone][Link]      │ identity list  (placeholder)              │
│ [Clone-Link]            │  name · type · active · default           │
│ [Flatten-Copy]          │  ·····    ·····    ·····    ·····         │
│ [Clone Mbrs]            │  ·····    ·····    ·····    ·····         │
│                         │                                           │
│ Filter: [_______][× Clr]│ [Add] [Remove] [Set Active]              │
│                         │                                           │
│ crowd / character   cnt │                                           │
│ ─────────────────────── │                                           │
│ + All Characters     23 │                                           │
│   + Armed Forces      8 │                                           │
│     ○ Sgt. Morris     - │                                           │
│     ○ Lt. Banner      - │                                           │
│   + Civilians         5 │                                           │
│     ○ Guard 1         - │                                           │
│     ○ Guard 2         - │                                           │
│   + Animals           3 │                                           │
├─────────────────────────┤                                           │
│ [By Concept]            │                                           │
│ [By Gangs]              │                                           │
│ [By COH Structure]      │                                           │
│ [All Characters]   ←act │                                           │
└─────────────────────────┴───────────────────────────────────────────┘
```

### Regions — panel slot (crowd tree, left 33%)

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| crowd tree toolbar — structural | toolbar | New Crowd, New Char, Rename, Delete | Create at root level or under selected crowd; Delete shows confirmation prompt |
| crowd tree toolbar — clipboard | toolbar | Cut, Clone, Link, Clone-Link | Cut removes from source immediately; Clone creates "(Copy)" suffix; Link adds as linked member with chain icon; Clone-Link clones and links in one step |
| crowd tree toolbar — batch ops | toolbar | Flatten-Copy, Clone Mbrs | Flatten-Copy works on selected crowd; Clone Mbrs copies all members of source crowd as linked members into target crowd |
| filter bar | form | Filter by Name (text), × Clear | Live substring match; collapses non-matching nodes; × Clear restores all nodes |
| crowd tree | list | crowd / character (name + hierarchy), cnt (member count) | Hierarchical display: crowd nodes expand/collapse (+/-); character nodes are leaves; drag-drop to reorder or re-nest; inline rename on double-click or F2; linked member nodes show chain icon |
| browse modes | button-bar | By Concept, By Gangs, By COH Structure, All Characters | Active mode changes tree grouping; By Concept shows concept category root nodes; By Gangs shows gang/crew/squad groups; By COH Structure shows faction hierarchy; All Characters shows flat alphabetical list |

### Regions — body slot (tab content, right 67%)

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| tab bar | nav-tabs | Identities (active), Abilities (greyed), Movements (greyed) | Abilities and Movements tabs are inactive/greyed in Increment 1 — implemented in Increments 2 and 3 |
| identity list (placeholder — Increment 2) | list | name, type, active, default columns; Add, Remove, Set Active buttons | Shown as empty placeholder with column headers; identity CRUD is Increment 2 scope; buttons visible for affordance but empty in Increment 1 |

### Controls detail — crowd tree

| Control | Type | State / behavior |
| --- | --- | --- |
| New Crowd | button | Creates crowd at root or under selected crowd; name in inline edit mode; Esc cancels |
| New Char | button | Creates character in selected crowd; name in inline edit mode; Esc cancels |
| Rename | button | Places selected node name in inline edit; Enter confirms; Esc cancels; duplicate name rejected |
| Delete | button | Shows confirmation prompt for crowd (includes all members) or character; linked members removed from all crowds |
| Cut | button | Removes member from current crowd; holds on clipboard; visual: node gone from tree |
| Clone | button | Deep-copies character in same crowd; "(Copy)" suffix; appears below original |
| Link | button | Adds character as linked member in target crowd; chain icon on node |
| Clone-Link | button | Clone + Link in one step; new copy appears with link indicator |
| Flatten-Copy | button | Replaces crowd's character members with numbered copies (Guard 1, Guard 2, …); breaks links |
| Clone Mbrs | button | Copies all members of source crowd as linked members into target crowd; source unchanged |
| Filter by Name | text input | Case-insensitive substring; live filter; tree collapses to matching nodes only |
| × Clear | button | Clears filter input; restores full tree to prior expand/collapse state |
| crowd tree list | hierarchical list | +/- expand; drag-drop for re-nest and move; right-click context menu (New, Edit, Delete, Cut, Clone, Link, Paste, Spawn, Place) |
| By Concept | button | Reorganizes tree into concept category groups (Animals, Armed Forces, Civilians, Vehicles, Supernatural, Other) |
| By Gangs | button | Reorganizes tree to show gangs/crews/squads groups only |
| By COH Structure | button | Reorganizes tree by COH faction/group hierarchy |
| All Characters | button (primary) | Shows *all characters crowd*: flat alphabetical list of every character |

### Stories covered — Screen 2

| Story | Region | Trigger |
| --- | --- | --- |
| Load Prism Shell and Module | (system) | Shell start |
| Open Character Crowd Main Workspace | (system) | Module load complete |
| Load Crowd Collection from Repository | (system) | Workspace opens |
| Deserialize Crowd Collection from JSON | (system) | Repository file read |
| Load Default Crowd Members from Embedded Resource | (system) | First run — no JSON file |
| Create Crowd | crowd tree toolbar — structural | New Crowd button |
| Rename Crowd | crowd tree toolbar — structural | Rename button on selected crowd |
| Delete Crowd | crowd tree toolbar — structural | Delete button on selected crowd |
| Nest Crowd inside Crowd | crowd tree | Drag-drop crowd onto crowd |
| Create Character in Crowd | crowd tree toolbar — structural | New Char button |
| Rename Character | crowd tree toolbar — structural | Rename button on selected character |
| Delete Character from Crowd | crowd tree toolbar — structural | Delete button on selected character |
| Clone Character | crowd tree toolbar — clipboard | Clone button |
| Cut Character to Clipboard | crowd tree toolbar — clipboard | Cut button |
| Link Character across Crowds | crowd tree toolbar — clipboard | Link button |
| Clone-Link Character | crowd tree toolbar — clipboard | Clone-Link button |
| Flatten-Copy Crowd into Numbered Characters | crowd tree toolbar — batch ops | Flatten-Copy button |
| Clone Memberships to Another Crowd | crowd tree toolbar — batch ops | Clone Mbrs button |
| Drag-Drop Character between Crowds | crowd tree | Drag character node onto target crowd |
| Filter Characters by Name | filter bar | Text input (live) |
| Browse Crowds by Concept | browse modes | By Concept button |
| Browse Crowds by Gangs, Crews, and Squads | browse modes | By Gangs button |
| Browse Crowds by COH Structure | browse modes | By COH Structure button |
| Browse All Characters Crowd | browse modes | All Characters button |
| Save Crowd Collection to Repository | (system) | Ctrl+S / toolbar Save |
| Serialize Crowd Collection to JSON | (system) | Save triggered |
| Create Daily Backup of Crowd Repository | (system) | On save (first time per day) and on load |
| Store Crowd Repository in COH Data Directory | (system) | On save |
| Back Up Repository on Load | (system) | On open (before read) |

---

## Conditional States

| Condition | Affected region / control | Visual state |
| --- | --- | --- |
| Path invalid in game directory prompt | Continue button | Disabled (greyed, non-clickable) |
| Path valid in game directory prompt | Continue button | Enabled (primary fill, clickable) |
| Validation error | Validation feedback label | Error text visible (red or italic) |
| No item selected in crowd tree | Rename, Delete, Cut, Clone, Link, Clone-Link, Flatten-Copy, Clone Mbrs | Greyed / disabled |
| Crowd selected | New Char, Rename, Delete, Flatten-Copy, Clone Mbrs | Enabled |
| Character selected | Rename, Delete, Cut, Clone, Link, Clone-Link | Enabled |
| All Characters crowd selected | Rename, Delete | Disabled — protected crowd |
| Clipboard empty | Paste (context menu) | Disabled |
| Clipboard has item | Paste (context menu) | Enabled |
| Filter text present | × Clear | Visible; crowd tree filtered |
| Browse mode active | Active browse mode button | Highlighted (primary fill) |
| Name conflict during rename/create | Inline edit field | Error ring + tooltip message |
| Linked member in tree | Character node | Chain icon visible beside name |

---

## Affordance Trace Table

Every interactive control mapped to the AC clause it satisfies.

| Control | Screen | AC clause |
| --- | --- | --- |
| COH Game Directory (text) | game directory prompt | Validate COH Game Directory AC1–AC5 |
| Validation feedback label | game directory prompt | Prompt for Game Directory AC2–AC6 |
| Browse button | game directory prompt | Prompt for Game Directory AC4 |
| Continue button (disabled) | game directory prompt | Prompt for Game Directory AC1, AC7 |
| Continue button (enabled) | game directory prompt | Prompt for Game Directory AC5; Validate Game Directory AC2 |
| Dismiss modal / open workspace | game directory prompt → crowd manager | Load Prism Shell AC1; Open Workspace AC1 |
| New Crowd button | crowd manager | Create Crowd AC1–AC5 |
| New Char button | crowd manager | Create Character in Crowd AC1–AC5 |
| Rename button (crowd) | crowd manager | Rename Crowd AC1–AC5 |
| Rename button (character) | crowd manager | Rename Character AC1–AC4 |
| Delete button (crowd) | crowd manager | Delete Crowd AC1–AC5 |
| Delete button (character) | crowd manager | Delete Character from Crowd AC1–AC5 |
| Inline edit confirm (Enter) | crowd manager | Rename Crowd AC2; Rename Character AC2 |
| Inline edit cancel (Esc) | crowd manager | Create Crowd AC4; Rename Crowd AC4; Create Character AC4; Rename Character AC4 |
| Duplicate name error | crowd manager | Create Crowd AC3; Rename Crowd AC3; Create Character AC3; Rename Character AC3 |
| Drag crowd onto crowd | crowd manager | Nest Crowd inside Crowd AC1–AC5 |
| Cut button | crowd manager | Cut Character to Clipboard AC1–AC5 |
| Clone button | crowd manager | Clone Character AC1–AC4 |
| Link button | crowd manager | Link Character across Crowds AC1–AC4 |
| Clone-Link button | crowd manager | Clone-Link Character AC1–AC3 |
| Flatten-Copy button | crowd manager | Flatten-Copy Crowd AC1–AC4 |
| Clone Mbrs button | crowd manager | Clone Memberships AC1–AC3 |
| Drag character onto crowd | crowd manager | Drag-Drop Character AC1–AC4 |
| Filter by Name (text) | crowd manager | Filter Characters by Name AC1–AC5 |
| × Clear button | crowd manager | Filter Characters by Name AC4 |
| By Concept button | crowd manager | Browse Crowds by Concept AC1–AC4 |
| By Gangs button | crowd manager | Browse Crowds by Gangs AC1–AC3 |
| By COH Structure button | crowd manager | Browse Crowds by COH Structure AC1–AC3 |
| All Characters button | crowd manager | Browse All Characters Crowd AC1–AC4 |
| Ctrl+S / Save toolbar | crowd manager | Save Crowd Collection AC1–AC4 |
| App open event | (system) | Load Crowd Collection AC1–AC5; Back Up on Load AC1–AC4 |
| First run (no JSON) | (system) | Load Default Crowd Members AC1–AC4 |
| JSON read | (system) | Deserialize Crowd Collection AC1–AC5 |
| Save event | (system) | Serialize Crowd Collection AC1–AC4; Daily Backup AC1–AC3; Store in COH Data Dir AC1–AC3 |

---

## CLI Snippet

```powershell
# Create output directory (idempotent)
New-Item -ItemType Directory -Force -Path "c:\hero-desktop\city-of-heroes-virtual-tabletop\docs\ux\lo-fi"

# Regenerate drawio from state JSON
node "C:\dev\agilebydesign-skills\skills\user-experience-design\abd-lo-mockup\scripts\drawio-mockup.mjs" `
  save `
  --state "c:\hero-desktop\city-of-heroes-virtual-tabletop\docs\ux\lo-fi\increment-1-state.json" `
  --out   "c:\hero-desktop\city-of-heroes-virtual-tabletop\docs\ux\lo-fi\increment-1.drawio"
```

---

## Change log

| Date | Direction | Summary |
| --- | --- | --- |
| 2026-05-17 | authored | Initial lo-fi for Increment 1 — game directory prompt (modal) + crowd manager — identities (sidebar); covers all 31 increment 1 stories; drawio generated via drawio-mockup.mjs |
