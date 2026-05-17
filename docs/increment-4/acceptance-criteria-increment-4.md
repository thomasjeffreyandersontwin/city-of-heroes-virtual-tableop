# Acceptance Criteria — Increment 4: Single Character Movement

> Domain source: `docs/domain/ubiquitous-language-increment-4.md`  
> Stories: 35 stories across Memory Interface, Character Movement authoring, Movement Execution, and Camera Rig.

---

## Memory Interface

---

### Story: Detect Game Process for Connection

**Domain terms:**
- *Game Process* — the running COH client process the *memory interface* must detect and attach to
- *Memory Interface* — the service that attaches to the running *game process* and reads/writes game-state values
- *Memory Pointer* — a resolved process-memory address identifying a specific game-state value
- *Game Bridge* — the boundary service that routes game commands (Increment 2)

1. **WHEN** the application starts and the COH client is running  
   **THEN** the *memory interface* detects the *game process* by its known executable name and window handle  
   **AND** the *memory interface* attaches to the *game process* and resolves all required *memory pointers*  
   **AND** the movement and camera services become available for use

2. **WHEN** the *game process* is not running at application start  
   **THEN** the *memory interface* enters an unattached state and all memory reads and writes are blocked  
   **BUT** no crash or error is raised on startup; the application waits or shows a not-connected indicator

3. **WHEN** the *game process* terminates during a session  
   **THEN** the *memory interface* detects the loss of the process handle  
   **AND** all in-flight memory operations are cancelled and blocked until the *game process* is re-detected  
   **BUT** previously resolved *memory pointers* are not reused after process termination; a fresh detection and resolution cycle must occur

4. **WHEN** multiple COH processes are found on the system  
   **THEN** the *memory interface* attaches to the process whose window handle matches the expected COH window  
   **BUT** it does not attach to any other process, even if both have the correct executable name

---

### Story: Read Target Character from Memory

**Domain terms:**
- *Current Target* — the game entity identifier in the COH targeting register; identifies which *spawned NPC* the GM intends to act on
- *Memory Interface* — reads the targeting register from *game process* memory
- *Spawned NPC* — the COH game-world entity targeted for movement operations
- *Memory Pointer* — the cached address for the targeting register

1. **WHEN** the GM has a character selected in the COH game client  
   **THEN** the *memory interface* reads the *current target* identifier from the COH targeting register in process memory  
   **AND** the application identifies the targeted *spawned NPC* by the resolved identifier  
   **AND** movement services use that *spawned NPC* as the active action target

2. **WHEN** no character is selected in the COH game client  
   **THEN** the *memory interface* reads an empty or null *current target* from the targeting register  
   **AND** movement commands that require a target are blocked until a target is confirmed  
   **BUT** no error is raised; the system waits for a valid target

3. **WHEN** the GM changes the selected character in the COH client  
   **THEN** the *memory interface* detects the change in the targeting register  
   **AND** notifies movement services of the new *current target*

---

### Story: Monitor Current Target in Game

**Domain terms:**
- *Current Target* — continuously monitored entity identifier in the COH targeting register
- *Memory Interface* — polls the targeting register on each update cycle
- *Spawned NPC* — the entity being tracked
- *Movement Execution* — the consumer of the *current target* for movement dispatch

1. **WHEN** a session is active and a *spawned NPC* is the *current target*  
   **THEN** the *memory interface* continuously polls the targeting register and exposes the active *current target* to movement services  
   **AND** the movement list and crowd manager reflect the currently targeted character

2. **WHEN** the *current target* changes while movement is in progress  
   **THEN** the *memory interface* detects the change and notifies movement services  
   **AND** any in-progress *movement execution* against the previous target is halted before the new target is activated  
   **BUT** no movement command is issued to the new target without explicit GM action

3. **WHEN** the *current target* is cleared (the GM deselects all characters)  
   **THEN** movement dispatch is suspended; movement commands cannot fire with no target  
   **BUT** the *camera rig* and *camera follow* continue operating independently of target state if already active

---

### Story: Wait until Target is Registered after Spawn

**Domain terms:**
- *Target Registration* — confirmation that a newly spawned NPC is addressable in the COH targeting system
- *Memory Interface* — polls for *target registration* after spawn
- *Spawned NPC* — the newly created game entity waiting for registration
- *Movement Execution* — blocked until *target registration* succeeds

1. **WHEN** a *spawned NPC* has just been created via a spawn command  
   **THEN** the *memory interface* polls the COH targeting system until the NPC's name resolves correctly  
   **AND** *movement execution* is blocked from issuing any commands against the NPC until *target registration* succeeds  
   **AND** once confirmed, movement commands are unblocked and the GM may proceed

2. **WHEN** *target registration* polling exceeds a configured timeout  
   **THEN** the application reports that the NPC failed to register  
   **AND** movement commands remain blocked for that character  
   **BUT** no memory is written to the unregistered NPC's address space; the failure is surfaced without side effects

