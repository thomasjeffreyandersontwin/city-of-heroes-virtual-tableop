# Specification by Example — Increment 4: Single Character Movement

> Domain sources: `docs/increment-4/crc-increment-4.md`, `docs/increment-4/acceptance-criteria-increment-4.md`, `docs/increment-4/ubiquitous-language-increment-4.md`.
> 35 stories, 4 Key Abstractions: Memory Interface, Character Movement, Movement Execution, Camera Rig.

---

## Memory Interface

---

### Story: Detect Game Process for Connection

**Covers AC:** 1, 2, 3, 4

Background:
  Given the application has started

Scenario Outline: Detect and attach to game process
  When the **Memory Interface** attempts to attach
  Then the **Game Process** has *running state* {running_state} as shown below
  And the **Memory Interface** has *attached state* {attached_state} as shown below

  Game Process (Given/Then):
  | scenario                                      | running_state | process_handle         | memory_base_address |
  | COH client running, single process            | running       | resolved_handle_01     | 0x00400000          |
  | COH client not running at startup             | not running   | absent                 | absent              |
  | COH terminates during session                 | not running   | invalid                | absent              |
  | Multiple COH processes, correct window handle | running       | resolved_handle_01     | 0x00400000          |

  Memory Interface (Then):
  | scenario                                      | attached_state |
  | COH client running, single process            | attached       |
  | COH client not running at startup             | unattached     |
  | COH terminates during session                 | unattached     |
  | Multiple COH processes, correct window handle | attached       |

  Then when *running state* is "running" the **Memory Interface** resolves all required **Memory Pointers** and movement services become available
  And when *running state* is "not running" all memory reads and writes are blocked without crash or error
  And when the **Game Process** terminates during a session, previously resolved **Memory Pointers** are not reused; a fresh detection and resolution cycle must occur
  And when multiple COH processes are found, the **Memory Interface** attaches only to the process whose *process handle* matches the expected COH window handle

---

### Story: Read Target Character from Memory

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached to the **Game Process**

Scenario Outline: Read current target from targeting register
  When the GM has a character selected in the COH game client
  Then the **Current Target** has *targeted entity identifier* {targeted_entity_identifier} as shown below

  Current Target (Then):
  | scenario                             | targeted_entity_identifier |
  | Character selected in COH            | Guard_Captain_01           |
  | No character selected                | empty                      |
  | GM changes selected character        | Villain_Boss_03            |

  Then when *targeted entity identifier* is "empty" movement commands are blocked until a target is confirmed
  And when the GM changes the selected character the **Memory Interface** detects the change and notifies movement services of the new **Current Target**

---

### Story: Monitor Current Target in Game

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached and a session is active

Scenario Outline: Continuous monitoring of target register
  Given the **Current Target** has *targeted entity identifier* {targeted_entity_identifier} as shown in the Given table below
  When the *targeted entity identifier* changes
  Then the **Current Target** has *targeted entity identifier* as shown in the Then table below and **Movement Execution** is notified

  Current Target (Given):
  | scenario                              | targeted_entity_identifier |
  | Target changes during movement        | Guard_Captain_01           |
  | Target cleared by GM                  | Guard_Captain_01           |
  | Target restored after clear           | empty                      |

  Current Target (Then):
  | scenario                              | targeted_entity_identifier |
  | Target changes during movement        | Villain_03                 |
  | Target cleared by GM                  | empty                      |
  | Target restored after clear           | Guard_Captain_01           |

  Then when *targeted entity identifier* changes while movement is in progress, any in-progress **Movement Execution** against the previous target is halted before the new target is activated
  And when *targeted entity identifier* becomes "empty" movement dispatch is suspended
  But the **Camera Rig** and **Camera Follow** continue operating independently of target state

---

### Story: Wait until Target is Registered after Spawn

**Covers AC:** 1, 2, 3

Background:
  Given a **Spawned NPC** has just been created via a spawn command

Scenario Outline: Poll for target registration
  When the **Memory Interface** polls for NPC name resolution
  Then the **Target Registration** has *registration state* {registration_state} as shown below

  Target Registration (Then):
  | scenario                              | registration_state |
  | NPC registers within timeout          | confirmed          |
  | NPC fails to register (timeout)       | pending            |
  | Movement triggered before registration| pending            |

  Then when *registration state* is "confirmed" **Movement Execution** is unblocked and the GM may proceed
  And when *registration state* is "pending" after timeout the application reports failure and movement remains blocked without writing to unregistered address space
  And when movement is triggered before registration the command is queued or rejected until **Target Registration** is confirmed

