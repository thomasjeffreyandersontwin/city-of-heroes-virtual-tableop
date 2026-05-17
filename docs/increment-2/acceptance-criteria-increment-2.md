# Acceptance Criteria — Increment 2: Character Identities

> Domain sources: `docs/domain/ubiquitous-language-increment-2.md` (primary), `docs/domain/ubiquitous-language-increment-1.md` (boundary concepts: *character*, *crowd*, *COH game directory*, *COH data directory*).
> All domain terms in this file are drawn from those sources. Terms italicized on first use in each story's AC.

---

## Game Bridge Initialization

---

### Load HookCostume DLL from Game Directory

**Domain terms** (vocabulary for this story's AC):
- *HookCostume DLL* — native Win32 DLL in the *COH Game Directory* providing the low-level game API
- *COH Game Directory* — validated file-system path to the COH installation
- *Game Bridge* — application service that owns the DLL load and game connection lifecycle
- *Native Game Bridge* — .NET P/Invoke wrapper that calls DLL entry points

1. **WHEN** the *Game Bridge* starts initialization and the *HookCostume DLL* is present in the *COH Game Directory*  
   **THEN** the *Game Bridge* successfully loads the *HookCostume DLL* into the application process  
   **AND** the *Native Game Bridge* is ready to marshal calls to the DLL

2. **WHEN** the *HookCostume DLL* is absent from the *COH Game Directory* at initialization time  
   **THEN** the *Game Bridge* reports a fatal initialization error identifying the missing DLL  
   **BUT** no further initialization steps are attempted and no *Game Command* is queued

3. **WHEN** the *HookCostume DLL* is present but cannot be loaded (wrong architecture, corrupted file)  
   **THEN** the *Game Bridge* reports a fatal load failure with a descriptive error  
   **BUT** the application does not attempt to call any DLL entry point

4. **WHEN** the *HookCostume DLL* loads successfully  
   **THEN** the *Game Bridge* transitions to the initializing state and proceeds to call *InitGame*  
   **AND** the loaded DLL remains in the process for the duration of the session

5. **WHEN** the *COH Game Directory* is not yet validated at the time the *Game Bridge* attempts to load the DLL  
   **THEN** the *Game Bridge* defers the load until a valid *COH Game Directory* is confirmed  
   **BUT** no load attempt is made against an unvalidated path

---

### Initialize Game Bridge (InitGame)

**Domain terms** (vocabulary for this story's AC):
- *InitGame* — named initialization operation on the *Game Bridge* that calls the DLL init entry point and starts polling
- *Game Bridge* — application service owning the full initialization sequence
- *HookCostume DLL* — must be loaded before *InitGame* is called
- *Game Loaded Event* — the signal fired when polling confirms the COH client is ready

1. **WHEN** the *HookCostume DLL* is loaded and the *Game Bridge* calls *InitGame*  
   **THEN** the DLL init entry point executes successfully  
   **AND** the *Game Bridge* transitions to the polling state, ready to detect when the COH client finishes loading

2. **WHEN** *InitGame* is called and the DLL init entry point reports success  
   **THEN** the *Game Bridge* begins polling the game state at a regular interval  
   **AND** the session waits for the *Game Loaded Event* before allowing any *Game Command*

3. **WHEN** *InitGame* is called before the *HookCostume DLL* is loaded  
   **THEN** the *Game Bridge* rejects the call with an ordering error  
   **BUT** no DLL entry point is invoked and the bridge does not transition to the polling state

4. **WHEN** the DLL init entry point returns a failure code  
   **THEN** the *Game Bridge* reports the *InitGame* failure and halts initialization  
   **BUT** no polling loop is started and no *Game Command* is accepted

5. **WHEN** *InitGame* is called a second time in the same session after the *Game Bridge* has already reached the ready state  
   **THEN** the *Game Bridge* ignores the duplicate call  
   **BUT** no re-initialization of the DLL is performed

---

### Poll until Game Client is Loaded

**Domain terms** (vocabulary for this story's AC):
- *Game Bridge* — owns the polling loop
- *Game Loaded Event* — fired exactly once when polling confirms the COH client is ready
- *Game Command* — must be withheld until the *Game Loaded Event* fires

1. **WHEN** the *Game Bridge* enters the polling state after *InitGame* succeeds  
   **THEN** the *Game Bridge* repeatedly queries the game client's readiness at a fixed interval  
   **AND** each poll that returns not-ready causes the *Game Bridge* to wait and retry

2. **WHEN** a poll returns that the COH client has fully loaded  
   **THEN** the *Game Bridge* stops polling and fires the *Game Loaded Event*  
   **AND** the bridge transitions to the ready state, permitting *Game Commands*

3. **WHEN** polling continues beyond the configured maximum wait time without the game reporting ready  
   **THEN** the *Game Bridge* reports a timeout error to the application  
   **BUT** no *Game Loaded Event* is published and no *Game Commands* are released

4. **WHEN** the *Game Bridge* is in the polling state and the application attempts to issue a *Game Command*  
   **THEN** the command is held pending or rejected with a not-ready indicator  
   **BUT** no *Slash Command* is delivered to the game before the *Game Loaded Event*

5. **WHEN** the *Game Loaded Event* has already fired and a second poll somehow returns not-ready  
   **THEN** the *Game Bridge* does not re-publish the *Game Loaded Event*  
   **AND** the bridge remains in the ready state

---

### Inject Required KeyBinds into Game

**Domain terms** (vocabulary for this story's AC):
- *Game Bridge* — triggers keybind injection after the *Game Loaded Event*
- *Game Loaded Event* — the trigger for injection
- *KeyBind File* — file written to *COH Data Directory* and loaded into COH
- *Slash Command* — `/bind_load_file` delivers the keybind file to the game
- *COH Data Directory* — destination for the *KeyBind File*

1. **WHEN** the *Game Loaded Event* fires  
   **THEN** the *Game Bridge* writes the required *KeyBind File* to the *COH Data Directory*  
   **AND** issues the `/bind_load_file` *Slash Command* to load it into COH  
   **AND** the game engine processes the bindings, making them available for subsequent *Game Commands*

2. **WHEN** the required *KeyBind File* cannot be written to the *COH Data Directory* (permission denied)  
   **THEN** the *Game Bridge* reports a keybind injection failure  
   **BUT** no bind_load_file command is issued against a partial or missing file

3. **WHEN** the *KeyBind File* is written but the `/bind_load_file` command fails to execute  
   **THEN** the *Game Bridge* reports that keybinds could not be loaded  
   **AND** the issue is surfaced to the operator so keybind injection can be retried

4. **WHEN** keybind injection is triggered a second time in the same session (e.g. reinitializing)  
   **THEN** the *Game Bridge* re-writes and re-loads the required *KeyBind File*  
   **AND** the previously loaded bindings are replaced

5. **WHEN** keybind injection completes successfully  
   **THEN** the *Game Bridge* proceeds to the next post-initialization step (costume pack extraction) without waiting for user action

---

### Extract Costume Pack on First Run

**Domain terms** (vocabulary for this story's AC):
- *Game Bridge* — triggers costume pack extraction after the *Game Loaded Event*
- *COH Costumes Directory* — destination for the extracted pack contents
- *Costume File* — the files unpacked from the embedded costume pack

1. **WHEN** the *Game Loaded Event* fires and no costume pack has been extracted in this installation (first run)  
   **THEN** the *Game Bridge* extracts the embedded costume pack into the *COH Costumes Directory*  
   **AND** all packed *Costume Files* are written to the directory and available for use

2. **WHEN** the *COH Costumes Directory* already contains the costume pack files (not first run)  
   **THEN** the *Game Bridge* skips extraction  
   **AND** proceeds to the next initialization step without modifying any existing *Costume Files*

3. **WHEN** the *COH Costumes Directory* does not exist at extraction time  
   **THEN** the *Game Bridge* creates the directory and extracts the pack contents into it  
   **AND** extraction proceeds as if it were a standard first-run flow

4. **WHEN** extraction fails partway through (disk full, permission denied)  
   **THEN** the *Game Bridge* reports an extraction failure identifying the cause  
   **BUT** no partial pack is treated as a complete extraction; the first-run flag is not cleared

5. **WHEN** extraction completes on first run  
   **THEN** the application records that the pack has been extracted so subsequent runs skip this step  
   **AND** control passes to the next post-initialization step

---

### Publish Game Loaded Event

**Domain terms** (vocabulary for this story's AC):
- *Game Loaded Event* — the typed signal published when polling confirms COH is ready
- *Game Bridge* — the publisher; owns the "published exactly once" invariant
- *Game Command* — blocked until this event fires; released after

1. **WHEN** the *Game Bridge* polling loop confirms the COH client is fully loaded  
   **THEN** the *Game Bridge* publishes the *Game Loaded Event* to all registered subscribers  
   **AND** post-initialization steps (keybind injection, costume pack extraction, model list load) are triggered in sequence

2. **WHEN** the *Game Loaded Event* is published  
   **THEN** all *Game Commands* that were held pending are released for execution  
   **AND** new *Game Commands* may be issued immediately without waiting

3. **WHEN** the *Game Loaded Event* has already been published in this session and the polling loop receives another ready confirmation  
   **THEN** the *Game Bridge* does not publish a second *Game Loaded Event*  
   **BUT** the session state remains in the ready condition

4. **WHEN** a subscriber registers for the *Game Loaded Event* after it has already been published  
   **THEN** the late subscriber receives the event notification (or the system signals that the game is already loaded)  
   **AND** the subscriber is not silently skipped

5. **WHEN** the *Game Loaded Event* fails to fire because polling timed out  
   **THEN** no post-initialization steps are triggered  
   **AND** the *Game Bridge* surfaces the timeout error so the operator can restart or retry

---

### Initialize Native Game Bridge

**Domain terms** (vocabulary for this story's AC):
- *Native Game Bridge* — .NET P/Invoke wrapper over the *HookCostume DLL*
- *HookCostume DLL* — must be loaded before the *Native Game Bridge* can execute calls
- *Slash Command* — the primary operation type exposed by the *Native Game Bridge*

1. **WHEN** the *HookCostume DLL* is loaded and the *Game Bridge* initializes the *Native Game Bridge*  
   **THEN** the *Native Game Bridge* successfully binds to the DLL entry points  
   **AND** `ExecuteSlashCommand` and the game state query methods are available for the session

2. **WHEN** the *Native Game Bridge* is initialized before the *HookCostume DLL* is loaded  
   **THEN** initialization fails with a dependency-ordering error  
   **BUT** no P/Invoke call is attempted against an unloaded DLL

3. **WHEN** the *Native Game Bridge* is initialized successfully  
   **THEN** the *Game Bridge* can route *Slash Commands* through `ExecuteSlashCommand` for immediate delivery  
   **AND** game state queries (hovered NPC info, mouse XYZ, collision, game-done state) are ready

4. **WHEN** the *Native Game Bridge* initialization is attempted twice in the same session  
   **THEN** the second initialization is silently ignored  
   **AND** the already-active binding remains in use

5. **WHEN** the *Game Bridge* is shut down at session end  
   **THEN** the *Native Game Bridge* releases its DLL bindings  
   **AND** any subsequent calls to `ExecuteSlashCommand` after shutdown return a not-ready error

---

### Execute Slash Command via DLL

**Domain terms** (vocabulary for this story's AC):
- *Slash Command* — COH in-game command string delivered to the game engine
- *Native Game Bridge* — the P/Invoke layer that marshals *Slash Commands* to the *HookCostume DLL*
- *Game Bridge* — routes commands through the *Native Game Bridge*; enforces the ready precondition

1. **WHEN** the *Game Bridge* is in the ready state and a *Slash Command* string is submitted for immediate execution  
   **THEN** the *Native Game Bridge* marshals the command to the *HookCostume DLL* entry point  
   **AND** the DLL delivers the command to the COH game engine, which processes it

2. **WHEN** a *Slash Command* is submitted before the *Game Loaded Event* has fired  
   **THEN** the command is rejected or held with a not-ready indicator  
   **BUT** no DLL call is made against a game that has not finished loading

3. **WHEN** a null or empty *Slash Command* string is submitted  
   **THEN** the *Native Game Bridge* rejects the call without forwarding it to the DLL  
   **AND** an argument error is reported to the caller

4. **WHEN** the DLL call succeeds but COH reports an unknown command error  
   **THEN** the *Game Bridge* surfaces the COH error to the calling service  
   **AND** the error is logged without crashing the bridge

5. **WHEN** a valid *Slash Command* executes successfully  
   **THEN** the COH game engine processes the command and applies its effect in the game world  
   **AND** control returns to the *Game Bridge* without blocking further commands

---

## KeyBind Execution

---

### Generate KeyBind File for Game Event

**Domain terms** (vocabulary for this story's AC):
- *KeyBind File* — the plain-text file containing *Keybind* entries assembled by the *Game Bridge*
- *Keybind* — a key-to-command mapping entry written into the file
- *Game Command* — the unit of work whose *Slash Commands* are encoded in the *Keybind* entries
- *Slash Command* — the primitive command string embedded in each *Keybind*

1. **WHEN** the *Game Bridge* receives a *Game Command* that requires keybind delivery  
   **THEN** it assembles the correct *Keybind* entries (key name mapped to *Slash Command* chain) for that command  
   **AND** writes them into a *KeyBind File* ready for loading into COH

2. **WHEN** the *Game Command* requires a chain of *Slash Commands* (e.g. target + load costume)  
   **THEN** all commands in the chain are embedded in the same *Keybind* entry in the correct sequence  
   **AND** the resulting *KeyBind File* contains a single entry that executes the full chain on bind load

3. **WHEN** a *Game Command* is submitted with no *Slash Commands* specified  
   **THEN** the *Game Bridge* rejects the request and does not write a *KeyBind File*  
   **AND** an error is reported identifying the empty command

4. **WHEN** the *Slash Command* chain for a single *Keybind* exceeds COH's command-chain length limit  
   **THEN** the *Game Bridge* splits the chain across multiple *Keybind* entries or files  
   **AND** the generated *KeyBind File* executes the full command set when loaded

5. **WHEN** the *KeyBind File* is generated successfully  
   **THEN** the file content is valid for loading via `/bind_load_file` and the *Game Bridge* proceeds to write it to the *COH Data Directory*

---

### Execute Spawn NPC Command

**Domain terms** (vocabulary for this story's AC):
- *Spawn NPC Command* — *Game Command* that creates a *Spawned NPC* at the camera position
- *Spawned NPC* — the COH game-world entity instantiated by this command
- *Model* — the COH NPC archetype name used as the appearance reference
- *KeyBind File* — the delivery vehicle for the *Spawn NPC Command*
- *Game Bridge* — must be in the ready state for the command to execute

1. **WHEN** the *Game Bridge* is ready and the *Spawn NPC Command* is issued with a valid *Model* name and *character* name  
   **THEN** the *Spawn NPC Command* is delivered via a *KeyBind File*  
   **AND** COH spawns a *Spawned NPC* using that *Model* at the current camera position  
   **AND** the *Spawned NPC* is addressable by the *character* name in subsequent *Game Commands*

2. **WHEN** the *Spawn NPC Command* is issued with a *Model* name not present in the *Model List*  
   **THEN** COH fails to spawn the NPC and the *Game Bridge* surfaces the failure  
   **BUT** no *Spawned NPC* with an invalid model exists in the game world

3. **WHEN** a *Spawned NPC* with the same *character* name already exists in the game world  
   **THEN** the *Game Bridge* logs a warning and the duplicate spawn attempt proceeds; COH may create a second NPC or use the existing one per game rules  
   **AND** the behavior is observable (duplicate NPC visible in game) rather than silently corrected

4. **WHEN** the *Spawn NPC Command* is issued but the *Game Bridge* has not yet received the *Game Loaded Event*  
   **THEN** the command is rejected with a not-ready status  
   **BUT** no *KeyBind File* is written and no spawn attempt is made

5. **WHEN** the *Spawn NPC Command* executes successfully  
   **THEN** the *Spawned NPC* is visible in the game world at the camera position  
   **AND** the identity activation pipeline proceeds to the next step (*Target by Name Command* for costume identities, animation for model identities)

---

### Execute Target by Name Command

**Domain terms** (vocabulary for this story's AC):
- *Target by Name Command* — *Game Command* that sets the COH game's current target to a named *Spawned NPC*
- *Spawned NPC* — the named entity in the game world that must be targeted before *Load Costume Command* can run
- *Load Costume Command* — the dependent command that requires a valid target
- *KeyBind File* — delivery mechanism

1. **WHEN** the *Target by Name Command* is issued with the *character's* name after the *Spawned NPC* exists in the game world  
   **THEN** COH sets the game's current target to that *Spawned NPC*  
   **AND** the *Load Costume Command* may now be safely issued

2. **WHEN** the *Target by Name Command* is issued and no *Spawned NPC* with the specified name exists in the game world  
   **THEN** COH sets no target and the current target is unchanged  
   **BUT** any subsequent *Load Costume Command* in the same chain applies to an undefined or previously targeted NPC

3. **WHEN** the *Target by Name Command* is chained immediately before a *Load Costume Command* in the same *KeyBind File*  
   **THEN** COH processes the target step first, then applies the costume to the now-targeted *Spawned NPC*  
   **AND** the costume loads onto the correct *Spawned NPC*

4. **WHEN** the *Target by Name Command* is issued successfully  
   **THEN** the targeted *Spawned NPC* is highlighted as the active target in the COH game view  
   **AND** the *Game Bridge* continues with the remaining steps in the identity activation pipeline

5. **WHEN** the *Target by Name Command* is issued for a *Spawned NPC* that has been despawned between the spawn step and the target step  
   **THEN** COH finds no matching NPC and the target is not set  
   **AND** the *Game Bridge* surfaces the target failure so the calling service can abort or retry

---

### Execute Load Costume Command

**Domain terms** (vocabulary for this story's AC):
- *Load Costume Command* — *Game Command* that applies a *Costume File* to the currently targeted *Spawned NPC*
- *Costume File* — the `.costume` file in the *COH Costumes Directory* whose path is embedded in the command
- *COH Costumes Directory* — the directory where *Costume Files* must reside for COH to read them
- *Spawned NPC* — must be the current game target when the command executes
- *KeyBind File* — delivery vehicle

1. **WHEN** the *Spawned NPC* is the current game target and the *Load Costume Command* is issued with a valid *Costume File* path  
   **THEN** COH reads the *Costume File* and applies the costume to the targeted *Spawned NPC*  
   **AND** the NPC's visible appearance changes in the game world to match the costume

2. **WHEN** the *Load Costume Command* references a *Costume File* path that does not exist in the *COH Costumes Directory*  
   **THEN** COH ignores the command and the *Spawned NPC*'s appearance is unchanged  
   **AND** the *Game Bridge* surfaces the missing file error

3. **WHEN** no *Spawned NPC* is targeted (the *Target by Name Command* failed or was skipped)  
   **THEN** the *Load Costume Command* applies the costume to whatever COH considers the current target, or to nothing  
   **AND** the *Game Bridge* treats the command result as ambiguous and logs a warning

4. **WHEN** the *Load Costume Command* executes successfully  
   **THEN** the *Spawned NPC* displays the loaded costume in the game world  
   **AND** the *Game Bridge* marks the *Costume Identity* as actively rendered

5. **WHEN** the *Load Costume Command* is used to apply a *Ghost Costume File* to a *Ghost NPC*  
   **THEN** the ghost appearance is loaded onto the *Ghost NPC* using the same command pipeline  
   **AND** the *Ghost NPC* displays the ghost material treatment in the game world

---

### Execute Delete NPC Command

**Domain terms** (vocabulary for this story's AC):
- *Delete NPC Command* — *Game Command* that removes a *Spawned NPC* from the game world by name
- *Spawned NPC* — the named game-world entity to remove
- *Ghost NPC* — also removed via this command when the *Ghost Shadow* is cleared

1. **WHEN** the *Delete NPC Command* is issued with the name of an existing *Spawned NPC*  
   **THEN** COH removes the *Spawned NPC* from the game world  
   **AND** the NPC is no longer visible or targetable in the game

2. **WHEN** the *Delete NPC Command* is issued with a name that does not match any *Spawned NPC* in the current game session  
   **THEN** COH silently ignores the command  
   **AND** no error is reported; the operation is treated as a successful no-op

3. **WHEN** the *Delete NPC Command* is used to remove a *Ghost NPC*  
   **THEN** the *Ghost NPC* is removed from the game world  
   **AND** the *Ghost Shadow* is marked as inactive in the identity list

4. **WHEN** the *Delete NPC Command* is issued before the *Game Loaded Event* has fired  
   **THEN** the *Game Bridge* rejects or queues the command  
   **BUT** no delete attempt is made against a game that has not finished loading

5. **WHEN** the *Delete NPC Command* succeeds  
   **THEN** the *Active Identity* flag may be cleared on the associated *Identity* record  
   **AND** the character is marked as not spawned in the *Crowd Tree*

---

## Costume File Management

---

### Store Costume Files in COH Costumes Directory

**Domain terms** (vocabulary for this story's AC):
- *Costume File* — `.costume` file containing character appearance data
- *COH Costumes Directory* — `<coh_dir>/costumes/` target directory
- *Costume Surface* — file path reference stored on a *Costume Identity*

1. **WHEN** HVT writes a *Costume File* for a *Costume Identity*  
   **THEN** the file is created in the *COH Costumes Directory* at the path specified by the *Costume Surface*  
   **AND** the file is readable by the COH game engine via the *Load Costume Command*

2. **WHEN** a *Costume File* write is attempted and the *COH Costumes Directory* does not exist  
   **THEN** HVT creates the directory and then writes the file  
   **AND** the write completes successfully as if the directory had always been present

3. **WHEN** a *Costume File* write is attempted and the *COH Costumes Directory* is read-only or permission-denied  
   **THEN** HVT reports a file write error identifying the directory and the cause  
   **BUT** no partial or zero-byte file is left at the destination path

4. **WHEN** a *Costume File* already exists at the destination path  
   **THEN** HVT overwrites it with the new content  
   **AND** the updated file is available immediately for the next *Load Costume Command*

5. **WHEN** a *Costume File* is stored successfully  
   **THEN** the file path is recorded as the *Costume Surface* on the associated *Costume Identity*  
   **AND** the identity can be activated using that surface path

---

### Create Original-Backup Costume Files

**Domain terms** (vocabulary for this story's AC):
- *Original-Backup Costume File* — protected immutable copy of a character's costume created once before first modification
- *Costume File* — the working file being backed up
- *COH Costumes Directory* — storage location for both working and backup files

1. **WHEN** HVT is about to modify a character's *Costume File* for the first time  
   **THEN** it creates an *Original-Backup Costume File* in the *COH Costumes Directory* before making any changes  
   **AND** the backup file is an exact copy of the unmodified original

2. **WHEN** an *Original-Backup Costume File* already exists for the character  
   **THEN** HVT does not overwrite it, regardless of subsequent modifications to the working *Costume File*  
   **AND** the backup remains the immutable original, available as the source for variant generation

3. **WHEN** HVT cannot write the *Original-Backup Costume File* (permission denied, disk full)  
   **THEN** it halts the modification that would have overwritten the original  
   **AND** reports the backup failure before proceeding  
   **BUT** the working *Costume File* is not modified if its backup cannot be secured first

4. **WHEN** the *Original-Backup Costume File* is created  
   **THEN** its file name follows the backup naming convention (e.g., `guard_original.costume`) distinguishing it from the working file  
   **AND** it is stored in the *COH Costumes Directory* alongside the working file

5. **WHEN** the source *Costume File* does not exist at backup time (new character, no prior costume)  
   **THEN** HVT skips the backup step  
   **AND** no empty backup file is created; the backup will be created the first time a real costume is written

---

### Write Custom KeyBind Files to COH Data Directory

**Domain terms** (vocabulary for this story's AC):
- *KeyBind File* — plain-text file of *Keybind* entries written by the *Game Bridge*
- *COH Data Directory* — `<coh_dir>/data/` where *KeyBind Files* are written
- *Keybind* — key-to-command mapping entry in the file

1. **WHEN** the *Game Bridge* has assembled the *Keybind* entries for a *Game Command*  
   **THEN** it writes the *KeyBind File* to the *COH Data Directory*  
   **AND** the file is fully written and closed before the load instruction is issued

2. **WHEN** the *COH Data Directory* does not exist at write time  
   **THEN** the *Game Bridge* reports a directory not found error  
   **BUT** no partial *KeyBind File* is left in an incomplete state

3. **WHEN** a *KeyBind File* already exists at the target path  
   **THEN** the *Game Bridge* overwrites it with the new content for the current command  
   **AND** the file reflects only the current command's *Keybind* entries after the write

4. **WHEN** the *KeyBind File* write fails (permission denied, disk full)  
   **THEN** the *Game Bridge* reports the write failure  
   **BUT** no load instruction is issued and the failed *Game Command* is not delivered to COH

5. **WHEN** the *KeyBind File* is written successfully  
   **THEN** the file is immediately available for loading via `/bind_load_file`  
   **AND** the *Game Bridge* proceeds to issue the load instruction

---

### Load KeyBind File into Game

**Domain terms** (vocabulary for this story's AC):
- *KeyBind File* — the file to load; must exist on disk before the load instruction
- *Slash Command* — `/bind_load_file <path>` instructs COH to load the file
- *Native Game Bridge* — delivers the load *Slash Command* to the DLL
- *Game Bridge* — orchestrates write-then-load sequence

1. **WHEN** the *KeyBind File* has been written to the *COH Data Directory*  
   **THEN** the *Game Bridge* issues the `/bind_load_file <path>` *Slash Command* via the *Native Game Bridge*  
   **AND** COH loads the file and executes all *Keybind* entries it contains

2. **WHEN** the `/bind_load_file` command is issued against a path where no *KeyBind File* exists  
   **THEN** COH silently ignores the load instruction  
   **AND** the *Game Bridge* surfaces the load failure to the calling service

3. **WHEN** the *KeyBind File* contains a chain of *Slash Commands* in a single *Keybind* entry  
   **THEN** COH executes all commands in the chain sequentially after the file is loaded  
   **AND** each step in the chain (target, load costume, etc.) completes before the next begins

4. **WHEN** the *Game Bridge* is not in the ready state when the load instruction is issued  
   **THEN** the load instruction is rejected  
   **BUT** no `/bind_load_file` call is made against a game that has not reported loaded

5. **WHEN** a *KeyBind File* loads successfully and its commands execute  
   **THEN** the effects of those commands are visible in the COH game world  
   **AND** the *Game Bridge* marks the associated *Game Command* as delivered

---

## Identity Management

---

### Add Identity to Character

**Domain terms** (vocabulary for this story's AC):
- *Identity* — named visual appearance entry in a *Character's* Identities *Option Group*
- *Character* — the entity whose identity list receives the new entry
- *Identity List* — the body panel of the *Crowd Manager — Identities* screen showing identities for the selected *Character*
- *Option Group* — the Identities container on the *Character*

1. **WHEN** the GM selects a *Character* in the *Crowd Tree* and chooses Add in the *Identity List*  
   **THEN** a new *Identity* entry is added to the *Character's* Identities *Option Group*  
   **AND** the *Identity List* displays the new entry with a default name, type unset, and no active or default indicators

2. **WHEN** the GM enters a name for the new *Identity* that is already used by another *Identity* on the same *Character*  
   **THEN** the application rejects the duplicate name with an inline validation message  
   **AND** the new *Identity* is not added until a unique name is provided  
   **BUT** existing *Identities* on the *Character* are unchanged

3. **WHEN** the GM attempts to add a new *Identity* without entering a name  
   **THEN** the application requires a name and displays a validation prompt  
   **BUT** no unnamed *Identity* is created

4. **WHEN** no *Character* is selected in the *Crowd Tree* and the GM attempts to add an *Identity*  
   **THEN** the Add action is disabled in the *Identity List*  
   **AND** no *Identity* is created

5. **WHEN** an *Identity* is successfully added  
   **THEN** the *Identity List* updates immediately to show the new entry  
   **AND** the *Character* data is updated so the new *Identity* persists with the crowd collection

---

### Set Identity Type (Model or Costume)

**Domain terms** (vocabulary for this story's AC):
- *Identity* — the entry whose type is being set
- *Model Identity* — *Identity* subtype backed by a COH model name
- *Costume Identity* — *Identity* subtype backed by a *Costume File*
- *Costume Surface* — the path field shown when type is Costume
- *Identity List* — the panel where type is changed

1. **WHEN** the GM selects an *Identity* in the *Identity List* and sets its type to Model  
   **THEN** the *Identity* is configured as a *Model Identity*  
   **AND** a model name input field is shown for the GM to specify the COH archetype  
   **AND** any previously set *Costume Surface* is cleared

2. **WHEN** the GM sets the *Identity* type to Costume  
   **THEN** the *Identity* is configured as a *Costume Identity*  
   **AND** a *Costume Surface* input field is shown for the GM to specify the `.costume` file path  
   **AND** any previously set model name is cleared

3. **WHEN** the type is changed from Costume to Model on an *Identity* that has an active *Costume Surface*  
   **THEN** the *Costume Surface* is cleared from the *Identity*  
   **AND** the *Identity List* shows the updated type and empty model name field

4. **WHEN** the GM attempts to set the type on an *Identity* that is currently the *Active Identity* (already spawned)  
   **THEN** the application warns that changing type requires despawning the character  
   **AND** if confirmed, the *Spawned NPC* is despawned before the type change is applied

5. **WHEN** the type is set and confirmed  
   **THEN** the *Identity List* row shows the new type indicator (Model or Costume)  
   **AND** the *Character* data is updated immediately

---

### Assign Costume Surface to Identity

**Domain terms** (vocabulary for this story's AC):
- *Costume Surface* — file path referencing the *Costume File* for a *Costume Identity*
- *Costume Identity* — the *Identity* subtype that requires a *Costume Surface*
- *COH Costumes Directory* — the directory in which the referenced *Costume File* must reside
- *Identity List* — the panel where assignment occurs

1. **WHEN** the GM selects a *Costume Identity* in the *Identity List* and provides a valid *Costume File* path  
   **THEN** the *Costume Surface* is saved on the *Identity*  
   **AND** the *Identity List* shows the assigned surface path for that identity

2. **WHEN** the provided *Costume Surface* path does not resolve to an existing file in the *COH Costumes Directory*  
   **THEN** the application shows a validation error indicating the file is not found  
   **AND** the *Identity* retains its previous *Costume Surface* (or remains unassigned if it had none)  
   **BUT** the invalid path is not saved

3. **WHEN** the GM attempts to assign a *Costume Surface* to a *Model Identity*  
   **THEN** the *Costume Surface* field is not available for *Model Identities*  
   **AND** no surface assignment is made

4. **WHEN** the *Costume Surface* assignment is cleared (path removed)  
   **THEN** the *Costume Identity* is marked as missing its surface  
   **AND** attempting to activate the identity is blocked with a "no costume surface" indicator

5. **WHEN** a *Costume Surface* is assigned successfully  
   **THEN** the *Identity* can be activated and the *Load Costume Command* will use this path  
   **AND** the updated *Character* data is persisted

---

### Set Default Identity

**Domain terms** (vocabulary for this story's AC):
- *Default Identity* — the *Identity* automatically activated when the *Character* is first spawned
- *Identity* — the entry receiving or losing the default flag
- *Identity List* — the panel showing the default marker
- *Character* — the entity whose identities are being configured

1. **WHEN** the GM selects an *Identity* and chooses Set Default in the *Identity List*  
   **THEN** the default flag is set on that *Identity*  
   **AND** the *Identity List* shows the default marker on the selected entry  
   **AND** any other *Identity* on the same *Character* that previously held the default flag is cleared

2. **WHEN** a *Character* already has a *Default Identity* and the GM sets a different *Identity* as default  
   **THEN** the previous *Default Identity* loses its default marker in the *Identity List*  
   **AND** the new *Default Identity* gains the marker  
   **AND** exactly one *Identity* carries the default flag at all times after the action

3. **WHEN** the GM removes the default flag from the current *Default Identity* (set default to none)  
   **THEN** no *Identity* on the *Character* carries the default flag  
   **AND** the *Character* will not auto-activate any identity on spawn until a new default is designated

4. **WHEN** the *Character* has no *Identities* and the GM attempts to set a default  
   **THEN** the Set Default action is disabled  
   **AND** no change is made

5. **WHEN** the default flag is set  
   **THEN** the *Character's* data is updated and the default assignment persists across session restarts  
   **AND** the *Crowd Tree* node for the *Character* may display a default identity indicator alongside the character name

---

### Set Active Identity

**Domain terms** (vocabulary for this story's AC):
- *Active Identity* — the *Identity* currently rendered as a *Spawned NPC* in the game world
- *Costume Identity* — activated via spawn + target + load costume sequence
- *Model Identity* — activated via spawn command alone
- *Spawned NPC* — the game-world entity created by activation
- *Game Bridge* — must be in the ready state; executes the activation pipeline
- *Animation* — plays on the *Spawned NPC* after activation completes

1. **WHEN** the GM selects a *Model Identity* and chooses Set Active  
   **THEN** the *Game Bridge* issues the *Spawn NPC Command* with the model name  
   **AND** the *Spawned NPC* appears in the game world at the camera position  
   **AND** the spawn *Animation* plays and the active indicator is shown on that *Identity* in the *Identity List*

2. **WHEN** the GM selects a *Costume Identity* (with a valid *Costume Surface*) and chooses Set Active  
   **THEN** the *Game Bridge* issues the *Spawn NPC Command*, then *Target by Name Command*, then *Load Costume Command* in sequence  
   **AND** the *Spawned NPC* appears with the loaded costume  
   **AND** the spawn *Animation* plays and the active indicator is shown in the *Identity List*

3. **WHEN** the GM sets a new *Active Identity* while another *Identity* is already active  
   **THEN** the previous *Active Identity* is deactivated first (its *Spawned NPC* despawned, *Persistent Abilities* stopped)  
   **AND** the new identity's activation sequence runs after the old NPC is removed

4. **WHEN** the GM attempts to set an *Active Identity* and the *Game Bridge* is not in the ready state  
   **THEN** the Set Active action is blocked with a "game not connected" indicator  
   **BUT** no game commands are issued

5. **WHEN** the GM attempts to activate a *Costume Identity* with no *Costume Surface* assigned  
   **THEN** the application blocks activation and displays a "no costume surface" validation message  
   **BUT** no *Spawn NPC Command* is issued

6. **WHEN** the active indicator is set on the *Identity*  
   **THEN** the active indicator is visible in the *Identity List* and the *Character* node in the *Crowd Tree* shows spawned status

---

### Remove Identity from Character

**Domain terms** (vocabulary for this story's AC):
- *Identity* — the entry being removed from the *Character's* Identities *Option Group*
- *Active Identity* — if removed, the *Spawned NPC* must be despawned first
- *Default Identity* — if removed, the default flag is cleared from the *Character*
- *Identity List* — panel updated after removal

1. **WHEN** the GM selects an *Identity* that is not active and chooses Remove  
   **THEN** the *Identity* is removed from the *Character's* Identities *Option Group*  
   **AND** the *Identity List* no longer shows that entry

2. **WHEN** the GM removes an *Identity* that is the current *Active Identity*  
   **THEN** the application despawns the *Spawned NPC* via the *Delete NPC Command* before removing the *Identity*  
   **AND** the *Identity* is removed after the despawn completes  
   **AND** the *Character* is marked as not spawned in the *Crowd Tree*

3. **WHEN** the GM removes an *Identity* that is the *Default Identity*  
   **THEN** the default flag is cleared; no *Identity* on the *Character* holds the default marker after removal  
   **AND** the *Character* will not auto-activate any identity on next spawn until a new default is set

4. **WHEN** the GM removes the last remaining *Identity* on a *Character*  
   **THEN** the *Identity List* is empty  
   **AND** all identity-specific actions (Set Active, Set Default, Assign Surface) are disabled until a new *Identity* is added

5. **WHEN** the GM removes an *Identity* that has both active and default flags set  
   **THEN** the *Spawned NPC* is despawned, the *Identity* is removed, and both the active and default markers are cleared  
   **AND** all clearing steps are performed as a single atomic operation

---

## Identity Rendering

---

### Load Costume File for Active Identity

**Domain terms** (vocabulary for this story's AC):
- *Costume File* — the `.costume` file applied to the *Spawned NPC*
- *Active Identity* — the *Costume Identity* being rendered
- *Load Costume Command* — the *Game Command* that applies the file
- *COH Costumes Directory* — where the *Costume File* must exist
- *Target by Name Command* — must precede the *Load Costume Command*

1. **WHEN** a *Costume Identity* is activated and the *Spawned NPC* is present in the game world  
   **THEN** the *Game Bridge* issues the *Target by Name Command* to select the *Spawned NPC*  
   **AND** then issues the *Load Costume Command* referencing the *Costume Surface* path  
   **AND** COH applies the *Costume File* to the targeted *Spawned NPC*

2. **WHEN** the *Costume File* referenced by the *Costume Surface* does not exist in the *COH Costumes Directory*  
   **THEN** COH ignores the *Load Costume Command* and the *Spawned NPC* retains its base appearance  
   **AND** the *Game Bridge* reports a missing file error and the *Active Identity* is flagged as failed to render

3. **WHEN** the *Target by Name Command* fails to find the *Spawned NPC* before the *Load Costume Command* is issued  
   **THEN** the costume is not applied to the intended *Spawned NPC*  
   **AND** the *Game Bridge* logs the targeting failure and aborts the load step

4. **WHEN** the *Costume File* is successfully loaded onto the *Spawned NPC*  
   **THEN** the *Spawned NPC's* appearance changes to match the costume in the game world  
   **AND** the *Active Identity* is marked as fully rendered

5. **WHEN** the costume load is triggered as part of an identity switch  
   **THEN** the previous costume or model appearance is replaced by the new *Costume File*  
   **AND** the visual change is visible in the game world immediately after the *Load Costume Command* executes

---

### Spawn Character with Model Identity

**Domain terms** (vocabulary for this story's AC):
- *Model Identity* — the *Identity* being activated; carries a COH model name
- *Spawn NPC Command* — the *Game Command* that creates the *Spawned NPC*
- *Spawned NPC* — appears at the camera position in the game world after this command
- *Game Bridge* — must be in the ready state
- *Model* — the COH NPC archetype used as the appearance

1. **WHEN** the GM activates a *Model Identity* on a *Character* with the *Game Bridge* ready  
   **THEN** the *Game Bridge* issues the *Spawn NPC Command* with the *Model* name and *character* name  
   **AND** the *Spawned NPC* appears in the game world at the current camera position displaying that *Model*

2. **WHEN** the *Model Identity* carries a model name not in the loaded *Model List*  
   **THEN** the activation is blocked with a "model not found" indicator  
   **BUT** no *Spawn NPC Command* is issued with an invalid model name

3. **WHEN** the *Game Bridge* is not in the ready state and the GM activates a *Model Identity*  
   **THEN** the activation is blocked with a "game not connected" indicator  
   **BUT** no game command is issued

4. **WHEN** the *Spawn NPC Command* succeeds  
   **THEN** the *Spawned NPC* is visible at the camera position in the game world  
   **AND** the *Character* is marked as spawned in the *Crowd Tree*  
   **AND** the spawn *Animation* plays on the *Spawned NPC*

5. **WHEN** the spawn command is issued for a *Character* whose *Spawned NPC* name is already present in the game world  
   **THEN** the *Game Bridge* first issues the *Delete NPC Command* for the existing NPC, then re-spawns  
   **AND** the result is a single *Spawned NPC* with the updated *Model Identity*

---

### Switch Active Identity on Spawned Character

**Domain terms** (vocabulary for this story's AC):
- *Active Identity* — switching replaces the current *Active Identity* with a new one
- *Persistent Abilities* — must be stopped before the switch completes
- *Spawned NPC* — the existing NPC is despawned before the new identity is spawned
- *Delete NPC Command* — used to remove the old *Spawned NPC*
- *Game Bridge* — orchestrates the full switch sequence

1. **WHEN** the GM sets a new *Active Identity* on a *Character* that already has an *Active Identity*  
   **THEN** the *Game Bridge* first stops all *Persistent Abilities* on the *Character*  
   **AND** then issues the *Delete NPC Command* to remove the existing *Spawned NPC*  
   **AND** then runs the new identity's full activation sequence (spawn, load costume if costume identity, play animation)

2. **WHEN** the new *Active Identity* is a *Model Identity*  
   **THEN** after the old NPC is removed the *Spawn NPC Command* is issued with the new model name  
   **AND** the new *Spawned NPC* appears at the camera position with the new model

3. **WHEN** the new *Active Identity* is a *Costume Identity*  
   **THEN** the spawn-target-load sequence is run for the new identity after the old NPC is removed  
   **AND** the new *Spawned NPC* displays the new costume in the game world

4. **WHEN** the *Delete NPC Command* for the old identity fails (NPC already gone)  
   **THEN** the *Game Bridge* treats the delete as a no-op and continues with the new identity activation  
   **AND** the switch is not blocked by the stale NPC state

5. **WHEN** the switch completes  
   **THEN** the *Identity List* shows the active indicator only on the new *Active Identity*  
   **AND** the *Character* remains marked as spawned in the *Crowd Tree*  
   **AND** the spawn *Animation* plays on the new *Spawned NPC*

---

### Play Animation on Identity Load

**Domain terms** (vocabulary for this story's AC):
- *Animation* — the spawn animation played on the *Spawned NPC* immediately after identity load
- *Spawned NPC* — the recipient of the animation
- *Active Identity* — the identity whose load triggers the animation
- *Game Bridge* — issues the animation command after spawning

1. **WHEN** an *Identity* activation sequence completes (NPC spawned and costume loaded if applicable)  
   **THEN** the *Game Bridge* plays the spawn *Animation* on the *Spawned NPC*  
   **AND** the animation is visible in the game world

2. **WHEN** no spawn *Animation* is configured for the *Identity* type  
   **THEN** identity load completes without an animation  
   **AND** the *Spawned NPC* is rendered at rest without error

3. **WHEN** the animation command is issued before the *Spawned NPC* is confirmed present in the game world  
   **THEN** the *Game Bridge* waits for the NPC to register before issuing the animation  
   **AND** a brief post-spawn delay is observed if COH requires it before accepting animation commands

4. **WHEN** the animation is triggered during an identity switch  
   **THEN** the animation plays on the new *Spawned NPC* after the new identity's activation completes  
   **AND** no animation plays on the already-removed old NPC

5. **WHEN** the animation command fails (game does not acknowledge)  
   **THEN** the *Game Bridge* logs the failure  
   **AND** the identity is still marked as active; the animation failure does not undo the spawn

---

### Stop Persistent Abilities on Identity Switch

**Domain terms** (vocabulary for this story's AC):
- *Persistent Abilities* — (boundary, owned by Increment 3) abilities with ongoing visual effects tied to the current costume state
- *Active Identity* — the identity being replaced in the switch
- *Character* — the entity whose *Persistent Abilities* are stopped

1. **WHEN** the GM initiates an identity switch on a *Character* that has one or more active *Persistent Abilities*  
   **THEN** all *Persistent Abilities* on that *Character* are stopped before the old *Active Identity* is despawned  
   **AND** the *Persistent Abilities* stop indicators are updated in the ability list

2. **WHEN** the *Character* has no active *Persistent Abilities* at the time of the identity switch  
   **THEN** the stop step is skipped without error  
   **AND** the identity switch proceeds directly to despawning the old *Spawned NPC*

3. **WHEN** a *Persistent Ability* fails to stop during the switch (command not acknowledged)  
   **THEN** the *Game Bridge* logs the failure and continues the switch  
   **AND** the identity switch is not blocked by a failed ability stop

4. **WHEN** all *Persistent Abilities* have been stopped  
   **THEN** the switch sequence continues: old NPC despawned, new *Active Identity* activated  
   **AND** no *Persistent Ability* effects from the old identity are visible on the new *Spawned NPC*

5. **WHEN** the identity switch completes  
   **THEN** previously stopped *Persistent Abilities* remain in their stopped state  
   **AND** the GM may manually reactivate them on the new identity if desired

---

## Ghost Shadows

---

### Superimpose Ghost on Model Character

**Domain terms** (vocabulary for this story's AC):
- *Ghost Shadow* — semi-transparent NPC overlay on a *Model Identity* character
- *Ghost NPC* — the *Spawned NPC* carrying the *Ghost Costume File*
- *Ghost Costume File* — derived from the *Original-Backup Costume File*
- *Ghost Alignment* — positions the *Ghost NPC* to match the *Character*
- *Model Identity* — the only identity type that supports ghost shadows

1. **WHEN** the GM selects a *Character* with an active *Model Identity* and chooses Add Ghost in the *Identity List*  
   **THEN** HVT generates the *Ghost Costume File* from the *Character's* *Original-Backup Costume File*  
   **AND** spawns the *Ghost NPC* in the game world  
   **AND** loads the *Ghost Costume File* onto the *Ghost NPC* via *Load Costume Command*  
   **AND** performs *Ghost Alignment* to co-locate the *Ghost NPC* with the *Character*

2. **WHEN** the GM attempts to add a ghost shadow to a *Character* with an active *Costume Identity*  
   **THEN** the Add Ghost action is disabled for *Costume Identity* characters  
   **BUT** no ghost spawn is attempted

3. **WHEN** the GM attempts to add a ghost shadow to a *Character* that is not currently spawned  
   **THEN** the application blocks the action with a "character not spawned" indicator  
   **BUT** no *Ghost NPC* is created

4. **WHEN** the *Ghost Shadow* is activated successfully  
   **THEN** the ghost indicator is shown on the *Model Identity* entry in the *Identity List*  
   **AND** the *Ghost NPC* is visible in the game world overlaid on the *Spawned NPC*

5. **WHEN** the *Original-Backup Costume File* does not exist at the time of ghost shadow activation  
   **THEN** HVT cannot generate the *Ghost Costume File*  
   **AND** reports a "no original backup found" error  
   **BUT** no partial *Ghost NPC* is spawned

---

### Create Ghost Costume File from Original

**Domain terms** (vocabulary for this story's AC):
- *Ghost Costume File* — *Costume File* derived from the *Original-Backup Costume File* using ghost material treatment
- *Original-Backup Costume File* — immutable source; never modified
- *COH Costumes Directory* — storage location for the resulting *Ghost Costume File*

1. **WHEN** HVT generates a *Ghost Costume File* from the *Character's* *Original-Backup Costume File*  
   **THEN** the ghost material or reduced-opacity treatment is applied to all costume parts in the source  
   **AND** the resulting *Ghost Costume File* is written to the *COH Costumes Directory* with the ghost naming convention

2. **WHEN** the *Original-Backup Costume File* does not exist  
   **THEN** *Ghost Costume File* generation fails with a "missing original backup" error  
   **BUT** no incomplete *Ghost Costume File* is written to disk

3. **WHEN** a *Ghost Costume File* already exists for the *Character*  
   **THEN** HVT regenerates it from the *Original-Backup Costume File*  
   **AND** the existing file is overwritten with the freshly derived version

4. **WHEN** the write to the *COH Costumes Directory* fails during ghost file creation  
   **THEN** HVT reports the write error  
   **BUT** the *Original-Backup Costume File* is not modified

5. **WHEN** the *Ghost Costume File* is created successfully  
   **THEN** it is available at its expected path in the *COH Costumes Directory*  
   **AND** it can be loaded onto the *Ghost NPC* via the *Load Costume Command*

---

### Align Ghost Position and Orientation with Character

**Domain terms** (vocabulary for this story's AC):
- *Ghost Alignment* — operation that matches the *Ghost NPC's* position and facing to the *Character's*
- *Ghost NPC* — the entity being repositioned
- *Spawned NPC* — the *Character's* primary NPC, whose position is the reference
- *Game Bridge* — reads position and facing, then writes to the *Ghost NPC*

1. **WHEN** the *Ghost NPC* has been spawned  
   **THEN** the *Game Bridge* reads the *Character's* current in-game position and facing orientation  
   **AND** writes those values to the *Ghost NPC* so it occupies the same space and faces the same direction

2. **WHEN** the *Character* is not present in the game world at alignment time (despawned between ghost spawn and alignment)  
   **THEN** the *Game Bridge* reports a "character not found" error and does not attempt to align  
   **BUT** the *Ghost NPC* remains in its default spawn position until the alignment is retried

3. **WHEN** the *Ghost NPC* is not found in the game world at alignment time  
   **THEN** the *Game Bridge* reports a "ghost NPC not found" error  
   **BUT** no write operation is attempted on a missing entity

4. **WHEN** *Ghost Alignment* completes successfully  
   **THEN** the *Ghost NPC* is visually co-located with the *Character's* *Spawned NPC*  
   **AND** the ghost overlay effect is visible from the player's camera angle

5. **WHEN** the *Character* moves after the *Ghost Shadow* is active  
   **THEN** *Ghost Alignment* must be re-executed to correct the positional drift between the *Character* and the *Ghost NPC*  
   **AND** without re-alignment the ghost overlay will appear displaced from the character

---

### Remove Ghost from Desktop

**Domain terms** (vocabulary for this story's AC):
- *Ghost Shadow* — the overlay being deactivated
- *Ghost NPC* — the *Spawned NPC* to be removed via *Delete NPC Command*
- *Delete NPC Command* — despawns the *Ghost NPC* by name
- *Identity List* — shows the ghost indicator that is cleared after removal

1. **WHEN** the GM selects a *Character* with an active *Ghost Shadow* and chooses Remove Ghost  
   **THEN** the *Game Bridge* issues the *Delete NPC Command* targeting the *Ghost NPC* by name  
   **AND** the *Ghost NPC* is removed from the game world  
   **AND** the ghost indicator on the *Model Identity* entry in the *Identity List* is cleared

2. **WHEN** the *Ghost NPC* has already been removed from the game world (e.g. game reset) before the Remove Ghost action  
   **THEN** the *Delete NPC Command* is a no-op in COH  
   **AND** the ghost indicator is still cleared in the *Identity List*, resulting in a clean UI state

3. **WHEN** the Remove Ghost action is triggered as part of clearing the *Character* from the desktop  
   **THEN** both the primary *Spawned NPC* and the *Ghost NPC* are despawned in the correct order  
   **AND** neither NPC remains in the game world after the clear operation completes

4. **WHEN** the *Game Bridge* is not in the ready state when Remove Ghost is attempted  
   **THEN** the ghost indicator remains in the *Identity List* and a reconnection is required  
   **AND** the ghost indicator is not cleared until the *Delete NPC Command* can be confirmed as executed

5. **WHEN** the *Ghost Shadow* has been removed  
   **THEN** the Add Ghost action is re-enabled for the *Character* so a new *Ghost Shadow* can be added  
   **AND** no ghost-related state remains on the *Character's* identity record

---

## Costume Variant Generation

---

### Create Persistent-FX Costume Variants

**Domain terms** (vocabulary for this story's AC):
- *Persistent-FX Costume Variant* — *Costume File* derived from the *Original-Backup Costume File* with persistent-ability FX layers
- *Original-Backup Costume File* — immutable source for derivation
- *COH Costumes Directory* — target for the generated variant file
- *Persistent Abilities* — the abilities whose FX are embedded in this variant

1. **WHEN** HVT generates a *Persistent-FX Costume Variant* from the *Character's* *Original-Backup Costume File*  
   **THEN** the persistent-ability FX layers are overlaid on the source costume data  
   **AND** the resulting variant file is written to the *COH Costumes Directory*

2. **WHEN** the *Original-Backup Costume File* does not exist at generation time  
   **THEN** variant generation fails with a "missing original backup" error  
   **BUT** no incomplete variant file is written to disk

3. **WHEN** a *Persistent-FX Costume Variant* already exists for the *Character*  
   **THEN** HVT regenerates it from the *Original-Backup Costume File*  
   **AND** the existing variant is overwritten with the freshly derived version

4. **WHEN** the variant write to *COH Costumes Directory* fails  
   **THEN** the error is reported and the variant is not available  
   **AND** the *Original-Backup Costume File* is not modified

5. **WHEN** the *Persistent-FX Costume Variant* is created successfully  
   **THEN** it is loadable via the *Load Costume Command* onto the *Spawned NPC* when *Persistent Abilities* are active  
   **AND** the variant persists in the *COH Costumes Directory* until regenerated

---

### Create Ghost Costume Files

**Domain terms** (vocabulary for this story's AC):
- *Ghost Costume File* — *Costume File* with ghost material treatment derived from the *Original-Backup Costume File*
- *Original-Backup Costume File* — the source; not modified by this operation
- *COH Costumes Directory* — the output directory

1. **WHEN** HVT creates *Ghost Costume Files* for a *Character*  
   **THEN** the ghost material treatment is applied to the *Original-Backup Costume File* data  
   **AND** the resulting *Ghost Costume File* is written to the *COH Costumes Directory* with the ghost naming convention

2. **WHEN** the *Original-Backup Costume File* is missing  
   **THEN** ghost file creation fails with a descriptive error  
   **BUT** no partial or zero-byte ghost file is left in the *COH Costumes Directory*

3. **WHEN** a *Ghost Costume File* already exists at the target path  
   **THEN** HVT overwrites it with a freshly derived version  
   **AND** the updated ghost file is available for the next *Ghost Shadow* activation

4. **WHEN** the ghost file write succeeds  
   **THEN** the file is present in the *COH Costumes Directory* at the naming-convention path  
   **AND** it can be loaded onto a *Ghost NPC* via the *Load Costume Command*

5. **WHEN** multiple *Characters* each need ghost files  
   **THEN** each *Character's* *Ghost Costume File* is written separately with its own character-specific naming  
   **AND** no two characters' ghost files overwrite each other

---

## Model Browser

---

### Load Available Models from Models.txt

**Domain terms** (vocabulary for this story's AC):
- *Models.txt* — plain-text file in the *COH Game Directory* enumerating NPC model names
- *Model List* — the in-memory collection loaded from *Models.txt*
- *Game Loaded Event* — the trigger for this load
- *COH Game Directory* — contains *Models.txt*

1. **WHEN** the *Game Loaded Event* fires  
   **THEN** HVT reads *Models.txt* from the *COH Game Directory* and loads the *Model List* into memory  
   **AND** all model names and their type classifications are available for *Model Identity* assignment and *Model Browser* display

2. **WHEN** *Models.txt* is absent from the *COH Game Directory* at load time  
   **THEN** HVT reports a "Models.txt not found" fatal initialization error  
   **AND** the *Model List* is empty; *Model Browser* interactions and *Model Identity* assignments are blocked

3. **WHEN** *Models.txt* exists but contains malformed or unparseable lines  
   **THEN** HVT skips the unparseable lines and loads the valid entries  
   **AND** reports how many lines were skipped so the operator can investigate

4. **WHEN** *Models.txt* is present but empty  
   **THEN** the *Model List* is loaded as an empty collection  
   **AND** the *Model Browser* displays an empty list with an informational "no models available" message

5. **WHEN** the *Model List* is loaded successfully  
   **THEN** it is held in memory for the duration of the session  
   **AND** HVT does not re-read *Models.txt* mid-session; the loaded list is the session's source of truth for model names

---

### Create Crowd from COH Model List

**Domain terms** (vocabulary for this story's AC):
- *Model Browser* — the modal screen where the GM selects *Models* and triggers crowd creation
- *Model* — a selected COH NPC archetype
- *Crowd* — the new entity created in the *Crowd Repository*
- *Character* — one generated per selected *Model* in the new *Crowd*
- *Model Identity* — pre-configured on each generated *Character*
- *Model List* — the data source displayed in the *Model Browser*

1. **WHEN** the GM opens the *Model Browser*, selects one or more *Models*, and chooses Create Crowd from Selection  
   **THEN** a new *Crowd* is added to the *Crowd Repository*  
   **AND** one *Character* is created per selected *Model* in the new *Crowd*  
   **AND** each *Character* carries a pre-configured *Model Identity* referencing its selected *Model* name

2. **WHEN** no *Models* are selected in the *Model Browser*  
   **THEN** the Create Crowd from Selection action is disabled  
   **AND** no *Crowd* or *Character* is created

3. **WHEN** the GM confirms crowd creation and the new *Crowd* name conflicts with an existing *Crowd* at the same repository level  
   **THEN** HVT prompts the GM to supply a unique crowd name  
   **AND** creation is held until a unique name is confirmed

4. **WHEN** the crowd creation succeeds  
   **THEN** the new *Crowd* appears in the *Crowd Tree* with all generated *Characters* visible as child nodes  
   **AND** each *Character* node shows the type indicator (Model) for its *Identity*  
   **AND** the *Model Browser* closes and focus returns to the *Crowd Manager — Identities* screen

5. **WHEN** the GM cancels the *Model Browser* without confirming crowd creation  
   **THEN** no *Crowd* or *Character* is created  
   **AND** the *Crowd Repository* is unchanged

---

### Select Models to Include in Crowd

**Domain terms** (vocabulary for this story's AC):
- *Model Browser* — the modal panel showing the *Model List*
- *Model* — a named COH NPC archetype in the list
- *Model List* — the full collection displayed in the *Model Browser*

1. **WHEN** the GM opens the *Model Browser*  
   **THEN** the *Model List* is displayed with each *Model* name and type classification visible  
   **AND** all *Models* start in a deselected state

2. **WHEN** the GM selects a *Model* in the *Model Browser*  
   **THEN** the *Model* is marked as selected with a visible selection indicator  
   **AND** the Create Crowd from Selection button becomes enabled

3. **WHEN** the GM deselects a previously selected *Model*  
   **THEN** the selection indicator is cleared for that *Model*  
   **AND** if no *Models* remain selected, the Create Crowd from Selection button is disabled

4. **WHEN** the GM selects all *Models* in the list  
   **THEN** all entries are marked selected  
   **AND** Create Crowd from Selection is enabled and will generate a *Character* for every *Model* in the list

5. **WHEN** the GM uses the filter/search input in the *Model Browser*  
   **THEN** the displayed *Model List* is filtered to entries matching the search term  
   **AND** previously selected *Models* that are filtered out remain in the selection  
   **AND** clearing the filter restores the full list with selections intact

---

### Generate Characters with Model Identities

**Domain terms** (vocabulary for this story's AC):
- *Character* — the entity generated per selected *Model*
- *Model Identity* — the pre-configured *Identity* placed on each generated *Character*
- *Model* — the COH archetype; its name becomes the default *Character* name and *Model Identity* reference
- *Crowd* — the container for the generated *Characters*

1. **WHEN** the GM confirms crowd creation from a *Model* selection  
   **THEN** HVT generates one *Character* per selected *Model*  
   **AND** each *Character* is named after its *Model* by default  
   **AND** each *Character* has a *Model Identity* pre-configured with that *Model* name

2. **WHEN** two selected *Models* have the same name (duplicates in the *Model List*)  
   **THEN** HVT creates two *Characters* with disambiguated names (e.g. the model name with a numeric suffix)  
   **AND** each *Character* still carries a *Model Identity* referencing the shared *Model* name

3. **WHEN** a generated *Character* name would conflict with an existing *Character* in the target *Crowd*  
   **THEN** HVT automatically suffixes the name to make it unique within the *Crowd*  
   **AND** the *Model Identity* on the *Character* still references the original *Model* name unchanged

4. **WHEN** all *Characters* are generated  
   **THEN** each *Character's* identity list contains exactly one *Model Identity* entry  
   **AND** that entry is set as the *Default Identity* for the *Character*

5. **WHEN** the generated *Characters* are added to the new *Crowd*  
   **THEN** the *Crowd* contains exactly as many *Characters* as *Models* were selected  
   **AND** each *Character* is immediately visible in the *Crowd Tree* under the new *Crowd*

---

### Load Models List for Crowd Creation

**Domain terms** (vocabulary for this story's AC):
- *Model List* — in-memory collection of available *Models*; data source for the *Model Browser*
- *Models.txt* — the source file read to populate the *Model List*
- *Game Loaded Event* — triggers the load
- *Model Browser* — blocked until the *Model List* is loaded

1. **WHEN** the *Game Loaded Event* fires  
   **THEN** HVT reads *Models.txt* from the *COH Game Directory* and populates the *Model List*  
   **AND** the *Model Browser* open action becomes available to the GM

2. **WHEN** the GM attempts to open the *Model Browser* before the *Model List* is loaded  
   **THEN** the *Model Browser* open action is disabled with a "model list not ready" indicator  
   **AND** no empty or partially populated *Model Browser* is shown

3. **WHEN** the *Model List* load fails (file missing or unreadable)  
   **THEN** the *Model Browser* remains unavailable for the session  
   **AND** the error is surfaced with guidance to verify the *COH Game Directory* path

4. **WHEN** the *Model List* is loaded  
   **THEN** it is used as the complete source of available *Models* for the session  
   **AND** any *Model Identity* assignment validates the model name against this loaded list

5. **WHEN** the session ends and a new session begins  
   **THEN** the *Model List* is cleared from memory  
   **AND** *Models.txt* is re-read on the next *Game Loaded Event* to ensure the list reflects the current COH installation
