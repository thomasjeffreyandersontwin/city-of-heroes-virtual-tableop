---
state: ubiquitous-language
increment: 2
scope: Character Identities
date: 2026-05-17
---

# Ubiquitous Language — Increment 2: Character Identities

> Scope: the vocabulary needed to assign visual identities to characters, initialize and communicate with the live COH game engine, manage costume and keybind files on disk, render ghost shadow overlays, and build crowds from available COH models. This increment is the first to touch the live game. Tests validate identity CRUD, game bridge initialization, costume file management, keybind generation, spawn/despawn, ghost shadow lifecycle, and model-list crowd creation.

---

**Terms**:
- **Identity**
  - **identity** — a named visual appearance assigned to a *character*, rendered in the COH game world as a *spawned NPC*; either a *model identity* or a *costume identity*
  - **model identity** — an *identity* backed by a COH NPC model name; spawns the character directly as that NPC model without a separate costume file load
  - **costume identity** — an *identity* backed by a *costume file*; loaded into the game via the *load costume command* after spawn
  - **active identity** — the *identity* currently rendered in the game world for a *character*; at most one identity may be active per character at any time
  - **default identity** — the *identity* automatically activated when a *character* is first spawned; at most one identity per character carries this designation
  - **costume surface** — the file path or identifier of the *costume file* assigned to a *costume identity*
  - **spawned NPC** — the COH game-world entity created when a *character's* active identity is rendered; addressed by the character's name for targeting and costume load operations
- **Game Bridge**
  - **game bridge** — the application service that initializes the COH game engine connection and routes all outbound game communication for the session
  - **HookCostume DLL** — the native Win32 DLL located in the *COH game directory* that provides the low-level API used by the *native game bridge*
  - **native game bridge** — the .NET P/Invoke wrapper that calls *HookCostume DLL* entry points; executes *slash commands* and exposes game state queries to managed code
  - **slash command** — a COH in-game command string (e.g., `/spawnnpc`, `/target_name`, `/loadcostume`) executed via the *native game bridge* or delivered through a *keybind file*
  - **game event** — a typed signal emitted by the *game bridge* to notify registered application components of a significant game-side state change
  - **game loaded event** — the *game event* published once the *game bridge* confirms the COH client has fully loaded and is ready to accept commands
  - **InitGame** — the named initialization operation on the *game bridge* that loads the *HookCostume DLL*, calls the DLL init entry point, and starts the ready-poll loop
- **Costume File**
  - **costume file** — a `.costume` file stored in the *COH costumes directory* that encodes body shape, costume parts, and color assignments for a character or NPC
  - **COH costumes directory** — the `<coh_dir>/costumes/` subdirectory of the *COH game directory* where HVT writes and reads all managed *costume files*
  - **original-backup costume file** — a protected copy of a character's *costume file* created once, before HVT first modifies it; the immutable source for all derived variant files
  - **persistent-FX costume variant** — a *costume file* derived from the *original-backup costume file* by overlaying persistent-ability FX layers
  - **ghost costume file** — a *costume file* derived from the *original-backup costume file* using a ghost material or reduced-opacity treatment; used as the *ghost shadow* appearance
- **KeyBind**
  - **keybind** — a COH key-binding entry mapping a named key to one or more *slash commands*; the execution unit written inside a *keybind file*
  - **keybind file** — a plain-text `.txt` file containing one or more *keybind* entries, written by HVT to the *COH data directory* and loaded into COH to execute game commands
  - **game command** — a unit of game-side work assembled from *slash commands* and delivered via the *native game bridge* or a *keybind file*; the four core commands in this increment are *spawn NPC command*, *target by name command*, *load costume command*, and *delete NPC command*
  - **spawn NPC command** — a *game command* that creates a *spawned NPC* in the game world at the current camera position using the model name or a base model
  - **target by name command** — a *game command* that sets the COH game's current target to the *spawned NPC* whose name matches the *character's* name
  - **load costume command** — a *game command* that applies a *costume file* to the currently targeted *spawned NPC*
  - **delete NPC command** — a *game command* that removes a *spawned NPC* from the COH game world
- **Ghost Shadow**
  - **ghost shadow** — a semi-transparent NPC overlay superimposed on a *model identity* character; displays a costume-based appearance alongside the NPC model in the game world
  - **ghost NPC** — the *spawned NPC* instance carrying the *ghost costume file* that represents the *ghost shadow* in the game world
  - **ghost alignment** — the operation that sets the *ghost NPC's* position and facing to match the *character's* current in-game position and facing exactly
