# Lo-fi Wireframes — Increment 3: Abilities

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 3: Abilities |
| Increment outcome | GM can create abilities with animation element trees, browse and assign MOV/FX/Sound resources, nest sequences, manage ability groups, and configure attacks |
| State JSON | `docs/ux/lo-fi/increment-3-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-3.drawio` |
| Design references | `Design/4) Ability/*.png` (20 images), `Design/5) Ability Groups/*.png` (2 images) |
| Date | 2026-05-17 |

---

## Screen 1: crowd manager — abilities

**Layout:** `sidebar` — crowd tree (panel slot, left 33% dimmed) · ability tab body (body slot, right 67%)
**Context:** pre-session — primary workspace, Abilities tab active
**Grid position:** col=1, row=1

### ASCII sketch

```
[crowd manager — abilities]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬────────────────────────────────────┐
╎ [New Crowd][New Char] ╎  Identities  [Abilities]  Movements│
╎ [Rename]  [Delete]    ├────────────────────────────────────┤
╎ (dimmed controls)     │ ability list                        │
╎                       │  name    │ key │ persist │ attack   │
╎ crowd tree (dimmed)   │ ────────┼─────┼─────────┼──────── │
╎  ▼ Animals (12)       │ Burst…  │  1  │         │    ●     │
╎    ─ Wolf_Lt_01       │ Fire…   │  2  │    ●    │          │
╎    ─ Wolf_Lt_02       │ Dodge   │  3  │         │          │
╎  ▶ Bears (3)          │ Shield… │  4  │    ●    │          │
╎  ▶ Civilians (15)     │         │     │         │          │
╎                       ├────────────────────────────────────┤
╎ [By Concept][All Char]│ [Create][Edit][Delete][Set Key]    │
╎                       │ [Toggle Persist] [Play●] [Stop]    │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| crowd tree (dimmed) | chrome | panel | Same tree as Increment 1 | Greyed out on non-Identities tab; selection still works |
| tab bar | nav-tabs | body | Identities · Abilities (active) · Movements | Tab navigation between character editing modes |
| ability list | list | body | name · key · persist · attack | Selectable rows; Create/Edit/Delete/Set Key/Toggle Persist/Play/Stop actions |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| ability list rows | list | Each row shows: name, activation key, persistent indicator (●), attack flag (●) |
| Create | button | Creates a new ability on the selected character |
| Edit | button | Opens ability editor for selected ability |
| Delete | button | Removes selected ability; prompts confirmation |
| Set Key | button | Assigns activation keyboard key to selected ability |
| Toggle Persist | button | Toggles persistent flag on selected ability |
| Play | button (primary) | Plays the selected ability animation on the spawned character |
| Stop | button | Stops the currently playing ability animation |

---

## Screen 2: ability editor

**Layout:** `form` — ability config form at top, element tree below
**Context:** opened from ability list Edit action
**Grid position:** col=2, row=1

### ASCII sketch

```
[ability editor]
┌──────────────────────────────────────────────────┐
│ Name [Ability 1    ]  Key [  ]  ☐ Attack         │
│ ☐ Persistent                                     │
│ Sequence  ○ And  ◉ Or                            │
│                                                  │
│ [Cut][Clone][Paste][▲][▼][−][▶]                  │
│ [+MOV][+FX][+SND][+SEQ][+PAUSE][+REF]           │
│                                                  │
│ element tree                                     │
│ ─ Mov Element 1                                  │
│ ─ FX Element 1                                   │
│ ─ Sound Element                                  │
│ ▼ Sequence:AND                                   │
│   ─ Mov Element 2                                │
│   ─ Pause 5                         ← selected   │
│   ─ Sound Element                                │
│   ▼ Sequence:OR                                  │
│     ─ Mov Element 2                              │
│     ─ FX Element 1                               │
│ ─ Ref Ability 1                                  │
│                                                  │
│ [Save●]  [Cancel]                                │
└──────────────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| ability config | form | body | Name, Key, Attack checkbox, Persistent checkbox, Sequence radio (And/Or) | Form fields configure ability metadata |
| element toolbar | toolbar | body | Cut · Clone · Paste · ▲ · ▼ · − · ▶ · +MOV · +FX · +SND · +SEQ · +PAUSE · +REF | Add element types to the tree; clipboard ops; reorder; play/remove |
| element tree | tree | body | Hierarchical tree with indented nodes; sequences expand to show children | **TREE** — not a flat list; sequences are expandable parent nodes; elements nest under sequences |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| element tree nodes | tree | Each node shows type prefix and name; selectable; sequences expand/collapse with ▼/▶; nested elements indented under parent sequence |
| +MOV | button | Adds a Movement animation element to the tree at the current position |
| +FX | button | Adds an FX (visual effect) element to the tree |
| +SND | button | Adds a Sound element to the tree |
| +SEQ | button | Adds a Sequence container (AND or OR) that can hold child elements |
| +PAUSE | button | Adds a Pause element with a duration value |
| +REF | button | Adds a Reference to another ability; opens reference ability browser |
| Cut / Clone / Paste | buttons | Clipboard operations on the selected element; supports cross-ability pasting |
| ▲ / ▼ | buttons | Reorder the selected element up/down within its parent |
| − | button | Remove the selected element from the tree |
| ▶ | button (primary) | Play/preview the ability animation |

