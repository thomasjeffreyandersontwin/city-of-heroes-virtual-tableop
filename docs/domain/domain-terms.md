---
state: domain-terms
---

# Module: [Hero Virtual Tabletop]

Scope: The full runtime vocabulary for the Hero Virtual Tabletop — the GM tool that orchestrates live superhero RPG sessions inside the City of Heroes 3D engine.

**Key Abstractions (term grouping)**:
- **Character**: option group, position, attack configuration, ghost shadow, active identity, default identity, maneuvering with camera, distance count
- **Crowd**: crowd member, saved position, crowd repository, gang mode, crowd manager, all characters crowd, clipboard, flatten-copy
- **Identity**: model identity, costume identity, animation on load, costume file, surface
- **Animated Ability**: animation element, FX effect element, MOV element, sound element, pause element, sequence element, reference ability, identity element, animation resource, attack, on-hit animation
- **Character Movement**: movement instruction
- **Roster**: desktop, character targeting
- **Game Bridge**: keybind, keybind file, HookCostume DLL, memory element, camera, pop-up menu, COH game directory
h
**Boundary terms**:
- HCS (Hero Combat System) *(owned by: External Combat System)*
- COH Game Engine *(owned by: City of Heroes Platform)*

---

# Core Domain

## Character

A *character* is the foundational active unit of the system — the entity the GM creates, names, equips, and directs. Every runtime capability (spawn, move, animate, attack) executes against a *character*. Its state is the aggregate of what it looks like (*active identity*), what it can do (*animated abilities*, *character movements*), where it is (*position*), whether it is present in the game world (*spawned state*), and what combat it is involved in (*attack configuration map*). All other KAs either own *characters*, direct them, or communicate their commands to the game engine on their behalf.

- holds an ordered *option group* for each capability class — Identities, Abilities, and Movements — created on demand and never absent
- resolves its *active identity* at construction by scanning for a *costume file* matching its name; defaults to a Model identity if none is found
- spawns into the game world by issuing a *keybind* with the *active identity's* surface name, then polls the *game bridge* until the NPC is registered in game memory, producing a live *position* and *memory element*
- targets itself in the game — a prerequisite for all game-side commands — by issuing a target *keybind* or instructing the *memory element* directly
- clears from the desktop by issuing a delete *keybind*, releasing its *memory element*, and removing any *ghost shadow*
- moves to a 3D destination by delegating to its *default movement to activate*, which selects the *active movement*, *default movement*, or Walk movement in order of preference
- tracks accumulated travel distance against its *distance limit*, updating *distance count* on every position write
- stores zero or more *attack configurations*, from which it derives combat state flags: attacker, defender, stunned, unconscious, dying, dead, knocked back
- enters *maneuvering with camera* mode, assigning itself as the *camera's* maneuvered target and receiving continuous position updates from camera movement
- adds *default abilities* (Walk, Run, Swim movements; Recovery, Strike, Dodge, and 17 other standard abilities) to every non-special character on first configuration
- **Invariant:** a *spawned character* always has a valid *position* and an *active identity*; the *default identity* is used if the *active identity* is removed or unset
- **Invariant:** exactly three canonical *option groups* (Identities, Abilities, Movements) must always exist on every *character* — they are created lazily but never absent

### Decisions made

- `character` is the central domain concept — it has distinct identity (name), rich state (spawned, active, positioned), and is the subject of every significant behavior in the system
- `ghost shadow` is modeled as a separate concept rather than a property because it is a full *character* instance with its own *spawned state*, *position*, *identity*, and *movement* lifecycle
- `option group` is a concept: it has distinct identity (name), state (ordered keyed collection), behavior (add/remove/find), and carries the invariant that canonical groups must exist
- `available identities`, `animated abilities` (collection), and `character movements` (collection) are *properties* — each is simply the *option group* slot of a specific type on *character*; no additional concept is needed
- `position` is a concept: distinct state (X, Y, Z, model matrix), distinct behavior (read/write game memory at offsets), and a clear creation/destruction lifecycle tied to *spawned state*
- although `position` uses *game bridge* memory primitives, it is placed under *character* because it represents the character's spatial state, not a communication service
- `attack configuration` is a concept: distinct structure (Guid → Attack + config params), distinct behavior (add/remove/derive flags), and a clear lifecycle
- Combat as a workflow is deferred; this concept records only the per-character snapshot of current attack involvement
- `active identity` is a property: a reference slot on *character*; the behavior triggered on change (stop abilities, remove ghost, render new identity) is owned by *character*
- `default identity` is a property: a reference slot on *character* with a fallback-creation rule; no distinct behavior beyond selection
- `maneuvering with camera` is a property: a boolean slot; the behavior (camera pushes position) belongs to *character* and *camera* concepts
- `distance count` is a property (derived scalar); `distance limit` is also a property (computed from the highest movement distance limit plus reach bonus); neither has independent identity or behavior

### References

**Ref — Character.cs (class definition and Spawn)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 31–1733
Extract: partial

**Ref — Character.cs (AvailableIdentities / AnimatedAbilities / Movements)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 348–1395
Extract: partial