---

### Story: Scan and Fix Stale Memory Pointers

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Memory Interface** is attached to the **Game Process**

Scenario Outline: Detect and refresh stale pointers
  Given the **Memory Pointer** has *validation state* {validation_state} as shown in the Given table below
  When the **Memory Interface** runs its periodic scan
  Then the **Memory Pointer** has *validation state* {validation_state} as shown in the Then table below

  Memory Pointer (Given):
  | scenario                                   | validation_state | cached_address |
  | Pointer valid on scan                      | valid            | 0x1A2B3C4D     |
  | Pointer stale on scan — re-resolved        | stale            | 0x1A2B3C4D     |
  | Pointer stale mid-operation                | stale            | 0x1A2B3C4D     |
  | Game process restarts — all pointers reset | stale            | 0x00000000     |

  Memory Pointer (Then):
  | scenario                                   | validation_state |
  | Pointer valid on scan                      | valid            |
  | Pointer stale on scan — re-resolved        | valid            |
  | Pointer stale mid-operation                | valid            |
  | Game process restarts — all pointers reset | valid            |

  Stale Memory Pointer (Then):
  | scenario                                   | detected_state |
  | Pointer valid on scan                      | not detected   |
  | Pointer stale on scan — re-resolved        | detected       |
  | Pointer stale mid-operation                | detected       |
  | Game process restarts — all pointers reset | detected       |

  Then when *detected state* is "detected" the **Memory Interface** re-resolves the pointer from known address patterns before the next read or write
  And when a **Stale Memory Pointer** is detected mid-operation the in-progress read/write is cancelled and held until re-resolution completes
  And when the **Game Process** restarts all previously resolved **Memory Pointers** are treated as invalid and re-resolved from scratch

---

### Story: Read Character Position (X, Y, Z) from Memory

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Read character position from process memory
  Given the **Memory Pointer** for character position has *validation state* {validation_state}
  When **Movement Execution** requests the character's current location
  Then the **Memory Interface** has *character position* {character_position} as shown below

  Memory Interface (Then):
  | scenario                     | character_position          |
  | Valid pointer — normal read  | (125.5, 0.0, -340.2)       |
  | Stale pointer — refresh first| (125.5, 0.0, -340.2)       |

  Memory Pointer (Given):
  | scenario                     | validation_state |
  | Valid pointer — normal read  | valid            |
  | Stale pointer — refresh first| stale            |

  Then the three coordinate values are returned as a validated world-space triple
  And when *validation state* is "stale" the read is blocked until the pointer is refreshed

---

### Story: Write Character Position to Memory

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached

Scenario Outline: Write character position to process memory
  Given the **Target Registration** has *registration state* {registration_state}
  And the **Memory Pointer** for character position has *validation state* {validation_state}
  When **Movement Execution** computes destination {character_position}
  Then the **Memory Interface** writes *character position* as shown below

  Memory Interface (Then):
  | scenario                             | character_position         |
  | Valid pointer, registered target     | (200.0, 5.0, -100.0)      |
  | Stale pointer — refresh then write   | (200.0, 5.0, -100.0)      |

  Target Registration (Given):
  | scenario                             | registration_state |
  | Valid pointer, registered target     | confirmed          |
  | Stale pointer — refresh then write   | confirmed          |
  | Unregistered target — write blocked  | pending            |

  Memory Pointer (Given):
  | scenario                             | validation_state |
  | Valid pointer, registered target     | valid            |
  | Stale pointer — refresh then write   | stale            |
  | Unregistered target — write blocked  | valid            |

  Then when *registration state* is "pending" the write is queued until **Target Registration** succeeds
  And when *validation state* is "stale" the pointer is refreshed before the write is retried

---

### Story: Read Character Model Matrix from Memory

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Read model matrix for orientation computation
  Given the **Memory Pointer** for character model matrix has *validation state* {validation_state}
  When **Movement Execution** needs to compute a turn or orientation change
  Then the **Memory Interface** returns *character model matrix* as a validated 4×4 transform

  Memory Pointer (Given):
  | scenario                       | validation_state |
  | Valid pointer — normal read    | valid            |
  | Stale pointer — refresh first  | stale            |

  Then when *validation state* is "stale" the read is held until the pointer is refreshed

