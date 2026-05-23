# Specification by Example — Increment 6: Crowd Orchestration and Combat

> Domain sources: `docs/increment-6/crc-increment-6.md`, `docs/increment-6/acceptance-criteria-increment-6.md`, `docs/increment-6/ubiquitous-language-increment-6.md`.
> 42 stories, 5 Key Abstractions: Crowd Move, Attack Configuration, Combat Execution, Combat Geometry, HCS Integration.

---

## Crowd Move Stories

---

### Story: Move Crowd with Relative Positioning

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has spawned crowd members

Scenario Outline: Move crowd with relative positioning strategy
  Given the **Crowd Move** has *positioning strategy* "relative" and *target crowd members* as shown below
  When the GM designates a destination
  Then the **Relative Positioning** applies *displacement vector* {displacement_vector} to all members

  Crowd Move (Given):
  | scenario                              | positioning_strategy | target_crowd_members           |
  | All members spawned                   | relative             | Guard_A, Guard_B, Guard_C      |
  | One member unspawned                  | relative             | Guard_A, Guard_C (spawned only)|
  | Zero offset destination               | relative             | Guard_A, Guard_B               |
  | One member fails mid-move             | relative             | Guard_A, Guard_B, Guard_C      |

  Relative Positioning (Then):
  | scenario                              | displacement_vector   |
  | All members spawned                   | (50.0, 0.0, -30.0)   |
  | One member unspawned                  | (50.0, 0.0, -30.0)   |
  | Zero offset destination               | (0.0, 0.0, 0.0)      |
  | One member fails mid-move             | (50.0, 0.0, -30.0)   |

  Then all members begin moving simultaneously with the same offset vector
  And the **Group Formation** *relative spatial offsets* are preserved after the move
  And unspawned members are silently excluded without error
  And when a member's **Movement Execution** fails the failure is reported but other members are not rolled back

---

### Story: Move Crowd with Optimal Spread Positioning

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has spawned crowd members

Scenario Outline: Move crowd with optimal spread strategy
  Given the **Crowd Move** has *positioning strategy* "optimal spread" and *target crowd members* as shown below
  When the GM designates a destination center
  Then the **Optimal Spread Positioning** assigns *computed spread slots* as shown below

  Crowd Move (Given):
  | scenario                              | positioning_strategy | target_crowd_members           |
  | Multiple members — spread slots       | optimal spread       | Guard_A, Guard_B, Guard_C      |
  | Single member — center slot           | optimal spread       | Guard_A                        |
  | Partial obstruction                   | optimal spread       | Guard_A, Guard_B, Guard_C      |
  | Gang mode — leader facing applied     | optimal spread       | Guard_A, Guard_B (gang)        |

  Optimal Spread Positioning (Then):
  | scenario                              | computed_spread_slots                      |
  | Multiple members — spread slots       | slot_1, slot_2, slot_3 (evenly spaced)    |
  | Single member — center slot           | destination_center                         |
  | Partial obstruction                   | nearest unobstructed alternatives          |
  | Gang mode — leader facing applied     | slot_1, slot_2 (evenly spaced)            |

  Then each member is assigned a unique slot minimizing individual travel distance
  And no two members share the same slot position
  And when the destination area is partially obstructed, slots in unobstructed areas are assigned first
  And when the crowd is a **Gang Mode** group, post-move facing uses **Gang Leader Facing** instead of **Facing Destination**

---

### Story: Maintain Group Formation during Crowd Move

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Roster** has spawned crowd members with known positions

Scenario Outline: Preserve formation during relative positioning move
  Given the **Group Formation** has *relative spatial offsets* {relative_spatial_offsets}
  When a **Crowd Move** with **Relative Positioning** completes
  Then the **Group Formation** *relative spatial offsets* are preserved as shown below

  Group Formation (Given/Then):
  | scenario                              | relative_spatial_offsets                   |
  | Formation preserved after move        | A:(0,0,0), B:(5,0,0), C:(0,0,5)          |
  | Different starting positions          | A:(0,0,0), B:(10,0,0), C:(5,0,10)        |
  | Member position unreadable            | blocked until resolved                     |

  Then the pairwise distances between all members match those recorded at move start
  And the absolute positions are all offset by the move delta
  And when a member's position cannot be read the move is not issued until all positions are resolved

---

### Story: Turn Characters to Face Destination

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a **Crowd Move** has just completed

Scenario Outline: Apply facing direction after crowd move
  When facing commands are issued post-move
  Then the **Facing Destination** has *facing vector* {facing_vector} as shown below

  Facing Destination (Then):
  | scenario                              | facing_vector          |
  | Non-gang — face destination           | toward_destination     |
  | Gang — leader facing substitutes      | N/A (leader facing)    |
  | Member at destination point — skip    | skip_no_command        |
  | One member facing fails               | toward_destination     |

  Then when the crowd is not a **Gang Mode** group each member faces the movement destination center
  And when the crowd is an active **Gang Mode** group **Gang Leader Facing** is applied instead
  And when a member's new position equals the destination no facing command is issued for that member
  And facing updates are applied before the **Crowd Move** is considered complete
  And when a facing command fails for one member all other members still receive their commands

---

### Story: Align Character Facing with Gang Leader

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a **Gang Mode** group is active

