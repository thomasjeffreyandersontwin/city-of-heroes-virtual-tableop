# Lo-fi Wireframes — Increment 2: Character Identities

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 2: Character Identities |
| Increment outcome | GM can assign *identities* (Model or Costume) to characters and see them rendered in the game world |
| UL file | `docs/domain/ubiquitous-language-increment-2.md` |
| AC file | `docs/stories/acceptance-criteria-increment-2.md` |
| State JSON | `docs/ux/lo-fi/increment-2-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-2.drawio` |
| Drawio file size | 28,254 bytes |
| IA reference | `docs/ux/initial-ia.md` |
| Design references | `Design/3) Identities/*.png` (6 images) |
| Date | 2026-05-17 |
| Generator | `drawio-mockup.mjs save` |

---

## Screen 1: crowd manager — identities

**Layout:** `sidebar` — crowd tree (panel slot, left 33%) · identity tab body (body slot, right 67%)  
**Context:** pre-session — primary workspace, Identities tab active  
**Grid position:** col=1, row=0 (continues from Increment 1 *crowd manager — identities*)

### ASCII sketch

```
[crowd manager — identities]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬────────────────────────────────────┐
╎ [New Crowd][New Char] ╎ [Identities]  Abilities  Movements │
╎ [Rename]  [Delete]    ├────────────────────────────────────┤
╎ [Cut][Clone][Link]..  │ identity list                      │
╎ ────────────────────  │  name  │ type  │ active● │ default★│
╎ Filter by Name [    ] │ ───────┼───────┼─────────┼─────────│
╎              [× Clear]│  ···   │       │         │         │
╎                       │        │       │         │         │
╎ crowd tree (dimmed)   │        │       │         │         │
╎  crowd / char  type   ├────────────────────────────────────┤
╎  ···                  │ [Add] [Remove] [Set Default]       │
╎                       │ [Set Active●]  [Add Ghost]         │
╎                       │                                    │
╎ [By Concept][By Gangs]│                                    │
╎ [By COH Str][All Char]│                                    │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| crowd tree toolbar — structural | toolbar | panel | New Crowd · New Char · Rename · Delete | Same as Increment 1; panel shown dimmed on non-Identities tab states |
| crowd tree toolbar — clipboard | toolbar | panel | Cut · Clone · Link · Clone-Link | Same as Increment 1 |
| crowd tree toolbar — batch ops | toolbar | panel | Flatten-Copy · Clone Mbrs | Same as Increment 1 |
| filter bar | form | panel | Filter by Name (text) · × Clear | Same as Increment 1 |
| crowd tree (dimmed) | list | panel | crowd / character · type · spawned | Shows type (Model/Costume) and spawned state on character nodes; dimmed on sibling tabs |
| browse modes | button-bar | panel | By Concept · By Gangs · By COH Structure · All Characters | Same as Increment 1 |
| tab bar | nav-tabs | body | Identities (active) · Abilities (inactive) · Movements (inactive) | Identities tab is active; sibling tabs navigate to crowd manager — abilities / movements |
| identity list | list | body | name · type · active ● · default ★ | Add / Remove / Set Default / Set Active / Add Ghost actions below list |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| identity list rows | list | Each row shows: name, type (Model / Costume), active indicator (●, shown when identity is the *active identity*), default marker (★, shown when identity is the *default identity*), ghost indicator (👻, shown when a *ghost shadow* is active on a model identity row) |
| Add | button | Opens inline name input; creates a new *Identity* entry on the selected *Character*; disabled when no *Character* is selected in the crowd tree |
| Remove | button | Removes selected *Identity*; if active, despawns the *Spawned NPC* first; prompts confirmation |
| Set Default | button | Sets the default flag (★) on selected *Identity*; clears previous default; disabled when no identity is selected |
| Set Active | button (primary) | Triggers the identity activation pipeline (spawn → load costume if costume identity → play animation); disabled when game bridge is not ready or no identity is selected |
| Add Ghost | button | Activates *Ghost Shadow* for selected *Model Identity*; disabled for *Costume Identity* rows and when character is not spawned |

### Conditional states

| State | What changes in the identity list |
| --- | --- |
| Empty list | List body shows no rows; Add button enabled; all other identity actions disabled |
| Active identity | The active row displays ● in the "active" column; Set Active disabled for that row |
| Default marker | The default row displays ★ in the "default" column |
| Ghost indicator | Model identity row displays ghost indicator alongside the active ● marker |
| Game not connected | Set Active and Add Ghost are disabled with a "game not connected" tooltip |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Add Identity to Character | identity list | Add button |
| Set Identity Type (Model or Costume) | identity list | row type field |
| Assign Costume Surface to Identity | identity list | assign-surface action |
| Set Default Identity | identity list | Set Default button |
| Set Active Identity | identity list | Set Active button |
| Remove Identity from Character | identity list | Remove button |
| Superimpose Ghost on Model Character | identity list | Add Ghost button |
| Load Costume File for Active Identity | identity list | triggered by Set Active |
| Spawn Character with Model Identity | identity list | triggered by Set Active (model type) |
| Switch Active Identity on Spawned Character | identity list | Set Active on a second identity |
| Play Animation on Identity Load | identity list | triggered after spawn completes |
| Stop Persistent Abilities on Identity Switch | identity list | triggered before despawn on switch |

### Domain terms traced

| Term | Appears as | Region |
| --- | --- | --- |
| identity | region name (identity list) | identity list |
| model identity | type field value "Model" | identity list |
| costume identity | type field value "Costume" | identity list |
| active identity | active ● column | identity list |
| default identity | default ★ column | identity list |
| ghost shadow | ghost indicator · Add Ghost button | identity list |
| crowd | region name (crowd tree) | crowd tree |
| crowd member | row in crowd tree | crowd tree |
| spawned NPC | type · spawned column on crowd tree character node | crowd tree |

---

## Screen 2: model browser

**Layout:** `modal` — centered floating panel, single column  
**Context:** pre-session — opened from crowd manager — identities to build a crowd from COH model list  
**Grid position:** col=2, row=0

### ASCII sketch

```
[model browser]
┌──────────────────────────────────────────┐
│ ╔════════════════════════════════════╗   │
│ ║  filter                            ║   │
│ ║   Search models  [________________]║   │
│ ╠════════════════════════════════════╣   │
│ ║  model list                        ║   │
│ ║   model name           │ type      ║   │
│ ║  ──────────────────────┼──────────║    │
│ ║   ···                  │           ║   │
│ ║                        │           ║   │
│ ║                        │           ║   │
│ ║                        │           ║   │
│ ║                        │           ║   │
│ ║                        │           ║   │
│ ╠════════════════════════════════════╣   │
│ ║  [Select]  [Deselect]              ║   │
│ ║  [Create Crowd from Selection●]    ║   │
│ ╚════════════════════════════════════╝   │
└──────────────────────────────────────────┘
```

### Regions

| Region | Type | Slot | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| filter | form | body | Search models (text input) | Filters displayed model list to entries matching the search term; case-insensitive; clearing restores full list |
| model list | list | body | model name · type | Rows are selectable; selected rows get a visual selection indicator; Select / Deselect / Create Crowd from Selection actions below list |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| Search models | text | Filters the *model list* live as the GM types; clearing restores all models |
| model list rows | list | Each row shows: model name (COH NPC archetype string) and type (villain group / hero / civilian); selectable via Select action |
| Select | button | Marks the highlighted row as selected; selection indicator shown on row |
| Deselect | button | Clears selection on highlighted row |
| Create Crowd from Selection | button (primary) | Disabled when no models are selected; enabled when ≥ 1 model is selected; creates a new *Crowd* with one *Character* per selected *Model*, each pre-configured with a *Model Identity* |

### Conditional states

| State | What changes |
| --- | --- |
| No selection | Create Crowd from Selection is disabled; Select and Deselect actions available |
| Selection active | ≥ 1 row has selection indicator; Create Crowd from Selection becomes enabled |
| Filter active | Model list shows only matching entries; previously selected models that are filtered out remain in selection |
| Empty model list | List body shows "no models available" message; all actions disabled |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Create Crowd from COH Model List | model list | Create Crowd from Selection button |
| Select Models to Include in Crowd | model list | Select · Deselect buttons |
| Load Available Models from Models.txt | model list | populated on Game Loaded Event before modal opens |
| Generate Characters with Model Identities | model list | triggered by Create Crowd from Selection |
| Load Models List for Crowd Creation | model list | model list loaded on game bridge initialization |

### Domain terms traced

| Term | Appears as | Region |
| --- | --- | --- |
| model | row in model list | model list |
| model list | region name (model list) | model list |
| crowd | created by Create Crowd from Selection | model list actions |
| character | generated one per selected model | model list actions |
| model identity | pre-configured on each generated character | model list actions |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| crowd manager — identities | model browser | opens model browser |
| model browser | crowd manager — identities | creates crowd / cancels |

### Transition notes

- The **opens model browser** connection is triggered from the crowd manager — identities body panel, typically via an "Add from Model List" button or the browse-modes area (not modeled as an explicit button in this wireframe; documented as a transition in the IA)
- The **creates crowd / cancels** connection covers both the confirmed Create Crowd from Selection flow and a Cancel/close action on the modal

---

## Design reference notes

Design images reviewed from `Design/3) Identities/`:

| Image | Key observations |
| --- | --- |
| Identities - Display Identities, movements, powers, crowds.png | Shows the Character Editor with circular identity slots, movements, powers, and crowds sections; confirms identity list is a visual panel within the character editor body |
| Identity - edit, add, remove Identity.png | Shows "Costume Edit" side panel with: Identity Name field, Model\Costume dropdown, Default checkbox, Costume/Model radio buttons, tag-model-name list, Animation section; model identity rows show Tag/Model columns |
| Identity - Filter and Browse Models and Demo as you Chang Records.png | Shows model filtering in the Costume Edit panel with search field and filtered model list; Minion2 is highlighted/selected |
| Identity - load identity.png | Shows the same panel with model list fully populated (Minion 1, Minion2, Brunhilda Crazy); confirms list-per-character approach for identity management |

**Key design decisions captured:**
- Identity list uses name · type · active · default columns (aligned with the original circular slot panel, translated to a structured list)
- "active ●" and "default ★" are textual indicators replacing the original filled/unfilled circle visual language
- Add Ghost action surfaces the ghost shadow workflow directly in the identity list (not a separate panel in this increment's wireframe)
- Model browser condenses the original "Costume Edit" panel's model filtering into a dedicated modal, consistent with the IA spec

---

## CLI command used

```powershell
node "C:\dev\agilebydesign-skills\skills\user-experience-design\abd-lo-mockup\scripts\drawio-mockup.mjs" save --state "c:\hero-desktop\city-of-heroes-virtual-tabletop\docs\ux\lo-fi\increment-2-state.json" --out "c:\hero-desktop\city-of-heroes-virtual-tabletop\docs\ux\lo-fi\increment-2.drawio"
```

Output: `2 screens, 2 connections` — `increment-2.drawio` written (28,254 bytes)
