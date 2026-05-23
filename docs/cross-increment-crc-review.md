# Cross-Increment CRC Consistency Review

Reviewed files:
- `docs/increment-1/crc-increment-1.md`
- `docs/increment-2/crc-increment-2.md`
- `docs/increment-3/crc-increment-3.md`
- `docs/increment-4/crc-increment-4.md`
- `docs/increment-5/crc-increment-5.md`
- `docs/increment-6/crc-increment-6.md`
- `docs/corrections-log.md`

---

## Pass 1 — Class name consistency

### F1 — HIGH: `Persistent Ability` class anticipated in Inc 2 but never created in Inc 3

**Inc 2 boundary — `Persistent Ability` class block (verbatim):**
```
### **Persistent Ability**
stop on identity switch                | Character, Active Identity
                                       |   invariant: must be stopped before the old active identity is despawned during an identity switch
```

**Inc 2 boundary — decisions (verbatim):**
> `Persistent Ability` is boundary: full lifecycle owned by Increment 3; only the "stop on identity switch" behavior is in scope for this increment

Inc 2 explicitly names a forward class it expects Increment 3 to define. Inc 3 does not define `Persistent Ability` as a class. The concept is instead represented as:

**Inc 3 core — `Animated Ability` (verbatim):**
```
persistence designation               | (persistent or non-persistent)
```

**Inc 3 decisions (verbatim):**
> `persistent ability` maps to the `persistence designation` property on Animated Ability: the persistence flag gates auto-replay on identity load but the ability's execution pipeline is unchanged

The result is that Inc 2 declares "Persistent Ability: owned by Inc 3" and Inc 3 eliminates the class entirely. Any reader following the forward reference from Inc 2 will not find the promised class. This is a broken forward reference that must be resolved — either Inc 2's boundary entry is updated to reference the actual `Animated Ability` / `persistence designation` pattern, or the decisions on the Inc 3 side must explicitly note the class was retired and why.

---

### F2 — HIGH: `Crowd Tree` and `Crowd Repository` both claim load/save lifecycle in Inc 1

The following responsibilities appear on **both** `Crowd Tree` and `Crowd Repository` in the same file with overlapping names, collaborators, and invariants:

**`Crowd Tree` (verbatim):**
```
load active crowd files on open     | Active Crowd List, Crowd File, Crowd
save dirty crowds                   | Crowd, Crowd File, Daily Backup
  invariant: only crowds with dirty flag set are written; each to its own source file; clean crowds are not touched
  invariant: a dirty crowd with no source file is automatically routed to save crowd to new file; cancellation leaves it unsaved
save crowd to new file              | Crowd, Crowd File, Active Crowd List
  invariant: on success the chosen path becomes the crowd's source file, appended to active crowd list, dirty flag cleared
  invariant: only top-level crowds may be saved to new file; nested crowd selection is rejected
```

**`Crowd Repository` (verbatim):**
```
load active crowd files on startup  | Active Crowd List, Crowd File, Crowd, Daily Backup
save dirty crowds to source files   | Crowd, Crowd File, Daily Backup
  invariant: a non-dirty crowd's source file is never written; a failed write leaves the crowd dirty and other writes proceed
  invariant: a dirty crowd with no source file is routed to Save Crowd to New File — the dialog opens automatically; cancellation leaves that crowd unsaved with no source file
save crowd to new file              | Crowd, Crowd File, Active Crowd List
  invariant: on success the new path becomes the crowd's source file and is appended to the active crowd list
```

`save crowd to new file` is **literally identical** across both classes (same name, same collaborators). The save-dirty-crowds operations share the same invariants. The load operations describe the same operation with a different trigger label.

The **decisions contradict each other directly**:

**Crowd KA decisions (verbatim):**
> `Crowd Tree` absorbs `Crowd Manager`; file-level and structural operations (load, save, browse-activate, nest, move) live on the tree

**Crowd Repository KA decisions (verbatim):**
> `Crowd Repository` is now an in-memory aggregator, not a single-file store; it owns the load/save lifecycle

One decision places file/save ownership on `Crowd Tree`; the other places it on `Crowd Repository`. Both cannot be the owner. This must be resolved by either (a) having `Crowd Tree` initiate operations that delegate to `Crowd Repository`, with the delegation made explicit in collaborators, or (b) removing the duplicate blocks from one class.

---

### F3 — MEDIUM: `Active Character HCS` — naming inconsistency within Inc 6

**Inc 6 core class block header (verbatim):**
```
### **Active Character HCS**
HCS active turn designation           | Roster Entry
  invariant: if the active character (HCS) does not match any roster entry, the event is logged and no roster selection change is made
synchronize with HVT active character | Active Character
```

