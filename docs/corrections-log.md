# Corrections Log

## CRC-001: Properties promoted to classes

**Context:** abd-class-responsibility-collaborator output for increment-2 (crc-increment-2.md)
**Affects:** stage: story-definition, role: Analyst, skill: abd-class-responsibility-collaborator, increments: all

**DO NOT** create separate CRC class blocks for properties/designations that already appear as properties on a parent class. A boolean designation (active, default, persistent) is a property on the owning class — not a standalone class.

**DO** express designation invariants (at-most-one-active, at-most-one-default) on the property line of the owning class or on the collection class that enforces them.

**Example (wrong):**
```
### **Identity**
active designation | (active or inactive)
default designation | (default or unset)

### **Active Identity**       ← WRONG: this is a property, not a class
active designation | ...

### **Default Identity**      ← WRONG: same
default designation | ...
```

**Example (correct):**
```
### **Identity**
identity name | ...
costume surface | Costume File   (on Costume Identity subtype)
model name | Model              (on Model Identity subtype)

### **Identity Option Group : Option Group**
active identity | Identity       ← at most one active at a time
default identity | Identity      ← at most one default
  invariant: exactly zero or one carries active; setting new active clears previous
  invariant: at most one carries default; may be cleared without assigning another
```

**Likely source:** `prompt gap` — CRC skill rules don't explicitly prohibit promoting properties to classes when their invariants are "interesting"

**Status:** confirmed

---

## CRC-002: Option Group is a missing base abstraction

**Context:** Cross-increment pattern — Identities (inc-2), Abilities (inc-3), Movements (inc-4) all share the same collection-with-bespoke-selection-properties pattern
**Affects:** stage: story-definition, role: Analyst, skill: abd-class-responsibility-collaborator, increments: 2, 3, 4

**DO** model type-safe domain collections as `<Name> Option Group : Option Group` classes when the collection has bespoke selection/designation behavior (active, default, persistent).

**DO** place the at-most-one / multiple-active / default invariants on the Option Group class — that's where enforcement lives.

**DO NOT** scatter designation invariants across individual item classes as if each item independently knows whether it's "the active one."

**Pattern:**
```
### **Option Group** (base concept)
(abstract collection with selection semantics)

### **Identity Option Group : Option Group**
active identity | Identity
  invariant: exactly zero or one active at a time; setting new clears previous
default identity | Identity
  invariant: at most one default; may be cleared

### **Movement Option Group : Option Group**
active movement | Character Movement
  invariant: exactly one active at a time
default movement | Character Movement
  invariant: at most one default

### **Ability Option Group : Option Group**
active abilities | Animated Ability (collection — multiple may be active)
  invariant: persistent abilities remain active until explicitly stopped
default ability | Animated Ability
  invariant: at most one default; auto-plays on spawn
```

**Likely source:** `edge case` — CRC skill doesn't have a pattern for domain collections that own selection invariants

**Status:** confirmed

---

## CRC-003: Cross-increment CRC consistency fixes

**Date:** 2026-05-21
**Source:** cross-increment-crc-review.md (findings F1–F16)

### Changes applied

| Finding | File | Change |
|---------|------|--------|
| F2/F10 | crc-increment-1.md | Crowd Tree / Crowd Repository — clarified distinct roles (UI trigger vs persistence layer); renamed Crowd Tree responsibilities to surface delegation to Crowd Repository |
| F14 | crc-increment-1.md | COH data directory root → COH Data Directory (vocabulary unification) |
| F14 | crc-increment-2.md | COH data directory path → COH Data Directory |
| F1/F15/F6 | crc-increment-2.md | Persistent Ability boundary block retired; Active Identity collaborator replaced; forward-reference resolved to Animated Ability / persistence designation / Ability Option Group |
| F7 | crc-increment-2.md | Game Bridge execute identity deactivation: Persistent Ability collaborator → Animated Ability |
| F8/F13 | crc-increment-2.md | Character boundary: Identities option group / Identity → Identity Option Group / Identity Option Group |
| F11 | crc-increment-2.md | Spawned NPC: presence in game world → entity presence (avoids collision with Spawned State in Inc 5) |
| F1/F15 | crc-increment-3.md | Added retirement note for Persistent Ability class in boundary decisions |
| F8/F13 | crc-increment-3.md | Character boundary: Abilities option group / Animated Ability → Ability Option Group / Ability Option Group |
| F12 | crc-increment-3.md | Ability Option Group.active abilities: added note that it is a derived filter over execution state, not parallel truth |
| F8/F13 | crc-increment-4.md | Character boundary: Movements option group / Character Movement → Movement Option Group / Movement Option Group |
| F9 | crc-increment-4.md | Replaced property-named collaborators (Character Position, Character Facing Vector, Character Rotation Matrix) with Memory Interface throughout |
| F3 | crc-increment-6.md | Active Character (HCS) → Active Character HCS in decisions section |

### Dropped findings (by user decision)

| Finding | Reason |
|---------|--------|
| F4/F16 | Inheriting from a concrete class is valid; no fix required |
| F5 | Active Character is a roster selection concern, not a collection-with-options; Option Group pattern does not apply |

---

## SBE-001: SBE aligned to CRC changes (Option Group pattern and vocabulary)

**Date:** 2026-05-21
**Source:** cross-increment CRC fixes (CRC-001, CRC-002, CRC-003)

### Changes applied

| File | Change |
|------|--------|
| specification-by-example-increment-2.md | `presence in game world` → `entity presence` (Spawned NPC rename) |
| specification-by-example-increment-2.md | `Identities option group` → `Identity Option Group` throughout |
| specification-by-example-increment-2.md | `active_designation`/`default_designation` ownership moved from Identity steps to Identity Option Group steps |
| specification-by-example-increment-2.md | `**Persistent Abilities**` → `persistent abilities` (not a class) |
| specification-by-example-increment-2.md | `**Active Identity**` → `active Identity in Identity Option Group` |
| specification-by-example-increment-3.md | `Abilities option group` → `Ability Option Group` throughout |
| specification-by-example-increment-3.md | `default_designation` removed from Animated Ability tables; moved to Ability Option Group step |
| specification-by-example-increment-4.md | `Movements option group` → `Movement Option Group` throughout |
| specification-by-example-increment-4.md | `default_movement_designation` removed from Character Movement tables; moved to Movement Option Group step |
