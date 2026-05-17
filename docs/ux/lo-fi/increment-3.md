# Lo-fi Wireframes — Increment 3: Animated Abilities

> Increment 3 adds the Animated Abilities authoring surface to the crowd manager. Two screens are introduced: the **crowd manager — abilities** tab (sibling to the identities tab from Increment 1) and the **ability editor** form. Design images for folders `Design/4) Ability/` and `Design/5) Ability Groups/` were referenced but found empty at generation time; wireframe structure was derived from `initial-ia.md`.

---

## Metadata

| Field         | Value |
| ---           | --- |
| Increment     | 3 — Animated Abilities |
| State file    | `docs/ux/lo-fi/increment-3-state.json` |
| Drawio file   | `docs/ux/lo-fi/increment-3.drawio` |
| File size     | 26,884 bytes |
| Screens       | 2 |
| Connections   | 5 |
| Generated     | 2026-05-17 |

---

## Screens

### crowd manager — abilities

- **Layout:** `sidebar` — crowd tree panel (left 33%, `--dimmed`) · tab content (right 67%)
- **Grid position:** col 1, row 1 (sibling to crowd manager — identities at col 1, row 0)
- **Context:** pre-session — abilities tab active; crowd tree greyed (same shell as identities/movements)

```
[crowd manager — abilities]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬─────────────────────────────────┐
╎ crowd tree (dimmed)  ╎ Identities  [Abilities]  Mvt    │
╎  crowd name (n)      ├─────────────────────────────────┤
╎    > char name       │ ability list                    │
╎    > type · spawned  │  name · key · persistent · atk  │
╎    > active          │  ─────────────────────────────  │
╎  (greyed)            │  (no selection)                 │
╎                      │  (active ability indicator →●)  │
╎                      │  (persistent flag →★)           │
╎                      ├─────────────────────────────────┤
╎                      │  Create  Edit  Delete  Set Key  │
╎                      │  Toggle Persist  Set Default    │
╎                      │  [Play]  Stop                   │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴─────────────────────────────────┘
```

**Regions:**

| Region | Slot | Type | Visible fields | Actions |
| --- | --- | --- | --- | --- |
| crowd tree (dimmed) | panel | chrome | crowd name · char name · type · spawned · active (greyed) | *(none active on this tab)* |
| tab bar | body | nav-tabs | Identities · **Abilities** · Movements | tab navigation |
| ability list | body | list | name · key · persistent · attack | Create · Edit · Delete · Set Key · Toggle Persist · Set Default · **Play** · Stop |

**Conditional states:**
- *No selection* — action buttons disabled until an ability row is selected
- *Active ability indicator* — `●` marker on the currently executing ability row
- *Persistent flag* — `★` marker on rows with persistence enabled

---

### ability editor

- **Layout:** `form` — single column, two stacked sections (ability config above element list)
- **Grid position:** col 2, row 1
- **Context:** pre-session — opened from crowd manager — abilities via Edit action

```
[ability editor]
┌────────────────────────────────────────┐
│ ability config                         │
│  Name         [____________________]  │
│  Activation Key [________________]    │
│  Persistent   [✓]                     │
│  Attack Flag  [ ]                     │
├────────────────────────────────────────┤
│               [Save]  [Cancel]         │
├────────────────────────────────────────┤
│ element list                           │
│  type · resource · order · persistent  │
│  ────────────────────────────────────  │
│  (empty state: no elements yet)        │
│  (drag-drop reorder active: ↕ handle)  │
├────────────────────────────────────────┤
│  Add FX · Add MOV · Add Sound          │
│  Add Reference · Add Sequence          │
│  Add Pause · Add Identity              │
│  Reorder ↕ · Remove                   │
└────────────────────────────────────────┘
```

**Regions:**

| Region | Slot | Type | Visible fields | Actions |
| --- | --- | --- | --- | --- |
| ability config | body | form | Name · Activation Key · Persistent (checkbox) · Attack Flag (checkbox) | Save · Cancel |
| element list | body | list | type · resource · order · persistent flag | Add FX · Add MOV · Add Sound · Add Reference · Add Sequence · Add Pause · Add Identity · Reorder ↕ · Remove |

**Conditional states:**
- *Empty element list* — element list shows an empty-state placeholder row before any elements are added
- *Drag-drop reorder active* — reorder drag handle (↕) visible on each row when Reorder is activated; row opacity changes during drag to indicate active target

---

## Connections

| From | To | Label | Style |
| --- | --- | --- | --- |
| crowd manager — identities | crowd manager — abilities | selects Abilities tab | dashed |
| crowd manager — abilities | crowd manager — identities | selects Identities tab | dashed |
| crowd manager — abilities | crowd manager — movements | selects Movements tab | dashed |
| crowd manager — abilities | ability editor | edit ability | solid |
| ability editor | crowd manager — abilities | saves / cancels | solid |

---

## Screens not covered in this increment

The following screens from `initial-ia.md` are present in the drawio diagram from prior increments (col 0, row 0 and col 1, row 0) but are not re-generated here:

- game directory prompt (Increment 1 — col 0, row 0)
- crowd manager — identities (Increment 1 — col 1, row 0)

The crowd manager — movements screen (col 1, row 2) and movement editor (col 2, row 2) are scoped to Increment 4 and are not wired in this drawio.

---

## Stories mapped to screens

| Story | Screen | Region | Action |
| --- | --- | --- | --- |
| Create Animated Ability | crowd manager — abilities | ability list | Create |
| Edit Animated Ability | ability editor | ability config | Save |
| Delete Animated Ability | crowd manager — abilities | ability list | Delete |
| Set Ability Activation Key | crowd manager — abilities | ability list | Set Key |
| Toggle Ability Persistence | crowd manager — abilities | ability list | Toggle Persist |
| Set Default Ability for Character | crowd manager — abilities | ability list | Set Default |
| Play Animated Ability on Character | crowd manager — abilities | ability list | Play |
| Stop Active Ability | crowd manager — abilities | ability list | Stop |
| Add FX Element to Ability | ability editor | element list | Add FX |
| Add Movement Element to Ability | ability editor | element list | Add MOV |
| Add Sound Element to Ability | ability editor | element list | Add Sound |
| Add Reference Element to Another Ability | ability editor | element list | Add Reference |
| Add Sequence Element (And/Or) | ability editor | element list | Add Sequence |
| Add Pause Element | ability editor | element list | Add Pause |
| Add Load-Identity Element | ability editor | element list | Add Identity |
| Reorder Animation Elements via Drag-Drop | ability editor | element list | Reorder ↕ |
| Browse FX Resources for Ability Authoring | ability editor | element list | Add FX (resource picker) |
| Browse Movement Resources for Ability Authoring | ability editor | element list | Add MOV (resource picker) |
| Browse Sound Resources for Ability Authoring | ability editor | element list | Add Sound (resource picker) |
