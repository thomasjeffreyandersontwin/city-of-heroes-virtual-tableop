# Lo-fi Wireframes — Increment 4: Movement

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 4: Movement |
| Increment outcome | GM can assign movement types (Fly, Run, Swim, etc.) to characters with directional ability mappings and preview animations |
| State JSON | `docs/ux/lo-fi/increment-4-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-4.drawio` |
| Design references | `Design/1) Character and Crowds/Edit Character - Change Movement.png` |
| Date | 2026-05-17 |

---

## Screen 1: crowd manager — movements

**Layout:** `sidebar` — crowd tree (panel slot, left 33% dimmed) · movement tab body (body slot, right 67%)
**Context:** pre-session — primary workspace, Movements tab active
**Grid position:** col=1, row=2

### ASCII sketch

```
[crowd manager — movements]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬────────────────────────────────────┐
╎ [New Crowd][New Char] ╎  Identities  Abilities  [Movements]│
╎ [Rename]              ├────────────────────────────────────┤
╎ (dimmed controls)     │ movement list                      │
╎                       │  name  │ key │ default │ type      │
╎ crowd tree (dimmed)   │ ───────┼─────┼─────────┼──────── │
╎  ▼ Animals (12)       │ Fly    │  F  │    ★    │ Fly      │
╎    ─ Wolf_Lt_01       │ Run    │  R  │         │ Run      │
╎  ▶ Bears (3)          │ Swim   │  S  │         │ Swim     │
╎  ▶ Civilians (15)     │        │     │         │          │
╎                       ├────────────────────────────────────┤
╎                       │ [Add][Remove][Set Default]         │
╎                       │ [Set Key] [Edit●]                  │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| crowd tree (dimmed) | chrome | panel | Same tree as Increment 1 | Greyed out on non-Identities tab |
| tab bar | nav-tabs | body | Identities · Abilities · Movements (active) | Tab navigation |
| movement list | list | body | name · key · default · type | Selectable rows with movement entries; Add/Remove/Set Default/Set Key/Edit actions |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| movement list rows | list | Each row shows: name, activation key, default indicator (★), movement type |
| Add | button | Creates a new movement entry on the selected character |
| Remove | button | Removes the selected movement entry |
| Set Default | button | Sets the default flag (★) on the selected movement |
| Set Key | button | Assigns an activation keyboard key |
| Edit | button (primary) | Opens the movement editor for the selected movement |

---

## Screen 2: movement editor

**Layout:** `form` — movement configuration with direction ability assignments and animation listbox
**Context:** opened from movement list Edit action
**Grid position:** col=2, row=2

### ASCII sketch

```
[movement editor]
┌──────────────────────────────────────────────────┐
│ [Movement          ▾]   [▶]                      │
│ ☐ Default                                        │
│                                                  │
│ direction abilities                              │
│  → [Fly Right Ability                       ▾]   │
│  ← [Fly Left Ability                        ▾]   │
│  ↑ [Fly Forward Ability                     ▾]   │
│  ↓ [Fly Back Ability                        ▾]   │
│  ⬆ [Fly Up Ability                          ▾]   │
│  ⬇ [Fly Down Ability                        ▾]   │
│                                                  │
│ animations                                       │
│  Become The Character 2!       ← selected        │
│  Character 2 Attack                              │
│  Character 2 Dodge                               │
│                                                  │
│                                                  │
│                                                  │
│ [Save●]  [Cancel]                                │
└──────────────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| movement config | form | body | Movement Type dropdown, Default checkbox, Play button | Dropdown selects movement type; Default marks as character's default movement |
| direction abilities | form | body | 6 directional ability dropdowns (Right, Left, Forward, Back, Up, Down) | Each direction maps to an ability that plays when the character moves in that direction |
| animations | listbox | body | Selectable animation list | **LISTBOX** — selectable items; selecting previews the animation |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| Movement Type | dropdown | Selects movement category: Walk, Run, Swim, Fly, Jump |
| Default | checkbox | Marks this movement as the character's default |
| ▶ (Play) | button (primary) | Previews the movement animation on the spawned character |
| Direction ability dropdowns | dropdown × 6 | Each maps a direction (Right/Left/Forward/Back/Up/Down) to an ability that triggers during directional movement |
| Animation listbox | listbox | **LISTBOX** — selectable animation items; selecting an item previews it; items are character-specific animations |
| Save | button (primary) | Saves movement configuration |
| Cancel | button | Discards changes and closes editor |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| crowd manager — movements | movement editor | edit movement |
| movement editor | crowd manager — movements | saves / cancels |

---

## Design reference notes

Design image reviewed from `Design/1) Character and Crowds/`:

| Image | Key observations |
| --- | --- |
| Edit Character - Change Movement.png | Shows Movements Edit panel with: Movement type dropdown, Default checkbox, Play button, 6 directional ability dropdowns (Fly Right/Left/Forward/Back/Up/Down Ability), and animation listbox at bottom (Become The Character 2!, Character 2 Attack, Character 2 Dodge) |

**Key design decisions captured:**
- Movement editor uses **dropdown** fields for the movement type and each directional ability
- Direction abilities form a **6-row form** with icon + dropdown per direction
- Animation list is a **selectable listbox** (not a grid/table) showing available animations
- The play button (▶) is a prominent action for previewing the movement
