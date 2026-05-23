---
state: ubiquitous-language
increment: 4
scope: Single Character Movement
date: 2026-05-17
---

# Ubiquitous Language — Increment 4: Single Character Movement

> Scope: the vocabulary needed to author *character movements* (Walk/Run/Swim/Fly/Jump), read and write game-side position, rotation, and camera data from process memory, execute move and follow commands on spawned NPCs, animate locomotion, enforce distance limits, detect floor and wall collisions, manage a camera rig for scene navigation, and activate maneuver-with-camera mode for camera-directed movement. Builds on Increment 1 (Character, Crowd Repository), Increment 2 (Spawned NPC, Game Bridge, KeyBind), and Increment 3 (Keyboard Hook). Crowd movement is deferred to Increment 6.

---

**Terms**:
- **Character Movement**
  - **character movement** — a named movement configuration the GM assigns to a character; defined by *movement type*, *movement parameters*, a *movement activation key*, a *distance limit*, and a default flag; lives in the character's Movements *option group*
  - **movement type** — the locomotion category of a *character movement*; one of Walk, Run, Swim, Fly, or Jump; determines which *movement animation* plays; whether the character is ground-tethered is determined by the *levitate* property (derived from movement type)
  - **movement parameters** — the configurable values that govern how a *character movement* executes (step interval, speed factor, approach behavior)
  - **movement activation key** — the keyboard key the GM assigns to trigger a *character movement*; read by the *keyboard hook* to dispatch movement on the active character
  - **default movement** — the *character movement* applied automatically when a character begins moving without an explicit selection; at most one per character carries this designation
  - **distance limit** — the maximum in-game distance the character may travel per activation of a *character movement*; enforced by *movement execution*; absent or zero means no limit
  - **movement distance count** — the running tally of in-game distance traveled during the current *movement execution*; reset at each new activation; compared against the *distance limit* to halt movement
- **Memory Interface**
  - **memory interface** — the service that attaches to the running *game process* and reads or writes live COH game-state values directly from process memory
  - **character position** — the X/Y/Z world-space coordinates of a character's location in the COH game world, stored in and written to process memory
  - **character model matrix** — the 4×4 world-space transform matrix in process memory encoding the character's full position, rotation, and scale; read before turning and orientation operations
  - **character rotation matrix** — the orientation subcomponent of the character's spatial transform; written to process memory to change the direction the *spawned NPC* faces
  - **character facing vector** — the unit direction vector in process memory pointing in the direction the character currently faces; read to compute turns and camera-relative movement steps
  - **camera position** — the X/Y/Z world-space coordinates of the COH game camera in process memory; the destination reference for camera-relative movement commands
  - **memory pointer** — a cached process-memory address resolved by the *memory interface* that identifies a specific game-state value; must be re-resolved if the *game process* restarts or reallocates memory
  - **stale memory pointer** — a *memory pointer* whose cached address no longer refers to the expected game-state value; detected by a periodic scan and refreshed before the next read or write
  - **game process** — the running COH client process that the *memory interface* must detect and attach to before any memory operation can proceed
  - **current target** — the game entity identifier in the COH targeting register; monitored by the *memory interface* to determine which *spawned NPC* the GM intends to act on
  - **target registration** — the confirmation that a newly spawned NPC's name is resolvable in the COH targeting system and its *memory pointer* addresses are valid; movement commands are blocked until confirmed
- **Movement Execution**
  - **movement execution** — the service that applies *character movements* to *spawned NPCs* by issuing *move NPC commands*, enforcing *distance limits*, checking collisions, and playing *movement animations*
  - **move NPC command** — the game command delivered via the *native game bridge* that repositions a *spawned NPC* to a specified world-space location
  - **movement animation** — the game-side locomotion animation played on the *spawned NPC* during *movement execution*; selected by the active *movement type* (Walk/Run/Swim/Fly/Jump)
  - **floor collision** — the runtime check that detects whether a movement step's path intersects a floor surface; stops vertical descent and anchors the character at the contact point
  - **wall collision** — the runtime check that detects whether a movement step's path intersects a wall or obstacle; halts movement in the blocked direction without error