3. **WHEN** movement is triggered immediately after spawn without waiting  
   **THEN** the movement command is queued or rejected until *target registration* is confirmed  
   **BUT** the command is not silently dropped; the GM can retry once registration completes

---

### Story: Scan and Fix Stale Memory Pointers

**Domain terms:**
- *Stale Memory Pointer* — a *memory pointer* whose cached address no longer refers to the expected game-state value
- *Memory Pointer* — a cached resolved process-memory address
- *Memory Interface* — runs the periodic scan and refreshes stale pointers
- *Game Process* — the COH client process whose memory layout may change

1. **WHEN** the *memory interface* runs its periodic scan  
   **THEN** each cached *memory pointer* is read at its recorded address and validated against expected game-state patterns  
   **AND** any *memory pointer* that fails validation is marked as *stale*  
   **AND** the *memory interface* re-resolves each *stale memory pointer* from known address patterns before the next read or write proceeds

2. **WHEN** a *stale memory pointer* is detected mid-operation  
   **THEN** the in-progress read or write is cancelled  
   **AND** the *memory interface* completes the re-resolution cycle before retrying the operation  
   **BUT** no stale address is written to; the write is held until a valid pointer is restored

3. **WHEN** a previously stale *memory pointer* is re-resolved successfully  
   **THEN** normal read/write operations resume using the refreshed address  
   **AND** the stale indicator is cleared from the affected pointer

4. **WHEN** the *game process* restarts and the *memory interface* re-attaches  
   **THEN** all previously resolved *memory pointers* are treated as invalid and re-resolved from scratch  
   **BUT** cached pointer values from the previous session are not reused under any circumstance

---

### Story: Read Character Position (X, Y, Z) from Memory

**Domain terms:**
- *Character Position* — the X/Y/Z world-space coordinates of the character's location in process memory
- *Memory Interface* — reads *character position* before each movement step
- *Memory Pointer* — the resolved address for the position register
- *Movement Execution* — consumer of the returned coordinates

1. **WHEN** *movement execution* requires the character's current location  
   **THEN** the *memory interface* reads the *character position* (X, Y, Z) from the resolved *memory pointer* address in the *game process*  
   **AND** the three coordinate values are returned as a validated world-space triple  
   **AND** *movement execution* uses the coordinates to compute the next movement destination

2. **WHEN** the *memory pointer* for *character position* is stale at read time  
   **THEN** the read is blocked until the pointer is refreshed  
   **AND** the refreshed address is used for the read  
   **BUT** no stale address is used to compute a movement destination

---

### Story: Write Character Position to Memory

**Domain terms:**
- *Character Position* — the X/Y/Z coordinates written to process memory to reposition the *spawned NPC*
- *Memory Interface* — writes the new coordinates to the resolved *memory pointer*
- *Spawned NPC* — the entity repositioned in the game world
- *Target Registration* — must be confirmed before position is written

1. **WHEN** *movement execution* computes a new destination position  
   **THEN** the *memory interface* writes the new *character position* (X, Y, Z) to the resolved *memory pointer* address in the *game process*  
   **AND** the *spawned NPC* moves to the new coordinates in the COH game world  
   **AND** subsequent reads of *character position* return the newly written values

2. **WHEN** the *memory pointer* for *character position* is stale at write time  
   **THEN** the write is blocked and the pointer is refreshed before the write is retried  
   **BUT** no coordinates are written to a stale or unresolved address

3. **WHEN** *target registration* has not yet been confirmed for the target character  
   **THEN** the *memory interface* does not write *character position* for that character  
   **BUT** the write is queued until *target registration* succeeds, not silently dropped

---

### Story: Read Character Model Matrix from Memory

**Domain terms:**
- *Character Model Matrix* — the 4×4 world-space transform matrix in process memory encoding position, rotation, and scale
- *Memory Interface* — reads the matrix before turning and orientation operations
- *Movement Execution* — consumer of the matrix for orientation computation

1. **WHEN** *movement execution* needs to compute a turn or orientation change  
   **THEN** the *memory interface* reads the *character model matrix* from the resolved *memory pointer* in the *game process*  
   **AND** the full 4×4 matrix is returned as a validated transform  
   **AND** *movement execution* uses the matrix to derive the current facing for delta computation

2. **WHEN** the *memory pointer* for the *character model matrix* is stale  
   **THEN** the read is held until the pointer is refreshed, then proceeds with the refreshed address  
   **BUT** no stale matrix data is used for orientation computation

---

### Story: Write Character Rotation Matrix to Memory

**Domain terms:**
- *Character Rotation Matrix* — the orientation subcomponent of the character's transform; written to process memory to change facing
- *Memory Interface* — writes the computed rotation matrix
- *Movement Execution* — computes and supplies the new rotation matrix
- *Spawned NPC* — changes facing in the game world upon write

1. **WHEN** *movement execution* computes a new facing direction for the character  
   **THEN** the *memory interface* writes the *character rotation matrix* to the resolved *memory pointer* in the *game process*  
   **AND** the *spawned NPC* faces the new direction in the COH game world immediately

