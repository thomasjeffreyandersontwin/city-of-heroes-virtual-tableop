# Specification by Example — Increment 5: Roster and Desktop Interaction

> Domain sources: `docs/increment-5/crc-increment-5.md`, `docs/increment-5/acceptance-criteria-increment-5.md`, `docs/increment-5/ubiquitous-language-increment-5.md`.
> 33 stories, 5 Key Abstractions: Roster, Desktop Overlay, Context Menu, Pop-Up Menu, Game State Query.

---

## Game State Query

---

### Story: Query Hovered NPC Info from Game

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the application is running

Scenario Outline: Query hovered NPC info via DLL
  Given the **Game State Query** has *available state* {available_state}
  When the GM's mouse pointer hovers over an entity in the COH game viewport
  Then the **Hovered NPC Info** has *observed state* {observed_state} and *NPC name* {NPC_name} as shown below

  Game State Query (Given):
  | scenario                          | available_state |
  | Mouse over visible NPC            | available       |
  | Mouse not over any NPC            | available       |
  | Game bridge not initialized       | unavailable     |
  | Mouse moves from NPC to NPC       | available       |
  | Rapid successive queries          | available       |

  Hovered NPC Info (Then):
  | scenario                          | observed_state | NPC_name          | identity_data      |
  | Mouse over visible NPC            | present        | Guard_Captain_01  | hero_costume_A     |
  | Mouse not over any NPC            | absent         | empty             | empty              |
  | Game bridge not initialized       | absent         | empty             | empty              |
  | Mouse moves from NPC to NPC       | present        | Villain_Boss_03   | villain_costume_B  |
  | Rapid successive queries          | present        | Guard_Captain_01  | hero_costume_A     |

  Then when *available state* is "unavailable" the query returns an unavailable signal without fabricating data
  And when the mouse moves from one NPC to another the previous NPC's data is discarded
  And each query returns independently and promptly without blocking subsequent queries

---

### Story: Query Mouse XYZ Position in Game World

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application is running

Scenario Outline: Query mouse world-space position via DLL
  Given the **Game State Query** has *available state* {available_state}
  And the **Mouse XYZ Position** has *focus validity* {focus_validity}
  When the application requests the **Mouse XYZ Position**
  Then the **Mouse XYZ Position** has *world-space coordinates* {world_space_coordinates} as shown below

  Game State Query (Given):
  | scenario                          | available_state |
  | Focused — valid position          | available       |
  | No focus — potentially stale      | available       |
  | Game bridge unavailable           | unavailable     |
  | Different mouse placements        | available       |

  Mouse XYZ Position (Given/Then):
  | scenario                          | focus_validity      | world_space_coordinates   |
  | Focused — valid position          | authoritative       | (125.5, 0.0, -340.2)     |
  | No focus — potentially stale      | potentially stale   | (125.5, 0.0, -340.2)     |
  | Game bridge unavailable           | potentially stale   | unavailable               |
  | Different mouse placements        | authoritative       | (200.0, 10.0, -100.0)    |

  Then when *focus validity* is "potentially stale" coordinates are not silently treated as authoritative
  And when *available state* is "unavailable" no zero-coordinate result is returned as a valid position

---

### Story: Check Game Done State

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the application is running with a **Roster** containing entries

Scenario Outline: Poll game done state and cascade effects
  Given the **Game State Query** has *available state* {available_state}
  When the application polls the **Game Done State**
  Then the **Game Done State** has *session ended* {session_ended} as shown below

  Game State Query (Given):
  | scenario                               | available_state |
  | Session active — game running          | available       |
  | Session ended — map unload             | available       |
  | Game done blocks commands              | available       |
  | New session after game done            | available       |
  | Game bridge unreachable                | unavailable     |

  Game Done State (Then):
  | scenario                               | session_ended |
  | Session active — game running          | false         |
  | Session ended — map unload             | true          |
  | Game done blocks commands              | true          |
  | New session after game done            | false         |
  | Game bridge unreachable                | indeterminate |

  Then when *session ended* is "false" no **Roster Entry** is affected and the **Desktop Overlay** remains unchanged
  And when *session ended* becomes "true" all **Roster Entries** have their **Spawned State** *presence in game world* set to false and all **Character Overlays** are removed
  And when *session ended* is "true" no spawn, move, or game command may be issued until a new session is established
  And when a new session is established the *session ended* flag resets to "false" and spawning resumes
  And when *available state* is "unavailable" game commands are suspended but the **Roster** is not cleared

---