- **Camera Rig**
  - **camera rig** — the virtual camera system rendered in the COH game world that the GM uses to navigate the scene; the source of the *camera position* for camera-relative movement commands
  - **camera follow** — the mode in which the *camera rig* continuously tracks the *character position* of the targeted *spawned NPC*, moving with the character
  - **maneuver-with-camera mode** — the movement input mode in which *movement execution* drives the character in the *camera rig's* current facing direction rather than toward a fixed world-space destination
  - **camera detach** — the operation that disconnects the *camera rig* from any followed *spawned NPC* and returns it to free-roam mode
  - **camera enable/disable script** — a COH script file deployed to the game session by the *game bridge* that activates or deactivates the *camera rig*

---

The Single Character Movement increment is the first in which HVT moves characters through the live COH game world. Before any movement can execute, the *memory interface* must detect the running *game process* and resolve all *memory pointers* for the active character — *character position*, *character model matrix*, *character rotation matrix*, *character facing vector*, and *camera position*. A periodic scan checks for *stale memory pointers* and refreshes them before any read or write. The *memory interface* also monitors the *current target* to confirm which character the GM intends to move, and waits for *target registration* after a spawn before allowing movement commands to proceed.

Each *character* in the crowd manager's Movements tab holds an ordered list of *character movements* in its Movements *option group*. A *character movement* carries a name, a *movement type* (Walk, Run, Swim, Fly, or Jump), configurable *movement parameters*, a *movement activation key*, a *distance limit*, and a default flag. The GM authors movements in the *crowd manager — movements* screen, edits their *movement parameters* in the *movement editor*, and designates one as the *default movement*. New characters receive Walk, Run, and Swim *character movements* via the Add Default Movements operation.

When the GM triggers movement, *movement execution* issues the *move NPC command* against the targeted *spawned NPC*, computing the destination from the chosen mode: a fixed location, the *camera position*, or a camera-directed bearing in *maneuver-with-camera mode*. As movement progresses, the service tracks the *movement distance count* and halts when the *distance limit* is reached. *Floor collision* and *wall collision* are checked before each step; a blocked path stops movement cleanly. The *spawned NPC* plays a *movement animation* matched to the active *movement type*.

The *camera rig* is the GM's scene-navigation tool and the prerequisite for all camera-relative movement. Deployed via *camera enable/disable scripts*, the rig is rendered as a controllable camera object in the game world. When *camera follow* is active, the rig tracks the targeted *spawned NPC* continuously. When *maneuver-with-camera mode* is on, character movement follows the camera's facing direction. *Camera detach* breaks the follow link and returns the camera to free-roam.

---

# Core Domain

## Character Movement

*Character Movement* is the named locomotion configuration the GM authors on a character and triggers by pressing the assigned *movement activation key* or via menu. Each *character movement* lives in the character's Movements *option group* and defines how the character physically traverses the COH game world: which locomotion category (*movement type*) it uses, how its execution is governed (*movement parameters*), how far the character may travel in one activation (*distance limit*), and whether it fires automatically when the character begins moving (*default movement*). The GM manages *character movements* in the *crowd manager — movements* screen and edits their configuration in the *movement editor*. New characters receive Walk, Run, and Swim *character movements* via the Add Default Movements operation. Every *character movement* must declare a *movement type*; at most one *character movement* per character may carry the default flag; at most one may hold a given *movement activation key*.

### character_movement

- is created in a *character's* Movements *option group* by the GM supplying a name in the movement list; the name must be unique within that character's Movements *option group*
- is edited in the *movement editor*, where the GM changes its *movement type*, *movement parameters*, *movement activation key*, *distance limit*, and default flag; changes apply on save and are discarded on cancel
- is removed from a *character* by the GM; removal clears the *movement activation key* binding for that movement
- is played on a *spawned NPC* by issuing *movement execution* commands in the configured *movement type*, animating the character with the matching *movement animation*
- is dispatched by the *keyboard hook* when the pressed key matches the *movement activation key* on the active character
- **Invariant:** a *character movement's* name must be unique within the character's Movements *option group* at all times
- **Invariant:** at most one *character movement* per character may carry the default flag at any time; setting a new default clears the previous one
- **Invariant:** at most one *character movement* per character may hold a given *movement activation key*; assigning a key already in use on the same character must be rejected

