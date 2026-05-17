---
state: ubiquitous-language
increment: 6
scope: Crowd Orchestration and Combat
date: 2026-05-17
---

# Ubiquitous Language — Increment 6: Crowd Orchestration and Combat

> Scope: the vocabulary needed to move crowds as coordinated formations, execute combat (single-target, area, sweep, auto-fire), resolve outcomes through status effects and knockback, perform collision-geometry queries, and integrate with the external Hero Combat System (HCS) via file-watcher and info-file events. Builds on Increment 1 (Character, Crowd, Crowd Repository), Increment 2 (Spawned NPC, Game Bridge), Increment 3 (Animated Ability), Increment 4 (Movement Execution), and Increment 5 (Roster, Desktop Overlay, Gang Mode, Gang Leader, Active Character).

---

**Terms**:
- **Crowd Move**
  - **crowd move** — a coordinated movement operation that displaces all members of a crowd together as a unit
  - **relative positioning** — a crowd-move strategy in which each member is displaced by the same vector offset from its current position
  - **optimal spread positioning** — a crowd-move strategy that computes a target position for each member so the crowd occupies a maximally evenly distributed arrangement around the destination
  - **group formation** — the spatial arrangement of crowd members relative to each other, preserved across a crowd move
  - **facing destination** — the post-move orientation applied to all crowd members so they face the movement destination
  - **gang leader facing** — the orientation assigned to all gang members so that their facing aligns with the gang leader's facing direction
- **Attack Configuration**
  - **attack configuration** — the complete parameter set that defines one combat exchange: attacker, defenders, effect, result, mode, knockback distance, and area/sweep options
  - **attacker** — the roster entry assigned the attacking role in a combat exchange
  - **defender** — a roster entry assigned as a target in a combat exchange; there may be one or more per attack
  - **combatant** — a roster entry acting in either the attacker or defender role during an active attack configuration
  - **attack effect** — the status outcome applied to a defender after a hit; one of Stunned, Unconscious, Dying, or Dead
  - **attack result** — the resolution outcome of a single attacker-defender exchange: Hit or Miss
  - **attack mode** — the strategic stance designation for a combatant: Attack or Defend
  - **knockback distance** — the number of world-space units a defender is displaced away from the attacker after a hit
  - **area center** — the designated target NPC that anchors an area attack; all characters within range of the area center receive the attack
  - **area attack** — an attack variant in which every target within the area radius of the area center receives the attack outcome
  - **sweep attack** — an attack variant in which the attacker delivers sequential hits to multiple defenders in a defined order
  - **auto-fire** — the mechanism that distributes a fixed number of shots across multiple defender targets proportionally
  - **ranged attack** — an attack that requires an unobstructed line-of-sight between attacker and defender
  - **attacker-defender pair** — a single configured relationship between the attacker and one specific defender within a multi-defender attack
- **Combat Execution**
  - **attack animation** — the animated ability played on the attacker character at the moment an attack is executed
  - **on-hit animation** — the animated ability played on a defender character upon receiving a hit
  - **knockback movement** — the physical displacement applied to a defender after a hit, moving them knockback distance units away from the attacker
  - **knockback obstruction** — a physical blocker detected by the collision ray that reduces or stops the knockback movement before the full distance is reached
  - **status effect** — a persisted combat condition applied to a defender after a hit: Stunned, Unconscious, Dying, or Dead
  - **combat state** — the per-character record of current combat role (attacker, defender, or neutral), active status effects, and attack configuration linkage
  - **attack state indicator** — the visual overlay element displayed on a character overlay reflecting the character's current combat state and status effects
  - **non-attack ability lock** — the suppression of non-attack animated abilities on a character while that character is active in a combat configuration
- **Combat Geometry**
  - **collision ray** — a geometric probe issued through the HookCostume DLL to detect physical obstructions in the game world along a directional path
  - **line-of-sight** — the unobstructed straight-line path between attacker and defender required for a ranged attack to proceed
  - **game collision detection** — the HookCostume DLL capability that responds to collision-ray queries with obstruction data from the live COH physics world
- **HCS Integration**
  - **HCS** — the Hero Combat System; the external turn-tracking application that manages initiative order and turn state for a combat session and communicates results to HVT via file events
  - **HCS file watcher** — the HVT component that monitors the designated HCS output directory for new or updated info files and dispatches the contents to the event pipeline
  - **info file** — a file written by HCS to its output directory containing combatant state for one turn phase; read by the HCS file watcher
  - **on-deck combatants** — the roster of characters whose turns are imminent as reported in the current info file
  - **eligible combatants** — the roster of characters currently available to act in the active HCS phase, as reported in the current info file
  - **active character (HCS)** — the specific character whose turn is currently active according to the HCS chronometer, as reported in the info file
  - **chronometer turn state** — the HCS-side turn progression indicator reporting the current turn phase (e.g. active, held, passed) for each combatant
  - **attack result event** — an HCS-generated info-file entry describing the outcome of a resolved attack, including attacker, defender, and result type
  - **simple ability event** — an HCS-generated info-file entry describing the use of a non-attack ability by a combatant
  - **held character state** — the HCS-side flag indicating that a character has chosen to hold its action for a later turn phase
  - **sweep results** — the HCS-generated multi-target sweep outcome data listing each defender and their individual attack results

