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
| IA reference | `docs/ux/initial-ia.md` |
| Design references | `Design/3) Identities/*.png` (6 images) |
| Date | 2026-05-17 |
| Generator | hand-crafted mxGraph XML |

---

## Screen 1: Full Application View

**Layout:** `multi-panel` — four panels side by side: Character Explorer, Active Roster, Character Editor, Costume Edit
**Context:** pre-session — full editing workspace with all panels expanded; Costume Edit visible when editing an identity
**Grid position:** col=0, row=0

### ASCII sketch

```
┌──────────┬──────────┬+┬─────────────────┬──────────────┐
│ Char     │ Active   │E│ Character Editor │ Costume Edit │
│ Explorer │ Roster   │d│                  │              │
│          │          │i│ [portrait]       │ Identity Name│
│ [toolbar]│ [toolbar]│t│  Spyder          │ Model\Costume│
│ Animation│          │ │                  │ □ Default  ▶ │
│          │ Crowd 1  │C│ Identity         │ ○ Costume    │
│ - All    │  Char 1  │h│ (●)(○)(○)(○)(+) │ ● Model      │
│   + Crwd │  Char 2  │a│                  │              │
│   + Crwd │  Char 3  │r│ Movements        │ Tag │ Model  │
│     Char │          │a│ [Loc][Dir][Flw]  │ Min │ Min 1  │
│     Char │ Crowd 2  │c│ [Cam][Scl][Rmv]  │ Min │ Min2   │
│     Char │  Char 1  │t│ (○)(○)(○)(○)     │ Min │ Brunhl │
│     Char │  Char 2  │e│                  │              │
│   + Crwd │          │r│ Powers           │ Animation  ▼ │
│   + Crwd │ No Crowd │ │ (○)(○)(○)(○)(○)  │ Become The.. │
│   + Crwd │  Char 1  │ │ (○)(○)(○)        │ Char 2 Attck │
│   + Crwd │ [Char 2] │ │                  │ Char 2 Dodge │
│   + Crwd │  Char 1  │ │ Crowds           │              │
│   + Crwd │  Char 2  │ │ (○)(○)(○)(○)     │              │
│   + Crwd │          │ │                  │              │
│          │          │ │ Navigate ●       │              │
└──────────┴──────────┴─┴─────────────────┴──────────────┘
```

### Panel 1: Character Explorer

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| toolbar | toolbar | 8 icon buttons (same as Increment 1) | Same toolbar as Increment 1 |
| animation dropdown | dropdown | Animation mode selector | Selects animation/filter mode for the tree display |
| crowd tree | tree | Hierarchical `+`/`-` expand/collapse tree | Same tree structure as Increment 1; shows crowds and characters with indentation |

### Panel 2: Active Roster

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| toolbar | toolbar | 6 icon buttons (Save, navigation, edit, delete) | Manages active roster state |
| crowd groups | grouped-list | Character entries grouped under crowd headers (Crowd 1, Crowd 2, No Crowd) | Each entry shows portrait thumbnail + character name; selected character highlighted green; right-click opens context menu |

### Panel 3: Character Editor

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| portrait | avatar | Character portrait circle + name ("Spyder") | Displays current character; name shown in text field |
| identity section | slot-row | Circular identity slots (active = green filled, empty = white) + add/remove buttons + scrollbar | Click slot to activate identity; green = active identity; `+` adds new identity, `-` removes selected |
| movements section | slot-row-with-icons | 6 action icon buttons (Loc, Dir, Flw, Cam, Scl, Rmv) + circular movement slots + scrollbar | Icon buttons activate movement commands; slots represent assigned movements |
| powers section | slot-grid | 2 rows of circular power slots + scrollbar | Slots represent assigned powers |
| crowds section | slot-row | Row of circular crowd membership slots + scrollbar | Shows crowd memberships for the character |
| navigate toggle | toggle | Dark circle toggle button | Enables/disables navigation mode |

### Panel 4: Costume Edit

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| identity name | form | Identity Name text input | Names the current identity being edited |
| model/costume selector | dropdown | "Model \ Costume" dropdown | Selects between model and costume identity type |
| default and play | form | Default checkbox, Play button (green triangle) | Checkbox marks identity as default; Play button previews the identity in game |
| type radios | radio-group | Costume / Model radio buttons | Switches between costume file mode and model mode |
| tag-model table | table | Tag and Model columns with data rows (Minion/Minion 1, Minion/Minion2, Minion/Brunhilda Crazy) | Selectable rows; selected row highlighted blue; shows tag-to-model mappings for the identity |
| animation dropdown | dropdown | Animation selector | Selects animation category |
| animation listbox | listbox | Scrollable list of animations (Become The Character 2!, Character 2 Attack, Character 2 Dodge) | Selected item highlighted blue; animations play on identity load |

### Controls detail — tag-model table

| Control | Input type | State / behavior |
| --- | --- | --- |
| Tag column | text (read-only) | Shows the tag category (e.g. "Minion") |
| Model column | text (read-only) | Shows the model name (e.g. "Minion 1", "Minion2", "Brunhilda Crazy") |
| Row selection | click | Single-select; selected row highlighted blue |

### Controls detail — animation listbox