### Story: Split Oversized Command Chains for Execution

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Inspect and split command chains before delivery
  Given the **Command Chain** has *ordered commands* as shown in the Given table below
  When the application assembles and delivers the **Command Chain**
  Then the **Oversized Command Chain** has *detected state* {detected_state} as shown below

  Command Chain (Given):
  | scenario                               | ordered_commands                        |
  | Within limit — single batch            | [cmd_A, cmd_B, cmd_C]                   |
  | Oversized — split into sub-chains      | [cmd_A, cmd_B, ..., cmd_Z] (exceeds)    |
  | Split with delivery failure            | [cmd_A, cmd_B, ..., cmd_Z] (exceeds)    |
  | Zero-command sub-chain produced        | [cmd_A, (empty), cmd_C, cmd_D]          |

  Oversized Command Chain (Then):
  | scenario                               | detected_state |
  | Within limit — single batch            | not detected   |
  | Oversized — split into sub-chains      | detected       |
  | Split with delivery failure            | detected       |
  | Zero-command sub-chain produced        | detected       |

  Then when *detected state* is "not detected" the **Command Chain** is delivered as a single batch in stated order
  And when *detected state* is "detected" the chain is split into sub-chains each within the COH limit, delivered in sequence
  And when a sub-chain delivery fails, remaining sub-chains are not attempted but already-delivered commands remain in effect
  And zero-command sub-chains are skipped; subsequent non-empty sub-chains are still delivered in order

---

### Story: Close Game Bridge on Shutdown

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application is running with the **Game Bridge** in some state

Scenario Outline: Close game bridge during application shutdown
  Given the **Game State Query** has *available state* {available_state}
  When the GM closes the application or triggers shutdown
  Then the **Game Bridge** shutdown sequence completes as shown below

  Game State Query (Given):
  | scenario                              | available_state |
  | Normal shutdown — bridge active       | available       |
  | Bridge already uninitialized          | unavailable     |
  | Abnormal crash                        | available       |

  Then when *available state* is "available" DLL handles are released, the poll loop is stopped, and no further commands are issued
  And when *available state* is "unavailable" the close sequence completes without error; no handles to release
  And on abnormal crash the OS process cleanup unloads the DLL; COH-side **Spawned NPCs** remain in game

---

### Story: Execute Load Map Command

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application is running

Scenario Outline: Issue load map command via game bridge
  Given the **Game State Query** has *available state* {available_state}
  When the GM triggers Load Map with a specified map identifier
  Then the **Game Done State** is polled to confirm transition as shown below

  Game State Query (Given):
  | scenario                              | available_state |
  | Valid map — transition succeeds       | available       |
  | Invalid map — COH rejects            | available       |
  | Game bridge not initialized           | unavailable     |

  Game Done State (Then):
  | scenario                              | session_ended |
  | Valid map — transition succeeds       | false         |
  | Invalid map — COH rejects            | false         |

  Then when the transition succeeds the **Game Done State** confirms the new session is active
  And when the map identifier is invalid the application receives an error signal and no state is modified
  And when *available state* is "unavailable" the command is not issued and the GM sees that the map load cannot proceed

---

## Pop-Up Menu

---

### Story: Write Pop-Up Menu Files to COH Menus Directory

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application needs to write a **Pop-Up Menu**

Scenario Outline: Write menu definition file to disk
  Given the **COH Menus Directory** has *writable state* {writable_state}
  When the application writes the **Pop-Up Menu** to the directory
  Then the **Pop-Up Menu** has *menu definition content* {menu_definition_content} as shown below

  COH Menus Directory (Given):
  | scenario                              | writable_state | directory_path                   |
  | Directory writable — write succeeds   | writable       | C:\COH\data\menus\              |
  | File already exists — overwritten     | writable       | C:\COH\data\menus\              |
  | Directory not writable — write fails  | not writable   | C:\COH\data\menus\              |

  Pop-Up Menu (Then):
  | scenario                              | menu_definition_content       |
  | Directory writable — write succeeds   | area_attack_menu_v1           |
  | File already exists — overwritten     | area_attack_menu_v2           |
  | Directory not writable — write fails  | not_written                   |

  Then when *writable state* is "writable" the file is available on disk for a subsequent load command
  And when a file already exists it is overwritten with the new content
  And when *writable state* is "not writable" the write fails with an error and no partial file is left

---

### Story: Load Pop-Up Menu in Game

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Load pop-up menu file into COH client
  Given the **Pop-Up Menu** has *menu definition content* {menu_definition_content}
  When the application issues the load-pop-up-menu command
  Then the **Game Bridge** delivers the command and the COH client loads the menu

  Pop-Up Menu (Given):
  | scenario                              | menu_definition_content       |
  | File written — load succeeds          | area_attack_menu_v1           |
  | File not written — load fails         | not_written                   |
  | COH client not running                | area_attack_menu_v1           |
  | Updated file — reload replaces        | area_attack_menu_v2           |

  Then when *menu definition content* is written the COH client loads the menu and entries are accessible from the HUD
  And when *menu definition content* is "not_written" the load fails and the application surfaces a warning
  And when the COH client is not running the **Game Bridge** reports unavailable
  And when an updated file is loaded the COH client replaces the previous version

