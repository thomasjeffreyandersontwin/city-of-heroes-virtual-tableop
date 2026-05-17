---
state: ubiquitous-language
increment: 3
scope: Animated Abilities
date: 2026-05-17
---

# Ubiquitous Language — Increment 3: Animated Abilities

> Scope: the vocabulary needed to author *animated abilities* from composable *animation elements* (FX, movement, sound, reference, sequence, pause, load-identity), bind them to *activation keys*, manage *resource catalogs*, play them on spawned characters, maintain *persistent abilities* across identity changes, and route key events from a *keyboard hook* to character ability dispatch. Builds on Increment 1 (Character, Crowd, Crowd Repository) and Increment 2 (Identity, Game Bridge, Spawned NPC, KeyBind). No attacks yet — only standard non-combat abilities.

---

**Terms**:
- **Animated Ability**
  - **animated ability** — a named, composable action sequence the GM authors for a character; contains an ordered list of *animation elements*, an *activation key*, a persistence flag, an attack flag, and an optional default designation
  - **activation key** — the keyboard key assigned to an *animated ability*; pressing it triggers the ability's execution on the owning character
  - **persistent ability** — an *animated ability* with the persistence flag set; it replays automatically each time a new *identity* is loaded on the character
  - **default ability** — the *animated ability* automatically activated when a character is first spawned; at most one per character carries this designation
  - **attack flag** — a boolean property on an *animated ability* that marks it as an attack type, controlling eligibility in the combat system in a later increment
  - **ability activation eligibility** — the computed readiness state that determines whether an *animated ability's* activation key may currently trigger execution on a character
- **Animation Element**
  - **animation element** — a single ordered composition unit within an *animated ability*; typed as one of: *FX element*, *movement element*, *sound element*, *reference element*, *sequence element*, *pause element*, or *load-identity element*
  - **FX element** — an *animation element* that plays a named *FX resource* on the target *spawned NPC* when the ability executes
  - **movement element** — an *animation element* that applies a named *movement resource* to the target *spawned NPC* when the ability executes
  - **sound element** — an *animation element* that plays a named *sound resource* when the ability executes
  - **reference element** — an *animation element* that delegates execution to another *animated ability* by name, inserting that ability's element sequence inline at the point of reference
  - **sequence element** — an *animation element* that groups one or more child *animation elements* with an And/Or execution type; And plays all children sequentially; Or picks exactly one child at random to execute
  - **pause element** — an *animation element* that introduces a fixed timed delay between the preceding and following elements in the sequence
  - **load-identity element** — an *animation element* that triggers an identity switch on the character mid-sequence, activating the named *identity* before the following elements execute
  - **animation sequence** — the execution pattern applied to an ordered collection of *animation elements*: And-mode runs every element in order; Or-mode selects one element at random and runs only that one
- **Resource Catalog**
  - **resource catalog** — the persistent store for a typed collection of *animation resources*; loaded from a binary data file on startup; seeded from an *embedded CSV* on first run when no data file exists
  - **FX resource catalog** — the *resource catalog* that holds all available *FX resources*, loaded from `FxRepo.data`
  - **movement resource catalog** — the *resource catalog* that holds all available *movement resources*, loaded from `MoveRepo.data`
  - **sound resource catalog** — the *resource catalog* that holds all available *sound resources*, loaded from `SoundRepo.data`
  - **FX resource** — a named visual-effects entry in the *FX resource catalog*; references a COH FX identifier playable on a *spawned NPC*
  - **movement resource** — a named movement entry in the *movement resource catalog*; references a COH movement identifier applicable to a *spawned NPC*
  - **sound resource** — a named sound entry in the *sound resource catalog*; references a COH audio identifier
  - **embedded CSV** — the packaged default data bundled in the application assembly for all three *resource catalogs*; written to disk and loaded on first run when no data file is found
- **Keyboard Hook**
  - **keyboard hook** — the low-level Windows keyboard hook installed by the application that intercepts key events system-wide and forwards matching presses to *ability dispatch*
  - **key routing** — the behavior of the *keyboard hook* that selects the target character and ability to invoke for a given key press based on which window currently holds OS input focus
  - **game window focus** — the OS input focus state in which the COH game window is active; key events are routed to the active character's *animated ability* matching the pressed key
  - **application window focus** — the OS input focus state in which the HVT application window is active; key events are routed in the same way as game window focus
  - **ability dispatch** — the action of matching an intercepted key press to the *activation key* of a character's *animated ability* and triggering that ability's execution

