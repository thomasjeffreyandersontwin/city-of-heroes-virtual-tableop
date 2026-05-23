---
state: crc
increment: 6
scope: Crowd Orchestration and Combat
date: 2026-05-21
---

# CRC — Increment 6: Crowd Orchestration and Combat

> Domain sources: `docs/increment-6/ubiquitous-language-increment-6.md`, `docs/increment-6/acceptance-criteria-increment-6.md`.

---

# Core Domain

## **Crowd Move**

The coordinated movement capability that displaces all members of a crowd together as a spatial unit while preserving their relative arrangement.

### **Crowd Move**
positioning strategy                  | Relative Positioning, Optimal Spread Positioning
                                      |   invariant: every spawned member of the target crowd receives a move command; no member is left at its original position when a crowd move completes successfully
target crowd members                  | Crowd, Roster Entry
issue movement commands               | Movement Execution
apply facing after move               | Facing Destination, Gang Leader Facing, Gang Mode

### **Relative Positioning**
displacement vector                   | (uniform delta from origin to destination)
preserve spatial offsets              | Group Formation

### **Optimal Spread Positioning**
computed spread slots                 | (evenly distributed destinations around target point)
                                      |   invariant: every member receives a unique destination slot; no two members are assigned the same spread position
assign slots to members               | Roster Entry, Movement Execution

### **Group Formation**
relative spatial offsets              | (captured offset set between members)
capture at move start                 | Crowd Move
re-apply at move end                  | Crowd Move

### **Facing Destination**
facing vector                         | (per-member direction toward destination center)
apply after move completion           | Crowd Move, Movement Execution

### **Gang Leader Facing**
leader facing vector                  | Gang Leader, Gang Mode
apply to all gang members             | Roster Entry, Movement Execution

### references

**Ref — ubiquitous-language-increment-6.md (Crowd Move KA)**
Source: docs/increment-6/ubiquitous-language-increment-6.md
Locator: Crowd Move section (lines 77–140)

### decisions made