### Element types in tree

| Type | Icon/prefix | Behavior |
| --- | --- | --- |
| Mov Element | ─ | Movement animation; browseable via animation resource browser |
| FX Element | ─ | Visual effect; browseable via animation resource browser |
| Sound Element | ─ | Sound clip; browseable via animation resource browser |
| Sequence:AND | ▼/▶ | All children play in order; expandable container |
| Sequence:OR | ▼/▶ | One child plays randomly; expandable container |
| Pause | ─ | Wait N seconds before next element |
| Ref Ability | ─ | Reference to another ability; link or copy mode |

---

## Screen 3: animation resource browser

**Layout:** `modal` — floating panel with filter and resource listbox
**Context:** opened when assigning MOV/FX/Sound resources to elements
**Grid position:** col=3, row=1

### ASCII sketch

```
[animation resource browser]
┌────────────────────────────────────────┐
│ Animation Resource [Animation Res ▾]   │
│                       ☐ Play W/ Next   │
│                                        │
│  Tag         │ Animation               │
│ ─────────────┼──────────────────────── │
│  Minion      │ Burst_Attack_1          │
│  Minion      │ Burst_Attack_2          │
│  movie       │ run_this_mov3  ← sel    │
│  movie       │ run_this_mov4           │
│  movie       │ run_this_mov5           │
│  movie       │ run_this_mov6           │
│              │                         │
│              │                         │
│                                        │
│ [Select●]  [Demo]                      │
└────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| resource filter | form | body | Animation Resource dropdown, Play W/ Next checkbox | Dropdown filters by resource type; Play W/ Next chains with next element |
| resource list | listbox | body | Tag · Animation columns | **LISTBOX** — selectable rows; selecting a row demos the animation in the game world |

---

## Screen 4: reference ability browser

**Layout:** `modal` — floating panel with filter and ability listbox
**Context:** opened when adding a Reference element to the ability tree
**Grid position:** col=3, row=2

### ASCII sketch

```
[reference ability browser]
┌────────────────────────────────────────┐
│ [Character      ▾]  ○ Link  ◉ Copy    │
│                                        │
│  Character    │ Ability                 │
│ ──────────────┼──────────────────────  │
│  Character    │ Ability 1              │
│  Spyder       │ Ability 1              │
│  Spyder       │ Ability 2              │
│  Ogun         │ Ability 3   ← sel      │
│  Ogun         │ Ability 4              │
│               │                        │
│                                        │
│ [Select●]  [Demo]                      │
└────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| reference filter | form | body | Character dropdown, Link/Copy radio | Filter by character; Link creates a live reference, Copy duplicates the ability tree |
| ability list | listbox | body | Character · Ability columns | **LISTBOX** — selectable rows; selecting demos the referenced ability |

---

## Screen 5: ability groups

**Layout:** `form` — group management with tree of groups and their abilities
**Context:** opened from crowd manager to organize abilities into named groups (Common, Defenses, Weapons, etc.)
**Grid position:** col=4, row=1

### ASCII sketch

