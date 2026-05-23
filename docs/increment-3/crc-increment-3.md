---
state: crc
increment: 3
scope: Animated Abilities
date: 2026-05-21
---

# CRC — Increment 3: Animated Abilities

> Domain sources: `docs/increment-3/ubiquitous-language-increment-3.md`, `docs/increment-3/acceptance-criteria-increment-3.md`.

---

# Core Domain

## **Animated Ability**

The central authored domain object: a named, composable action sequence the GM creates on a character to produce visible animation effects on the spawned NPC. Each character holds an Ability Option Group — a type-safe collection that owns the active/default/persistent selection invariants.

### **Ability Option Group : Option Group**
active abilities                      | Animated Ability (collection — multiple may be active simultaneously)
                                      |   invariant: persistent abilities remain active until explicitly stopped; non-persistent stop when a new non-persistent ability starts
                                      |   note: active abilities is a live filter over abilities whose `execution state = executing`; `Animated Ability.execution state` is the source of truth; membership in this collection reflects that state
default ability                       | Animated Ability
                                      |   invariant: at most one default per character; auto-plays on spawn
                                      |   invariant: at most one non-persistent ability actively executing at any moment

### **Animated Ability**
ability name                          | (text, unique within character's Ability Option Group)
                                      |   invariant: ability name must be unique within the character's Ability Option Group at all times
ordered animation elements            | Animation Element
activation key                        | (keyboard key value or unset)
                                      |   invariant: at most one animated ability per character may hold a given activation key value
persistence designation               | (persistent or non-persistent)
attack designation                    | (attack or non-attack)
execution state                       | (executing or stopped)
play on character                     | Animation Element, Spawned NPC, Game Bridge, Animation Sequence
                                      |   invariant: executes each animation element in order per animation sequence rules
stop execution                        | (abandons current in-progress element immediately)

### **Ability Activation Eligibility**
eligible state                        | Animated Ability, Spawned NPC
refresh eligibility                   | Animated Ability, Spawned NPC
                                      |   invariant: an ineligible ability does not dispatch even when its activation key is pressed
                                      |   invariant: eligibility is ineligible when: no activation key assigned, ability currently executing, or character not spawned

### references

**Ref — thin-slicing.md (Increment 3: Animated Abilities stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 108–146
Extract: Create Animated Ability, Edit Animated Ability, Delete Animated Ability, Set Ability Activation Key, Toggle Ability Persistence, Set Default Ability for Character, Play Animated Ability on Character, Stop Active Ability, Maintain Persistent Ability across Identity Changes, Load Persistent Costume on Deactivation, Add Default Abilities to Character, Refresh Ability Activation Eligibility

```source
- Create Animated Ability
- Edit Animated Ability
- Delete Animated Ability
- Set Ability Activation Key
- Toggle Ability Persistence
- Set Default Ability for Character
- Play Animated Ability on Character
- Stop Active Ability
- Maintain Persistent Ability across Identity Changes
- Load Persistent Costume on Deactivation
- Add Default Abilities to Character
- Refresh Ability Activation Eligibility
```

### decisions made

- `activation key` maps to a property on Animated Ability (not a separate class): it is a data value with a uniqueness invariant but no distinct identity, behavior chain, or lifecycle of its own
- `persistent ability` maps to the `persistence designation` property on Animated Ability: the persistence flag gates auto-replay on identity load but the ability's execution pipeline is unchanged
- `default ability` maps to the `default designation` property on Animated Ability: at most one per character, identical structure and execution
- `attack flag` maps to the `attack designation` property on Animated Ability: controls later-increment combat eligibility; no behavioral difference in this increment
- `ability activation eligibility` earns its own class: it has distinct computed behavior (refresh, gate dispatch) and its own story (Refresh Ability Activation Eligibility)
- The ordered animation elements list does not require a named collection class: its management behavior (add at bottom, reorder positions, remove with shift) is straightforward list ordering without supersession, end-of-turn, or constraint logic
- The persistence lifecycle (stop before identity switch → restart after new identity loads) is expressed through collaborations with Identity (boundary) and does not require a state-carrier class
- `Ability Option Group.active abilities` is a derived view — the collection reflects which `Animated Ability` instances currently have `execution state = executing`; it is not a separate truth; the source of truth for whether an ability is executing is the `execution state` property on the `Animated Ability` instance itself

---

## **Animation Element**

The typed, ordered composition unit within an animated ability. Each subtype defines what happens when executed.

### **Animation Element**
display order position                | (integer, unique within parent list)
execute                               | Spawned NPC, Game Bridge
                                      |   invariant: each element's position in the ordered list is unique
                                      |   invariant: drag-drop reorder updates all affected positions atomically

### **FX Element : Animation Element**
referenced FX resource                | FX Resource
execute FX command                    | FX Resource, Spawned NPC, Game Bridge
                                      |   invariant: referenced FX resource must exist in loaded FX resource catalog at execution time; unresolvable reference produces a silent no-op

### **Movement Element : Animation Element**
referenced movement resource          | Movement Resource
execute movement command              | Movement Resource, Spawned NPC, Game Bridge
                                      |   invariant: referenced movement resource must exist in loaded movement resource catalog at execution time; unresolvable reference produces a silent no-op

### **Sound Element : Animation Element**
referenced sound resource             | Sound Resource
execute sound command                 | Sound Resource, Game Bridge

### **Reference Element : Animation Element**
referenced ability name               | Animated Ability
execute referenced ability inline     | Animated Ability
                                      |   invariant: must not reference the owning animated ability (no self-reference)
                                      |   invariant: circular reference chains (A→B→A) must not exist and are rejected at save time

### **Sequence Element : Animation Element**
child animation elements              | Animation Element
execution type                        | (And or Or)
execute children per type             | Animation Element, Animation Sequence
                                      |   invariant: must contain at least one child animation element; an empty sequence is not executable

### **Pause Element : Animation Element**
pause duration                        | (time value)
block progression for duration        | (suspends sequence advancement for configured time)

### **Load-Identity Element : Animation Element**
target identity name                  | Identity
trigger identity switch               | Identity
                                      |   invariant: referenced identity must belong to the same character that owns the animated ability; non-existent identity produces a no-op

### **Animation Sequence**
execution type                        | (And or Or)
execute sequence                      | Animation Element
                                      |   invariant: And-mode executes every element in ascending order position; does not skip
                                      |   invariant: Or-mode selects one element at random (uniform distribution) and executes only that element; all others skipped

### references

**Ref — thin-slicing.md (Increment 3: Add Element stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 128–138
Extract: Add Movement Element, Add Sound Element, Add FX Element, Add Reference Element, Add Sequence Element (And/Or), Add Pause Element, Add Load-Identity Element, Reorder Animation Elements via Drag-Drop, Execute Animation Sequence

```source
- Add Movement Element to Ability
- Add Sound Element to Ability
- Add FX Element to Ability
- Add Reference Element to Another Ability
- Add Sequence Element (And/Or)
- Add Pause Element
- Add Load-Identity Element
- Reorder Animation Elements via Drag-Drop
- Execute Animation Sequence (And: sequential, Or: random)
```

### decisions made

- `animation element` is a base class: it carries the shared position property and abstract execute operation; all seven typed elements are subtypes with behaviorally distinct execution
- All seven element subtypes pass the Liskov test: anywhere an animation element is expected in the ordered list, any subtype can execute within the typed dispatch without breaking the contract
- `animation sequence` earns its own class: its And/Or rule is the central branching invariant that applies at two levels (ability root is always And; sequence elements carry And or Or) — not merely a property of sequence element
- The base `execute` operation on Animation Element lists Spawned NPC and Game Bridge as collaborators because all resource-backed subtypes ultimately issue commands through them; subtypes that do not (pause, reference, load-identity) override with their own collaborator set

---

## **Resource Catalog**

The persistent store for typed animation resource entries. Three catalogs exist — FX, movement, and sound — each loaded from its own binary data file on startup.

### **Resource Catalog**
loaded state                          | (loaded or not loaded)
resource entries                      | (typed collection of named resources)
data file path                        | COH Data Directory
load from data file                   | COH Data Directory
seed from embedded CSV                | Embedded CSV, COH Data Directory
                                      |   invariant: must be loaded before any resource-picker interaction or element-save operation that references a resource of its type
                                      |   invariant: each resource catalog is authoritative for its type; FX resources not available from movement or sound catalog

### **FX Resource Catalog**
data file reference                   | (FxRepo.data in COH Data Directory)
FX resource entries                   | FX Resource

### **Movement Resource Catalog**
data file reference                   | (MoveRepo.data in COH Data Directory)
movement resource entries             | Movement Resource

### **Sound Resource Catalog**
data file reference                   | (SoundRepo.data in COH Data Directory)
sound resource entries                | Sound Resource

### **FX Resource**
display name                          | (text)
COH FX command identifier             | (COH FX ID)

### **Movement Resource**
display name                          | (text)
COH movement command identifier       | (COH movement ID)

### **Sound Resource**
display name                          | (text)
COH audio identifier                  | (COH audio ID)

### **Embedded CSV**
bundled resource data                 | (per catalog type — FX, movement, sound)
seed catalog on first run             | Resource Catalog, COH Data Directory
                                      |   invariant: read exactly once per catalog type when no binary data file exists; not read again after seed completes and data file is written

### references

**Ref — thin-slicing.md (Increment 3: Resource Catalog stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 115–121
Extract: Load FX Resource Catalog, Load Movement Resource Catalog, Load Sound Resource Catalog, Seed Resource Catalogs from Embedded CSV, Browse FX Resources, Browse Movement Resources, Browse Sound Resources

```source
- Load FX Resource Catalog (FxRepo.data)
- Load Movement Resource Catalog (MoveRepo.data)
- Load Sound Resource Catalog (SoundRepo.data)
- Seed Resource Catalogs from Embedded CSV on First Run
- Browse FX Resources for Ability Authoring
- Browse Movement Resources for Ability Authoring
- Browse Sound Resources for Ability Authoring
```

### decisions made

- `resource catalog` is the KA's own class with the full shared behavior (load, seed, guard invariants); the three typed catalogs are subordinate concepts, not subtypes — they differ only in data file name and resource type held, which is a type-property difference with no behavioral delta
- FX Resource Catalog, Movement Resource Catalog, and Sound Resource Catalog receive their own blocks to document their specific file reference and resource collection — they do not repeat the shared load/seed behavior from Resource Catalog
- FX Resource, Movement Resource, and Sound Resource are separate classes (not subtypes of a common base): each carries a different command identifier type and is held by a different animation element subtype; no base "Animation Resource" concept exists in the UL
- `embedded CSV` earns a class: it has distinct identity (bundled assembly resource), distinct behavior (seed exactly once per catalog on first run), and a clear lifecycle trigger

---

## **Keyboard Hook**

The system-level input infrastructure that bridges the physical keyboard to the animated ability execution pipeline.

### **Keyboard Hook**
installed state                       | (installed or not installed)
install hook                          | (OS low-level keyboard hook registration)
uninstall hook                        | (OS hook handle release)
intercept key event                   | Key Routing
                                      |   invariant: must be installed before any key-press-based ability dispatch can occur; uninstalled means no key events are routed

### **Key Routing**
route key event                       | Game Window Focus, Application Window Focus, Character, Animated Ability, Ability Dispatch
                                      |   invariant: routing applied only when game window focus or application window focus is confirmed; all other focus states result in pass-through without dispatch

### **Game Window Focus**
focus state                           | (focused or unfocused)
detect focus                          | (OS foreground window handle compared to COH process window)

### **Application Window Focus**
focus state                           | (focused or unfocused)
detect focus                          | (OS foreground window handle compared to HVT application window)

### **Ability Dispatch**
dispatch ability execution            | Animated Ability, Spawned NPC, Ability Activation Eligibility
                                      |   invariant: dispatch fires only when ability activation eligibility permits; ineligible abilities are suppressed

### references

**Ref — thin-slicing.md (Increment 3: Keyboard Hook stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 143–146
Extract: Install Low-Level Keyboard Hook, Route Key Events when Game Window is Focused, Route Key Events when Application Window is Focused, Dispatch Ability Activation Keys to Characters

```source
- Install Low-Level Keyboard Hook
- Route Key Events when Game Window is Focused
- Route Key Events when Application Window is Focused
- Dispatch Ability Activation Keys to Characters
```

### decisions made

- `keyboard hook` is a concept: distinct identity (OS hook handle), state (installed/not installed), behavior (install, intercept, route), and its own story (Install Low-Level Keyboard Hook)
- `key routing` earns a class: it spans multiple responsibilities (focus check → character lookup → activation key match → dispatch invocation) that form a testable behavioral unit
- `game window focus` and `application window focus` are separate classes: each has a distinct detection mechanism and its own story; they are not merely enum values of a single "focus state" concept
- `ability dispatch` earns a class: it owns the retrieval-and-execution action, collaborates with eligibility gating, and forms the bridge between key routing and the animated ability play pipeline

---

# Boundary Domain

### **Character**
Ability Option Group                  | Ability Option Group
character name                        | (text)
add default abilities                 | Animated Ability

### **Identity**
(lifecycle owned by Increment 2; referenced here for load-identity element and persistent ability restart)

### **Game Bridge**
(initialization and command routing owned by Increment 2; used here to execute FX, movement, and sound game commands during animated ability play)

### **Spawned NPC**
(lifecycle owned by Increment 2; targeted by animated ability execution — FX, movement, and sound commands apply to the spawned NPC identified by the character name)

### **KeyBind**
(file-generation and bind-load-file delivery owned by Increment 2; keyboard hook supplements this channel as a parallel key-routing path)

### references

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

```source
Character: holds option groups, character name
```

**Ref — ubiquitous-language-increment-2.md (Identity, Game Bridge, Spawned NPC, KeyBind)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA, Game Bridge KA, KeyBind KA sections

```source
Identity: active/inactive, identity switch pipeline
Game Bridge: initialization state, slash command execution
Spawned NPC: character name, presence in game world
KeyBind: file path, keybind entries
```

### decisions made

- All five boundary concepts retain their full lifecycle ownership from their home increment; this CRC only documents the collaboration surface this increment depends on
- COH Data Directory (a property of COH Game Directory from Increment 2) is referenced as a collaborator on Resource Catalog for file-based load operations but does not receive its own class block — it has no distinct behavior in this increment beyond providing a file path
- Persistent-FX Costume Variant (from Increment 2) is referenced in the persistent ability deactivation flow as a collaborator through Game Bridge; no separate block needed since this increment only triggers the reload via the existing Load Costume Command pipeline
- `Persistent Ability` class was anticipated in Increment 2 boundary but was not created in this increment; the concept is modeled as `persistence designation` property on `Animated Ability`; the stop-on-identity-switch behavior is an `Animated Ability` / `Ability Option Group` collaboration, not a separate class
