# Acceptance Criteria — Increment 6: Crowd Orchestration and Combat

> Domain source: `docs/domain/ubiquitous-language-increment-6.md` (and prior increment ULs).
> All domain terms in this file are drawn from that source.

---

## Crowd Move Stories

---

### Move Crowd with Relative Positioning

**Domain terms** (vocabulary for this story's AC):

- *Crowd Move* — coordinated movement of all crowd members together as a unit
- *Relative Positioning* — crowd-move strategy displacing each member by the same delta vector
- *Group Formation* — spatial arrangement of members relative to each other, preserved during the move
- *Context Menu* — right-click popup menu on a character overlay; entry point for crowd-move commands
- *Movement Execution* — boundary service issuing the per-character move command
- *Roster Entry* — session record for a character; each spawned member receives a move command

1. **WHEN** the GM selects Move Crowd with Relative Positioning from the *Context Menu* on a spawned crowd member and designates a destination  
   **THEN** every spawned *Roster Entry* in the target crowd receives a *Movement Execution* command  
   **AND** each member's destination is its current position plus the same offset vector from the GM's pick point to the destination  
   **AND** all members begin moving simultaneously

2. **WHEN** all *Movement Execution* commands complete  
   **THEN** all crowd members are at their new positions, each displaced by the same delta  
   **AND** the *Group Formation* — the relative offsets between members — is unchanged

3. **WHEN** the GM triggers *Relative Positioning* on a crowd that has at least one unspawned member  
   **THEN** only spawned members receive move commands  
   **BUT** no error is raised for the unspawned members; they are silently excluded

4. **WHEN** the destination is the same as the crowd's current center point  
   **THEN** move commands are issued with zero offset  
   **BUT** no error is raised and no member position changes

5. **WHEN** a *Movement Execution* command for one member fails mid-move  
   **THEN** the system reports which member failed  
   **BUT** commands already issued to other members are not rolled back

---

### Move Crowd with Optimal Spread Positioning

**Domain terms** (vocabulary for this story's AC):

- *Optimal Spread Positioning* — crowd-move strategy distributing members to an evenly spaced arrangement at the destination
- *Crowd Move* — coordinated movement operation
- *Movement Execution* — boundary service issuing per-character move commands
- *Group Formation* — spatial arrangement; optimally re-distributed in this strategy
- *Context Menu* — entry point for crowd-move commands

1. **WHEN** the GM selects Move Crowd with Optimal Spread Positioning from the *Context Menu* on a spawned crowd member and designates a destination  
   **THEN** the system computes a set of spread destination slots around the destination center, one per spawned member  
   **AND** each member is assigned a unique slot that minimizes its individual travel distance  
   **AND** each member receives a *Movement Execution* command to its assigned slot

2. **WHEN** all *Movement Execution* commands complete  
   **THEN** the crowd occupies an evenly distributed spread arrangement at the destination  
   **AND** no two members share the same slot position

3. **WHEN** the crowd has only one spawned member  
   **THEN** that member's destination slot is the designated destination point itself  
   **AND** the move completes normally

4. **WHEN** the destination area is partially obstructed  
   **THEN** spread slots in unobstructed areas are assigned first  
   **AND** members assigned to obstructed slots receive their nearest unobstructed alternative

5. **WHEN** the crowd is a *Gang Mode* group and the GM triggers *Optimal Spread Positioning*  
   **THEN** spread positioning is applied to all spawned gang members as normal  
   **AND** post-move facing alignment uses *Gang Leader Facing* rather than *Facing Destination*

---

### Maintain Group Formation during Crowd Move

**Domain terms** (vocabulary for this story's AC):

- *Group Formation* — relative spatial offsets between crowd members captured at move start and re-applied at completion
- *Crowd Move* — the movement operation that must preserve formation
- *Relative Positioning* — the specific strategy whose defining invariant is formation preservation

1. **WHEN** a *Crowd Move* with *Relative Positioning* completes  
   **THEN** the relative offset from each member to every other member is the same after the move as before  
   **AND** no member has drifted from its expected position relative to its neighbors

2. **WHEN** a *Crowd Move* with *Relative Positioning* is issued for a crowd whose members have different current positions  
   **THEN** the *Group Formation* offsets are captured from the actual positions at move start  
   **AND** each member's destination preserves its pre-move offset from the formation center

3. **WHEN** a member's position cannot be read before the move begins  
   **THEN** the system reports the missing position data  
   **BUT** the move is not issued until all member positions are resolved

4. **WHEN** formation is checked after a crowd move using *Relative Positioning*  
   **THEN** the pairwise distances between all members match the pairwise distances recorded at move start within the positional tolerance of the movement system  
   **BUT** the absolute positions are all offset by the move delta

---

### Turn Characters to Face Destination

**Domain terms** (vocabulary for this story's AC):

- *Facing Destination* — post-move orientation applied to all crowd members, pointing toward the movement destination
- *Crowd Move* — the movement whose completion triggers facing
- *Gang Mode* — collective activation state; when active, *Gang Leader Facing* substitutes

1. **WHEN** a *Crowd Move* completes and the crowd is not an active *Gang Mode* group  
   **THEN** a facing command is issued to every moved member pointing toward the movement destination center  
   **AND** each member's orientation is updated to face the destination

2. **WHEN** a *Crowd Move* completes and the crowd is an active *Gang Mode* group  
   **THEN** *Gang Leader Facing* is applied instead of *Facing Destination*  
   **BUT** no facing-destination command is issued

3. **WHEN** the destination and a member's new position are at the same point  
   **THEN** no facing command is issued for that member  
   **BUT** all other members receive facing commands normally

4. **WHEN** facing commands are issued after a move  
   **THEN** facing updates are applied before the *Crowd Move* operation is considered complete  
   **AND** the *Desktop Overlay* character overlays reflect the updated orientations

5. **WHEN** a facing command for one member fails  
   **THEN** the failure is reported  
   **BUT** facing commands for all other members are still applied

---

### Align Character Facing with Gang Leader

**Domain terms** (vocabulary for this story's AC):

- *Gang Leader Facing* — orientation assigned to all gang members aligning them with the gang leader's facing direction
- *Gang Leader* — the designated roster entry whose facing vector is the reference
- *Gang Mode* — the collective activation state that triggers this alignment

1. **WHEN** the GM triggers Align Character Facing with Gang Leader for an active *Gang Mode* group  
   **THEN** the system reads the current facing vector of the *Gang Leader* from game memory  
   **AND** a facing command aligned to that vector is issued to every other spawned gang member

2. **WHEN** *Gang Leader Facing* is applied during a *Crowd Move* completion  
   **THEN** all gang members are oriented to match the *Gang Leader*'s facing direction  
   **AND** no facing-destination command is issued

3. **WHEN** the *Gang Leader* is not spawned at the time of alignment  
   **THEN** the system reports that the *Gang Leader* facing cannot be read  
   **BUT** no facing commands are issued to any member

4. **WHEN** a gang member is not spawned at the time of alignment  
   **THEN** that member is skipped  
   **AND** all other spawned gang members receive the facing command

5. **WHEN** *Gang Mode* is not active for the crowd  
   **THEN** *Gang Leader Facing* alignment is not available  
   **BUT** *Facing Destination* is applied at crowd move completion

---

## Attack Configuration Stories

---

### Select Attacking Character

**Domain terms** (vocabulary for this story's AC):

- *Attacker* — the roster entry assigned the attacking role
- *Attack Configuration* — the panel opened when an attack is initiated
- *Combatant Selectors* — the region in the attack configuration listing attacker and defenders
- *Combat State* — per-character record of current combat role

1. **WHEN** the GM opens an *Attack Configuration* by activating an attack ability from the *Context Menu* on a spawned character  
   **THEN** the *Attack Configuration* panel opens  
   **AND** the activated character is pre-assigned as the *Attacker* in the *Combatant Selectors* region  
   **AND** the character's *Combat State* is set to attacker role

2. **WHEN** the GM selects a different character as the *Attacker* via the Select Attacker action  
   **THEN** the *Combatant Selectors* region updates to show the newly selected character as the *Attacker*  
   **AND** the previous attacker's *Combat State* is reset to neutral  
   **AND** the new attacker's *Combat State* is set to attacker role

3. **WHEN** the GM attempts to select a character as *Attacker* who is already listed as a *Defender*  
   **THEN** the selection is rejected with feedback  
   **BUT** the existing *Attacker* assignment is unchanged

4. **WHEN** the GM attempts to select an unspawned character as *Attacker*  
   **THEN** the selection is rejected with feedback  
   **BUT** the existing *Attacker* assignment is unchanged

---

### Activate Attack Ability

**Domain terms** (vocabulary for this story's AC):

- *Attack Configuration* — the panel that opens when an attack ability is activated
- *Attacker* — the character whose attack ability is activated
- *Animated Ability* — the ability flagged as an attack ability
- *Desktop* — the in-session screen that transitions to the *Attack Configuration*

1. **WHEN** the GM activates an attack ability on a spawned character from the *Context Menu* on the *Desktop*  
   **THEN** the application transitions to the *Attack Configuration* panel  
   **AND** the activating character is assigned as the *Attacker*  
   **AND** the *Attack Configuration* panel displays with the *Combatant Selectors* region and the *Attack Parameters* region visible

2. **WHEN** the GM activates an attack ability from the *Context Menu* on a character with no attack ability defined  
   **THEN** no *Attack Configuration* opens  
   **AND** feedback is shown indicating no attack ability is available

3. **WHEN** the *Attack Configuration* panel is open  
   **THEN** the *Attacker*'s non-attack abilities are locked  
   **AND** the Confirm button is disabled until at least one *Defender* is added

4. **WHEN** the GM cancels from the *Attack Configuration* panel  
   **THEN** the panel closes and the application returns to the *Desktop*  
   **AND** the *Attacker*'s *Combat State* is reset to neutral  
   **AND** all *Non-Attack Ability Lock* suppressions are released

---

### Select Defender Targets

**Domain terms** (vocabulary for this story's AC):

- *Defender* — a roster entry added as a target
- *Combatant Selectors* — the region listing attacker and all added defenders
- *Attacker-Defender Pair* — configuration record created for each defender added

1. **WHEN** the GM clicks Add Defender in the *Combatant Selectors* region and selects a spawned character  
   **THEN** the selected character is added as a *Defender* in the list  
   **AND** an *Attacker-Defender Pair* record is created for this defender with default parameter values  
   **AND** the defender's *Combat State* is set to defender role

2. **WHEN** the GM adds a second and subsequent *Defenders*  
   **THEN** each new character is appended to the *Combatant Selectors* list  
   **AND** each receives its own *Attacker-Defender Pair* with independent parameters

3. **WHEN** the GM attempts to add a character that is already the *Attacker* as a *Defender*  
   **THEN** the addition is rejected with feedback  
   **BUT** existing defenders are unchanged

4. **WHEN** the GM attempts to add an unspawned character as a *Defender*  
   **THEN** the addition is rejected  
   **BUT** the existing defender list is unchanged

5. **WHEN** the GM removes a *Defender* using Remove Defender  
   **THEN** the defender is removed from the *Combatant Selectors* list  
   **AND** their *Attacker-Defender Pair* record is deleted  
   **AND** their *Combat State* is reset to neutral

---

### Confirm Attack Targets

**Domain terms** (vocabulary for this story's AC):

- *Attack Configuration* — the panel; Confirm Targets locks in the combatant selection before parameters are finalized
- *Combatant Selectors* — the region holding the confirmed list
- *Attacker-Defender Pair* — confirmed for each defender when targets are confirmed

1. **WHEN** the GM clicks Confirm Targets in the *Combatant Selectors* region with one *Attacker* and at least one *Defender* selected  
   **THEN** the combatant list is locked  
   **AND** the *Attack Parameters* region becomes fully editable  
   **AND** each *Attacker-Defender Pair* is confirmed with default values

2. **WHEN** Confirm Targets is triggered with no *Defender* in the list  
   **THEN** the confirmation is rejected  
   **AND** feedback is displayed indicating at least one defender is required  
   **BUT** the *Combatant Selectors* list is unchanged

3. **WHEN** Confirm Targets is triggered with no *Attacker* assigned  
   **THEN** the confirmation is rejected  
   **AND** feedback is displayed  
   **BUT** no lock-in occurs

4. **WHEN** the combatant list is locked after Confirm Targets  
   **THEN** the Add Defender and Remove Defender actions are disabled  
   **AND** the *Combat State* of all combatants reflects their confirmed roles

---

### Configure Attack for Attacker-Defender Pair

**Domain terms** (vocabulary for this story's AC):

- *Attacker-Defender Pair* — the independent configuration record for one attacker-to-defender relationship
- *Attack Effect* — outcome applied on hit (Stunned / Unconscious / Dying / Dead)
- *Knockback Distance* — displacement units after a hit
- *Attack Result* — Hit or Miss for this pair
- *Attack Mode* — Attack or Defend stance

1. **WHEN** the GM edits *Attack Effect*, *Knockback Distance*, *Attack Result*, and *Attack Mode* for a specific *Attacker-Defender Pair* in the *Attack Parameters* region  
   **THEN** each parameter is stored independently on that pair's record  
   **AND** changes to one pair's parameters do not affect any other pair

2. **WHEN** the GM selects a different *Attack Effect* for a pair  
   **THEN** the dropdown updates to show the selected effect  
   **AND** the pair record stores the new value immediately

3. **WHEN** the GM enters a negative value for *Knockback Distance*  
   **THEN** the value is rejected and the field reverts to zero  
   **AND** feedback is shown

4. **WHEN** the *Attack Configuration* has multiple *Defenders*  
   **THEN** each *Attacker-Defender Pair* shows its independently configured parameters  
   **AND** selecting a different row in the *Combatant Selectors* list updates the *Attack Parameters* region to reflect that pair's values

5. **WHEN** the GM leaves all parameters at default and confirms  
   **THEN** default values (Miss, zero knockback, Stunned, Attack mode) are used for execution

---

### Set Attack Effect (Stunned, Unconscious, Dying, Dead)

**Domain terms** (vocabulary for this story's AC):

- *Attack Effect* — the status outcome applied to the defender on a hit: Stunned, Unconscious, Dying, or Dead
- *Status Effect* — the persisted condition that reflects the chosen attack effect after execution
- *On-Hit Animation* — the animation played on the defender, driven by the attack effect type

1. **WHEN** the GM selects Stunned, Unconscious, Dying, or Dead from the Attack Effect dropdown for a pair  
   **THEN** the *Attack Effect* for that *Attacker-Defender Pair* is set to the chosen value  
   **AND** the corresponding *On-Hit Animation* will be selected at execution time

2. **WHEN** the *Attack Result* for the pair is Hit and Confirm is pressed  
   **THEN** the *Status Effect* matching the *Attack Effect* is applied to the *Defender* during execution  
   **AND** the *Attack State Indicator* on the defender's *Character Overlay* is updated

3. **WHEN** the *Attack Result* for the pair is Miss  
   **THEN** no *Status Effect* is applied regardless of the *Attack Effect* setting  
   **BUT** the *Attack Effect* value is preserved in the pair configuration

4. **WHEN** no *Attack Effect* is selected (field left blank)  
   **THEN** Confirm is blocked with feedback indicating *Attack Effect* is required  
   **BUT** all other pair parameters are unchanged

---

### Set Knockback Distance

**Domain terms** (vocabulary for this story's AC):

- *Knockback Distance* — world-space units the defender is displaced after a hit
- *Knockback Movement* — the physical displacement applied during execution
- *Knockback Obstruction* — blocker that clips the displacement before it reaches the full distance

1. **WHEN** the GM enters a positive integer in the Knockback Distance field for a pair  
   **THEN** *Knockback Distance* for that *Attacker-Defender Pair* is updated  
   **AND** a *Knockback Movement* command of that distance will be issued during execution when *Attack Result* is Hit

2. **WHEN** the GM enters zero in the Knockback Distance field  
   **THEN** no *Knockback Movement* is applied during execution for that pair  
   **AND** the field accepts zero as a valid value

3. **WHEN** *Knockback Distance* is greater than zero and *Attack Result* is Hit  
   **THEN** a *Collision Ray* is fired along the knockback vector before the displacement is applied  
   **AND** if *Knockback Obstruction* is detected, the defender is moved only to the obstruction point  
   **AND** the *Attack State Indicator* reflects the applied *Status Effect* regardless of clipped distance

4. **WHEN** the GM enters a non-numeric value in the Knockback Distance field  
   **THEN** the value is rejected and the field is cleared  
   **AND** feedback is shown

---

### Set Attack Result (Hit or Miss)

**Domain terms** (vocabulary for this story's AC):

- *Attack Result* — Hit or Miss for a specific *Attacker-Defender Pair*
- *Status Effect* — applied only on Hit
- *Knockback Movement* — applied only on Hit
- *On-Hit Animation* — played only on Hit

1. **WHEN** the GM selects Hit from the Attack Result dropdown for a pair  
   **THEN** the pair's *Attack Result* is set to Hit  
   **AND** *On-Hit Animation*, *Knockback Movement*, and *Status Effect* application are all enabled for this pair's execution

2. **WHEN** the GM selects Miss from the Attack Result dropdown for a pair  
   **THEN** the pair's *Attack Result* is set to Miss  
   **AND** *On-Hit Animation*, *Knockback Movement*, and *Status Effect* are all skipped for this pair during execution  
   **BUT** *Attack Animation* on the *Attacker* still plays

3. **WHEN** a multi-defender configuration has some pairs set to Hit and others to Miss  
   **THEN** each pair's execution is independent  
   **AND** only Hit pairs apply effects; Miss pairs skip effects without affecting other pairs

4. **WHEN** no *Attack Result* is selected  
   **THEN** Confirm is blocked with feedback

---

### Set Attack Mode (Attack or Defend)

**Domain terms** (vocabulary for this story's AC):

- *Attack Mode* — strategic stance designation for the attack configuration: Attack or Defend
- *HCS* — external system that uses *Attack Mode* for turn-tracking purposes

1. **WHEN** the GM selects Attack from the Attack Mode dropdown  
   **THEN** the *Attack Configuration*'s *Attack Mode* is set to Attack  
   **AND** the mode is recorded in the confirmed configuration for HCS reporting

2. **WHEN** the GM selects Defend from the Attack Mode dropdown  
   **THEN** the *Attack Configuration*'s *Attack Mode* is set to Defend  
   **AND** the mode is recorded accordingly

3. **WHEN** the *Attack Configuration* is confirmed with *Attack Mode* set to Defend  
   **THEN** the execution proceeds identically to Attack mode (animations and effects are applied)  
   **AND** the Defend mode is passed to the HCS event pipeline for turn-state tracking

4. **WHEN** no *Attack Mode* is selected  
   **THEN** a default of Attack is used  
   **AND** no confirmation block occurs

---

### Designate Center Target for Area Attack

**Domain terms** (vocabulary for this story's AC):

- *Area Center* — designated NPC that anchors the area attack
- *Area Attack* — attack variant affecting all targets within the area radius
- *Area Attack Pop-Up Menu* — the in-game menu used to designate the center target
- *Attack Configuration* — the panel where the area center is indicated

1. **WHEN** the GM checks the Area Center option in the *Attack Parameters* region  
   **THEN** the *Attack Configuration* enters area-attack mode  
   **AND** the GM is prompted to designate the center target in the COH game view using the *Area Attack Pop-Up Menu*

2. **WHEN** the GM designates a center target via the *Area Attack Pop-Up Menu*  
   **THEN** the *Area Center* character name is populated in the *Attack Parameters* region  
   **AND** all spawned characters within the area radius of the *Area Center* are automatically added as *Defenders*

3. **WHEN** the *Area Attack Pop-Up Menu* is not deployed  
   **THEN** area center designation is blocked  
   **AND** feedback is displayed indicating the menu must be loaded first  
   **BUT** the *Attack Configuration* remains open

4. **WHEN** no characters are within the area radius of the designated *Area Center*  
   **THEN** no *Defenders* are added automatically  
   **AND** feedback indicates the area is empty  
   **BUT** the *Area Center* designation is preserved

5. **WHEN** the GM unchecks the Area Center option  
   **THEN** all automatically added *Defenders* are removed from the list  
   **AND** the configuration reverts to single-target mode

---

### Execute Ranged Area Attack

**Domain terms** (vocabulary for this story's AC):

- *Area Attack* — the confirmed area-attack execution
- *Area Center* — the NPC anchoring the radius
- *Line-of-Sight* — required from *Attacker* to each area *Defender*
- *Ranged Attack* — requires unobstructed LOS
- *Combat Execution* — the runtime resolution phase

1. **WHEN** the GM confirms an *Area Attack* with an *Area Center* designated and *Defenders* populated  
   **THEN** *Line-of-Sight* is calculated from the *Attacker* to each *Defender*  
   **AND** only *Defenders* with a clear *Line-of-Sight* are included in *Combat Execution*

2. **WHEN** *Line-of-Sight* to a *Defender* is blocked  
   **THEN** that *Defender* is excluded from the attack  
   **AND** the GM is shown which defenders were excluded  
   **BUT** the attack proceeds for all defenders with clear LOS

3. **WHEN** all *Defenders* have blocked *Line-of-Sight*  
   **THEN** no execution occurs  
   **AND** feedback is displayed indicating no valid targets

4. **WHEN** execution proceeds with valid area targets  
   **THEN** *Attack Animation* plays on the *Attacker* once  
   **AND** *On-Hit Animation*, *Knockback Movement*, and *Status Effect* are applied per pair to each included *Defender*

---

### Execute Sweep Attack across Multiple Targets

**Domain terms** (vocabulary for this story's AC):

- *Sweep Attack* — sequential attack delivering hits to multiple defenders in order
- *Attacker-Defender Pair* — each resolved in sequence during sweep execution
- *Attack Animation* — played on attacker before each pair's on-hit step
- *On-Hit Animation* — played per defender after each pair's attack animation

1. **WHEN** the GM confirms a *Sweep Attack* with multiple *Defenders*  
   **THEN** execution begins with the first *Attacker-Defender Pair* in the *Combatant Selectors* list  
   **AND** *Attack Animation* plays on the *Attacker*  
   **AND** after animation completion, the *Defender*'s *On-Hit Animation*, *Knockback Movement*, and *Status Effect* are applied per the pair's configuration  
   **AND** then execution advances to the next pair in sequence

2. **WHEN** all *Attacker-Defender Pairs* have been resolved  
   **THEN** execution is complete  
   **AND** all *Attack State Indicators* are updated  
   **AND** the *Attack Configuration* closes and the *Desktop* screen is shown

3. **WHEN** a pair's *Attack Result* is Miss  
   **THEN** the *Attack Animation* still plays  
   **AND** no *On-Hit Animation*, *Knockback*, or *Status Effect* is applied for that pair  
   **AND** execution advances to the next pair

4. **WHEN** execution is aborted mid-sweep  
   **THEN** no further pairs are resolved  
   **AND** *Combat State* of all combatants is reset  
   **AND** *Attack State Indicators* reflect any effects already applied before the abort

---

### Assign Auto-Fire Shots per Target

**Domain terms** (vocabulary for this story's AC):

- *Auto-Fire* — mechanism distributing shots across defenders in a sweep attack
- *Sweep Attack* — the attack context in which auto-fire operates
- *Attacker-Defender Pair* — each pair receives a shot count from auto-fire distribution

1. **WHEN** the GM enters a total shot count in the Auto-Fire Shots per Target field for a *Sweep Attack*  
   **THEN** shots are distributed across all *Defenders* proportionally  
   **AND** each *Attacker-Defender Pair* is assigned an integer shot count

2. **WHEN** total shots do not divide evenly across *Defenders*  
   **THEN** the remainder shots are allocated starting from the first defender in the list  
   **AND** all shots are distributed with none omitted

3. **WHEN** total shot count is zero or blank  
   **THEN** auto-fire distribution is skipped  
   **AND** each pair defaults to a single exchange

4. **WHEN** auto-fire assigns more than one shot to a pair  
   **THEN** the *Attack Animation* and effect sequence is repeated for each shot on that pair  
   **AND** the effect accumulates or is overwritten per the configured *Attack Effect* for the pair

---

### Spread Attack across Crowd

**Domain terms** (vocabulary for this story's AC):

- *Area Attack* — the variant where all crowd members within range are auto-added as defenders
- *Crowd* — the crowd whose spawned members within range receive the attack
- *Attacker-Defender Pair* — created automatically for each in-range crowd member

1. **WHEN** the GM triggers Spread Attack across Crowd and designates an *Area Center* within the target crowd  
   **THEN** all spawned members of the crowd within the area radius are added as *Defenders* automatically  
   **AND** an *Attacker-Defender Pair* is created for each with the same default parameters

2. **WHEN** multiple crowds have members within range  
   **THEN** all in-range members from all crowds are included as *Defenders*

3. **WHEN** no crowd members are within range of the *Area Center*  
   **THEN** no *Defenders* are added  
   **AND** feedback indicates the area contains no crowd members  
   **BUT** the *Attack Configuration* remains open

---

## Combat Execution Stories

---

### Play Attack Animation on Attacker

**Domain terms** (vocabulary for this story's AC):

- *Attack Animation* — *Animated Ability* played on the *Attacker*
- *Combat Execution* — the runtime phase driving animation playback
- *Attacker* — character receiving the animation command

1. **WHEN** *Combat Execution* begins a pair resolution  
   **THEN** the *Attack Animation* associated with the *Attacker*'s attack ability is played  
   **AND** execution waits for the animation to complete before proceeding to the defender step

2. **WHEN** the *Attack Animation* completes  
   **THEN** execution advances to the *On-Hit Animation* step for the paired *Defender*

3. **WHEN** the *Attacker* has no attack animation configured  
   **THEN** the animation step is skipped  
   **AND** execution advances to the defender step immediately

4. **WHEN** the *Attacker* is not spawned at execution time  
   **THEN** the animation is skipped and execution reports the attacker unavailable  
   **BUT** the remaining pair resolutions are aborted

---

### Play On-Hit Animation on Defender

**Domain terms** (vocabulary for this story's AC):

- *On-Hit Animation* — *Animated Ability* played on the *Defender* on a Hit
- *Attack Effect* — determines which on-hit animation variant to play
- *Attack Result* — must be Hit for the on-hit animation to play

1. **WHEN** the pair's *Attack Result* is Hit  
   **THEN** the *On-Hit Animation* corresponding to the pair's *Attack Effect* is played on the *Defender*  
   **AND** execution waits for the animation to complete before proceeding to *Knockback Movement*

2. **WHEN** the pair's *Attack Result* is Miss  
   **THEN** no *On-Hit Animation* is played for the *Defender*  
   **AND** execution advances directly to the next pair

3. **WHEN** the *Defender* has no on-hit animation configured for the given *Attack Effect*  
   **THEN** the animation step is skipped  
   **AND** *Knockback Movement* and *Status Effect* application still proceed

4. **WHEN** the *Defender* is not spawned at execution time  
   **THEN** the on-hit step is skipped and a warning is recorded  
   **AND** execution advances to the next pair

---

### Apply Knockback Movement to Defender

**Domain terms** (vocabulary for this story's AC):

- *Knockback Movement* — physical displacement of defender away from attacker
- *Knockback Distance* — configured distance; may be clipped by obstruction
- *Knockback Obstruction* — detected by *Collision Ray* before displacement
- *Movement Execution* — boundary service issuing the movement command

1. **WHEN** the pair's *Attack Result* is Hit and *Knockback Distance* is greater than zero  
   **THEN** a *Collision Ray* is fired from the *Defender*'s position along the knockback vector  
   **AND** the knockback destination is set to the full *Knockback Distance* if the ray is clear  
   **AND** a *Movement Execution* command displaces the *Defender* to the computed destination

2. **WHEN** the *Collision Ray* detects *Knockback Obstruction*  
   **THEN** the knockback destination is set to the obstruction point  
   **AND** the *Defender* is moved only to the obstruction edge  
   **AND** the *Attack State Indicator* still reflects the *Status Effect*

3. **WHEN** *Knockback Distance* is zero  
   **THEN** no *Collision Ray* is fired and no movement command is issued  
   **AND** *Status Effect* application proceeds normally

4. **WHEN** the pair's *Attack Result* is Miss  
   **THEN** no knockback command is issued  
   **BUT** the pair's *Knockback Distance* setting is preserved for reference

---

### Apply Status Effect to Defender (Stunned, Unconscious, Dying, Dead)

**Domain terms** (vocabulary for this story's AC):

- *Status Effect* — persisted condition applied after a Hit: Stunned, Unconscious, Dying, or Dead
- *Attack Effect* — determines which *Status Effect* is applied
- *Combat State* — records the applied *Status Effect* on the defender
- *Attack State Indicator* — visual display reflecting the applied effect

1. **WHEN** the pair's *Attack Result* is Hit  
   **THEN** the *Status Effect* corresponding to the pair's *Attack Effect* is applied to the *Defender*'s *Combat State*  
   **AND** the *Attack State Indicator* on the *Defender*'s *Character Overlay* is updated to show the effect name

2. **WHEN** a *Defender* already has an active *Status Effect* from a prior attack in the same session  
   **THEN** the new *Status Effect* replaces the prior one  
   **AND** the *Attack State Indicator* is updated to reflect the new effect

3. **WHEN** the pair's *Attack Result* is Miss  
   **THEN** no *Status Effect* is applied  
   **AND** any existing *Status Effect* on the *Defender* from a prior attack is unchanged

4. **WHEN** a Dead *Status Effect* is applied  
   **THEN** the *Defender*'s *Combat State* is marked Dead  
   **AND** all further combat actions targeting the Dead *Defender* are blocked in the UI  
   **BUT** the *Defender* remains in the *Roster* and on the *Desktop Overlay* unless explicitly removed by the GM

---

### Update Character Attack State Indicators

**Domain terms** (vocabulary for this story's AC):

- *Attack State Indicator* — visual element on the *Character Overlay* showing combat status
- *Status Effect* — the state the indicator reflects
- *Combat State* — source of truth for indicator content
- *Desktop Overlay* — the visual layer containing the character overlays

1. **WHEN** a *Status Effect* is applied to a *Defender* during execution  
   **THEN** the *Attack State Indicator* on the *Defender*'s *Character Overlay* is updated immediately to show the effect label  
   **AND** the indicator is visible on the *Desktop Overlay*

2. **WHEN** a character's *Combat State* shows an active attacker role  
   **THEN** the *Attack State Indicator* shows an attacker designation  
   **AND** the indicator persists until *Combat State* is reset

3. **WHEN** *Combat State* is reset for a character  
   **THEN** the *Attack State Indicator* on that character's *Character Overlay* is cleared  
   **AND** the overlay returns to its standard non-combat display

4. **WHEN** *Combat Execution* completes all pairs  
   **THEN** all *Attack State Indicators* reflect the final applied *Status Effects* before the *Attack Configuration* closes

---

### Cancel Active Attack

**Domain terms** (vocabulary for this story's AC):

- *Cancel* — exit path from *Attack Configuration* before execution begins
- *Combat State* — reset to neutral for all combatants on cancel
- *Non-Attack Ability Lock* — released on cancel

1. **WHEN** the GM clicks Cancel in the *Attack Configuration* panel before clicking Confirm  
   **THEN** the *Attack Configuration* panel closes  
   **AND** the application returns to the *Desktop*  
   **AND** the *Combat State* of the *Attacker* and all *Defenders* is reset to neutral  
   **AND** all *Non-Attack Ability Lock* suppressions are released

2. **WHEN** Cancel is clicked after parameters are partially configured  
   **THEN** all unsaved parameters are discarded  
   **AND** no effects are applied to any character

3. **WHEN** Cancel is triggered via keyboard shortcut  
   **THEN** the same result as clicking Cancel applies

4. **WHEN** the GM closes the *Attack Configuration* panel without using Cancel or Confirm  
   **THEN** the behavior is equivalent to Cancel: *Combat State* resets and the *Desktop* is shown

---

### Abort Attack in Progress

**Domain terms** (vocabulary for this story's AC):

- *Abort* — halt execution mid-flight after Confirm has been clicked
- *Combat State* — reset after abort
- *Sweep Attack* — sequential execution halted at current position on abort

1. **WHEN** the GM clicks Abort during *Combat Execution*  
   **THEN** the current animation (if running) completes  
   **AND** no further *Attacker-Defender Pairs* are resolved  
   **AND** execution stops

2. **WHEN** execution stops after an Abort  
   **THEN** the *Combat State* of all *Combatants* is reset to neutral  
   **AND** *Attack State Indicators* reflect any *Status Effects* applied before the abort point  
   **AND** the *Attack Configuration* panel closes and the *Desktop* is shown

3. **WHEN** Abort is triggered before any pair has been resolved  
   **THEN** no effects have been applied  
   **AND** the reset leaves all characters in their pre-configuration state

4. **WHEN** Abort is triggered for a *Sweep Attack* mid-sequence  
   **THEN** pairs already resolved retain their applied effects  
   **AND** unresolved pairs produce no effects

5. **WHEN** Abort is not available (e.g. before Confirm is clicked)  
   **THEN** the Abort button is disabled  
   **AND** Cancel is the applicable exit

---

### Reset Character Combat State

**Domain terms** (vocabulary for this story's AC):

- *Combat State* — per-character record of combat role and status effects
- *Attack State Indicator* — cleared when combat state resets
- *Non-Attack Ability Lock* — released on reset

1. **WHEN** the GM triggers Reset Character Combat State on a specific character  
   **THEN** that character's *Combat State* is set to neutral  
   **AND** all active *Status Effects* are cleared from the *Combat State* record  
   **AND** the *Attack State Indicator* on the character's *Character Overlay* is cleared  
   **AND** any *Non-Attack Ability Lock* on that character is released

2. **WHEN** Reset is triggered on a character currently in an active *Attack Configuration*  
   **THEN** the reset is blocked  
   **AND** feedback is displayed indicating the character is in an active configuration  
   **BUT** the character's *Combat State* is unchanged

3. **WHEN** Reset is triggered after a completed or aborted attack  
   **THEN** the character's *Combat State* resets normally regardless of prior *Status Effect* state

4. **WHEN** the GM resets a character with a Dead *Status Effect*  
   **THEN** the Dead state is cleared from *Combat State*  
   **AND** the character becomes eligible for combat again  
   **BUT** the GM must re-confirm the character's roster presence before attacking or defending

---

### Disable Non-Attack Abilities during Combat

**Domain terms** (vocabulary for this story's AC):

- *Non-Attack Ability Lock* — suppression of non-attack abilities on active combatants
- *Combatant* — any roster entry in attacker or defender role in an active configuration
- *Animated Ability* — suppressed for non-attack types while lock is active

1. **WHEN** a character is assigned as *Attacker* or *Defender* in an *Attack Configuration*  
   **THEN** all non-attack *Animated Abilities* on that character are locked from activation  
   **AND** any attempt to trigger a non-attack ability is silently blocked while the lock is active

2. **WHEN** the *Attack Configuration* is confirmed and execution begins  
   **THEN** the *Non-Attack Ability Lock* remains active on all *Combatants* throughout execution

3. **WHEN** the *Attack Configuration* is cancelled, completed, or aborted  
   **THEN** the *Non-Attack Ability Lock* is released for all *Combatants*  
   **AND** non-attack abilities become available for activation again

4. **WHEN** a character is removed from the *Attack Configuration* before Confirm  
   **THEN** their *Non-Attack Ability Lock* is released immediately upon removal

---

### Track Attacker and Defender Roles per Character

**Domain terms** (vocabulary for this story's AC):

- *Combat State* — per-character record storing the current role (attacker, defender, neutral)
- *Attacker* — role indicator set on the attacking character
- *Defender* — role indicator set on each defending character

1. **WHEN** a character is assigned as *Attacker* in an *Attack Configuration*  
   **THEN** their *Combat State* shows role = attacker  
   **AND** the *Attack State Indicator* shows the attacker designation on the *Character Overlay*

2. **WHEN** a character is assigned as *Defender*  
   **THEN** their *Combat State* shows role = defender  
   **AND** the *Attack State Indicator* shows the defender designation

3. **WHEN** a character holds both an attacker and defender role assignment simultaneously (invalid state)  
   **THEN** the second assignment is blocked  
   **AND** feedback is shown

4. **WHEN** a character's role is removed (defender removed, or configuration cancelled)  
   **THEN** their *Combat State* role is reset to neutral  
   **AND** the role indicator on the *Character Overlay* is cleared

5. **WHEN** the session has multiple simultaneous *Attack Configurations* open  
   **THEN** each character's *Combat State* reflects its role in its own configuration independently  
   **BUT** a character may not hold a role in more than one active configuration at the same time

---

## Combat Geometry Stories

---

### Detect Knockback Obstruction via Collision Ray

**Domain terms** (vocabulary for this story's AC):

- *Collision Ray* — geometric probe issued through *Game Collision Detection*
- *Knockback Obstruction* — obstruction detected along the knockback vector
- *Knockback Distance* — the configured displacement, clipped at the obstruction
- *Game Collision Detection* — HookCostume DLL capability

1. **WHEN** *Knockback Distance* is greater than zero for a Hit pair  
   **THEN** a *Collision Ray* is issued from the *Defender*'s position along the knockback direction vector, up to *Knockback Distance* units  
   **AND** *Game Collision Detection* returns the first obstruction point (or a clear result)

2. **WHEN** *Game Collision Detection* returns an obstruction  
   **THEN** *Knockback Obstruction* is recorded at the obstruction distance  
   **AND** *Knockback Movement* is applied only to the obstruction point, not the full *Knockback Distance*

3. **WHEN** *Game Collision Detection* returns a clear path  
   **THEN** *Knockback Movement* is applied for the full *Knockback Distance*

4. **WHEN** the COH game client is not running at query time  
   **THEN** *Game Collision Detection* returns a clear-path result as the safe default  
   **AND** the full *Knockback Distance* is applied  
   **AND** a warning is logged that the collision query was unavailable

---

### Calculate Line-of-Sight for Ranged Attack

**Domain terms** (vocabulary for this story's AC):

- *Line-of-Sight* — clear / blocked result of a collision ray from attacker to defender
- *Ranged Attack* — requires clear LOS to each defender
- *Collision Ray* — the probe used to evaluate LOS
- *Game Collision Detection* — HookCostume DLL capability

1. **WHEN** the GM confirms a *Ranged Attack*  
   **THEN** a *Collision Ray* is issued from the *Attacker*'s position to each *Defender*'s position  
   **AND** *Game Collision Detection* returns a clear or blocked result for each

2. **WHEN** *Line-of-Sight* to a *Defender* is clear  
   **THEN** that *Defender* is included in *Combat Execution* normally

3. **WHEN** *Line-of-Sight* to a *Defender* is blocked  
   **THEN** that *Defender* is excluded from execution  
   **AND** the GM is shown which defenders were excluded and why  
   **BUT** other defenders with clear LOS are included

4. **WHEN** all defenders have blocked *Line-of-Sight*  
   **THEN** Confirm is blocked  
   **AND** feedback indicates no valid targets for the ranged attack

5. **WHEN** the COH game client is not running at LOS query time  
   **THEN** all defenders are treated as clear LOS (safe default)  
   **AND** a warning is logged

---

### Query Game Collision Detection via HookCostume DLL

**Domain terms** (vocabulary for this story's AC):

- *Game Collision Detection* — the HookCostume DLL capability returning obstruction data
- *Collision Ray* — the probe parameters passed to the DLL
- *Game Bridge* — the initialized boundary that routes DLL queries

1. **WHEN** a *Collision Ray* query is issued by the application  
   **THEN** the query parameters are passed to the HookCostume DLL via the *Game Bridge*  
   **AND** the DLL returns the distance and position of the first obstruction, or a clear-path indicator

2. **WHEN** the *Game Bridge* is not initialized at query time  
   **THEN** the query returns a clear-path result as the safe default  
   **AND** a warning is logged indicating the DLL was unavailable

3. **WHEN** a query is issued with zero maximum distance  
   **THEN** the DLL immediately returns a clear-path result  
   **AND** no obstruction processing is performed

4. **WHEN** the DLL returns an error response  
   **THEN** the application logs the error  
   **AND** a clear-path result is used as the fallback  
   **BUT** the invoking operation (LOS check or knockback) proceeds with the fallback

---

## HCS Integration Stories

---

### Start HCS File Watcher Integration

**Domain terms** (vocabulary for this story's AC):

- *HCS Integration* — the subsystem connecting HVT to the Hero Combat System
- *HCS File Watcher* — the component that monitors the HCS output directory
- *Game Bridge* — must be initialized before HCS integration can start

1. **WHEN** the GM triggers Start HCS File Watcher Integration  
   **THEN** the *HCS File Watcher* begins monitoring the designated HCS output directory for new or updated *Info Files*  
   **AND** the HCS integration status indicator shows active

2. **WHEN** the *Game Bridge* is not initialized at start time  
   **THEN** the start is blocked  
   **AND** feedback is displayed indicating the game bridge must be initialized first  
   **BUT** the HCS integration state is not changed

3. **WHEN** the HCS output directory does not exist  
   **THEN** the start is blocked  
   **AND** feedback indicates the directory cannot be found  
   **BUT** the *HCS File Watcher* is not activated

4. **WHEN** *HCS Integration* is already active  
   **THEN** a second Start request is a no-op  
   **AND** no duplicate watcher is created

---

### Read On-Deck Combatants from Info File

**Domain terms** (vocabulary for this story's AC):

- *On-Deck Combatants* — characters whose turns are imminent per the HCS turn order
- *Info File* — the file written by HCS containing turn state
- *Roster Entry* — the HVT record matched to each on-deck character name
- *Character Overlay* — updated to highlight on-deck status

1. **WHEN** a new *Info File* arrives and contains an on-deck combatants list  
   **THEN** the *HCS File Watcher* reads the list  
   **AND** each named character is matched to a *Roster Entry*  
   **AND** the matched *Character Overlays* are highlighted to indicate upcoming-turn status

2. **WHEN** a named character in the on-deck list does not match any *Roster Entry*  
   **THEN** that character is skipped  
   **AND** a warning is logged  
   **BUT** other matched characters are processed normally

3. **WHEN** the on-deck list is empty  
   **THEN** no character overlays are highlighted for on-deck status  
   **AND** no error is raised

---

### Read Eligible Combatants from Info File

**Domain terms** (vocabulary for this story's AC):

- *Eligible Combatants* — characters available to act in the current HCS phase
- *Info File* — source of the eligible list
- *Roster Entry* — matched to each eligible character name

1. **WHEN** a new *Info File* arrives with an eligible combatants list  
   **THEN** each named character is matched to a *Roster Entry*  
   **AND** eligible status is reflected in the HVT UI for matched characters

2. **WHEN** a character in the eligible list does not match any *Roster Entry*  
   **THEN** the character is skipped with a warning  
   **BUT** other eligible characters are processed

3. **WHEN** the eligible list is empty  
   **THEN** no characters are marked eligible  
   **AND** the UI reflects no eligible combatants for the current phase

---

### Read Active Character from Info File

**Domain terms** (vocabulary for this story's AC):

- *Active Character (HCS)* — the character whose turn is currently active in the HCS chronometer
- *Info File* — the source of the active character designation
- *Active Character* (Increment 5) — the HVT roster selection synchronized to the HCS designation

1. **WHEN** a new *Info File* arrives with an active character designation  
   **THEN** the *HCS File Watcher* reads the active character name  
   **AND** matches it to a *Roster Entry*  
   **AND** the HVT *Active Character* selection is synchronized to the matched *Roster Entry*

2. **WHEN** the designated *Active Character (HCS)* does not match any *Roster Entry*  
   **THEN** no roster selection change is made  
   **AND** a warning is logged with the unmatched character name

3. **WHEN** the active character designation is absent from the *Info File*  
   **THEN** the current HVT *Active Character* selection is unchanged  
   **AND** no error is raised

---

### Read Chronometer Turn State from Info File

**Domain terms** (vocabulary for this story's AC):

- *Chronometer Turn State* — per-combatant turn phase indicator from HCS (active, held, passed, waiting)
- *Info File* — the file containing the turn state data
- *Combat State* — updated for affected characters based on their phase

1. **WHEN** a new *Info File* arrives with chronometer turn state data  
   **THEN** each named character's phase is read  
   **AND** each character's *Combat State* is updated to reflect their current HCS phase

2. **WHEN** a character's HCS phase changes to held  
   **THEN** their *Combat State* is updated to reflect the held state  
   **AND** the *Attack State Indicator* is updated accordingly

3. **WHEN** a character listed in the *Chronometer Turn State* is not in the *Roster*  
   **THEN** that character is skipped  
   **AND** a warning is logged

---

### Process Attack Result Events from HCS

**Domain terms** (vocabulary for this story's AC):

- *Attack Result Event* — HCS info file entry describing a resolved attack outcome
- *Combat Execution* — the HVT path invoked to apply the attack result
- *Attacker-Defender Pair* — instantiated from the event payload

1. **WHEN** the *HCS File Watcher* reads an *Attack Result Event* from an *Info File*  
   **THEN** the event payload (attacker, defender(s), Hit or Miss) is dispatched to *Combat Execution*  
   **AND** *Combat Execution* applies effects per the event parameters to the matched *Roster Entries*

2. **WHEN** the *Attack Result Event* payload names characters not in the *Roster*  
   **THEN** unmatched characters are skipped  
   **AND** a warning is logged  
   **BUT** matched characters receive their effects normally

3. **WHEN** the *Attack Result Event* specifies a Miss  
   **THEN** no *Status Effect*, *On-Hit Animation*, or *Knockback* is applied  
   **AND** the *Attack Animation* on the attacker still plays

4. **WHEN** multiple *Attack Result Events* arrive in the same *Info File*  
   **THEN** each event is processed in file order  
   **AND** each is dispatched independently to *Combat Execution*

---

### Process Simple Ability Events from HCS

**Domain terms** (vocabulary for this story's AC):

- *Simple Ability Event* — HCS info file entry for a non-attack ability use
- *Animated Ability* — the HVT ability triggered by the event

1. **WHEN** the *HCS File Watcher* reads a *Simple Ability Event* from an *Info File*  
   **THEN** the named ability is triggered on the named character's *Animated Ability* playback path

2. **WHEN** the named character is not in the *Roster*  
   **THEN** the event is skipped with a warning

3. **WHEN** the named ability does not exist on the character  
   **THEN** a warning is logged  
   **AND** no ability is played  
   **BUT** other events from the same *Info File* are still processed

4. **WHEN** the character has a *Non-Attack Ability Lock* active  
   **THEN** the *Simple Ability Event* is blocked from triggering the ability  
   **AND** a warning is logged indicating the lock prevented playback

---

### Resolve Held Character State from HCS

**Domain terms** (vocabulary for this story's AC):

- *Held Character State* — HCS flag indicating a character is holding its action
- *Combat State* — updated to reflect held status
- *Attack State Indicator* — updated on the character overlay

1. **WHEN** the *HCS File Watcher* reads a *Held Character State* entry from an *Info File*  
   **THEN** the named character's *Combat State* is updated to reflect the held phase  
   **AND** the *Attack State Indicator* on the character's *Character Overlay* shows the held designation

2. **WHEN** the held character is not in the *Roster*  
   **THEN** the entry is skipped with a warning

3. **WHEN** a subsequent *Info File* no longer lists a character as held  
   **THEN** the held designation is removed from the character's *Combat State*  
   **AND** the *Attack State Indicator* is updated to reflect the new phase

---

### Execute Sweep Results from HCS

**Domain terms** (vocabulary for this story's AC):

- *Sweep Results* — HCS-generated multi-target sweep outcome listing defenders and their individual results
- *Sweep Attack* — the execution path triggered by the sweep results payload
- *Attacker-Defender Pair* — instantiated from each sweep result entry

1. **WHEN** the *HCS File Watcher* reads *Sweep Results* from an *Info File*  
   **THEN** the payload is dispatched to the *Sweep Attack* execution path  
   **AND** each entry in the payload is treated as an *Attacker-Defender Pair* to be resolved in sequence

2. **WHEN** an entry in the *Sweep Results* lists a defender not in the *Roster*  
   **THEN** that entry is skipped  
   **AND** all other entries are resolved normally

3. **WHEN** the sweep execution applies effects to all listed defenders  
   **THEN** *Attack State Indicators* are updated for each affected character  
   **AND** the *Attack Configuration* is closed after all pairs resolve

4. **WHEN** the *Sweep Results* payload is empty  
   **THEN** no execution occurs  
   **AND** a warning is logged

---

### Stop HCS Integration

**Domain terms** (vocabulary for this story's AC):

- *HCS Integration* — the file-watcher subsystem
- *HCS File Watcher* — deactivated on stop

1. **WHEN** the GM triggers Stop HCS Integration  
   **THEN** the *HCS File Watcher* stops monitoring the output directory  
   **AND** the HCS integration status indicator shows inactive

2. **WHEN** an *Info File* is being processed at the time Stop is triggered  
   **THEN** the current file processing completes  
   **AND** no further files are read after processing is done

3. **WHEN** HCS Integration is already stopped  
   **THEN** a Stop request is a no-op  
   **AND** no error is raised

4. **WHEN** the session ends while HCS Integration is active  
   **THEN** the *HCS File Watcher* is stopped automatically  
   **AND** no further file events are processed