Scenario Outline: Align all gang members to leader facing
  Given the **Gang Leader Facing** references a **Gang Leader** with *leader facing vector* {leader_facing_vector}
  When the GM triggers Align Character Facing with Gang Leader
  Then all spawned gang members receive a facing command matching the *leader facing vector*

  Gang Leader Facing (Given/Then):
  | scenario                              | leader_facing_vector   |
  | Leader spawned — alignment applied    | (1.0, 0.0, 0.0)       |
  | Leader not spawned — blocked          | unreadable             |
  | One member not spawned — skipped      | (1.0, 0.0, 0.0)       |
  | Gang mode not active — unavailable    | N/A                    |

  Then when the **Gang Leader** is spawned all other spawned members align to the leader's facing
  And when the **Gang Leader** is not spawned no facing commands are issued and the failure is reported
  And unspawned members are skipped; all other spawned members receive the command
  And when **Gang Mode** is not active, **Gang Leader Facing** alignment is unavailable

---

## Attack Configuration Stories

---

### Story: Select Attacking Character

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Assign attacker role in attack configuration
  When the GM opens or changes the **Attacker** in the **Attack Configuration**
  Then the **Attacker** has *attacking role* {attacking_role} as shown below

  Attacker (Then):
  | scenario                              | attacking_role      |
  | Character pre-assigned on open        | Guard_Captain_01    |
  | Different attacker selected           | Villain_Boss_03     |
  | Already a defender — rejected         | rejected            |
  | Unspawned character — rejected        | rejected            |

  Combat State (Then):
  | scenario                              | current_role |
  | Character pre-assigned on open        | attacker     |
  | Different attacker selected           | attacker     |
  | Already a defender — rejected         | unchanged    |
  | Unspawned character — rejected        | unchanged    |

  Then when a character is pre-assigned the **Combat State** *current role* is set to "attacker"
  And when a different character is selected the previous attacker's *current role* resets to neutral
  And when the character is already a **Defender** the selection is rejected
  And when the character is unspawned the selection is rejected

---

### Story: Activate Attack Ability

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Activate attack ability from context menu
  When the GM activates an attack ability from the **Context Menu**
  Then the **Attack Configuration** panel opens as shown below

  Attack Configuration (Then):
  | scenario                              | attacker_assignment |
  | Attack ability activated              | Guard_Captain_01    |
  | No attack ability defined — blocked   | not_opened          |
  | Panel open — abilities locked         | Guard_Captain_01    |
  | GM cancels — state reset              | closed              |

  Non-Attack Ability Lock (Then):
  | scenario                              | suppression_state |
  | Panel open — abilities locked         | active            |
  | GM cancels — state reset              | released          |

  Then when the panel opens the activating character is assigned as **Attacker** and the Confirm button is disabled until at least one **Defender** is added
  And when no attack ability is defined no panel opens with appropriate feedback
  And when the panel is open all non-attack abilities on the **Attacker** are locked
  And when the GM cancels the **Combat State** resets to neutral and locks are released

---

### Story: Select Defender Targets

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Attack Configuration** panel is open with an **Attacker** assigned

Scenario Outline: Add and remove defenders in combatant selectors
  When the GM adds or removes a **Defender**
  Then the **Defender** has *defending role* {defending_role} as shown below

  Defender (Then):
  | scenario                              | defending_role   |
  | Add spawned defender                  | Villain_Boss_03  |
  | Add second defender                   | Healer_01        |
  | Already the attacker — rejected       | rejected         |
  | Unspawned — rejected                  | rejected         |
  | Remove defender                       | removed          |

  Combat State (Then):
  | scenario                              | current_role |
  | Add spawned defender                  | defender     |
  | Remove defender                       | neutral      |

  Then when a spawned character is added an **Attacker-Defender Pair** is created with default parameters
  And when the character is already the **Attacker** the addition is rejected
  And when unspawned the addition is rejected
  And when removed the **Attacker-Defender Pair** is deleted and *current role* resets to neutral

---

### Story: Confirm Attack Targets

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Lock in combatant selection
  Given the **Attack Configuration** has *attacker assignment* {attacker_assignment} and *configured defenders* {configured_defenders}
  When the GM clicks Confirm Targets
  Then the combatant list is locked as shown below

  Attack Configuration (Given):
  | scenario                              | attacker_assignment | configured_defenders |
  | Valid — lock succeeds                 | Guard_Captain_01    | Villain_Boss_03      |
  | No defender — blocked                 | Guard_Captain_01    | empty                |
  | No attacker — blocked                 | empty               | Villain_Boss_03      |
  | Post-lock — add/remove disabled       | Guard_Captain_01    | Villain_Boss_03      |

  Then when both attacker and defenders are present the list is locked and the attack parameters region becomes editable
  And when no **Defender** is present the confirmation is rejected with feedback
  And when no **Attacker** is assigned the confirmation is rejected
  And after lock the Add/Remove Defender actions are disabled

---

### Story: Configure Attack for Attacker-Defender Pair

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Attack Configuration** has confirmed targets

Scenario Outline: Configure parameters per attacker-defender pair
  When the GM edits parameters for an **Attacker-Defender Pair**
  Then the **Attacker-Defender Pair** stores values as shown below

  Attacker-Defender Pair (Then):
  | scenario                              | paired_attacker    | paired_defender    | attack_effect | knockback_distance | attack_result |
  | Configure effect and knockback        | Guard_Captain_01   | Villain_Boss_03    | Stunned       | 5                  | Hit           |
  | Different pair — independent          | Guard_Captain_01   | Healer_01          | Dead          | 0                  | Miss          |
  | Negative knockback — rejected         | Guard_Captain_01   | Villain_Boss_03    | Stunned       | 0 (reverted)       | Hit           |
  | All defaults accepted                 | Guard_Captain_01   | Villain_Boss_03    | Stunned       | 0                  | Miss          |

  Then each pair's parameters are stored independently and changes to one pair do not affect others
  And when a negative *knockback distance* is entered the value is rejected and reverts to zero
  And when all defaults are left unchanged default values (Miss, zero knockback, Stunned, Attack mode) are used