---

The Crowd Orchestration and Combat increment unifies all prior capabilities into the full GM tabletop experience. In the *crowd move* half of the increment, the GM selects a *crowd* already placed in the *roster* and issues a move command: either *relative positioning* (each member displaced by the same delta vector) or *optimal spread positioning* (members redistributed to maximize spacing at the destination). During the move the *group formation* is maintained so relative positions between members are preserved. After the move, members are optionally turned to *facing destination*, or all gang members are reoriented to *gang leader facing*.

In the *attack configuration* half, the GM activates an attack ability from the *desktop* screen, opening the *attack configuration* panel. The GM designates the *attacker* (one roster entry) and one or more *defenders*. Each *attacker-defender pair* is configured independently: the GM sets *attack effect* (Stunned / Unconscious / Dying / Dead), *knockback distance*, *attack result* (Hit or Miss), and *attack mode* (Attack or Defend). For multi-target variants the GM may designate an *area center* (triggering *area attack* that hits all targets in radius), or configure a *sweep attack* (sequential delivery) with *auto-fire* shot distribution. When the GM confirms, the system plays the *attack animation* on the attacker, plays the *on-hit animation* on each hit defender, applies *knockback movement* (clipped by *knockback obstruction* when a *collision ray* detects a blocker), and applies the *status effect* to each defender. *Attack state indicators* on the *desktop overlay* are updated, and *non-attack abilities* are locked on all active *combatants*. The GM may *cancel* (before confirm, resetting without effect) or *abort* (mid-execution, halting remaining animations and resetting *combat state*).

*Combat geometry* services underpin ranged attacks and knockback: *line-of-sight* is calculated before a *ranged attack* confirms that no obstruction blocks the path, and *knockback obstruction* detection fires a *collision ray* along the knockback vector before applying displacement. Both use *game collision detection* exposed through the HookCostume DLL.

*HCS Integration* runs in parallel with the GM's direct interactions. When *Start HCS File Watcher Integration* is invoked the *HCS file watcher* begins monitoring the output directory. On each new *info file* arrival, the watcher reads *on-deck combatants*, *eligible combatants*, *active character (HCS)*, and *chronometer turn state*, then routes the event type — *attack result event*, *simple ability event*, *held character state*, or *sweep results* — to the appropriate execution path in HVT. When *Stop HCS Integration* is invoked, the watcher halts and no further file events are processed.

---

# Core Domain

## Crowd Move

*Crowd Move* is the increment's crowd-orchestration capability: moving all members of a *crowd* or *gang mode* group together as a spatial unit while preserving their relative arrangement. The GM triggers a crowd move from the *context menu* targeting any member; the system determines the destination and either applies *relative positioning* (each member offset by the same delta) or *optimal spread positioning* (each member assigned a slot in a computed spread). Throughout the move the *group formation* is maintained. On completion, all members are turned to *facing destination*, or — when the crowd is an active *gang mode* group — all members are reoriented to *gang leader facing*.

### crowd_move

- is triggered from the *context menu* on any member of the crowd; the chosen command determines whether *relative positioning* or *optimal spread positioning* is applied
- moves all spawned members of the target *crowd* simultaneously, issuing one *movement execution* command per member derived from the chosen positioning strategy
- maintains *group formation* by computing each member's destination relative to the same formation-center offset used at the start of the move
- turns all members to *facing destination* after positions are confirmed, unless the crowd is operating under *gang mode* which substitutes *gang leader facing*
- **Invariant:** every spawned member of the target *crowd* receives a move command; no member is left at its original position when a crowd move completes successfully

### relative_positioning

- is a crowd-move strategy in which every member receives a displacement vector equal to the offset between the GM's designated origin and destination
- preserves the exact spatial offsets between members — no member changes its position relative to any other
- is applied when the GM selects Move Crowd with Relative Positioning from the *context menu*

### optimal_spread_positioning

- is a crowd-move strategy that computes a set of destination slots evenly distributed around the target point, then assigns one slot to each member minimizing total travel distance
- results in the crowd occupying a spread arrangement at the destination rather than a clustered copy of the original shape
- is applied when the GM selects Move Crowd with Optimal Spread Positioning from the *context menu*
- **Invariant:** every member receives a unique destination slot; no two members are assigned the same spread position

### group_formation

- is the set of relative spatial offsets between crowd members captured at the moment a crowd move begins
- is preserved during the move by deriving each member's destination from their offset to the same formation-center reference point
- is re-applied after any crowd move to confirm that member spacing matches the pre-move relative arrangement

### facing_destination

- is the post-move orientation applied to every crowd member so they face toward the movement destination point
- is computed by deriving the facing vector from each member's final position to the destination center
- is applied after all position commands have been confirmed, before the move is considered complete

### gang_leader_facing

- is the orientation assigned to all gang members so that each member's facing aligns with the current facing direction of the *gang leader*
- is applied instead of *facing destination* when the crowd being moved is an active *gang mode* group
- reads the *gang leader*'s current facing vector from memory at the time of move completion and issues facing commands to all other gang members

### Decisions made