**Ref — Character.cs (CurrentPositionVector / CurrentModelMatrix / CurrentFacingVector)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 222–293
Extract: partial

**Ref — Character.cs (AttackConfigurationMap / AddAttackConfiguration / combat flags)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 154–653
Extract: partial

**Ref — Character.cs (CreateGhostShadow / AlignGhost / RemoveGhost)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 794–865
Extract: partial

**Ref — Character.cs (ActiveIdentity getter)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 435–479
Extract: partial

**Ref — Character.cs (DefaultIdentity getter)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 371–404
Extract: partial

**Ref — Character.cs (ManeuveringWithCamera)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 1233–1259
Extract: partial

**Ref — Character.cs (UpdateDistanceCount / MaxDistanceLimit)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 654–213
Extract: partial

---

### option group

- groups *character options* — identities, *animated abilities*, or *character movements* — under a named key, maintaining insertion order and name-keyed lookup
- adds, inserts, removes, and replaces *character options* by index or name, notifying observers on every structural change
- enforces uniqueness by name within the group; duplicate names are rejected on add
- **Invariant:** the three canonical *option groups* (Identities, Abilities, Movements) are always present on a *character*, created on first access if not already present

---

### position

- reads the *character's* current X, Y, Z world-space coordinates from game memory at the target NPC's memory pointer offset
- writes X, Y, Z back to game memory to physically relocate the NPC in the game world, triggering a *distance count* update
- reads and writes the character's 4×4 model matrix at the NPC memory offset, encoding rotation and translation for orientation changes
- reads the character's facing vector from the model matrix and writes a target-facing direction by computing a look-at matrix from world-space destination
- clones itself for saved-position storage, preserving coordinate and matrix state without a live memory pointer
- **Invariant:** a *position* object is created only after the *memory element* confirms NPC registration; it is meaningless and absent for an un-spawned *character*

---

### attack configuration

- records the *attack* and its associated parameters (attack mode, knockback distance, effect severity, hit/miss result) for one combat engagement, keyed by a unique GUID
- holds zero or more concurrent *attack configurations* simultaneously, allowing a *character* to participate in multiple overlapping attacks as attacker and defender
- derives the *character's* combat state flags — attacker, defender, stunned, unconscious, dying, dead, knocked back — by evaluating the union of all held *attack configuration* entries
- adds a new entry when the GM initiates an attack, updates it when parameters change, and removes it when combat for that engagement ends
- **Invariant:** combat state flags are always recomputed from the full map; no single *attack configuration* entry can alone make a *character* "dead" while another marks it "alive"

---

### ghost shadow

- spawns as an independent *character* instance alongside a *model identity character* to serve as a live FX carrier in the game world
- mirrors the principal *character's* world-space position and model matrix on every move and turn, keeping itself co-located
- carries *FX costume file* overlays — effect particles and visual augmentations — that the COH engine cannot load directly onto model NPCs
- is created with a ghost *costume file* copied from the `ghost_original.costume` template, and its own *character movements* cloned from the principal's movement set
- is removed and set to null when the principal switches to a *costume identity*, clears from the desktop, or explicitly removes it
- **Invariant:** a *ghost shadow* exists only while the principal *character's* active identity is of type Model and the principal is *spawned*; a *costume identity character* never has a *ghost shadow*

---

### active identity

- is a property of *character* — the reference slot pointing to the currently rendered *identity*; falls back to *default identity* if unset or removed from the available identities collection

---

### default identity

- is a property of *character* — the fallback *identity* used when no *active identity* is set; auto-creates a Model identity if the available identities collection is empty

---

### maneuvering with camera

- is a property of *character* — a boolean mode flag; when true, the *camera* is the maneuvered character's movement controller

---

### distance count

- is a property of *character* — the accumulated travel distance in the current movement, computed as Δposition / 8 (game units to tabletop distance)

---

## Crowd

A *crowd* is a named, hierarchical container of *crowd members* — each of whom is either a *character* or a nested *crowd* — that the GM organizes for scene staging and group management. It is the organizing unit of the *crowd repository* and the persistence boundary for the entire *character* collection. A *crowd* can save and restore the *position* of each *crowd member*, turning the *crowd* into a reusable scene arrangement. In *gang mode*, one *crowd member* is the *gang leader* and the *crowd* coordinates activation and movement for the group as a unit.

- contains an ordered, name-keyed collection of *crowd members*, which may be *characters* or nested *crowds*
- adds, removes, clones, and reorders *crowd members*; notifies observers on every structural change
- saves the *position* of each *crowd member* to its saved-positions dictionary, keyed by member name
- restores *crowd member* positions from the saved-positions dictionary, teleporting each member back to its saved arrangement
- filters its visible *crowd members* by name regex, collapsing non-matching branches and expanding matching ones
- responds to *gang mode* activation by nominating one *crowd member* as *gang leader* and coordinating group activation and movement
- **Invariant:** *crowd member* names must be unique within a *crowd* — the collection is keyed by name and rejects duplicates

### Decisions made