**Inc 6 decisions (verbatim):**
> `Active Character (HCS)` is distinguished from `Active Character` (Increment 5)

The class header uses `Active Character HCS` (no parentheses) while the decisions section uses `Active Character (HCS)` (with parentheses). This is a minor internal inconsistency within Inc 6, but since the class name is the canonical reference, the decisions should match it.

---

## Pass 2 — Option Group pattern consistency

### F4 — HIGH: `Option Group` base class in Inc 1 has concrete properties that conflict with its role as an abstract base

**Inc 1 `Option Group` (verbatim):**
```
### **Option Group**
canonical group name                | (Identities, Abilities, or Movements)
ordered name-keyed options          | (empty in Increment 1)
                                    |   invariant: exactly three canonical option groups must always exist on every character; each is created on first access but never absent
```

The Inc 1 definition carries:
- A `canonical group name` property constrained to exactly three values (Identities, Abilities, or Movements)
- An invariant: "exactly three canonical option groups must always exist on every character"

The subtypes that inherit from this base in later increments:

**Inc 2 (verbatim):** `### **Identity Option Group : Option Group**`
**Inc 3 (verbatim):** `### **Ability Option Group : Option Group**`
**Inc 4 (verbatim):** `### **Movement Option Group : Option Group**`

None of these subtypes carries a `canonical group name` property. None is constrained to a three-group rule. The "exactly three canonical option groups" invariant is a Character-level structural constraint, not a property of any individual Option Group. If the subtypes inherit the base's invariant and `canonical group name` property literally, they violate it — there are now three distinct Option Group types, not three instances of a single class.

The corrections log CRC-002 establishes the intended abstract base pattern:
> `### **Option Group** (base concept) / (abstract collection with selection semantics)`

But Inc 1's CRC never defines `Option Group` this way — it defines it as a concrete class with a specific enumeration. The base abstraction that Inc 2/3/4 inherit from has never been written down as an abstract base; only the concrete Inc 1 form exists. This means the `: Option Group` inheritance in Inc 2, 3, 4 has no well-defined abstract contract to satisfy.

**Required fix:** The `Option Group` block in Inc 1 should be split: retain the concrete In-Increment-1 behavior as a note on `Character`, and add a separate abstract `Option Group` base block with the general "type-safe collection with selection semantics" contract that Inc 2/3/4 subtypes inherit from.

---

### F5 — LOW: `Active Character` in Inc 5 owns a "single active at a time" selection invariant but does not follow the Option Group pattern

**Inc 5 `Active Character` (verbatim):**
```
### **Active Character**
active designation                    | Roster Entry
                                      |   invariant: at most one active character holds the single-character active turn at any time unless gang mode activates multiple entries collectively
```

The pattern established across Inc 2, 3, 4 is: whenever a domain collection owns an "at most one active" or "exactly one active" selection invariant, the collection is modeled as `<Concept> Option Group : Option Group`. `Active Character` owns exactly this invariant on the roster. It does not follow the pattern.

This may be intentional — `Active Character` is a session-scope turn-management construct, not an authored named-item collection. However, the asymmetry is worth noting for any reviewer applying the pattern consistently.

---

## Pass 3 — Shared collaborators referenced but not defined

### F6 — HIGH: `Active Identity` used as collaborator in Inc 2 boundary but does not exist as a class

**Inc 2 boundary `Persistent Ability` (verbatim):**
```
### **Persistent Ability**
stop on identity switch                | Character, Active Identity
```

`Active Identity` is listed as a collaborator. However, corrections log CRC-001 explicitly identifies `Active Identity` as a wrongly promoted property:

**Corrections log CRC-001 Example (wrong) (verbatim):**
```
### **Active Identity**       ← WRONG: this is a property, not a class
active designation | ...
```

The corrected Inc 2 CRC removes `Active Identity` as a standalone class and places the active-designation behavior on `Identity Option Group`:

**Inc 2 core `Identity Option Group` (verbatim):**
```
active identity                        | Identity
                                       |   invariant: exactly zero or one identity carries the active designation at any time; setting a new active clears the previous before the new activation sequence begins
```

The `Persistent Ability` boundary entry was not updated when CRC-001 was applied. The collaborator `Active Identity` now references a class that does not exist in the corrected CRC. The correct collaborator for the "stop before identity switch" interaction should reference `Identity Option Group` (which owns the active designation enforcement).

---

### F7 — MEDIUM: `Persistent Ability` referenced as collaborator in Inc 2 `Game Bridge` but never defined in any increment