---

### Story: Set Attack Effect (Stunned, Unconscious, Dying, Dead)

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has confirmed targets

Scenario Outline: Set attack effect on a pair
  Given the **Attacker-Defender Pair** has *attack result* {attack_result}
  When the GM selects an **Attack Effect** *effect type* {effect_type}
  Then the **Status Effect** applies *applied condition* as shown below

  Attack Effect (When):
  | scenario                              | effect_type   |
  | Stunned selected — Hit pair           | Stunned       |
  | Unconscious selected — Hit pair       | Unconscious   |
  | Dead selected — Hit pair              | Dead          |
  | Any effect — Miss pair (no apply)     | Dying         |
  | No effect selected — blocked          | empty         |

  Attacker-Defender Pair (Given):
  | scenario                              | attack_result |
  | Stunned selected — Hit pair           | Hit           |
  | Unconscious selected — Hit pair       | Hit           |
  | Dead selected — Hit pair              | Hit           |
  | Any effect — Miss pair (no apply)     | Miss          |

  Status Effect (Then):
  | scenario                              | applied_condition |
  | Stunned selected — Hit pair           | Stunned           |
  | Unconscious selected — Hit pair       | Unconscious       |
  | Dead selected — Hit pair              | Dead              |
  | Any effect — Miss pair (no apply)     | not_applied       |
  | No effect selected — blocked          | not_applied       |

  Then when *attack result* is "Hit" the **Status Effect** matching the *effect type* is applied during execution
  And when *attack result* is "Miss" no **Status Effect** is applied regardless of the setting
  And when no *effect type* is selected Confirm is blocked with feedback

---

### Story: Set Knockback Distance

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has confirmed targets

Scenario Outline: Set knockback distance for a pair
  When the GM enters *displacement units* {displacement_units} in the **Knockback Distance** field
  Then the **Knockback Distance** is stored as shown below

  Knockback Distance (Then):
  | scenario                              | displacement_units |
  | Positive value entered                | 5                  |
  | Zero entered — no knockback           | 0                  |
  | Non-numeric — rejected                | rejected           |
  | Obstruction clips distance            | 5 (may be clipped) |

  Then when *displacement units* is positive a **Knockback Movement** of that distance is issued on Hit
  And when *displacement units* is zero no **Knockback Movement** is applied
  And when a **Knockback Obstruction** is detected the defender moves only to the obstruction point
  And non-numeric values are rejected with feedback

---

### Story: Set Attack Result (Hit or Miss)

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has confirmed targets

Scenario Outline: Set hit or miss for a pair
  When the GM selects **Attack Result** *result type* {result_type}
  Then the pair's execution behavior is determined as shown below

  Attack Result (Then):
  | scenario                              | result_type |
  | Hit selected                          | Hit         |
  | Miss selected                         | Miss        |
  | Multi-defender mixed results          | Hit         |
  | No result selected — blocked          | empty       |

  Then when *result type* is "Hit" all effects (animation, knockback, status) are enabled
  And when *result type* is "Miss" on-hit animation, knockback, and status are skipped but **Attack Animation** still plays
  And each pair's result is independent; Hit pairs apply effects while Miss pairs skip them
  And when no result is selected Confirm is blocked

---

### Story: Set Attack Mode (Attack or Defend)

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has confirmed targets

Scenario Outline: Set attack or defend mode
  When the GM selects **Attack Mode** *mode type* {mode_type}
  Then the mode is stored as shown below

  Attack Mode (Then):
  | scenario                              | mode_type |
  | Attack mode selected                  | Attack    |
  | Defend mode selected                  | Defend    |
  | Defend mode — execution identical     | Defend    |
  | No selection — default Attack         | Attack    |

  Then execution proceeds identically regardless of mode; the mode is passed to HCS for turn-state tracking
  And when no mode is selected the default "Attack" is used without blocking

---

### Story: Designate Center Target for Area Attack

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Designate area center for area attack
  When the GM checks or unchecks Area Center in the attack parameters
  Then the **Area Center** is configured as shown below

  Area Center (Then):
  | scenario                              | designated_target_NPC | area_radius_targets           |
  | Center designated — targets auto-added| Guard_Captain_01      | Villain_A, Villain_B, Villain_C|
  | Pop-up menu not deployed — blocked    | blocked               | N/A                           |
  | No targets in radius — empty          | Guard_Captain_01      | empty                         |
  | Area center unchecked — reverts       | cleared               | cleared                       |

  Then when a center is designated via the **Area Attack Pop-Up Menu** all spawned characters within radius are auto-added as **Defenders**
  And when the pop-up menu is not deployed the designation is blocked with feedback
  And when no characters are in the radius the area is reported empty but the designation is preserved
  And when unchecked all automatically added **Defenders** are removed and configuration reverts to single-target

---

### Story: Execute Ranged Area Attack

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has an **Area Center** designated and **Defenders** populated