- `crowd` is a concept: distinct identity (name), rich state (member collection, saved positions, gang mode), and behaviors that operate on the collection as a whole
- `crowd member` is a concept: it extends *character* with explicit membership behavior (roster crowd reference, position save/restore, filter participation) that plain *characters* do not have
- `crowd collection` is a *property* — the typed observable collection on *crowd*; no independent concept
- `saved position` is a concept: distinct structure (coordinates + matrix keyed by name), distinct behavior (capture / restore), and a lifecycle tied to scene-staging operations
- `parent crowd` and `roster crowd` are *properties* — reference slots on *crowd member* pointing to containing *crowd* instances; no independent concept
- `crowd repository` is a concept: distinct identity (file path), distinct behavior (serialize, deserialize, backup, seed), and a clear persistence lifecycle
- `character crowd repository` is the same concept as *crowd repository* — "Character Crowd Repository" is the UI workspace name for this concept, not a separate artifact
- `gang mode` is a property: a boolean slot on *crowd*; the coordination behavior (activate all, move in formation) belongs to the *crowd* and *roster* concepts
- `gang leader` is a property of *character* — a boolean flag on a *crowd member*; it is a role designation, not an independent concept
- `gang activation` is a behavior owned by *roster*, not a separate concept

### References

**Ref — Crowd.cs (Crowd / CrowdModel classes)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Crowds\Crowd.cs
Locator: lines 34–416
Extract: partial

**Ref — CrowdMember.cs (ICrowdMember / CrowdMember)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Crowds\CrowdMember.cs
Locator: lines 24–79
Extract: partial

**Ref — Crowd.cs (SavePosition / Place)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Crowds\Crowd.cs
Locator: lines 344–403
Extract: partial

**Ref — CrowdRepository.cs**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Crowds\CrowdRepository.cs
Locator: lines 17–134
Extract: partial

**Ref — Crowd.cs (IsGangMode) / Character.cs (IsGangLeader)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Crowds\Crowd.cs
Locator: lines 71–83
Extract: whole

---

### crowd member

- participates in one or more *crowds* by maintaining a back-reference to its *roster crowd*
- can be cloned into an independent copy, linked across multiple *crowds* as a shared reference, or flattened into a numbered standalone *character*
- saves its own *position* when its containing *crowd* triggers a save-position operation
- is placed at a *saved position* when its containing *crowd* places it, teleporting it back to a prior arrangement
- **Invariant:** a *crowd member's* name must be unique within any *crowd* it belongs to at any moment

---

### saved position

- stores the X, Y, Z world-space coordinates and model matrix of a *crowd member* at a snapshot moment, keyed by the *crowd member's* name in the parent *crowd's* saved-positions dictionary
- is applied to a *crowd member* by a place call, writing the stored coordinates back to the *crowd member's* live *position*
- is cloned without a live memory pointer so it can be persisted and restored across sessions

---

### crowd repository

- deserializes the full *crowd* hierarchy from a JSON file in the COH data directory on session start, restoring all *crowd members* with their *option groups*, *identities*, *abilities*, and *movements*
- serializes the full *crowd* hierarchy back to JSON on save, preserving the complete *character* and *crowd* state tree
- creates a daily backup copy of the valid JSON file before overwriting, protecting against corruption
- seeds the collection from an embedded resource of default *crowd members* when no file is found on first run
- **Invariant:** exactly one *crowd repository* file is the source of truth; the backup is read-only and used only for disaster recovery

---

### gang mode

- is a property of *crowd* — a boolean flag indicating whether the *crowd* is operating as a coordinated gang with a designated *gang leader*

---

### crowd manager

- is the pre-session library surface — the main application screen that opens at startup and shows the *crowd* hierarchy
- presents the *crowd* tree where the GM creates, renames, deletes, nests, clones, links, filters, and browses *crowds* and *crowd members*
- triggers loading of the *crowd repository* on open and saving on explicit save actions
- is distinct from the *desktop*, which is the live session overlay for *spawned characters* during play
- **Invariant:** the *crowd manager* is always the first surface the GM sees; the *desktop* is only active once game session begins

---

### all characters crowd

- is a special protected root *crowd* that aggregates every *character* in the *crowd repository* as a flat alphabetically sorted list
- is automatically maintained — any *character* added to any *crowd* also appears here
- cannot be deleted; attempts to delete it are blocked
- **Invariant:** the *all characters crowd* is always present and always current; it reflects the full character population of the *crowd repository* at all times

---

### clipboard

- holds at most one cut or copied *crowd member* (or *crowd*) at a time, ready for paste into any *crowd*
- is populated when a *crowd member* is cut (removes it from the source *crowd*) or copied (leaves the source intact)
- is consumed and cleared when the GM pastes into a target *crowd*
- **Invariant:** cutting a *crowd member* immediately removes it from the source *crowd*; pasting places the held item into the target *crowd* and clears the *clipboard*

---

### flatten-copy

- is an operation on *crowd* — replaces its membership with independently numbered deep-copy *characters* (e.g. "Guard 1", "Guard 2")
- breaks any shared *crowd member* references within the flattened *crowd* — the resulting copies are fully independent
- leaves nested *crowds* in place; only character-level members are numbered and replaced
- **Invariant:** after flatten-copy, no two resulting *characters* share state; modifying one does not affect any other