**Inc 2 `Game Bridge` core (verbatim):**
```
execute identity deactivation          | Identity, Spawned NPC, Delete NPC Command, Persistent Ability
```

`Persistent Ability` appears as a formal collaborator in a core class responsibility. As established in F1, `Persistent Ability` was never defined as a class in any increment — Inc 3 chose to represent the concept as `persistence designation` property on `Animated Ability`. The `Game Bridge` deactivation responsibility should reference the collaborator that actually performs stop-on-deactivation. In Inc 3, that would be `Animated Ability` (via `execution state`) coordinated through `Ability Option Group`.

---

### F8 — MEDIUM: Character boundary definitions (Inc 2, 3, 4) use item-type class as collaborator instead of collection-class

In Inc 2, 3, and 4, the `Character` boundary sections describe the character's option group responsibilities. In each case, the collaborator names the **item type** that goes inside the group, not the **collection class** that manages the group:

**Inc 2 boundary `Character` (verbatim):**
```
Identities option group                | Identity
```
The collection class for identities is `Identity Option Group : Option Group`. The collaborator should be `Identity Option Group`, not `Identity`.

**Inc 3 boundary `Character` (verbatim):**
```
Abilities option group                | Animated Ability
```
The collection class is `Ability Option Group : Option Group`. Collaborator should be `Ability Option Group`.

**Inc 4 boundary `Character` (verbatim):**
```
Movements option group                | Character Movement
```
The collection class is `Movement Option Group : Option Group`. Collaborator should be `Movement Option Group`.

By contrast, Inc 1's `Character` correctly references the collection class:

**Inc 1 `Character` (verbatim):**
```
option groups                       | Option Group
```

The collaborator here names the collection class. Inc 2, 3, 4 degraded this to naming the item class, losing the structural significance of the Option Group collection.

---

### F9 — LOW: `Character Position`, `Character Facing Vector`, `Character Rotation Matrix` used as named collaborators in Inc 4 though they are properties, not classes

**Inc 4 `Movement Distance Count` (verbatim):**
```
increment after step                  | Movement Execution, Character Position
```
**Inc 4 `Floor Collision` (verbatim):**
```
detect floor intersection             | Movement Execution, Character Position
anchor at contact point               | Character Position
```
**Inc 4 `Camera Follow` (verbatim):**
```
track character position              | Memory Interface, Character Position, Spawned NPC
```
**Inc 4 `Movement Execution` (verbatim):**
```
turn spawned NPC to face target       | Memory Interface, Character Facing Vector, Character Rotation Matrix
reset character orientation           | Memory Interface, Character Rotation Matrix
```

**Inc 4 decisions (verbatim):**
> `character position`, `character model matrix`, `character rotation matrix`, `character facing vector`, `camera position` are properties of Memory Interface — documented as named fields whose read/write behaviors are individually storied, but their lifecycle and identity are fully owned by the Memory Interface service

The decision correctly states these are properties, but they are still used as collaborator labels across the same document. This makes the status of these concepts ambiguous to a reader — are they classes or properties? The decisions resolve it, but the inconsistency in the CRC body itself should be addressed, either by replacing the property names with `Memory Interface` as the single collaborator or by adding explicit notes that these are named property references.

---

## Pass 4 — Responsibility overlap / contradiction

### F10 — HIGH: Same as F2 above (documented there in full)

To summarize the overlap count from Inc 1 in one place: `Crowd Tree` and `Crowd Repository` share three duplicate responsibility blocks — `save dirty crowds`, `save crowd to new file`, and `load active crowd files` — with overlapping invariants and the decisions contradiction quoted in F2. All three must be unambiguously owned by exactly one class with the other class delegating.

---

### F11 — LOW: `Spawned NPC` and `Spawned State` both use the label "presence in game world" for different concepts

**Inc 2 `Spawned NPC` (verbatim):**
```
presence in game world                 | (present or absent)
                                       |   invariant: must be present before any load costume command or ghost alignment can be applied
```

**Inc 5 `Spawned State` (verbatim):**
```
presence in game world                | (true or false)
set on spawn                          | Game Bridge, Spawned NPC, Roster Entry
clear on despawn                      | Game Bridge, Roster Entry
clear on game done                    | Game Done State, Roster Entry
```

`Spawned NPC` is the game-side entity; its "presence in game world" is the entity's own existence state. `Spawned State` is a roster-tracking concept on a `Roster Entry`; its "presence in game world" is the roster's view of whether a character's NPC has been spawned. The identical label applied to two semantically distinct properties is a vocabulary collision. The `Spawned NPC` property should be "entity presence" or simply retained as an internal state label, while `Spawned State` describes the roster-level observation.