Scenario Outline: Execute area attack with line-of-sight checks
  When the GM confirms the **Area Attack**
  Then the **Line-of-Sight** has *path state* {path_state} for each defender as shown below

  Line-of-Sight (Then):
  | scenario                              | path_state |
  | Clear LOS — defender included         | clear      |
  | Blocked LOS — defender excluded       | blocked    |
  | All blocked — no execution            | blocked    |

  Area Attack (Then):
  | scenario                              | area_variant_activation |
  | Clear LOS — defender included         | executed                |
  | All blocked — no execution            | not_executed            |

  Then only defenders with *path state* "clear" are included in **Combat Execution**
  And excluded defenders are shown to the GM with the reason
  And when all defenders are blocked no execution occurs with appropriate feedback
  And when execution proceeds **Attack Animation** plays once on the **Attacker** and effects apply per pair

---

### Story: Execute Sweep Attack across Multiple Targets

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** has confirmed multiple **Defenders**

Scenario Outline: Resolve attacker-defender pairs in sequence
  Given the **Sweep Attack** has *sequential delivery order* {sequential_delivery_order}
  When the GM confirms the **Sweep Attack**
  Then pairs are resolved in sequence as shown below

  Sweep Attack (Given/Then):
  | scenario                              | sequential_delivery_order              |
  | All pairs resolved                    | Pair_1, Pair_2, Pair_3                 |
  | Miss pair — advance without effects   | Pair_1 (Miss), Pair_2 (Hit)           |
  | Abort mid-sweep                       | Pair_1 (resolved), Pair_2 (not resolved)|

  Then for each pair the **Attack Animation** plays then the defender's effects are applied per the pair's configuration
  And when *attack result* is "Miss" no on-hit animation, knockback, or status is applied but execution advances
  And when aborted mid-sweep unresolved pairs produce no effects; already-applied effects are retained
  And when all pairs resolve the **Attack Configuration** closes and the desktop is shown

---

### Story: Assign Auto-Fire Shots per Target

**Covers AC:** 1, 2, 3, 4

Background:
  Given a **Sweep Attack** is configured with multiple **Defenders**

Scenario Outline: Distribute auto-fire shots across defenders
  Given the **Auto-Fire** has *total shot count* {total_shot_count}
  When the GM enters the shot count
  Then shots are distributed as shown below

  Auto-Fire (Given/Then):
  | scenario                              | total_shot_count |
  | Divides evenly — 6 shots, 3 targets   | 6                |
  | Remainder — 7 shots, 3 targets        | 7                |
  | Zero or blank — single exchange        | 0                |
  | Multi-shot per pair — repeats          | 4                |

  Then shots are distributed proportionally; remainders are allocated starting from the first defender
  And when *total shot count* is zero or blank auto-fire is skipped and each pair defaults to a single exchange
  And when more than one shot is assigned the animation and effect sequence repeats for each shot on that pair

---

### Story: Spread Attack across Crowd

**Covers AC:** 1, 2, 3

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Spread attack to all in-range crowd members
  When the GM triggers Spread Attack and designates an **Area Center**
  Then **Defenders** are populated as shown below

  Area Center (Then):
  | scenario                              | designated_target_NPC | area_radius_targets              |
  | Members in range — auto-added         | Guard_Captain_01      | Villain_A, Villain_B             |
  | Multiple crowds in range              | Guard_Captain_01      | Villain_A, Guard_X, Ally_Y       |
  | No members in range                   | Guard_Captain_01      | empty                            |

  Then all spawned crowd members within the area radius are added as **Defenders** with default parameters
  And when multiple crowds have members in range all are included
  And when no members are in range feedback indicates the area is empty but the configuration remains open

---

## Combat Execution Stories

---

### Story: Play Attack Animation on Attacker

**Covers AC:** 1, 2, 3, 4

Background:
  Given **Combat Execution** has begun

Scenario Outline: Play attack animation during pair resolution
  Given the **Attack Animation** has *selected ability* {selected_ability}
  When **Combat Execution** begins a pair resolution
  Then the **Attack Animation** plays as shown below

  Attack Animation (Given/Then):
  | scenario                              | selected_ability    |
  | Ability configured — plays            | fire_blast_attack   |
  | No animation configured — skipped     | none                |
  | Attacker not spawned — aborted        | fire_blast_attack   |

  Then when *selected ability* is configured it plays and execution waits for completion
  And when no animation is configured the step is skipped and execution advances
  And when the **Attacker** is not spawned the animation is skipped and remaining pairs are aborted

---

### Story: Play On-Hit Animation on Defender

**Covers AC:** 1, 2, 3, 4

Background:
  Given **Combat Execution** is resolving a pair

Scenario Outline: Play on-hit animation on defender after attack
  Given the **Attacker-Defender Pair** has *attack result* {attack_result}
  And the **On-Hit Animation** has *selected ability* {selected_ability}
  When the attack animation completes
  Then the **On-Hit Animation** plays as shown below

  Attacker-Defender Pair (Given):
  | scenario                              | attack_result |
  | Hit — on-hit plays                    | Hit           |
  | Miss — no on-hit                      | Miss          |
  | No animation configured — skipped     | Hit           |
  | Defender not spawned — skipped        | Hit           |

  On-Hit Animation (Given/Then):
  | scenario                              | selected_ability  |
  | Hit — on-hit plays                    | stun_hit_react    |
  | No animation configured — skipped     | none              |
  | Defender not spawned — skipped        | stun_hit_react    |

  Then when *attack result* is "Hit" the **On-Hit Animation** corresponding to the **Attack Effect** plays
  And when *attack result* is "Miss" no on-hit animation plays and execution advances
  And when no animation is configured the step is skipped but knockback and status still proceed
  And when the **Defender** is not spawned the step is skipped with a warning