### movement_type

- is a type property of *character movement* — one of Walk, Run, Swim, Fly, or Jump; set via the type dropdown in the *movement editor*
- determines which *movement animation* the *spawned NPC* plays during *movement execution*; it also determines the *levitate* value (Walk and Run → `false`; Swim, Fly, Jump → `true`)
- **Invariant:** every *character movement* must have a *movement type*; a movement with no type assigned cannot be saved

### levitate

- is a boolean property of *character movement* — the single execution difference between movement types
- is `true` for Swim, Fly, and Jump: the character is not ground-tethered; vertical displacement is permitted and floor collision detection does not apply
- is `false` for Walk and Run: the character stays on the floor; floor collision detection applies
- is derived from *movement type* and does not require separate GM configuration; the movement editor sets it automatically based on the selected type

### movement_parameters

- is a property of *character movement* — the configurable values (step interval, speed factor, approach behavior) that control how the movement executes
- is edited in the *movement editor* and saved with the *character movement*

### movement_activation_key

- is a property of *character movement* — the keyboard key (e.g., F1, Numpad1) the GM assigns to trigger execution of the movement
- is set via the set-key action in the movement list or via the activation key field in the *movement editor*
- is read by the *keyboard hook* when routing key events; a press matching this value dispatches the owning *character movement* on the active character
- **Invariant:** at most one *character movement* per character may hold a given *movement activation key*; assigning a key already in use on the same character must be rejected

### default_movement

- is a property of *character movement* — the boolean flag marking the movement applied automatically when the character begins moving without an explicit *movement activation key* press
- is set via the set-default action in the movement list; displayed with a default marker in the movement list
- **Invariant:** at most one *character movement* per character may carry the default flag; clearing the flag leaves no default without error

### distance_limit

- is a property of *character movement* — the maximum in-game distance the character may travel during a single activation
- is configured in the *movement editor*; *movement execution* halts and shows limit-reached feedback to the GM when the *movement distance count* reaches this value
- **Invariant:** a *distance limit* of zero or absent means no limit is enforced; movement continues until the GM stops it or a collision occurs

### movement_distance_count

- tracks the cumulative in-game distance traveled by the character during the current *movement execution* session
- is reset to zero at the start of each new *character movement* activation
- is compared against the *distance limit* on each movement step; when equal to or exceeding the limit, *movement execution* halts and the GM sees limit-reached feedback
- **Invariant:** the *movement distance count* never causes the character to travel beyond the *distance limit*; the final step is clamped to land at or before the limit, not past it

### Decisions made

