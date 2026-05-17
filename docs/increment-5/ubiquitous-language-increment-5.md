---
state: ubiquitous-language
increment: 5
scope: Roster and Desktop Interaction
date: 2026-05-17
---

# Ubiquitous Language — Increment 5: Roster and Desktop Interaction

> Scope: the vocabulary needed to populate a session *roster* from the crowd library, spawn characters to the in-game *desktop overlay*, activate characters for play, manage *gang mode*, interact with characters through mouse selection and a *context menu*, load COH *pop-up menus*, and observe live game state (*hovered NPC info*, *mouse XYZ position*, *game done state*, *command chain* splitting). Builds on Increment 1 (Character, Crowd, Crowd Repository), Increment 2 (Spawned NPC, Game Bridge, Identity), Increment 3 (Animated Ability, Keyboard Hook), and Increment 4 (Memory Interface, Movement Execution, Camera Rig).

---

**Terms**:
- **Roster**
  - **roster** — the ordered list of characters staged for the current game session; the live workspace record of who is in play
  - **roster entry** — a single character's session record in the *roster*, tracking name, *spawned state*, active turn indicator, and gang membership
  - **spawned state** — the per-*roster entry* Boolean flag indicating whether the character's NPC is currently rendered in the COH game world
  - **active character** — a *roster entry* marked as holding the current turn in play; its abilities and overlays are foregrounded
  - **gang mode** — the collective activation state applied to a group of *roster entries* from the same *crowd*, causing them to activate and deactivate together
  - **gang leader** — the *roster entry* designated to lead the gang; whose activation triggers gang-wide activation for all members
- **Desktop Overlay**
  - **desktop overlay** — the visual interaction layer rendered atop the COH game view, showing each spawned character as a *character overlay* with status indicators
  - **character overlay** — the per-character visual marker in the *desktop overlay* at the character's in-game position; the click, drag, and double-click target for GM interaction
  - **multi-select** — the state in which two or more *character overlays* are simultaneously selected, enabling batch operations
- **Context Menu**
  - **context menu** — the right-click popup menu appearing on a *character overlay*, presenting actions targeted at the specific character that was right-clicked
  - **saved character position** — the stored X/Y/Z world-space coordinate persisted for a *roster entry* after the GM triggers Save Position
- **Pop-Up Menu**
  - **pop-up menu** — a COH-native menu definition file written to the *COH menus directory* and loaded into the game client to provide in-game action shortcuts
  - **area attack pop-up menu** — the specific *pop-up menu* deployed at session start to support area-attack target designation in the COH game client
  - **COH menus directory** — the file-system subdirectory within the COH installation where *pop-up menu* files are stored and picked up by the game client
- **Game State Query**
  - **game state query** — the collection of real-time observation operations that poll the live COH game via the HookCostume DLL for data not available through process memory
  - **hovered NPC info** — the NPC name and identity data returned when the GM's mouse hovers over an NPC entity in the COH game viewport
  - **mouse XYZ position** — the three-dimensional world-space coordinates of the GM's mouse cursor position in the COH game world
  - **game done state** — the Boolean flag reported by the COH engine indicating whether the current game session has ended
  - **command chain** — the ordered sequence of game commands assembled for delivery to the *game bridge* as a single execution batch
  - **oversized command chain** — a *command chain* whose payload exceeds the COH engine's per-execution limit; must be split into sub-chains before delivery

---

The Roster and Desktop Interaction increment is the first that transforms the GM's pre-session crowd library into a live session workspace. The GM populates the *roster* by adding individual *characters* or entire *crowds*; each addition creates a *roster entry* recording the character's name, *spawned state* (initially false), and turn status. The GM then spawns selected characters from the *roster panel*, setting each entry's *spawned state* to true and rendering the character as a *spawned NPC* visible in the COH game world.