---

### Story: Apply Knockback Movement to Defender

**Covers AC:** 1, 2, 3, 4

Background:
  Given **Combat Execution** is resolving a pair

Scenario Outline: Apply knockback displacement after on-hit
  Given the **Attacker-Defender Pair** has *attack result* {attack_result} and *knockback distance* {knockback_distance}
  When the knockback step executes
  Then the **Knockback Movement** has *knockback destination* as shown below

  Attacker-Defender Pair (Given):
  | scenario                              | attack_result | knockback_distance |
  | Hit with knockback — full distance    | Hit           | 5                  |
  | Hit with obstruction — clipped        | Hit           | 5                  |
  | Zero knockback — no movement          | Hit           | 0                  |
  | Miss — no knockback                   | Miss          | 5                  |

  Knockback Movement (Then):
  | scenario                              | knockback_destination  |
  | Hit with knockback — full distance    | full_5_units           |
  | Hit with obstruction — clipped        | obstruction_point      |
  | Zero knockback — no movement          | no_movement            |
  | Miss — no knockback                   | no_movement            |

  Then when *attack result* is "Hit" and *knockback distance* > 0 a **Collision Ray** is fired first
  And when **Knockback Obstruction** is detected the defender moves only to the obstruction edge
  And when *knockback distance* is zero no collision ray is fired and no movement occurs
  And when *attack result* is "Miss" no knockback is issued

---

### Story: Apply Status Effect to Defender (Stunned, Unconscious, Dying, Dead)

**Covers AC:** 1, 2, 3, 4

Background:
  Given **Combat Execution** is resolving a pair

Scenario Outline: Apply status effect after knockback
  Given the **Attacker-Defender Pair** has *attack result* {attack_result} and *attack effect* {attack_effect}
  When the status effect step executes
  Then the **Status Effect** has *applied condition* {applied_condition} as shown below

  Status Effect (Then):
  | scenario                              | applied_condition |
  | Hit — Stunned applied                 | Stunned           |
  | Hit — Dead applied                    | Dead              |
  | Miss — no effect                      | not_applied       |
  | Prior effect replaced                 | Unconscious       |

  Combat State (Then):
  | scenario                              | active_status_effects |
  | Hit — Stunned applied                 | Stunned               |
  | Hit — Dead applied                    | Dead                  |
  | Miss — no effect                      | unchanged             |
  | Prior effect replaced                 | Unconscious           |

  Then when *attack result* is "Hit" the **Status Effect** is applied to the **Defender**'s **Combat State**
  And the **Attack State Indicator** is updated on the **Character Overlay**
  And when a prior effect exists it is replaced by the new one
  And when *attack result* is "Miss" no effect is applied and any existing effect is unchanged
  And when "Dead" is applied all further combat targeting that defender is blocked in the UI

---

### Story: Update Character Attack State Indicators

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Update indicators on overlays during combat
  Given the **Combat State** has *current role* {current_role} and *active status effects* {active_status_effects}
  When the **Combat State** changes
  Then the **Attack State Indicator** has *displayed effect label* {displayed_effect_label} and *role indicator* {role_indicator} as shown below

  Attack State Indicator (Then):
  | scenario                              | displayed_effect_label | role_indicator |
  | Status effect applied                 | Stunned                | defender       |
  | Attacker role set                     | none                   | attacker       |
  | Combat state reset                    | cleared                | cleared        |
  | Execution completes — final state     | Dead                   | defender       |

  Then when a **Status Effect** is applied the indicator shows the effect label immediately
  And when *current role* is "attacker" the indicator shows the attacker designation
  And when **Combat State** is reset all indicators are cleared
  And when execution completes all indicators reflect final applied effects before the panel closes

---

### Story: Cancel Active Attack

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Cancel attack configuration before execution
  When the GM clicks Cancel in the **Attack Configuration** panel
  Then the **Combat State** and **Non-Attack Ability Lock** reset as shown below

  Combat State (Then):
  | scenario                              | current_role | configuration_linkage |
  | Cancel before Confirm                 | neutral      | cleared               |
  | Cancel with partial parameters        | neutral      | cleared               |
  | Cancel via keyboard shortcut          | neutral      | cleared               |
  | Close without Cancel or Confirm       | neutral      | cleared               |

  Non-Attack Ability Lock (Then):
  | scenario                              | suppression_state |
  | Cancel before Confirm                 | released          |
  | Cancel with partial parameters        | released          |

  Then the **Attack Configuration** panel closes and the desktop is shown
  And all **Combat State** *current role* values reset to neutral for all combatants
  And all **Non-Attack Ability Lock** suppressions are released
  And all unsaved parameters are discarded with no effects applied

---

### Story: Abort Attack in Progress

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given **Combat Execution** is in progress