---

### Story: Deploy Area Attack Pop-Up Menu

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Deploy area attack menu at session start
  Given the **Area Attack Pop-Up Menu** has *deployment trigger* {deployment_trigger}
  When a game session is initialized
  Then the **Pop-Up Menu** is written to the **COH Menus Directory** and loaded as shown below

  Area Attack Pop-Up Menu (Given/Then):
  | scenario                              | deployment_trigger       |
  | Session init — deploy succeeds        | session initialization   |
  | Write or load fails                   | session initialization   |
  | Already deployed — redeploy           | session initialization   |
  | GM uses HUD menu entries              | session initialization   |

  Then when deployment succeeds the area attack entries become accessible from the COH HUD
  And when write or load fails the application warns that area attack designation is unavailable but the session continues
  And when already deployed the file is overwritten and reloaded without error
  And when the GM designates an area attack center from the HUD the attack configuration panel receives the designation

---

## Roster

---

### Story: Add Character to Roster

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a session is active

Scenario Outline: Add a character to the session roster
  When the GM adds **Character** {character_name} to the **Roster**
  Then the **Roster Entry** is created as shown below

  Roster Entry (Then):
  | scenario                              | character_name     | spawned_state | active_turn_indicator | gang_membership_indicator |
  | New character added                   | Guard_Captain_01   | false         | hidden                | hidden                    |
  | Duplicate rejected                    | Guard_Captain_01   | rejected      | N/A                   | N/A                       |
  | Empty roster before add               | Villain_Boss_03    | false         | hidden                | hidden                    |
  | Multiple added in sequence            | Healer_01          | false         | hidden                | hidden                    |
  | No identity configured                | Blank_Character    | false         | hidden                | hidden                    |

  Then when *character name* already exists in the **Roster** the addition is rejected with user feedback
  And when the **Roster** was empty the empty-roster placeholder is replaced
  And identity or ability configuration is not required for roster membership

---

### Story: Add Crowd to Roster

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a session is active

Scenario Outline: Add all leaf characters from a crowd
  When the GM adds a **Crowd** to the **Roster**
  Then **Roster Entries** are created as shown below

  Roster Entry (Then):
  | scenario                              | character_name     | spawned_state |
  | Crowd with 3 characters              | Guard_A            | false         |
  | Crowd with 3 characters              | Guard_B            | false         |
  | Crowd with 3 characters              | Guard_C            | false         |
  | One member already on roster          | Guard_B            | skipped       |
  | Empty crowd — no entries added        | N/A                | N/A           |
  | All members already present           | N/A                | N/A           |
  | Nested crowd — leaf expansion         | Nested_Guard_01    | false         |

  Then when a **Character** in the **Crowd** is already present in the **Roster** it is skipped with per-character feedback
  And when the **Crowd** contains no characters the action completes with feedback and no entries are added
  And when the **Crowd** contains nested crowds, leaf characters from all levels are added
  And when all members are already present the **Roster** is unchanged with appropriate feedback

---

### Story: Spawn Character to Desktop from Roster

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Spawn a character from the roster panel
  Given the **Roster Entry** has *character name* {character_name} and **Spawned State** *presence in game world* as shown in the Given table below
  When the GM triggers Spawn on the **Roster Entry**
  Then the **Spawned State** has *presence in game world* {presence_in_game_world} as shown below

  Spawned State (Given/Then):
  | scenario                              | presence_in_game_world |
  | Not spawned — spawn succeeds          | true                   |
  | Already spawned — no-op               | true                   |
  | Spawn command fails                   | false                  |
  | Multiple spawns in sequence           | true                   |

  Roster Entry (Given):
  | scenario                              | character_name     |
  | Not spawned — spawn succeeds          | Guard_Captain_01   |
  | Already spawned — no-op               | Guard_Captain_01   |
  | Spawn command fails                   | Villain_Boss_03    |
  | Multiple spawns in sequence           | Healer_01          |

  Then when *presence in game world* becomes "true" a **Character Overlay** appears in the **Desktop Overlay** and a spawned indicator is shown on the **Roster Entry**
  And when already spawned the action is a no-op with user feedback
  And when the spawn command fails the *presence in game world* remains "false" and the GM sees an error
  And each spawn is independent; failure for one does not affect others

---

### Story: Remove Character from Roster

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has entries

