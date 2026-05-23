---
state: crc
increment: 2
scope: Character Identities
date: 2026-05-21
---

# Module: [Character Identities]

Scope: assign visual identities to characters, initialize and communicate with the live COH game engine, manage costume and keybind files on disk, render ghost shadow overlays, and build crowds from available COH models.

**Core terms**:
- Identity, Model Identity, Costume Identity, Spawned NPC
- Game Bridge, HookCostume DLL, Native Game Bridge, Slash Command, Game Event, Game Loaded Event, InitGame
- Costume File, COH Costumes Directory, Original-Backup Costume File, Persistent-FX Costume Variant, Ghost Costume File
- KeyBind, KeyBind File, Game Command, Spawn NPC Command, Target by Name Command, Load Costume Command, Delete NPC Command
- Ghost Shadow, Ghost NPC, Ghost Alignment
- Model, Model List, Models.txt

**Key Abstractions (term grouping)**:
- **Identity**: Identity, Model Identity, Costume Identity, Spawned NPC
- **Game Bridge**: Game Bridge, HookCostume DLL, Native Game Bridge, Slash Command, Game Event, Game Loaded Event, InitGame
- **Costume File**: Costume File, COH Costumes Directory, Original-Backup Costume File, Persistent-FX Costume Variant, Ghost Costume File
- **KeyBind**: KeyBind, KeyBind File, Game Command, Spawn NPC Command, Target by Name Command, Load Costume Command, Delete NPC Command
- **Ghost Shadow**: Ghost Shadow, Ghost NPC, Ghost Alignment
- **Model**: Model, Model List, Models.txt

---

# Core Domain

## **Identity**

Identity is the named visual configuration that determines how a character appears in the COH game world. Each character holds an Identity Option Group — a type-safe collection that owns the active/default selection invariants. An identity is either a Model Identity or a Costume Identity — the two differ in their activation pipeline.

### **Identity Option Group : Option Group**
active identity                        | Identity
                                       |   invariant: exactly zero or one identity carries the active designation at any time; setting a new active clears the previous before the new activation sequence begins
default identity                       | Identity
                                       |   invariant: at most one identity may carry the default designation; may be cleared without assigning another, leaving no default