Scenario Outline: Abort execution mid-flight
  Given the **Combat Execution** has *pair resolution sequence* {pair_resolution_sequence}
  When the GM clicks Abort
  Then execution halts as shown below

  Combat Execution (Given/Then):
  | scenario                              | pair_resolution_sequence            |
  | Abort mid-sweep                       | Pair_1 (done), Pair_2 (halted)     |
  | Abort before any pair resolved        | no pairs resolved                   |
  | Abort — already-applied retained      | Pair_1 effects retained             |
  | Abort not available before Confirm    | N/A (button disabled)               |

  Then the current animation (if running) completes but no further pairs are resolved
  And **Combat State** is reset to neutral for all **Combatants**
  And **Attack State Indicators** reflect any effects applied before the abort point
  And when no pairs have been resolved all characters return to pre-configuration state
  And when Abort is triggered before Confirm has been clicked the button is disabled; Cancel is the exit

---

### Story: Reset Character Combat State

**Covers AC:** 1, 2, 3, 4

Background:
  Given a character has a non-neutral **Combat State**

Scenario Outline: Reset combat state for a character
  Given the **Combat State** has *current role* {current_role} and *active status effects* {active_status_effects}
  When the GM triggers Reset Character Combat State
  Then the **Combat State** resets as shown below

  Combat State (Given/Then):
  | scenario                              | current_role | active_status_effects | configuration_linkage |
  | Reset after completed attack          | defender     | Stunned               | none                  |
  | Reset during active config — blocked  | attacker     | none                  | active (blocked)      |
  | Reset Dead character                  | defender     | Dead                  | none                  |

  Non-Attack Ability Lock (Then):
  | scenario                              | suppression_state |
  | Reset after completed attack          | released          |
  | Reset Dead character                  | released          |

  Then when reset the *current role* becomes neutral, all *active status effects* are cleared, and the **Attack State Indicator** is cleared
  And when in an active configuration the reset is blocked with feedback
  And when a Dead effect is cleared the character becomes eligible for combat again

---

### Story: Disable Non-Attack Abilities during Combat

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Lock non-attack abilities on combatants
  Given the **Combatant** has *combat role* {combat_role}
  When the **Non-Attack Ability Lock** is evaluated
  Then the **Non-Attack Ability Lock** has *suppression state* {suppression_state} as shown below

  Combatant (Given):
  | scenario                              | combat_role |
  | Assigned as attacker — locked         | attacker    |
  | Assigned as defender — locked         | defender    |
  | Config cancelled — released           | neutral     |
  | Removed before Confirm — released     | neutral     |

  Non-Attack Ability Lock (Then):
  | scenario                              | suppression_state |
  | Assigned as attacker — locked         | active            |
  | Assigned as defender — locked         | active            |
  | Config cancelled — released           | released          |
  | Removed before Confirm — released     | released          |

  Then when *combat role* is "attacker" or "defender" all non-attack **Animated Abilities** are locked
  And the lock remains active throughout execution
  And when cancelled, completed, or aborted the lock is released
  And when removed before Confirm the lock is released immediately for that character

---

### Story: Track Attacker and Defender Roles per Character

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Attack Configuration** panel is open

Scenario Outline: Track combat roles via combat state
  Given the **Combat State** has *current role* {current_role}
  When a role is assigned or removed
  Then the **Combat State** updates as shown below

  Combat State (Given/Then):
  | scenario                              | current_role | configuration_linkage |
  | Assigned as attacker                  | attacker     | config_A              |
  | Assigned as defender                  | defender     | config_A              |
  | Dual role attempt — blocked           | unchanged    | unchanged             |
  | Role removed — reset to neutral       | neutral      | cleared               |
  | Multiple configs — independent        | attacker     | config_B              |

  Attack State Indicator (Then):
  | scenario                              | role_indicator |
  | Assigned as attacker                  | attacker       |
  | Assigned as defender                  | defender       |
  | Role removed — reset to neutral       | cleared        |

  Then each character's *current role* reflects its assignment in the active configuration
  And a character may not hold both attacker and defender roles simultaneously
  And when a role is removed the *current role* resets to neutral and the indicator clears
  And a character may not hold a role in more than one active configuration simultaneously

---

## Combat Geometry Stories

---

### Story: Detect Knockback Obstruction via Collision Ray

**Covers AC:** 1, 2, 3, 4

Background:
  Given **Combat Execution** is applying knockback

Scenario Outline: Fire collision ray to detect obstruction
  Given the **Collision Ray** has *origin point* {origin_point}, *direction vector* {direction_vector}, and *maximum distance* {maximum_distance}
  When **Game Collision Detection** processes the ray
  Then the **Knockback Obstruction** has *obstruction point* as shown below

  Collision Ray (Given):
  | scenario                              | origin_point          | direction_vector   | maximum_distance |
  | Clear path — full knockback           | (100, 0, -200)       | (1, 0, 0)         | 5                |
  | Obstruction detected — clipped        | (100, 0, -200)       | (1, 0, 0)         | 5                |
  | Game client not running — safe default| (100, 0, -200)       | (1, 0, 0)         | 5                |

  Knockback Obstruction (Then):
  | scenario                              | obstruction_point     |
  | Clear path — full knockback           | none (full distance)  |
  | Obstruction detected — clipped        | (103, 0, -200)       |
  | Game client not running — safe default| none (full distance)  |

  Then when **Game Collision Detection** returns clear the full *maximum distance* is applied
  And when an obstruction is detected **Knockback Movement** is applied only to the *obstruction point*
  And when the game client is not running a clear-path result is used with a warning logged

---

### Story: Calculate Line-of-Sight for Ranged Attack

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a **Ranged Attack** is confirmed

