# Lo-fi Wireframes — Increment 5: Roster

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 5: Roster |
| Increment outcome | GM can manage the active roster, spawn/place characters into the game world, target and navigate characters, control the camera, and interact via context menus and the active character HUD widget |
| State JSON | `docs/ux/lo-fi/increment-5-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-5.drawio` |
| Design references | `Design/2) Roster/*.png` (11 images) |
| Date | 2026-05-17 |

---

## Screen 1: character explorer

**Layout:** `panel` — toolbar, search filter, hierarchical crowd/character tree
**Context:** pre-session and in-session — browse all characters and crowds; drag to active roster
**Grid position:** col=1, row=0

### ASCII sketch

```
[character explorer]
┌──────────────────────────┐
│ [Save][Cut][Copy][Add]   │
│ [×] [Edit]               │
│ [Spyder              ]   │
│                          │
│ ─ All Characters         │
│   ─ Character 1          │
│   ─ Character 2  ← green │
│ ▼ Crowd 1                │
│   ─ Character 1          │
│   ─ Character 2          │
│ ▼ Crowd 2                │
│   ─ Character 1 (1)      │
│   ─ Character 2 (1)      │
│                          │
│                          │
└──────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| explorer toolbar | toolbar | body | Save · Cut · Copy · Add · × · Edit | Standard CRUD and clipboard operations on characters/crowds |
| filter | form | body | Search text input | Filters the character tree by name |
| character tree | tree | body | Hierarchical crowd/character tree with expand/collapse | **TREE** — crowds are expandable parent nodes (▼/▶); characters are leaf nodes (─); selected character highlighted green; drag characters to active roster to add |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| character tree nodes | tree | Root "All Characters" node; crowd nodes expand/collapse with ▼/▶; character leaf nodes selectable; green highlight on selected/active character; drag-and-drop to active roster |
| Save | button | Saves current character/crowd state |
| Cut / Copy | buttons | Clipboard operations on selected character/crowd |
| Add | button | Creates new character or crowd |
| × | button | Removes selected character/crowd |
| Edit | button | Opens character editor for selected character |

---

## Screen 2: active roster

**Layout:** `panel` — toolbar, crowd-grouped character list with collapsible sections
**Context:** in-session — characters actively in play; spawn/place/activate/manage
**Grid position:** col=2, row=0

### ASCII sketch

```
[active roster]
┌────────────────────────────────┐
│ [💾][📍][✏][⬜][➡][⬅][🎥][×]  │
│                                │
│ Crowd 1                        │
│   ─ Character 1                │
│   ─ Character 2                │
│   ─ Character 3                │
│                                │
│ Crowd 2                        │
│   ─ Character 1                │
│   ─ Character 2                │
│                                │
│ No Crowd                       │
│   ─ Character 1                │
│   ─ Character 2 ✔  ← active   │
│   ─ Character 1                │
│   ─ Character 2                │
│                                │
│ [Spawn●][Place][Remove]        │
│ [Activate●]                    │
│                      + Edit    │
│                      Character │
└────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| roster toolbar | toolbar | body | Save · Location · Edit · Move · Forward · Back · Camera · Remove | Icon buttons for roster-level operations; camera/navigation controls |
| roster list | tree | body | Crowd-grouped character list with collapsible crowd headers | **TREE** — crowd sections are expandable group headers; characters are selectable leaf nodes within groups; active character shown with ✔ and green highlight; "No Crowd" section for ungrouped characters |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| roster list | tree | Crowd headers (Crowd 1, Crowd 2, No Crowd) are collapsible section headers; characters within are selectable; active character highlighted green with ✔; multi-select supported for batch operations |
| Spawn | button (primary) | Spawns the selected character(s) into the game world at a default position |
| Place | button | Places the selected character at a specific location (camera position or coordinates) |
| Remove | button | Removes the selected character from the active roster |
| Activate | button (primary) | Makes the selected character the active/controlled character |
| + Edit Character | side label | Opens the character editor (crowd manager) for the selected character |

### Conditional states

| State | What changes |
| --- | --- |
| Empty roster | No crowd sections; Spawn/Place/Activate disabled |
| Crowd selected | All characters in crowd become selected; batch Spawn/Place available |
| Character active | Active character shows ✔ and green highlight; Activate disabled for that character |
| Character spawned | Spawned characters show spawn indicator; Place becomes available |

---

## Screen 3: game world

**Layout:** `viewport` — 3D game scene with ground plane, characters, camera
**Context:** in-session — visual representation of spawned characters
**Grid position:** col=3, row=0

### ASCII sketch