---

### F12 — LOW: `Animated Ability` has `execution state` property while `Ability Option Group` independently tracks `active abilities` — dual-tracking of the same truth

**Inc 3 `Animated Ability` (verbatim):**
```
execution state                       | (executing or stopped)
```

**Inc 3 `Ability Option Group` (verbatim):**
```
active abilities                      | Animated Ability (collection — multiple may be active simultaneously)
                                      |   invariant: persistent abilities remain active until explicitly stopped; non-persistent stop when a new non-persistent ability starts
```

**Inc 3 `Ability Activation Eligibility` (verbatim):**
```
  invariant: eligibility is ineligible when: no activation key assigned, ability currently executing, or character not spawned
```

The eligibility invariant consults `ability currently executing` — but it is unclear whether this queries the `execution state` property on the `Animated Ability` instance, or whether it checks membership in the `Ability Option Group`'s `active abilities` collection. Both representations exist simultaneously, and no delegation or source-of-truth assignment is documented between them. An ability that is executing should be in `active abilities` AND have `execution state = executing`, but neither class says who sets the other.

---

## Pass 5 — Vocabulary drift

### F13 — MEDIUM: "Abilities/Identities/Movements option group" (boundary, plural, lowercase) vs "Ability/Identity/Movement Option Group" (core class, singular, title case)

The boundary descriptions of `Character` across Inc 2, 3, and 4 name the option groups with plural, lowercase forms. The core class names in those same increments use singular, title-case forms.

| Increment | Boundary label (verbatim) | Core class name (verbatim) |
|-----------|--------------------------|---------------------------|
| Inc 2 | `Identities option group` | `Identity Option Group : Option Group` |
| Inc 3 | `Abilities option group` | `Ability Option Group : Option Group` |
| Inc 4 | `Movements option group` | `Movement Option Group : Option Group` |

The plural/singular drift ("Identities" vs "Identity", "Abilities" vs "Ability", "Movements" vs "Movement") means the boundary label does not match the class name it should reference. In a domain model, property labels should match the canonical class name of the collaborating concept. All three boundary labels on `Character` should be updated to match their corresponding class names.

---

### F14 — LOW: "COH data directory root" (Inc 1) vs "COH Data Directory" (Inc 2/3) — three different names for one derived path concept

**Inc 1 `COH Game Directory` property label (verbatim):**
```
COH data directory root             | (derived path: stored path / data /)
```

**Inc 2 boundary `COH Game Directory` property label (verbatim):**
```
COH data directory path                | (COH Data Directory)
```

**Inc 3 `Resource Catalog` collaborator (verbatim):**
```
data file path                        | COH Data Directory
load from data file                   | COH Data Directory
seed from embedded CSV                | Embedded CSV, COH Data Directory
```

Three names appear for the same derived path: "COH data directory root" (Inc 1), "COH data directory path" with value "(COH Data Directory)" (Inc 2), and "COH Data Directory" as a bare collaborator (Inc 3). The canonical name, once chosen (the corrections log and Inc 3 usage suggest "COH Data Directory"), should be used consistently in Inc 1 and Inc 2.

---

## Pass 6 — Missing / asymmetric coverage

### F15 — MEDIUM: `Persistent Ability` forward-referenced in Inc 2 boundary was retired in Inc 3 without a note, leaving Inc 2's boundary incomplete

This is the boundary-coverage side of F1. The issue is not only that the class is missing in Inc 3 (F1) but that Inc 2's boundary documentation actively misleads: it says a class is coming, names the class, and specifies behavior for it — none of which materializes in Inc 3. The Inc 2 boundary section's decision:

**Inc 2 boundary decisions (verbatim):**
> `Persistent Ability` is boundary: full lifecycle owned by Increment 3; only the "stop on identity switch" behavior is in scope for this increment

was accurate at the time of writing but became stale when Inc 3 chose a property-based representation. This pattern — where a boundary entry forwards to a class that the target increment subsequently didn't create — leaves any reader of Inc 2 in a false state.

The `Persistent Ability` boundary block in Inc 2 should be removed and replaced with a note pointing to `Animated Ability` / `persistence designation` / `Ability Option Group` as the implemented representation of the concept in Inc 3.

---

### F16 (supplemental to F4) — LOW: No increment defines `Option Group` as an abstract base with the contract the subtypes depend on

The corrections log CRC-002 describes the intended pattern:

**Corrections log CRC-002 (verbatim):**
```
### **Option Group** (base concept)
(abstract collection with selection semantics)
```