---

## Identity

An *identity* is the visual appearance a *character* presents in the COH game world, defined by a *surface* name and an *identity type*. The *identity type* determines the rendering path: a *model identity* instructs the game engine to swap the NPC's model; a *costume identity* loads a `.costume` file. Each *identity* may carry an *animation on load* that plays automatically on render. The *identity* manages the *costume file* variants that persist FX effects and provide ghost overlays.

- owns a *surface* name and an *identity type* (Model or Costume), which together determine how the game engine renders the *character*
- renders itself by generating the appropriate *keybind* — model swap for Model, `load_costume` for Costume — and triggering *animation on load* if present
- renders without animation (on spawn) by generating only the *keybind* without triggering *animation on load*
- clones itself, producing an independent *identity* with the same surface, type, and a deep-copy of its *animation on load*
- **Invariant:** a *costume identity* requires a `.costume` file at `<coh_dir>/costumes/<surface>.costume` to render successfully; a *model identity* uses the surface name directly as the NPC model argument

### Decisions made

- `identity` is a top-level concept with distinct state (surface, type, animation on load), distinct behavior (render, render-without-animation, clone), and critical rendering path logic
- two meaningful subtypes exist: *model identity* (triggers ghost shadow, uses model swap command) and *costume identity* (manages costume file variants, uses `load_costume`)
- *model identity* is a subtype: it adds ghost shadow lifecycle behavior that *costume identity* does not have
- *costume identity* is a subtype: it adds costume file variant management and deactivation restore behavior that *model identity* does not have
- `animation on load` is a concept: distinct lifecycle (start on render, stop on identity switch), distinct behavior (play with/without initial costume), and its own state (active/inactive tracking)
- `identity type` is a *property* — the `IdentityType` enum discriminator (Model / Costume) on *identity*; the behavioral distinction is captured by the two subtypes
- `surface` is a *property* — a string slot on *identity*; no independent behavior
- `costume file` is a concept: distinct on-disk identity (file path), behavior (archive, inject FX, restore, create variants), and multi-variant state
- `original costume`, `persistent costume`, and `ghost costume` are instances / named variants of *costume file*, not separate concepts

### References

**Ref — Identity.cs (Render / RenderWithoutAnimation)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Identities\Identity.cs
Locator: lines 115–183
Extract: partial

**Ref — Character.cs (ActiveIdentity setter) / Identity.cs (Render)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 449–479
Extract: partial

**Ref — Character.cs (Deactivate)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 1419–1460
Extract: partial

**Ref — Identity.cs (animationOnLoad / Render call sequence)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Identities\Identity.cs
Locator: lines 88–128
Extract: partial

**Ref — AnimatedElement.cs (FXEffectElement — PrepareCostumeFile)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 739–808
Extract: partial

---

### model identity *(is a type of identity)*

- triggers creation and alignment of the *character's* *ghost shadow* on every render, because the game engine cannot embed costume FX directly onto model NPCs
- removes the *ghost shadow* when the *character* switches away from this *identity*

---

### costume identity *(is a type of identity)*

- manages three *costume file* variants on disk: the *original costume* (archived backup), the *persistent costume* (with active FX baked in), and the *ghost costume* (for the *ghost shadow*)
- issues a `load_costume` *keybind* with the *surface* filename on render
- on *character* deactivation, reloads the *persistent costume* variant if a *persistent* *animated ability* is active, or the *original costume* otherwise

---

### animation on load

- fires automatically when an *identity* is rendered, playing a specified *animated ability* on the *character* at the moment of the identity switch
- is stopped before the new *identity's* *animation on load* begins, ensuring no two load animations overlap
- plays with an optional initial *costume* argument for *costume identity* renders, enabling FX sequencing on top of the freshly loaded costume

---

### costume file

- exists as a text file in `<coh_dir>/costumes/<name>.costume` encoding the visual parts, colors, and FX attachment slots of a *costume identity*
- maintains three variants: the *original* (archived backup copied before modification), the *persistent* (original with FX baked in for active persistent abilities), and the *ghost* (copy of `ghost_original.costume` for the *ghost shadow*)
- is injected with an FX reference by an *FX effect element* — the element writes a new file with `Fx <path>` inserted into a `CostumePart` block, then loads it
- is restored to the *original* variant when a *persistent* *animated ability* stops
- **Invariant:** the *original* variant is written only once — on first archive — and is never overwritten by FX injection; subsequent injections read from the *original*

---

### surface

- is a property of *identity* — the model name or costume filename string that identifies the visual resource; no independent behavior

---

## Animated Ability

An *animated ability* is a named, composable behavior a *character* can perform — a tree of *animation elements* executed in defined order. It extends the *sequence element* pattern, making it both a top-level ability and a nestable element. Its execution model follows the *animation sequence type*: And plays elements sequentially in order; Or picks one element at random. A *persistent* ability sustains its FX and sounds until explicitly stopped. An ability flagged as an *attack* participates in combat targeting and is gated by the attack workflow.