---

The Animated Abilities increment is the first in which HVT authors and plays structured, composable animations on spawned characters. On startup, the application loads three *resource catalogs* — *FX resource catalog*, *movement resource catalog*, and *sound resource catalog* — from their respective binary data files (`FxRepo.data`, `MoveRepo.data`, `SoundRepo.data`) stored in the *COH data directory*. When no data file exists on first run, each catalog is seeded from its *embedded CSV*. With the catalogs loaded, the GM can browse *FX resources*, *movement resources*, and *sound resources* in the ability editor to populate *animation elements*.

Each *animated ability* owned by a *character* lives in the character's Abilities *option group* and consists of an ordered list of *animation elements*, an *activation key*, a persistence flag, an attack flag, and an optional default designation. The GM authors an ability in the ability editor by adding elements of any type — *FX element*, *movement element*, *sound element*, *reference element*, *sequence element*, *pause element*, or *load-identity element* — and reordering them via drag-drop. A *sequence element* introduces branching: And-type executes all its child elements sequentially; Or-type executes one child chosen at random. Playing an *animated ability* on a character dispatches each element in order, applying FX, movement commands, or sounds to the *spawned NPC*, delegating to referenced abilities, branching through sequences, pausing, or switching identities mid-sequence.

When the persistence flag is set, an *animated ability* becomes a *persistent ability*: it replays automatically each time the character's *active identity* changes, maintaining its visual effects across identity transitions. On identity deactivation, the *persistent-FX costume variant* is loaded back to preserve persistent FX appearance. The GM may also designate one *animated ability* as the *default ability*, which activates automatically when the character is first spawned. New characters receive a standard set of default abilities (Recovery, Stun Recovery, Pass Turn, and others) via the Add Default Abilities operation.

Ability execution is triggered from the crowd manager via direct play actions, or from the keyboard via the *keyboard hook*. The hook installs at application startup and intercepts key events when either the game window or the HVT application window holds OS focus. On each key press, *key routing* matches the pressed key to the *activation key* of the active character's *animated ability* and fires *ability dispatch* to execute it, subject to *ability activation eligibility*.

---

# Core Domain

## Animated Ability

*Animated Ability* is the central authored domain object of this increment: a named, composable action sequence that the GM creates, edits, and assigns to a *character* to produce visible animation effects in the COH game world. Each *animated ability* lives in the character's Abilities *option group*. It is defined by an ordered list of *animation elements*, an *activation key* that a *keyboard hook* listens for, a persistence flag that causes it to replay on each *identity* load, an attack flag that marks it as a combat ability for later increments, and an optional *default ability* designation that auto-activates it at spawn. The *animated ability* is the unit the GM plays from the ability list, the unit the *keyboard hook* dispatches on key press, and the unit a *reference element* can invoke by name from within another ability's sequence.

### animated_ability

- is created on a *character* by the GM supplying a name in the *crowd manager — abilities* screen; the name must be unique within the character's Abilities *option group*
- is edited in the *ability editor*, where the GM changes its configuration fields and element list; changes are applied on save and discarded on cancel
- is deleted from a *character* by the GM; deletion removes the *animated ability* and all its *animation elements* permanently
- holds an ordered list of *animation elements*, an *activation key*, a persistence flag, an attack flag, and a default designation flag
- is played on a *spawned NPC* by executing each *animation element* in its ordered list according to the *animation sequence* rules (And: all elements sequentially; Or: one element at random)
- is stopped mid-execution when the GM issues the stop action or when a new ability starts on the same character; the in-progress element is abandoned
- is dispatched by the *keyboard hook* when the pressed key matches the ability's *activation key* and the character is the active character
- **Invariant:** an *animated ability's* name must be unique within the character's Abilities *option group* at all times
- **Invariant:** at most one *animated ability* per character carries the default flag; at most one is actively executing at any moment

### activation_key