---

### Story: Write Character Rotation Matrix to Memory

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Write rotation matrix for facing change
  Given the **Memory Pointer** for character rotation matrix has *validation state* {validation_state}
  When **Movement Execution** computes a new facing direction
  Then the **Memory Interface** writes *character rotation matrix* and the **Spawned NPC** faces the new direction

  Memory Pointer (Given):
  | scenario                       | validation_state |
  | Valid pointer — normal write   | valid            |
  | Stale pointer — refresh first  | stale            |

  Then when *validation state* is "stale" the write is blocked and the pointer is refreshed before retrying

---

### Story: Read Character Facing Vector from Memory

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Read facing vector for rotation delta computation
  Given the **Memory Pointer** for character facing vector has *validation state* {validation_state}
  When **Movement Execution** or a turn operation needs the character's current facing
  Then the **Memory Interface** returns *character facing vector* as a normalized unit vector

  Memory Pointer (Given):
  | scenario                       | validation_state |
  | Valid pointer — normal read    | valid            |
  | Stale pointer — refresh first  | stale            |

  Then when *validation state* is "stale" the read is held until the pointer is refreshed

---

### Story: Write Character Facing Direction to Memory

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Write computed facing direction
  Given the **Memory Interface** has *character facing vector* {character_facing_vector} as shown below
  When **Movement Execution** determines a new facing direction
  Then the **Memory Interface** writes *character rotation matrix* {character_rotation_matrix} as shown below

  Memory Interface (Given/Then):
  | scenario                                | character_facing_vector | character_rotation_matrix |
  | New facing differs from current         | (0.0, 0.0, 1.0)        | computed_north_matrix     |
  | New facing identical to current (no-op) | (1.0, 0.0, 0.0)        | skip_no_write             |

  Then when the new facing is identical to the current *character facing vector* no write is issued

---

### Story: Read Camera Position from Memory

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached

Scenario Outline: Read camera position for camera-relative commands
  Given the **Camera Rig** has *active state* {active_state}
  And the **Memory Pointer** for camera position has *validation state* {validation_state}
  When the GM triggers a camera-relative movement command
  Then the **Memory Interface** returns *camera position* {camera_position} as shown below

  Camera Rig (Given):
  | scenario                         | active_state |
  | Rig active — normal read         | active       |
  | Rig inactive — raw coords used   | inactive     |

  Memory Interface (Then):
  | scenario                         | camera_position         |
  | Rig active — normal read         | (50.0, 10.0, -200.0)   |
  | Rig inactive — raw coords used   | (50.0, 10.0, -200.0)   |
  | Stale pointer — refresh first    | (50.0, 10.0, -200.0)   |

  Memory Pointer (Given):
  | scenario                         | validation_state |
  | Rig active — normal read         | valid            |
  | Rig inactive — raw coords used   | valid            |
  | Stale pointer — refresh first    | stale            |

  Then when *active state* is "inactive" the GM sees a warning that the **Camera Rig** is not active but the read still proceeds
  And when *validation state* is "stale" the read is blocked until the pointer is refreshed

---

## Character Movement Authoring

---

### Story: Add Movement to Character

**Covers AC:** 1, 2, 3, 4

Background:
  Given a **Character** is selected in the crowd tree

Scenario Outline: Add a new character movement
  When the GM adds a movement with *movement name* {movement_name}
  Then the **Character Movement** is created as shown below

  Character Movement (When/Then):
  | scenario                       | movement_name   | movement_type |
  | New movement added             | Sprint          | Walk          |
  | Duplicate name rejected        | Sprint          | rejected      |
  | No character selected          | N/A             | disabled      |

  Then when *movement name* "Sprint" is unique a new **Character Movement** appears in the movement list with *movement type* Walk as default
  And when *movement name* "Sprint" already exists the add is rejected with a name-collision message
  And when no **Character** is selected the Add action is disabled

---

### Story: Edit Movement Parameters

**Covers AC:** 1, 2, 3, 4, 5

Background:
  Given a **Character Movement** *Sprint* exists on the character