- plays its *animation elements* in order (And) or at random (Or), delegating each element's execution to its concrete type
- stops all active *animation elements*, reverting any *FX effect element* costume modifications and silencing any *sound elements*
- tracks its own active state, setting active at play start and inactive on stop
- binds to an *activation key* so the GM can trigger it with a keyboard shortcut during play
- maintains a flag indicating whether it is an *attack*, enabling combat targeting restrictions and the attack workflow
- fires grouped animations across multiple *characters* simultaneously for area-effect scenarios, collecting *keybinds* per target into a single batched command
- clones itself with a deep copy of all *animation elements*, preserving structure while allowing independent configuration
- **Invariant:** a *persistent* *animated ability* that is active must be stopped before any *identity* switch on the owning *character*

### Decisions made

- `animated ability` is a top-level concept: distinct identity (name), rich behavior (play/stop/clone/group), activation key, persistence, and attack flag
- `animation sequence type`, `persistence`, `activation key`, `attack flag`, and `area effect flag` are *properties* — enum/boolean slots on *animated ability*; their behavioral effects are described in the ability's own behavior bullets
- `animation element` is a concept: abstract base with distinct state (name, order, type, flags), distinct behavioral contract (play/stop/clone/getKeybind), and a concrete subtype hierarchy
- seven concrete subtypes are identified (FX effect element, MOV element, sound element, pause element, sequence element, reference ability, identity element)
- `animation resource` is a concept: it has distinct structure (typed union of string/object), distinct behavior (implicit conversion), and is the single typed payload carrier across all element subtypes
- *attack* is a subtype of *animated ability*: it shares the sequence-playback contract and adds only defender-routing and combat-state management
- `on-hit animation` is a property of *attack* — a reference slot; the play behavior is owned by *attack*

### References

**Ref — AnimatedAbility.cs / SequenceElement.Play**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedAbility.cs
Locator: lines 25–115
Extract: partial

**Ref — AnimatedElement.cs (IAnimationElement / AnimationElement)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 34–291
Extract: partial

**Ref — AnimatedElement.cs (FXEffectElement.Play / Stop)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 640–969
Extract: partial

**Ref — AnimatedElement.cs (MOVElement.Play)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 576–637
Extract: partial

**Ref — AnimatedElement.cs (SoundElement.Play / Stop)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 435–547
Extract: partial

**Ref — AnimatedElement.cs (PauseElement.Play)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 293–432
Extract: partial

**Ref — AnimatedElement.cs (SequenceElement.PlayAnimations)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 992–1157
Extract: partial

**Ref — AnimatedElement.cs (ReferenceAbility.Play)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 1454–1531
Extract: partial

**Ref — AnimatedElement.cs (IdentityElement.Play / Stop)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimatedElement.cs
Locator: lines 1553–1613
Extract: partial

**Ref — AnimationResource.cs**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\AnimatedAbilities\AnimationResource.cs
Locator: (class definition — typed union with implicit operators)
Extract: partial

---

### animation element

- holds a name, an execution order index, a *type* discriminator, a *persistent* flag, and a play-with-next flag that chains this element with the next in the same *keybind* batch
- declares play and stop operations that concrete subtypes implement — the base implementation is a no-op
- carries an *animation resource* that the concrete subtype interprets as its payload (file path, time, or object reference)
- provides a get-keybind operation that returns the raw *keybind* string for the element without executing it, used in grouped-animation batching
- can be cloned into an independent copy that retains type, resource, order, and flags

---

### FX effect element *(is a type of animation element)*

- reads the *character's* active *costume file* (or the *ghost shadow's* if the character is a Model type), injects an `Fx <path>` directive into a `CostumePart` block, writes the result to a new variant file, and issues a load-costume *keybind*
- archives the *original costume* on first modification so it can be restored on stop
- stops by reloading the *original costume* file, erasing the FX overlay from the game world
- supports a *fire coordinates* string that directs the FX emission point for directional attack effects
- **Invariant:** the *original costume* archive is written once and never overwritten; all FX variants are built from the original

---

### MOV element *(is a type of animation element)*

- issues a `mov` *keybind* targeting the *character* (and its *ghost shadow* if present) to trigger a game-engine animation from the MOV catalog
- issues the keybind in a play-with-next chain when the flag is true, batching multiple elements into one command execution

---

### sound element *(is a type of animation element)*

- plays a 3D positional audio file from the COH sound directory, positioned at the *character's* world-space location relative to the *camera* as listener
- loops the sound continuously when *persistent*, updating the 3D position as the *character* moves using a recurring timer callback
- stops all sounds immediately, canceling the loop timer

---

### pause element *(is a type of animation element)*

- blocks execution for a fixed number of milliseconds before releasing, creating timing gaps between animation steps in a sequence
- supports distance-adaptive delay variants — close, short, medium, long — that let the sequence timing adjust based on how far a target is from the attacker

---

### sequence element *(is a type of animation element)*

- groups a set of child *animation elements* and plays them in And order (sequential) or picks one at random (Or)
- manages child element ordering, play-with-next chaining rules, and event subscriptions for property change propagation
- supports grouped execution across multiple *character* targets simultaneously, collecting all *keybinds* into a batched command

---

### reference ability *(is a type of animation element)*