```
[game world]
┌──────────────────────────────────────┐
│                              ☁      │
│                                      │
│                   Character 2        │
│                      ◎ (targeted)    │
│                                      │
│  Character 1     Character 3         │
│    🧍              🧍                │
│ ─────────────────────────────────── │
│                                      │
│   🎥                                 │
│   camera ↔ character navigation      │
│                                      │
└──────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| 3d scene | viewport | body | Character figures with name labels, target reticle (red ◎), camera icon, ground plane | Click to select; double-click to activate; right-click for context menu; camera toggle for character navigation |

---

## Screen 4: context menu

**Layout:** `popup` — right-click context menu on game world character
**Context:** in-session — right-clicking a character in the game world
**Grid position:** col=4, row=0

### ASCII sketch

```
[context menu]
┌──────────────────┐
│ > Cam            │
│ < Cam            │
│ Target  ← sel    │
│ Maneuver         │
│ ─────────────── │
│ Spawn            │
│ Place            │
│ Save Location    │
│ ─────────────── │
│ Activate         │
│ Edit             │
│ Remove           │
└──────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| menu items | listbox | body | Camera controls (> Cam, < Cam), Target, Maneuver, Spawn, Place, Save Location, Activate, Edit, Remove | **LISTBOX** — selectable menu items; separated into camera/targeting, spawn/placement, and management groups |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| > Cam | menu item | Move camera to the targeted character's position |
| < Cam | menu item | Move character to the camera's current position |
| Target | menu item | Set this character as the current target (shows reticle) |
| Maneuver | menu item | Enable camera-based character movement mode |
| Spawn | menu item | Spawn the character at a default game position |
| Place | menu item | Place the character at the camera's current position |
| Save Location | menu item | Save the character's current position for later recall |
| Activate | menu item | Make this character the active/controlled character |
| Edit | menu item | Open the character editor for this character |
| Remove | menu item | Remove the character from the roster |

---

## Screen 5: active character widget

**Layout:** `hud` — bottom overlay showing active character details
**Context:** in-session — always visible when a character is active
**Grid position:** col=3, row=1

### ASCII sketch

```
[active character widget]
┌──────────────────────────────────────────────────────┐
│ Spyder                        + Common               │
│ 📍 ✏ ⬜ ➡ ⬅ 🎥 ×             + Defenses              │
│                                                      │
│ Weapons ──────────────          + Abilities           │
│  ○ ○ ○ ○ ○                      ○ ○ ○ ○ ○ ○         │
│                                                      │
│ Identity ─────  Movements ───                        │
│  ○ ○ ○           ○ ○ ○ ○                             │
└──────────────────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| character HUD | composite | body | Character name, toolbar icons, group headers (Common, Defenses), slot rows (Weapons, Identity, Movements, Abilities) | Shows active character's option groups with circular ability/identity/movement slots; click slot to activate ability; group headers expand/collapse |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| character explorer | active roster | drag to add |
| active roster | game world | spawn / place |
| game world | context menu | right-click character |
| game world | active character widget | active character HUD |

---

## Design reference notes

Design images reviewed from `Design/2) Roster/`:

| Image | Key observations |
| --- | --- |
| Roster.png | Full layout: Character Explorer (left tree), Active Roster (middle, crowd-grouped), Game World (right, 3D scene with character figures and target reticle), Active Character Widget (bottom HUD) |
| Roster - Activate Character On Target.png | Shows context menu on right-click with: > Cam, < Cam, Target (highlighted), Maneuver, Remove, Spawn, Place, Save Location, Edit |
| Roster - Add Character with No Crowd.png | Shows drag from Character Explorer to Active Roster creating a "No Crowd" section |
| Roster - Spawn and Place.png | Shows Spawn/Place toolbar buttons highlighted; Character 2 spawned with target reticle in game world |
| Roster - Target, Untarget, Follow.png | Shows targeting toolbar buttons highlighted (Forward, Back); character targeted with reticle |
| Roster - Toggle Camera And Character Navigation.png | Shows Camera toggle button highlighted; camera icon with arrow pointing to character |
| Roster Character - activate command from keyboard.png | Shows active character with HUD widget at bottom; keyboard activation |
| Roster crowd - Add Character with Crowd.png | Dragging a character that belongs to a crowd creates a crowd-specific section with only the dragged character |
| Roster Crowd - Add Crowd to roster.png | Dragging a crowd adds all its characters to a crowd section (highlighted green) |
| Roster Crowd - Select Crowd.png | Selecting a crowd header selects all characters within (all highlighted green) |
| Roster Crowd - Spawn OR Place Crowd.png | Spawning a crowd places all characters with crowd/character labels in game world |

**Key design decisions captured:**
- Character Explorer is a **hierarchical tree** with expand/collapse crowd nodes and character leaf nodes
- Active Roster uses **crowd-grouped sections** (tree-like) — crowd headers are collapsible; characters are selectable leaf nodes; "No Crowd" section for standalone characters
- Game world shows **character figures** with name labels, target reticle (red circle), and crowd labels
- Context menu is a **listbox** with grouped menu items separated by dividers
- Active character widget shows **circular ability/identity/movement slots** organized by option groups (Common, Defenses, Weapons)
- **Drag-and-drop** from Character Explorer to Active Roster — dragging a crowd adds all members; dragging a crowd member creates a crowd section with just that character