Scenario Outline: Edit movement in the movement editor
  When the GM edits the **Character Movement** and saves with values shown below
  Then the **Character Movement** is updated as shown below

  Character Movement (When/Then):
  | scenario                        | movement_name | movement_type | distance_limit | movement_activation_key |
  | Change movement type to Fly     | Sprint        | Fly           | 100            | F               |
  | Set distance limit              | Sprint        | Walk          | 50             | unset           |
  | Cancel without saving           | Sprint        | Walk          | absent         | unset           |
  | Save with empty name — rejected | (empty)       | Walk          | absent         | unset           |

  Then when *movement type* changes to "Fly" the **Movement Animation** on next execution matches the updated type
  And when the GM cancels the editor without saving the **Character Movement** retains its previous *movement parameters*
  And when the GM saves with an empty name field the save is rejected with a validation message

---

### Story: Remove Movement from Character

**Covers AC:** 1, 2, 3

Background:
  Given a **Character** has **Character Movements** in its Movement Option Group

Scenario Outline: Remove a movement from the character
  Given the **Movement Option Group** has *default movement* designation {default_movement_designation}
  When the GM removes the **Character Movement** {movement_name}
  Then the **Character Movement** is deleted as shown below

  Character Movement (Given/When):
  | scenario                              | movement_name | movement_option_group_default | movement_activation_key |
  | Remove non-default movement           | Sprint        | unset                        | S                       |
  | Remove the default movement           | Walk          | default                      | W                       |
  | No movement selected                  | N/A           | N/A                          | N/A                     |

  Then when *movement name* "Sprint" is removed the *movement activation key* "S" is freed
  And when the removed **Character Movement** was the *default movement designation* no default remains on the character
  And when no **Character Movement** is selected the Remove action is disabled

---

### Story: Set Default Movement

**Covers AC:** 1, 2, 3

Background:
  Given a **Character** has two **Character Movements**: *Walk* and *Sprint*

Scenario Outline: Set default movement designation
  When the GM sets **Character Movement** {movement_name} as the default
  Then the **Character Movement** designations update as shown below

  Character Movement (Then):
  | scenario                              | movement_name | movement_option_group_default |
  | Set Sprint as default                 | Sprint        | default                      |
  | Previous default Walk cleared         | Walk          | unset                        |
  | Remove default without replacement    | Walk          | unset                        |

  Then at no moment do two **Character Movements** carry the *default movement designation* simultaneously

---

### Story: Set Movement Activation Key

**Covers AC:** 1, 2, 3

Background:
  Given a **Character** has **Character Movements** in its Movement Option Group

Scenario Outline: Assign activation key to a movement
  When the GM assigns *movement activation key* {movement_activation_key} to **Character Movement** {movement_name}
  Then the **Character Movement** is updated as shown below

  Character Movement (When/Then):
  | scenario                              | movement_name | movement_activation_key |
  | Assign key F to Sprint                | Sprint        | F                       |
  | Key F already used — rejected         | Run           | F                       |
  | Clear activation key                  | Sprint        | unset                   |

  Then when *movement activation key* "F" is assigned the **Keyboard Hook** begins routing presses of that key to dispatch the **Character Movement**
  And when *movement activation key* "F" is already used by another **Character Movement** the assignment is rejected with a conflict message
  And when *movement activation key* is cleared the movement is no longer dispatchable via the **Keyboard Hook** but remains accessible from the movement list

---

### Story: Add Default Movements to Character (Walk, Run, Swim)

**Covers AC:** 1, 2, 3

Background:
  Given a **Character** is selected in the crowd tree

Scenario Outline: Add the three default movements
  When the GM invokes Add Default Movements
  Then **Character Movements** are created as shown below

  Character Movement (Then):
  | scenario                              | movement_name | movement_type | movement_option_group_default |
  | Empty option group — all three added  | Walk          | Walk          | default                      |
  | Empty option group — all three added  | Run           | Run           | unset                        |
  | Empty option group — all three added  | Swim          | Swim          | unset                        |
  | Walk exists — only Run and Swim added | Run           | Run           | unset                        |
  | Walk exists — only Run and Swim added | Swim          | Swim          | unset                        |
  | All three exist — none added          | N/A           | N/A           | N/A                          |

  Then when existing names contain conflicts only non-conflicting movements are added with a message indicating which were skipped

---

## Movement Execution

---

### Story: Execute Move NPC Command

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached

Scenario Outline: Issue move NPC command to the game bridge
  Given the **Target Registration** has *registration state* {registration_state}
  When **Movement Execution** computes a valid destination
  Then the **Move NPC Command** is issued as shown below

  Move NPC Command (Then):
  | scenario                              | target_NPC_name    | destination_coordinates   |
  | Registered target — command issued    | Guard_Captain_01   | (200.0, 0.0, -150.0)     |
  | Name has no matching NPC — no-op      | NonExistent_NPC    | (200.0, 0.0, -150.0)     |

  Target Registration (Given):
  | scenario                              | registration_state |
  | Registered target — command issued    | confirmed          |
  | Unregistered — command held           | pending            |
  | Name has no matching NPC — no-op      | confirmed          |

  Then when *registration state* is "pending" the command is held until registration succeeds
  And when *target NPC name* has no matching **Spawned NPC** the COH engine produces a no-op and the application shows a "character not found" indicator

---

### Story: Move Character to Location

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Move character step by step to destination
  Given a **Character Movement** with *distance limit* {distance_limit} is active as shown in the Given table below
  When the GM triggers Move to Location
  Then **Movement Execution** issues steps and halts as shown below

  Movement Distance Count (Given):
  | scenario                                | cumulative_distance_traveled |
  | Destination reached before limit        | 0                            |
  | Distance limit reached before dest      | 0                            |
  | Floor collision halts vertical          | 0                            |
  | Wall collision halts horizontal         | 0                            |

  Movement Distance Count (Then):
  | scenario                                | cumulative_distance_traveled |
  | Destination reached before limit        | 35                           |
  | Distance limit reached before dest      | 50                           |
  | Floor collision halts vertical          | 20                           |
  | Wall collision halts horizontal         | 15                           |

  Character Movement (Given):
  | scenario                                | distance_limit |
  | Destination reached before limit        | 100            |
  | Distance limit reached before dest      | 50             |
  | Floor collision halts vertical          | 100            |
  | Wall collision halts horizontal         | 100            |

  Then when the destination is reached the step loop stops and the *cumulative distance traveled* resets to zero
  And when *cumulative distance traveled* reaches the *distance limit* **Movement Execution** halts with a limit-reached indicator; the final step is clamped
  And when a **Floor Collision** is detected vertical movement stops at the contact point
  And when a **Wall Collision** is detected movement in the blocked direction halts

---

### Story: Move Character to Camera Position

**Covers AC:** 1, 2

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Move character toward camera position
  Given the **Camera Rig** has *active state* {active_state}
  When the GM triggers Move to Camera Position
  Then the **Memory Interface** reads *camera position* and **Movement Execution** drives the **Spawned NPC** step by step toward that position

  Camera Rig (Given):
  | scenario                                | active_state |
  | Camera rig active — normal move         | active       |
  | Camera rig inactive — raw coords used   | inactive     |

  Then the same *distance limit*, **Floor Collision**, and **Wall Collision** rules apply
  And when *active state* is "inactive" the GM sees a notice but movement proceeds using raw camera coordinates

---

### Story: Teleport Character to Camera

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached

Scenario Outline: Teleport character to camera position instantly
  Given the **Target Registration** has *registration state* {registration_state}
  And the **Memory Pointer** for camera position has *validation state* {validation_state}
  When the GM triggers Teleport to Camera
  Then the **Memory Interface** writes *character position* directly to the *camera position* value as shown below

  Target Registration (Given):
  | scenario                              | registration_state |
  | Registered — instant teleport         | confirmed          |
  | Unregistered — teleport blocked       | pending            |

  Memory Pointer (Given):
  | scenario                              | validation_state |
  | Registered — instant teleport         | valid            |
  | Stale pointer — refresh then teleport | stale            |

  Memory Interface (Then):
  | scenario                              | character_position        | camera_position          |
  | Registered — instant teleport         | (50.0, 10.0, -200.0)     | (50.0, 10.0, -200.0)    |

  Then no **Movement Animation** plays during teleport
  And when *registration state* is "pending" the teleport is blocked until registration succeeds
  And when *validation state* is "stale" the teleport is held until the pointer is refreshed

---

### Story: Animate Walk/Run/Swim/Fly/Jump Movement

**Covers AC:** 1, 2, 3, 4

Background:
  Given a **Character Movement** is active on a **Spawned NPC**

