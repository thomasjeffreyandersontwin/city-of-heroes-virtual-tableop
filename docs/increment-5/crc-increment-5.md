---
state: crc
increment: 5
scope: Roster and Desktop Interaction
date: 2026-05-21
---

# CRC — Increment 5: Roster and Desktop Interaction

> Domain sources: `docs/increment-5/ubiquitous-language-increment-5.md`, `docs/increment-5/acceptance-criteria-increment-5.md`.

---

# Core Domain

## **Roster**

The session-scope ordered list of characters staged for active play. The live workspace record of who is in play, driving every desktop interaction.

### **Roster**
ordered entries                       | Roster Entry
                                      |   invariant: each character appears at most once in the roster at any time; adding a character already present is a no-op with user feedback
                                      |   invariant: the roster is session-scoped and is empty when a new session begins; prior session entries are not carried forward
add character                         | Character, Roster Entry
add crowd                             | Crowd, Character, Roster Entry
remove entry                          | Roster Entry, Spawned NPC, Game Bridge, Gang Mode
clear entry from desktop              | Roster Entry, Spawned NPC, Game Bridge

### **Roster Entry**
character name                        | Character
spawned state                         | Spawned State
                                      |   invariant: a roster entry's display name is the same as the owning character's name in the crowd library
active turn indicator                 | Active Character
gang membership indicator             | Gang Mode, Gang Leader

### **Spawned State**
presence in game world                | (true or false)
set on spawn                          | Game Bridge, Spawned NPC, Roster Entry
clear on despawn                      | Game Bridge, Roster Entry
clear on game done                    | Game Done State, Roster Entry

### **Active Character**
active designation                    | Roster Entry
                                      |   invariant: at most one active character holds the single-character active turn at any time unless gang mode activates multiple entries collectively
activate via roster panel             | Roster Entry, Character Overlay
activate via double-click             | Character Overlay, Roster Entry
activate via context menu             | Context Menu, Roster Entry
deactivate                            | Roster Entry, Character Overlay

### **Gang Mode**
collective activation state           | (active or inactive)
                                      |   invariant: gang mode may only be activated on a crowd whose members all have roster entries present; attempting activation when any member is absent produces an error and no partial activation occurs
member entries                        | Roster Entry, Crowd
activate gang                         | Roster Entry, Gang Leader, Crowd
                                      |   invariant: exactly one gang leader must be designated when gang mode is activated; no gang exists without a leader assignment
deactivate gang                       | Roster Entry, Character Overlay

### **Gang Leader**
leader designation                    | Roster Entry
                                      |   invariant: exactly one gang leader must be designated when gang mode is activated
leader indicator                      | Roster Entry

### references

**Ref — ubiquitous-language-increment-5.md (Roster KA)**
Source: docs/increment-5/ubiquitous-language-increment-5.md
Locator: Roster section (lines 55–128)
Extract: whole

```source
roster concept block, roster_entry concept block, spawned_state concept block, active_character concept block, gang_mode concept block, gang_leader concept block
```

### decisions made