- delegates all play, stop, and keybind-collection operations to a linked *animated ability* by reference rather than containing its own *animation elements*
- propagates active state from the referenced *animated ability* rather than tracking its own state

---

### identity element *(is a type of animation element)*

- switches the owning *character's* active *identity* mid-sequence by rendering the configured *identity* target
- reverts to the *character's* current *active identity* (without animation) on stop

---

### animation resource

- holds the typed payload an *animation element* interprets: a file path string for FX, a MOV catalog entry name, a sound file path, a millisecond integer for pause, a reference to an *animated ability*, or a reference to an *identity*
- implicit-converts from string or from domain objects (*animated ability*, *identity*) to provide a uniform carrier type across element subtypes

---

### attack *(is a type of animated ability)*

- is flagged with an attack indicator and an optional area-effect indicator, enabling combat targeting restrictions
- carries an *on-hit animation* — an *animated ability* played on each *defender* when the attack connects
- manages *attack configuration* entries on each involved *character*, recording attack mode, effect severity, knockback distance, and hit/miss result

---

### on-hit animation

- is a property of *attack* — a reference to an *animated ability* played on each *defender* when the *attack* connects

---

## Character Movement

A *character movement* is a named, configured locomotion behavior a *character* can use — Walk, Run, Swim, Fly, Jump, and others. Each wraps a movement implementation that issues *keybinds*, animates the *character* in incremental steps, handles floor and wall collision, tracks *distance count* against a *distance limit*, and turns the *character* to face its destination. The *character movement* is an option on the *character's* Movements *option group*, with an optional *activation key*. Crowd-level movement dispatches all *crowd members'* movements using relative or optimal-spread positioning algorithms.

- activates by fetching the animation resource for the target locomotion style, targeting the *character*, and issuing the *keybind*
- moves the *character* to a destination vector in incremental steps, computing the path, issuing movement *keybinds*, pausing between steps to allow the game to process, and detecting floor collision to maintain ground contact
- turns the *character* to face a target vector by computing a look-at rotation matrix, decomposing the angle, and rotating in 2-degree increments until the target bearing is reached
- tracks *distance count* by measuring Euclidean distance from the movement start point and expressing it in tabletop units (÷ 8 game units)
- enforces *distance limit* by refusing further movement steps when *distance count* exceeds the cap for the current movement
- accepts a *movement instruction* carrying the current rotation axis direction, enabling continuous turn operations during movement
- **Invariant:** only one *character movement* can be active at a time on a *character*; activating a new movement deactivates the previous one

### Decisions made

- `character movement` is a top-level concept: distinct identity (name, activation key), rich behavior (move, turn, distance tracking, collision), and a lifecycle (activate/deactivate/pause/resume)
- `movement instruction` is a concept: distinct transient state (rotation axis direction), distinct purpose (coordinate incremental turns), and a clear creation/consumption lifecycle within a single turn operation
- `movement type` (Walk, Run, Swim, etc.) is a *property* — the name string that selects which movement implementation to use; the movement types differ in speed and animation resource, not in structural behavior
- `activation key` is a *property* — a keyboard key assignment on *character movement*, same pattern as on *animated ability*
- `movement direction` (Upward/Downward) is a *property* on *movement instruction*
- `crowd movement` is a behavior on *crowd* and *roster* (relative/optimal spread dispatch), not a separate concept
- `floor collision` and `wall collision` are behaviors within the movement implementation, not separate concepts

### References

**Ref — Movement.cs (CharacterMovement)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Movements\Movement.cs
Locator: lines 30–80
Extract: partial

**Ref — Character.cs (TurnTowards — MovementInstruction usage)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 1669–1729
Extract: partial

---

### movement instruction

- holds the current rotation axis direction (Upward or Downward) for an in-progress turn operation, letting the *character movement* apply consistent rotation direction across multiple incremental turn steps
- is created on demand at the start of a turn and referenced until the turn completes

---

## Roster

The *roster* is the live staging area for a session — the set of *crowd members* the GM has promoted for active play and potentially *spawned* onto the desktop (the visible game window). A *character* enters the *roster* when the GM adds it from the *crowd* tree; it is tracked as a *spawned character* once spawn succeeds. The *roster* mediates all desktop interaction: mouse click selection, drag-to-move, double-click activation, and context-menu operations all operate on *roster* members. It maintains the *active character* turn state and coordinates gang-mode activation.

- tracks all *crowd members* that have been added to active play, maintaining their *spawned state*, *active* status, and *gang leader* designation
- syncs the application selection with the game's target pointer — selecting a *character* in the UI triggers targeting in the game, and the game's current target is reflected back into the selection
- spawns a *character* to the desktop on GM command by calling spawn on the *crowd member* and registering the result as a *spawned character*
- clears a *character* from the desktop by calling clear-from-desktop on the *crowd member* and removing it from the live-spawned set
- activates a *crowd member* (marking its turn) and deactivates it at turn end, coordinating with *gang mode* to activate/deactivate the full group
- dispatches desktop context-menu actions — place at location, move to camera, save position, clone-link, activate — to the targeted *crowd member*
- **Invariant:** the game's current target pointer must match the application's selected *character* before any game-side command is issued; targeting is always synchronized before command dispatch