2. **WHEN** the *memory pointer* for the *character rotation matrix* is stale at write time  
   **THEN** the write is blocked and the pointer is refreshed before retrying  
   **BUT** no rotation matrix is written to a stale address

---

### Story: Read Character Facing Vector from Memory

**Domain terms:**
- *Character Facing Vector* — the unit direction vector in process memory pointing in the character's current facing direction
- *Memory Interface* — reads the vector before turns and camera-relative steps
- *Movement Execution* — uses the vector to compute the required rotation delta

1. **WHEN** *movement execution* or a turn operation needs the character's current facing  
   **THEN** the *memory interface* reads the *character facing vector* from the resolved *memory pointer* in the *game process*  
   **AND** a normalized unit vector is returned representing the character's current facing direction  
   **AND** *movement execution* uses this to compute the rotation delta required to face the target

2. **WHEN** the *character facing vector* pointer is stale  
   **THEN** the read is held until the pointer is refreshed  
   **BUT** no stale facing data is used to compute movement or rotation

---

### Story: Write Character Facing Direction to Memory

**Domain terms:**
- *Character Rotation Matrix* — written to encode the new facing direction
- *Character Facing Vector* — the source direction used to compute the matrix
- *Memory Interface* — writes the new rotation data
- *Spawned NPC* — updates its facing in the game world

1. **WHEN** *movement execution* determines a new facing direction (from a turn-to-target or reset)  
   **THEN** the *memory interface* computes the corresponding *character rotation matrix* and writes it to the *game process*  
   **AND** the *spawned NPC* faces the computed direction in the COH game world

2. **WHEN** the new facing is identical to the current *character facing vector*  
   **THEN** no write is issued; the *memory interface* skips the operation as a no-op  
   **BUT** no error is raised; the skip is silent

---

### Story: Read Camera Position from Memory

**Domain terms:**
- *Camera Position* — the X/Y/Z world-space coordinates of the COH game camera in process memory
- *Memory Interface* — reads *camera position* on demand for camera-relative commands
- *Camera Rig* — provides the camera object whose position is read
- *Movement Execution* — uses *camera position* as the destination for camera-relative moves

1. **WHEN** the GM triggers a camera-relative movement command (move to camera position, teleport to camera, or maneuver-with-camera step)  
   **THEN** the *memory interface* reads the *camera position* (X, Y, Z) from the resolved *memory pointer* in the *game process*  
   **AND** the coordinates are returned to *movement execution* as the destination or bearing reference

2. **WHEN** the *camera rig* is not rendered in game  
   **THEN** the *camera position* read returns coordinates for the free-roam camera or last known position  
   **AND** a warning is shown to the GM that the *camera rig* is not active  
   **BUT** the read still proceeds; the raw camera coordinates are returned regardless of rig state

3. **WHEN** the *camera position* pointer is stale  
   **THEN** the read is blocked and the pointer refreshed before the camera-relative move proceeds  
   **BUT** no stale coordinates are used as a movement destination

---

## Character Movement Authoring

---

### Story: Add Movement to Character

**Domain terms:**
- *Character Movement* — the named movement configuration being added
- *Movement Type* — the locomotion category (Walk/Run/Swim/Fly/Jump) selected on creation
- *Option Group* — the Movements collection on the character that receives the new entry
- *Crowd Manager — Movements* — the screen where movement list actions occur

1. **WHEN** the GM selects a character in the *crowd tree* and adds a movement in the *crowd manager — movements* movement list  
   **THEN** a new *character movement* entry appears in the movement list for that character  
   **AND** the new movement requires a name unique within the character's Movements *option group*  
   **AND** the new movement is added with a default *movement type* of Walk until the GM changes it

2. **WHEN** the GM attempts to add a *character movement* with a name already used on that character  
   **THEN** the movement list rejects the add and shows a name-collision message  
   **BUT** no duplicate *character movement* is created in the *option group*

3. **WHEN** no character is selected in the *crowd tree*  
   **THEN** the Add action is disabled in the movement list  
   **BUT** no error is shown; the control is simply inactive

4. **WHEN** the GM adds a *character movement* successfully  
   **THEN** the movement list refreshes to show the new entry with its name and *movement type*  
   **AND** the default flag and *movement activation key* columns are empty until the GM configures them

---

### Story: Edit Movement Parameters

**Domain terms:**
- *Character Movement* — the movement being edited
- *Movement Parameters* — the configurable execution values (step interval, speed factor, approach behavior)
- *Movement Type* — the locomotion category, changeable in the editor
- *Distance Limit* — the max travel distance per activation, configurable in the editor
- *Movement Editor* — the form screen opened from the movement list

1. **WHEN** the GM selects a *character movement* in the movement list and opens the *movement editor*  
   **THEN** the *movement editor* displays the movement's current name, *movement type*, *movement activation key*, *distance limit*, and default flag  
   **AND** all fields are editable