### **Identity**
identity name                          | (text, unique within character's Identity Option Group)
                                       |   invariant: name must be unique within the character's Identity Option Group

### **Model Identity : Identity**
model name                             | Model, Model List
                                       |   invariant: model name must resolve to an entry in the loaded model list; unresolvable name must not be accepted at assignment time

### **Costume Identity : Identity**
costume surface                        | Costume File, COH Costumes Directory
                                       |   invariant: must resolve to an existing costume file in the COH costumes directory at activation time


### **Spawned NPC**
character name                         | Character
                                       |   invariant: addressed by the character's name for all subsequent game commands
entity presence                        | (present or absent)
                                       |   invariant: must be present before any load costume command or ghost alignment can be applied
                                       |   invariant: target by name command must succeed before the load costume command is issued

### references

**Ref — thin-slicing.md (Increment 2: identity stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 82–92
Extract: whole

```source
Add Identity, Set Identity Type, Assign Costume Surface, Set Default Identity, Set Active Identity, Remove Identity, Load Costume File, Spawn Character with Model Identity, Switch Active Identity, Play Animation on Identity Load, Stop Persistent Abilities on Identity Switch
```

**Ref — story-map.md (Manage Character Identities epic)**
Source: docs/stories/story-map.md
Locator: lines 91–109
Extract: whole

```source
Configure Identity · Render Identity in Game · Manage Ghost Shadows sub-epics
```

**Ref — initial-ia.md (crowd manager — identities — identity list)**
Source: docs/ux/initial-ia.md
Locator: lines 63–104
Extract: whole

```source
identity list — name · type · active · default; actions: add · remove · set-default · set-active · add ghost · assign-surface · set-type
```

### decisions made

- `Model Identity` and `Costume Identity` are subtypes of `Identity` because their activation pipelines are behaviorally distinct: model identity spawns directly from model name; costume identity requires spawn + target + load costume — the difference changes what the thing does, not just the data it carries (Liskov holds: both fulfil the same identity contract for the game bridge)
- `Identity Option Group` owns the active/default selection invariants — individual Identity instances do not know whether they are "the active one"; the collection enforces this
- `active designation` and `default designation` were previously modeled as properties on Identity — moved to Identity Option Group where enforcement belongs
- `costume surface` is a property of `Costume Identity` — NOT a separate class
- `Spawned NPC` is a concept with distinct identity (named game-world entity), state (present/absent), and invariants — the game-side realization of an active identity
- No collection class introduced for the Identities option group: the management behavior (at-most-one-active, at-most-one-default, name uniqueness) is simple flag enforcement, not complex supersession or sequencing logic; invariants are expressed on Identity directly

---

## **Game Bridge**

The game bridge is the application service responsible for opening and maintaining the live connection between HVT and the COH game engine, and the sole routing point for all outbound game communication.

### **Game Bridge**
initialization state                   | (uninitialized, initializing, polling, ready)
                                       |   invariant: no game command or slash command may be issued before the ready state is reached
load HookCostume DLL                   | HookCostume DLL, COH Game Directory
run InitGame                           | Native Game Bridge, HookCostume DLL
poll game state                        | Native Game Bridge
fire game loaded event                 | Game Loaded Event
inject required keybinds               | KeyBind File, COH Game Directory, Native Game Bridge
extract costume pack                   | COH Costumes Directory, Costume File
load model list                        | Models.txt, Model List
route slash command                    | Slash Command, Native Game Bridge
                                       |   invariant: rejected or queued if bridge is not in the ready state
generate keybind file                  | Game Command, KeyBind File, COH Game Directory
execute identity activation            | Identity, Spawned NPC, Spawn NPC Command, Target by Name Command, Load Costume Command, KeyBind File
execute identity deactivation          | Identity, Spawned NPC, Delete NPC Command, Animated Ability
perform ghost alignment                | Ghost NPC, Spawned NPC, Character

### **HookCostume DLL**
file location                          | COH Game Directory
loaded state                           | (loaded or not loaded)
                                       |   invariant: must be successfully loaded before InitGame is called; missing or unloadable DLL is a fatal initialization error
game communication API                 | (init, slash command execution, game state queries)

### **Native Game Bridge**
execute slash command                  | HookCostume DLL, Slash Command
call init entry point                  | HookCostume DLL
query game state                       | HookCostume DLL

### **Slash Command**
command string                         | (text, e.g. /spawnnpc, /target_name, /loadcostume, /bind_load_file)
delivery path                          | (immediate via Native Game Bridge, or embedded in KeyBind File)

### **Game Event**
signal type                            | (typed identifier)

### **Game Loaded Event : Game Event**
publication state                      | (unpublished or published)
                                       |   invariant: published exactly once per session; subsequent ready confirmations do not re-publish

### **InitGame**
call DLL init entry point              | HookCostume DLL, Native Game Bridge
start ready-poll loop                  | Game Bridge
                                       |   invariant: HookCostume DLL must be loaded before InitGame is called

### references

**Ref — thin-slicing.md (Increment 2: game bridge initialization stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 65–81
Extract: whole

```source
Load HookCostume DLL, Initialize Game Bridge (InitGame), Poll until Game Client is Loaded, Inject Required KeyBinds, Extract Costume Pack, Publish Game Loaded Event, Initialize Native Game Bridge, Execute Slash Command via DLL
```

**Ref — story-map.md (Launch and Initialize Session — Initialize Game Connection)**
Source: docs/stories/story-map.md
Locator: lines 18–28
Extract: whole

```source
Load HookCostume DLL, Initialize Game Bridge, Poll until Game Client is Loaded, Inject Required KeyBinds, Extract Costume Pack, Load Models List, Publish Game Loaded Event
```

**Ref — story-map.md (Communicate with Game Engine — Bridge via HookCostume DLL)**
Source: docs/stories/story-map.md
Locator: lines 216–223
Extract: whole

```source
Initialize Native Game Bridge, Execute Slash Command via DLL, Query Hovered NPC Info, Query Mouse XYZ, Check Game Done State
```

### decisions made

- `Game Bridge` is cohesive around "game connection lifecycle + command delivery"; its 13 responsibilities all serve the single purpose of managing the communication boundary with COH — not a dependency magnet
- `Native Game Bridge` is a separate concept from `Game Bridge`: it owns the P/Invoke marshaling boundary; `Game Bridge` orchestrates, `Native Game Bridge` executes
- `Game Event` is minimal (one property) because its role is architectural decoupling — the game bridge publishes, subscribers consume; the event itself is just the typed signal
- `Game Loaded Event` is a subtype of `Game Event` rather than an instance because it adds a directly testable invariant (published exactly once per session)
- `InitGame` is a named operation on `Game Bridge`, given a separate concept block because its ordering invariant (DLL must be loaded first) is directly testable
- `execute identity activation` and `execute identity deactivation` are Game Bridge operations because the AC explicitly names Game Bridge as the actor for the spawn/despawn pipeline; no separate "identity activation service" is introduced since all steps involve routing game commands
- `execute identity deactivation` collaborates with `Animated Ability` (not a separate `Persistent Ability` class) — persistent abilities are `Animated Ability` instances with `persistence designation = persistent`; stopping them before despawn is an `Animated Ability` operation via `Ability Option Group`

---

## **Costume File**

The costume file is the file-system artifact that carries a character's appearance data in COH's native format. HVT stores all managed costume files in the COH costumes directory and maintains derived variants alongside each character's primary costume.

### **Costume File**
file path                              | COH Costumes Directory
costume data                           | (body shape, costume parts, color assignments in COH native format)
                                       |   invariant: must exist at its file path when the load costume command is issued; a missing file causes the command to fail silently
create backup before first modification | Original-Backup Costume File, COH Costumes Directory
                                       |   invariant: backup is created before any HVT modification; never overwritten if already exists

### **COH Costumes Directory**
directory path                         | COH Game Directory
                                       |   invariant: must exist and be writable before any costume file write can proceed; created on first run if absent

### **Original-Backup Costume File**
file path                              | COH Costumes Directory
backup naming convention               | (e.g. guard_original.costume — distinguishes from active working file)
immutable source content               | Costume File
                                       |   invariant: written exactly once per character; subsequent modifications to the working costume file do not overwrite the backup

### **Persistent-FX Costume Variant**
derive from original backup            | Original-Backup Costume File, COH Costumes Directory
persistent FX layers                   | Animated Ability
                                       |   invariant: regenerated whenever the set of active persistent abilities changes; persistent abilities are `Animated Ability` instances with `persistence designation = persistent`

### **Ghost Costume File**
derive from original backup            | Original-Backup Costume File, COH Costumes Directory
ghost material treatment               | (reduced-opacity applied to all costume parts)
ghost naming convention                | (e.g. guard_ghost.costume — distinguishes from primary and backup files)

### references

**Ref — thin-slicing.md (Increment 2: costume file stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 78–81, 97–98
Extract: whole

```source
Store Costume Files in COH Costumes Directory, Create Original-Backup Costume Files, Create Persistent-FX Costume Variants, Create Ghost Costume Files
```

**Ref — story-map.md (Manage Game Data and Files — Manage Costume Files)**
Source: docs/stories/story-map.md
Locator: lines 256–260
Extract: whole

```source
Store Costume Files, Create Original-Backup Costume Files, Create Persistent-FX Costume Variants, Create Ghost Costume Files
```

### decisions made

- `Costume File` owns the "create backup before first modification" guard responsibility — the backup is a precautionary step intrinsic to costume modification, not a separate service concern
- `Original-Backup Costume File` is a concept (not just a naming convention) because it has distinct identity (created once, immutable), a distinct role (sole source for all variant generation), and a clear invariant
- `Persistent-FX Costume Variant` and `Ghost Costume File` own their own derivation operations because the knowledge of how to derive (which treatment to apply) is intrinsic to each variant type's definition
- `Persistent-FX Costume Variant` and `Ghost Costume File` are subordinate concepts (not subtypes of Costume File): they share file format and storage location but differ only in derivation context and use purpose, not in fundamental behavior as costume files

---

## **KeyBind**

The keybind is COH's built-in mechanism for mapping a key press to one or more slash commands. HVT exploits this mechanism as a command-delivery channel: it generates a keybind file for each game command and loads it via /bind_load_file.

### **KeyBind**
key name                               | (named key for the binding)
slash command chain                    | Slash Command
                                       |   invariant: COH executes commands in chain sequentially when the keybind file is loaded

### **KeyBind File**
file path                              | COH Game Directory
keybind entries                        | KeyBind
                                       |   invariant: must be fully written to disk before the bind_load_file instruction is issued; loading a partially written or absent file produces undefined behavior

### **Game Command**
command type                           | (spawn, target, load costume, delete)
target name                            | Character, Spawned NPC
slash command composition              | Slash Command
delivery method                        | (immediate via Native Game Bridge, or via KeyBind File)
                                       |   invariant: a game command targeting a spawned NPC by name fails silently if no NPC with that name exists in the current session

### **Spawn NPC Command : Game Command**
model name payload                     | Model
                                       |   invariant: first command in the activation sequence for any identity

### **Target by Name Command : Game Command**
target name payload                    | Character, Spawned NPC
                                       |   invariant: must succeed before load costume command is issued
                                       |   invariant: if no spawned NPC with the target name exists, no target is set and subsequent load costume commands apply to an undefined target

### **Load Costume Command : Game Command**
costume file path payload              | Costume File, COH Costumes Directory
                                       |   invariant: issued after target by name command confirms the correct NPC is selected
                                       |   invariant: applies costume to the currently targeted spawned NPC

### **Delete NPC Command : Game Command**
target name payload                    | Spawned NPC
                                       |   invariant: if no spawned NPC with the target name exists, the command is a no-op; the game ignores without error

### references

**Ref — thin-slicing.md (Increment 2: keybind and game command stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 82–83, 84–86
Extract: whole

```source
Generate KeyBind File for Game Event, Execute Spawn NPC Command, Execute Target by Name Command, Execute Load Costume Command, Execute Delete NPC Command, Write Custom KeyBind Files to COH Data Directory, Load KeyBind File into Game
```

**Ref — story-map.md (Communicate with Game Engine — Execute Game Commands via KeyBinds)**
Source: docs/stories/story-map.md
Locator: lines 224–234
Extract: whole

```source
Generate KeyBind File, Execute Spawn NPC Command, Execute Target by Name Command, Execute Delete NPC Command, Execute Load Costume Command
```

### decisions made

- `Spawn NPC Command`, `Target by Name Command`, `Load Costume Command`, `Delete NPC Command` are subtypes of `Game Command` (not type-property instances) because each adds distinct ordering invariants and payload constraints — the sequencing rules (target before load, spawn first in activation) are behaviorally different per command type (Liskov holds: all share the same composition-and-delivery contract from Game Command)
- `Game Command` holds the routing decision (direct vs. keybind delivery) as a property because both paths share the same command composition logic; the route diverges at delivery time only

---

## **Ghost Shadow**

The ghost shadow is a semi-transparent NPC overlay superimposed on a model identity character. It provides a visual bridge between the NPC model appearance and the costume-based look the GM wants to present.

### **Ghost Shadow**
associated character                   | Character, Model Identity
active state                           | (active or inactive)
                                       |   invariant: can only be activated when the associated character is currently spawned with an active model identity; attempting on unspawned character or costume identity must be rejected
activate                               | Ghost Costume File, Ghost NPC, Game Bridge, Original-Backup Costume File
remove                                 | Ghost NPC, Game Bridge, Delete NPC Command

### **Ghost NPC : Spawned NPC**
ghost costume appearance               | Ghost Costume File
aligned position and facing            | Character
                                       |   invariant: position and facing must match the character's after spawn and after each character movement; drift makes the overlay visually incorrect

### **Ghost Alignment**
read character position and facing     | Character, Spawned NPC
write position and facing to ghost     | Ghost NPC
                                       |   invariant: must be re-executed whenever the character moves; without re-alignment the ghost overlay appears displaced

### references

**Ref — thin-slicing.md (Increment 2: ghost shadow stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 93–98
Extract: whole

```source
Superimpose Ghost on Model Character, Create Ghost Costume File from Original, Align Ghost Position and Orientation with Character, Remove Ghost from Desktop, Create Ghost Costume Files
```

**Ref — story-map.md (Manage Ghost Shadows sub-epic)**
Source: docs/stories/story-map.md
Locator: lines 105–109
Extract: whole

```source
Superimpose Ghost on Model Character, Create Ghost Costume File from Original, Align Ghost Position and Orientation with Character, Remove Ghost from Desktop
```

### decisions made

- `Ghost Shadow` owns `activate` and `remove` operations because it coordinates the full ghost lifecycle sequence (generate file → spawn ghost NPC → load costume → align); individual steps are delegated to collaborators (Game Bridge for game commands, Ghost Costume File for derivation)
- `Ghost NPC` is a subtype of `Spawned NPC` because it fulfils the same game-world contract (targetable, has costume applied, despawnable) but adds the alignment requirement and ghost-specific state (Liskov holds: delete, target, load costume all work identically on a ghost NPC)
- `Ghost Alignment` is a concept (not just a property of Ghost Shadow) because it has distinct behavior (read → write), a distinct trigger (after spawn, after movement), and a correctness invariant (co-location)

---

## **Model**

A model is a named COH NPC archetype available in the game client. The model list powers model identity assignment and the model browser crowd-creation flow.

### **Model**
archetype name                         | (text, e.g. Skull_Lt_01, Clockwork_Gear_01)
type classification                    | (e.g. villain group, hero, civilian)

### **Model List**
available models                       | Model
loaded state                           | (loaded or not loaded)
                                       |   invariant: must be loaded before any model browser interaction or model identity name assignment is permitted; operations against an unloaded model list must be rejected

### **Models.txt**
file location                          | COH Game Directory
model name entries                     | (one name per line, full enumeration of available NPC archetypes)
                                       |   invariant: must be present in COH game directory; absence or unreadability prevents model list loading and is a fatal initialization error

### references

**Ref — thin-slicing.md (Increment 2: model list stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 99–103
Extract: whole

```source
Load Available Models from Models.txt, Create Crowd from COH Model List, Select Models to Include in Crowd, Generate Characters with Model Identities, Load Models List for Crowd Creation
```

**Ref — story-map.md (Build Crowds from Game Models sub-epic)**
Source: docs/stories/story-map.md
Locator: lines 55–59
Extract: whole

```source
Create Crowd from COH Model List, Load Available Models from Models.txt, Select Models to Include in Crowd, Generate Characters with Model Identities
```

**Ref — initial-ia.md (model browser screen)**
Source: docs/ux/initial-ia.md
Locator: lines 275–307
Extract: whole

```source
model browser — model list: model name · type; actions: select · deselect · create crowd from selection
```

### decisions made

- `Model` is a concept (not just a string): it has distinct identity (named archetype), is referenced by model identity and spawn NPC command, and carries type classification
- `Model List` is a concept (not just an array): it has distinct state (loaded/not loaded), behavior (loaded from Models.txt, queried by model browser and model identity validation), and invariants — analogous to a collection class that gates access to its contents
- `Models.txt` is a concept: distinct identity (named file in game directory), its own invariant (must be present), and the single authoritative source for its data type

---

# Boundary Domain

### **Character**
Identity Option Group                  | Identity Option Group
character name                         | (text, used as spawned NPC name in game commands)

### **Crowd**
characters for identity assignment     | Character
create from model selection            | Model, Character, Model Identity

### **COH Game Directory**
base path                              | (validated file-system path to COH installation)
HookCostume DLL location               | HookCostume DLL
COH costumes directory path            | COH Costumes Directory
COH Data Directory                     | (derived path from stored configuration path)
Models.txt location                    | Models.txt

### **Persistent Ability → retired**
> Not a separate class. Modeled in Increment 3 as `Animated Ability` with `persistence designation = persistent`. The stop-on-identity-switch behavior is owned by `Animated Ability` via collaboration with `Ability Option Group`. The invariant (persistent abilities must stop before the old active identity despawns) is enforced through the `Ability Option Group` stopping all persistent abilities before `Identity Option Group` switches the active identity.

### references

**Ref — ubiquitous-language-increment-1.md (Character, Crowd, COH Game Directory)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA, Crowd KA, COH Game Directory KA
Extract: whole

```source
Character: lifecycle, CRUD, crowd membership fully defined by Increment 1; this increment depends on character as the host for identities and as the name source for game commands.
Crowd: lifecycle fully owned by Increment 1; this increment reads crowd tree for character selection and writes a new crowd via model browser flow.
COH Game Directory: validated and stored in Increment 1; this increment reads derived paths.
```

**Ref — thin-slicing.md (Stop Persistent Abilities on Identity Switch)**
Source: docs/stories/thin-slicing.md
Locator: line 91
Extract: whole

```source
Stop Persistent Abilities on Identity Switch
```

### decisions made

- `Character` is boundary: lifecycle/CRUD/crowd-membership owned by Increment 1; this increment uses it as the host for identities and as the name source for spawned NPCs
- `Crowd` is boundary: lifecycle owned by Increment 1; this increment adds a creation path from the model browser (create from model selection)
- `COH Game Directory` is boundary: validated in Increment 1; this increment reads its derived paths (costumes/, data/, HookCostume DLL, Models.txt)
- `Persistent Ability` was anticipated as a boundary class owned by Increment 3; Increment 3 chose to model the concept as `persistence designation` property on `Animated Ability` rather than a separate class — the class was retired; this increment's stop-on-identity-switch concern is now expressed through `Animated Ability` / `Ability Option Group` collaboration