Scenario Outline: Remove a roster entry
  Given the **Roster Entry** has *character name* {character_name} and **Spawned State** *presence in game world* {presence_in_game_world}
  And the **Roster Entry** has *gang membership indicator* {gang_membership_indicator}
  When the GM removes the **Roster Entry**
  Then the entry is deleted from the **Roster** as shown below

  Roster Entry (Given):
  | scenario                              | character_name     | gang_membership_indicator |
  | Spawned — despawn then remove         | Guard_Captain_01   | hidden                    |
  | Not spawned — remove only             | Villain_Boss_03    | hidden                    |
  | Despawn fails — still removed         | Healer_01          | hidden                    |
  | Gang member — gang deactivated first  | Guard_A            | visible                   |
  | Last entry — empty placeholder shown  | Guard_Captain_01   | hidden                    |

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — despawn then remove         | true                   |
  | Not spawned — remove only             | false                  |
  | Despawn fails — still removed         | true                   |
  | Gang member — gang deactivated first  | true                   |
  | Last entry — empty placeholder shown  | true                   |

  Then when *presence in game world* is "true" the **Game Bridge** issues a despawn command and the **Character Overlay** is removed
  And when *presence in game world* is "false" no game command is issued
  And when the despawn command fails the entry is still deleted but the GM sees a warning
  And when *gang membership indicator* is "visible" the gang is deactivated for all members before removal
  And when the last entry is removed the empty-roster placeholder appears

---

### Story: Clear Character from Desktop

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has entries

Scenario Outline: Clear a character from the desktop (despawn but keep on roster)
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  And the **Active Character** has *active designation* {active_designation}
  When the GM triggers Clear on the **Roster Entry**
  Then the **Spawned State** has *presence in game world* as shown below

  Spawned State (Given/Then):
  | scenario                              | presence_in_game_world |
  | Spawned — despawn succeeds            | false                  |
  | Already not spawned — no-op           | false                  |
  | Despawn command fails                 | true                   |

  Active Character (Given):
  | scenario                              | active_designation |
  | Cleared character was active          | cleared            |
  | Cleared character was not active      | unchanged          |

  Then when *presence in game world* was "true" the **Game Bridge** despawns the NPC and the **Character Overlay** is removed
  And when *presence in game world* is already "false" the action is a no-op with user feedback
  And when the despawn fails the *presence in game world* remains "true" and the GM sees an error
  And when the cleared character was the **Active Character** the *active designation* is removed with no auto-replacement
  And the **Roster Entry** remains in the **Roster** after clear

---

### Story: Activate Character (mark as active turn)

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has entries

Scenario Outline: Activate a character for the active turn
  When the GM triggers Activate on a **Roster Entry**
  Then the **Active Character** *active designation* updates as shown below

  Roster Entry (Then):
  | scenario                              | character_name     | active_turn_indicator |
  | Activate new entry                    | Guard_Captain_01   | visible               |
  | Replace existing active               | Villain_Boss_03    | visible               |
  | Previous active cleared               | Guard_Captain_01   | hidden                |
  | Activate unspawned entry              | Healer_01          | visible               |
  | Already active — no-op                | Guard_Captain_01   | visible               |
  | Gang member activated — gang overrides| Guard_A            | visible               |

  Active Character (Then):
  | scenario                              | active_designation   |
  | Activate new entry                    | Guard_Captain_01     |
  | Replace existing active               | Villain_Boss_03      |
  | Activate unspawned entry              | Healer_01            |
  | Already active — no-op                | Guard_Captain_01     |
  | Gang member activated — gang overrides| Guard_A (+ all gang)|

  Then when activated the **Character Overlay** shows an active status indicator (if spawned)
  And when a different entry was active it loses its indicator atomically
  And when *spawned state* is "false" the active indicator is applied but no overlay indicator shows
  And when the character belongs to an active **Gang Mode** group all entries in the gang are activated collectively

---

### Story: Deactivate Character

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Roster** has entries with an **Active Character**

Scenario Outline: Remove active designation from a character
  Given the **Roster Entry** has *gang membership indicator* {gang_membership_indicator} as shown below
  When the GM triggers Deactivate on the **Roster Entry**
  Then the **Active Character** *active designation* is removed as shown below

  Active Character (Then):
  | scenario                              | active_designation |
  | Active entry deactivated              | none               |
  | Not active — no-op                    | unchanged          |
  | Gang member deactivated individually  | none (that entry)  |

  Roster Entry (Given):
  | scenario                              | character_name     | gang_membership_indicator |
  | Active entry deactivated              | Guard_Captain_01   | hidden                    |
  | Not active — no-op                    | Villain_Boss_03    | hidden                    |
  | Gang member deactivated individually  | Guard_A            | visible                   |

  Then when deactivated no other entry is automatically activated
  And when the entry is not active the action is a no-op with no error
  And when *gang membership indicator* is "visible" only that specific entry is deactivated; gang mode is not ended

---

### Story: Activate Crowd as Gang with Gang Leader

**Covers AC:** 1, 2, 3, 4, 5, 6