- `crowd move` is a concept: distinct trigger (context menu command), distinct state (in-progress, complete), behavior (compute destinations, issue movement commands, apply facing), invariants (all spawned members moved); its own stories (Move Crowd with Relative Positioning, Move Crowd with Optimal Spread Positioning, Maintain Group Formation, Turn Characters to Face Destination, Align Character Facing with Gang Leader)
- `relative positioning` earns its own concept block: distinct algorithm (uniform delta), distinct GM action, distinct outcome (formation-preserved translation); not merely a parameter on crowd move
- `optimal spread positioning` earns its own concept block: distinct algorithm (minimize-distance slot assignment), distinct outcome (evenly distributed arrangement); produces structurally different result from relative positioning
- `group formation` is a concept: distinct state (the captured offset set), distinct behavior (captured at start, re-applied at end), its own story (Maintain Group Formation during Crowd Move)
- `facing destination` is a concept: distinct post-move behavior step, computed per-member vector, its own story (Turn Characters to Face Destination)
- `gang leader facing` is a concept: distinct condition (active gang mode), distinct data source (gang leader's facing vector), its own story (Align Character Facing with Gang Leader)

### References

**Ref — thin-slicing.md (Increment 6: crowd move stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 248–252
Extract: Move Crowd with Relative Positioning, Move Crowd with Optimal Spread Positioning, Maintain Group Formation during Crowd Move, Turn Characters to Face Destination, Align Character Facing with Gang Leader

**Ref — initial-ia.md (desktop context menu — movement)**
Source: docs/ux/initial-ia.md
Locator: lines 345–346
Extract: move-crowd-relative · move-crowd-spread · turn-to-target · align-with-gang-leader

---

## Attack Configuration

*Attack Configuration* is the in-session combat-setup panel that the GM uses to define and confirm a full combat exchange before execution. The GM reaches it from the *desktop* screen by activating an attack ability on a character; the panel opens with the *attacker* pre-assigned. The GM then adds one or more *defenders*, forming *attacker-defender pairs*. Each pair carries its own *attack effect*, *knockback distance*, *attack result*, and *attack mode*. Optionally the GM designates an *area center* (converting the attack to an *area attack*) or configures a *sweep attack* with *auto-fire* shot distribution. The panel provides three exits: Confirm (execute the configured attack), Cancel (close without effect), and Abort (halt execution in progress).

### attack_configuration

- opens from the *desktop* screen when the GM activates an attack ability on a spawned character; the activating character is pre-assigned as the *attacker*
- displays the *combatant selectors* region listing the *attacker* and all added *defenders*, each with their role label
- displays the *attack parameters* region with *attack effect*, *knockback distance*, *attack result*, *attack mode*, and *area center* fields
- persists all parameter values across defender additions until the GM confirms or cancels
- is dismissed via Confirm (proceeds to execution), Cancel (discards, returns to *desktop*), or Abort (halts mid-execution, resets *combat state*)
- **Invariant:** the *attack configuration* must have exactly one *attacker* and at least one *defender* before Confirm is enabled

### attacker

- is the *roster entry* assigned the attacking role; exactly one attacker is active per *attack configuration*
- is pre-assigned when the GM activates an attack ability from the *desktop* context menu on a specific character
- may be changed by the GM via the Select Attacker action in the *combatant selectors* region before confirming
- receives the *attack animation* during execution
- has its *combat state* set to attacker role and all *non-attack abilities* locked while the configuration is active
- **Invariant:** exactly one *attacker* must be designated in every *attack configuration*; a configuration with no attacker cannot be confirmed

### defender

- is a *roster entry* assigned as a target of the attack; one or more defenders may be added to a single *attack configuration*
- is added by the GM via the Add Defender action in the *combatant selectors* region; removed via Remove Defender
- receives an independently configured *attacker-defender pair* record specifying its *attack effect*, *knockback distance*, and *attack result*
- receives the *on-hit animation* and *knockback movement* during execution when the paired *attack result* is Hit
- has its *combat state* set to defender role while the configuration is active
- **Invariant:** a *defender* may not be the same *roster entry* as the *attacker* in the same *attack configuration*

### combatant

- is a *roster entry* in either the *attacker* or *defender* role within an active *attack configuration*
- has its *combat state* updated to reflect the assigned role
- has *non-attack abilities* locked while designated as a *combatant* in an active configuration

### attacker_defender_pair

- is the configuration record linking one *attacker* to one specific *defender*, carrying *attack effect*, *knockback distance*, and *attack result* for that pairing
- is created for each *defender* added to the *attack configuration*
- is configured independently; parameters on one pair do not affect other pairs in the same configuration
- is the unit of execution: during confirm, each pair is resolved in sequence

### attack_effect

- is a type property of *attacker-defender pair*: one of Stunned, Unconscious, Dying, or Dead
- is selected by the GM via the Attack Effect dropdown in the *attack parameters* region
- determines which *status effect* is applied to the *defender* after a Hit result is confirmed

### attack_result

- is a type property of *attacker-defender pair*: Hit or Miss
- is set by the GM via the Attack Result dropdown in the *attack parameters* region
- controls whether *knockback movement*, *on-hit animation*, and *status effect* are applied to the *defender*: all three apply only on Hit

### attack_mode

- is a type property of *attack configuration*: Attack or Defend
- is set by the GM via the Attack Mode dropdown in the *attack parameters* region
- records the GM's declared combat stance for the *attacker* for HCS chronometer tracking purposes

### knockback_distance

- is a numeric property of *attacker-defender pair* specifying the number of world-space units the *defender* is displaced away from the *attacker* after a Hit
- is entered by the GM in the Knockback Distance field in the *attack parameters* region
- is reduced or set to zero by *knockback obstruction* detection before the *knockback movement* is applied

### area_center

- is the designated *spawned NPC* that anchors an *area attack*; all targets within the area radius of the *area center* are added as *defenders* automatically
- is designated by the GM via the Area Center checkbox / selector in the *attack parameters* region, followed by selecting the target in the COH game view via the *area attack pop-up menu*
- **Invariant:** an *area attack* requires an *area center* to be designated before the attack can be confirmed; a confirmed area attack without an area center is not permitted

### area_attack

- is an attack variant triggered when an *area center* is designated; all characters within the area radius receive the configured *attack effect* and *attack result*
- adds all in-range *defenders* automatically based on proximity to the *area center* when the GM confirms the target designation
- executes each *attacker-defender pair* for the area set using the same configured parameters

### sweep_attack

- is an attack variant in which the *attacker* delivers sequential hits to each *defender* in the order listed in the *combatant selectors* region
- executes *attacker-defender pairs* one at a time in sequence, playing *attack animation* and *on-hit animation* for each pair before proceeding to the next
- may be combined with *auto-fire* shot distribution to apportion multiple shots across the defender list

### auto_fire

- is the mechanism that distributes a configured number of total shots proportionally across the *defender* list in a *sweep attack*
- assigns each *defender* a shot count equal to the total shots divided across the list (fractional allocations rounded per GM-configured rule)
- is configured via the Auto-Fire Shots per Target field in the *attack parameters* region

### ranged_attack

- is an attack that requires *line-of-sight* between the *attacker* and each *defender* to proceed
- checks *line-of-sight* before the attack is confirmed; a blocked path prevents the attack on that *defender*
- uses *game collision detection* to evaluate the line-of-sight path

### Decisions made

- `attack configuration` is a concept: distinct identity (the open panel), state (parameters, combatant list, open/confirmed/cancelled), behavior (open from desktop, populate, configure, confirm/cancel/abort), invariants (exactly one attacker, at least one defender before confirm); the central KA for combat setup
- `attacker` and `defender` are separate concepts: distinct roles (one vs. many), distinct execution behaviors (plays attack animation vs. receives on-hit), distinct invariants
- `combatant` earns its own concept block: it is the shared supertype behavior applicable to both roles (combat state assignment, non-attack ability lock)
- `attacker-defender pair` is a concept: distinct identity (per-defender config record), distinct parameters (effect/result/knockback independently per pair), executed as a unit; not merely a row in a list
- `attack effect`, `attack result`, `attack mode` are type properties: each varies by constrained label list (Stunned/Unconscious/Dying/Dead; Hit/Miss; Attack/Defend) with no behavioral divergence between options at UL level — effects are applied uniformly by effect type
- `knockback distance` is a property of *attacker-defender pair*; distinct enough for its own stub due to the collision-ray modification behavior
- `area center` is a concept: distinct designation behavior (popup menu selection), distinct invariant (required for area attack); not simply a boolean flag
- `area attack`, `sweep attack`, `auto-fire`, `ranged attack` are concepts: each has distinct behavior diverging from the base single-target attack; evaluated as subtypes-in-behavior-only — they are variants of the same attack configuration flow, not separate KAs
- `non-attack ability lock` is a property of *combatant* — a behavioral rule rather than an independent concept; included as a named stub because it has its own story (Disable Non-Attack Abilities during Combat)

### References

**Ref — thin-slicing.md (Increment 6: attack configuration stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 253–276
Extract: Select Attacking Character, Activate Attack Ability, Select Defender Targets, Confirm Attack Targets, Configure Attack for Attacker-Defender Pair, Set Attack Effect, Set Knockback Distance, Set Attack Result, Set Attack Mode, Designate Center Target for Area Attack, Execute Ranged Area Attack, Execute Sweep Attack across Multiple Targets, Assign Auto-Fire Shots per Target, Spread Attack across Crowd, Cancel Active Attack, Abort Attack in Progress, Reset Character Combat State, Disable Non-Attack Abilities during Combat, Track Attacker and Defender Roles per Character

**Ref — initial-ia.md (attack configuration screen)**
Source: docs/ux/initial-ia.md
Locator: lines 356–407
Extract: layout flyout; combatant selectors region (name · role · select attacker · add defender · remove defender · confirm targets); attack parameters region (attack effect · knockback distance · attack result · attack mode · area center · confirm · cancel · abort)

---

## Combat Execution

*Combat Execution* is the runtime application of a confirmed *attack configuration*: playing the *attack animation* on the *attacker*, delivering *on-hit animations* and *knockback movement* to each hit *defender*, applying *status effects*, and updating *attack state indicators* in the *desktop overlay*. Execution is driven by the *attacker-defender pairs* in sequence. The *combat state* of each involved character is updated throughout and reset when the exchange ends. *Non-attack abilities* remain locked on all *combatants* until *combat state* is reset.

### combat_execution

- begins when the GM confirms the *attack configuration*; each *attacker-defender pair* is resolved in sequence
- plays the *attack animation* on the *attacker* at the start of each pair's resolution; waits for animation completion before proceeding to the *defender* step
- plays the *on-hit animation* on the *defender* when the pair's *attack result* is Hit
- applies *knockback movement* to the *defender* when the pair's *attack result* is Hit and *knockback distance* is greater than zero
- applies the *status effect* corresponding to the pair's *attack effect* to the *defender* when *attack result* is Hit
- updates the *attack state indicator* on the *defender*'s *character overlay* to reflect the applied *status effect*
- skips *on-hit animation*, *knockback*, and *status effect* application when the pair's *attack result* is Miss
- **Invariant:** all *attacker-defender pairs* must be resolved (or the execution must be aborted) before *combat state* is reset; no partial reset occurs mid-execution

### attack_animation

- is the *animated ability* played on the *attacker* character at the moment of executing an *attacker-defender pair*
- is selected from the *attacker*'s configured attack abilities
- completes before execution advances to the *on-hit animation* step for the *defender*

### on_hit_animation

- is the *animated ability* played on a *defender* character upon receiving a hit in an *attacker-defender pair*
- is played only when the pair's *attack result* is Hit
- is selected based on the *attack effect* type — different *on-hit animations* correspond to Stunned, Unconscious, Dying, and Dead effects

### knockback_movement

- is the physical displacement applied to a *defender* after a Hit, moving them *knockback distance* units away from the *attacker*
- is applied via a *movement execution* command targeting the *defender* with the computed knockback destination
- is clipped to the obstruction point when a *collision ray* detects *knockback obstruction* along the knockback vector before the full distance is reached
- is not applied when *attack result* is Miss or *knockback distance* is zero

### status_effect

- is a persisted combat condition applied to a *defender* when their *attack result* is Hit; value corresponds to the *attack effect* of the pair: Stunned, Unconscious, Dying, or Dead
- is reflected as a named state on the *defender*'s *combat state* record and displayed via the *attack state indicator* on the *character overlay*
- persists until the *defender*'s *combat state* is reset via the Reset Character Combat State action

### combat_state

- is the per-character record of current combat role (attacker, defender, or neutral), active *status effects*, and linkage to the active *attack configuration*
- is set to attacker or defender role when a character is added to an *attack configuration*
- is updated to reflect applied *status effects* after execution
- is reset to neutral and cleared of *status effects* when the GM triggers Reset Character Combat State, or when the session ends
- **Invariant:** a character's *combat state* is always consistent with its role in the active *attack configuration*; a character cannot hold both attacker and defender roles simultaneously

### attack_state_indicator

- is the visual element displayed in the *character overlay* on the *desktop overlay* that reflects the character's current *combat state*
- shows the assigned *status effect* label (Stunned, Unconscious, Dying, Dead) when a *status effect* is active
- shows an active attacker or defender indicator when the character is currently involved in an *attack configuration*
- is updated immediately after each *attacker-defender pair* resolution during *combat execution*
- is cleared when the character's *combat state* is reset to neutral

### non_attack_ability_lock

- is a property of *combatant* — the suppression applied to all non-attack animated abilities on a character while that character has an active role in an *attack configuration*
- is applied to both the *attacker* and all *defenders* when the *attack configuration* opens
- is released when the *combat state* is reset after the attack completes, is cancelled, or is aborted

### Decisions made

- `combat execution` earns its own KA: distinct phase (post-confirm runtime), distinct sequence (animation → knockback → status effect → indicator update), distinct abort path; not merely the tail of attack configuration
- `attack animation` and `on-hit animation` are concepts: each plays a different *animated ability* on a different *combatant*, triggered at different points in execution; neither is reducible to a property
- `knockback movement` is a concept: distinct behavior (movement execution command with collision-clipped destination), distinct condition (Hit only, distance > 0), its own story
- `status effect` is a concept: distinct state (persisted after execution), distinct display behavior (attack state indicator), distinct persistence (survives until reset); not merely the value of attack effect at the moment of confirm
- `combat state` is a concept: distinct identity (per-character record), distinct transitions (neutral → attacker/defender → applied effects → neutral), invariants; its own story (Reset Character Combat State, Track Attacker and Defender Roles per Character)
- `attack state indicator` is a concept: distinct visual behavior (updated live during execution, cleared on reset), distinct display states per effect; not merely a CSS class
- `non-attack ability lock` is classified as a property of *combatant* with its own stub: it has a story (Disable Non-Attack Abilities during Combat) and a defined trigger/release cycle, but no independent identity beyond the combatant it constrains

### References

**Ref — thin-slicing.md (Increment 6: combat execution stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 267–278
Extract: Play Attack Animation on Attacker, Play On-Hit Animation on Defender, Apply Knockback Movement to Defender, Apply Status Effect to Defender, Update Character Attack State Indicators, Cancel Active Attack, Abort Attack in Progress, Reset Character Combat State, Disable Non-Attack Abilities during Combat, Track Attacker and Defender Roles per Character

---

## Combat Geometry

*Combat Geometry* provides the spatial reasoning services that underpin ranged attacks and knockback resolution. *Line-of-sight* calculation determines whether the *attacker* has an unobstructed path to each *defender* before a *ranged attack* is confirmed. *Knockback obstruction* detection fires a *collision ray* along the knockback vector to find any physical blocker that would stop the *defender* short of the full *knockback distance*. Both services are built on *game collision detection* — a capability exposed through the HookCostume DLL that responds to *collision-ray* queries with obstruction data from the live COH physics world.

### combat_geometry

- provides two query operations used by *combat execution*: *line-of-sight* evaluation before *ranged attack* confirmation, and *knockback obstruction* detection before *knockback movement* application
- issues *collision-ray* queries through *game collision detection* and returns obstruction results to the calling service
- is stateless between queries; each invocation is independent

### collision_ray

- is a geometric probe defined by an origin point, a direction vector, and a maximum distance; issued through *game collision detection* to detect physical obstructions in the live COH game world
- is issued for *line-of-sight* checks from the *attacker*'s world position toward each *defender*'s position
- is issued for *knockback obstruction* checks from the *defender*'s current position along the knockback direction vector
- returns the first obstruction point along the ray, or no obstruction if the path is clear within the maximum distance

### line_of_sight

- is the result of a *collision-ray* query from the *attacker* to the *defender* that indicates whether an unobstructed path exists between them
- is evaluated before a *ranged attack* is confirmed; a blocked path prevents the attack against that specific *defender*
- is clear when the *collision ray* reaches the *defender* without hitting any geometry; is blocked when the ray strikes an obstruction before reaching the *defender*
- **Invariant:** a *ranged attack* cannot proceed against a *defender* for whom *line-of-sight* is blocked; the GM is notified and the *defender* is excluded from the attack

### knockback_obstruction

- is a physical blocker detected by a *collision ray* fired along the knockback vector from the *defender*'s current position
- is detected before *knockback movement* is applied; when detected, the knockback destination is set to the obstruction point rather than the full *knockback distance*
- results in the *defender* being displaced only to the edge of the obstructing geometry rather than the configured distance

### game_collision_detection

- is the HookCostume DLL capability that accepts *collision-ray* query parameters and returns obstruction data from the live COH physics world
- is invoked by *combat geometry* for both *line-of-sight* and *knockback obstruction* queries
- returns the distance and position of the first obstruction along the ray, or a clear-path result if no obstruction is found within the maximum distance
- **Invariant:** *game collision detection* requires the COH game client to be running and the *game bridge* to be initialized; a query to an unavailable client returns a clear-path result as a safe default

### Decisions made

- `combat geometry` earns its own KA: introduces a new class of spatial-reasoning services (ray-based obstruction queries) not present in prior increments; distinct from *movement execution* (which applies movements) and *game state query* (which reads NPC/mouse state)
- `collision ray` is a concept: distinct structure (origin, direction, max distance), distinct behavior (issued to DLL, returns obstruction point), used in two different query contexts (LOS and knockback); not merely a parameter
- `line-of-sight` is a concept: distinct state (clear / blocked), distinct behavior (evaluated before ranged attack confirmation, triggers defender exclusion), its own story (Calculate Line-of-Sight for Ranged Attack)
- `knockback obstruction` is a concept: distinct detection behavior (fires ray along knockback vector before movement), distinct outcome (clips knockback distance to obstruction point), its own story (Detect Knockback Obstruction via Collision Ray)
- `game collision detection` is a concept: distinct DLL capability (separate from the state-query DLL calls in Increment 5), distinct query/response semantics, its own story (Query Game Collision Detection via HookCostume DLL); boundary of the HookCostume DLL from Increment 2

### References

**Ref — thin-slicing.md (Increment 6: collision geometry stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 277–280
Extract: Detect Knockback Obstruction via Collision Ray, Calculate Line-of-Sight for Ranged Attack, Query Game Collision Detection via HookCostume DLL

---

## HCS Integration

*HCS Integration* is the subsystem that connects HVT to the external Hero Combat System (HCS) application. HCS manages initiative order, turn phase tracking, and ability event recording for a combat session and communicates its results by writing *info files* to a shared output directory. The *HCS file watcher* monitors that directory; on each new *info file* it reads *on-deck combatants*, *eligible combatants*, *active character (HCS)*, and *chronometer turn state*, then dispatches the event to the appropriate HVT execution path. Four event types are recognized: *attack result events* (resolved attacks), *simple ability events* (non-attack ability uses), *held character state* (action-hold declarations), and *sweep results* (multi-target sweep outcomes). The GM starts the integration explicitly; it runs until stopped or until the session ends.

### HCS_integration

- is started by the GM via the Start HCS File Watcher Integration action; the *HCS file watcher* begins monitoring on start
- is stopped by the GM via the Stop HCS Integration action; the *HCS file watcher* halts and no further *info file* events are processed
- routes each recognized event type to its designated HVT execution path: *attack result events* trigger *combat execution*, *simple ability events* trigger *animated ability* playback, *held character state* updates *combat state*, and *sweep results* trigger *sweep attack* execution
- **Invariant:** *HCS integration* may only be started after the *game bridge* is initialized; it cannot route events to characters that are not in the *roster*

### HCS_file_watcher

- is the HVT component that monitors the designated HCS output directory for new or modified *info files*
- is activated when *HCS integration* starts and deactivated when it stops
- reads each new *info file* as it arrives and extracts the event payload for dispatch to the event pipeline
- detects file creation and modification events; ignores files that have not changed since the last read

### info_file

- is a file written by HCS to its output directory when the turn phase changes or when an ability or attack event is recorded
- carries the current *on-deck combatants*, *eligible combatants*, *active character (HCS)*, *chronometer turn state*, and any event payload for the current turn
- is read in full by the *HCS file watcher* on each arrival; old content is replaced with each new write by HCS
- **Invariant:** an *info file* must be fully written before the *HCS file watcher* reads it; partial reads are not permitted

### on_deck_combatants

- is the list of *roster entries* identified in the *info file* as having their turn imminent in the HCS initiative order
- is read from the *info file* on each arrival and used to pre-highlight upcoming *character overlays* in the *desktop overlay*

### eligible_combatants

- is the list of *roster entries* identified in the *info file* as currently available to act in the active HCS phase
- is read from the *info file* and used to restrict or highlight available actions for those characters in the HVT UI

### active_character_HCS

- is the specific *roster entry* identified in the *info file* as holding the active turn in the HCS chronometer
- is read from the *info file* and used to synchronize the HVT *active character* selection with the HCS turn state
- **Invariant:** if the *active character (HCS)* does not match any *roster entry*, the event is logged and no roster selection change is made

### chronometer_turn_state

- is the HCS-side turn progression indicator carried in each *info file*, reporting the current phase for each combatant (active, held, passed, or waiting)
- is read to update *combat state* records for affected characters and to determine the applicable event dispatch path

### attack_result_event

- is an HCS-generated *info file* entry describing the outcome of a resolved attack: attacker, defender(s), and result type (Hit or Miss)
- is dispatched by the *HCS file watcher* to *combat execution* which applies the configured effects to the named *defenders*

### simple_ability_event

- is an HCS-generated *info file* entry describing the use of a non-attack ability by a named combatant
- is dispatched by the *HCS file watcher* to the *animated ability* playback path for the named character

### held_character_state

- is the HCS-side flag in the *info file* indicating that a named character has chosen to hold its action for a later turn phase
- is dispatched by the *HCS file watcher* to update the named character's *combat state* to held, reflecting the deferred-turn posture in the HVT overlay

### sweep_results

- is the HCS-generated *info file* payload listing each *defender* in a sweep and their individual *attack result* (Hit or Miss)
- is dispatched by the *HCS file watcher* to the *sweep attack* execution path, which resolves each *attacker-defender pair* in the listed order

### Decisions made

- `HCS integration` earns its own KA: it is an entirely new subsystem — file-watcher-based event ingestion from an external application — with no counterpart in prior increments; it owns the start/stop lifecycle and the event dispatch routing
- `HCS file watcher` is a concept: distinct identity (the monitoring component), state (active/inactive), behavior (detect file changes, read, dispatch), invariants (only active during HCS integration); its own story (Start HCS File Watcher Integration, Stop HCS Integration)
- `info file` is a concept: distinct identity (the shared file boundary between HCS and HVT), state (content changes per turn), behavior (read on arrival, dispatched), invariants (fully written before read); its own stories (Read On-Deck Combatants, Read Eligible Combatants, Read Active Character, Read Chronometer Turn State from Info File)
- `on-deck combatants`, `eligible combatants`, `active character (HCS)`, `chronometer turn state` are concepts: each is a distinct read from the *info file* with its own story and a distinct HVT reaction; they are not merely fields on a struct
- `attack result event`, `simple ability event`, `held character state`, `sweep results` are concepts: each event type triggers a different dispatch path in HVT (combat execution vs. ability playback vs. combat state update vs. sweep execution); they have distinct behavioral divergence qualifying them as concepts over type-properties
- `active character (HCS)` is distinguished from *active character* (Increment 5): the HCS concept is the turn-active designator in the external system; the Increment 5 concept is the GM-designated current-turn character in the HVT roster; they are synchronized but independently owned

### References

**Ref — thin-slicing.md (Increment 6: HCS integration stories)**
Source: docs/stories/thin-slicing.md
Locator: lines 281–290
Extract: Start HCS File Watcher Integration, Read On-Deck Combatants from Info File, Read Eligible Combatants from Info File, Read Active Character from Info File, Read Chronometer Turn State from Info File, Process Attack Result Events from HCS, Process Simple Ability Events from HCS, Resolve Held Character State from HCS, Execute Sweep Results from HCS, Stop HCS Integration

---

# Boundary Domain

## Character

Owned by: Character and Crowd Library (Increment 1)

- is the named data entity from the *crowd library* whose *roster entry* is assigned attacker or defender roles in an *attack configuration*
- provides the character name used to identify *combatants* in *info files* dispatched by the *HCS file watcher*

### Decisions made

- *character* is a boundary concept: lifecycle and CRUD owned by Increment 1; this increment depends on *character* as the subject of combat role assignment and HCS event name matching

### References

**Ref — ubiquitous-language-increment-1.md (Character KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Character KA section

---

## Crowd

Owned by: Character and Crowd Library (Increment 1)

- provides the group of *characters* whose *roster entries* are moved together in a *crowd move*
- provides the membership list used to determine which *roster entries* receive spread or relative positioning commands

### Decisions made

- *crowd* is a boundary concept: structure and membership owned by Increment 1; this increment uses *crowd* as the target group for crowd-move operations

### References

**Ref — ubiquitous-language-increment-1.md (Crowd KA)**
Source: docs/domain/ubiquitous-language-increment-1.md
Locator: Crowd KA section

---

## Roster Entry

Owned by: Roster and Desktop Interaction (Increment 5)

- is the session record assigned to attacker or defender roles in an *attack configuration*
- provides the character identity matched against HCS *info file* event payloads
- carries *combat state* as a new tracked property in this increment

### Decisions made

- *roster entry* is a boundary concept: session lifecycle and spawned state are owned by Increment 5; this increment adds *combat state* tracking on top of the existing record

### References

**Ref — ubiquitous-language-increment-5.md (Roster KA — roster entry)**
Source: docs/domain/ubiquitous-language-increment-5.md
Locator: Roster KA — roster_entry section

---

## Desktop Overlay

Owned by: Roster and Desktop Interaction (Increment 5)

- renders *attack state indicators* on *character overlays* during and after *combat execution*
- provides the *character overlay* as the visual target for overlay updates triggered by *status effect* application

### Decisions made

- *desktop overlay* is a boundary concept: rendering infrastructure and character overlay lifecycle owned by Increment 5; this increment adds *attack state indicator* display to the existing overlay

### References

**Ref — ubiquitous-language-increment-5.md (Desktop Overlay KA)**
Source: docs/domain/ubiquitous-language-increment-5.md
Locator: Desktop Overlay KA section

---

## Animated Ability

Owned by: Animated Abilities (Increment 3)

- is played as the *attack animation* on the *attacker* and as the *on-hit animation* on each hit *defender* during *combat execution*
- is locked from activation for non-attack uses while a character holds a *combatant* role

### Decisions made

- *animated ability* is a boundary concept: authoring, element structure, and playback infrastructure owned by Increment 3; this increment uses *animated ability* as the execution vehicle for attack and on-hit animations

### References

**Ref — ubiquitous-language-increment-3.md (Animated Ability KA)**
Source: docs/domain/ubiquitous-language-increment-3.md
Locator: Animated Ability KA section

---

## Movement Execution

Owned by: Single Character Movement (Increment 4)

- is invoked for each crowd member to deliver the computed destination during a *crowd move*
- is invoked to apply *knockback movement* to a *defender* after a Hit result, using the collision-clipped knockback destination

### Decisions made

- *movement execution* is a boundary concept: command issuance and distance enforcement owned by Increment 4; this increment triggers movement execution for crowd-move destinations and knockback displacements

### References

**Ref — ubiquitous-language-increment-4.md (Movement Execution KA)**
Source: docs/domain/ubiquitous-language-increment-4.md
Locator: Movement Execution KA section

---

## Gang Mode

Owned by: Roster and Desktop Interaction (Increment 5)

- determines whether a *crowd move* applies *gang leader facing* instead of *facing destination* after the move completes
- provides the *gang leader* whose facing vector is read for *gang leader facing* alignment

### Decisions made

- *gang mode* is a boundary concept: activation lifecycle owned by Increment 5; this increment reads gang mode state to select the post-move facing strategy

### References

**Ref — ubiquitous-language-increment-5.md (Roster KA — gang_mode)**
Source: docs/domain/ubiquitous-language-increment-5.md
Locator: Roster KA — gang_mode section

---

## Game Bridge

Owned by: Character Identities (Increment 2)

- receives *collision-ray* query parameters from *game collision detection* and returns obstruction results from the COH physics world
- delivers movement commands for *knockback movement* and *crowd move* destinations to the COH engine
- must be initialized before *HCS integration* can route events to HVT execution paths

### Decisions made

- *game bridge* is a boundary concept: core DLL initialization and slash-command routing owned by Increment 2; this increment adds collision-ray query routing and confirms the initialization precondition for HCS integration

### References

**Ref — ubiquitous-language-increment-2.md (Game Bridge KA)**
Source: docs/domain/ubiquitous-language-increment-2.md
Locator: Game Bridge KA section

---

## Area Attack Pop-Up Menu

Owned by: Roster and Desktop Interaction (Increment 5)

- is the *pop-up menu* loaded at session initialization that enables the GM to designate an *area center* from within the COH game HUD
- must be deployed and loaded before an *area attack* can be configured

### Decisions made

- *area attack pop-up menu* is a boundary concept: deployment lifecycle owned by Increment 5; this increment depends on its presence as a prerequisite for *area attack* center designation

### References

**Ref — ubiquitous-language-increment-5.md (Pop-Up Menu KA — area_attack_pop_up_menu)**
Source: docs/domain/ubiquitous-language-increment-5.md
Locator: Pop-Up Menu KA — area_attack_pop_up_menu section
