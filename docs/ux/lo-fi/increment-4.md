# Lo-fi Wireframes — Increment 4: Single Character Movement

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 4: Single Character Movement |
| Increment outcome | GM can move individual characters through the 3D world using configured *movement types* — walk, run, teleport, turn, follow with camera |
| UL file | `docs/domain/ubiquitous-language-increment-4.md` |
| AC file | `docs/stories/acceptance-criteria-increment-4.md` |
| State JSON | `docs/ux/lo-fi/increment-4-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-4.drawio` |
| IA reference | `docs/ux/initial-ia.md` |
| Design references | `Design/1) Character and Crowds/Edit Character - Change Movement.png` |
| Date | 2026-05-17 |
| Generator | `drawio-mockup.mjs save` |

---

## Screen overview

| Screen | Layout | Col | Row | New in Increment 4 |
| --- | --- | --- | --- | --- |
| crowd manager — identities | sidebar | 1 | 0 | No (stub reference for tab navigation) |
| crowd manager — abilities | sidebar | 1 | 1 | No (stub reference for tab navigation) |
| crowd manager — movements | sidebar | 1 | 2 | **Yes** |
| movement editor | form | 2 | 2 | **Yes** |

---

## Screen 1 (reference): crowd manager — identities

**Layout:** `sidebar` — crowd tree (panel slot, left 33%) · tab content (body slot, right 67%)  
**Context:** pre-session — shown as a reference target for tab navigation from the movements tab

Included as a connection target only. Full wireframe defined in increment-2.drawio.

---

## Screen 2 (reference): crowd manager — abilities

**Layout:** `sidebar` — same shell as crowd manager — identities; abilities tab active  
**Context:** pre-session — shown as a reference target for tab navigation from the movements tab

Included as a connection target only. Full wireframe defined in increment-3.drawio.

---

## Screen 3: crowd manager — movements

**Layout:** `sidebar` — crowd tree dimmed (panel slot, left 33%) · movements tab active (body slot, right 67%)  
**Context:** pre-session — opened when the GM selects the Movements tab in the crowd manager

### ASCII sketch