- **Model**
  - **model** — a named COH NPC archetype (e.g., `Skull_Lt_01`, `Clockwork_Gear_01`) available in the game client; used as the appearance reference for a *model identity*
  - **model list** — the full ordered collection of *models* available in the COH installation, loaded from *Models.txt* after the *game loaded event* fires
  - **Models.txt** — a plain-text file in the *COH game directory* that enumerates all available NPC model names; the single source for the *model list*

---

The Character Identities increment is the first in which HVT makes live contact with the COH game engine. The *game bridge* bootstraps this connection by loading the *HookCostume DLL* from the *COH game directory*, running *InitGame*, and polling until the *game loaded event* fires — at which point the application injects required *keybinds*, extracts the *costume pack* into the *COH costumes directory*, and loads the *model list* from *Models.txt*. With the game connection active, each *character* in the *crowd repository* can hold a named list of *identities* in its Identities *option group*. An *identity* is either a *model identity* (referencing a COH NPC model name) or a *costume identity* (backed by a *costume file* stored in the *COH costumes directory*). The GM designates one identity as the *default identity* (activated automatically at spawn) and at most one as the *active identity* (currently rendered in the game world as a *spawned NPC*). Activating a *model identity* issues the *spawn NPC command* directly from the model name. Activating a *costume identity* issues the *spawn NPC command* followed by the *target by name command* and the *load costume command*, delivered via a *keybind file* written to the *COH data directory*. For *model identity* characters, the GM may optionally superimpose a *ghost shadow*: a *ghost costume file* is derived from the character's *original-backup costume file*, a separate *ghost NPC* is spawned, the ghost costume is loaded onto it, and *ghost alignment* positions it exactly over the character. Switching the *active identity* stops any *persistent abilities*, despawns the old *spawned NPC* via the *delete NPC command*, and runs the new identity's activation sequence. The *model list* also powers the *model browser*: the GM selects models from the list to generate a new *crowd* of *characters* each pre-configured with a *model identity*.

---

# Core Domain

## Identity

*Identity* is the named visual configuration that determines how a *character* appears in the COH game world. Each *character* holds zero or more *identities* in its Identities *option group*. An *identity* is either a *model identity* or a *costume identity* — the two differ in their rendering pipeline: a *model identity* drives spawn directly from a COH model name, while a *costume identity* requires loading a *costume file* onto the *spawned NPC* after spawn. At most one *identity* per character is the *active identity* (currently rendered in game); at most one is the *default identity* (activated automatically at spawn). Every identity management action — add, set type, assign surface, set default, set active, remove — operates on the Identities *option group* of the selected *character* shown in the identity list of the *crowd manager — identities* screen.

### identity

- is created on a *character* by the GM supplying a name in the identity list; the name must be unique within the character's Identities *option group*
- holds a type (Model or Costume), an optional *costume surface*, a default flag, and an active flag
- is removed from a *character* by the GM; if the removed *identity* is the *active identity*, the *spawned NPC* is despawned before removal completes
- displays in the identity list with name, type, active indicator, and default marker
- **Invariant:** at most one *identity* per character may carry the active flag; at most one may carry the default flag at any time

### model identity *is a type of* identity

- stores a COH model name as its appearance reference instead of a *costume surface*
- triggers the *spawn NPC command* directly using the model name when activated; no separate *load costume command* is needed
- supports *ghost shadow* overlay — the GM may superimpose a *ghost shadow* on a *spawned NPC* bearing this identity
- **Invariant:** the model name must resolve to an entry in the loaded *model list*; an unresolvable name must not be accepted at assignment time

### costume identity *is a type of* identity

- stores a *costume surface* referencing the path of its *costume file* in the *COH costumes directory*
- triggers the *spawn NPC command* followed by the *target by name command* and *load costume command* when activated; the costume is applied to the *spawned NPC* after spawn completes
- does not support *ghost shadow* overlay; ghost shadows are exclusive to *model identity* characters

### active identity

- is a property of *identity* — the boolean flag marking the identity currently rendered in the game world
- when set, triggers the full activation sequence: spawn *spawned NPC*, load costume (for *costume identity*), play spawn *animation*
- when cleared (by a new identity being set active or the character being despawned), triggers stop of any *persistent abilities* and despawn of the *spawned NPC* via *delete NPC command*
- **Invariant:** exactly zero or one *identity* per character carries the active flag at any time; setting a new active identity automatically clears the previous one before the new activation sequence begins