### Decisions made

- `roster` is a top-level concept: distinct identity (the live session staging set), rich behavior (spawn, clear, target sync, activate, context menu dispatch), and session-scoped lifecycle
- `spawned character` is a *property* — a *character* in spawned state that is a *roster* member; not a separate concept
- `desktop` is a concept: distinct rendering state (character overlays, status indicators), distinct behaviors (translate mouse events → character commands, context menu dispatch), and a session-scoped lifecycle
- `active character` is a *property* — the active flag on a *roster* member; the activation behavior belongs to *roster*
- `gang activation` is a behavior on *roster* and *crowd*, not a separate concept
- `character targeting` is a concept: distinct behavior (issue target keybind, poll until confirmed), a clear pre-condition role in the command flow, and its own failure/timeout logic
- it is placed under *roster* because targeting is a session-level selection mechanism, not an intrinsic property of the *character* data model

### References

**Ref — story-map.md (Manage Game Roster / Interact with Roster on Desktop)**
Source: docs/story-map.md
Locator: lines 63–91
Extract: partial

**Ref — Character.cs (Target / WaitUntilTargetIsRegistered)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Characters\Character.cs
Locator: lines 767–1099
Extract: partial

---

### desktop

- renders *spawned characters* as interactive overlays on the game window — showing position, status indicators (active, attacker, defender, stunned), and progress bars for each *spawned character*
- receives GM mouse click, multi-select, and drag events, translating screen coordinates to *character* targeting and movement commands
- responds to double-click by activating the clicked *character* and triggering its *default ability*
- presents a context menu on right-click, exposing spawn, place, move-to-camera, save position, clone-link, and activate-option commands for the targeted *character*

---

### character targeting

- resolves the game's live target pointer to the application's selected *character* by issuing a target *keybind* or directing the *memory element* by pointer
- polls the *memory element* in a tight loop until the game's current NPC label matches the *character's* label, confirming targeting registration, producing a confirmed *memory element* or a timeout
- is a prerequisite for every game-side command (spawn, move, load costume, delete) — all commands assume the targeted NPC is the intended *character*

---

## Game Bridge

The *game bridge* is the exclusive communication channel between the application and the COH game engine. All game-side mutations flow through one of three paths: *keybind files* written to disk and loaded via the two-key idiom, the *HookCostume DLL* for direct native queries, or *game memory* read/write at known pointer offsets via the *memory element*. No managed code in the application calls game APIs directly — every command is serialized into a *keybind file* or a DLL call.

- generates *keybind files* for each game event, writing a loader-key/trigger-key chain to the COH data directory and injecting commands into the game through key presses
- reads the NPC label, world-space position, and memory pointer of the currently hovered NPC via the *HookCostume DLL*
- reads the 3D mouse cursor position in world space from the game engine via the *HookCostume DLL*
- queries collision detection — whether a raycast from point A to point B is obstructed by geometry — via the *HookCostume DLL*
- reads and writes *character* position, model matrix, and facing vector directly in game process memory via the *memory element*
- initializes the *HookCostume DLL* on session start and closes it on session end
- **Invariant:** all game-side commands flow through the *keybind* mechanism or the *HookCostume DLL*; the application never invokes game engine code from managed memory directly

### Decisions made

- `game bridge` is a top-level concept: distinct identity (the communication layer boundary), rich behavior (keybind generation, DLL queries, memory access), and a session-scoped initialization/shutdown lifecycle
- `keybind` is a concept: distinct structure (command + arguments string), distinct assembly behavior, and a key role in the command pipeline
- `costume command` is the load-costume game event — it is an instance of *keybind* vocabulary, not a separate concept
- `keybind file` is a concept: distinct on-disk identity, behavior (write, load, execute, reload chain), and the two-key idiom protocol that enables repeated command injection
- `game event` is a *property* — the enum value that maps to a command string in the *game bridge*; it is a discriminator, not an independent concept
- `HookCostume DLL` is a concept: distinct native identity, distinct API (query hovered NPC, mouse 3D pos, collision raycast, game done state), and initialization/shutdown lifecycle
- `memory element` is a concept: distinct identity (NPC memory pointer), distinct behavior (pointer acquisition, label validation, stale-detection), and a lifecycle tied to *character targeting*
- `camera` is a concept: distinct readable state (world-space position from memory), distinct behaviors (report position, be the maneuvered target, be detached), and a session-scoped role in the movement workflow
- `pop-up menu` is a thin concept: it has on-disk identity and a load *keybind*, but minimal behavior beyond file write and load; modeled as a stub rather than a full concept

### References

**Ref — KeyBindsGenerator.cs / story-map.md (Communicate with Game Engine)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Library\GameCommunicator\KeyBindsGenerator.cs
Locator: lines 16–188
Extract: partial

**Ref — story-map.md (Bridge via HookCostume DLL)**
Source: docs/story-map.md
Locator: lines 218–225
Extract: partial

**Ref — Identities/Camera.cs (GetPositionVector) / Character.cs (ManeuveringWithCamera)**
Source: HeroVirtualTableTop\HeroVirtualTabletop.WPF\Modules\Module.HeroVirtualTabletop\Identities\Camera.cs
Locator: (GetPositionVector reads XYZ from game memory at camera pointer offset)
Extract: partial