- `Roster` is the root class: distinct identity (the session list), state (ordered entries), behavior (add character, add crowd, remove, clear), invariants (no duplicates, session-scoped)
- `Roster Entry` is a class: distinct identity (one character's session record), state (spawned state, active indicator, gang membership), behavior (displays conditional indicators); not a simple property
- `Spawned State` earns its own class: distinct behavior (set on spawn, cleared on despawn or game done), its own story, observable independently; not merely a Boolean property
- `Active Character` is a class: distinct state (active designation on a roster entry), behavior (activated via three paths, deactivated explicitly or by replacement), invariants (at most one unless gang)
- `Gang Mode` is a class: distinct state (active/inactive with member set), behavior (collective activation/deactivation), invariants (all members required, leader required)
- `Gang Leader` is a class: distinct identity (designated leader entry), distinct display behavior (leader indicator), serves as orientation reference in Increment 6; not merely a flag on roster entry

---

## **Desktop Overlay**

The visual interaction layer rendered atop the COH game view, showing each spawned character as a character overlay with status indicators.

### **Desktop Overlay**
rendered overlays                     | Character Overlay, Roster Entry
render overlay on spawn               | Character Overlay, Spawned State
remove overlay on despawn             | Character Overlay, Spawned State
update overlay positions              | Character Overlay, Movement Execution
highlight on game target change       | Character Overlay, Roster Entry, Memory Interface

### **Character Overlay**
position in game world                | Character, Spawned NPC
                                      |   invariant: a character overlay must exist for every roster entry with spawned state true; no spawned character is invisible in the desktop overlay
status indicator                      | Active Character, Gang Mode
selection highlight                   | (selected, multi-select, or none)
select via single click               | Roster Entry, Desktop Overlay
activate via double-click             | Active Character, Roster Entry
drag to new position                  | Movement Execution, Spawned NPC, Desktop Overlay
open context menu via right-click     | Context Menu

### **Multi-Select**
selected overlays                     | Character Overlay
                                      |   invariant: multi-select requires at least two character overlays to be selected; selecting a second without a modifier replaces the first selection
add to selection                      | Character Overlay, Desktop Overlay
remove from selection                 | Character Overlay
clear all                             | Desktop Overlay

### references

**Ref — ubiquitous-language-increment-5.md (Desktop Overlay KA)**
Source: docs/increment-5/ubiquitous-language-increment-5.md
Locator: Desktop Overlay section (lines 131–178)
Extract: whole

```source
desktop_overlay concept block, character_overlay concept block, multi_select concept block
```

### decisions made

- `Desktop Overlay` is the root class: distinct identity (session visual surface), state (set of rendered overlays, current selection), behavior (render, update, respond to target change, remove on despawn)
- `Character Overlay` is a class: distinct identity (per-character spatial marker), state (selected, active, gang), behavior (click, double-click, drag, right-click); a distinct spatial interaction object
- `Multi-Select` is a class: distinct state (two-or-more selected), behavior (modifier-click to add, cleared on plain click), its own story; not merely a property of a single overlay

---

## **Context Menu**

The right-click popup menu scoped to exactly one target character, routing actions to movement, camera, and bridge services.

### **Context Menu**
target character                      | Roster Entry, Character Overlay
                                      |   invariant: the context menu is always scoped to exactly one target character; no action applies to a different character than the one right-clicked
visible actions                       | Spawned State
open on right-click                   | Character Overlay, Desktop Overlay
dismiss                               | Desktop Overlay
invoke spawn                          | Spawned State, Game Bridge, Spawned NPC
invoke place at location              | Movement Execution, Mouse XYZ Position
invoke save position                  | Saved Character Position, Memory Interface
invoke move camera to target          | Camera Rig
invoke move target to camera          | Movement Execution, Camera Rig
invoke reset orientation              | Movement Execution
invoke maneuver with camera           | Camera Rig, Movement Execution
invoke activate option                | Active Character, Roster Entry
invoke clone-link                     | Character, Crowd, Roster Entry

### **Saved Character Position**
stored coordinates                    | (X/Y/Z world-space triple)
                                      |   invariant: a saved character position is only written when the target character's spawned state is true; the Save Position action is not available for unspawned characters
write from current position           | Memory Interface, Roster Entry

### references

**Ref — ubiquitous-language-increment-5.md (Context Menu KA)**
Source: docs/increment-5/ubiquitous-language-increment-5.md
Locator: Context Menu section (lines 182–218)
Extract: whole

```source
context_menu concept block, saved_character_position concept block
```

### decisions made

- `Context Menu` is a class: distinct identity (the targeted popup), state (open/closed, scoped target, conditional action visibility), behavior (opens on right-click, routes actions to boundary services), invariants (exactly one target)
- `Saved Character Position` is a class: distinct identity (persisted coordinate per roster entry), behavior (written on demand from memory interface, available for future restore), invariants (only when spawned)
- Context menu actions delegate to boundary services (Movement Execution, Camera Rig, Game Bridge); the context menu is the routing surface, not the executor

---

## **Pop-Up Menu**

The COH-native menu definition subsystem that writes and loads in-game action menus into the running COH client.

### **Pop-Up Menu**
menu definition content               | (text file content)
                                      |   invariant: a pop-up menu file must be written to the COH menus directory before a load command is issued; loading a non-existent file produces an error in the game client
write to menus directory              | COH Menus Directory
load into game client                 | Game Bridge

### **Area Attack Pop-Up Menu**
deployment trigger                    | (session initialization)
deploy at session start               | Pop-Up Menu, COH Menus Directory, Game Bridge

### **COH Menus Directory**
directory path                        | (file-system path within COH installation)
                                      |   invariant: the COH menus directory must exist and be writable before any pop-up menu write can proceed
writable state                        | (writable or not writable)

### references

**Ref — ubiquitous-language-increment-5.md (Pop-Up Menu KA)**
Source: docs/increment-5/ubiquitous-language-increment-5.md
Locator: Pop-Up Menu section (lines 222–262)
Extract: whole

```source
pop_up_menu concept block, area_attack_pop_up_menu concept block, COH_menus_directory concept block
```

### decisions made

- `Pop-Up Menu` is the root class: distinct identity (the menu file), state (written/not written, loaded/not loaded), behavior (write to disk, load into game, overwrite on update), invariants (write before load)
- `Area Attack Pop-Up Menu` earns its own class: distinct deployment trigger (session initialization), distinct dependency from attack configuration, its own story; not merely an instance
- `COH Menus Directory` is a class: distinct derivation and writable-check behavior, invariants ensuring readiness before writes; analogous to COH data directory from Increment 1

---

## **Game State Query**

The collection of real-time observation and execution infrastructure operations polling the live COH game via the HookCostume DLL.

### **Game State Query**
available state                       | Game Bridge
                                      |   invariant: game state query reports unavailable when the game bridge is not initialized or the COH game client is not running
query hovered NPC info                | Hovered NPC Info, Game Bridge
query mouse XYZ position              | Mouse XYZ Position, Game Bridge
check game done state                 | Game Done State, Game Bridge

### **Hovered NPC Info**
NPC name                              | Spawned NPC
identity data                         | Spawned NPC
observed state                        | (present or absent)

### **Mouse XYZ Position**
world-space coordinates               | (X/Y/Z coordinate triple)
                                      |   invariant: the mouse XYZ position is only meaningful when the COH game window has input focus; queries without focus may return stale or zero coordinates
focus validity                        | (authoritative or potentially stale)

### **Game Done State**
session ended                         | (true or false)
                                      |   invariant: once game done state becomes true, no spawn, move, or game command may be issued until a new game session is established
cascade to spawned state              | Spawned State, Roster Entry, Desktop Overlay

### **Command Chain**
ordered commands                      | Game Bridge
                                      |   invariant: no oversized command chain may be delivered to the game bridge whole; all sub-chains must be delivered in order
inspect before delivery               | Oversized Command Chain
deliver as batch                      | Game Bridge

### **Oversized Command Chain**
detected state                        | Command Chain
                                      |   invariant: no oversized command chain may be delivered to the game bridge whole; all sub-chains must be delivered in order before the next application operation proceeds
split into sub-chains                 | Command Chain, Game Bridge
deliver sub-chains in sequence        | Game Bridge

### references

**Ref — ubiquitous-language-increment-5.md (Game State Query KA)**
Source: docs/increment-5/ubiquitous-language-increment-5.md
Locator: Game State Query section (lines 266–327)
Extract: whole

```source
game_state_query concept block, hovered_NPC_info concept block, mouse_XYZ_position concept block, game_done_state concept block, command_chain concept block, oversized_command_chain concept block
```

### decisions made

- `Game State Query` is the root class of this KA: distinct identity (the DLL-backed observation service), behavior (three query operations), invariants (unavailable when bridge not ready)
- `Hovered NPC Info` is a class: distinct identity (observed entity under cursor), state (present/absent), behavior (returned on demand from DLL), its own story
- `Mouse XYZ Position` is a class: distinct identity (cursor-to-world coordinate), behavior (queried on demand for placement), invariants (focus-dependent validity), its own story
- `Game Done State` is a class: distinct state (false/true), behavior (polled, cascades to spawned state and overlay), invariants (blocks all game commands), its own story
- `Command Chain` is a class: distinct identity (ordered batch), behavior (assembled, inspected, delivered, split if oversized), its own story
- `Oversized Command Chain` is a class: distinct detection behavior and split-and-deliver response; analogous to Stale Memory Pointer from Increment 4 (problematic state with a defined recovery action)
- `execute load map command` and `close game bridge on shutdown` are new behaviors of the Game Bridge (boundary); recorded in the boundary entry rather than promoted to core concepts

---

# Boundary Domain

## **Character**

Owned by: Character and Crowd Library (Increment 1)

### **Character**
(no new responsibilities modeled — is the data entity added to the roster by name; receives a clone-linked copy when the GM invokes Clone-Link from the context menu)

### references

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

```source
Character concept — the named data entity from the crowd library added to the roster
```

### decisions made

- Character is a boundary concept: lifecycle and CRUD are owned by Increment 1; this increment depends on Character as the source of roster entry names and as the target of clone-link operations

---

## **Crowd**

Owned by: Character and Crowd Library (Increment 1)

### **Crowd**
(no new responsibilities modeled — provides the group of characters expanded into individual roster entries when the GM adds a crowd to the roster; receives clone-linked characters)

### references

**Ref — ubiquitous-language-increment-1.md (Crowd KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Crowd KA section

```source
Crowd concept — named hierarchical container of characters used as batch source for roster population
```

### decisions made

- Crowd is a boundary concept: structure, membership, and persistence are owned by Increment 1; this increment uses Crowd as a batch source for roster population and as the target for clone-link additions

---

## **Spawned NPC**

Owned by: Character Identities (Increment 2)

### **Spawned NPC**
(no new responsibilities modeled — is created for each roster entry when spawned, removed on clear/remove/game-done; its world-space position drives the character overlay's position)

### references

**Ref — ubiquitous-language-increment-2.md (Identity KA — spawned NPC)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA — spawned NPC section

```source
Spawned NPC concept — the in-game entity whose lifecycle drives roster entry spawned state
```

### decisions made

- Spawned NPC is a boundary concept: spawn and despawn lifecycle are owned by Increment 2; this increment depends on Spawned NPC as the in-game representation of each roster entry with spawned state true

---

## **Game Bridge**

Owned by: Character Identities (Increment 2)

### **Game Bridge**
(no new responsibilities modeled — executes spawn/despawn/place/load-map commands; writes pop-up menu files and issues load commands; executes game state query operations via HookCostume DLL; delivers command chains; closes cleanly on shutdown; executes load-map command for map transitions)

### references

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

```source
Game Bridge concept — DLL bridge extended with pop-up menu, game state query, command chain delivery, shutdown, and load-map operations
```

### decisions made

- Game Bridge is a boundary concept: core initialization and slash-command routing are owned by Increment 2; this increment extends routing with pop-up menu write/load, game state queries via HookCostume DLL, command chain splitting, clean shutdown, and load-map command

---

## **Memory Interface**

Owned by: Single Character Movement (Increment 4)

### **Memory Interface**
(no new responsibilities modeled — provides character position read for save-position; monitors current target register for roster and desktop overlay selection synchronization)

### references

**Ref — ubiquitous-language-increment-4.md (Memory Interface KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Memory Interface KA section

```source
Memory Interface concept — reads character position for save-position and monitors current target for roster sync
```

### decisions made

- Memory Interface is a boundary concept: attach, pointer resolution, and polling logic are owned by Increment 4; this increment reads character position for save-position and monitors current target for roster sync

---

## **Camera Rig**

Owned by: Single Character Movement (Increment 4)

### **Camera Rig**
(no new responsibilities modeled — invoked by Move Camera to Target, Move Target to Camera, and Maneuver with Camera actions from the context menu)

### references

**Ref — ubiquitous-language-increment-4.md (Camera Rig KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Camera Rig KA section

```source
Camera Rig concept — surfaces camera operations through the context menu without modifying the rig's own behavior
```

### decisions made

- Camera Rig is a boundary concept: deployment, follow, detach, and position reading are owned by Increment 4; this increment surfaces camera rig operations through the context menu

---

## **Movement Execution**

Owned by: Single Character Movement (Increment 4)

### **Movement Execution**
(no new responsibilities modeled — invoked when the GM drags a character overlay, selects Place at Location, Move Target to Camera, or Reset Orientation from the context menu)

### references

**Ref — ubiquitous-language-increment-4.md (Movement Execution KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Movement Execution KA section

```source
Movement Execution concept — triggered by drag and context-menu actions for character repositioning and orientation
```

### decisions made

- Movement Execution is a boundary concept: command issuance, distance tracking, and collision checking are owned by Increment 4; this increment triggers movement execution via drag and context-menu actions
