---
state: crc
increment: 4
scope: Single Character Movement
date: 2026-05-21
---

# CRC — Increment 4: Single Character Movement

> Domain sources: `docs/increment-4/ubiquitous-language-increment-4.md`, `docs/increment-4/acceptance-criteria-increment-4.md`.

---

# Core Domain

## **Character Movement**

The named locomotion configuration the GM authors on a character and triggers via keyboard or menu. Each character holds a Movement Option Group — a type-safe collection that owns the active/default selection invariants for movements.

### **Movement Option Group : Option Group**
active movement                       | Character Movement
                                      |   invariant: exactly one movement active at a time; starting a new movement stops the current
default movement                      | Character Movement
                                      |   invariant: at most one character movement may carry the default designation at any time

### **Character Movement**
movement name                         | (text, unique within character's Movement Option Group)
                                      |   invariant: movement name must be unique within the character's Movement Option Group at all times
movement type                         | (Walk, Run, Swim, Fly, or Jump)
                                      |   invariant: every character movement must have a movement type; a movement with no type cannot be saved
levitate                              | (boolean)
                                      |   invariant: true for Swim, Fly, and Jump — the character is not ground-tethered; floor collision detection does not apply and vertical displacement is permitted; false for Walk and Run — the character stays on the floor
movement parameters                   | (step interval, speed factor, approach behavior)
movement activation key               | (keyboard key value or unset)
                                      |   invariant: at most one character movement per character may hold a given movement activation key
distance limit                        | (distance value or absent)
                                      |   invariant: a distance limit of zero or absent means no limit is enforced
play on character                     | Movement Execution, Spawned NPC, Movement Animation
                                      |   invariant: movement execution must not proceed until target registration is confirmed

### **Movement Distance Count**
cumulative distance traveled          | (distance value, reset to zero per activation)
                                      |   invariant: the movement distance count must never cause the character to exceed the distance limit; the final step is clamped at or before the limit
reset on activation                   | Movement Execution
increment after step                  | Movement Execution, Memory Interface
compare against limit                 | Character Movement
                                      |   invariant: when the count reaches or exceeds the distance limit, movement execution halts immediately

### references

**Ref — ubiquitous-language-increment-4.md (Character Movement KA)**
Source: docs/increment-4/ubiquitous-language-increment-4.md
Locator: Character Movement section (lines 62–138)
Extract: whole

```source
character_movement concept block, movement_type type property, movement_parameters property, movement_activation_key property, default_movement property, distance_limit property, movement_distance_count concept block
```

### decisions made

- `movement type` is a type property of Character Movement, not a subtype: all five locomotion categories share the same execution pipeline; the type drives animation selection only — a data distinction, not a behavioral one
- `levitate` is a boolean property on Character Movement that is the single execution difference between movement types: Walk and Run have `levitate = false` (ground-tethered, floor collision applies); Swim, Fly, and Jump have `levitate = true` (not ground-tethered, vertical displacement permitted, floor collision skipped); no arc physics or gravity is modeled
- `movement parameters` is a composite property — step interval, speed factor, approach behavior are configured together in the movement editor and saved as a unit
- `Movement Option Group` owns the active/default selection invariants — individual Character Movement instances do not know whether they are "the active one"; the collection enforces this
- `movement activation key` and `distance limit` are scalar properties on Character Movement, not separate classes; each has its own invariant but no distinct identity or independent lifecycle
- `Movement Distance Count` earns its own class: distinct behavior (track, increment, compare, reset, clamp), its own story, testable independently of the parent Character Movement; it is not a simple counter property because it enforces the distance limit invariant with clamping logic

---

## **Memory Interface**

The service that attaches to the running game process and reads or writes live COH game-state values directly from process memory. The data layer enabling all movement operations.

### **Memory Interface**
attached state                        | Game Process
                                      |   invariant: no memory read or write may proceed until the game process is detected and all required memory pointers are resolved
character position                    | (X/Y/Z world-space coordinate triple)
character model matrix                | (4×4 world-space transform matrix)
character rotation matrix             | (orientation subcomponent of the spatial transform)
character facing vector               | (unit direction vector)
camera position                       | (X/Y/Z world-space coordinate triple)
attach to game process                | Game Process, Memory Pointer
read character position               | Memory Pointer
write character position              | Memory Pointer, Target Registration
read character model matrix           | Memory Pointer
write character rotation matrix       | Memory Pointer, Target Registration
read character facing vector          | Memory Pointer
read camera position                  | Memory Pointer, Camera Rig
monitor current target                | Current Target, Spawned NPC
                                      |   invariant: the memory interface notifies movement services when the current target changes
scan for stale memory pointers        | Memory Pointer, Stale Memory Pointer
                                      |   invariant: the memory interface must not proceed with a read or write against a known stale memory pointer
wait for target registration          | Target Registration, Spawned NPC
                                      |   invariant: movement commands are blocked until target registration succeeds for the target character

### **Memory Pointer**
cached address                        | Game Process
                                      |   invariant: a memory pointer must pass validation before any read or write; a stale or unresolved pointer must not be used
validation state                      | (valid or stale)
resolve from known pattern            | Game Process
validate against expected patterns    | Game Process
refresh when stale                    | Stale Memory Pointer, Game Process

### **Stale Memory Pointer**
detected state                        | Memory Pointer
                                      |   invariant: the memory interface must not proceed with a read or write against a known stale memory pointer; the re-resolution must complete before the operation is retried
trigger refresh cycle                 | Memory Interface, Memory Pointer

### **Game Process**
running state                         | (running or not running)
                                      |   invariant: the memory interface is inoperable until the game process is detected; all memory reads and writes are blocked while the process handle is absent or invalid
process handle                        | (OS process handle)
memory base address                   | (base address for pointer resolution)
detect by executable name             | (COH executable name and window handle match)

### **Current Target**
targeted entity identifier            | Spawned NPC
                                      |   invariant: movement dispatch is suspended when the current target is cleared; movement commands cannot fire with no target
notify on change                      | Memory Interface, Movement Execution

### **Target Registration**
registration state                    | (pending or confirmed)
                                      |   invariant: movement execution must not issue any game command against a spawned NPC before target registration is confirmed
poll for NPC name resolution          | Memory Interface, Spawned NPC
                                      |   invariant: polling that exceeds the configured timeout reports failure; movement remains blocked for that character without writing to unregistered address space

### references

**Ref — ubiquitous-language-increment-4.md (Memory Interface KA)**
Source: docs/increment-4/ubiquitous-language-increment-4.md
Locator: Memory Interface section (lines 141–230)
Extract: whole

```source
memory_interface concept block, memory_pointer concept block, stale_memory_pointer concept block, game_process concept block, character_position property, character_model_matrix property, character_rotation_matrix property, character_facing_vector property, camera_position property, current_target concept block, target_registration concept block
```

### decisions made

- `character position`, `character model matrix`, `character rotation matrix`, `character facing vector`, `camera position` are properties of Memory Interface — documented as named fields whose read/write behaviors are individually storied, but their lifecycle and identity are fully owned by the Memory Interface service
- `character position`, `character facing vector`, `character rotation matrix`, and `camera position` are Memory Interface properties — they are named fields on the Memory Interface service, not collaborating classes; collaborator lists name `Memory Interface` as the single structural collaborator for all position/rotation reads and writes
- `Memory Pointer` is a class: distinct identity (resolved address), state (valid/stale), behavior (resolve, validate, refresh), its own invariants; not merely a language-level address value
- `Stale Memory Pointer` is a class (not a subtype of Memory Pointer): it describes a pointer in the invalid-state condition with distinct detection and refresh-trigger behavior, its own story ("Scan and Fix Stale Memory Pointers"), and distinct invariants; at CRC level its responsibility is to be detected and to trigger the refresh cycle
- `Game Process` is a class: distinct identity (running OS process), state (running/not running), behavior (detected by scan, provides base address and process handle), its own story and invariant
- `Current Target` is a class: distinct behavior (monitored continuously, notifies on change), state (targeted entity identifier), its own story
- `Target Registration` is a class: distinct behavior (polled after spawn, blocks movement until confirmed), state (pending/confirmed), its own story, testable independently

---

## **Movement Execution**

The service that applies character movements to spawned NPCs by issuing move NPC commands, enforcing distance limits, checking collisions, and playing the correct movement animation.

### **Movement Execution**
compute movement destination          | Memory Interface, Camera Rig, Maneuver-with-Camera Mode
                                      |   invariant: movement execution must not issue any game command against a spawned NPC before target registration is confirmed
issue move NPC command                | Move NPC Command, Spawned NPC, Memory Interface
                                      |   invariant: the movement distance count must never cause the character to exceed the distance limit; the last step is clamped so the character stops at or before the limit
check floor collision                 | Floor Collision
check wall collision                  | Wall Collision
halt on distance limit reached        | Movement Distance Count, Character Movement
play movement animation               | Movement Animation, Spawned NPC
                                      |   invariant: the animation played must match the movement type of the active character movement
stop movement animation               | Movement Animation, Spawned NPC
turn spawned NPC to face target       | Memory Interface, Spawned NPC
reset character orientation           | Memory Interface

### **Move NPC Command**
target NPC name                       | Spawned NPC
                                      |   invariant: a move NPC command must target a registered spawned NPC by name; targeting an unregistered NPC produces a no-op
destination coordinates               | (X/Y/Z world-space location)
deliver via native game bridge        | Game Bridge

### **Movement Animation**
active animation cycle                | (walk, run, swim, fly, or jump)
                                      |   invariant: the animation played must match the movement type of the active character movement; mismatched animation is an error
start on movement begin               | Spawned NPC, Movement Execution
stop on movement halt                 | Spawned NPC, Movement Execution

### **Floor Collision**
detect floor intersection             | Movement Execution, Memory Interface
                                      |   invariant: the spawned NPC does not pass through floor geometry; vertical descent stops at the contact point
anchor at contact point               | Memory Interface

### **Wall Collision**
detect wall intersection              | Movement Execution, Memory Interface
                                      |   invariant: the spawned NPC stops at the wall boundary; movement in the blocked direction halts without error
halt in blocked direction             | Movement Execution

### references

**Ref — ubiquitous-language-increment-4.md (Movement Execution KA)**
Source: docs/increment-4/ubiquitous-language-increment-4.md
Locator: Movement Execution section (lines 233–287)
Extract: whole

```source
movement_execution concept block, move_NPC_command concept block, movement_animation concept block, floor_collision concept block, wall_collision concept block
```

### decisions made

- `Movement Execution` is the orchestrating service: it computes destinations, issues commands, tracks distance, checks collisions, and controls animation; collaborates with Memory Interface for all position/rotation reads and writes
- `Move NPC Command` is a class: distinct identity (game command with target name and destination), behavior (delivered via native game bridge), invariants (target must be registered); analogous to spawn NPC command and target by name command from Increment 2
- `Movement Animation` is a class: distinct behavior (type-driven selection, started/stopped with movement), invariants (must match movement type), its own story
- `Floor Collision` and `Wall Collision` are separate classes: each has a distinct detection behavior and distinct response (anchor to floor vs. halt in direction); their own story, testable independently
- Teleport to camera is a degenerate case of move-to-camera: the character position is set in one memory write directly to the camera position; no step loop, animation, or distance tracking applies; handled as a mode within Movement Execution, not a separate class

---

## **Camera Rig**

The virtual camera system rendered in the COH game world that the GM uses to navigate the scene and the prerequisite for all camera-relative movement commands.

### **Camera Rig**
active state                          | Camera Enable/Disable Script
                                      |   invariant: the camera rig must be rendered in game before any camera-relative movement command can succeed
activate by deploying enable script   | Camera Enable/Disable Script, Game Bridge
deactivate by deploying disable script| Camera Enable/Disable Script, Game Bridge, Camera Follow
provide camera position               | Memory Interface

### **Camera Follow**
follow state                          | (active or inactive)
                                      |   invariant: camera follow may only be active on one character at a time; activating follow on a second character automatically unfollows the first
followed character                    | Spawned NPC, Memory Interface
activate follow                       | Camera Rig, Spawned NPC
                                      |   invariant: camera follow cannot activate while the camera rig is not rendered in game
deactivate follow                     | Camera Rig, Camera Detach
track character position              | Memory Interface, Spawned NPC

### **Maneuver-with-Camera Mode**
active state                          | (active or inactive)
                                      |   invariant: maneuver-with-camera mode cannot activate while the camera rig is not rendered in game
redirect movement to camera bearing   | Movement Execution, Camera Rig, Memory Interface

### **Camera Detach**
disconnect follow link                | Camera Follow, Camera Rig
                                      |   invariant: camera detach also terminates maneuver-with-camera mode when active
execute detach command                 | Game Bridge
return to free-roam                   | Camera Rig

### **Camera Enable/Disable Script**
deployed state                        | (deployed or not deployed)
deploy enable variant                 | Game Bridge, Camera Rig
deploy disable variant                | Game Bridge, Camera Rig, Camera Follow
                                      |   invariant: deploying the disable variant while camera follow is active also terminates the follow mode

### references

**Ref — ubiquitous-language-increment-4.md (Camera Rig KA)**
Source: docs/increment-4/ubiquitous-language-increment-4.md
Locator: Camera Rig section (lines 291–353)
Extract: whole

```source
camera_rig concept block, camera_follow concept block, maneuver_with_camera_mode concept block, camera_detach concept block, camera_enable_disable_script concept block
```

### decisions made

- `Camera Rig` is the root class of this KA: distinct identity (rendered game object), state (active/inactive), behavior (activated/deactivated by script, provides camera position), invariants (must be active for camera-relative commands)
- `Camera Follow` is a class (not a property): distinct state (active/inactive with specific target), behavior (continuously tracks character position, activated/deactivated explicitly), invariants (one character at a time, requires rig active), its own stories
- `Maneuver-with-Camera Mode` is a class: distinct state (active/inactive), behavior (redirects movement computation to camera bearing), its own story, testable independently
- `Camera Detach` is a class: distinct operation behavior (disconnect follow, terminate maneuver mode, execute via game bridge, return to free-roam), triggered both explicitly and by character despawn, its own story
- `Camera Enable/Disable Script` is a class: distinct identity (deployable script file), state (deployed/not deployed), behavior (enable renders rig, disable removes rig and terminates follow), its own story

---

# Boundary Domain

## **Character**

Owned by: Character and Crowd Library (Increment 1)

### **Character**
Movement Option Group                 | Movement Option Group

### references

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

```source
Character concept — holds option groups for Identities, Abilities, and now Movements
```

### decisions made

- Character is a boundary concept: lifecycle, CRUD, and crowd membership are owned by Increment 1; this increment depends on Character as the host for Character Movements and as the name source for movement game commands

---

## **Spawned NPC**

Owned by: Character Identities (Increment 2)

### **Spawned NPC**
(no new responsibilities in this increment — movement, turning, animation, and camera follow operations target the spawned NPC by the character's name)

### references

**Ref — ubiquitous-language-increment-2.md (Identity KA — spawned NPC)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA — spawned NPC section

```source
Spawned NPC concept — the game-world entity targeted by movement commands
```

### decisions made

- Spawned NPC is a boundary concept: its lifecycle (spawn, despawn, targeting) is fully owned by Increment 2; this increment depends on it as the execution target for all movement and camera follow operations

---

## **Game Bridge**

Owned by: Character Identities (Increment 2)

### **Game Bridge**
(no new responsibilities modeled — executes move NPC command, follow command, camera detach command, and deploys camera enable/disable scripts via the existing slash command and script delivery infrastructure)

### references

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

```source
Game Bridge concept — routes slash commands and deploys scripts to the COH game session
```

### decisions made

- Game Bridge is a boundary concept: initialization and slash-command routing are established in Increment 2; this increment adds move NPC command, follow, camera detach, and script deployment to the existing pipeline

---

## **Keyboard Hook**

Owned by: Animated Abilities (Increment 3)

### **Keyboard Hook**
(no new responsibilities modeled — dispatches character movement activation keys using the same key routing logic established in Increment 3 for animated ability activation keys)

### references

**Ref — ubiquitous-language-increment-3.md (Keyboard Hook KA)**
Source: docs/domain/ubiquitous-language-increment-3.md
Locator: Keyboard Hook KA section

```source
Keyboard Hook concept — intercepts key events and routes to ability or movement dispatch
```

### decisions made

- Keyboard Hook is a boundary concept: installation and key-routing logic are owned by Increment 3; this increment extends the routing to dispatch character movement alongside animated ability dispatch

---

## **KeyBind**

Owned by: Character Identities (Increment 2)

### **KeyBind**
(no new responsibilities modeled — may be used to deliver move NPC commands and camera rig operations via the keybind execution path)

### references

**Ref — ubiquitous-language-increment-2.md (KeyBind KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: KeyBind KA section

```source
KeyBind concept — file generation and bind-load-file delivery pipeline for game commands
```

### decisions made

- KeyBind is a boundary concept: file generation and delivery pipeline are owned by Increment 2; this increment may use the same channel for movement-related commands where the keybind path is required

---

## **Animated Ability**

Owned by: Animated Abilities (Increment 3)

### **Animated Ability**
(no new responsibilities modeled — coexists with character movements on the same character via Abilities option group vs. Movements option group; shares the keyboard hook dispatch infrastructure)

### references

**Ref — ubiquitous-language-increment-3.md (Animated Ability KA)**
Source: docs/domain/ubiquitous-language-increment-3.md
Locator: Animated Ability KA section

```source
Animated Ability concept — composable action sequences sharing keyboard dispatch with character movements
```

### decisions made

- Animated Ability is a boundary concept: its full lifecycle belongs to Increment 3; this increment coexists with it on the same character (Abilities option group vs. Movements option group)