Scenario Outline: Play movement animation matching movement type
  Given the **Character Movement** has *movement type* {movement_type}
  When **Movement Execution** begins or halts the movement
  Then the **Movement Animation** has *active animation cycle* {active_animation_cycle} as shown below

  Character Movement (Given):
  | scenario                        | movement_type |
  | Walk movement begins            | Walk          |
  | Run movement begins             | Run           |
  | Swim movement begins            | Swim          |
  | Fly movement begins             | Fly           |
  | Jump movement begins            | Jump          |
  | Movement halts                  | Walk          |

  Movement Animation (Then):
  | scenario                        | active_animation_cycle |
  | Walk movement begins            | walk                   |
  | Run movement begins             | run                    |
  | Swim movement begins            | swim                   |
  | Fly movement begins             | fly                    |
  | Jump movement begins            | jump                   |
  | Movement halts                  | stopped                |

  Then the *active animation cycle* must match the *movement type* of the active **Character Movement**
  And when **Movement Execution** halts the animation stops and the **Spawned NPC** returns to idle pose

Scenario: Levitating movement skips floor collision detection
  Given the **Character Movement** has *movement type* Fly (levitate = true)
  And the **Memory Interface** is attached and **Target Registration** is confirmed
  And floor collision would occur at the destination
  When **Movement Execution** computes the next step
  Then floor collision is not detected and the step proceeds
  And the same behaviour applies for Jump and Swim movement types (levitate = true)

Scenario: Ground-tethered movement applies floor collision detection
  Given the **Character Movement** has *movement type* Walk (levitate = false)
  And floor collision would occur at the destination
  When **Movement Execution** computes the next step
  Then floor collision is detected and the **Spawned NPC** is anchored at the contact point
  And the same behaviour applies for Run movement type (levitate = false)

---

### Story: Track Movement Distance Count

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Track cumulative distance per activation
  Given the **Character Movement** has *distance limit* {distance_limit}
  When a movement activation begins and steps are issued
  Then the **Movement Distance Count** tracks as shown below

  Movement Distance Count (Then):
  | scenario                                | cumulative_distance_traveled |
  | Activation begins — reset to zero       | 0                            |
  | After steps — reaches limit             | 50                           |
  | No limit — distance tracked but no halt | 75                           |

  Character Movement (Given):
  | scenario                                | distance_limit |
  | Activation begins — reset to zero       | 50             |
  | After steps — reaches limit             | 50             |
  | No limit — distance tracked but no halt | absent         |

  Then when *cumulative distance traveled* reaches the *distance limit* **Movement Execution** halts
  And when *distance limit* is "absent" the count is tracked but no halting threshold applies

---

### Story: Enforce Distance Limit per Movement Type

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Each movement enforces its own distance limit
  Given the **Character Movement** has *movement name* {movement_name} and *distance limit* {distance_limit}
  When the **Movement Distance Count** *cumulative distance traveled* reaches the limit
  Then **Movement Execution** halts and the final step is clamped

  Character Movement (Given):
  | scenario                              | movement_name | movement_type | distance_limit |
  | Walk limited to 50                    | Walk          | Walk          | 50             |
  | Run limited to 100                    | Run           | Run           | 100            |
  | Limit changed mid-session             | Sprint        | Run           | 75             |

  Then each **Character Movement** enforces only its own *distance limit* independently
  And when a *distance limit* is changed in the editor the new limit applies on the next activation

---

### Story: Detect Floor and Wall Collisions

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed
  And a **Character Movement** execution is in progress

Scenario Outline: Collision detection on each step
  When **Movement Execution** computes the next movement step
  Then collisions are detected as shown below

  Floor Collision (Then):
  | scenario                               |
  | No floor collision — step proceeds     |
  | Floor collision detected — anchor      |
  | Both floor and wall on same step       |

  Wall Collision (Then):
  | scenario                               |
  | No wall collision — step proceeds      |
  | Wall collision detected — halt         |
  | Both floor and wall on same step       |

  Then when no collision is detected the **Move NPC Command** is issued for the step
  And when a **Floor Collision** is detected vertical movement stops at the contact point and the **Spawned NPC** is anchored there
  And when a **Wall Collision** is detected movement in the blocked direction halts at the wall boundary
  And when both are detected on the same step the **Spawned NPC** stops at the combined boundary

---