```
[ability groups]
┌──────────────────────────────────────┐
│ option groups                        │
│ [+ Add Group]  [Delete Group]        │
│                                      │
│ ▼ Common                             │
│   ─ Ability 1                        │
│   ─ Ability 2                        │
│ ▼ Defenses                           │
│   ─ Shield Buff                      │
│ ▼ Weapons  ← selected (green)       │
│   ─ Burst Attack                     │
│   ─ Fire Breath                      │
│   ─ Dodge                            │
│                                      │
│ [+ Place Ability]  [− Remove]        │
└──────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| group tree | tree | body | Group headers (expandable) with ability children | **TREE** — groups are expandable parent nodes; abilities are leaf nodes within groups; selected group highlighted green |

---

## Screen 6: attack configuration

**Layout:** `modal` — attack parameter dialog
**Context:** opened when activating an attack ability on a target
**Grid position:** col=4, row=2

### ASCII sketch

```
[attack configuration]
┌──────────────────────────────┐
│ result                       │
│  Hit   ← selected           │
│  Miss                        │
│                              │
│ effects                      │
│  ☑ Stunned                   │
│  ☑ Unconscious               │
│  ☐ Dead                      │
│                              │
│ knockback                    │
│  ◉ Knocked Down              │
│  ○ Knockback                 │
│                              │
│ [OK●]  [Cancel]              │
└──────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| result | listbox | body | Hit / Miss | **LISTBOX** — selectable; determines attack outcome |
| effects | form | body | Stunned · Unconscious · Dead checkboxes | Multi-select effect checkboxes |
| knockback | form | body | Knocked Down / Knockback radio; OK / Cancel | Radio for knockback type; confirm or cancel the attack |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| crowd manager — abilities | ability editor | edit ability |
| ability editor | crowd manager — abilities | saves / cancels |
| ability editor | animation resource browser | browse resources |
| ability editor | reference ability browser | add reference |
| crowd manager — abilities | ability groups | manage groups |
| crowd manager — abilities | attack configuration | activate attack |

---

## Design reference notes

Design images reviewed from `Design/4) Ability/` and `Design/5) Ability Groups/`:

| Image | Key observations |
| --- | --- |
| Ability - Edit, Add, Remove Abilities.png | Shows full layout: Active Roster (left), Character Editor (center), Ability Edit panel (right) with element tree and animation resource browser; element tree uses typed icons for MOV/FX/SND/SEQ/REF |
| Ability - Animate Nested Ability.png | Shows nested sequences: Sequence:AND containing children, Sequence:OR nested within; tree structure with hierarchical indentation |
| Ability - Animate Abilty with Pause Element.png | Shows Pause element in tree under Sequence:AND with duration value "5" |
| Ability - Animate Abilty with Play With Next.png | Shows "Play W/ Next" checkbox checked; elements chain together for simultaneous playback |
| Ability - Assign Sequence to Ability.png | Shows adding Sequence:OR to ability; element tree shows sequence types (AND/OR) |
| Ability - Assign Reference Ability.png | Shows reference ability browser with Character/Ability columns, Link/Copy radio |
| Ability - Filter and Browse resources.png | Shows Animation Resource dropdown filter with Tag/Animation listbox; green highlighting on active filter |
| Ability - Nest Ability - Cut, Clone, Paste.png | Shows clipboard operations on element tree; Sequence:OR and Sequence:AND with nested children |
| Play Attack - Attack Other Character.png | Shows attack dialog with Hit/Miss listbox, Stunned/Unconscious/Dead checkboxes, Knocked Down/Knockback radio |
| Options - Add, Remove Options Group.png | Shows ability groups: Common, Defenses, Weapons (highlighted green); +/delete group buttons |
| Options - Remove from, Place Ability Into Group.png | Shows placing abilities into groups; ability slots within group sections |

**Key design decisions captured:**
- Element tree is a **hierarchical tree** with expand/collapse, not a flat grid — sequences nest children with indentation
- Animation resource browser uses a **filterable listbox** with Tag and Animation columns
- Reference ability browser shows **all abilities across characters** with Character/Ability columns and Link/Copy mode
- Ability groups are organized as a **tree** with group headers and ability children
- Attack configuration uses **listbox** for Hit/Miss selection and checkbox/radio for effects