2. **WHEN** the GM changes the *movement type* in the *movement editor* and saves  
   **THEN** the *character movement* is updated with the new *movement type*  
   **AND** the movement list reflects the new type value  
   **AND** the *movement animation* played on next execution matches the updated *movement type*

3. **WHEN** the GM sets a *distance limit* value and saves  
   **THEN** the *character movement* enforces the new *distance limit* on the next *movement execution*

4. **WHEN** the GM cancels the *movement editor* without saving  
   **THEN** the *character movement* retains its previous *movement parameters*  
   **BUT** no changes are persisted

5. **WHEN** the GM saves the *movement editor* with an empty name field  
   **THEN** the save is rejected with a validation message  
   **BUT** the *movement editor* remains open so the GM can correct the field

---

### Story: Remove Movement from Character

**Domain terms:**
- *Character Movement* — the movement being removed
- *Option Group* — the Movements collection from which the entry is deleted
- *Movement Activation Key* — the key binding cleared when the movement is removed
- *Crowd Manager — Movements* — the screen where remove actions occur

1. **WHEN** the GM selects a *character movement* in the movement list and removes it  
   **THEN** the *character movement* is permanently deleted from the character's Movements *option group*  
   **AND** the *movement activation key* that was assigned to that movement is freed and no longer bound to any movement  
   **AND** the movement list refreshes to reflect the removal

2. **WHEN** the removed *character movement* was the *default movement*  
   **THEN** no *default movement* remains on the character after removal  
   **AND** the default marker is cleared from all other movements  
   **BUT** other *character movements* are unaffected; only the default designation is cleared

3. **WHEN** no *character movement* is selected in the movement list  
   **THEN** the Remove action is disabled  
   **BUT** no error is shown

---

### Story: Set Default Movement

**Domain terms:**
- *Default Movement* — the *character movement* automatically applied when no explicit activation key is pressed
- *Character Movement* — the movement receiving the default designation
- *Crowd Manager — Movements* — where the set-default action is invoked

1. **WHEN** the GM selects a *character movement* in the movement list and invokes Set Default  
   **THEN** the selected movement is marked as the *default movement*  
   **AND** the default marker is displayed in the movement list row for that movement  
   **AND** any previously designated *default movement* on the same character has its default flag cleared

2. **WHEN** the GM removes the *default movement* designation without selecting a replacement  
   **THEN** no *character movement* on the character carries the default flag  
   **BUT** existing movements are otherwise unaffected

3. **WHEN** the GM sets a second *character movement* as *default movement* while one already exists  
   **THEN** the new selection becomes the *default movement* and the previous one's flag is cleared atomically  
   **BUT** at no moment do two movements carry the default flag simultaneously

---

### Story: Set Movement Activation Key

**Domain terms:**
- *Movement Activation Key* — the keyboard key assigned to trigger a *character movement*
- *Character Movement* — the movement receiving the key assignment
- *Keyboard Hook* — intercepts presses of the assigned key to dispatch movement
- *Crowd Manager — Movements* — where set-key is invoked

1. **WHEN** the GM selects a *character movement* and assigns a *movement activation key* via set-key  
   **THEN** the key is saved on the *character movement* and displayed in the movement list's activation key column  
   **AND** the *keyboard hook* begins routing presses of that key to dispatch the *character movement* on the active character

2. **WHEN** the GM assigns a key already used by another *character movement* on the same character  
   **THEN** the assignment is rejected with a conflict message  
   **BUT** neither the new assignment nor the existing one is changed

3. **WHEN** the GM clears the *movement activation key* on a *character movement*  
   **THEN** the movement can no longer be dispatched via the *keyboard hook* but is still accessible from the movement list  
   **AND** the activation key column shows blank for that movement

---

### Story: Add Default Movements to Character (Walk, Run, Swim)

**Domain terms:**
- *Character Movement* — the three default movements (Walk, Run, Swim) added to the character
- *Movement Type* — Walk, Run, and Swim used as the three types
- *Option Group* — the Movements *option group* receiving the default set
- *Default Movement* — Walk is designated as the *default movement* in the default set

1. **WHEN** the GM invokes Add Default Movements on a character  
   **THEN** three *character movements* are added to the character's Movements *option group*: Walk (type Walk), Run (type Run), Swim (type Swim)  
   **AND** the Walk movement is designated as the *default movement*  
   **AND** all three appear in the movement list with their types

2. **WHEN** the character already has a *character movement* named Walk, Run, or Swim  
   **THEN** only the movements whose names do not conflict are added  
   **AND** the GM sees a message indicating which default movements were skipped due to name collisions  
   **BUT** existing movements are not overwritten

3. **WHEN** Add Default Movements is invoked on a character with an empty Movements *option group*  
   **THEN** all three default movements are added without conflict  
   **AND** the movement list shows all three entries after the operation

---

## Movement Execution

---

### Story: Execute Move NPC Command