- `Crowd Move` is the root class: distinct trigger (context menu command), state (in-progress, complete), behavior (compute destinations, issue movement commands, apply facing), invariants (all spawned members moved)
- `Relative Positioning` earns its own class: distinct algorithm (uniform delta), distinct GM action, distinct outcome (formation-preserved translation)
- `Optimal Spread Positioning` earns its own class: distinct algorithm (minimize-distance slot assignment), distinct outcome (evenly distributed arrangement)
- `Group Formation` is a class: distinct state (captured offset set), behavior (captured at start, re-applied at end), its own story
- `Facing Destination` is a class: distinct post-move behavior step, computed per-member vector, its own story
- `Gang Leader Facing` is a class: distinct condition (active gang mode), distinct data source (gang leader's facing vector), its own story

---

## **Attack Configuration**

The in-session combat-setup panel defining a full combat exchange before execution: attacker, defenders, parameters, and attack variants.

### **Attack Configuration**
attacker assignment                   | Attacker
                                      |   invariant: the attack configuration must have exactly one attacker and at least one defender before Confirm is enabled
configured defenders                  | Defender, Attacker-Defender Pair
attack parameters                     | Attack Effect, Knockback Distance, Attack Result, Attack Mode
open from desktop                     | Context Menu, Animated Ability
confirm                               | Combat Execution
cancel                                | Combat State
abort                                 | Combat Execution, Combat State

### **Attacker**
attacking role                        | Roster Entry
                                      |   invariant: exactly one attacker must be designated in every attack configuration; a configuration with no attacker cannot be confirmed
pre-assigned on open                  | Attack Configuration, Context Menu
receive attack animation              | Attack Animation
combat state assignment               | Combat State

### **Defender**
defending role                        | Roster Entry
                                      |   invariant: a defender may not be the same roster entry as the attacker in the same attack configuration
added by GM                           | Attack Configuration
receive on-hit animation              | On-Hit Animation
receive knockback movement            | Knockback Movement
combat state assignment               | Combat State

### **Combatant**
combat role                           | Attacker, Defender, Roster Entry
non-attack ability lock               | Non-Attack Ability Lock, Animated Ability

### **Attacker-Defender Pair**
paired attacker                       | Attacker
paired defender                       | Defender
attack effect                         | Attack Effect
knockback distance                    | Knockback Distance
attack result                         | Attack Result

### **Attack Effect**
effect type                           | (Stunned, Unconscious, Dying, or Dead)
determines status effect              | Status Effect
determines on-hit animation           | On-Hit Animation

### **Attack Result**
result type                           | (Hit or Miss)
controls effect application           | Status Effect, Knockback Movement, On-Hit Animation

### **Attack Mode**
mode type                             | (Attack or Defend)
recorded for HCS reporting            | HCS Integration

### **Knockback Distance**
displacement units                    | (non-negative integer)
clipped by obstruction                | Knockback Obstruction

### **Area Center**
designated target NPC                 | Spawned NPC
                                      |   invariant: an area attack requires an area center to be designated before the attack can be confirmed
area radius targets                   | Defender, Area Attack

### **Area Attack**
area variant activation               | Area Center, Area Attack Pop-Up Menu
add in-range defenders                | Defender, Roster Entry
execute per pair                      | Attacker-Defender Pair, Combat Execution

### **Sweep Attack**
sequential delivery order             | Attacker-Defender Pair, Combatant
resolve pairs in sequence             | Combat Execution, Attack Animation, On-Hit Animation
combine with auto-fire                | Auto-Fire

### **Auto-Fire**
total shot count                      | (configured integer)
distribute across defenders           | Defender, Attacker-Defender Pair

### **Ranged Attack**
line-of-sight requirement             | Line-of-Sight, Collision Ray
exclude blocked defenders             | Defender, Game Collision Detection

### references

**Ref — ubiquitous-language-increment-6.md (Attack Configuration KA)**
Source: docs/increment-6/ubiquitous-language-increment-6.md
Locator: Attack Configuration section (lines 143–263)

### decisions made

- `Attack Configuration` is the root class: distinct identity (the open panel), state (parameters, combatant list, open/confirmed/cancelled), behavior (open, populate, configure, confirm/cancel/abort), invariants (exactly one attacker, at least one defender)
- `Attacker` and `Defender` are separate classes: distinct roles (one vs. many), distinct execution behaviors, distinct invariants
- `Combatant` earns its own class: shared supertype behavior (combat state assignment, non-attack ability lock)
- `Attacker-Defender Pair` is a class: distinct identity (per-defender config record), independent parameters, executed as a unit
- `Attack Effect`, `Attack Result`, `Attack Mode` are type-property classes: each varies by constrained label list; modeled as stubs with their determination behavior
- `Knockback Distance` is a property class: distinct collision-ray modification behavior
- `Area Center` is a class: distinct designation behavior (popup menu selection), distinct invariant (required for area attack)
- `Area Attack`, `Sweep Attack`, `Auto-Fire`, `Ranged Attack` are classes: each has distinct behavioral divergence from base single-target attack

---

## **Combat Execution**

The runtime resolution of a confirmed attack configuration: animations, knockback, status effects, and indicator updates.

### **Combat Execution**
pair resolution sequence              | Attacker-Defender Pair
                                      |   invariant: all attacker-defender pairs must be resolved (or execution must be aborted) before combat state is reset; no partial reset occurs mid-execution
play attack animation                 | Attack Animation, Attacker
play on-hit animation                 | On-Hit Animation, Defender
apply knockback                       | Knockback Movement, Defender
apply status effect                   | Status Effect, Defender
update indicators                     | Attack State Indicator
abort execution                       | Combat State

### **Attack Animation**
selected ability                      | Animated Ability, Attacker
play on attacker                      | Combat Execution

### **On-Hit Animation**
selected ability                      | Animated Ability, Defender, Attack Effect
play on defender                      | Combat Execution

### **Knockback Movement**
knockback destination                 | Knockback Distance, Knockback Obstruction
apply displacement                    | Movement Execution, Defender

### **Status Effect**
applied condition                     | (Stunned, Unconscious, Dying, or Dead)
persist on combat state               | Combat State
display via indicator                 | Attack State Indicator

### **Combat State**
current role                          | (attacker, defender, or neutral)
                                      |   invariant: a character's combat state is always consistent with its role in the active attack configuration; a character cannot hold both attacker and defender roles simultaneously
active status effects                 | Status Effect
configuration linkage                 | Attack Configuration
reset to neutral                      | Attack State Indicator, Non-Attack Ability Lock

### **Attack State Indicator**
displayed effect label                | Status Effect, Combat State
role indicator                        | Attacker, Defender
update on execution                   | Combat Execution, Character Overlay
clear on reset                        | Combat State

### **Non-Attack Ability Lock**
suppression state                     | (locked or released)
apply to combatant                    | Combatant, Animated Ability
release on reset                      | Combat State

### references

**Ref — ubiquitous-language-increment-6.md (Combat Execution KA)**
Source: docs/increment-6/ubiquitous-language-increment-6.md
Locator: Combat Execution section (lines 267–344)

### decisions made

- `Combat Execution` is the root class: distinct phase (post-confirm runtime), distinct sequence (animation → knockback → status → indicator), distinct abort path
- `Attack Animation` and `On-Hit Animation` are classes: each plays a different animated ability on a different combatant, triggered at different execution points
- `Knockback Movement` is a class: distinct behavior (movement execution with collision-clipped destination), distinct condition (Hit only, distance > 0)
- `Status Effect` is a class: distinct state (persisted after execution), distinct display behavior, distinct persistence (survives until reset)
- `Combat State` is a class: distinct identity (per-character record), distinct transitions (neutral → role → effects → neutral), invariants
- `Attack State Indicator` is a class: distinct visual behavior (updated live during execution, cleared on reset)
- `Non-Attack Ability Lock` is a property class of Combatant: defined trigger/release cycle with its own story

---

## **Combat Geometry**

Spatial reasoning services that underpin ranged attacks and knockback resolution through collision-ray queries.

### **Combat Geometry**
line-of-sight evaluation              | Line-of-Sight, Collision Ray, Ranged Attack
knockback obstruction detection       | Knockback Obstruction, Collision Ray, Knockback Movement
issue collision queries               | Game Collision Detection

### **Collision Ray**
origin point                          | (world-space coordinate)
direction vector                      | (normalized direction)
maximum distance                      | (world-space units)
return obstruction result             | Game Collision Detection

### **Line-of-Sight**
path state                            | (clear or blocked)
                                      |   invariant: a ranged attack cannot proceed against a defender for whom line-of-sight is blocked; the GM is notified and the defender is excluded
evaluate from attacker to defender    | Collision Ray, Attacker, Defender

### **Knockback Obstruction**
obstruction point                     | Collision Ray
clip knockback destination            | Knockback Movement, Knockback Distance

### **Game Collision Detection**
DLL capability                        | Game Bridge
                                      |   invariant: game collision detection requires the COH game client to be running and the game bridge to be initialized; a query to an unavailable client returns a clear-path result as a safe default
accept ray parameters                 | Collision Ray
return obstruction data               | Collision Ray

### references

**Ref — ubiquitous-language-increment-6.md (Combat Geometry KA)**
Source: docs/increment-6/ubiquitous-language-increment-6.md
Locator: Combat Geometry section (lines 348–398)

### decisions made

- `Combat Geometry` is the root class: introduces ray-based spatial reasoning not present in prior increments; distinct from movement execution and game state query
- `Collision Ray` is a class: distinct structure (origin, direction, max distance), distinct behavior (issued to DLL, returns obstruction), used in two query contexts
- `Line-of-Sight` is a class: distinct state (clear/blocked), distinct behavior (evaluated before ranged attack, triggers defender exclusion), invariants
- `Knockback Obstruction` is a class: distinct detection behavior, distinct outcome (clips knockback to obstruction point)
- `Game Collision Detection` is a class: distinct DLL capability (separate from state-query DLL calls in Increment 5), its own story

---

## **HCS Integration**

The subsystem connecting HVT to the external Hero Combat System via file-watcher-based event ingestion.

### **HCS Integration**
integration state                     | (active or inactive)
                                      |   invariant: HCS integration may only be started after the game bridge is initialized; it cannot route events to characters that are not in the roster
start monitoring                      | HCS File Watcher, Game Bridge
stop monitoring                       | HCS File Watcher
route attack result events            | Attack Result Event, Combat Execution
route simple ability events           | Simple Ability Event, Animated Ability
route held character state            | Held Character State, Combat State
route sweep results                   | Sweep Results, Sweep Attack

### **HCS File Watcher**
monitoring state                      | (active or inactive)
watch output directory                | Info File
read on file arrival                  | Info File
dispatch event payload                | HCS Integration

### **Info File**
event payload                         | (turn state and event data per turn phase)
                                      |   invariant: an info file must be fully written before the HCS file watcher reads it; partial reads are not permitted
on-deck combatants data               | On-Deck Combatants
eligible combatants data              | Eligible Combatants
active character data                 | Active Character HCS
chronometer data                      | Chronometer Turn State

### **On-Deck Combatants**
imminent turn characters              | Roster Entry, Character Overlay
highlight upcoming-turn status        | Desktop Overlay

### **Eligible Combatants**
available-to-act characters           | Roster Entry
restrict or highlight actions         | Desktop Overlay

### **Active Character HCS**
HCS active turn designation           | Roster Entry
                                      |   invariant: if the active character (HCS) does not match any roster entry, the event is logged and no roster selection change is made
synchronize with HVT active character | Active Character

### **Chronometer Turn State**
per-combatant phase                   | (active, held, passed, or waiting)
update combat state                   | Combat State, Roster Entry

### **Attack Result Event**
attacker and defenders payload        | Roster Entry
result type                           | (Hit or Miss)
dispatch to combat execution          | Combat Execution

### **Simple Ability Event**
combatant name                        | Roster Entry
ability identifier                    | Animated Ability
dispatch to ability playback          | Animated Ability

### **Held Character State**
held action designation                | Roster Entry
update combat state to held           | Combat State, Attack State Indicator

### **Sweep Results**
defender results payload               | Roster Entry, Attacker-Defender Pair
dispatch to sweep execution           | Sweep Attack, Combat Execution

### references

**Ref — ubiquitous-language-increment-6.md (HCS Integration KA)**
Source: docs/increment-6/ubiquitous-language-increment-6.md
Locator: HCS Integration section (lines 402–482)

### decisions made

- `HCS Integration` is the root class: entirely new file-watcher-based event ingestion subsystem; owns start/stop lifecycle and event dispatch routing
- `HCS File Watcher` is a class: distinct identity (monitoring component), state (active/inactive), behavior (detect file changes, read, dispatch)
- `Info File` is a class: distinct identity (shared file boundary between HCS and HVT), state (content changes per turn), invariants (fully written before read)
- `On-Deck Combatants`, `Eligible Combatants`, `Active Character HCS`, `Chronometer Turn State` are classes: each is a distinct read from the info file with its own story and distinct HVT reaction
- `Attack Result Event`, `Simple Ability Event`, `Held Character State`, `Sweep Results` are classes: each event type triggers a different dispatch path with distinct behavioral divergence
- `Active Character HCS` is distinguished from `Active Character` (Increment 5): the HCS concept is the turn-active designator in the external system; synchronized but independently owned

---

# Boundary Domain

## **Character**

Owned by: Character and Crowd Library (Increment 1)

### **Character**
(no new responsibilities modeled — provides the named data entity whose roster entry is assigned attacker or defender roles; provides the character name used to identify combatants in info files)

### decisions made

- Character is a boundary concept: lifecycle and CRUD owned by Increment 1; this increment depends on Character as the subject of combat role assignment and HCS event name matching

---

## **Crowd**

Owned by: Character and Crowd Library (Increment 1)

### **Crowd**
(no new responsibilities modeled — provides the group of characters whose roster entries are moved together in a crowd move; provides the membership list for spread or relative positioning)

### decisions made

- Crowd is a boundary concept: structure and membership owned by Increment 1; this increment uses Crowd as the target group for crowd-move operations

---

## **Roster Entry**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Roster Entry**
(no new responsibilities modeled — is the session record assigned to attacker or defender roles; provides character identity matched against HCS info file payloads; carries combat state as a new tracked property)

### decisions made

- Roster Entry is a boundary concept: session lifecycle and spawned state owned by Increment 5; this increment adds combat state tracking on top of the existing record

---

## **Desktop Overlay**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Desktop Overlay**
(no new responsibilities modeled — renders attack state indicators on character overlays during and after combat execution; provides character overlay as the visual target for overlay updates)

### decisions made

- Desktop Overlay is a boundary concept: rendering infrastructure and character overlay lifecycle owned by Increment 5; this increment adds attack state indicator display to the existing overlay

---

## **Animated Ability**

Owned by: Animated Abilities (Increment 3)

### **Animated Ability**
(no new responsibilities modeled — is played as the attack animation on the attacker and as the on-hit animation on each hit defender; is locked from non-attack activation while a character holds a combatant role)

### decisions made

- Animated Ability is a boundary concept: authoring, element structure, and playback infrastructure owned by Increment 3; this increment uses animated ability as the execution vehicle for attack and on-hit animations

---

## **Movement Execution**

Owned by: Single Character Movement (Increment 4)

### **Movement Execution**
(no new responsibilities modeled — is invoked for each crowd member during a crowd move; is invoked to apply knockback movement to a defender using the collision-clipped destination)

### decisions made

- Movement Execution is a boundary concept: command issuance and distance enforcement owned by Increment 4; this increment triggers movement execution for crowd-move destinations and knockback displacements

---

## **Gang Mode**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Gang Mode**
(no new responsibilities modeled — determines whether a crowd move applies gang leader facing instead of facing destination; provides the gang leader whose facing vector is read for alignment)

### decisions made

- Gang Mode is a boundary concept: activation lifecycle owned by Increment 5; this increment reads gang mode state to select the post-move facing strategy

---

## **Game Bridge**

Owned by: Character Identities (Increment 2)

### **Game Bridge**
(no new responsibilities modeled — receives collision-ray query parameters from game collision detection; delivers movement commands for knockback and crowd move destinations; must be initialized before HCS integration can start)

### decisions made

- Game Bridge is a boundary concept: core DLL initialization and slash-command routing owned by Increment 2; this increment adds collision-ray query routing and confirms the initialization precondition for HCS integration

---

## **Area Attack Pop-Up Menu**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Area Attack Pop-Up Menu**
(no new responsibilities modeled — is the pop-up menu loaded at session initialization enabling the GM to designate an area center from within the COH game HUD; must be deployed before an area attack can be configured)

### decisions made

- Area Attack Pop-Up Menu is a boundary concept: deployment lifecycle owned by Increment 5; this increment depends on its presence as a prerequisite for area attack center designation

---

## **Active Character**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Active Character**
(no new responsibilities modeled — is the HVT roster selection synchronized to the Active Character HCS designation when HCS integration reads the active character from the info file)

### decisions made

- Active Character is a boundary concept: activation lifecycle and roster selection owned by Increment 5; this increment synchronizes it with the HCS-side active character designation

---

## **Character Overlay**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Character Overlay**
(no new responsibilities modeled — is the visual marker updated by attack state indicators during combat; displays on-deck and eligible highlights from HCS integration)

### decisions made

- Character Overlay is a boundary concept: spatial positioning and selection behavior owned by Increment 5; this increment adds attack state indicator display and HCS-driven highlights

---

## **Gang Leader**

Owned by: Roster and Desktop Interaction (Increment 5)

### **Gang Leader**
(no new responsibilities modeled — provides the facing vector read by gang leader facing during crowd move completion)

### decisions made

- Gang Leader is a boundary concept: designation and leader indicator owned by Increment 5; this increment reads the gang leader's facing direction for crowd-move alignment

---

## **Spawned NPC**

Owned by: Character Identities (Increment 2)

### **Spawned NPC**
(no new responsibilities modeled — is the in-game entity designated as the area center for area attacks; provides world-space position for collision ray queries)

### decisions made

- Spawned NPC is a boundary concept: spawn lifecycle owned by Increment 2; this increment uses spawned NPC as the area center anchor and as the source of world-space positions for geometry queries