This abstract base block does not exist in any increment. Inc 1 defines a concrete `Option Group` with specific properties (F4). Inc 2, 3, 4 use `: Option Group` inheritance syntax. The abstract contract — what an Option Group guarantees, what behaviors its subtypes must honour — is never written down. This means there is no normative definition against which the three typed Option Groups can be validated. The CRC-002 corrections log established the need for this block but it was not added.

---

## Summary

| # | Finding | Severity | Increments affected |
|---|---------|----------|---------------------|
| F1 | `Persistent Ability` forward-referenced in Inc 2 boundary; Inc 3 never created the class | HIGH | Inc 2, Inc 3 |
| F2 | `Crowd Tree` and `Crowd Repository` duplicate load/save responsibilities; decisions contradict each other | HIGH | Inc 1 |
| F3 | `Active Character HCS` — class header uses no parentheses; decisions write `Active Character (HCS)` with parentheses | MEDIUM | Inc 6 |
| F4 | `Option Group` base class in Inc 1 has concrete properties/invariants that conflict with its role as abstract base for Inc 2/3/4 subtypes | HIGH | Inc 1 (spillover: Inc 2, 3, 4) |
| F5 | `Active Character` in Inc 5 owns a single-active-at-a-time selection invariant but does not follow the `<Concept> Option Group : Option Group` pattern | LOW | Inc 5 |
| F6 | `Active Identity` used as collaborator in Inc 2 boundary `Persistent Ability` block; explicitly identified as a non-existent wrong concept by corrections log CRC-001 | HIGH | Inc 2 |
| F7 | `Persistent Ability` used as collaborator in Inc 2 `Game Bridge` "execute identity deactivation"; never defined as a class in any increment | MEDIUM | Inc 2 |
| F8 | Character boundary definitions (Inc 2, 3, 4) name item types (`Identity`, `Animated Ability`, `Character Movement`) instead of collection class types (`Identity Option Group`, `Ability Option Group`, `Movement Option Group`) | MEDIUM | Inc 2, Inc 3, Inc 4 |
| F9 | `Character Position`, `Character Facing Vector`, `Character Rotation Matrix` used as named collaborators in Inc 4 body though documented as Memory Interface properties, not classes | LOW | Inc 4 |
| F10 | (same root as F2) Three duplicate responsibility blocks; both classes claim ownership | HIGH | Inc 1 |
| F11 | `Spawned NPC` and `Spawned State` both use label "presence in game world" for distinct concepts | LOW | Inc 2, Inc 5 |
| F12 | `Animated Ability` carries `execution state`; `Ability Option Group` carries `active abilities` — no source-of-truth delegation documented | LOW | Inc 3 |
| F13 | Plural-lowercase boundary labels ("Abilities option group") vs singular-title-case class names ("Ability Option Group") in Inc 2, 3, 4 | MEDIUM | Inc 2, Inc 3, Inc 4 |
| F14 | "COH data directory root" (Inc 1) vs "COH Data Directory" (Inc 2/3) — vocabulary drift across three increments | LOW | Inc 1, Inc 2, Inc 3 |
| F15 | Inc 2 boundary documents `Persistent Ability` as "owned by Inc 3"; Inc 3 retired the class without updating Inc 2 | MEDIUM | Inc 2, Inc 3 |
| F16 | No increment defines an abstract `Option Group` base class with the contract the subtypes need; only the Inc 1 concrete form exists | LOW | Inc 1 (spillover: Inc 2, 3, 4) |

**Total findings: 16**
- High: 5 (F1, F2, F4, F6, F10)
- Medium: 5 (F3, F7, F8, F13, F15)
- Low: 6 (F5, F9, F11, F12, F14, F16)

**Files requiring CRC edits:**
- `docs/increment-1/crc-increment-1.md` — F2/F10 (Crowd Tree / Crowd Repository duplicate responsibilities + decision contradiction); F4/F16 (Option Group base class concrete definition vs abstract base role)
- `docs/increment-2/crc-increment-2.md` — F1/F15 (Persistent Ability forward reference must be retired); F6 (Active Identity collaborator must be replaced with Identity Option Group); F7 (Persistent Ability collaborator in Game Bridge must be corrected); F8/F13 (Character boundary collaborator names and label casing)
- `docs/increment-3/crc-increment-3.md` — F1/F15 (no Persistent Ability class — add explicit retirement note in boundary decisions); F8/F13 (Character boundary collaborator names and label casing); F12 (clarify execution-state / active-abilities dual-tracking)
- `docs/increment-4/crc-increment-4.md` — F8/F13 (Character boundary collaborator names and label casing)
- `docs/increment-6/crc-increment-6.md` — F3 (align Active Character HCS class name in decisions to match header)