**Domain terms:**
- *Move NPC Command* — the game command that repositions a *spawned NPC* to a specified world-space location
- *Movement Execution* — issues the *move NPC command* via the *native game bridge*
- *Spawned NPC* — the entity repositioned in the game world
- *Target Registration* — must be confirmed before the command fires

1. **WHEN** *movement execution* has computed a valid destination and *target registration* is confirmed  
   **THEN** the *move NPC command* is delivered via the *native game bridge* targeting the *spawned NPC* by name  
   **AND** the COH engine repositions the *spawned NPC* to the specified world-space coordinates immediately  
   **AND** a subsequent read of *character position* from the *memory interface* returns the new location

2. **WHEN** *target registration* is not yet confirmed for the target character  
   **THEN** the *move NPC command* is not issued  
   **BUT** the command is held or the GM is notified to wait; it is not silently discarded

3. **WHEN** the *move NPC command* targets a name that has no matching *spawned NPC* in the current game session  
   **THEN** the COH engine receives the command but produces a no-op; the *spawned NPC* does not move  
   **AND** the application detects the no-op and shows a "character not found" indicator to the GM

---

### Story: Move Character to Location

**Domain terms:**
- *Movement Execution* — drives the step-by-step movement to the target location
- *Character Position* — read before each step to track progress
- *Move NPC Command* — issued for each movement step
- *Distance Limit* — halts movement when the *movement distance count* reaches the limit
- *Floor Collision* / *Wall Collision* — checked before each step

1. **WHEN** the GM selects a target location on the desktop and triggers Move to Location  
   **THEN** *movement execution* issues *move NPC commands* step by step toward the destination  
   **AND** the *movement distance count* increments after each successful step  
   **AND** the *spawned NPC* appears to travel toward the destination in the COH game world

2. **WHEN** a *floor collision* or *wall collision* is detected on the next step  
   **THEN** movement halts in the blocked direction  
   **AND** the GM sees the character stop at the collision boundary  
   **BUT** no error is raised; the stop is the correct behavior

3. **WHEN** the *movement distance count* reaches the *distance limit* before the destination is reached  
   **THEN** *movement execution* halts and the GM sees a limit-reached indicator  
   **AND** the character stops at the limit boundary, not at the destination  
   **BUT** no overshoot occurs; the final step is clamped to the limit

4. **WHEN** the destination is reached before the *distance limit*  
   **THEN** *movement execution* stops the step loop and clears the moving indicator  
   **AND** the *movement distance count* is reset to zero

---

### Story: Move Character to Camera Position

**Domain terms:**
- *Camera Position* — the X/Y/Z coordinates of the COH game camera; the movement destination
- *Movement Execution* — drives the step-by-step movement toward *camera position*
- *Camera Rig* — provides the camera whose position is the destination
- *Memory Interface* — reads *camera position* to supply the destination

1. **WHEN** the GM triggers Move to Camera Position  
   **THEN** the *memory interface* reads the current *camera position*  
   **AND** *movement execution* drives the *spawned NPC* toward the *camera position* step by step  
   **AND** the same *distance limit*, *floor collision*, and *wall collision* rules apply as for Move to Location

2. **WHEN** the *camera rig* is not active but the GM triggers Move to Camera Position  
   **THEN** the *memory interface* reads the raw COH camera coordinates and uses them as the destination  
   **AND** the GM sees a notice that the *camera rig* is not active, but movement proceeds  
   **BUT** no movement is blocked solely because the *camera rig* is inactive; the raw camera position is always available

---

### Story: Teleport Character to Camera

**Domain terms:**
- *Camera Position* — the teleport destination read from process memory
- *Character Position* — set in one memory write to the *camera position* value
- *Memory Interface* — reads *camera position*, writes *character position*
- *Movement Animation* — not played during teleport (instant position change)
- *Target Registration* — must be confirmed before teleport

1. **WHEN** the GM triggers Teleport to Camera and *target registration* is confirmed  
   **THEN** the *memory interface* reads the current *camera position*  
   **AND** writes the *camera position* directly to the *character position* address in one operation  
   **AND** the *spawned NPC* appears at the camera's location instantly in the COH game world  
   **AND** no *movement animation* plays; the teleport is instantaneous

2. **WHEN** *target registration* is not confirmed  
   **THEN** the teleport is blocked until registration succeeds  
   **BUT** the camera position read still proceeds; only the write is held

3. **WHEN** the *camera position* pointer is stale at teleport time  
   **THEN** the teleport is held until the pointer is refreshed, then proceeds with the valid address

---

### Story: Animate Walk/Run/Swim/Fly/Jump Movement

**Domain terms:**
- *Movement Animation* — the game-side locomotion animation played on the *spawned NPC*
- *Movement Type* — determines which animation plays (Walk/Run/Swim/Fly/Jump)
- *Character Movement* — the active movement whose type drives animation selection
- *Spawned NPC* — the entity on which the animation plays
- *Movement Execution* — starts and stops the animation