### default identity

- is a property of *identity* — the boolean flag marking the identity activated automatically when the *character* is first spawned to the desktop
- is displayed with a default marker in the identity list in the *crowd manager*
- **Invariant:** at most one *identity* per character may carry the default flag; it may be cleared without assigning another, leaving no default

### costume surface

- is a property of *costume identity* — the file path identifying the *costume file* that defines the identity's appearance
- is assigned via the assign-surface action in the identity list
- must resolve to an existing *costume file* in the *COH costumes directory* at the time the identity is activated

### spawned NPC

- is the COH game-world entity instantiated when a *character's* active identity is rendered via the *spawn NPC command*
- is addressed by the *character's* name for all subsequent *game commands* (*target by name*, *load costume*, *delete NPC*, *ghost alignment*)
- is removed from the game world via the *delete NPC command* when the *character* is cleared from the desktop or when the active identity is switched
- **Invariant:** a *spawned NPC* must exist before any *load costume command* or *ghost alignment* can be applied to it; the *target by name command* must succeed before the *load costume command* is issued

### Decisions made

- `identity` is a concept: distinct identity (name, unique within the character's option group), state (type, active flag, default flag, costume surface), behavior (activate → spawn pipeline, deactivate → despawn), and invariants
- `model identity` and `costume identity` are subtypes (not type-property instances) because their activation pipelines are behaviorally distinct: model identity spawns directly from model name; costume identity requires a separate load-costume step — the difference changes what the thing *does*, not just the data it carries
- `active identity` and `default identity` are properties (boolean flags) on *identity*, documented as stub headings because their invariants are directly testable
- `costume surface` is a property of *costume identity*, not a separate concept; documented as a stub because its path-resolution rule is a testable invariant
- `spawned NPC` is a concept: distinct identity (named game-world entity), state (present/absent in the game world), behavior (targeted, has costume applied, despawned); it is the game-side realization of an *active identity*

### References

**Ref — thin-slicing.md (Increment 2: identity stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 82–92
Extract: Add Identity, Set Identity Type, Assign Costume Surface, Set Default Identity, Set Active Identity, Remove Identity, Load Costume File, Spawn Character with Model Identity, Switch Active Identity, Play Animation on Identity Load, Stop Persistent Abilities on Identity Switch

**Ref — story-map.md (Manage Character Identities epic)**
Source: docs/stories/story-map.md
Locator: lines 91–109
Extract: Configure Identity · Render Identity in Game · Manage Ghost Shadows sub-epics

**Ref — initial-ia.md (crowd manager — identities — identity list)**
Source: docs/ux/initial-ia.md
Locator: lines 63–104
Extract: identity list — name · type · active · default; actions: add · remove · set-default · set-active · add ghost · assign-surface · set-type

**Ref — story-map.md (consolidation notes — Identity Types)**
Source: docs/stories/story-map.md
Locator: lines 308–313
Extract: Model: NPC model name, ghost shadow, spawns with model directly; Costume: .costume file, original/persistent/ghost variants, loaded via LoadCostume after spawn

---

## Game Bridge

The *game bridge* is the application service responsible for opening and maintaining the live connection between HVT and the COH game engine. It owns the full initialization sequence — loading the *HookCostume DLL* from the *COH game directory*, running *InitGame*, polling until the COH client reports ready, injecting required *keybinds* and extracting the *costume pack* on first run — and it is the sole routing point for all outbound game communication: *slash commands* executed directly via the *native game bridge* and *game commands* delivered through *keybind files*. The *game bridge* fires the *game loaded event* once initialization completes, signaling the rest of the application that game-side operations are safe to perform.

### game bridge

- loads the *HookCostume DLL* from the *COH game directory* on application startup before any game operation is attempted
- runs *InitGame* on the *native game bridge* to initialize the DLL and establish the in-process communication channel with COH
- polls the game state at a regular interval until the COH client reports loaded, then fires the *game loaded event*
- injects required *keybinds* into the game and extracts the *costume pack* immediately after the *game loaded event* fires
- loads the *model list* from *Models.txt* after the *game loaded event* fires so it is available before any *model identity* assignment or *model browser* interaction
- routes all outbound *slash commands* requiring immediate execution through the *native game bridge*
- generates and writes *keybind files* for *game commands* that require COH's keybind execution path, then issues the load instruction
- **Invariant:** no *game command* or *slash command* may be issued before the *game loaded event* has fired; any attempt before initialization completes must be rejected or queued

### HookCostume DLL

- is the native Win32 shared library located in the *COH game directory* that provides the low-level API for game communication
- is loaded into the application process by the *game bridge* at startup; must be present in the *COH game directory* for any bridge operation to proceed
- exposes entry points called via the *native game bridge*: init, slash command execution, game state queries (hovered NPC info, mouse XYZ position, collision detection, game-done state)
- **Invariant:** the *HookCostume DLL* must be successfully loaded before *InitGame* is called; a missing or unloadable DLL is a fatal initialization error

### native game bridge

- is the .NET P/Invoke layer that marshals managed-code calls into calls on the *HookCostume DLL* native API
- exposes `ExecuteSlashCommand`, `InitGame`, and game state query methods to the managed application
- is activated after the *HookCostume DLL* is confirmed loaded; it cannot execute commands before the DLL reports ready

### slash command

- is a COH in-game text command string executed by the game engine when delivered
- is the primitive unit of all game communication; every *game command* is composed of one or more *slash commands*
- is either executed immediately via the *native game bridge* or embedded inside a *keybind file* for delivery through COH's keybind execution path

### game event

- is a typed signal emitted by the *game bridge* to notify registered application components of a significant game-side state change
- is the decoupling mechanism between the *game bridge* and the identity, costume, and model services that depend on game readiness

### game loaded event

- is the *game event* published by the *game bridge* when polling confirms the COH client has fully loaded and is ready to accept commands
- triggers all post-initialization steps: keybind injection, costume pack extraction, model list load
- **Invariant:** the *game loaded event* is published exactly once per session; subsequent successful polls after the first confirmation do not re-publish it

### InitGame

- is a property of *game bridge* — the named initialization operation that calls the *HookCostume DLL*'s init entry point and starts the ready-poll loop
- completes when polling confirms the game is loaded; on success the *game bridge* transitions to the ready state and fires the *game loaded event*

### Decisions made

- `game bridge` is a concept: distinct state (uninitialized → initializing → ready), behavior (load DLL, poll, inject keybinds, route commands), and invariants; the single boundary between the managed application and the native game engine
- `HookCostume DLL` is a concept: distinct identity (named file in the game directory), state (loaded/not loaded), behavior (exposes DLL API), invariants (must be loaded before InitGame)
- `native game bridge` is a concept distinct from *game bridge*: it owns the P/Invoke boundary behavior (marshal calls, expose DLL surface); the *game bridge* orchestrates, the *native game bridge* executes
- `slash command` is a concept: distinct identity (command string), routing rules (direct vs. keybind), and a dual delivery path — not just a string property of the game bridge
- `game event` is a concept: distinct identity (typed signal), publication behavior, decoupling role; not just a void notification call
- `game loaded event` is treated as a concept block (not merely an instance stub) because its invariant — published exactly once per session — is a directly testable behavioral rule
- `InitGame` is classified as a property of *game bridge* (a named operation it owns); documented as a stub heading so the term is traceable

### References

**Ref — thin-slicing.md (Increment 2: game bridge initialization stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 65–81
Extract: Load HookCostume DLL, Initialize Game Bridge (InitGame), Poll until Game Client is Loaded, Inject Required KeyBinds, Extract Costume Pack, Publish Game Loaded Event, Initialize Native Game Bridge, Execute Slash Command via DLL

**Ref — story-map.md (Launch and Initialize Session — Initialize Game Connection)**
Source: docs/stories/story-map.md
Locator: lines 18–28
Extract: Load HookCostume DLL, Initialize Game Bridge, Poll until Game Client is Loaded, Inject Required KeyBinds, Extract Costume Pack, Load Models List, Publish Game Loaded Event

**Ref — story-map.md (Communicate with Game Engine — Bridge via HookCostume DLL)**
Source: docs/stories/story-map.md
Locator: lines 216–223
Extract: Initialize Native Game Bridge, Execute Slash Command via DLL, Query Hovered NPC Info, Query Mouse XYZ, Check Game Done State

---

## Costume File

The *costume file* is the file-system artifact that carries a character's or NPC's appearance data — body shape, costume parts, and color assignments — in COH's native format. HVT stores all managed *costume files* in the *COH costumes directory* and maintains three categories of derived file alongside each character's primary costume: an *original-backup costume file* created before HVT first modifies the costume, a *persistent-FX costume variant* carrying persistent-ability visual layers, and a *ghost costume file* used by the *ghost shadow* overlay. Every *load costume command* references a specific *costume file* in the *COH costumes directory*.

### costume file

- is a `.costume` file stored in the *COH costumes directory* encoding body type, costume parts, and color assignments in COH's native format
- is written to the *COH costumes directory* by HVT when a *costume identity* surface is assigned or when variant files are generated
- is read by COH when the *load costume command* targets the currently selected *spawned NPC*
- **Invariant:** a *costume file* must exist at its recorded *costume surface* path at the time the *load costume command* is issued; a missing file causes the command to fail silently in COH

### COH costumes directory

- is the `<coh_dir>/costumes/` subdirectory derived from the *COH game directory*
- is the storage location for all *costume files* managed by HVT: working costumes, *original-backup costume files*, *persistent-FX costume variants*, and *ghost costume files*
- must exist and be writable before any costume file write operation can proceed; HVT creates it on first run if absent

### original-backup costume file

- is a protected copy of a character's primary *costume file* created once, the first time HVT writes to or modifies that character's costume
- is never overwritten after creation; it is the immutable source from which *persistent-FX costume variants* and *ghost costume files* are derived
- is stored in the *COH costumes directory* with a naming convention that distinguishes it from the active working file (e.g., `guard_original.costume`)
- **Invariant:** the *original-backup costume file* is written exactly once per character; subsequent primary costume modifications do not overwrite the backup

### persistent-FX costume variant

- is a *costume file* derived from the *original-backup costume file* by overlaying the visual FX layers required for a *persistent ability*
- is loaded onto the *spawned NPC* when a *persistent ability* is active, keeping persistent FX visible even as the *active identity* changes
- is regenerated whenever the *original-backup costume file* is replaced

### ghost costume file

- is a *costume file* derived from the *original-backup costume file* by applying ghost material or reduced-opacity treatment to all costume parts
- is loaded onto the *ghost NPC* when a *ghost shadow* is superimposed on a *model identity* character
- is stored in the *COH costumes directory* with a naming convention distinguishing it from the primary and backup files (e.g., `guard_ghost.costume`)

### Decisions made

- `costume file` is a concept: distinct identity (named `.costume` file on disk), state (present/absent, contents), behavior (written, read, modified), invariants (must exist at path when command is issued)
- `COH costumes directory` is a concept in this increment (unlike *COH data directory* in Increment 1 which had no active write behavior): the *COH costumes directory* is actively written to across six stories in this increment and carries its own prerequisite (exist and be writable); the increased behavioral weight warrants a concept block rather than a property stub
- `original-backup costume file` is a concept: distinct identity (created once, immutable), distinct role (source for all variant generation), clear invariant; not just a naming convention
- `persistent-FX costume variant` and `ghost costume file` are subordinate concepts, not subtypes of *costume file*: they share the same file format and storage location; they differ in *derivation context and use purpose*, not in fundamental behavior as costume files

### References

**Ref — thin-slicing.md (Increment 2: costume file stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 78–81, 97–98
Extract: Store Costume Files in COH Costumes Directory, Create Original-Backup Costume Files, Create Persistent-FX Costume Variants, Create Ghost Costume Files

**Ref — story-map.md (Manage Game Data and Files — Manage Costume Files)**
Source: docs/stories/story-map.md
Locator: lines 256–260
Extract: Store Costume Files, Create Original-Backup Costume Files, Create Persistent-FX Costume Variants, Create Ghost Costume Files

---

## KeyBind

The *keybind* is COH's built-in mechanism for mapping a key press to one or more *slash commands* that the game engine executes. HVT exploits this mechanism not for interactive shortcuts but as a command-delivery channel: it generates a *keybind file* for each *game command*, writes it to the *COH data directory*, and issues a `/bind_load_file` instruction via the *native game bridge* to make COH load and execute the bindings. The four *game commands* central to Increment 2 are the *spawn NPC command*, *target by name command*, *load costume command*, and *delete NPC command*, which together make up the activation and deactivation pipeline for every *identity*.

### keybind

- is a single entry in a *keybind file* mapping a named key to a *slash command* or a chain of *slash commands*
- is generated by the *game bridge* to encode a specific *game command* payload before being written to a *keybind file*
- is read and executed by the COH game engine when the containing *keybind file* is loaded via `/bind_load_file`

### keybind file

- is a plain-text `.txt` file written to the *COH data directory* containing one or more *keybind* entries
- is loaded into COH by issuing the `/bind_load_file <path>` *slash command* via the *native game bridge*, causing COH to execute all keybinds in the file
- is written fresh for each *game command* batch; HVT does not maintain a persistent keybind state across commands
- **Invariant:** the *keybind file* must be fully written to disk before the load instruction is issued; loading a partially written or absent file produces undefined behavior in COH

### game command

- is a unit of game-side work composed from one or more *slash commands*, assembled by the *game bridge* from the command type and target *character* or *spawned NPC* name
- is delivered either directly via the *native game bridge* (immediate slash command execution) or via a *keybind file* (COH keybind execution path)
- **Invariant:** a *game command* that targets a *spawned NPC* by name will fail silently if no NPC with that name exists in the current game session; the *target by name command* must precede any *load costume command*

### spawn NPC command

- is a *game command* that creates a *spawned NPC* in the COH game world at the current camera position
- carries the model name (for *model identity*) or a base model placeholder for subsequent costume loading (for *costume identity*)
- is the first command issued in the activation sequence for any *identity*

### target by name command

- is a *game command* that sets the COH game's current target to the *spawned NPC* whose name matches the provided *character* name
- must succeed before any *load costume command* is issued, because COH applies costume loads to the currently targeted NPC
- **Invariant:** if no *spawned NPC* with the target name exists, the *target by name command* sets no target and subsequent *load costume commands* in the chain apply to an undefined target

### load costume command

- is a *game command* that applies a *costume file* to the currently targeted *spawned NPC*
- is delivered via a *keybind file* containing the `/loadcostume <path>` *slash command*
- is issued after *target by name command* confirms the correct NPC is selected
- is used for *costume identity* activation, *persistent-FX costume variant* application, and *ghost costume file* loading onto the *ghost NPC*

### delete NPC command

- is a *game command* that removes a *spawned NPC* from the COH game world
- is issued when a *character* is cleared from the desktop or when the *active identity* is switched, prior to spawning the new identity
- **Invariant:** if no *spawned NPC* with the target name exists, the *delete NPC command* is a no-op; the game ignores the command without error

### Decisions made

- `keybind` is a concept: distinct unit (key→command mapping), distinct role in the delivery pipeline (COH's native execution mechanism); not just a text property of the keybind file
- `keybind file` is a concept: distinct identity (named file on disk), state (not-written / written / loaded), behavior (written, load-triggered, executes keybinds), invariants (must exist before load)
- `game command` is a concept: distinct composition (assembled from slash commands), routing decision (direct vs. keybind), targeting rules, and sequencing constraints between command types
- `spawn NPC command`, `target by name command`, `load costume command`, `delete NPC command` are subordinate concepts under *game command*: they share the same composition-and-delivery structure but differ in payload and ordering constraints — each documented separately because the sequencing invariants (e.g., target before load) are directly testable per command type
- Scope-fit: *keybind* is fully introduced and owned in this increment; no keybind delivery existed in Increment 1

### References

**Ref — thin-slicing.md (Increment 2: keybind and game command stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 82–83, 84–86
Extract: Generate KeyBind File for Game Event, Execute Spawn NPC Command, Execute Target by Name Command, Execute Load Costume Command, Execute Delete NPC Command, Write Custom KeyBind Files to COH Data Directory, Load KeyBind File into Game

**Ref — story-map.md (Communicate with Game Engine — Execute Game Commands via KeyBinds)**
Source: docs/stories/story-map.md
Locator: lines 224–234
Extract: Generate KeyBind File, Execute Spawn NPC Command, Execute Target by Name Command, Execute Delete NPC Command, Execute Load Costume Command

---

## Ghost Shadow

The *ghost shadow* is a semi-transparent NPC overlay superimposed on a *model identity* character visible in the COH game world. When the GM activates the ghost shadow for a character, HVT generates a *ghost costume file* from the character's *original-backup costume file*, spawns a separate *ghost NPC*, applies the ghost costume to it via the *load costume command*, and aligns the *ghost NPC* to the character's exact position and facing via *ghost alignment*. The *ghost shadow* provides a visual bridge between the raw NPC model appearance and the costume-based look the GM wants to present, without replacing the underlying *model identity*.

### ghost shadow

- is associated with exactly one *model identity* character; *costume identity* characters do not support ghost shadows
- is activated by the GM via the "Add Ghost" action in the identity list of the *crowd manager — identities* screen
- persists in the game world alongside the character's primary *spawned NPC* until the GM removes it or the character is cleared from the desktop
- is removed via the remove ghost action, which issues the *delete NPC command* for the *ghost NPC* and clears the ghost indicator in the identity list
- **Invariant:** a *ghost shadow* can only be activated when the associated *character* is currently spawned with an active *model identity*; attempting to add a ghost shadow to an unspawned character must be rejected

### ghost NPC

- is the *spawned NPC* instance carrying the *ghost costume file* that visually represents the *ghost shadow* in the COH game world
- is spawned via the *spawn NPC command* using a neutral base model, immediately followed by *target by name command* and *load costume command* to apply the *ghost costume file*
- is aligned immediately after spawn to the *character's* current position and facing via *ghost alignment*
- is despawned via the *delete NPC command* when the *ghost shadow* is removed or the character is cleared from the desktop

### ghost alignment

- is the operation that reads the *character's* current in-game position and facing orientation, then writes those same values to the *ghost NPC*
- is performed immediately after the *ghost NPC* is spawned to ensure the overlay starts co-located with the character
- must be re-executed whenever the character moves, because positional drift between the character and the ghost makes the overlay visually incorrect

### Decisions made

- `ghost shadow` is a concept: distinct identity (associated with one model identity character), state (active/inactive, ghost indicator in UI), behavior (activate → generate file → spawn ghost NPC → align; remove → despawn ghost NPC), invariants (only valid on spawned model identity characters)
- `ghost NPC` is a concept distinct from the character's primary *spawned NPC*: it is a separately spawned entity with a different name, the ghost costume as its appearance, its own lifecycle, and the alignment requirement; not just a property of *ghost shadow*
- `ghost alignment` is a concept: it has distinct behavior (read position/facing → write to ghost NPC), a distinct trigger (immediately after ghost spawn, and after character movement), and a correctness invariant (ghost must remain co-located with character)
- `ghost costume file` is classified under the Costume File KA (where it belongs as a costume management artifact); referenced here as the appearance source for the *ghost NPC*

### References

**Ref — thin-slicing.md (Increment 2: ghost shadow stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 93–98
Extract: Superimpose Ghost on Model Character, Create Ghost Costume File from Original, Align Ghost Position and Orientation with Character, Remove Ghost from Desktop, Create Ghost Costume Files

**Ref — story-map.md (Manage Ghost Shadows sub-epic)**
Source: docs/stories/story-map.md
Locator: lines 105–109
Extract: Superimpose Ghost on Model Character, Create Ghost Costume File from Original, Align Ghost Position and Orientation with Character, Remove Ghost from Desktop

**Ref — initial-ia.md (identity list — add ghost action)**
Source: docs/ux/initial-ia.md
Locator: line 88
Extract: identity list actions include add ghost

---

## Model

A *model* is a named COH NPC archetype available in the game client that can be instantiated as a *spawned NPC* without any *costume file*. The *model list* is the full catalog of available models, loaded from *Models.txt* in the *COH game directory* once the *game loaded event* fires. The *model list* powers two flows: the GM assigns a specific model name when creating a *model identity* on a *character*, and the GM browses the *model list* in the *model browser* to select models and generate a new *crowd* of *characters* each pre-configured with a *model identity*.

### model

- is a named COH NPC archetype identified by its archetype string (e.g., `Skull_Lt_01`, `Clockwork_Gear_01`)
- is referenced by name in a *model identity's* appearance field; the *spawn NPC command* uses this name to instantiate the *spawned NPC*
- carries a type classification (e.g., villain group, hero, civilian) displayed as the type column in the *model browser*

### model list

- is the ordered collection of all *models* available in the current COH installation, loaded from *Models.txt*
- is loaded once after the *game loaded event* fires and held in memory for the session; it is not re-read mid-session
- is the data source for the *model browser's* model list panel and for validating *model identity* name assignments
- **Invariant:** the *model list* must be loaded before any *model browser* interaction or *model identity* name assignment is permitted; operations against an unloaded model list must be rejected

### Models.txt

- is a plain-text file located in the *COH game directory* that enumerates all available NPC model names, one per line
- is the single authoritative source for the *model list*; HVT reads it on initialization and does not modify it
- **Invariant:** *Models.txt* must be present in the *COH game directory*; absence or unreadability prevents the *model list* from loading and must be reported as a fatal initialization error

### Decisions made

- `model` is a concept: distinct identity (named archetype), referenced by name in *model identity* and *spawn NPC command*, carries type classification; not just a string property
- `model list` is a concept: distinct identity (ordered collection), state (loaded/not loaded), behavior (loaded from Models.txt on init, queried by model browser and model identity validation), invariants (must be loaded before model-dependent operations); analogous to *crowd collection* in Increment 1 — a loaded, in-memory collection with its own readiness state
- `Models.txt` is a concept: distinct identity (named file in game directory), state (present/absent), behavior (read on initialization); the authoritative persistent store for its data type, analogous to the *crowd repository* JSON in Increment 1
- Scope-fit: the *model list* and *model browser* flow are explicitly scoped to Increment 2 in thin-slicing.md; all three concepts are core here

### References

**Ref — thin-slicing.md (Increment 2: model list stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 99–103
Extract: Load Available Models from Models.txt, Create Crowd from COH Model List, Select Models to Include in Crowd, Generate Characters with Model Identities, Load Models List for Crowd Creation

**Ref — story-map.md (Build Crowds from Game Models sub-epic)**
Source: docs/stories/story-map.md
Locator: lines 55–59
Extract: Create Crowd from COH Model List, Load Available Models from Models.txt, Select Models to Include in Crowd, Generate Characters with Model Identities

**Ref — initial-ia.md (model browser screen)**
Source: docs/ux/initial-ia.md
Locator: lines 275–307
Extract: model browser — model list: model name · type; actions: select · deselect · create crowd from selection

---

# Boundary Domain

## Character

Owned by: Character and Crowd Library (Increment 1)

- holds an Identities *option group* that this increment populates with *identity* entries
- provides the character name used as the *spawned NPC* name in all *game commands*
- appears in the *crowd tree* with a type indicator (Model/Costume) and spawned/active status visible once identities are assigned in Increment 2

### Decisions made

- *character* is a boundary concept: lifecycle, CRUD, and crowd membership are fully defined by Increment 1; this increment depends on *character* as the host for *identities* and as the name source for *game commands*

### References

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

---

## Crowd

Owned by: Character and Crowd Library (Increment 1)

- is the container from which the GM selects *characters* for identity assignment in the *crowd tree*
- is created by the *model browser* crowd-creation flow: the GM selects *models*, confirms, and a new *crowd* is added to the *crowd repository* containing *characters* pre-configured with *model identities*

### Decisions made

- *crowd* is a boundary concept: its lifecycle is fully owned by Increment 1; this increment reads the *crowd tree* for character selection and writes a new *crowd* via the model browser flow

### References

**Ref — ubiquitous-language-increment-1.md (Crowd KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Crowd KA section

---

## COH Game Directory

Owned by: Character and Crowd Library (Increment 1)

- provides the base path from which the *HookCostume DLL* is located and loaded by the *game bridge*
- provides the *COH costumes directory* (`<coh_dir>/costumes/`) where all *costume files* are stored
- provides the *COH data directory* (`<coh_dir>/data/`) where *keybind files* are written
- contains *Models.txt*, read by the *model list* loader after *game bridge* initialization

### Decisions made

- *COH game directory* is a boundary concept: validated and stored in Increment 1; this increment reads its derived paths but does not re-validate or reconfigure it

### References

**Ref — ubiquitous-language-increment-1.md (COH Game Directory KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: COH Game Directory KA section

---

## Persistent Ability

Owned by: Animated Abilities (Increment 3)

- must be stopped when the *active identity* is switched on a *spawned character*, because its FX are tied to the current costume state
- the stop operation is triggered by the identity activation service when it clears the *active identity* flag on the previous identity

### Decisions made

- *persistent ability* is a boundary concept for Increment 2: the "Stop Persistent Abilities on Identity Switch" story is in scope here, but the full lifecycle of a persistent ability (create, activate, persist, stop, reactivate) belongs to Increment 3; only the stop-on-identity-switch behavior is touched here

### References

**Ref — thin-slicing.md (Increment 2 — Stop Persistent Abilities on Identity Switch)**
Source: docs/stories/thin-slicing.md
Locator: line 91
Extract: Stop Persistent Abilities on Identity Switch

**Ref — story-map.md (Manage Animated Abilities — Play Abilities in Game)**
Source: docs/stories/story-map.md
Locator: lines 128–133
Extract: Maintain Persistent Ability across Identity Changes · Load Persistent Costume on Deactivation