Background:
  Given the **Roster** has entries from a **Crowd**

Scenario Outline: Activate gang mode with designated leader
  Given the **Gang Mode** has *collective activation state* {collective_activation_state}
  When the GM triggers Activate Gang, selects a **Crowd**, and designates a **Gang Leader**
  Then the **Gang Mode** and **Gang Leader** update as shown below

  Gang Mode (Given/Then):
  | scenario                              | collective_activation_state | member_entries              |
  | Gang activated successfully           | active                      | Guard_A, Guard_B, Guard_C  |
  | Member missing from roster — rejected | inactive                    | N/A                        |
  | No leader designated — blocked        | inactive                    | N/A                        |
  | Existing gang replaced                | active                      | Villain_A, Villain_B       |
  | Single member gang                    | active                      | Guard_A                    |

  Gang Leader (Then):
  | scenario                              | leader_designation | leader_indicator |
  | Gang activated successfully           | Guard_A            | visible          |
  | Existing gang replaced                | Villain_A          | visible          |
  | Single member gang                    | Guard_A            | visible          |

  Then when activation succeeds all member **Roster Entries** show a *gang membership indicator* and matching **Character Overlays** show gang status
  And when a crowd member is missing from the **Roster** the activation is rejected with an error listing missing members; no partial activation
  And when no **Gang Leader** is designated the dialog prevents confirmation
  And when a gang is already active it is deactivated first; previous indicators are cleared
  And a single-character gang is valid; the entry shows both gang and leader indicators

---

### Story: Deactivate Gang

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Roster** has entries

Scenario Outline: Deactivate gang mode
  Given the **Gang Mode** has *collective activation state* {collective_activation_state}
  When the GM triggers Deactivate Gang
  Then the **Gang Mode** has *collective activation state* as shown below

  Gang Mode (Given/Then):
  | scenario                              | collective_activation_state |
  | Gang active — deactivated             | inactive                    |
  | No gang active — no-op                | inactive                    |
  | Some members unspawned                | inactive                    |

  Then when deactivated all gang membership indicators are removed and the **Gang Leader** *leader indicator* is cleared
  And all matching **Character Overlays** have their gang status indicators cleared
  And no entry is automatically activated after deactivation; single-character mode resumes
  And when *collective activation state* is already "inactive" the action is a no-op with user feedback
  And no game command is issued for unspawned members during deactivation

---

## Desktop Overlay

---

### Story: Select Character on Desktop via Mouse Click

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Select character via single click
  When the GM single-clicks in the **Desktop Overlay**
  Then the **Character Overlay** has *selection highlight* {selection_highlight} as shown below

  Character Overlay (Given):
  | scenario                              | selection_highlight |
  | Click on unselected overlay           | none               |
  | Click empty space — clear all         | selected           |
  | Click already-selected overlay        | selected           |
  | Click during multi-select — clears    | multi-select       |

  Character Overlay (Then):
  | scenario                              | selection_highlight |
  | Click on unselected overlay           | selected           |
  | Click empty space — clear all         | none               |
  | Click already-selected overlay        | selected           |
  | Click during multi-select — clears    | selected           |

  Then when a different overlay was selected it loses its *selection highlight*
  And when clicking empty space all selections are cleared and no **Roster Entry** remains highlighted
  And when clicking an already-selected overlay the selection remains
  And when clicking during **Multi-Select** all multi-selections are cleared; only the clicked overlay is selected

---

### Story: Multi-Select Characters

**Covers AC:** 1, 2, 3, 4, 5

> **Note (Reviewer):** The Context Menu CRC invariant states "always scoped to exactly one target character." However, AC5 specifies that a context menu triggered on any selected overlay during multi-select applies to all selected characters simultaneously. For SBE purposes, the AC takes precedence: multi-select context menu actions apply to all selected characters.

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Multi-select characters via modifier-click
  When the GM interacts with a **Character Overlay** via shift/ctrl-click
  Then the **Multi-Select** has *selected overlays* {selected_overlays} as shown below

  Multi-Select (Given):
  | scenario                              | selected_overlays          |
  | Add second overlay to selection       | Guard_Captain_01           |
  | Remove from multi-selection           | Guard_A, Guard_B           |
  | Reduce to one — multi-select ends     | Guard_A, Guard_B           |
  | Plain click during multi — clears all | Guard_A, Guard_B, Guard_C  |
  | Context menu on multi — applies to all| Guard_A, Guard_B           |

  Multi-Select (Then):
  | scenario                              | selected_overlays          |
  | Add second overlay to selection       | Guard_Captain_01, Guard_B  |
  | Remove from multi-selection           | Guard_A                    |
  | Reduce to one — multi-select ends     | Guard_B                    |
  | Plain click during multi — clears all | Guard_C (single-select)    |
  | Context menu on multi — applies to all| Guard_A, Guard_B           |

  Character Overlay (Then):
  | scenario                              | selection_highlight |
  | Add second overlay to selection       | multi-select        |
  | Remove from multi-selection           | multi-select        |
  | Reduce to one — multi-select ends     | selected            |
  | Plain click during multi — clears all | selected            |
  | Context menu on multi — applies to all| multi-select        |

  Then when shift/ctrl-clicking a new overlay it is added and both show the multi-select highlight
  And when shift/ctrl-clicking an already-selected overlay it is removed from the selection
  And when reduced to one overlay the selection returns to single-select highlight
  And when clicking without modifier all multi-selections are cleared
  And when a **Context Menu** is triggered on any selected overlay during multi-select it applies to all selected characters simultaneously