1. **WHEN** *movement execution* begins a *character movement* activation  
   **THEN** the *movement animation* matching the active *movement type* is started on the *spawned NPC*  
   **AND** Walk type plays the walk cycle, Run plays the run cycle, Swim the swim cycle, Fly the fly cycle, Jump the jump arc

2. **WHEN** *movement execution* halts (distance limit reached, collision, or GM stop)  
   **THEN** the *movement animation* stops on the *spawned NPC*  
   **AND** the *spawned NPC* returns to its idle pose

3. **WHEN** the *movement type* of the active *character movement* changes mid-session  
   **THEN** the *movement animation* on the next execution matches the updated *movement type*  
   **AND** the previous animation is not continued

4. **WHEN** a *character movement* with a Fly or Jump *movement type* is executed  
   **THEN** the corresponding aerial animation plays and the *spawned NPC* moves in the vertical axis as well  
   **BUT** floor collision rules still apply; the NPC does not pass through floor geometry unless the movement type explicitly permits it

---

### Story: Track Movement Distance Count

**Domain terms:**
- *Movement Distance Count* — running tally of in-game distance traveled in the current execution
- *Movement Execution* — increments the count after each step
- *Distance Limit* — the threshold compared against the count
- *Character Position* — read before and after each step to compute the per-step distance delta

1. **WHEN** a *character movement* activation begins  
   **THEN** the *movement distance count* is reset to zero  
   **AND** *movement execution* begins incrementing the count by the per-step distance after each successful *move NPC command*

2. **WHEN** the *movement distance count* reaches or exceeds the *distance limit*  
   **THEN** *movement execution* halts and shows limit-reached feedback to the GM  
   **AND** the count is not incremented further until the next activation

3. **WHEN** the *distance limit* is zero or absent on the active *character movement*  
   **THEN** the *movement distance count* is still tracked but no halting threshold is applied  
   **AND** movement continues until the GM stops it or a collision occurs

---

### Story: Enforce Distance Limit per Movement Type

**Domain terms:**
- *Distance Limit* — the max travel distance configured on a *character movement*
- *Movement Distance Count* — the running tally compared against the *distance limit*
- *Movement Execution* — enforces the limit by halting movement when the count reaches the threshold
- *Movement Type* — each *movement type* may have a different *distance limit* configured

1. **WHEN** the *movement distance count* reaches the *distance limit* during a *character movement* execution  
   **THEN** *movement execution* immediately halts step issuance  
   **AND** the final step is clamped so the character stops at or before the limit, not beyond it  
   **AND** the GM sees a clear limit-reached indicator in the crowd manager or desktop

2. **WHEN** the configured *distance limit* is different between Walk and Run movements  
   **THEN** each *character movement* enforces only its own *distance limit*, independently of other movements  
   **BUT** once halted, the character does not move further until the GM activates a different *character movement* or the same one again

3. **WHEN** the GM changes a *character movement's* *distance limit* in the *movement editor* and saves  
   **THEN** the new *distance limit* applies on the next activation of that movement  
   **AND** any in-progress execution is unaffected by the change

---

### Story: Detect Floor and Wall Collisions

**Domain terms:**
- *Floor Collision* — the runtime check detecting intersection with a floor surface
- *Wall Collision* — the runtime check detecting intersection with a wall or obstacle
- *Movement Execution* — performs collision checks before each movement step
- *Spawned NPC* — the entity whose path is checked

1. **WHEN** *movement execution* computes the next movement step  
   **THEN** *floor collision* is checked to detect whether the path intersects a floor surface  
   **AND** *wall collision* is checked to detect whether the path intersects a wall or obstacle  
   **AND** if no collision is detected, the *move NPC command* is issued for that step

2. **WHEN** a *floor collision* is detected  
   **THEN** vertical movement stops and the *spawned NPC* is anchored at the floor contact point  
   **AND** horizontal movement in non-blocked directions may continue  
   **BUT** the *spawned NPC* does not pass through floor geometry

3. **WHEN** a *wall collision* is detected  
   **THEN** movement in the direction of the wall halts  
   **AND** the *spawned NPC* stops at the wall boundary  
   **BUT** no error is raised and movement in unblocked directions is unaffected

4. **WHEN** both a *floor collision* and a *wall collision* are detected on the same step  
   **THEN** movement halts in all blocked directions; the *spawned NPC* stops at the combined boundary  
   **AND** the GM sees the character stop without any error message

---

### Story: Turn Character towards Target

**Domain terms:**
- *Character Facing Vector* — the current facing direction read before the turn
- *Character Rotation Matrix* — computed from the target bearing and written to memory
- *Memory Interface* — reads the facing vector, writes the rotation matrix
- *Spawned NPC* — changes facing in the game world upon write
- *Movement Execution* — computes and issues the turn