Scenario Outline: Evaluate line-of-sight to each defender
  Given the **Ranged Attack** has *line-of-sight requirement* "required"
  When **Game Collision Detection** evaluates the path
  Then the **Line-of-Sight** has *path state* {path_state} as shown below

  Line-of-Sight (Then):
  | scenario                              | path_state |
  | Clear to defender — included          | clear      |
  | Blocked to defender — excluded        | blocked    |
  | All blocked — confirm blocked         | blocked    |
  | Game client unavailable — safe default| clear      |

  Then when *path state* is "clear" the **Defender** is included in **Combat Execution**
  And when *path state* is "blocked" the **Defender** is excluded and the GM is shown the reason
  And when all defenders are blocked Confirm is blocked with feedback
  And when the game client is unavailable all defenders are treated as clear (safe default) with a warning

---

### Story: Query Game Collision Detection via HookCostume DLL

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application needs collision data

Scenario Outline: Query HookCostume DLL for collision
  Given the **Game Collision Detection** has *DLL capability* {DLL_capability}
  When a **Collision Ray** query is issued
  Then **Game Collision Detection** returns as shown below

  Game Collision Detection (Given/Then):
  | scenario                              | DLL_capability |
  | DLL available — obstruction returned  | available      |
  | DLL available — clear path            | available      |
  | Game Bridge not initialized — default | unavailable    |
  | Zero max distance — immediate clear   | available      |
  | DLL error response — fallback         | error          |

  Then when *DLL capability* is "available" the query returns the first obstruction or a clear-path indicator
  And when *DLL capability* is "unavailable" a clear-path result is used with a warning logged
  And when maximum distance is zero the DLL returns clear immediately
  And when a DLL error occurs a clear-path fallback is used and the error is logged

---

## HCS Integration Stories

---

### Story: Start HCS File Watcher Integration

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application is running

Scenario Outline: Start file watcher for HCS integration
  Given the **HCS Integration** has *integration state* {integration_state}
  When the GM triggers Start HCS File Watcher Integration
  Then the **HCS File Watcher** has *monitoring state* {monitoring_state} as shown below

  HCS Integration (Given/Then):
  | scenario                              | integration_state |
  | Game bridge ready — start succeeds    | active            |
  | Game bridge not initialized — blocked | inactive          |
  | Output directory missing — blocked    | inactive          |
  | Already active — no-op               | active            |

  HCS File Watcher (Then):
  | scenario                              | monitoring_state |
  | Game bridge ready — start succeeds    | monitoring       |
  | Game bridge not initialized — blocked | not_monitoring   |
  | Output directory missing — blocked    | not_monitoring   |
  | Already active — no-op               | monitoring       |

  Then when the **Game Bridge** is ready the watcher begins monitoring and the status indicator shows active
  And when the **Game Bridge** is not initialized the start is blocked with feedback
  And when the output directory does not exist the start is blocked
  And when already active a second start is a no-op

---

### Story: Read On-Deck Combatants from Info File

**Covers AC:** 1, 2, 3

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Read on-deck combatants list from info file
  When a new **Info File** arrives with *on-deck combatants data* {on_deck_combatants_data}
  Then the **On-Deck Combatants** has *imminent turn characters* matched to **Roster Entries** as shown below

  Info File (When):
  | scenario                              | on_deck_combatants_data      |
  | Characters matched                    | Guard_A, Villain_B           |
  | One character unmatched               | Guard_A, Unknown_X           |
  | Empty list                            | (empty)                      |

  On-Deck Combatants (Then):
  | scenario                              | imminent_turn_characters |
  | Characters matched                    | Guard_A, Villain_B       |
  | One character unmatched               | Guard_A (only)           |
  | Empty list                            | none                     |

  Then matched characters' **Character Overlays** are highlighted for upcoming-turn status
  And unmatched characters are skipped with a warning logged
  And when the list is empty no overlays are highlighted

---

### Story: Read Eligible Combatants from Info File

**Covers AC:** 1, 2, 3

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Read eligible combatants from info file
  When a new **Info File** arrives with *eligible combatants data* {eligible_combatants_data}
  Then the **Eligible Combatants** has *available-to-act characters* matched as shown below

  Info File (When):
  | scenario                              | eligible_combatants_data   |
  | Characters matched                    | Guard_A, Guard_B, Villain_C|
  | One character unmatched               | Guard_A, Unknown_Y         |
  | Empty list                            | (empty)                    |

  Eligible Combatants (Then):
  | scenario                              | available_to_act_characters   |
  | Characters matched                    | Guard_A, Guard_B, Villain_C   |
  | One character unmatched               | Guard_A (only)                |
  | Empty list                            | none                          |

  Then eligible status is reflected in the UI for matched characters
  And unmatched characters are skipped with a warning
  And when empty no characters are marked eligible

---

### Story: Read Active Character from Info File

**Covers AC:** 1, 2, 3

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Read active character designation from info file
  When a new **Info File** arrives with *active character data* {active_character_data}
  Then the **Active Character HCS** has *HCS active turn designation* matched as shown below

  Info File (When):
  | scenario                              | active_character_data |
  | Character matched                     | Guard_Captain_01      |
  | Character not in roster               | Unknown_NPC           |
  | Designation absent                    | (absent)              |

  Active Character HCS (Then):
  | scenario                              | HCS_active_turn_designation |
  | Character matched                     | Guard_Captain_01            |
  | Character not in roster               | no_change                   |
  | Designation absent                    | no_change                   |

  Then when matched the HVT **Active Character** selection is synchronized to the **Roster Entry**
  And when unmatched no roster selection change is made and a warning is logged
  And when absent the current selection is unchanged