- is a property of *animated ability* — the keyboard key (e.g., F1, Numpad1) the GM assigns to trigger the ability's execution
- is set via the set-key action in the ability list or via the activation key field in the ability editor
- is read by the *keyboard hook* when routing key events; a key press matching this value dispatches the owning ability on the active character
- **Invariant:** at most one *animated ability* per character may hold a given *activation key* at any time; assigning a key already in use on the same character must be rejected

### persistent_ability

- is a property of *animated ability* — the boolean persistence flag that causes the ability to replay on each *identity* load
- is toggled via the toggle-persistence action in the ability list
- when set, the ability is automatically replayed after the new *identity* finishes loading, restoring visual effects tied to that ability
- when cleared (on deactivation), the *load persistent costume on deactivation* behavior applies: the *persistent-FX costume variant* is reloaded to preserve the persistent appearance on the *spawned NPC*
- **Invariant:** when a character's *active identity* changes, all currently running *persistent abilities* are stopped before the identity switch completes; they restart after the new identity has loaded

### default_ability

- is a property of *animated ability* — the boolean flag marking the ability that activates automatically when the character is first spawned
- is set via the set-default action in the ability list
- at most one *animated ability* per character carries this flag; setting a new default clears the previous one
- **Invariant:** at most one *animated ability* per character may carry the default flag at any time; clearing the flag leaves no default ability without error

### attack_flag

- is a property of *animated ability* — the boolean marker that classifies the ability as an attack type
- is toggled in the ability editor via the attack flag checkbox
- controls downstream combat system eligibility (Increment 6); no behavioral difference within this increment

### ability_activation_eligibility

- is the computed readiness state of an *animated ability*: the system evaluates whether the ability's *activation key* may currently trigger execution
- is refreshed when any condition changes that could affect eligibility (e.g., ability currently executing, character not spawned, no activation key assigned)
- gates *keyboard hook* dispatch: an ability with ineligible activation eligibility does not fire even when its key is pressed
- is displayed indirectly in the ability list via the active ability indicator (currently-playing marker on a row)

### Decisions made