```
[crowd manager — movements]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬─────────────────────────────────┐
╎ crowd tree           ╎ Identities  Abilities  [Movements]│
╎  crowd name (n)      ├─────────────────────────────────┤
╎    > char name       │ movement list                   │
╎    > type · spawned  │  name · key · default · type    │
╎  (dimmed)            │  ···                            │
╎                      │  (row 2)                        │
╎                      │  (row 3)                        │
╎                      │  (row 4)                        │
╎                      ├─────────────────────────────────┤
╎                      │ [Add] [Remove] [Set Default]    │
╎                      │ [Set Key]  [Edit ▶]             │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴─────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| crowd tree | list (dimmed) | panel | crowd / character · count columns; 5 data rows | Dimmed to indicate the panel is visible but non-interactive while the Movements tab is active; character selection still drives movement list content |
| tab bar | nav-tabs | body | Identities (inactive) · Abilities (inactive) · Movements (active) | Active tab shown with blue highlight; inactive tabs shown grey; clicking Identities or Abilities transitions to the sibling tab state |
| movement list | list | body | name · key · default · type columns; 4 data rows; Add · Remove · Set Default · Set Key · Edit actions | No-selection state: Add enabled, Remove/Set Default/Set Key/Edit disabled. Row selected: all actions enabled. Default marker shown in default column for the *default movement* row. Edit opens the movement editor. |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| movement list row | selectable list row | Selecting a row enables Remove, Set Default, Set Key, Edit; deselecting disables them |
| name column | read-only text | Displays the *character movement* name as set in the movement editor |
| key column | read-only text | Displays the *movement activation key* assigned to the movement; blank if none assigned |
| default column | read-only indicator | Shows a default marker (★) for the *default movement*; blank for all others |
| type column | read-only text | Displays the *movement type* value (Walk / Run / Swim / Fly / Jump) |
| Add | button | Creates a new *character movement* entry in the movement list with a default Walk type; opens the movement editor inline or as a separate screen |
| Remove | button (disabled without selection) | Removes the selected *character movement* after confirmation |
| Set Default | button (disabled without selection) | Marks the selected movement as the *default movement*; clears the previous default marker |
| Set Key | button (disabled without selection) | Opens a key-capture prompt to assign a *movement activation key* to the selected movement |
| Edit | button primary (disabled without selection) | Opens the movement editor for the selected *character movement* |

### Conditional states

| State | Visual change |
| --- | --- |
| No character selected | movement list body is empty; Add disabled |
| Character selected, no movement selected | movement list shows rows; Remove / Set Default / Set Key / Edit disabled |
| Movement row selected | Remove / Set Default / Set Key / Edit enabled; selected row highlighted |
| Default movement assigned | ★ marker in default column on the designated row; other rows show blank |
| Edit action clicked | transitions to movement editor |

### Stories mapped

- Add Movement to Character — movement list · Add action
- Remove Movement from Character — movement list · Remove action
- Set Default Movement — movement list · Set Default action
- Set Movement Activation Key — movement list · Set Key action
- Edit Movement Parameters — movement list · Edit action → movement editor
- Add Default Movements to Character (Walk, Run, Swim) — system batch action, reflected in movement list

### Transitions out

- → crowd manager — identities: *selects Identities tab* (dashed)
- → crowd manager — abilities: *selects Abilities tab* (dashed)
- → movement editor: *clicks Edit on selected movement row*

---

## Screen 4: movement editor

**Layout:** `form` — single column; movement config form  
**Context:** pre-session — opened from crowd manager — movements via Edit action

### ASCII sketch

```
[movement editor]
┌──────────────────────────────────────────┐
│ movement config                          │
│  Name              [_________________]  │
│  Movement Type     [Walk            ▾]  │
│  Activation Key    [_________________]  │
│  Distance Limit    [_________________]  │
│  ☐  Default                            │
├──────────────────────────────────────────┤
│                    [Save ▶]  [Cancel]   │
└──────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| movement config | form | body | Name (text) · Movement Type (dropdown) · Activation Key (text) · Distance Limit (text) · Default (checkbox) · Save · Cancel | Save validates Name (non-empty, unique within character) and Movement Type (must be selected); validation errors shown inline; Cancel discards all changes and returns to movement list |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| Name | text input | Required; must be non-empty and unique within the character's Movements option group; validation error shown inline on save if empty or duplicate |
| Movement Type | dropdown | Options: Walk / Run / Swim / Fly / Jump; defaults to Walk on new creation; required — save blocked if unset |
| Activation Key | text input | Optional; single key value (e.g. F1, Numpad1); accepts capture input; blank if no key assigned; conflict validation on save |
| Distance Limit | text input | Optional numeric; blank or zero means no limit; displayed as a numeric field |
| Default | checkbox | Unchecked by default; when checked, marks this movement as the *default movement*; checking clears the default designation from any other movement on the character |
| Save | primary button | Validates all fields; on success, saves the *character movement* and returns to the movement list; on validation failure, shows inline errors and keeps the form open |
| Cancel | secondary button | Discards all unsaved changes and returns to the movement list without modifying the *character movement* |

### Stories mapped

- Edit Movement Parameters — full form; opened from movement list Edit action or from Add

### Transitions out

- → crowd manager — movements: *saves (success)* or *cancels*

---

## Connections

| From | To | Label | Style |
| --- | --- | --- | --- |
| crowd manager — movements | crowd manager — identities | selects Identities tab | dashed |
| crowd manager — movements | crowd manager — abilities | selects Abilities tab | dashed |
| crowd manager — movements | movement editor | edit movement | solid |
| movement editor | crowd manager — movements | saves / cancels | solid |

---

## Domain terms visible in wireframes

| Domain term | Appears as | Screen | Region / control |
| --- | --- | --- | --- |
| character movement | movement list row | crowd manager — movements | movement list |
| movement type | type column value (Walk/Run/Swim/Fly/Jump) | crowd manager — movements · movement editor | movement list · Movement Type dropdown |
| movement activation key | key column · Activation Key field | crowd manager — movements · movement editor | movement list · movement config form |
| default movement | default column (★ marker) · Default checkbox | crowd manager — movements · movement editor | movement list · movement config form |
| distance limit | Distance Limit field | movement editor | movement config form |
| option group (Movements) | movement list body | crowd manager — movements | movement list |

---

## Drawio file

| Property | Value |
| --- | --- |
| File | `docs/ux/lo-fi/increment-4.drawio` |
| File size | 41,573 bytes |
| Screens | 4 |
| Connections | 4 |
| Generator command | `node drawio-mockup.mjs save --state increment-4-state.json --out increment-4.drawio` |