---

### Story: Drag Character to New Position on Desktop

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Drag character overlay to reposition
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  And the **Multi-Select** has *selected overlays* {selected_overlays}
  When the GM drags a **Character Overlay** to a new position
  Then the **Character Overlay** has *position in game world* updated as shown below

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — drag repositions            | true                   |
  | Out-of-bounds — drag cancelled        | true                   |
  | Not spawned — drag unavailable        | false                  |
  | Collision — halts at boundary         | true                   |
  | Multi-select — all move together      | true                   |

  Multi-Select (Given):
  | scenario                              | selected_overlays          |
  | Spawned — drag repositions            | Guard_Captain_01           |
  | Multi-select — all move together      | Guard_A, Guard_B, Guard_C  |

  Character Overlay (Then):
  | scenario                              | position_in_game_world    |
  | Spawned — drag repositions            | (200.0, 0.0, -100.0)     |
  | Out-of-bounds — drag cancelled        | original_position         |
  | Not spawned — drag unavailable        | N/A                       |
  | Collision — halts at boundary         | collision_point           |
  | Multi-select — all move together      | relative_offset_positions |

  Then when spawned **Movement Execution** is invoked and the **Spawned NPC** repositions
  And when the drop point is outside the game world boundary the drag is cancelled and the overlay returns to its original position
  And when *presence in game world* is "false" the drag is not available
  And when **Movement Execution** reports a collision the character stops at the collision point
  And when **Multi-Select** is active all selected characters move together with relative offsets

---

### Story: Double-Click Character to Activate

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Double-click overlay to activate character
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  And the **Gang Mode** has *collective activation state* {collective_activation_state}
  When the GM double-clicks a **Character Overlay**
  Then the **Active Character** has *active designation* as shown below

  Active Character (Then):
  | scenario                              | active_designation  |
  | Double-click activates character      | Guard_Captain_01    |
  | Already active — no-op               | Guard_Captain_01    |
  | Gang active — replaces with single   | Guard_Captain_01    |
  | Not spawned — no effect              | unchanged           |

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Double-click activates character      | true                   |
  | Already active — no-op               | true                   |
  | Gang active — replaces with single   | true                   |
  | Not spawned — no effect              | false                  |

  Gang Mode (Given):
  | scenario                              | collective_activation_state |
  | Double-click activates character      | inactive                    |
  | Gang active — replaces with single   | active                      |
  | Not spawned — no effect              | inactive                    |

  Then when spawned the double-click activates the character and previous active entry loses its indicator
  And when already active the action is a no-op
  And when **Gang Mode** is active the collective activation is replaced by single-character activation
  And when *presence in game world* is "false" the double-click has no effect

---

### Story: Sync Roster Selection with Game Target

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Memory Interface** is monitoring the target register

Scenario Outline: Synchronize roster highlight with in-game target
  When the current target in the COH game client changes
  Then the **Roster Entry** and **Character Overlay** highlights update as shown below

  Roster Entry (Then):
  | scenario                              | character_name     |
  | Target matches roster entry           | Guard_Captain_01   |
  | Target not in roster                  | none highlighted   |
  | Target changes to another roster char | Villain_Boss_03    |
  | Target cleared                        | none highlighted   |

  Character Overlay (Then):
  | scenario                              | selection_highlight |
  | Target matches roster entry           | selected           |
  | Target not in roster                  | none               |
  | Target changes to another roster char | selected           |
  | Target cleared                        | none               |

  Then when the target matches a **Roster Entry** that entry is highlighted in the roster panel and the **Character Overlay** shows a selection highlight
  And when the target is not in the **Roster** no entry or overlay is highlighted
  And when the target changes the previous highlight is cleared and the new one applied
  And when the target is cleared all target-sync highlights are cleared but GM-driven selections are preserved independently

---

### Story: Track Spawned State per Character

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Roster** has entries