1. **WHEN** the GM triggers Turn to Target on a *spawned NPC*  
   **THEN** the *memory interface* reads the *character facing vector* and the target entity's *character position*  
   **AND** *movement execution* computes the required *character rotation matrix* to face the target  
   **AND** the *memory interface* writes the *character rotation matrix* to process memory  
   **AND** the *spawned NPC* faces the target direction in the COH game world

2. **WHEN** the *spawned NPC* is already facing the target within an acceptable tolerance  
   **THEN** no rotation write is issued; the operation is treated as a no-op  
   **BUT** no error is raised

3. **WHEN** the target entity is not a *spawned NPC* (e.g., a location point)  
   **THEN** *movement execution* computes the bearing from the character's *character position* to the target point and applies the turn  
   **AND** the *spawned NPC* faces the target location

---

### Story: Reset Character Orientation

**Domain terms:**
- *Character Rotation Matrix* — the identity-equivalent rotation matrix written to reset orientation
- *Memory Interface* — writes the reset matrix to process memory
- *Spawned NPC* — returns to default forward-facing orientation in the game world
- *Movement Execution* — computes and issues the reset

1. **WHEN** the GM triggers Reset Character Orientation on a *spawned NPC*  
   **THEN** *movement execution* computes the identity-equivalent *character rotation matrix* (default forward-facing orientation)  
   **AND** the *memory interface* writes the matrix to the *game process*  
   **AND** the *spawned NPC* faces the default forward direction in the COH game world

2. **WHEN** the character is already in the default orientation  
   **THEN** the write is issued but produces no visible change in the game world  
   **BUT** no error or warning is raised; the operation is idempotent

3. **WHEN** the *character rotation matrix* pointer is stale at reset time  
   **THEN** the write is blocked and the pointer is refreshed before the reset is retried  
   **BUT** no stale address is written to

---

## Camera Rig

---

### Story: Deploy Camera Enable and Disable Scripts

**Domain terms:**
- *Camera Enable/Disable Script* — the COH script file deployed to arm or disarm the *camera rig*
- *Game Bridge* — delivers the script to the COH game session
- *Camera Rig* — the virtual camera system armed by the enable script
- *Camera Follow* — terminated when the disable script is deployed

1. **WHEN** the GM activates the *camera rig* for a session  
   **THEN** the *game bridge* deploys the *camera enable/disable script* (enable variant) to the running COH game session  
   **AND** the *camera rig* object appears in the COH game world  
   **AND** camera-relative movement commands (move to camera position, teleport to camera, follow, maneuver-with-camera) become available

2. **WHEN** the GM deactivates the *camera rig*  
   **THEN** the *game bridge* deploys the *camera enable/disable script* (disable variant)  
   **AND** the *camera rig* object is removed from the COH game world  
   **AND** any active *camera follow* is terminated  
   **BUT** previously stored *camera position* readings are not cleared from memory

3. **WHEN** the *camera rig* is already active and the enable script is deployed again  
   **THEN** the script is deployed without error; the *camera rig* remains active  
   **BUT** no duplicate camera objects are created in the game world

---

### Story: Render Camera Rig in Game

**Domain terms:**
- *Camera Rig* — the virtual camera object rendered in the COH game world
- *Camera Enable/Disable Script* — the mechanism that renders the rig
- *Camera Position* — the location in the game world where the rig appears after activation
- *Movement Execution* — requires the rig to be rendered before camera-relative operations

1. **WHEN** the *camera enable/disable script* (enable variant) is successfully deployed  
   **THEN** the *camera rig* is visible as a camera object in the COH game world at the current camera position  
   **AND** the *memory interface* can resolve and read the *camera position* from the rig's process memory location

2. **WHEN** the *camera rig* is rendered and the GM navigates the scene  
   **THEN** the *camera position* in process memory updates to reflect the rig's current location  
   **AND** camera-relative movement commands use the updated *camera position*

3. **WHEN** *movement execution* is attempted before the *camera rig* is rendered  
   **THEN** camera-relative commands (move to camera position, teleport to camera, follow, maneuver-with-camera) are blocked  
   **AND** the GM sees a message indicating the *camera rig* must be activated first  
   **BUT** non-camera movement commands (move to fixed location) are unaffected

---

### Story: Execute Follow Command

**Domain terms:**
- *Camera Follow* — the mode in which the *camera rig* tracks the targeted *spawned NPC*
- *Camera Rig* — the virtual camera that follows the character
- *Spawned NPC* — the entity being followed
- *Character Position* — tracked continuously while *camera follow* is active

1. **WHEN** the GM triggers Follow on a targeted *spawned NPC*  
   **THEN** the *camera rig* enters *camera follow* mode, continuously updating its position to match the *character position* of the targeted *spawned NPC*  
   **AND** the game camera tracks the character's movement in the COH game world

2. **WHEN** *camera follow* is already active on one character and the GM triggers Follow on a different character  
   **THEN** the *camera rig* unfollows the first character and begins following the second  
   **AND** the game camera shifts to track the newly targeted *spawned NPC*  
   **BUT** at no moment does the rig follow two characters simultaneously