Once spawned, characters appear in the *desktop overlay* as *character overlays* with status indicators. The GM selects a character via single click (selecting the *character overlay* and highlighting the matching *roster entry*), builds a *multi-select* group with shift/ctrl modifiers, drags *character overlays* to reposition characters in the game world, and double-clicks to activate a character's turn immediately. When the COH game client changes its current target, the *memory interface* detects the change and the *roster* selection synchronizes automatically. A right-click on any *character overlay* reveals the *context menu*, presenting actions targeted at that character: spawn (if unspawned), place at location, save the current position as a *saved character position*, move camera to target, move target to camera, reset orientation, maneuver with camera, activate an ability option, and clone-link the character back to the crowd library.

*Gang mode* is a roster-level collective activation pattern: the GM designates a *crowd* as a gang and assigns a *gang leader*; all *roster entries* from that crowd are activated simultaneously. Deactivating the gang clears active status from all gang members at once, with the gang indicator removed from the *roster panel*.

The increment introduces the *pop-up menu* subsystem: the application writes *pop-up menu* files to the *COH menus directory* and issues load commands so the game client picks them up immediately. The *area attack pop-up menu* is deployed at session initialization. Five *game state query* operations complete the session infrastructure: reading *hovered NPC info* on mouse hover, querying *mouse XYZ position* for placement, checking *game done state* to detect session end, splitting *oversized command chains* into valid sub-chains before delivery, and closing the *game bridge* cleanly on application shutdown.

---

# Core Domain

## Roster

*Roster* is the session-scope ordered list of characters the GM stages for active play. Each *roster entry* carries the character's name, *spawned state*, and active turn indicator; the *roster* is the live session record of who is in play and drives every desktop interaction. The GM populates the *roster* by adding a single *character* or an entire *crowd* (expanding its members individually). Characters can be removed from the *roster* (and despawned if needed), or cleared from the desktop (despawning without removing the entry). A *roster entry* with *spawned state* true has a live *spawned NPC* in the game world and a *character overlay* in the *desktop overlay*; one with *spawned state* false is listed but not visible in game. The *roster* enforces no duplicate entries and is session-scoped: it is empty at session start and is not persisted between sessions. The *roster* exposes *gang mode* as a collective activation pattern in which a group of entries from the same *crowd* are activated and deactivated as a unit under a *gang leader*.

### roster

- is populated when the GM adds a single *character* by name; the entry appears in the *roster panel* with *spawned state* false and no active or gang indicator
- is expanded when the GM adds a *crowd*; each *character* in the crowd (expanding nested crowds recursively to all leaf members) is added as a separate *roster entry*
- removes an entry and clears the *spawned NPC* when the GM removes a character whose *spawned state* is true; if *spawned state* is false, the entry is removed without any game command
- preserves the *roster entry* but sets *spawned state* false when the GM clears the character from the desktop; the game command despawns the *spawned NPC* without deleting the entry
- shows an empty-roster message in the *roster panel* when it contains no entries
- **Invariant:** each *character* appears at most once in the *roster* at any time; adding a character already present is a no-op with user feedback
- **Invariant:** the *roster* is session-scoped and is empty when a new session begins; prior session entries are not carried forward

### roster_entry

- records the character's name, *spawned state*, active turn indicator, and gang membership for one character in the *roster*
- displays a spawned indicator in the *roster panel* when *spawned state* is true; the indicator is hidden when *spawned state* is false
- displays an active indicator when the entry has been activated for the current turn
- displays a gang membership indicator and the *gang leader* designation when the entry belongs to an active *gang mode* group
- displays the empty-roster message placeholder row when it is the only content and *spawned state* has never been set
- **Invariant:** a *roster entry's* display name is the same as the owning *character's* name in the crowd library

### spawned_state

- is a Boolean property of *roster entry* — true when the character's *spawned NPC* is present in the COH game world, false otherwise
- is set to true when the GM spawns the character via the *roster panel* Spawn action or via Spawn in the *context menu*; the *game bridge* issues the spawn command and the NPC appears in game
- is set to false when the GM clears the character from the desktop, when the character is removed from the *roster*, or when *game done state* becomes true
- is tracked independently per entry; multiple characters can be spawned simultaneously