Scenario Outline: Track spawned state across lifecycle events
  When a lifecycle event occurs
  Then the **Spawned State** has *presence in game world* {presence_in_game_world} as shown below

  Spawned State (Given/Then):
  | scenario                              | presence_in_game_world |
  | Spawned from roster or context menu   | true                   |
  | Cleared or removed from desktop       | false                  |
  | Game done state becomes true          | false                  |
  | Not spawned — overlay not rendered    | false                  |
  | Multiple spawned simultaneously       | true                   |

  Then when *presence in game world* is "true" a spawned indicator appears on the **Roster Entry** and a **Character Overlay** is rendered
  And when *presence in game world* becomes "false" the spawned indicator is hidden and the **Character Overlay** is removed
  And when **Game Done State** becomes true all entries have *presence in game world* set to "false" simultaneously
  And when *presence in game world* is "false" drag, double-click, and movement interactions are unavailable
  And each character tracks its own *presence in game world* independently

---

## Context Menu

---

### Story: Spawn Character via Context Menu

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Spawn via context menu action
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM opens the **Context Menu** and selects Spawn
  Then the **Context Menu** shows *visible actions* as shown below

  Context Menu (Then):
  | scenario                              | visible_actions  |
  | Not spawned — Spawn available         | Spawn shown      |
  | Already spawned — Spawn hidden        | Spawn hidden     |

  Spawned State (Given/Then):
  | scenario                              | presence_in_game_world |
  | Spawn succeeds                        | true                   |
  | Spawn command fails                   | false                  |

  Then when *presence in game world* is "false" the Spawn action appears in the **Context Menu**
  And when *presence in game world* is "true" the Spawn action is hidden
  And when the spawn succeeds the **Character Overlay** updates and *presence in game world* becomes "true"
  And when the spawn fails *presence in game world* remains "false" and the GM sees an error

---

### Story: Place Character at Location

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Place at location via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  And the **Mouse XYZ Position** has *focus validity* {focus_validity}
  When the GM selects Place at Location from the **Context Menu**
  Then the **Character Overlay** has *position in game world* updated as shown below

  Mouse XYZ Position (Given):
  | scenario                              | focus_validity    | world_space_coordinates   |
  | Valid position — placement succeeds   | authoritative     | (150.0, 0.0, -200.0)     |
  | No focus — placement blocked          | potentially stale | unavailable               |
  | Collision — adjusted destination      | authoritative     | (150.0, 0.0, -200.0)     |

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Valid position — placement succeeds   | true                   |
  | Not spawned — action unavailable      | false                  |
  | Collision — adjusted destination      | true                   |

  Character Overlay (Then):
  | scenario                              | position_in_game_world    |
  | Valid position — placement succeeds   | (150.0, 0.0, -200.0)     |
  | No focus — placement blocked          | unchanged                 |
  | Collision — adjusted destination      | collision_adjusted_point  |

  Then when *focus validity* is "authoritative" **Movement Execution** repositions the character
  And when *focus validity* is "potentially stale" the action surfaces feedback and no movement occurs
  And when *presence in game world* is "false" the action is not available in the **Context Menu**
  And when a collision occurs the character is placed at the closest valid position

---

### Story: Save Character Position

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Save current position via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM selects Save Position from the **Context Menu**
  Then the **Saved Character Position** has *stored coordinates* {stored_coordinates} as shown below

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — save succeeds               | true                   |
  | Position already saved — overwrite    | true                   |
  | Memory read fails — save fails        | true                   |
  | Not spawned — action unavailable      | false                  |

  Saved Character Position (Then):
  | scenario                              | stored_coordinates        |
  | Spawned — save succeeds               | (125.5, 0.0, -340.2)     |
  | Position already saved — overwrite    | (200.0, 5.0, -100.0)     |
  | Memory read fails — save fails        | unchanged                 |
  | Not spawned — action unavailable      | N/A                       |

  Then when the save succeeds the GM sees confirmation and the position is available for future restore
  And when a position already exists it is overwritten
  And when the **Memory Interface** cannot read the position the save fails with an error and the prior saved value is preserved
  And when *presence in game world* is "false" Save Position is not available in the **Context Menu**

---

### Story: Move Camera to Target Character

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Move camera to character via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM selects Move Camera to Target from the **Context Menu**
  Then the **Camera Rig** is directed to the target character's position

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — camera moves                | true                   |
  | Camera rig not active                 | true                   |
  | Not spawned — action unavailable      | false                  |

  Then when spawned the **Camera Rig** moves to frame the target character
  And when the **Camera Rig** is not deployed the action surfaces feedback that it is unavailable
  And when *presence in game world* is "false" Move Camera to Target is not in the **Context Menu**
  And when the move completes subsequent camera operations use the new position

---

