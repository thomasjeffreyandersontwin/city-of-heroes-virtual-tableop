# Lo-fi Wireframes — Increment 5: Roster and Desktop Interaction

## Metadata

| Field | Value |
| --- | --- |
| Scope | Increment 5: Roster and Desktop Interaction |
| Increment outcome | GM can populate a *roster*, spawn characters to the desktop, activate them for play, and interact via mouse and context menus — the live session workspace |
| UL file | `docs/domain/ubiquitous-language-increment-5.md` |
| AC file | `docs/stories/acceptance-criteria-increment-5.md` |
| State JSON | `docs/ux/lo-fi/increment-5-state.json` |
| Drawio file | `docs/ux/lo-fi/increment-5.drawio` |
| IA reference | `docs/ux/initial-ia.md` |
| Design references | `Design/2) Roster/` (empty — no source images available) |
| Date | 2026-05-17 |
| Generator | `drawio-mockup.mjs save` |
| Drawio file size | 40,544 bytes |

---

## Screen 1: crowd manager — identities *(reference — col 1, row 0)*

**Layout:** `sidebar` — crowd tree (panel slot, 33%) · identity list (body slot, 67%)  
**Context:** pre-session — placeholder screen included for connection source

This screen is fully specified in Increment 2. Shown here only as the connection source for the *starts game session* transition to the *desktop* screen.

**Transition out:** → desktop : *starts game session*

---

## Screen 2: desktop *(primary — col 3, row 0)*

**Layout:** `split-screen` — roster panel (left slot, 50%) · game overlay + context menu stacked (right slot, 50%)  
**Context:** in-session — active once the game session begins

### ASCII sketch

```
[desktop]
┌──────────────────────┬────────────────────────┐
│ roster panel         │ game overlay           │
│  character name      │  character overlay     │
│  spawned · active    │  status indicator      │
│  status              ├────────────────────────┤
│  ─────────────────── │  [Select]              │
│  [Add] [Add Crowd]   │  [Multi-Select]        │
│  [Spawn*] [Remove]   │  [Drag to Position]    │
│  [Clear]             │  [Dbl-Click Activate*] │
│  ─────────────────── ├────────────────────────┤
│  [Activate*]         │ context menu           │
│  [Deactivate]        │  target character      │
│  [Activate Gang]     ├────────────────────────┤
│  [Deactivate Gang]   │  [Spawn]               │
│                      │  [Place at Location]   │
│                      │  [Save Position]       │
│                      │  [Move Camera→Target]  │
│                      │  [Move Target→Camera]  │
│                      │  [Reset Orientation]   │
│                      │  [Maneuver w/ Camera]  │
│                      │  [Activate Option*]    │
│                      │  [Clone-Link]          │
└──────────────────────┴────────────────────────┘
```

*Primary actions marked with \**

### Regions

| Region | Type | Slot | Columns / Fields | Actions | Interaction decisions |
| --- | --- | --- | --- | --- | --- |
| roster panel | list | left | character name · spawned · active · status | Add · Add Crowd · Spawn · Remove · Clear | Spawn is primary; spawned indicator shown only when *Spawned State* is true; active indicator shown only when character is *Active Character*; status column shows gang/leader indicator |
| roster panel — activation | button-bar | left | — | Activate · Deactivate · Activate Gang · Deactivate Gang | Activate is primary; Activate Gang opens crowd/leader selection dialog; Deactivate Gang is only actionable when *Gang Mode* is active |
| game overlay | list | right | character overlay · status indicator | Select · Multi-Select · Drag to Position · Double-Click to Activate | Double-Click is primary; overlays rendered only for entries with *Spawned State* true; multi-select via shift/ctrl click; drag enables *Movement Execution* |
| context menu | list | right | target character | Spawn · Place at Location · Save Position · Move Camera to Target · Move Target to Camera · Reset Orientation · Maneuver with Camera · Activate Option · Clone-Link | Activate Option is primary; Spawn only visible when *Spawned State* false; menu scoped to the right-clicked *Character Overlay* |

### Controls detail