### Story: Turn Character towards Target

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Turn spawned NPC to face a target
  Given the **Memory Interface** has *character facing vector* {character_facing_vector} as shown below
  When the GM triggers Turn to Target
  Then the **Memory Interface** writes *character rotation matrix* {character_rotation_matrix} as shown below

  Memory Interface (Given/Then):
  | scenario                                 | character_facing_vector | character_rotation_matrix   |
  | Turn to NPC target                       | (0.0, 0.0, 1.0)        | computed_bearing_matrix     |
  | Already facing target (no-op)            | (1.0, 0.0, 0.0)        | skip_no_write               |
  | Turn to location point                   | (0.0, 0.0, 1.0)        | computed_location_matrix    |

  Then when the **Spawned NPC** is already facing the target within tolerance no rotation write is issued
  And when the target is a location point rather than a **Spawned NPC** the bearing is computed from the character's *character position* to the target point

---

### Story: Reset Character Orientation

**Covers AC:** 1, 2, 3

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Reset character to default forward-facing orientation
  Given the **Memory Pointer** for character rotation matrix has *validation state* {validation_state}
  When the GM triggers Reset Character Orientation
  Then the **Memory Interface** writes the identity-equivalent *character rotation matrix*

  Memory Pointer (Given):
  | scenario                              | validation_state |
  | Valid pointer — normal reset          | valid            |
  | Already in default orientation        | valid            |
  | Stale pointer — refresh first         | stale            |

  Then the **Spawned NPC** faces the default forward direction in the COH game world
  And the operation is idempotent; issuing it while already in default orientation produces no visible change
  And when *validation state* is "stale" the write is blocked until the pointer is refreshed

---

## Camera Rig

---

### Story: Deploy Camera Enable and Disable Scripts

**Covers AC:** 1, 2, 3

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Deploy enable or disable script to the game session
  When the GM activates or deactivates the **Camera Rig**
  Then the **Camera Enable/Disable Script** has *deployed state* {deployed_state} as shown below

  Camera Enable/Disable Script (Then):
  | scenario                             | deployed_state |
  | Enable script deployed — rig active  | deployed       |
  | Disable script deployed — rig removed| deployed       |
  | Enable on already-active rig (no-op) | deployed       |

  Camera Rig (Then):
  | scenario                             | active_state |
  | Enable script deployed — rig active  | active       |
  | Disable script deployed — rig removed| inactive     |
  | Enable on already-active rig (no-op) | active       |

  Then when the enable variant is deployed the **Camera Rig** appears in the game world and camera-relative commands become available
  And when the disable variant is deployed any active **Camera Follow** is terminated and the rig object is removed
  And deploying enable on an already-active rig causes no duplicate camera objects

---

### Story: Render Camera Rig in Game

**Covers AC:** 1, 2, 3

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Camera rig rendering in COH game world
  Given the **Camera Rig** has *active state* {active_state}
  When a camera-relative movement command is attempted
  Then the command proceeds or is blocked as shown below

  Camera Rig (Given):
  | scenario                                | active_state |
  | Rig rendered — command proceeds         | active       |
  | Rig not rendered — command blocked      | inactive     |

  Then when *active state* is "active" the **Memory Interface** resolves and reads *camera position* from the rig's process memory location
  And when *active state* is "inactive" camera-relative commands are blocked with a message to activate the **Camera Rig** first
  But non-camera movement commands (move to fixed location) are unaffected

---

### Story: Execute Follow Command

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Game Bridge** is initialized

Scenario Outline: Activate camera follow on a spawned NPC
  Given the **Camera Rig** has *active state* {active_state}
  And the **Camera Follow** has *followed character* as shown in the Given table below
  When the GM triggers Follow on a **Spawned NPC**
  Then the **Camera Follow** updates *follow state* and *followed character* as shown below

  Camera Follow (Given):
  | scenario                                 | follow_state | followed_character |
  | Follow on new target                     | inactive     | none               |
  | Switch follow to second character        | active       | Guard_Captain_01   |
  | Followed NPC despawned — auto-detach     | active       | Guard_Captain_01   |

  Camera Follow (Then):
  | scenario                                 | follow_state | followed_character |
  | Follow on new target                     | active       | Guard_Captain_01   |
  | Switch follow to second character        | active       | Villain_Boss_03    |
  | Followed NPC despawned — auto-detach     | inactive     | none               |

  Camera Rig (Given):
  | scenario                                 | active_state |
  | Follow on new target                     | active       |
  | Switch follow to second character        | active       |
  | Followed NPC despawned — auto-detach     | active       |
  | Rig not active — follow rejected         | inactive     |

  Then when *active state* is "inactive" the Follow command is rejected with a message
  And when follow switches to a second character the rig unfollows the first automatically
  And when the followed **Spawned NPC** is despawned **Camera Follow** is terminated and **Camera Detach** is issued