| Control | Input type | State / behavior |
| --- | --- | --- |
| Animation items | listbox | Scrollable selectable list; selected item highlighted; items represent demo animations for the identity |

### Conditional states

| State | What changes |
| --- | --- |
| No identity selected | Costume Edit panel hidden or disabled; only Character Editor sections visible |
| Model identity active | Model radio selected; tag-model table shows model entries |
| Costume identity active | Costume radio selected; table shows costume file entries |
| Game not connected | Play button disabled; Spawn/Place in context menu disabled |
| Character not spawned | Navigation and movement actions disabled |

### Stories covered

| Story | Region | Trigger |
| --- | --- | --- |
| Add Identity to Character | identity section | `+` button |
| Set Identity Type (Model or Costume) | costume edit type radios | Costume/Model radio selection |
| Assign Costume Surface to Identity | costume edit tag-model table | Row selection in table |
| Set Default Identity | costume edit default checkbox | Default checkbox |
| Set Active Identity | identity section | Click identity slot to activate (green) |
| Remove Identity from Character | identity section | `-` button |
| Load Costume File for Active Identity | costume edit | Triggered by Set Active |
| Spawn Character with Model Identity | active roster context menu | Spawn command |
| Switch Active Identity on Spawned Character | identity section | Click different identity slot |
| Play Animation on Identity Load | animation listbox | Triggered after spawn completes |

### Domain terms traced

| Term | Appears as | Region |
| --- | --- | --- |
| identity | circular slot in identity section | character editor |
| model identity | "Model" radio button; model entries in table | costume edit |
| costume identity | "Costume" radio button; costume entries in table | costume edit |
| active identity | green filled circle in identity slot row | character editor |
| default identity | "Default" checkbox | costume edit |
| crowd | group header in active roster | active roster |
| crowd member | character entry under crowd header | active roster |
| spawned NPC | character in active roster | active roster |
| animation | items in animation listbox | costume edit |

---

## Screen 2: Active Roster Context Menu

**Layout:** `popup-overlay` — shadow-boxed popup on right-click
**Context:** triggered by right-clicking a character in the Active Roster
**Grid position:** col=1, row=0

### ASCII sketch

```
┌──────────────────┐
│ > Cam            │
│ < Cam            │
│ Target           │
│ Maneuver         │
│──────────────────│
│ Spawn            │
│ Place            │
│ Save Location    │
│──────────────────│
│ Activate         │
│ Edit       ← sel │
│ Remove           │
└──────────────────┘
```

### Regions

| Region | Type | Controls |
| --- | --- | --- |
| camera/navigation | menu-group | > Cam, < Cam, Target, Maneuver |
| spawn/placement | menu-group | Spawn, Place, Save Location |
| actions | menu-group | Activate, Edit (highlighted), Remove |

---

## Connections

| From | To | Label |
| --- | --- | --- |
| character editor (identity section) | costume edit | edit identity (dashed arrow) |

### Transition notes

- The **edit identity** connection is triggered when clicking an identity slot in the Character Editor; the Costume Edit panel opens/expands to edit that identity's properties
- The Active Roster context menu's **Edit** command (highlighted blue) opens the Character Editor for the selected character

---

## Design Reference Notes

Design images reviewed from `Design/3) Identities/`:

| Image | Key observations |
| --- | --- |
| Identities - Display Identities, movements, powers, crowds.png | Full 4-panel layout: Character Explorer tree, Active Roster with crowd groups, Character Editor with circular identity/movements/powers/crowds slots, and right-click context menu on Active Roster |
| Identity - activate all commands from character editor.png | Same 4-panel layout; Character Editor movements section has green highlighted icon buttons (Loc, Dir, Flw) |
| Identity - edit, add, remove Identity.png | Costume Edit panel visible with Identity Name, Model\Costume dropdown, Default checkbox, Play button, Costume/Model radios, Tag/Model table, Animation dropdown/listbox |
| Identity - Filter and Browse Costumes and Demo as you Chang Records.png | Costume Edit showing costume filter with listbox (Costume, Spyder, Spydera selected, White Spy) |
| Identity - Filter and Browse Models and Demo as you Chang Records.png | Costume Edit showing model filter with listbox (Model, Minion 1, Minion2 selected) |
| Identity - load identity.png | Costume Edit showing loaded identity with Tag/Model table populated (3 Minion entries) and animation list |

**Key design decisions captured:**
- Full application uses 4 side-by-side panels (Character Explorer, Active Roster, Character Editor, Costume Edit)
- Character Editor uses circular slot UI for identity/movements/powers/crowds sections (not flat grid tables)
- Active Roster groups characters under crowd headers with portrait thumbnails
- Costume Edit panel has form fields, a tag-model data table, and an animation listbox
- "Edit Character" collapsed vertical tab visible between Active Roster and Character Editor panels
- Context menu on Active Roster has 11 commands in 3 groups (camera, spawn/place, actions)
- Animation listbox is a scrollable selectable list (not a dropdown)

---

## Change log

| Date | Direction | Summary |
| --- | --- | --- |
| 2026-05-17 | redrawn | Hand-crafted mxGraph XML faithfully reproducing production UI from design images; 4-panel layout with circular slot UI in Character Editor, grouped Active Roster, and Costume Edit form with tag-model table and animation listbox |