| Control | Input type | State / behavior |
| --- | --- | --- |
| Add | button | opens character picker from crowd library |
| Add Crowd | button | opens crowd picker; expands all leaf characters into roster entries |
| Spawn | button (primary) | disabled when no roster entry selected or selected entry is already spawned |
| Remove | button | triggers despawn if *Spawned State* true, then removes roster entry |
| Clear | button | despawns NPC, sets *Spawned State* false, entry stays in roster |
| Activate | button (primary) | marks selected entry as *Active Character*; clears previous active |
| Deactivate | button | removes active indicator from selected entry |
| Activate Gang | button | opens gang setup dialog: crowd picker + leader designation |
| Deactivate Gang | button | disabled when no *Gang Mode* is active |
| Select (overlay) | mouse click | single-click selects overlay and highlights roster entry |
| Multi-Select (overlay) | shift/ctrl click | adds overlay to current selection; repeated click removes from selection |
| Drag to Position (overlay) | drag-drop | invokes *Movement Execution* to reposition *Spawned NPC* at drop point |
| Double-Click to Activate | double-click (primary) | marks character as *Active Character*; equivalent to Activate action |
| Spawn (context) | menu item | visible only when *Spawned State* false |
| Place at Location | menu item | reads *Mouse XYZ Position*; moves character to cursor location |
| Save Position | menu item | reads position from *Memory Interface*; stores *Saved Character Position* |
| Move Camera to Target | menu item | moves *Camera Rig* to character's in-game position |
| Move Target to Camera | menu item | moves *Spawned NPC* to *Camera Rig* position via *Movement Execution* |
| Reset Orientation | menu item | writes identity rotation via *Movement Execution* |
| Maneuver with Camera | menu item | toggles maneuver-with-camera mode for target character |
| Activate Option | menu item (primary) | marks character as *Active Character* |
| Clone-Link | menu item | creates linked copy in crowd library; adds new roster entry |

### Conditional states

| State | Roster Panel | Game Overlay |
| --- | --- | --- |
| No characters in roster | Empty-roster placeholder row shown | No overlays rendered |
| Character with *Spawned State* false | Spawned indicator hidden; Spawn button enabled | No *Character Overlay* shown |
| Character with *Spawned State* true | Spawned indicator visible | *Character Overlay* rendered at in-game position |
| *Active Character* | Active indicator shown on entry | Active status indicator on *Character Overlay* |
| *Gang Mode* active | Gang indicator on all member entries; leader indicator on *Gang Leader* | Gang status indicator on all member *Character Overlays* |
| *Multi-Select* | All selected entries highlighted | All selected overlays show multi-select highlight |

### Stories covered by this screen

**Roster Panel:**
- Add Character to Roster
- Add Crowd to Roster
- Spawn Character to Desktop from Roster
- Remove Character from Roster
- Clear Character from Desktop
- Activate Character (mark as active turn)
- Deactivate Character
- Activate Crowd as Gang with Gang Leader
- Deactivate Gang
- Track Spawned State per Character
- Sync Roster Selection with Game Target

**Game Overlay:**
- Select Character on Desktop via Mouse Click
- Multi-Select Characters
- Drag Character to New Position on Desktop
- Double-Click Character to Activate

**Context Menu:**
- Spawn Character via Context Menu
- Place Character at Location
- Save Character Position
- Move Camera to Target Character
- Move Target Character to Camera
- Reset Character Orientation via Context Menu
- Maneuver Character with Camera via Context Menu
- Activate Character Option via Context Menu
- Clone and Link Character from Desktop

---

## Screen 3: attack configuration *(reference — col 4, row 0)*

**Layout:** `flyout` — attack configuration panel (body slot, 65%) · panel slot (35%, unused)  
**Context:** in-session — active during attack workflow

This screen is fully specified in Increment 6. Shown here only as the connection target for the *activates attack ability* transition from the *desktop* screen.

**Transition in:** ← desktop : *activates attack ability*

---

## Connections

| From | To | Label | Direction |
| --- | --- | --- | --- |
| crowd manager — identities | desktop | starts game session | → |
| desktop | attack configuration | activates attack ability | → |

---

## Infrastructure stories not covered by a dedicated screen

The following Increment 5 stories are delivered via the game engine layer and do not have dedicated UI screens in the lo-fi:

| Story | Where behavior is visible |
| --- | --- |
| Query Hovered NPC Info from Game | *Roster Panel* and *Desktop Overlay* selection sync reflects hovered NPC identity |
| Query Mouse XYZ Position in Game World | Place at Location uses the queried position as the destination |
| Check Game Done State | All *Character Overlays* are cleared and spawned indicators reset when game ends |
| Split Oversized Command Chains for Execution | Transparent to UI; ensures game commands execute without COH-side rejection |
| Close Game Bridge on Shutdown | Transparent to UI; executes on application exit |
| Execute Load Map Command | Triggered by session initialization; no dedicated UI panel in this increment |
| Write Pop-Up Menu Files to COH Menus Directory | File-system operation; result visible in COH game client HUD |
| Load Pop-Up Menu in Game | Game-side effect; area attack HUD entries appear in COH |
| Deploy Area Attack Pop-Up Menu | Session initialization step; area attack pop-up active after session start |