---

### Story: Execute Camera Detach Command

**Covers AC:** 1, 2, 3

Background:
  Given the **Camera Rig** has *active state* "active"

Scenario Outline: Detach camera from followed character
  Given the **Camera Follow** has *follow state* {follow_state}
  And the **Maneuver-with-Camera Mode** has *active state* as shown in the Given table below
  When the GM triggers Camera Detach
  Then the **Camera Follow** and **Maneuver-with-Camera Mode** update as shown below

  Camera Follow (Given/Then):
  | scenario                              | follow_state | followed_character |
  | Follow active — detach terminates     | active       | none               |
  | No follow active — no-op             | inactive     | none               |
  | Maneuver mode also active — both end  | active       | none               |

  Maneuver-with-Camera Mode (Given/Then):
  | scenario                              | active_state |
  | Follow active — detach terminates     | inactive     |
  | No follow active — no-op             | inactive     |
  | Maneuver mode also active — both end  | inactive     |

  Then **Camera Detach** terminates **Camera Follow** and returns the **Camera Rig** to free-roam mode
  And when **Maneuver-with-Camera Mode** is active it is also terminated
  And when no follow is active the detach is a no-op with no error

---

### Story: Follow Character with Game Camera

**Covers AC:** 1, 2, 3

Background:
  Given the **Camera Rig** has *active state* "active"
  And the **Camera Follow** has *follow state* "active" on **Spawned NPC** *Guard_Captain_01*

Scenario Outline: Camera tracks character position continuously
  Given the **Memory Interface** has *character position* {character_position}
  When the **Spawned NPC** moves to a new position
  Then the **Camera Rig** updates to match the **Memory Interface** *character position* continuously

  Memory Interface (Given):
  | scenario                              | character_position          |
  | Character moves — camera tracks       | (300.0, 0.0, -50.0)        |
  | Movement command while follow active  | (350.0, 5.0, -75.0)        |

  Then the COH game camera remains focused on the character throughout movement
  And movement destination is not changed by follow mode; movement proceeds normally

---

### Story: Unfollow Character

**Covers AC:** 1, 2, 3

Background:
  Given the **Camera Rig** has *active state* "active"

Scenario Outline: Terminate camera follow mode
  Given the **Camera Follow** has *follow state* {follow_state} and *followed character* {followed_character}
  When the GM triggers Unfollow
  Then the **Camera Follow** has *follow state* "inactive" and *followed character* "none"

  Camera Follow (Given/Then):
  | scenario                              | follow_state | followed_character |
  | Active follow — terminated            | active       | Guard_Captain_01   |
  | No follow active — no-op             | inactive     | none               |

  Then the **Camera Rig** stops tracking and enters free-roam mode
  And subsequent Move to Camera Position reads the free-roam *camera position*

---

### Story: Activate Maneuver-with-Camera Mode

**Covers AC:** 1, 2, 3, 4

Background:
  Given the **Memory Interface** is attached and **Target Registration** is confirmed

Scenario Outline: Activate and use maneuver-with-camera mode
  Given the **Camera Rig** has *active state* {active_state}
  When the GM activates Maneuver-with-Camera Mode
  Then the **Maneuver-with-Camera Mode** has *active state* as shown below

  Camera Rig (Given):
  | scenario                                    | active_state |
  | Rig active — mode activated                 | active       |
  | Rig inactive — activation blocked           | inactive     |

  Maneuver-with-Camera Mode (Then):
  | scenario                                    | active_state |
  | Rig active — mode activated                 | active       |
  | Rig inactive — activation blocked           | inactive     |

  Then when *active state* of the **Camera Rig** is "active" subsequent movement commands use the **Camera Rig** facing direction as the movement bearing
  And when the GM rotates the camera the next movement step uses the updated camera facing
  And when *active state* of the **Camera Rig** is "inactive" the activation is blocked with a message
  And the same *distance limit*, **Floor Collision**, and **Wall Collision** rules apply during maneuver mode