3. **WHEN** the followed *spawned NPC* is despawned  
   **THEN** *camera follow* is automatically terminated and *camera detach* is issued  
   **AND** the *camera rig* returns to free-roam mode

4. **WHEN** the *camera rig* is not rendered in game  
   **THEN** the Follow command is rejected with a message indicating the rig must be activated first  
   **BUT** no follow state is entered while the rig is inactive

---

### Story: Execute Camera Detach Command

**Domain terms:**
- *Camera Detach* — the operation that disconnects the *camera rig* from any followed *spawned NPC*
- *Camera Rig* — returns to free-roam mode after detach
- *Camera Follow* — the mode terminated by detach
- *Game Bridge* — executes the detach *slash command*

1. **WHEN** the GM triggers Camera Detach while *camera follow* is active  
   **THEN** the *game bridge* executes the *execute camera detach command* as a *slash command*  
   **AND** *camera follow* mode is terminated  
   **AND** the *camera rig* returns to free-roam mode; the game camera no longer tracks any character

2. **WHEN** Camera Detach is triggered while no *camera follow* is active  
   **THEN** the detach command is issued as a no-op; the *camera rig* remains in free-roam mode  
   **BUT** no error is raised

3. **WHEN** Camera Detach is triggered while *maneuver-with-camera mode* is active  
   **THEN** *maneuver-with-camera mode* is also terminated  
   **AND** the *camera rig* returns to free-roam and the character stops receiving camera-directed movement inputs

---

### Story: Follow Character with Game Camera

**Domain terms:**
- *Camera Follow* — the active tracking mode
- *Camera Rig* — the virtual camera tracking the character
- *Spawned NPC* — the character being followed
- *Character Position* — the position the camera tracks
- *Crowd Manager — Movements* / *Desktop* — surfaces where Follow is accessible

1. **WHEN** the GM activates Follow on a *spawned NPC* via the desktop context menu or crowd manager  
   **THEN** *camera follow* is activated for that character  
   **AND** the *camera rig* moves to match the *character position* continuously  
   **AND** the Follow action label changes to indicate follow is active for that character

2. **WHEN** the *spawned NPC* moves (via any movement command)  
   **THEN** the *camera rig* updates its position to match the new *character position* in real time  
   **AND** the COH game camera remains focused on the character throughout the movement

3. **WHEN** the GM issues a movement command while *camera follow* is active  
   **THEN** both the character moves and the *camera rig* tracks the new position simultaneously  
   **BUT** the movement destination is not changed by the follow mode; movement proceeds normally

---

### Story: Unfollow Character

**Domain terms:**
- *Camera Follow* — the mode being terminated
- *Camera Rig* — returns to free-roam mode
- *Spawned NPC* — the character no longer followed
- *Camera Detach* — the underlying operation executed on unfollow

1. **WHEN** the GM triggers Unfollow on the currently followed *spawned NPC*  
   **THEN** *camera follow* mode is terminated  
   **AND** the *camera rig* stops tracking the *character position* and enters free-roam mode  
   **AND** the action label returns to the default Follow state

2. **WHEN** the GM triggers Unfollow while no character is being followed  
   **THEN** the Unfollow action is a no-op; the *camera rig* remains in free-roam  
   **BUT** no error is raised

3. **WHEN** the GM unfollows and then immediately issues a Move to Camera Position command  
   **THEN** the *camera position* at the moment of the command is read from the now-free-roam *camera rig*  
   **AND** movement proceeds toward that position  
   **AND** the character is not followed back by the camera unless Follow is explicitly re-activated

---

### Story: Activate Maneuver-with-Camera Mode

**Domain terms:**
- *Maneuver-with-Camera Mode* — the movement input mode driving character movement in the camera's facing direction
- *Camera Rig* — must be rendered; provides the bearing for movement steps
- *Movement Execution* — uses the *camera rig's* facing direction as the movement destination bearing
- *Camera Position* — read on each step as the direction reference

1. **WHEN** the GM activates Maneuver-with-Camera Mode via the context menu or crowd manager  
   **THEN** *maneuver-with-camera mode* becomes active and subsequent movement commands drive the *spawned NPC* in the *camera rig's* current facing direction  
   **AND** the mode indicator is shown in the UI

2. **WHEN** a movement command is issued while *maneuver-with-camera mode* is active  
   **THEN** *movement execution* computes the destination using the *camera rig's* current facing bearing rather than a fixed world-space target  
   **AND** the same *distance limit*, *floor collision*, and *wall collision* rules apply as for normal movement

3. **WHEN** the GM rotates the camera while *maneuver-with-camera mode* is active  
   **THEN** the next movement step uses the updated camera facing direction as the bearing  
   **AND** the character pivots to follow the new camera direction on the next command

4. **WHEN** the *camera rig* is not rendered and the GM attempts to activate *maneuver-with-camera mode*  
   **THEN** the activation is blocked and the GM sees a message indicating the *camera rig* must be active first  
   **BUT** no mode state change occurs