---

### Story: Read Chronometer Turn State from Info File

**Covers AC:** 1, 2, 3

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Read per-combatant turn phase from info file
  When a new **Info File** arrives with *chronometer data*
  Then the **Chronometer Turn State** has *per-combatant phase* {per_combatant_phase} as shown below

  Chronometer Turn State (Then):
  | scenario                              | per_combatant_phase |
  | Phase read — combat state updated     | active              |
  | Phase changes to held                 | held                |
  | Character not in roster — skipped     | skipped             |

  Then each character's **Combat State** is updated to reflect their current HCS phase
  And when a phase changes to "held" the **Attack State Indicator** is updated accordingly
  And unmatched characters are skipped with a warning

---

### Story: Process Attack Result Events from HCS

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Process attack result events from info file
  When a new **Info File** arrives with **Attack Result Events**
  Then the **Attack Result Event** is dispatched as shown below

  Attack Result Event (Then):
  | scenario                              | attacker_and_defenders_payload | result_type |
  | Hit event — effects applied           | Guard_A → Villain_B           | Hit         |
  | Miss event — animation only           | Guard_A → Villain_B           | Miss        |
  | Unmatched character — skipped         | Guard_A → Unknown_X           | Hit         |
  | Multiple events — sequential          | Event_1, Event_2              | Hit, Miss   |

  Then when *result type* is "Hit" all effects (animation, knockback, status) are applied via **Combat Execution**
  And when *result type* is "Miss" no effects are applied but **Attack Animation** still plays
  And unmatched characters are skipped with a warning; matched characters receive effects normally
  And multiple events in the same file are processed in file order

---

### Story: Process Simple Ability Events from HCS

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Process simple ability events from info file
  When a new **Info File** arrives with **Simple Ability Events**
  Then the **Simple Ability Event** is dispatched as shown below

  Simple Ability Event (Then):
  | scenario                              | combatant_name     | ability_identifier |
  | Matched — ability played              | Guard_Captain_01   | heal_burst         |
  | Character not in roster — skipped     | Unknown_NPC        | heal_burst         |
  | Ability not found — warning           | Guard_Captain_01   | nonexistent_skill  |
  | Non-attack lock active — blocked      | Guard_Captain_01   | heal_burst         |

  Then when the character and ability are matched the ability is triggered on the playback path
  And when the character is not in the **Roster** the event is skipped with a warning
  And when the ability does not exist on the character a warning is logged and no ability plays
  And when a **Non-Attack Ability Lock** is active the event is blocked with a warning

---

### Story: Resolve Held Character State from HCS

**Covers AC:** 1, 2, 3

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Resolve held state from info file
  When a new **Info File** arrives with **Held Character State** entries
  Then the **Held Character State** has *held action designation* {held_action_designation} as shown below

  Held Character State (Then):
  | scenario                              | held_action_designation |
  | Character held — state updated        | held                    |
  | Character not in roster — skipped     | skipped                 |
  | No longer held — designation removed  | released                |

  Then when a character is held their **Combat State** reflects the held phase and the **Attack State Indicator** shows the held designation
  And when the character is not in the **Roster** the entry is skipped with a warning
  And when a subsequent file no longer lists the character as held the designation is removed

---

### Story: Execute Sweep Results from HCS

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **HCS File Watcher** is active

Scenario Outline: Execute sweep results from HCS info file
  When a new **Info File** arrives with **Sweep Results**
  Then the **Sweep Results** are dispatched as shown below

  Sweep Results (Then):
  | scenario                              | defender_results_payload                |
  | All defenders matched                 | Villain_A:Hit, Villain_B:Miss          |
  | One defender unmatched                | Villain_A:Hit, Unknown_X:Hit           |
  | All resolved — indicators updated     | Villain_A:Stunned, Villain_B:no_effect |
  | Empty payload — warning               | (empty)                                |

  Then the payload is dispatched to the **Sweep Attack** execution path and each entry is resolved as an **Attacker-Defender Pair** in sequence
  And unmatched defenders are skipped; all other entries resolve normally
  And when all pairs resolve **Attack State Indicators** are updated for affected characters
  And when the payload is empty no execution occurs and a warning is logged

---

### Story: Stop HCS Integration

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **HCS Integration** is active

Scenario Outline: Stop HCS file watcher
  Given the **HCS Integration** has *integration state* {integration_state}
  When the GM triggers Stop HCS Integration
  Then the **HCS File Watcher** has *monitoring state* {monitoring_state} as shown below

  HCS Integration (Given/Then):
  | scenario                              | integration_state |
  | Active — stopped                      | inactive          |
  | Mid-processing — completes then stops | inactive          |
  | Already stopped — no-op              | inactive          |
  | Session ends — auto-stopped           | inactive          |

  HCS File Watcher (Then):
  | scenario                              | monitoring_state |
  | Active — stopped                      | not_monitoring   |
  | Mid-processing — completes then stops | not_monitoring   |
  | Already stopped — no-op              | not_monitoring   |
  | Session ends — auto-stopped           | not_monitoring   |

  Then the watcher stops monitoring and the status indicator shows inactive
  And when a file is being processed it completes before the watcher stops
  And when already stopped the request is a no-op with no error
  And when the session ends the watcher is stopped automatically