### active_character

- is a *roster entry* marked with an active turn indicator set by the GM
- is activated explicitly via the Activate action in the *roster panel*, via double-click on the *character overlay* in the *desktop overlay*, or via the Activate Option action in the *context menu*
- is deactivated explicitly via the Deactivate action in the *roster panel* or automatically when another character is activated and the session enforces single-active mode
- shows a distinct active indicator in the *roster panel* and a status indicator in the matching *character overlay*
- **Invariant:** at most one *active character* holds the single-character active turn at any time unless *gang mode* activates multiple entries collectively

### gang_mode

- is the collective activation state applied to a group of *roster entries* from the same *crowd*, enabling them to activate and deactivate together
- is activated by the GM via the Activate Gang action, specifying a *crowd* and designating a *gang leader*; all *roster entries* from that crowd are immediately marked active and display the gang indicator
- is deactivated by the GM via the Deactivate Gang action; all *roster entries* in the gang are marked inactive and their gang indicators are removed simultaneously
- **Invariant:** *gang mode* may only be activated on a *crowd* whose members all have *roster entries* present; attempting activation when any member is absent produces an error and no partial activation occurs

### gang_leader

- is the *roster entry* designated by the GM to represent the collective activation trigger and orientation reference for a *gang mode* group
- is assigned at the time *gang mode* is activated from the Activate Gang dialog
- is displayed with a leader indicator in the *roster panel* while *gang mode* is active, distinguishing it from other gang members
- serves as the orientation reference for crowd-facing alignment in Increment 6
- **Invariant:** exactly one *gang leader* must be designated when *gang mode* is activated; no gang exists without a leader assignment

### Decisions made