- `animated ability` is a concept: distinct identity (named, unique per character's option group), state (elements, activation key, persistence flag, attack flag, default flag, active/stopped), behavior (play, stop, dispatch, replay on identity load), and invariants; the central authored object of this increment
- `activation key` is a property of *animated ability*, documented as a stub heading because its uniqueness invariant is directly testable
- `persistent ability` is a property (persistence flag) on *animated ability*, not a subtype: the execution pipeline is the same; only the auto-replay on identity load differs — a behavioral delta, but one tied to a simple flag on the same concept rather than a structurally distinct activation path; documented as a stub heading because its cross-identity invariant is testable
- `default ability` is a property (boolean flag) of *animated ability*, not a subtype: identical structure and behavior; differs only in auto-activation at spawn
- `attack flag` is a property of *animated ability*; included as a stub because it appears in the ability list display and ability editor, though its behavioral effect belongs to Increment 6
- `ability activation eligibility` earns a concept block: it has distinct behavior (computed, refreshed, gates dispatch) and its own refresh story, making it testable independently of play and stop; not merely a UI flag
- Scope-fit: all terms pass; animated ability and its properties are central to this increment; attack flag is present in this scope's UI but its combat consequence is deferred

### References

**Ref — thin-slicing.md (Increment 3: Animated Abilities stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 108–146
Extract: Create Animated Ability, Edit Animated Ability, Delete Animated Ability, Set Ability Activation Key, Toggle Ability Persistence, Set Default Ability for Character, Play Animated Ability on Character, Stop Active Ability, Maintain Persistent Ability across Identity Changes, Load Persistent Costume on Deactivation, Add Default Abilities to Character, Refresh Ability Activation Eligibility

**Ref — initial-ia.md (crowd manager — abilities, ability editor)**
Source: docs/ux/initial-ia.md
Locator: lines 108–270
Extract: ability list fields (name · activation key · persistent · attack flag), actions (create · delete · set-key · toggle-persistence · set-default · play · stop · edit); ability config form fields; element list columns and actions

**Ref — story-map.md**
Source: docs/stories/story-map.md
Locator: Manage Animated Abilities epic

---

## Animation Element

*Animation Element* is the typed, ordered composition unit within an *animated ability*. Every *animated ability* is a sequence of zero or more *animation elements*. Each element has a type that determines what it does when executed: a *FX element* plays a visual effects resource, a *movement element* applies a movement resource, a *sound element* plays audio, a *reference element* delegates to another ability by name, a *sequence element* groups children with And/Or branching, a *pause element* introduces a timed delay, and a *load-identity element* switches the character's active identity mid-execution. Elements are displayed in the element list in the ability editor with type, resource, and order columns; they are reordered via drag-drop and added individually via type-specific add actions. The *animation sequence* executed when the ability plays is governed by the ordered element list and any nested *sequence elements* it contains.

### animation_element

- is created in an *animated ability* by the GM selecting a typed add action (Add FX, Add MOV, Add Sound, Add Reference, Add Sequence, Add Pause, Add Identity) in the ability editor element list
- appears immediately in the element list at the bottom of the ordered sequence on creation; the GM may reorder it via drag-drop
- is removed from the ability by the GM; removal shifts subsequent elements up by one position
- holds a type identifier, a resource reference (where applicable), a display order position, and (for *sequence elements*) a list of child *animation elements*
- **Invariant:** each *animation element's* position in the ordered list is unique; drag-drop reorder must update all affected positions atomically
- **Invariant:** a *reference element* must not reference the *animated ability* that owns it (no self-reference); circular reference chains must not be created

### FX_element *is a type of* animation_element

- references a named *FX resource* selected from the *FX resource catalog* via the resource picker in the ability editor
- when executed, issues the COH FX command for the referenced resource on the target *spawned NPC*
- **Invariant:** the referenced *FX resource* must exist in the loaded *FX resource catalog* at execution time; an unresolvable reference produces a silent no-op

### movement_element *is a type of* animation_element

- references a named *movement resource* selected from the *movement resource catalog* via the resource picker in the ability editor
- when executed, applies the referenced COH movement command to the target *spawned NPC*
- **Invariant:** the referenced *movement resource* must exist in the loaded *movement resource catalog* at execution time; an unresolvable reference produces a silent no-op

### sound_element *is a type of* animation_element

- references a named *sound resource* selected from the *sound resource catalog* via the resource picker in the ability editor
- when executed, plays the referenced COH audio identifier

### reference_element *is a type of* animation_element

- stores the name of another *animated ability* on the same character
- when executed, runs the referenced ability's full element list inline at the current point in the parent sequence, as if those elements were inserted directly
- **Invariant:** a *reference element* must not reference its own owning ability; circular reference chains (A references B, B references A) must not be created and must be rejected at save time

### sequence_element *is a type of* animation_element

- holds a list of child *animation elements* and an execution type: And or Or
- when executed with type And, runs every child *animation element* in order (sequential execution)
- when executed with type Or, selects exactly one child *animation element* at random and executes only that one
- child elements are added and reordered within the *sequence element* using the same add/reorder gestures as the parent ability
- **Invariant:** a *sequence element* must contain at least one child *animation element*; an empty sequence is not executable

### pause_element *is a type of* animation_element

- holds a duration value (in seconds or milliseconds, as configured)
- when executed, blocks progression to the next *animation element* for the configured duration
- requires no resource reference; duration is configured inline in the element list

### load_identity_element *is a type of* animation_element

- stores the name of a target *identity* on the same character
- when executed, triggers an identity switch on the character using the stored identity name, as if the GM had selected set-active on that identity
- the following elements in the sequence execute after the identity switch completes
- **Invariant:** the referenced *identity* must belong to the same character that owns the *animated ability*; a reference to a non-existent identity produces a no-op

### animation_sequence

- is the execution pattern applied to an ordered list of *animation elements* at runtime
- And-mode: executes every element in the list one after another in ascending order position; does not skip any element
- Or-mode: selects one element from the list at random (uniform distribution) and executes only that element; all other siblings are skipped
- is the execution type carried by each *sequence element*; the top-level element list of an *animated ability* is always And-mode (all elements execute in order)

### Decisions made

- `animation element` is a concept: distinct identity (positioned within an ability's ordered list), state (type, resource reference, order position), behavior (created, reordered, removed, executed per type), interactions with ability and resource catalogs
- `FX element`, `movement element`, `sound element`, `reference element`, `sequence element`, `pause element`, and `load-identity element` are all subtypes of *animation element*: each has behaviorally distinct execution — different commands issued, different data held, different rules — not merely different data values on the same behavior; they pass the Liskov test (anywhere an animation element is expected, any subtype executes correctly within the typed dispatch)
- `animation sequence` earns a concept block: it has distinct behavior (select all vs. select one at random), applies at two levels (ability root and sequence element), and its And/Or rule is the central invariant of the sequence story; not merely a property of *sequence element*
- Self-reference and circular-reference invariants on *reference element* are scoped here; they are directly testable behavioral rules

### References

**Ref — thin-slicing.md (Increment 3: Add Element stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 128–138
Extract: Add Movement Element to Ability, Add Sound Element to Ability, Add FX Element to Ability, Add Reference Element to Another Ability, Add Sequence Element (And/Or), Add Pause Element, Add Load-Identity Element, Reorder Animation Elements via Drag-Drop, Execute Animation Sequence (And: sequential, Or: random)

**Ref — initial-ia.md (ability editor — element list)**
Source: docs/ux/initial-ia.md
Locator: lines 226–270
Extract: element list — type · resource · order · persistent flag; actions: add FX · add MOV · add sound · add reference · add sequence · add pause · add identity · reorder · remove

---

## Resource Catalog

*Resource Catalog* is the persistent store for the typed animation resource entries available when authoring *animation elements*. There are three catalogs — *FX resource catalog*, *movement resource catalog*, and *sound resource catalog* — each loaded from its own binary data file in the *COH data directory* on application startup. When no data file is found on first run, the catalog is seeded from its *embedded CSV* resource, which is bundled in the application assembly. The catalogs are in-memory once loaded; they are not re-read mid-session. The GM browses each catalog via a typed resource picker in the ability editor to select the resource reference for a new *FX element*, *movement element*, or *sound element*.

### resource_catalog

- is loaded from its binary data file (`FxRepo.data`, `MoveRepo.data`, or `SoundRepo.data`) stored in the *COH data directory* on application startup
- is seeded from its *embedded CSV* on first run when no binary data file exists in the *COH data directory*, then written to disk for subsequent sessions
- holds an ordered collection of named *animation resource* entries of its specific type, available for assignment to *animation elements*
- is the data source for the resource picker displayed when the GM adds a typed element in the ability editor
- **Invariant:** a *resource catalog* must be loaded before any resource-picker interaction or element-save operation that references a resource of its type; operations against an unloaded catalog must be rejected
- **Invariant:** each *resource catalog* is authoritative for its type; FX resources are not available from the movement or sound catalog, and vice versa

### FX_resource_catalog

- is the *resource catalog* for visual-effects resources, loaded from `FxRepo.data`
- holds *FX resource* entries; each entry provides the name and COH FX identifier used by a *FX element*
- is browsed via the resource picker opened by Add FX in the ability editor

### movement_resource_catalog

- is the *resource catalog* for movement resources, loaded from `MoveRepo.data`
- holds *movement resource* entries; each entry provides the name and COH movement identifier used by a *movement element*
- is browsed via the resource picker opened by Add MOV in the ability editor

### sound_resource_catalog

- is the *resource catalog* for sound resources, loaded from `SoundRepo.data`
- holds *sound resource* entries; each entry provides the name and COH audio identifier used by a *sound element*
- is browsed via the resource picker opened by Add Sound in the ability editor

### FX_resource

- is a named visual-effects entry in the *FX resource catalog*
- carries a display name and a COH FX command identifier; the identifier is the value issued to the game engine when a *FX element* executes
- is the data type held by *FX element* as its resource reference

### movement_resource

- is a named movement entry in the *movement resource catalog*
- carries a display name and a COH movement command identifier; the identifier is the value applied to the *spawned NPC* when a *movement element* executes
- is the data type held by *movement element* as its resource reference

### sound_resource

- is a named sound entry in the *sound resource catalog*
- carries a display name and a COH audio identifier; the identifier is played when a *sound element* executes
- is the data type held by *sound element* as its resource reference

### embedded_CSV

- is a set of plain-text comma-separated data files bundled as embedded resources in the application assembly, one per catalog type
- is read once on first run per catalog type when no binary data file exists; after seed the catalog is persisted as a binary data file and the embedded CSV is not read again
- provides the default population of *FX resources*, *movement resources*, and *sound resources* available from a fresh installation

### Decisions made

- `resource catalog` is a concept: distinct identity (named typed data file), state (loaded/not loaded, holds resource entries), behavior (load from data file, seed from embedded CSV, expose for browsing), invariants (must be loaded before picker or element save)
- `FX resource catalog`, `movement resource catalog`, `sound resource catalog` are subordinate concepts (not subtypes): their load behavior and structure are identical; they differ only in the file they load and the type of resource they hold — a type-property difference that does not add distinct behavior; each documented separately because the load story distinguishes them by file name and the resource picker scope-fits to its catalog type
- `FX resource`, `movement resource`, `sound resource` are subordinate concepts, not instances: each is a distinct data type with a different field shape (FX identifier vs. movement identifier vs. audio identifier) and is held by a different *animation element* subtype; they have distinct identity in the resource picker display
- `embedded CSV` is a concept: distinct identity (bundled resource file), state (present/consumed), behavior (read exactly once per catalog type on first run), clear trigger condition (no data file found)

### References

**Ref — thin-slicing.md (Increment 3: Resource Catalog stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 115–121
Extract: Load FX Resource Catalog (FxRepo.data), Load Movement Resource Catalog (MoveRepo.data), Load Sound Resource Catalog (SoundRepo.data), Seed Resource Catalogs from Embedded CSV on First Run, Browse FX Resources for Ability Authoring, Browse Movement Resources for Ability Authoring, Browse Sound Resources for Ability Authoring

---

## Keyboard Hook

*Keyboard Hook* is the system-level input infrastructure that enables the GM to trigger *animated ability* execution on characters by pressing their configured *activation keys* without needing to click in the application. The *keyboard hook* is a low-level Windows keyboard hook installed at application startup; it intercepts all key events system-wide when either the COH game window or the HVT application window holds OS input focus. On each intercepted key press, *key routing* identifies the active character, matches the pressed key to that character's *animated ability* activation keys, and fires *ability dispatch* if a match is found and *ability activation eligibility* permits. The *keyboard hook* is the bridge between the physical keyboard and the animated ability execution pipeline.

### keyboard_hook

- is installed at application startup via a Windows low-level keyboard hook (WH_KEYBOARD_LL or equivalent)
- intercepts key-down events system-wide; evaluates each event against the active character's *activation keys* via *key routing*
- is active whenever either the COH game window or the HVT application window holds OS input focus; key events from other windows are passed through without dispatch
- routes the intercepted key event to *ability dispatch* when a matching *animated ability* is found and *ability activation eligibility* permits
- **Invariant:** the *keyboard hook* must be installed before any key-press-based ability dispatch can occur; uninstalled state means no key events are routed

### key_routing

- is the behavior executed by the *keyboard hook* on each intercepted key press: determine the focused window, identify the active character, look up that character's *animated abilities* for an *activation key* match, and invoke *ability dispatch* if found
- is applied only when *game window focus* or *application window focus* is confirmed; all other focus states result in pass-through
- looks up the *active character* from the current session state before searching ability activation keys

### game_window_focus

- is the OS input focus state in which the COH game window is the foreground window
- when active, the *keyboard hook* routes key events to the active character's ability dispatch pipeline
- is detected by checking the foreground window handle against the known COH process window handle

### application_window_focus

- is the OS input focus state in which the HVT application window is the foreground window
- when active, the *keyboard hook* routes key events to the active character's ability dispatch pipeline, using the same routing logic as *game window focus*

### ability_dispatch

- is the execution action triggered by *key routing* when an *activation key* match is found and *ability activation eligibility* permits
- retrieves the matched *animated ability* and initiates its element-by-element execution on the active character's *spawned NPC*
- sends the ability play signal into the animated ability execution pipeline, which dispatches each *animation element* in the ability's ordered list

### Decisions made

- `keyboard hook` is a concept: distinct identity (installed Windows hook handle), state (installed/not installed), behavior (intercept key events, evaluate focus, route to dispatch), invariants (must be installed before dispatch), and its own story (Install Low-Level Keyboard Hook)
- `key routing` is a concept: distinct behavior (focus check → active character lookup → activation key match → dispatch or pass-through), interactions with game window focus, application window focus, and ability dispatch; not merely an internal method — the routing logic spans window focus detection and character selection, making it a testable behavioral unit
- `game window focus` and `application window focus` are concepts: distinct state (detectable OS condition), distinct behavior (enables or gates routing), testable independently (route when COH focused, route when HVT focused, no route when neither); they are not merely enum values — each has a separate story
- `ability dispatch` is a concept: distinct identity (the execution-triggering action), behavior (retrieve ability → execute element pipeline), interactions with ability activation eligibility and the animated ability execution engine; not just a method call label

### References

**Ref — thin-slicing.md (Increment 3: Keyboard Hook stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 143–146
Extract: Install Low-Level Keyboard Hook, Route Key Events when Game Window is Focused, Route Key Events when Application Window is Focused, Dispatch Ability Activation Keys to Characters

---

# Boundary Domain

## Character

Owned by: Character and Crowd Library (Increment 1)

- holds an Abilities *option group* that this increment populates with *animated ability* entries; each *animated ability* in the option group carries its own *animation elements*, *activation key*, and flags
- provides the character name used as the target *spawned NPC* name when *animated ability* execution issues game commands
- receives the Add Default Abilities operation in this increment, which populates the Abilities *option group* with the standard set of named default abilities (Recovery, Stun Recovery, Pass Turn, Half Phase Action, Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power)

### Decisions made

- *character* is a boundary concept: its lifecycle, CRUD, and crowd membership are fully owned by Increment 1; this increment depends on *character* as the host for *animated abilities* and as the target name for game-side execution

### References

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

---

## Identity

Owned by: Character Identities (Increment 2)

- is referenced by the *load-identity element* in this increment; the element stores an identity name and triggers an identity switch when executed mid-sequence
- stops any running *persistent abilities* when the *active identity* changes; this increment owns the restart side of that lifecycle (persistent abilities restart after the new identity loads)
- triggers reload of *persistent abilities* on activation: the *persistent-FX costume variant* is loaded back after the identity loads when a *persistent ability* is active

### Decisions made

- *identity* is a boundary concept: its full lifecycle (add, set-type, set-active, remove, spawn pipeline) belongs to Increment 2; this increment depends on *identity* as the target for *load-identity element* and as the lifecycle event that triggers persistent-ability replay

### References

**Ref — ubiquitous-language-increment-2.md (Identity KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA section

---

## Game Bridge

Owned by: Character Identities (Increment 2)

- executes the FX, movement, and sound game commands issued by *animation element* execution during an *animated ability* play sequence
- routes each game command (FX play, movement apply, sound play) as *slash commands* through the *native game bridge* or via *keybind files*, using the same delivery infrastructure established in Increment 2

### Decisions made

- *game bridge* is a boundary concept: its initialization and command routing infrastructure are fully established in Increment 2; this increment uses it as the execution channel for animation element game commands

### References

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

---

## Spawned NPC

Owned by: Character Identities (Increment 2)

- is the game-world entity targeted by *animated ability* execution; FX elements, movement elements, and sound elements apply their game commands to the *spawned NPC* identified by the character's name
- must be present in the game world before an *animated ability* can be played on it; playing on an unspawned character must be blocked or result in a no-op

### Decisions made

- *spawned NPC* is a boundary concept: its lifecycle (spawn, despawn, targeting) is fully owned by Increment 2; this increment depends on *spawned NPC* as the execution target for animation commands

### References

**Ref — ubiquitous-language-increment-2.md (Identity KA — spawned NPC)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Identity KA — spawned NPC section

---

## KeyBind

Owned by: Character Identities (Increment 2)

- is used in this increment to deliver *activation key* game commands; the *keyboard hook* supplements (rather than replaces) the *keybind* delivery channel for key-triggered ability execution

### Decisions made

- *keybind* is a boundary concept: the file-generation and bind-load-file delivery pipeline is owned by Increment 2; this increment adds the *keyboard hook* as a parallel key-routing path

### References

**Ref — ubiquitous-language-increment-2.md (KeyBind KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: KeyBind KA section