### Story: Move Target Character to Camera

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Move character to camera position via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM selects Move Target to Camera from the **Context Menu**
  Then **Movement Execution** repositions the character as shown below

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — move to camera              | true                   |
  | Camera rig not active                 | true                   |
  | Collision on path to camera           | true                   |
  | Not spawned — action unavailable      | false                  |

  Character Overlay (Then):
  | scenario                              | position_in_game_world    |
  | Spawned — move to camera              | camera_position           |
  | Camera rig not active                 | unchanged                 |
  | Collision on path to camera           | collision_point           |
  | Not spawned — action unavailable      | N/A                       |

  Then when spawned the character moves to the **Camera Rig** position and the **Character Overlay** updates
  And when the **Camera Rig** is not active the action surfaces feedback and no movement occurs
  And when a collision blocks the path the character stops at the collision point
  And when *presence in game world* is "false" the action is not available

---

### Story: Reset Character Orientation via Context Menu

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Reset orientation via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM selects Reset Orientation from the **Context Menu**
  Then **Movement Execution** writes the identity rotation matrix

  Spawned State (Given):
  | scenario                              | presence_in_game_world |
  | Spawned — reset succeeds              | true                   |
  | Not spawned — action unavailable      | false                  |
  | Write fails — facing unchanged        | true                   |

  Then when spawned the character faces the default north-facing direction; no position change occurs
  And when *presence in game world* is "false" Reset Orientation is not in the **Context Menu**
  And when **Movement Execution** fails to write the rotation the facing is unchanged and the GM sees an error

---

### Story: Maneuver Character with Camera via Context Menu

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Activate/deactivate maneuver-with-camera via context menu
  Given the **Spawned State** has *presence in game world* {presence_in_game_world}
  When the GM selects Maneuver with Camera from the **Context Menu**
  Then the maneuver-with-camera mode updates as shown below

  Spawned State (Given):
  | scenario                                  | presence_in_game_world |
  | Spawned — mode activated                  | true                   |
  | Camera rig not active — blocked           | true                   |
  | Already active — toggle deactivates       | true                   |
  | Not spawned — action unavailable          | false                  |
  | Mode active and GM moves character        | true                   |

  Then when spawned and the **Camera Rig** is active the maneuver-with-camera mode is activated
  And subsequent movement commands drive the character in the **Camera Rig** facing direction
  And when the **Camera Rig** is not active the action surfaces feedback and is not applied
  And when already active selecting the action again deactivates the mode
  And when *presence in game world* is "false" the action is not available

---

### Story: Activate Character Option via Context Menu

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Activate character via context menu option
  Given the **Gang Mode** has *collective activation state* {collective_activation_state}
  And the **Context Menu** has *target character* {target_character} as shown below
  When the GM selects Activate Option from the **Context Menu**
  Then the **Active Character** has *active designation* {active_designation} as shown below

  Active Character (Then):
  | scenario                              | active_designation   |
  | Activate via context menu             | Guard_Captain_01     |
  | Already active — no-op               | Guard_Captain_01     |
  | Gang active — replaces with single   | Villain_Boss_03      |

  Context Menu (Given):
  | scenario                              | target_character   |
  | Activate via context menu             | Guard_Captain_01   |
  | Already active — no-op               | Guard_Captain_01   |
  | Gang active — replaces with single   | Villain_Boss_03    |

  Gang Mode (Given):
  | scenario                              | collective_activation_state |
  | Activate via context menu             | inactive                    |
  | Already active — no-op               | inactive                    |
  | Gang active — replaces with single   | active                      |

  Then when activated via context menu the result is identical to clicking Activate in the roster panel
  And when already active the action is a no-op
  And when **Gang Mode** is active the collective activation is replaced by single-character activation

---

### Story: Clone and Link Character from Desktop

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given the **Desktop Overlay** has **Character Overlays** rendered

Scenario Outline: Clone and link character via context menu
  Given the **Context Menu** has *target character* {target_character}
  When the GM selects Clone-Link from the **Context Menu**
  Then a new **Roster Entry** is created as shown below

  Context Menu (Given):
  | scenario                              | target_character   |
  | Clone succeeds                        | Guard_Captain_01   |
  | Name duplicates in crowd              | Guard_Captain_01   |
  | Library save fails                    | Guard_Captain_01   |

  Roster Entry (Then):
  | scenario                              | character_name         | spawned_state |
  | Clone succeeds                        | Guard_Captain_01_copy  | false         |
  | Name duplicates in crowd              | Guard_Captain_01 (Copy)| false         |
  | Library save fails                    | not_created            | N/A           |

  Then when the clone succeeds a new **Character** is created as a linked copy in the same **Crowd** and a **Roster Entry** appears below the original
  And when the name would duplicate an existing name a copy suffix is appended
  And when the library save fails no new entry is created and the GM sees an error
  And subsequent modifications to either the original or the clone are reflected in all crowds where either appears