**Ref — story-map.md (Manage Pop-Up Menus)**
Source: docs/story-map.md
Locator: lines 266–269
Extract: partial

**Ref — Reverse Engineering Stories.txt (memory pointer layout)**
Source: Design/Reverse Engineering Stories.txt
Locator: lines 12–18
Extract: whole

---

### keybind

- encodes one COH slash command and its arguments as a string (e.g. `spawn_npc Model_Statesman Hero1`)
- is assembled by the *game bridge* from a typed game event and zero or more string arguments
- is written into a *keybind file* alongside a loader-key binding and the trigger-key re-bind so the two-key idiom can be re-used on the next command

---

### keybind file

- is a text file written to `<coh_dir>/data/custom_keybinds.txt` containing two lines: a trigger-key command line and a self-referential re-bind of the trigger key
- is loaded into the game via a bind-load-file command (the loader key), then executed by pressing the trigger key, then auto-reloaded for the next command
- chains multiple commands in sequence by writing them all before the single complete-event call, which writes the final file and presses the loader key

---

### HookCostume DLL

- is loaded from `<coh_dir>/HookCostume.dll` at session start, establishing a native bridge into the game process
- queries the NPC name and world-space position of the NPC currently under the mouse cursor, enabling hover-to-target functionality
- queries the 3D world-space position of the mouse cursor in the game scene
- performs collision-detection raycasts between two world-space points, reporting whether a straight-line path is obstructed by game geometry
- checks whether the game process has exited, enabling clean application shutdown
- is closed at session end

---

### memory element

- obtains the target NPC's memory pointer from the game process by reading the pointer-to-target address after the *character* has been successfully targeted
- reads the NPC's label string from the pointer offset to confirm the targeted entity matches the expected *character*
- exposes the memory pointer as an address for *position* to use in reading/writing X, Y, Z coordinates and the model matrix
- detects staleness — when the pointer no longer points to the correct NPC — and triggers a re-target and pointer refresh
- **Invariant:** a *memory element* is valid only when the game's current target matches the *character's* label; stale pointers must be refreshed before any read or write

---

### camera

- reports its current world-space position by reading the camera position vector from game memory at the camera pointer offset
- is the destination for "move to camera" and "teleport to camera" operations — the *character* or *crowd* moves to wherever the GM has aimed the game camera
- receives a *character* reference in *maneuvering with camera* mode and continuously pushes its current position to that *character's* position, creating camera-driven locomotion
- can be detached from the player model to enable free-camera movement independent of the player NPC

---

### pop-up menu

- is a COH menu definition file written to `<coh_dir>/data/texts/English/Menus/` and loaded in-game via a pop-up menu *keybind*
- is used for the area-attack selection menu; has on-disk identity and a load *keybind*, but minimal session state or behavior beyond triggering the menu

---

### COH game directory

- is the file-system path to the City of Heroes installation, validated at startup before any *game bridge* operations begin
- provides the root for all derived paths: the *HookCostume DLL* location, the COH data directory (for *keybind files* and the *crowd repository*), and the costumes directory (for *costume files*)
- is stored in application configuration and prompted from the GM when absent or invalid
- **Invariant:** the *game bridge* cannot initialize and no game-side operations can proceed until the *COH game directory* is confirmed valid

---

# Boundary Domain

### HCS (Hero Combat System) *(owned by: External Combat System)*

- an external application that writes event info files to a shared directory watched by the Hero Virtual Tabletop
- processes attack result events by writing a file that the *game bridge's* file watcher reads and dispatches as attack, simple-ability, held-character, or sweep-result events
- provides turn order data (on-deck combatants, eligible combatants, active character, chronometer turn state) that the *roster* reads to highlight the current actor

### Decisions made

- scope-fit: the HCS owns the combat sequencing and turn-management protocol; the Hero Virtual Tabletop only reads HCS output via file watcher — it does not define the HCS event schema; boundary placement is correct

### References

**Ref — story-map.md (Integrate Hero Combat System)**
Source: docs/story-map.md
Locator: lines 170–180
Extract: whole

---

### COH Game Engine *(owned by: City of Heroes Platform)*

- the Titan Icon fork of the City of Heroes client that the Hero Virtual Tabletop drives via *keybind files*, *slash commands*, and the *HookCostume DLL*
- hosts NPCs (spawnable *characters*), the 3D map, the *camera*, costumes, pop-up menus, and sound/FX systems
- exposes no public API; all control is through file-system conventions (*keybind files*, *costume files*, pop-up menu files) and memory read/write via the *HookCostume DLL*

### Decisions made

- scope-fit: the COH game engine is the rendering and physics substrate; it is entirely external and unmodified; all interaction is boundary-level

### References

**Ref — Reverse Engineering Stories.txt**
Source: Design/Reverse Engineering Stories.txt
Locator: lines 1–104
Extract: whole

**Ref — story-map.md (Launch and Initialize Session)**
Source: docs/story-map.md
Locator: lines 12–28
Extract: whole

---