- `roster` is a concept: distinct identity (the session list), state (ordered set of entries, session-scoped), behavior (add character, add crowd, remove, clear, session lifecycle), invariants (no duplicates, session-scoped); the central data structure of this increment
- `roster entry` is a concept: distinct identity (one character's session record), state (spawned state, active indicator, gang membership), behavior (shows conditional indicators across multiple stories), invariants (name tied to library character); not reducible to a simple property
- `spawned state` earns its own concept block (not just a property stub): it has its own story (Track Spawned State per Character), distinct behavior (set on spawn, cleared on despawn or session end), and is observable independently across the roster panel
- `active character` is a concept: distinct state (active indicator set), behavior (activated via multiple paths, deactivated explicitly or by replacement), invariants (at most one unless gang mode), its own stories (Activate Character, Deactivate Character, Double-Click to Activate)
- `gang mode` is a concept: distinct state (active/inactive with member set), behavior (collective activation/deactivation, shows indicators on all members), invariants (all crowd members required, leader required), its own stories (Activate Crowd as Gang, Deactivate Gang)
- `gang leader` is a concept: distinct identity (the designated leader entry), distinct display behavior (leader indicator), serves as orientation reference in Increment 6; not merely a Boolean flag on roster entry

### References

**Ref — thin-slicing.md (Increment 5: roster stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 213–228
Extract: Add Character to Roster, Add Crowd to Roster, Spawn Character to Desktop from Roster, Remove Character from Roster, Clear Character from Desktop, Activate Character, Deactivate Character, Activate Crowd as Gang with Gang Leader, Deactivate Gang, Sync Roster Selection with Game Target, Track Spawned State per Character

**Ref — initial-ia.md (desktop — roster panel)**
Source: docs/ux/initial-ia.md
Locator: lines 311–354
Extract: roster panel — columns: character name · spawned · active · status; actions: add · add-crowd · spawn · remove · clear · activate · deactivate · activate-gang · deactivate-gang; domain terms: roster, active character, gang mode, gang leader, spawned character

---

## Desktop Overlay

*Desktop Overlay* is the visual interaction layer rendered atop the COH game view during a session. It displays each *roster entry* with *spawned state* true as a *character overlay* marker positioned at the character's in-game world coordinates, with a status indicator showing active and gang state. The GM uses the *desktop overlay* as the primary spatial interface: single-clicking a *character overlay* selects it and highlights the matching *roster entry* in the *roster panel*; shift/ctrl clicking builds a *multi-select* group; dragging repositions the character in the game world; double-clicking activates the character's turn. The *desktop overlay* also synchronizes roster selection with the COH game target: when the *memory interface* detects a target change, the matching *character overlay* and *roster entry* are highlighted automatically. Characters whose *spawned state* becomes false are removed from the overlay without delay.

### desktop_overlay

- renders a *character overlay* for each *roster entry* with *spawned state* true, placed at the character's current in-game world coordinates
- updates *character overlay* positions when characters are moved in the game world via drag, Place at Location, or Move Target to Camera operations
- removes a *character overlay* immediately when the character's *spawned state* becomes false
- highlights the *character overlay* and *roster entry* matching the current game target when the *memory interface* detects a target-register change
- shows no overlays when no *roster entries* have *spawned state* true

### character_overlay

- is the per-character visual marker in the *desktop overlay* positioned at the character's in-game world coordinates
- shows a status indicator reflecting the character's current state: spawned indicator when present, active indicator when the character is the *active character*, gang indicator when part of an active *gang mode* group
- is single-clicked by the GM to select the character: the overlay displays a selection highlight and the matching *roster entry* is highlighted in the *roster panel*
- is double-clicked by the GM to activate the character: equivalent to pressing the Activate action in the *roster panel*; the character becomes the *active character*
- is dragged by the GM to a new position: *movement execution* repositions the *spawned NPC* to the drop-point coordinates; the overlay repositions to the new in-game location
- is right-clicked to open the *context menu* targeting this character
- **Invariant:** a *character overlay* must exist for every *roster entry* with *spawned state* true; no spawned character is invisible in the *desktop overlay*

### multi_select

- is the state in which two or more *character overlays* are simultaneously selected using shift-click or ctrl-click modifiers on the *desktop overlay*
- is activated by shift-clicking or ctrl-clicking an additional *character overlay* while at least one is already selected; each clicked overlay is added to the current selection
- shows all selected *character overlays* with a distinct multi-select highlight; all matching *roster entries* are highlighted simultaneously in the *roster panel*
- is cleared when the GM clicks a single *character overlay* without a shift/ctrl modifier, or clicks empty space in the overlay
- **Invariant:** *multi-select* requires at least two *character overlays* to be selected; selecting a second without a modifier replaces the first selection

### Decisions made

- `desktop overlay` is a concept: distinct identity (the session visual surface), state (set of rendered overlays, current selection), behavior (render, update positions, respond to target change, remove on despawn), interactions with memory interface and movement execution
- `character overlay` is a concept: distinct identity (per-character spatial marker), state (selected, active, gang, none), behavior (click to select, double-click to activate, drag to move, right-click for context menu); not a data row — it is a distinct spatial interaction object
- `multi-select` is a concept: distinct state (two-or-more selected), behavior (modifier-click to add, cleared on plain click); its own story (Multi-Select Characters); not merely a property of a single overlay
- Scope: drag-to-new-position is modeled here as a desktop overlay interaction; the movement execution machinery that handles the NPC repositioning is boundary (Increment 4)

### References

**Ref — thin-slicing.md (Increment 5: desktop overlay stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 220–227
Extract: Select Character on Desktop via Mouse Click, Multi-Select Characters, Drag Character to New Position on Desktop, Double-Click Character to Activate, Sync Roster Selection with Game Target

**Ref — initial-ia.md (desktop — game overlay)**
Source: docs/ux/initial-ia.md
Locator: lines 334–337
Extract: game overlay — columns: character overlay · status indicator; actions: select · multi-select · drag to position · double-click to activate

---

## Context Menu

*Context Menu* is the right-click popup menu that appears in the *desktop overlay* when the GM right-clicks a *character overlay*. It presents a targeted action set for the specific character under the cursor: spawn (if not yet spawned), place at a specified location, save the current position as a *saved character position*, move the camera to the character, move the character to the camera, reset orientation, engage maneuver-with-camera mode, activate a character ability option, and clone-link the character to the crowd library. The *context menu* is always scoped to exactly one target character — the one that was right-clicked. All actions invoke existing execution paths (movement execution, camera rig, game bridge) on the specific target without requiring a prior selection step in the *roster panel*.

### context_menu

- appears at the right-click point in the *desktop overlay* when the GM right-clicks a *character overlay*; the target character's name is shown as the menu header
- is dismissed when the GM clicks outside the menu, selects an action, or presses Escape
- shows Spawn only when the target character's *spawned state* is false; Spawn is hidden when the character is already spawned
- shows Place at Location, Save Position, Move Camera to Target, Move Target to Camera, Reset Orientation, Maneuver with Camera, Activate Option, and Clone-Link when the target character is spawned
- applies all selected actions exclusively to the right-clicked target character regardless of any current selection in the *roster panel* or *desktop overlay*
- **Invariant:** the *context menu* is always scoped to exactly one target character; no action applies to a different character than the one right-clicked

### saved_character_position

- is the X/Y/Z world-space coordinate stored for a *roster entry* when the GM triggers Save Position from the *context menu*
- is written by reading the *character position* from the *memory interface* at the moment of the Save Position action and persisting it to the *roster entry*
- may be used by subsequent Place at Location or equivalent commands to return the character to the saved location
- **Invariant:** a *saved character position* is only written when the target character's *spawned state* is true; the Save Position action is not available for unspawned characters

### Decisions made

- `context menu` is a concept: distinct identity (the targeted popup), state (open/closed, scoped target), behavior (conditional action display, invokes movement/camera/bridge on target), invariants (always exactly one target); its own stories across several context-menu sub-epics
- `saved character position` is a concept: distinct identity (persisted coordinate), behavior (written from memory interface on demand, available for future restore), invariants (only when spawned); its own story (Save Character Position)
- Scope: Move Camera to Target, Move Target to Camera, and Maneuver with Camera delegate to Camera Rig (Increment 4); Reset Orientation delegates to Movement Execution (Increment 4); Clone-Link delegates to Crowd Repository (Increment 1); all remain boundary; the context menu is the routing surface

### References

**Ref — thin-slicing.md (Increment 5: context menu stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 226–237
Extract: Spawn Character via Context Menu, Place Character at Location, Save Character Position, Move Camera to Target Character, Move Target Character to Camera, Reset Character Orientation via Context Menu, Maneuver Character with Camera via Context Menu, Activate Character Option via Context Menu, Clone and Link Character from Desktop

**Ref — initial-ia.md (desktop — context menu)**
Source: docs/ux/initial-ia.md
Locator: lines 335–337
Extract: context menu — target character; actions: spawn · place at location · save-position · move-camera-to-target · move-target-to-camera · reset-orientation · activate-option · clone-link · maneuver-with-camera

---

## Pop-Up Menu

*Pop-Up Menu* is the COH-native menu definition subsystem that injects additional in-game action menus into the running game client. A *pop-up menu* is a text file written to the *COH menus directory* and then loaded into COH via a game command, making its entries accessible from the in-game HUD without restarting the client. The *area attack pop-up menu* is the specific pop-up deployed at session initialization to support area attack target designation. Writing and loading are always a two-step sequence — a write alone does not update what the game client sees, and a load before write targets a stale or missing file.

### pop_up_menu

- is a text file written to the *COH menus directory* by the application, defining a named set of in-game action entries accessible from the COH HUD
- is loaded into the running COH game client by issuing a load-pop-up-menu game command via the *game bridge* after the file has been written to disk
- is overwritten on each write operation; the most recently written version is the one active after the next load
- **Invariant:** a *pop-up menu* file must be written to the *COH menus directory* before a load command is issued; loading a non-existent file produces an error in the game client

### area_attack_pop_up_menu

- is the specific *pop-up menu* deployed at game session initialization to enable area-attack target designation in the COH client
- is deployed by writing the menu file to the *COH menus directory* and issuing the load command as part of the session initialization sequence
- must be present and loaded before the GM can designate an area attack center target from within the COH game HUD

### COH_menus_directory

- is the file-system subdirectory within the COH installation where *pop-up menu* definition files are placed and read by the game client
- is derived from the *COH game directory* confirmed at application startup
- **Invariant:** the *COH menus directory* must exist and be writable before any *pop-up menu* write can proceed; the *COH game directory* validation at startup ensures this precondition

### Decisions made

- `pop-up menu` is a concept: distinct identity (the menu file), state (written/not written, loaded/not loaded in client), behavior (write to disk, load into game, overwrite on update), invariants (write before load); its own stories (Write Pop-Up Menu Files, Load Pop-Up Menu in Game)
- `area attack pop-up menu` earns its own concept block: distinct deployment trigger (session initialization), distinct dependency from attack configuration; its own story (Deploy Area Attack Pop-Up Menu); not merely an instance of pop-up menu
- `COH menus directory` is a concept: distinct derivation and writable-check behavior, invariants ensuring it is ready before writes; analogous to *COH data directory* from Increment 1 but scoped to menus
- Scope-fit: pop-up menus are introduced here because the first concrete use (area attack designation) begins in this increment; the write/load infrastructure is new

### References

**Ref — thin-slicing.md (Increment 5: pop-up menu stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 209–212
Extract: Write Pop-Up Menu Files to COH Menus Directory, Load Pop-Up Menu in Game, Deploy Area Attack Pop-Up Menu

**Ref — story-map.md (Manage Pop-Up Menus)**
Source: docs/stories/story-map.md
Locator: lines 264–266
Extract: Write Pop-Up Menu Files to COH Menus Directory, Load Pop-Up Menu in Game

---

## Game State Query

*Game State Query* is the collection of real-time observation and execution infrastructure operations added to the *game bridge* in this increment. Three query operations observe the live COH game via the HookCostume DLL: reading *hovered NPC info* when the GM mouses over an NPC in the game viewport, reading *mouse XYZ position* for placement and spatial commands, and checking *game done state* to detect session end. Two infrastructure operations complete the extension: splitting *oversized command chains* into valid sub-chains before delivery, and closing the *game bridge* cleanly when the application shuts down. The *execute load map command* is also introduced here — the game command that transitions the COH client to a designated map, enabling the GM to stage a specific game environment.

### game_state_query

- provides three live observation operations against the running COH game client: *hovered NPC info*, *mouse XYZ position*, and *game done state*
- each operation is executed on demand by calling the HookCostume DLL via the *game bridge*; results are returned synchronously to the calling service
- reports an unavailable signal when the *game bridge* is not initialized or the COH game client is not running

### hovered_NPC_info

- is the NPC name and identity data returned by the *game state query* when the GM's mouse pointer hovers over a visible NPC entity in the COH game viewport
- is read on demand to identify which in-game character the GM is pointing at, enabling roster sync and selection feedback
- is absent (empty) when the mouse is not hovering over any NPC entity in the game viewport

### mouse_XYZ_position

- is the three-dimensional world-space coordinate triple returned by the *game state query* representing the point in the COH game world where the GM's mouse cursor is aimed
- is read on demand before Place at Location and equivalent placement operations, providing the destination world-space coordinate
- **Invariant:** the *mouse XYZ position* is only meaningful when the COH game window has input focus; queries without focus may return stale or zero coordinates

### game_done_state

- is the Boolean flag returned by the *game state query* indicating whether the COH game session has ended (map unload, disconnect, or client shutdown)
- is polled periodically; when true, all *roster entries* have their *spawned state* set to false, all *character overlays* are removed from the *desktop overlay*, and no further game commands are issued
- **Invariant:** once *game done state* becomes true, no spawn, move, or game command may be issued until a new game session is established

### command_chain

- is the ordered sequence of game commands assembled by the application for delivery to the *game bridge* as a single execution batch
- is delivered via the *game bridge* slash-command mechanism; the COH engine processes each command in the stated order
- is inspected before delivery to detect whether it constitutes an *oversized command chain* requiring splitting

### oversized_command_chain

- is a *command chain* whose total payload or command count exceeds the COH engine's per-execution limit, which would cause the engine to reject or truncate the batch
- is detected before delivery by measuring command count and payload size against the known COH limit
- is handled by splitting into two or more sub-chains, each within the limit, which are then delivered to the *game bridge* in sequence with no commands omitted
- **Invariant:** no *oversized command chain* may be delivered to the *game bridge* whole; all sub-chains must be delivered in order before the next application operation proceeds

### Decisions made

- `game state query` earns its own KA: it introduces three new game-engine observation channels (hovered NPC, mouse XYZ, game done) not achievable via the memory interface; each has a distinct story and distinct application reaction
- `hovered NPC info` is a concept: distinct identity (the observed entity under cursor), state (present/absent), behavior (returned on demand from DLL), distinct story (Query Hovered NPC Info from Game)
- `mouse XYZ position` is a concept: distinct identity (cursor-to-world coordinate), behavior (queried on demand for placement), invariants (focus-dependent validity), distinct story (Query Mouse XYZ Position in Game World)
- `game done state` is a concept: distinct state (false/true), behavior (polled, cascades to spawned state and overlay), invariants (blocks all game commands), distinct story (Check Game Done State)
- `command chain` is a concept: distinct identity (ordered batch), behavior (assembled, inspected, delivered, split if oversized), interactions with game bridge; distinct story (Split Oversized Command Chains for Execution)
- `oversized command chain` earns its own block: distinct detection behavior and split-and-deliver response; analogous to *stale memory pointer* from Increment 4 (problematic state with a defined recovery action)
- `execute load map command` and `close game bridge on shutdown` are new behaviors of the *game bridge* (boundary, Increment 2); they are recorded in the Game Bridge boundary entry below rather than promoted to concepts here, because the game bridge infrastructure is already the owning KA

### References

**Ref — thin-slicing.md (Increment 5: game engine stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 204–213
Extract: Query Hovered NPC Info from Game, Query Mouse XYZ Position in Game World, Check Game Done State, Split Oversized Command Chains for Execution, Close Game Bridge on Shutdown, Execute Load Map Command

**Ref — story-map.md (Communicate with Game Engine)**
Source: docs/stories/story-map.md
Locator: lines 218–234
Extract: Query Hovered NPC Info from Game, Query Mouse XYZ Position in Game World, Check Game Done State, Close Game Bridge on Shutdown, Execute Load Map Command, Split Oversized Command Chains for Execution

---

# Boundary Domain

## Character

Owned by: Character and Crowd Library (Increment 1)

- is the data entity added to the *roster* by name; each *roster entry* names the character it represents and inherits the character's name for display and game command targeting
- receives a clone-linked copy when the GM invokes Clone-Link from the *context menu*; the new linked character is added to the target *crowd* in the crowd library and a new *roster entry* is created for it

### Decisions made

- *character* is a boundary concept: lifecycle and CRUD are owned by Increment 1; this increment depends on *character* as the source of *roster entry* names and as the target of clone-link operations

### References

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

---

## Crowd

Owned by: Character and Crowd Library (Increment 1)

- provides the group of *characters* expanded into individual *roster entries* when the GM adds a crowd to the *roster*
- receives a clone-linked *character* when the GM invokes Clone-Link from the *context menu* on a desktop character

### Decisions made

- *crowd* is a boundary concept: its structure, membership, and persistence are owned by Increment 1; this increment uses *crowd* as a batch source for roster population and as the target for clone-link additions

### References

**Ref — ubiquitous-language-increment-1.md (Crowd KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Crowd KA section

---

## Spawned NPC

Owned by: Character Identities (Increment 2)

- is created for each *roster entry* when the GM spawns a character, setting the entry's *spawned state* to true and rendering the NPC in the COH game world
- is the in-game entity the *desktop overlay* represents as a *character overlay*; its world-space position drives the overlay's position
- is removed when the GM clears the character from the desktop, when the character is removed from the *roster* while spawned, or when *game done state* becomes true

### Decisions made

- *spawned NPC* is a boundary concept: spawn and despawn lifecycle are owned by Increment 2; this increment depends on the *spawned NPC* as the in-game representation of each *roster entry* with *spawned state* true

### References

**Ref — ubiquitous-language-increment-2.md (Identity KA — spawned NPC)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA — spawned NPC section

---

## Game Bridge

Owned by: Character Identities (Increment 2)

- executes spawn, despawn, place, and load-map commands as slash commands for roster and context-menu actions
- writes *pop-up menu* files and issues load-pop-up-menu commands for the *pop-up menu* subsystem
- executes *game state query* operations by calling the HookCostume DLL for *hovered NPC info*, *mouse XYZ position*, and *game done state*
- delivers *command chains* (and split *oversized command chains*) to the COH engine in correct sub-chain sequence
- is closed cleanly on application shutdown, releasing DLL handles and terminating all active game connections
- executes the load-map command to transition the COH client to a designated game map

### Decisions made

- *game bridge* is a boundary concept: core initialization and slash-command routing are owned by Increment 2; this increment extends the routing with pop-up menu write/load, game state queries, command chain splitting, clean shutdown, and load-map command

### References

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

---

## Memory Interface

Owned by: Single Character Movement (Increment 4)

- provides the *character position* coordinate read when the GM triggers Save Position from the *context menu*, which is stored as the *saved character position*
- monitors the *current target* register to detect game-side target changes that drive *roster* and *desktop overlay* selection synchronization

### Decisions made

- *memory interface* is a boundary concept: attach, pointer resolution, and polling logic are owned by Increment 4; this increment reads *character position* for save-position and monitors *current target* for roster sync

### References

**Ref — ubiquitous-language-increment-4.md (Memory Interface KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Memory Interface KA section

---

## Camera Rig

Owned by: Single Character Movement (Increment 4)

- is invoked by Move Camera to Target Character and Move Target Character to Camera actions from the *context menu*
- is engaged by the Maneuver with Camera action in the *context menu*, activating maneuver-with-camera mode on the target character

### Decisions made

- *camera rig* is a boundary concept: deployment, follow, detach, and position reading are owned by Increment 4; this increment surfaces camera rig operations through the *context menu* without modifying the rig's own behavior

### References

**Ref — ubiquitous-language-increment-4.md (Camera Rig KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Camera Rig KA section

---

## Movement Execution

Owned by: Single Character Movement (Increment 4)

- is invoked when the GM drags a *character overlay* to a new position; the drop coordinates become the movement destination for the move NPC command
- is invoked by Place Character at Location from the *context menu*, with the *mouse XYZ position* or specified coordinates as the destination
- is invoked by Reset Character Orientation from the *context menu*

### Decisions made

- *movement execution* is a boundary concept: command issuance, distance tracking, and collision checking are owned by Increment 4; this increment triggers movement execution via drag and context-menu actions

### References

**Ref — ubiquitous-language-increment-4.md (Movement Execution KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Movement Execution KA section