- `character movement` is a concept: distinct identity (named, unique within the character's Movements option group), state (movement type, parameters, activation key, distance limit, default flag), behavior (created, edited, removed, played, dispatched by keyboard hook), and invariants; the central authored object of this increment
- `movement type` is a type property, not a subtype: all five locomotion categories share the same execution pipeline; the type drives animation selection only — a data distinction, not a behavioral one
- `levitate` is a boolean property that captures the single execution difference between movement types: Walk and Run have `levitate = false` (ground-tethered); Swim, Fly, and Jump have `levitate = true` (not ground-tethered, vertical displacement permitted, floor collision skipped); no arc physics is modeled
- `movement parameters`, `movement activation key`, `default movement`, `distance limit` are properties of *character movement*, documented as stub headings because their invariants are directly testable
- `movement distance count` earns a concept block: distinct behavior (track, compare against limit, halt, reset), its own story (Track Movement Distance Count), testable independently of the parent *character movement*
- Scope-fit: crowd movement is deferred to Increment 6; all terms pass the single-character movement scope

### References

**Ref — thin-slicing.md (Increment 4: character movement authoring stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 175–181
Extract: Add Movement to Character, Edit Movement Parameters, Remove Movement from Character, Set Default Movement, Set Movement Activation Key, Add Default Movements to Character (Walk, Run, Swim)

**Ref — initial-ia.md (crowd manager — movements)**
Source: docs/ux/initial-ia.md
Locator: lines 153–191
Extract: movement list — name · activation key · default · type; actions: add · remove · set-default · set-key · edit

**Ref — initial-ia.md (movement editor)**
Source: docs/ux/initial-ia.md
Locator: lines 194–223
Extract: movement config form — name · activation key · default · type; save · cancel

---

## Memory Interface

*Memory Interface* is the service that attaches to the running *game process* and reads or writes live COH game-state data directly from process memory. It is the data layer that makes all movement operations possible: before any *movement execution* command can fire, the *memory interface* must supply the current *character position*, *character model matrix*, *character rotation matrix*, *character facing vector*, and *camera position*. It resolves and caches the *memory pointers* for these values and runs a periodic scan to detect and refresh any *stale memory pointers*. The *memory interface* monitors the *current target* to confirm which *spawned NPC* the GM intends to move, and waits for *target registration* before allowing movement commands to proceed against any newly spawned character.

### memory_interface

- attaches to the *game process* at session start, resolving all required *memory pointers* before any read or write operation proceeds
- reads *character position*, *character model matrix*, *character facing vector*, and *camera position* from process memory on demand during *movement execution*
- writes *character position* and *character rotation matrix* to process memory to apply position and orientation changes issued by *movement execution*
- monitors the *current target* by reading the targeting register in process memory; notifies dependent services when the targeted character changes
- runs a periodic *stale memory pointer* scan that checks each cached *memory pointer* for validity and re-resolves any that have gone stale before the next read or write
- waits for *target registration* after a character spawn before exposing the new *spawned NPC's* memory addresses to *movement execution*
- **Invariant:** no memory read or write may proceed until the *game process* is detected and all required *memory pointers* are resolved; operations attempted before attachment must be rejected or queued

### memory_pointer

- is a cached process-memory address resolved by the *memory interface* that identifies a specific game-state value (position, matrix, camera coordinate, current target)
- is resolved at session start by scanning known address patterns in the *game process* memory layout
- is validated before each read or write; a pointer is considered stale when its address no longer contains valid game-state data matching expected patterns
- is refreshed by the *stale memory pointer* scan cycle when declared stale, restoring the correct address before the next operation
- **Invariant:** a *memory pointer* must pass validation before any read or write; a stale or unresolved pointer must not be used to write position or rotation data to the *game process*

### stale_memory_pointer

- is a *memory pointer* whose cached address no longer refers to the expected game-state value, typically caused by the *game process* restarting or reallocating memory
- is detected by the *memory interface* during its periodic scan by reading the cached address and comparing the resident value against expected game-state patterns
- triggers a refresh cycle when detected: the *memory interface* re-resolves the pointer address from the known pattern before the next read or write proceeds
- **Invariant:** the *memory interface* must not proceed with a read or write against a known *stale memory pointer*; the re-resolution must complete before the operation is retried

### game_process

- is the running COH client process that the *memory interface* must detect and attach to before any memory operation can proceed
- is detected by scanning running OS processes for the known COH executable name and matching window handle; provides the process handle and memory base address for *memory pointer* resolution
- **Invariant:** the *memory interface* is inoperable until the *game process* is detected; all memory reads and writes are blocked while the process handle is absent or invalid

### character_position

- is a property of *memory interface* context — the X/Y/Z world-space coordinate triple stored in process memory that records where the character's model origin is placed in the COH game world
- is read by the *memory interface* before any movement step to determine the character's starting location
- is written by the *memory interface* when *movement execution* computes a new destination, repositioning the *spawned NPC* in the game world

### character_model_matrix

- is a property of *memory interface* context — the 4×4 world-space transform matrix read from process memory encoding the character's full position, rotation, and scale
- is read before turning and orientation-reset operations to obtain the character's full spatial state

### character_rotation_matrix

- is a property of *memory interface* context — the orientation subcomponent of the character's spatial transform
- is computed by *movement execution* from the desired facing direction and written to process memory after any turn or facing-direction change

### character_facing_vector

- is a property of *memory interface* context — the unit direction vector in process memory pointing in the direction the character currently faces
- is read before computing a turn toward a target or a camera-relative movement step

### camera_position

- is a property of *memory interface* context — the X/Y/Z world-space coordinate triple in process memory recording where the COH game camera is placed
- is read on demand when the GM triggers "move to camera position" or "teleport to camera" commands; is the destination or reference bearing for all camera-relative *movement execution* commands

### current_target

- is the game entity identifier stored in the COH targeting register in process memory, identifying which *spawned NPC* is currently selected in the game client
- is monitored continuously by the *memory interface* and surfaced to the application as the active character for movement operations
- changes when the GM selects a different character in the game world; the *memory interface* notifies movement services of the change

### target_registration

- is the confirmation that a newly spawned NPC's name is resolvable in the COH targeting system and its *memory pointer* addresses are valid
- is awaited by the *memory interface* after a spawn command completes; movement commands are blocked until *target registration* succeeds
- is detected by polling the COH targeting system until the NPC's name resolves correctly and the associated *memory pointer* addresses return valid game-state data

### Decisions made

- `memory interface` is a concept: distinct identity (the attaching service), state (unattached/attached/ready), behavior (detect process, resolve pointers, read/write position/matrix, monitor target, scan for stale pointers), invariants (no operations before attachment); the entire low-level data layer for this increment
- `memory pointer` is a concept: distinct identity (resolved address), state (valid/stale), behavior (resolved, validated before use, refreshed), invariants (must pass validation before read/write); not merely a language-level address value
- `stale memory pointer` earns a concept block: distinct identity (pointer in invalid state), behavior (detected by periodic scan, triggers refresh cycle, blocks read/write while stale), its own story (Scan and Fix Stale Memory Pointers), testable independently
- `game process` is a concept: distinct identity (running OS process), state (running/not running, attached/detached), behavior (detected, provides base address), invariants (all operations blocked without it); its own story (Detect Game Process for Connection)
- `character position`, `character model matrix`, `character rotation matrix`, `character facing vector`, `camera position` are properties of *memory interface* context — documented as stub headings because their read/write behaviors are individually storied and testable
- `current target` is a concept: distinct behavior (monitored continuously, notifies on change), its own story (Monitor Current Target in Game), state (currently targeted entity identifier)
- `target registration` is a concept: distinct behavior (polled after spawn, blocks movement until confirmed), its own story (Wait until Target is Registered after Spawn), testable independently of other memory reads

### References

**Ref — thin-slicing.md (Increment 4: memory read/write and process stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 158–174
Extract: Read Target Character from Memory, Read Character Position (X, Y, Z) from Memory, Write Character Position to Memory, Read Character Model Matrix from Memory, Write Character Rotation Matrix to Memory, Read Character Facing Vector from Memory, Write Character Facing Direction to Memory, Read Camera Position from Memory, Scan and Fix Stale Memory Pointers, Detect Game Process for Connection, Monitor Current Target in Game, Wait until Target is Registered after Spawn

---

## Movement Execution

*Movement Execution* is the service that applies *character movements* to *spawned NPCs* in the COH game world by issuing *move NPC commands*, enforcing *distance limits*, checking *floor collision* and *wall collision*, and playing the correct *movement animation*. It consumes *character position* and *camera position* from the *memory interface* to compute movement destinations. Movement modes include: move to a fixed location, move to the current *camera position*, teleport to the camera (single-step position write), and follow a camera bearing in *maneuver-with-camera mode*. Turning operations change the character's facing by writing a new *character rotation matrix* without repositioning the character. *Movement execution* requires *target registration* before issuing any command and halts cleanly when the *distance limit* is reached or a collision blocks the path.

### movement_execution

- computes the movement destination from the active mode: fixed world-space location supplied by the GM, *camera position* read from the *memory interface*, or camera-direction bearing in *maneuver-with-camera mode*
- issues the *move NPC command* against the targeted *spawned NPC* for each movement step, incrementing the *movement distance count* after each successful step
- checks *floor collision* and *wall collision* before committing each movement step; a detected collision halts movement in the blocked direction without error, leaving movement in unblocked directions unaffected
- halts and displays limit-reached feedback to the GM when the *movement distance count* reaches the *distance limit*
- plays the *movement animation* matched to the active *movement type* on the *spawned NPC* while movement is in progress, stopping animation when movement halts
- resets the *movement distance count* to zero at the start of each new *character movement* activation
- turns the *spawned NPC* to face a target position by computing the required *character rotation matrix* and writing it via the *memory interface*
- resets character orientation by writing the identity-equivalent *character rotation matrix* to process memory via the *memory interface*
- waits for *target registration* before issuing any movement command against a newly spawned character
- **Invariant:** *movement execution* must not issue any game command against a *spawned NPC* before *target registration* is confirmed
- **Invariant:** the *movement distance count* must never cause the character to exceed the *distance limit*; the last step is clamped so the character stops at or before the limit

### move_NPC_command

- is the game command issued by *movement execution* that repositions a *spawned NPC* to a specified world-space location
- is delivered via the *native game bridge* as a *slash command*; the COH engine applies the move immediately on receipt
- **Invariant:** a *move NPC command* must target a registered *spawned NPC* by name; targeting an unregistered NPC produces a no-op in the game engine

### movement_animation

- is the game-side locomotion animation played on the *spawned NPC* during *movement execution*
- is selected based on the active *movement type*: Walk plays the walk cycle, Run the run cycle, Swim the swim cycle, Fly the fly cycle, Jump the jump arc animation
- is started when *movement execution* begins its step loop and stopped when movement halts or the *distance limit* is reached
- **Invariant:** the animation played must match the *movement type* of the active *character movement*; mismatched animation is an error

### floor_collision

- is the runtime detection performed by *movement execution* before each movement step to determine whether the step's path intersects a floor surface
- stops vertical descent and anchors the character to the floor at the collision point when detected; the character does not pass through floor geometry

### wall_collision

- is the runtime detection performed by *movement execution* before each movement step to determine whether the step's path intersects a wall or obstacle surface
- halts movement in the direction of the wall when detected; the character stops at the wall boundary without error

### Decisions made

- `movement execution` is a concept: distinct behavior (compute destination, issue move command, track distance, check collisions, play animation, turn, reset orientation), state (active/halted, distance count), interactions with memory interface, move NPC command, spawned NPC, camera rig; the execution engine of this increment
- `move NPC command` is a concept: distinct identity (game command with target NPC name and destination position), behavior (delivered via native game bridge), invariants (target must be registered); analogous in structure to *spawn NPC command* and *target by name command* from Increment 2
- `movement animation` is a concept: distinct behavior (type-driven selection, started/stopped with movement), invariants (must match movement type), its own story (Animate Walk/Run/Swim/Fly/Jump Movement), testable independently
- `floor collision` and `wall collision` are concepts: each has distinct detection behavior and a distinct response (anchor vs. halt in direction); their own story (Detect Floor and Wall Collisions), testable independently of move success
- Teleport to camera is a degenerate move-to-camera-position: the *character position* is set in one memory write directly to the *camera position* rather than a step-by-step loop; animation and distance tracking are bypassed; no separate concept needed

### References

**Ref — thin-slicing.md (Increment 4: movement execution stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 165–192
Extract: Execute Move NPC Command, Move Character to Location, Move Character to Camera Position, Teleport Character to Camera, Animate Walk/Run/Swim/Fly/Jump Movement, Track Movement Distance Count, Enforce Distance Limit per Movement Type, Detect Floor and Wall Collisions, Turn Character towards Target, Reset Character Orientation

---

## Camera Rig

*Camera Rig* is the virtual camera system rendered in the COH game world that the GM uses to navigate the scene during a session. The rig is armed by deploying a *camera enable/disable script* and rendered as a controllable camera object in the game world. The *camera rig* provides the *camera position* consumed by the *memory interface* for all camera-relative movement commands. When *camera follow* is active, the rig tracks the targeted *spawned NPC* continuously. When *maneuver-with-camera mode* is on, character movement is directed by the camera's current facing rather than a fixed destination. *Camera detach* releases the follow link and returns the camera to free-roam. The *camera rig* is the prerequisite for all camera-relative movement modes; none can succeed unless the rig is rendered in game.

### camera_rig

- is activated by deploying the *camera enable/disable script* (enable variant) to the running game session; the rig appears as a visible camera object in the COH game world
- is deactivated by deploying the *camera enable/disable script* (disable variant), which removes the camera object from the game world and terminates any active *camera follow*
- provides the *camera position* that the *memory interface* reads for camera-relative movement destination calculations
- tracks the targeted *spawned NPC* when *camera follow* is active, updating the camera's world position to match the character's *character position* on each update cycle
- releases the follow link when *camera detach* is issued, returning to free-roam mode
- **Invariant:** the *camera rig* must be rendered in game before any camera-relative movement command (*camera follow*, *maneuver-with-camera mode*, move to camera position, teleport to camera) can succeed

### camera_follow

- is the mode in which the *camera rig* continuously tracks the *character position* of the targeted *spawned NPC*
- is activated by the GM via the Follow action on the targeted character; the *camera rig* moves to match the character's location on each update
- is deactivated by the GM via the Unfollow action or when *camera detach* is issued
- **Invariant:** *camera follow* may only be active on one character at a time; activating follow on a second character automatically unfollows the first

### maneuver_with_camera_mode

- is the movement input mode in which *movement execution* computes the movement destination using the *camera rig's* current facing direction rather than a fixed world-space target
- is activated by the GM via the Maneuver-with-Camera action; subsequent movement commands drive the character in the direction the camera faces until the mode is deactivated
- is deactivated by the GM via the same action toggle or by issuing Unfollow

### camera_detach

- is the operation that disconnects the *camera rig* from any currently followed *spawned NPC* and returns the camera to free-roam mode
- is issued by the GM explicitly via the Camera Detach action, or triggered automatically when the followed character is despawned
- executes the *execute camera detach command* via the *native game bridge* to instruct COH to release the camera lock

### camera_enable_disable_script

- is a COH script file deployed to the game session by the *game bridge* that arms or disarms the *camera rig*
- the enable variant renders the camera rig object in the COH game world; the disable variant removes it and terminates any active *camera follow*
- must be deployed before any *camera rig* operation can succeed; deploying the disable variant while *camera follow* is active also terminates the follow mode

### Decisions made

- `camera rig` is a concept: distinct identity (rendered game object), state (inactive/active, follow mode on/off), behavior (activated by script, provides camera position, follows character, releases on detach), invariants (must be rendered before camera-relative operations); its own story (Render Camera Rig in Game)
- `camera follow` is a concept (not a property): distinct state (active/inactive with a specific target character), behavior (continuously updates camera position to match character, activated and deactivated explicitly), invariant (only one character at a time), its own stories (Follow Character with Game Camera, Unfollow Character)
- `maneuver with camera mode` is a concept: distinct state (active/inactive), behavior (redirects movement destination computation to camera facing direction), its own story (Activate Maneuver-with-Camera Mode)
- `camera detach` earns a concept block: distinct operation behavior (disconnect follow link, return to free-roam, execute via game bridge), triggered both explicitly and by character despawn; its own story (Execute Camera Detach Command)
- `camera enable/disable script` is a concept: distinct identity (deployable script file), state (deployed/not deployed), behavior (enable renders rig, disable removes rig and terminates follow), its own story (Deploy Camera Enable and Disable Scripts)
- Scope-fit: the *camera rig* is introduced in this increment as the prerequisite for camera-relative movement; no camera system existed in prior increments

### References

**Ref — thin-slicing.md (Increment 4: camera rig stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 167–170
Extract: Execute Follow Command, Execute Camera Detach Command, Deploy Camera Enable and Disable Scripts, Render Camera Rig in Game

**Ref — thin-slicing.md (Increment 4: camera mode stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 189–192
Extract: Activate Maneuver-with-Camera Mode, Follow Character with Game Camera, Unfollow Character

**Ref — initial-ia.md (desktop — context menu: follow/unfollow/maneuver-with-camera)**
Source: docs/ux/initial-ia.md
Locator: lines 337, 543–543
Extract: context menu actions: follow · unfollow · maneuver-with-camera

---

# Boundary Domain

## Character

Owned by: Character and Crowd Library (Increment 1)

- holds a Movements *option group* that this increment populates with *character movement* entries; each *character movement* carries its own *movement type*, *movement parameters*, *movement activation key*, *distance limit*, and default flag
- provides the character name used as the targeted *spawned NPC* name in all *move NPC commands* and *camera follow* operations

### Decisions made

- *character* is a boundary concept: lifecycle, CRUD, and crowd membership are fully owned by Increment 1; this increment depends on *character* as the host for *character movements* and as the name source for movement game commands

### References

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

---

## Spawned NPC

Owned by: Character Identities (Increment 2)

- is the game-world entity that *movement execution* repositions via the *move NPC command*; all movement, turning, animation, and camera follow operations target the *spawned NPC* by the character's name
- must pass *target registration* before any *movement execution* command can proceed against it

### Decisions made

- *spawned NPC* is a boundary concept: its lifecycle (spawn, despawn, targeting) is fully owned by Increment 2; this increment depends on *spawned NPC* as the execution target for all movement commands

### References

**Ref — ubiquitous-language-increment-2.md (Identity KA — spawned NPC)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA — spawned NPC section

---

## Game Bridge

Owned by: Character Identities (Increment 2)

- executes the *move NPC command*, *execute follow command*, and *execute camera detach command* as *slash commands* via the *native game bridge*
- deploys *camera enable/disable scripts* to the game session using the same keybind and script delivery infrastructure established in Increment 2

### Decisions made

- *game bridge* is a boundary concept: initialization and slash-command routing are fully established in Increment 2; this increment adds *move NPC command*, follow, and camera detach commands plus script deployment to the existing routing pipeline

### References

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

---

## Keyboard Hook

Owned by: Animated Abilities (Increment 3)

- intercepts key presses matching a *character movement's* *movement activation key* on the active character and fires movement dispatch, using the same *key routing* logic established in Increment 3 for *animated ability* activation keys

### Decisions made

- *keyboard hook* is a boundary concept: installation and key-routing logic are owned by Increment 3; this increment extends the routing to dispatch *character movement* execution alongside *animated ability* dispatch

### References

**Ref — ubiquitous-language-increment-3.md (Keyboard Hook KA)**
Source: docs/domain/ubiquitous-language-increment-3.md
Locator: Keyboard Hook KA section

---

## KeyBind

Owned by: Character Identities (Increment 2)

- may be used to deliver *move NPC commands* and *camera rig* operations that require COH's keybind execution path rather than direct slash command execution via the *native game bridge*

### Decisions made

- *keybind* is a boundary concept: file generation and bind-load-file delivery pipeline are owned by Increment 2; this increment may use the same channel for movement-related game commands where the keybind path is required

### References

**Ref — ubiquitous-language-increment-2.md (KeyBind KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: KeyBind KA section

---

## Animated Ability

Owned by: Animated Abilities (Increment 3)

- the *movement element* subtype from Increment 3 applies a COH movement resource (a raw COH movement command identifier) rather than a *character movement* authored in Increment 4; the two are distinct: *animation elements* are ability composition units, *character movements* are standalone locomotion configurations authored on the character
- the *keyboard hook* dispatches both *animated ability* activation keys and *movement activation keys* using the same routing path; the two dispatch targets coexist without conflict

### Decisions made

- *animated ability* is a boundary concept: its full lifecycle belongs to Increment 3; this increment coexists with it on the same character (Abilities option group vs. Movements option group) and shares the keyboard hook dispatch infrastructure

### References

**Ref — ubiquitous-language-increment-3.md (Animated Ability KA)**
Source: docs/domain/ubiquitous-language-increment-3.md
Locator: Animated Ability KA section
