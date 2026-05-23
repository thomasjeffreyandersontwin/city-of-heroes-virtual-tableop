# Specification by Example — Increment 2: Character Identities

> Domain sources: `docs/increment-2/crc-increment-2.md` (CRC model), `docs/increment-2/acceptance-criteria-increment-2.md`, `docs/increment-2/ubiquitous-language-increment-2.md`.
> All table names are CRC class names. All column names are CRC responsibilities (properties). Scenario Outlines are the primary notation; plain Scenarios used only when flows are materially distinct.

---

## Game Bridge Initialization

---

### Story: Load HookCostume DLL from Game Directory

Background:
  Given a **Game Bridge** with *initialization state* *uninitialized*

#### Scenario Outline: DLL load outcome depends on file presence and validity

When the **Game Bridge** attempts to *load HookCostume DLL* from **COH Game Directory** {base_path}
Then the **HookCostume DLL** has *loaded state* {loaded_state}
And the **Game Bridge** has *initialization state* {initialization_state}

HookCostume DLL (Then):
| scenario                           | file_location                            | loaded_state |
| DLL present and valid              | C:\Games\CoH\bin\HookCostume.dll         | loaded       |
| DLL absent from directory          | (not found)                              | not loaded   |
| DLL present but wrong architecture | C:\Games\CoH\bin\HookCostume.dll         | not loaded   |

COH Game Directory (Given):
| scenario                           | base_path      |
| DLL present and valid              | C:\Games\CoH   |
| DLL absent from directory          | C:\Games\CoH   |
| DLL present but wrong architecture | C:\Games\CoH   |

Game Bridge (Then):
| scenario                           | initialization_state |
| DLL present and valid              | initializing         |
| DLL absent from directory          | uninitialized        |
| DLL present but wrong architecture | uninitialized        |

Then for *DLL present and valid*: the **Game Bridge** proceeds to call **InitGame** and the loaded DLL remains in the process for the session duration
Then for *DLL absent from directory*: the **Game Bridge** reports a fatal initialization error identifying the missing DLL and no further initialization steps are attempted
Then for *DLL present but wrong architecture*: the **Game Bridge** reports a fatal load failure with a descriptive error and does not attempt to call any DLL entry point

#### Scenario: Load deferred until COH Game Directory is validated

Given a **Game Bridge** with *initialization state* *uninitialized*
And the **COH Game Directory** *base path* is not yet validated
When the **Game Bridge** attempts to *load HookCostume DLL*
Then the **Game Bridge** defers the load until a valid *base path* is confirmed
And no load attempt is made against an unvalidated path

---

### Story: Initialize Game Bridge (InitGame)

Background:
  Given a **Game Bridge** with *initialization state* *initializing*
  And the **HookCostume DLL** has *loaded state* *loaded*

#### Scenario Outline: InitGame outcome depends on DLL state and prior calls

When the **Game Bridge** calls **InitGame** to *call DLL init entry point* via **Native Game Bridge**
Then the **Game Bridge** has *initialization state* {initialization_state}

Game Bridge (Then):
| scenario                           | initialization_state |
| DLL loaded, init succeeds          | polling              |
| DLL loaded, init returns failure   | uninitialized        |
| DLL not loaded before call         | uninitialized        |
| Duplicate call after ready reached | ready                |

Then for *DLL loaded, init succeeds*: the ready-poll loop starts and the session awaits the **Game Loaded Event** before allowing any **Game Command**
Then for *DLL loaded, init returns failure*: the **Game Bridge** reports **InitGame** failure and halts initialization; no polling loop started
Then for *DLL not loaded before call*: the **Game Bridge** rejects the call with an ordering error; no DLL entry point is invoked
Then for *Duplicate call after ready reached*: the duplicate call is ignored; no re-initialization of the DLL is performed

---

### Story: Poll until Game Client is Loaded

Background:
  Given a **Game Bridge** with *initialization state* *polling*

#### Scenario Outline: Polling outcome determines bridge state

When the **Game Bridge** *polls game state* via **Native Game Bridge**
Then the **Game Bridge** has *initialization state* {initialization_state}
And the **Game Loaded Event** has *publication state* {publication_state}

Game Bridge (Then):
| scenario                            | initialization_state |
| Poll returns game loaded            | ready                |
| Poll returns not ready              | polling              |
| Polling times out                   | polling              |
| Game command attempted while polling| polling              |
| Already ready, redundant not-ready  | ready                |

Game Loaded Event (Then):
| scenario                            | publication_state |
| Poll returns game loaded            | published         |
| Poll returns not ready              | unpublished       |
| Polling times out                   | unpublished       |
| Game command attempted while polling| unpublished       |
| Already ready, redundant not-ready  | published         |

Then for *Poll returns game loaded*: polling stops and the **Game Bridge** permits **Game Commands**
Then for *Poll returns not ready*: the **Game Bridge** waits and retries at the next interval
Then for *Polling times out*: the **Game Bridge** reports a timeout error to the application
Then for *Game command attempted while polling*: the command is held pending or rejected with a not-ready indicator
Then for *Already ready, redundant not-ready*: the **Game Loaded Event** is not re-published and the bridge remains in the ready state

---

### Story: Inject Required KeyBinds into Game

Background:
  Given the **Game Loaded Event** has *publication state* *published*
  And the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Keybind injection outcome

When the **Game Bridge** *injects required keybinds* writing a **KeyBind File** to **COH Game Directory** *C:\Games\CoH*
Then the **KeyBind File** has *file path* {file_path} with *keybind entries* {keybind_entries}

KeyBind File (Then):
| scenario                           | file_path                           | keybind_entries                    |
| Successful injection               | C:\Games\CoH\data\hvt_binds.txt    | required HVT bindings              |
| Re-injection in same session       | C:\Games\CoH\data\hvt_binds.txt    | refreshed HVT bindings             |

Then for *Successful injection*: the **Game Bridge** issues the `/bind_load_file` **Slash Command** and the game engine processes the bindings; the **Game Bridge** proceeds to costume pack extraction
Then for *Re-injection in same session*: the file is re-written and re-loaded, replacing previously loaded bindings

#### Scenario: Keybind file write fails

Given the **Game Loaded Event** has *publication state* *published*
When the **Game Bridge** attempts to write the **KeyBind File** and the directory is permission-denied
Then the **Game Bridge** reports a keybind injection failure
And no `/bind_load_file` command is issued against a partial or missing file

#### Scenario: Keybind file load command fails

Given the **KeyBind File** has been written to *file path* *C:\Games\CoH\data\hvt_binds.txt*
When the `/bind_load_file` **Slash Command** fails to execute
Then the **Game Bridge** reports that keybinds could not be loaded
And the issue is surfaced to the operator so injection can be retried

---

### Story: Extract Costume Pack on First Run

Background:
  Given the **Game Loaded Event** has *publication state* *published*
  And the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Costume pack extraction outcome

Given the **COH Costumes Directory** has *directory path* {directory_path}
When the **Game Bridge** *extracts costume pack* to the **COH Costumes Directory**
Then the **COH Costumes Directory** at *directory path* {directory_path} is available for costume operations

COH Costumes Directory (Given/Then):
| scenario                        | directory_path            |
| First run, directory exists     | C:\Games\CoH\costumes    |
| Not first run, files present    | C:\Games\CoH\costumes    |
| First run, directory missing    | C:\Games\CoH\costumes    |

Then for *First run, directory exists*: all packed **Costume Files** are written to the directory; the application records that the pack has been extracted
Then for *Not first run, files present*: the **Game Bridge** skips extraction and proceeds to the next step without modifying existing files
Then for *First run, directory missing*: the **Game Bridge** creates the directory and extracts the pack contents into it

#### Scenario: Extraction fails partway

Given the **COH Costumes Directory** has *directory path* *C:\Games\CoH\costumes*
When the **Game Bridge** *extracts costume pack* and the extraction fails (disk full)
Then the **Game Bridge** reports an extraction failure identifying the cause
And no partial pack is treated as complete — the first-run flag is not cleared

---

### Story: Publish Game Loaded Event

Background:
  Given a **Game Bridge** with *initialization state* *polling*

#### Scenario Outline: Game Loaded Event publication rules

When the **Game Bridge** polling loop confirms the COH client status
Then the **Game Loaded Event** has *publication state* {publication_state}

Game Loaded Event (Then):
| scenario                             | publication_state |
| First ready confirmation             | published         |
| Already published, second ready poll | published         |
| Late subscriber after publication    | published         |
| Polling timed out                    | unpublished       |

Then for *First ready confirmation*: all pending **Game Commands** are released for execution and post-initialization steps are triggered in sequence
Then for *Already published, second ready poll*: no second **Game Loaded Event** is published; the session remains in the ready state
Then for *Late subscriber after publication*: the late subscriber receives the event notification or the system signals that the game is already loaded
Then for *Polling timed out*: no post-initialization steps are triggered and the **Game Bridge** surfaces the timeout error

---

### Story: Initialize Native Game Bridge

Background:
  Given the **HookCostume DLL** has *loaded state* *loaded*

#### Scenario Outline: Native Game Bridge initialization

When the **Game Bridge** initializes the **Native Game Bridge** to bind to the **HookCostume DLL**
Then the **HookCostume DLL** remains with *loaded state* {loaded_state}

HookCostume DLL (Then):
| scenario                          | loaded_state |
| DLL loaded, binding succeeds      | loaded       |
| DLL not loaded before init        | not loaded   |
| Duplicate initialization          | loaded       |
| Session shutdown                  | loaded       |

Then for *DLL loaded, binding succeeds*: the **Native Game Bridge** can *execute slash command*, *call init entry point*, and *query game state* for the session
Then for *DLL not loaded before init*: initialization fails with a dependency-ordering error; no P/Invoke call is attempted
Then for *Duplicate initialization*: the second init is silently ignored; the already-active binding remains in use
Then for *Session shutdown*: the **Native Game Bridge** releases its DLL bindings; subsequent calls return a not-ready error

---

### Story: Execute Slash Command via DLL

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And the **Native Game Bridge** is initialized

#### Scenario Outline: Slash command execution

When a **Slash Command** with *command string* {command_string} is submitted for execution
Then the **Slash Command** is delivered via *delivery path* {delivery_path}

Slash Command (When/Then):
| scenario                          | command_string      | delivery_path                    |
| Valid command, bridge ready        | /spawnnpc Guard_01  | immediate via Native Game Bridge |
| Command before Game Loaded Event  | /spawnnpc Guard_01  | (rejected)                       |
| Null or empty command string      | (empty)             | (rejected)                       |
| Valid command, COH reports unknown | /invalidcmd         | immediate via Native Game Bridge |

Then for *Valid command, bridge ready*: the DLL delivers the command to the COH game engine which processes it; control returns without blocking
Then for *Command before Game Loaded Event*: rejected with not-ready indicator; no DLL call made
Then for *Null or empty command string*: the **Native Game Bridge** rejects the call; an argument error is reported
Then for *Valid command, COH reports unknown*: COH error is surfaced to the calling service and logged without crashing the bridge

---

## KeyBind Execution

---

### Story: Generate KeyBind File for Game Event

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: KeyBind file generation from Game Command

Given a **Game Command** with *command type* {command_type} and *slash command composition* {slash_command_composition} and *target name* {target_name}
When the **Game Bridge** *generates keybind file* for the **Game Command**
Then a **KeyBind File** is written with *file path* {file_path} and *keybind entries* {keybind_entries}

Game Command (Given):
| scenario                         | command_type  | target_name    | slash_command_composition                                     | delivery_method   |
| Single slash command             | spawn         | Guard_Captain  | /spawnnpc Guard_Captain Skull_Lt_01                           | via KeyBind File  |
| Chained commands (target+load)   | load costume  | Guard_Captain  | /target_name Guard_Captain$$loadcostume guard.costume         | via KeyBind File  |
| Empty command (invalid)          | (none)        | (none)         | (empty)                                                       | (rejected)        |

KeyBind File (Then):
| scenario                         | file_path                        | keybind_entries                                            |
| Single slash command             | C:\Games\CoH\data\hvt_cmd.txt   | F1 /spawnnpc Guard_Captain Skull_Lt_01                    |
| Chained commands (target+load)   | C:\Games\CoH\data\hvt_cmd.txt   | F1 /target_name Guard_Captain$$loadcostume guard.costume  |
| Empty command (invalid)          | (not written)                    | (none)                                                     |

Then for *Single slash command*: the file is valid for loading via `/bind_load_file`
Then for *Chained commands (target+load)*: all commands in the chain are embedded in the same **KeyBind** entry
Then for *Empty command (invalid)*: the **Game Bridge** rejects the request and does not write a **KeyBind File**

#### Scenario: Chain exceeding length limit is split

Given a **Game Command** with *slash command composition* exceeding COH's command-chain length limit
When the **Game Bridge** *generates keybind file*
Then the chain is split across multiple **KeyBind** entries in the **KeyBind File**
And the generated file executes the full command set when loaded

---

### Story: Execute Spawn NPC Command

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And the **Model List** has *loaded state* *loaded*

#### Scenario Outline: Spawn NPC command execution

Given a **Character** with *character name* {character_name}
And a **Model Identity** with *model name* {model_name}
When the **Game Bridge** executes a **Spawn NPC Command** with *model name payload* {model_name} for **Character** {character_name}
Then the **Spawned NPC** has *character name* {character_name} and *entity presence* {entity_presence}

Character (Given):
| scenario                          | character_name  |
| Valid model, bridge ready         | Guard_Captain   |
| Model not in list                 | Shadow_Knight   |
| Duplicate NPC name exists         | Guard_Captain   |
| Bridge not ready                  | Frost_Archer    |

Model Identity (Given):
| scenario                          | model_name       |
| Valid model, bridge ready         | Skull_Lt_01      |
| Model not in list                 | Invalid_Model_99 |
| Duplicate NPC name exists         | Skull_Lt_01      |
| Bridge not ready                  | Clockwork_Gear_01|

Spawned NPC (Then):
| scenario                          | character_name  | entity_presence |
| Valid model, bridge ready         | Guard_Captain   | present                |
| Model not in list                 | Shadow_Knight   | absent                 |
| Duplicate NPC name exists         | Guard_Captain   | present                |
| Bridge not ready                  | Frost_Archer    | absent                 |

Then for *Valid model, bridge ready*: the NPC appears at the camera position and the identity activation pipeline continues
Then for *Model not in list*: the **Game Bridge** surfaces the spawn failure; no **Spawned NPC** exists
Then for *Duplicate NPC name exists*: the **Game Bridge** logs a warning; the duplicate spawn proceeds per COH game rules
Then for *Bridge not ready*: the command is rejected with not-ready status; no **KeyBind File** is written

---

### Story: Execute Target by Name Command

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Target by name command execution

Given a **Spawned NPC** with *character name* {character_name} and *entity presence* {entity_presence}
When the **Game Bridge** executes a **Target by Name Command** with *target name payload* {target_name_payload}
Then the **Target by Name Command** *target name payload* {target_name_payload} resolves against the game world

Spawned NPC (Given):
| scenario                          | character_name  | entity_presence |
| NPC exists in game                | Guard_Captain   | present                |
| NPC does not exist                | Ghost_Entity    | absent                 |
| NPC despawned between commands    | Guard_Captain   | absent                 |

Target by Name Command (Then):
| scenario                          | target_name_payload |
| NPC exists in game                | Guard_Captain       |
| NPC does not exist                | Ghost_Entity        |
| NPC despawned between commands    | Guard_Captain       |

Then for *NPC exists in game*: COH sets the current target to **Spawned NPC** *Guard_Captain*; the **Load Costume Command** may safely follow
Then for *NPC does not exist*: COH sets no target; any subsequent **Load Costume Command** applies to an undefined target
Then for *NPC despawned between commands*: the **Game Bridge** surfaces the target failure so the calling service can abort or retry

#### Scenario: Target chained with Load Costume in same KeyBind File

Given a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*
When the **Target by Name Command** is chained before a **Load Costume Command** in the same **KeyBind File**
Then COH processes target first, then applies costume to the now-targeted **Spawned NPC** *Guard_Captain*

---

### Story: Execute Load Costume Command

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Load costume command execution

Given a **Spawned NPC** with *character name* {character_name} and *entity presence* *present* as current target
And a **Costume File** at *file path* {file_path}
When the **Game Bridge** executes a **Load Costume Command** with *costume file path payload* {costume_file_path_payload}
Then the **Spawned NPC** *character name* {character_name} has *entity presence* *present*

Costume File (Given):
| scenario                    | file_path                                |
| Valid costume, NPC targeted | C:\Games\CoH\costumes\guard.costume      |
| File does not exist         | C:\Games\CoH\costumes\missing.costume    |
| Ghost costume onto Ghost NPC| C:\Games\CoH\costumes\guard_ghost.costume|

Load Costume Command (Then):
| scenario                    | costume_file_path_payload                |
| Valid costume, NPC targeted | C:\Games\CoH\costumes\guard.costume      |
| File does not exist         | C:\Games\CoH\costumes\missing.costume    |
| Ghost costume onto Ghost NPC| C:\Games\CoH\costumes\guard_ghost.costume|

Then for *Valid costume, NPC targeted*: COH reads the **Costume File** and the NPC's appearance changes to match; the **Costume Identity** is marked as actively rendered
Then for *File does not exist*: COH ignores the command; the NPC's appearance is unchanged; the **Game Bridge** surfaces the missing file error
Then for *Ghost costume onto Ghost NPC*: the ghost appearance is loaded via the same pipeline; the **Ghost NPC** displays the ghost material treatment

#### Scenario: No NPC targeted when load costume issued

Given no **Spawned NPC** is currently targeted (the **Target by Name Command** failed)
When the **Game Bridge** executes a **Load Costume Command**
Then the costume applies to whatever COH considers the current target, or to nothing
And the **Game Bridge** treats the result as ambiguous and logs a warning

---

### Story: Execute Delete NPC Command

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Delete NPC command execution

Given a **Spawned NPC** with *character name* {character_name} and *entity presence* {entity_presence}
When the **Game Bridge** executes a **Delete NPC Command** with *target name payload* {target_name_payload}
Then the **Spawned NPC** *character name* {character_name} has *entity presence* as shown below

Spawned NPC (Given):
| scenario                       | character_name       | entity_presence |
| NPC exists                     | Guard_Captain        | present                |
| NPC does not exist (no-op)     | NonExistent_NPC      | absent                 |
| Ghost NPC removal              | Guard_Captain_Ghost  | present                |

Delete NPC Command (When):
| scenario                       | target_name_payload  |
| NPC exists                     | Guard_Captain        |
| NPC does not exist (no-op)     | NonExistent_NPC      |
| Ghost NPC removal              | Guard_Captain_Ghost  |

Spawned NPC (Then):
| scenario                       | character_name       | entity_presence |
| NPC exists                     | Guard_Captain        | absent                 |
| NPC does not exist (no-op)     | NonExistent_NPC      | absent                 |
| Ghost NPC removal              | Guard_Captain_Ghost  | absent                 |

Then for *NPC exists*: the NPC is removed from the game world and is no longer visible or targetable
Then for *NPC does not exist (no-op)*: COH silently ignores the command; no error reported
Then for *Ghost NPC removal*: the **Ghost NPC** is removed and the **Ghost Shadow** *active state* is set to *inactive*

#### Scenario: Delete command before Game Loaded Event

Given a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*
And the **Game Bridge** has *initialization state* *polling*
When the **Game Bridge** attempts a **Delete NPC Command**
Then the command is rejected or queued; no delete attempt is made against a game that has not finished loading

---

## Costume File Management

---

### Story: Store Costume Files in COH Costumes Directory

Background:
  Given a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Costume file write

Given a **Costume Identity** with *costume surface* {costume_surface}
When HVT writes the **Costume File** for the **Costume Identity**
Then the **Costume File** has *file path* {file_path} with *costume data* {costume_data}

Costume File (Then):
| scenario                    | file_path                                  | costume_data                      |
| Successful write            | C:\Games\CoH\costumes\guard.costume        | body shape, costume parts, colors |
| File already exists         | C:\Games\CoH\costumes\guard.costume        | updated body shape, parts, colors |

Costume Identity (Given):
| scenario                    | costume_surface                            |
| Successful write            | C:\Games\CoH\costumes\guard.costume        |
| File already exists         | C:\Games\CoH\costumes\guard.costume        |

Then for *Successful write*: the file is readable by COH via the **Load Costume Command**; the path is recorded as the *costume surface*
Then for *File already exists*: HVT overwrites it; the updated file is available immediately for the next **Load Costume Command**

#### Scenario: Directory missing — created before write

Given the **COH Costumes Directory** *directory path* *C:\Games\CoH\costumes* does not exist
When HVT writes a **Costume File** for a **Costume Identity**
Then the **COH Costumes Directory** is created and the file is written successfully

#### Scenario: Directory is read-only — write fails

Given the **COH Costumes Directory** *directory path* *C:\Games\CoH\costumes* is read-only
When HVT attempts to write a **Costume File**
Then HVT reports a file write error identifying the directory and the cause
And no partial or zero-byte file is left at the destination path

---

### Story: Create Original-Backup Costume Files

Background:
  Given a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Original backup creation rules

Given a **Costume File** at *file path* {file_path} for **Character** *Guard_Captain*
When HVT is about to modify the **Costume File** for the first time
Then the **Original-Backup Costume File** has *file path* {backup_file_path} and *backup naming convention* {backup_naming_convention} and *immutable source content* {immutable_source_content}

Costume File (Given):
| scenario                         | file_path                                  |
| First modification — backup made | C:\Games\CoH\costumes\guard.costume        |
| Backup already exists            | C:\Games\CoH\costumes\guard.costume        |
| Backup write fails               | C:\Games\CoH\costumes\guard.costume        |

Original-Backup Costume File (Then):
| scenario                         | file_path                                        | backup_naming_convention | immutable_source_content      |
| First modification — backup made | C:\Games\CoH\costumes\guard_original.costume     | guard_original.costume   | exact copy of unmodified file |
| Backup already exists            | C:\Games\CoH\costumes\guard_original.costume     | guard_original.costume   | original content preserved    |
| Backup write fails               | C:\Games\CoH\costumes\guard_original.costume     | guard_original.costume   | (not written)                 |

Then for *First modification — backup made*: the backup is created before any changes; the working file may then be modified
Then for *Backup already exists*: HVT does not overwrite it regardless of subsequent modifications
Then for *Backup write fails*: HVT halts the modification; the working **Costume File** is not modified if its backup cannot be secured

#### Scenario: No prior costume exists — backup skipped

Given no **Costume File** exists for **Character** *Guard_Captain* (new character, no prior costume)
When HVT would create the backup
Then HVT skips the backup step; no empty backup file is created
And the backup will be created the first time a real costume is written

---

### Story: Write Custom KeyBind Files to COH Data Directory

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: KeyBind file write to data directory

Given the **Game Bridge** has assembled **KeyBind** entries for a **Game Command**
When the **Game Bridge** writes the **KeyBind File**
Then the **KeyBind File** has *file path* {file_path} and *keybind entries* {keybind_entries}

KeyBind File (Then):
| scenario                    | file_path                          | keybind_entries                |
| Successful write            | C:\Games\CoH\data\hvt_cmd.txt     | F1 /spawnnpc Guard Skull_Lt_01 |
| File already exists         | C:\Games\CoH\data\hvt_cmd.txt     | F1 /target_name Guard          |

Then for *Successful write*: the file is fully written and closed before the load instruction is issued; immediately available for `/bind_load_file`
Then for *File already exists*: overwritten with current command's entries only

#### Scenario: Data directory does not exist

Given the **COH Game Directory** *base path* *C:\Games\CoH* does not have a data subdirectory
When the **Game Bridge** attempts to write the **KeyBind File**
Then the **Game Bridge** reports a directory not found error
And no partial **KeyBind File** is left in an incomplete state

#### Scenario: KeyBind file write fails

Given the **Game Bridge** has assembled **KeyBind** entries
When the write fails (permission denied, disk full)
Then the **Game Bridge** reports the write failure
And no load instruction is issued and the failed **Game Command** is not delivered to COH

---

### Story: Load KeyBind File into Game

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And the **Native Game Bridge** is initialized

#### Scenario Outline: KeyBind file load via bind_load_file

Given a **KeyBind File** at *file path* {file_path} with *keybind entries* {keybind_entries}
When the **Game Bridge** issues **Slash Command** with *command string* */bind_load_file {file_path}* via the **Native Game Bridge**
Then the **KeyBind File** at *file path* {file_path} is processed by COH

KeyBind File (Given):
| scenario                     | file_path                          | keybind_entries                                          |
| File exists, commands execute| C:\Games\CoH\data\hvt_cmd.txt     | F1 /spawnnpc Guard_Captain Skull_Lt_01                   |
| Chained commands in entry    | C:\Games\CoH\data\hvt_cmd.txt     | F1 /target_name Guard$$loadcostume guard.costume         |

Then for *File exists, commands execute*: COH loads the file and executes all **KeyBind** entries; effects are visible in the game world
Then for *Chained commands in entry*: COH executes all commands in the chain sequentially; each step completes before the next begins

#### Scenario: KeyBind file does not exist at load time

Given no **KeyBind File** exists at *file path* *C:\Games\CoH\data\missing.txt*
When the **Game Bridge** issues `/bind_load_file`
Then COH silently ignores the load instruction
And the **Game Bridge** surfaces the load failure to the calling service

#### Scenario: Load instruction when bridge not ready

Given the **Game Bridge** has *initialization state* *polling*
When the **Game Bridge** attempts to issue `/bind_load_file`
Then the load instruction is rejected; no `/bind_load_file` call is made against a game that has not reported loaded

---

## Identity Management

---

### Story: Add Identity to Character

Background:
  Given a **Character** with *character name* *Guard_Captain* in the *Identity Option Group*

#### Scenario Outline: Add identity validation

When the GM adds an **Identity** with *identity name* {identity_name} to **Character** *Guard_Captain*
Then the **Identity Option Group** holds *identity name* {identity_name} as *active identity* {active_designation} and *default identity* {default_designation}

Identity (Then):
| scenario                      | identity_name     | active_identity | default_identity |
| Unique name provided          | Knight_Armor      | inactive           | unset               |
| Duplicate name on character   | (rejected)        | (unchanged)        | (unchanged)         |
| Empty name provided           | (rejected)        | (unchanged)        | (unchanged)         |

Then for *Unique name provided*: the **Identity** is added and the *Identity List* displays the new entry; the **Character** data is updated so it persists
Then for *Duplicate name on character*: the application rejects the duplicate with an inline validation message; existing **Identities** unchanged
Then for *Empty name provided*: the application requires a name and displays a validation prompt; no unnamed **Identity** is created

#### Scenario: Add disabled when no character selected

Given no **Character** is selected in the *Crowd Tree*
When the GM attempts to add an **Identity**
Then the Add action is disabled in the *Identity List*
And no **Identity** is created

---

### Story: Set Identity Type (Model or Costume)

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Identity** with *identity name* *Knight_Armor* in the *Identity Option Group*

#### Scenario: Set type to Model

When the GM sets **Identity** *Knight_Armor* type to Model
Then the **Identity** is configured as a **Model Identity** with *model name* (awaiting assignment)
And any previously set *costume surface* is cleared
And the *Identity List* row shows type indicator *Model*

#### Scenario: Set type to Costume

When the GM sets **Identity** *Knight_Armor* type to Costume
Then the **Identity** is configured as a **Costume Identity** with *costume surface* (awaiting assignment)
And any previously set *model name* is cleared
And the *Identity List* row shows type indicator *Costume*

#### Scenario: Type change on active identity requires despawn confirmation

Given an **Identity** *Knight_Armor* with *active designation* *active*
And a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*
When the GM attempts to change type on **Identity** *Knight_Armor*
Then the application warns that changing type requires despawning the character
And if confirmed, the **Spawned NPC** is despawned before the type change is applied

#### Scenario: Type confirmed updates character data

Given an **Identity** *Knight_Armor* with *active designation* *inactive*
When the GM sets **Identity** *Knight_Armor* type and confirms
Then the **Character** data is updated immediately

---

### Story: Assign Costume Surface to Identity

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And a **Costume Identity** with *identity name* *Knight_Armor* and *costume surface* unassigned
  And a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Costume surface assignment

When the GM assigns *costume surface* {costume_surface} to **Costume Identity** *Knight_Armor*
Then the **Costume Identity** *Knight_Armor* has *costume surface* {costume_surface}

Costume Identity (Then):
| scenario                          | costume_surface                              |
| Valid file path                   | C:\Games\CoH\costumes\guard.costume          |
| File does not exist               | (unchanged — unassigned)                     |
| Surface cleared                   | (unassigned)                                 |

Then for *Valid file path*: surface saved; the identity can be activated and the **Load Costume Command** will use this path
Then for *File does not exist*: validation error shown; the invalid path is not saved
Then for *Surface cleared*: the **Costume Identity** is marked as missing its surface; activation is blocked

#### Scenario: Costume surface not available on Model Identity

Given a **Model Identity** with *identity name* *Dragon_Model*
When the GM attempts to assign a *costume surface* to **Model Identity** *Dragon_Model*
Then the *costume surface* field is not available for **Model Identities**
And no surface assignment is made

---

### Story: Set Default Identity

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And **Identity** *Knight_Armor* in the *Identity Option Group* with no *default identity* designation
  And **Identity** *Shadow_Form* in the *Identity Option Group* designated as *default identity*

#### Scenario Outline: Default designation management

When the GM sets *default designation* to *default* on **Identity** {identity_name}
Then the **Identity Option Group** has *default identity* {identity_name} with *default_designation* {default_designation}

Identity (Then):
| scenario                          | identity_name  | default_identity_state |
| Set new default on Knight_Armor   | Knight_Armor   | default             |

Then for *Set new default on Knight_Armor*: the previous default (**Identity** *Shadow_Form*) has *default designation* cleared to *unset*; exactly one **Identity** on the **Character** carries the default flag at a time; the *Identity List* shows the default marker only on *Knight_Armor*

#### Scenario: Clear default (set to none)

When the GM removes the *default designation* from **Identity** *Shadow_Form* without assigning another
Then **Identity** *Shadow_Form* has *default designation* *unset*
And no **Identity** on the **Character** carries the default flag
And the **Character** will not auto-activate any identity on spawn

#### Scenario: Set Default disabled when no identities exist

Given a **Character** with no **Identities** in the *Identity Option Group*
When the GM attempts to set a default
Then the Set Default action is disabled and no change is made

#### Scenario: Default persists across sessions

Given a **Character** *Guard_Captain* with **Identity** *Knight_Armor* as *default designation* *default*
When the session restarts
Then the *default designation* on **Identity** *Knight_Armor* persists

---

### Story: Set Active Identity

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Character** with *character name* *Guard_Captain*

#### Scenario Outline: Identity activation pipeline

Given a **Model Identity** with *identity name* {identity_name} and *model name* {model_name}
When the GM sets *active designation* on **Identity** {identity_name}
Then the **Spawned NPC** has *character name* *Guard_Captain* and *entity presence* {entity_presence}

Model Identity (Given):
| scenario                              | identity_name  | model_name      |
| Model Identity activated              | Dragon_Model   | Skull_Lt_01     |

Spawned NPC (Then):
| scenario                              | character_name  | entity_presence |
| Model Identity activated              | Guard_Captain   | present                |

Then for *Model Identity activated*: the **Game Bridge** issues the **Spawn NPC Command** with model name; the spawn animation plays; the active indicator is shown in the *Identity List*

#### Scenario: Costume Identity activated

Given a **Costume Identity** with *identity name* *Knight_Armor* and *costume surface* *C:\Games\CoH\costumes\guard.costume*
When the GM sets *active designation* on **Identity** *Knight_Armor*
Then the **Spawned NPC** has *character name* *Guard_Captain* and *entity presence* *present*
And the **Game Bridge** issues **Spawn NPC Command**, then **Target by Name Command**, then **Load Costume Command** in sequence
And the spawn animation plays and the active indicator is shown in the *Identity List*

#### Scenario: Switch from existing active identity

Given an **Identity** *Old_Look* with *active designation* *active* on **Character** *Guard_Captain*
And a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*
When the GM sets *active designation* on a new **Identity** *Dragon_Model*
Then the previous *active identity* in the **Identity Option Group** is cleared; persistent abilities are stopped; the old **Spawned NPC** is despawned
And the new identity's full activation sequence runs after the old NPC is removed

#### Scenario: Bridge not ready — activation blocked

Given the **Game Bridge** has *initialization state* *polling*
When the GM attempts to set *active designation* on an **Identity**
Then the Set Active action is blocked with a "game not connected" indicator
And no game commands are issued

#### Scenario: Costume Identity with no surface — activation blocked

Given a **Costume Identity** with *identity name* *Bare_Armor* and *costume surface* *(unassigned)*
When the GM attempts to set *active designation* on **Identity** *Bare_Armor*
Then the application blocks activation with a "no costume surface" validation message
And no **Spawn NPC Command** is issued

#### Scenario: Active indicator visible in UI after activation

Given a **Model Identity** *Dragon_Model* with *model name* *Skull_Lt_01*
When the GM sets *active designation* on **Identity** *Dragon_Model* and activation succeeds
Then the *active designation* indicator is visible on *Dragon_Model* in the *Identity List*
And the **Character** *Guard_Captain* node in the *Crowd Tree* shows spawned status

---

### Story: Remove Identity from Character

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Identity removal

Given an **Identity** with *identity name* {identity_name} where *active identity* in **Identity Option Group** is {active_designation} and *default identity* is {default_designation}
When the GM removes **Identity** {identity_name} from **Character** *Guard_Captain*
Then the **Identity** {identity_name} is no longer in the *Identity Option Group*

Identity (Given):
| scenario                              | identity_name  | active_identity | default_identity |
| Not active, not default               | Old_Armor      | inactive           | unset               |
| Currently active                      | Dragon_Model   | active             | unset               |
| Is default identity                   | Knight_Armor   | inactive           | default             |
| Last identity on character            | Solo_Look      | inactive           | unset               |
| Both active and default               | Dragon_Model   | active             | default             |

Then for *Not active, not default*: the *Identity List* no longer shows the entry
Then for *Currently active*: the **Spawned NPC** is despawned via **Delete NPC Command** before removal; **Character** marked as not spawned
Then for *Is default identity*: the default flag is cleared; no **Identity** carries the default marker after removal
Then for *Last identity on character*: the *Identity List* is empty; all identity-specific actions are disabled
Then for *Both active and default*: the **Spawned NPC** is despawned, both flags cleared — as a single atomic operation

---

## Identity Rendering

---

### Story: Load Costume File for Active Identity

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*

#### Scenario Outline: Costume load during activation

Given a **Costume Identity** *Knight_Armor* with *costume surface* {costume_surface}
When the **Game Bridge** *executes identity activation* for **Costume Identity** *Knight_Armor*
Then the **Target by Name Command** has *target name payload* *Guard_Captain*
And the **Load Costume Command** has *costume file path payload* {costume_surface}

Costume Identity (Given):
| scenario                          | costume_surface                              |
| Valid file, NPC targeted          | C:\Games\CoH\costumes\guard.costume          |
| File does not exist               | C:\Games\CoH\costumes\missing.costume        |

Load Costume Command (Then):
| scenario                          | costume_file_path_payload                    |
| Valid file, NPC targeted          | C:\Games\CoH\costumes\guard.costume          |
| File does not exist               | C:\Games\CoH\costumes\missing.costume        |

Then for *Valid file, NPC targeted*: COH applies the costume; the NPC's appearance changes; the active **Identity** in the **Identity Option Group** is marked as fully rendered
Then for *File does not exist*: COH ignores the command; the NPC retains base appearance; the **Game Bridge** reports a missing file error

#### Scenario: Target fails before load

Given a **Costume Identity** *Knight_Armor* with *costume surface* *C:\Games\CoH\costumes\guard.costume*
And the **Spawned NPC** *Guard_Captain* has been despawned between spawn and target steps
When the **Target by Name Command** fails to find the **Spawned NPC**
Then the **Game Bridge** logs the targeting failure and aborts the load step

#### Scenario: Costume load during identity switch replaces previous appearance

Given a **Costume Identity** *Knight_Armor* with *costume surface* *C:\Games\CoH\costumes\knight.costume*
When the **Game Bridge** loads the new costume as part of an identity switch
Then the previous costume or model appearance is replaced
And the visual change is visible in the game world immediately

---

### Story: Spawn Character with Model Identity

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And the **Model List** has *loaded state* *loaded* with *available models* including *Skull_Lt_01* and *Clockwork_Gear_01*

#### Scenario Outline: Model identity spawn

Given a **Character** with *character name* {character_name}
And a **Model Identity** with *model name* {model_name}
When the GM activates the **Model Identity** on the **Character**
Then the **Spawned NPC** has *character name* {character_name} and *entity presence* {entity_presence}

Character (Given):
| scenario                         | character_name  |
| Valid model, bridge ready        | Guard_Captain   |
| Model not in loaded list         | Shadow_Knight   |
| NPC name already exists in game  | Guard_Captain   |

Model Identity (Given):
| scenario                         | model_name         |
| Valid model, bridge ready        | Skull_Lt_01        |
| Model not in loaded list         | Invalid_Model_99   |
| NPC name already exists in game  | Skull_Lt_01        |

Spawned NPC (Then):
| scenario                         | character_name  | entity_presence |
| Valid model, bridge ready        | Guard_Captain   | present                |
| Model not in loaded list         | Shadow_Knight   | absent                 |
| NPC name already exists in game  | Guard_Captain   | present                |

Then for *Valid model, bridge ready*: the NPC is visible at the camera position; the **Character** is marked as spawned; the spawn animation plays
Then for *Model not in loaded list*: activation is blocked with a "model not found" indicator; no **Spawn NPC Command** is issued
Then for *NPC name already exists in game*: the **Game Bridge** first issues the **Delete NPC Command** for the existing NPC, then re-spawns

#### Scenario: Bridge not ready — activation blocked

Given the **Game Bridge** has *initialization state* *polling*
And a **Model Identity** *Dragon_Model* with *model name* *Clockwork_Gear_01*
When the GM activates the **Model Identity**
Then activation is blocked with a "game not connected" indicator; no game command is issued

---

### Story: Switch Active Identity on Spawned Character

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Character** with *character name* *Guard_Captain*
  And an **Identity** with *active designation* *active* (current)
  And a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*

#### Scenario Outline: Identity switch — new identity type determines sequence

Given a new **Identity** with *identity name* {identity_name}
When the GM sets *active designation* on **Identity** {identity_name} replacing the current active
Then the **Spawned NPC** has *character name* *Guard_Captain* and *entity presence* *present*

Identity (Given):
| scenario                           | identity_name  |
| Switch to Model Identity           | Dragon_Model   |
| Switch to Costume Identity         | Knight_Armor   |

Then for *Switch to Model Identity*: persistent abilities are stopped → **Delete NPC Command** removes old NPC → **Spawn NPC Command** issued with new model name; new NPC at camera
Then for *Switch to Costume Identity*: persistent abilities are stopped → **Delete NPC Command** removes old NPC → Spawn + Target + Load Costume for new identity

#### Scenario: Delete old NPC fails (already gone)

Given the old **Spawned NPC** has already been removed from the game world
When the **Game Bridge** issues the **Delete NPC Command** during the switch
Then the delete is a no-op and the switch continues with the new identity activation

#### Scenario: Switch completes — UI indicators updated

When the identity switch completes
Then the *Identity List* shows *active designation* only on the new **Identity**
And the **Character** *Guard_Captain* remains marked as spawned in the *Crowd Tree*
And the spawn animation plays on the new **Spawned NPC**

---

### Story: Play Animation on Identity Load

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Spawned NPC** with *character name* *Guard_Captain* and *entity presence* *present*

#### Scenario: Animation plays after activation completes

Given an **Identity** activation sequence has completed (NPC spawned and costume loaded if applicable)
When the **Game Bridge** issues the spawn animation command
Then the animation plays on the **Spawned NPC** *Guard_Captain* and is visible in the game world

#### Scenario: No animation configured

Given no spawn animation is configured for the **Identity** type
When the identity load completes
Then the **Spawned NPC** is rendered at rest without error

#### Scenario: Animation waits for NPC presence

Given the animation command would be issued before the **Spawned NPC** is confirmed *present*
When the **Game Bridge** detects the NPC is not yet registered
Then the **Game Bridge** waits for the NPC to register before issuing the animation

#### Scenario: Animation during identity switch

Given an identity switch has completed and the new **Spawned NPC** is *present*
When the animation is triggered
Then the animation plays on the new **Spawned NPC** only
And no animation plays on the already-removed old NPC

#### Scenario: Animation command fails

Given the **Spawned NPC** *Guard_Captain* is *present*
When the animation command fails (game does not acknowledge)
Then the **Game Bridge** logs the failure
And the **Identity** is still marked as *active designation* *active* — the animation failure does not undo the spawn

---

### Story: Stop Persistent Abilities on Identity Switch

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Character** with *character name* *Guard_Captain*
  And an **Identity** with *active designation* *active* on **Character** *Guard_Captain*

#### Scenario: Active persistent abilities stopped before despawn

Given **Character** *Guard_Captain* has one or more active persistent abilities
When the GM initiates an identity switch on **Character** *Guard_Captain*
Then all persistent abilities on that **Character** are stopped before the old *active designation* is cleared
And the stop indicators are updated; the switch continues to despawn the old **Spawned NPC**

#### Scenario: No active persistent abilities — step skipped

Given **Character** *Guard_Captain* has no active persistent abilities
When the GM initiates an identity switch
Then the stop step is skipped without error
And the identity switch proceeds directly to despawning the old **Spawned NPC**

#### Scenario: Persistent ability stop fails — switch continues

Given **Character** *Guard_Captain* has active persistent abilities
When a persistent ability fails to stop during the switch (command not acknowledged)
Then the **Game Bridge** logs the failure and continues the switch
And the identity switch is not blocked by a failed ability stop

#### Scenario: Stopped abilities remain stopped after switch

Given the identity switch has completed
Then previously stopped persistent abilities remain in their stopped state
And the GM may manually reactivate them on the new identity if desired

---

## Ghost Shadows

---

### Story: Superimpose Ghost on Model Character

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Character** with *character name* *Guard_Captain*

#### Scenario Outline: Ghost shadow activation

Given a **Model Identity** *Dragon_Model* with *model name* {model_name} and *active designation* *active*
And an **Original-Backup Costume File** at *file path* {backup_file_path}
When the GM chooses Add Ghost on **Character** *Guard_Captain*
Then the **Ghost Shadow** has *associated character* *Guard_Captain* and *active state* {active_state}

Original-Backup Costume File (Given):
| scenario                          | file_path                                        |
| Active model identity with backup | C:\Games\CoH\costumes\guard_original.costume     |
| Original backup missing           | (not found)                                      |

Ghost Shadow (Then):
| scenario                          | associated_character | active_state |
| Active model identity with backup | Guard_Captain        | active       |
| Original backup missing           | Guard_Captain        | inactive     |

Then for *Active model identity with backup*: **Ghost Costume File** is generated, **Ghost NPC** is spawned, ghost costume loaded, **Ghost Alignment** performed; ghost indicator shown in *Identity List*
Then for *Original backup missing*: "no original backup found" error reported; no partial **Ghost NPC** is spawned

#### Scenario: Ghost shadow on Costume Identity — disabled

Given a **Costume Identity** *Knight_Armor* with *active designation* *active* on **Character** *Guard_Captain*
When the GM attempts to add a ghost shadow
Then the Add Ghost action is disabled for **Costume Identity** characters; no ghost spawn attempted

#### Scenario: Ghost shadow on unspawned character — blocked

Given a **Model Identity** *Dragon_Model* with *active designation* *inactive* (character not spawned)
When the GM attempts to add a ghost shadow
Then the action is blocked with a "character not spawned" indicator; no **Ghost NPC** is created

#### Scenario: Ghost indicator shown after activation

Given a **Ghost Shadow** with *active state* *active* on **Character** *Guard_Captain*
Then the ghost indicator is shown on the **Model Identity** entry in the *Identity List*
And the **Ghost NPC** is visible in the game world overlaid on the **Spawned NPC** *Guard_Captain*

---

### Story: Create Ghost Costume File from Original

Background:
  Given a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Ghost costume file generation

Given an **Original-Backup Costume File** at *file path* {source_file_path} for **Character** *Guard_Captain*
When HVT generates the **Ghost Costume File** from the original backup
Then the **Ghost Costume File** has *ghost naming convention* {ghost_naming_convention} and *ghost material treatment* {ghost_material_treatment}

Original-Backup Costume File (Given):
| scenario                    | file_path                                        |
| Successful generation       | C:\Games\CoH\costumes\guard_original.costume     |
| Original does not exist     | (not found)                                      |
| Ghost file already exists   | C:\Games\CoH\costumes\guard_original.costume     |

Ghost Costume File (Then):
| scenario                    | ghost_naming_convention | ghost_material_treatment             |
| Successful generation       | guard_ghost.costume     | reduced-opacity on all costume parts |
| Original does not exist     | (none)                  | (none)                               |
| Ghost file already exists   | guard_ghost.costume     | reduced-opacity on all costume parts |

Then for *Successful generation*: written to **COH Costumes Directory**; available for loading onto the **Ghost NPC** via **Load Costume Command**
Then for *Original does not exist*: generation fails with "missing original backup" error; no incomplete file written
Then for *Ghost file already exists*: regenerated from the **Original-Backup Costume File**; overwritten with freshly derived version

#### Scenario: Ghost costume write fails

Given an **Original-Backup Costume File** at *file path* *C:\Games\CoH\costumes\guard_original.costume*
When the write to the **COH Costumes Directory** fails during ghost file creation
Then HVT reports the write error
And the **Original-Backup Costume File** is not modified

---

### Story: Align Ghost Position and Orientation with Character

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Ghost NPC** with *character name* *Guard_Captain_Ghost* and *entity presence* *present*

#### Scenario Outline: Ghost alignment execution

Given a **Spawned NPC** (primary) with *character name* *Guard_Captain* and *entity presence* {entity_presence}
When the **Game Bridge** *performs ghost alignment* via **Ghost Alignment**
Then the **Ghost NPC** has *aligned position and facing* {aligned_position_and_facing}

Spawned NPC (Given):
| scenario                           | character_name | entity_presence |
| Both NPCs present                  | Guard_Captain  | present                |
| Primary NPC not found              | Guard_Captain  | absent                 |

Ghost NPC (Then):
| scenario                           | character_name        | aligned_position_and_facing               |
| Both NPCs present                  | Guard_Captain_Ghost   | matches character position and facing     |
| Primary NPC not found              | Guard_Captain_Ghost   | unchanged — default spawn position        |

Then for *Both NPCs present*: the **Ghost NPC** occupies the same space and faces the same direction as *Guard_Captain*; the overlay is visible from the camera
Then for *Primary NPC not found*: the **Game Bridge** reports "character not found" and does not attempt to align

#### Scenario: Ghost NPC not found at alignment time

Given no **Ghost NPC** with *character name* *Guard_Captain_Ghost* is *present* in the game world
When the **Game Bridge** attempts **Ghost Alignment**
Then the **Game Bridge** reports "ghost NPC not found"; no write operation is attempted

#### Scenario: Character moves — re-alignment required

Given a **Ghost Shadow** with *active state* *active*
And the **Character** *Guard_Captain* moves after the **Ghost Shadow** was activated
When **Ghost Alignment** is not re-executed
Then positional drift occurs between the **Character** and the **Ghost NPC**
And the ghost overlay appears displaced from the character

---

### Story: Remove Ghost from Desktop

Background:
  Given the **Game Bridge** has *initialization state* *ready*
  And a **Ghost Shadow** with *active state* *active* and *associated character* *Guard_Captain*
  And a **Ghost NPC** with *character name* *Guard_Captain_Ghost* and *entity presence* *present*

#### Scenario Outline: Ghost removal

When the GM chooses Remove Ghost on **Character** *Guard_Captain*
Then the **Ghost NPC** has *character name* *Guard_Captain_Ghost* and *entity presence* {entity_presence}
And the **Ghost Shadow** has *active state* {active_state}

Ghost NPC (Then):
| scenario                          | character_name        | entity_presence |
| Normal removal                    | Guard_Captain_Ghost   | absent                 |
| Ghost NPC already gone            | Guard_Captain_Ghost   | absent                 |

Ghost Shadow (Then):
| scenario                          | associated_character | active_state |
| Normal removal                    | Guard_Captain        | inactive     |
| Ghost NPC already gone            | Guard_Captain        | inactive     |

Then for *Normal removal*: the **Delete NPC Command** is issued; the ghost indicator is cleared from the *Identity List*
Then for *Ghost NPC already gone*: the **Delete NPC Command** is a no-op in COH; the ghost indicator is still cleared for a clean UI state

#### Scenario: Clear character from desktop removes ghost too

When the **Character** *Guard_Captain* is cleared from the desktop
Then both the primary **Spawned NPC** and the **Ghost NPC** are despawned in the correct order
And neither NPC remains in the game world

#### Scenario: Bridge not ready — removal deferred

Given the **Game Bridge** has *initialization state* *polling*
When the GM attempts Remove Ghost
Then the ghost indicator remains in the *Identity List*
And the ghost indicator is not cleared until the **Delete NPC Command** can be confirmed as executed

#### Scenario: Add Ghost re-enabled after removal

Given the **Ghost Shadow** has *active state* *inactive* (just removed)
Then the Add Ghost action is re-enabled for the **Character**
And no ghost-related state remains on the **Character's** identity record

---

## Costume Variant Generation

---

### Story: Create Persistent-FX Costume Variants

Background:
  Given a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Persistent-FX variant generation

Given an **Original-Backup Costume File** at *file path* {source_file_path} for **Character** *Guard_Captain*
When HVT generates a **Persistent-FX Costume Variant** from the original backup
Then the **Persistent-FX Costume Variant** has *persistent FX layers* {persistent_FX_layers}

Original-Backup Costume File (Given):
| scenario                    | file_path                                        |
| Successful generation       | C:\Games\CoH\costumes\guard_original.costume     |
| Original does not exist     | (not found)                                      |
| Variant already exists      | C:\Games\CoH\costumes\guard_original.costume     |

Persistent-FX Costume Variant (Then):
| scenario                    | persistent_FX_layers              |
| Successful generation       | FX overlaid on source costume data|
| Original does not exist     | (none)                            |
| Variant already exists      | FX overlaid on source costume data|

Then for *Successful generation*: written to **COH Costumes Directory**; loadable via **Load Costume Command** when persistent abilities are active
Then for *Original does not exist*: generation fails with "missing original backup" error; no incomplete file written
Then for *Variant already exists*: overwritten with freshly derived version from the **Original-Backup Costume File**

#### Scenario: Variant write fails

Given an **Original-Backup Costume File** at *file path* *C:\Games\CoH\costumes\guard_original.costume*
When the variant write to **COH Costumes Directory** fails
Then the error is reported and the variant is not available
And the **Original-Backup Costume File** is not modified

---

### Story: Create Ghost Costume Files

Background:
  Given a **COH Costumes Directory** with *directory path* *C:\Games\CoH\costumes*

#### Scenario Outline: Ghost costume file creation (batch)

Given an **Original-Backup Costume File** at *file path* {source_file_path} for **Character** {character_name}
When HVT creates the **Ghost Costume File** for **Character** {character_name}
Then the **Ghost Costume File** has *ghost naming convention* {ghost_naming_convention} and *ghost material treatment* {ghost_material_treatment}

Original-Backup Costume File (Given):
| scenario                         | file_path                                        |
| Successful creation              | C:\Games\CoH\costumes\guard_original.costume     |
| Original missing                 | (not found)                                      |
| File already exists              | C:\Games\CoH\costumes\archer_original.costume    |

Character (Given):
| scenario                         | character_name   |
| Successful creation              | Guard_Captain    |
| Original missing                 | Shadow_Knight    |
| File already exists              | Frost_Archer     |

Ghost Costume File (Then):
| scenario                         | ghost_naming_convention | ghost_material_treatment     |
| Successful creation              | guard_ghost.costume     | reduced-opacity on all parts |
| Original missing                 | (none)                  | (none)                       |
| File already exists              | archer_ghost.costume    | reduced-opacity on all parts |

Then for *Successful creation*: written with ghost naming convention; loadable onto a **Ghost NPC** via **Load Costume Command**
Then for *Original missing*: creation fails with descriptive error; no partial ghost file left
Then for *File already exists*: overwritten with freshly derived version; available for next **Ghost Shadow** activation

#### Scenario: Multiple characters — separate ghost files

Given **Original-Backup Costume Files** for both *Guard_Captain* and *Frost_Archer*
When HVT creates **Ghost Costume Files** for each
Then each **Character's** **Ghost Costume File** is written with character-specific naming
And no two characters' ghost files overwrite each other

---

## Model Browser

---

### Story: Load Available Models from Models.txt

Background:
  Given the **Game Loaded Event** has *publication state* *published*
  And the **COH Game Directory** has *base path* *C:\Games\CoH*

#### Scenario Outline: Model list loading

Given **Models.txt** at *file location* {file_location} with *model name entries* {model_name_entries}
When HVT reads **Models.txt** to populate the **Model List**
Then the **Model List** has *loaded state* {loaded_state} with *available models* {available_models}

Models.txt (Given):
| scenario                    | file_location                  | model_name_entries                                      |
| File present and valid      | C:\Games\CoH\Models.txt        | Skull_Lt_01, Clockwork_Gear_01, Hellion_Thug_01        |
| File absent                 | (not found)                    | (none)                                                  |
| File has malformed lines    | C:\Games\CoH\Models.txt        | Skull_Lt_01, [INVALID], Clockwork_Gear_01              |
| File present but empty      | C:\Games\CoH\Models.txt        | (empty)                                                 |

Model List (Then):
| scenario                    | loaded_state | available_models                                |
| File present and valid      | loaded       | Skull_Lt_01, Clockwork_Gear_01, Hellion_Thug_01 |
| File absent                 | not loaded   | (empty)                                          |
| File has malformed lines    | loaded       | Skull_Lt_01, Clockwork_Gear_01                  |
| File present but empty      | loaded       | (empty collection)                               |

Then for *File present and valid*: all model names and type classifications available for **Model Identity** assignment and *Model Browser* display
Then for *File absent*: "Models.txt not found" fatal initialization error; *Model Browser* interactions and **Model Identity** assignments blocked
Then for *File has malformed lines*: valid entries loaded, unparseable lines skipped; count of skipped lines reported
Then for *File present but empty*: *Model Browser* displays "no models available" message

#### Scenario: Model list held for session duration

Given the **Model List** has *loaded state* *loaded*
Then the **Model List** is held in memory for the session
And HVT does not re-read **Models.txt** mid-session

---

### Story: Create Crowd from COH Model List

Background:
  Given the **Model List** has *loaded state* *loaded*
  And the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Crowd creation from model selection

Given the GM has selected **Models** with *archetype name* {archetype_name} in the *Model Browser*
When the GM chooses Create Crowd from Selection
Then a **Crowd** is created with *characters for identity assignment* containing generated **Characters**

Model (Given):
| scenario                       | archetype_name     | type_classification |
| Two models selected            | Skull_Lt_01        | villain group       |
| Two models selected (second)   | Clockwork_Gear_01  | villain group       |

Then for the selection: a new **Crowd** is added to the *Crowd Repository*; one **Character** per selected **Model**; each carries a **Model Identity** referencing its selected model name; the *Model Browser* closes and focus returns to the *Crowd Manager — Identities* screen

#### Scenario: No models selected — action disabled

Given no **Models** are selected in the *Model Browser*
When the GM looks at the Create Crowd from Selection action
Then the action is disabled and no **Crowd** or **Character** is created

#### Scenario: Crowd name conflicts with existing

Given the GM has selected **Models** and chosen Create Crowd
When the new **Crowd** name conflicts with an existing **Crowd**
Then HVT prompts the GM to supply a unique crowd name
And creation is held until a unique name is confirmed

#### Scenario: Cancel model browser without creating

Given the GM has selected **Models** in the *Model Browser*
When the GM cancels without confirming crowd creation
Then no **Crowd** or **Character** is created and the *Crowd Repository* is unchanged

---

### Story: Select Models to Include in Crowd

Background:
  Given the **Model List** has *loaded state* *loaded*
  And the *Model Browser* is open displaying the **Model List**

#### Scenario Outline: Model selection in browser

Given **Model** with *archetype name* {archetype_name} and *type classification* {type_classification} in the *Model Browser*
When the GM selects **Model** {archetype_name}
Then the **Model** {archetype_name} is marked as selected in the *Model Browser*

Model (Given/Then):
| scenario                    | archetype_name     | type_classification |
| Select a model              | Skull_Lt_01        | villain group       |
| Select another model        | Clockwork_Gear_01  | villain group       |

Then for each selection: the Create Crowd from Selection button becomes enabled

#### Scenario: Deselect removes from selection

Given **Model** *Skull_Lt_01* is selected in the *Model Browser*
When the GM deselects **Model** *Skull_Lt_01*
Then the selection indicator is cleared
And if no **Models** remain selected, the Create Crowd from Selection button is disabled

#### Scenario: Filter preserves selections

Given the GM has selected **Model** *Skull_Lt_01* in the *Model Browser*
When the GM enters a filter term that hides *Skull_Lt_01*
Then *Skull_Lt_01* is not visible in the displayed list but remains in the selection
When the GM clears the filter
Then the full **Model List** is restored with *Skull_Lt_01* still marked as selected

---

### Story: Generate Characters with Model Identities

Background:
  Given the **Model List** has *loaded state* *loaded*

#### Scenario Outline: Character generation from selected models

Given the GM confirms crowd creation selecting **Model** with *archetype name* {archetype_name}
When HVT generates a **Character** for the new **Crowd**
Then the **Character** has *character name* {character_name}
And the **Model Identity** on that **Character** has *model name* {model_name}

Model (Given):
| scenario                          | archetype_name     |
| Single model                      | Skull_Lt_01        |
| Duplicate model names (first)     | Skull_Lt_01        |
| Duplicate model names (second)    | Skull_Lt_01        |
| Name conflicts with existing      | Guard_Captain      |

Character (Then):
| scenario                          | character_name       |
| Single model                      | Skull_Lt_01          |
| Duplicate model names (first)     | Skull_Lt_01          |
| Duplicate model names (second)    | Skull_Lt_01_2        |
| Name conflicts with existing      | Guard_Captain_2      |

Model Identity (Then):
| scenario                          | model_name      |
| Single model                      | Skull_Lt_01     |
| Duplicate model names (first)     | Skull_Lt_01     |
| Duplicate model names (second)    | Skull_Lt_01     |
| Name conflicts with existing      | Guard_Captain   |

Then for each generated **Character**: the identity list contains exactly one **Model Identity** entry set as the *default designation* *default*

#### Scenario: Generated crowd contains exact count of selected models

Given the GM selected 5 **Models** from the *Model Browser*
When the **Crowd** creation completes
Then the **Crowd** has *characters for identity assignment* containing exactly 5 **Characters**
And each **Character** is immediately visible in the *Crowd Tree* under the new **Crowd**

---

### Story: Load Models List for Crowd Creation

Background:
  Given the **Game Bridge** has *initialization state* *ready*

#### Scenario Outline: Model list readiness for crowd creation

Given the **Game Loaded Event** has *publication state* {publication_state}
And the **Model List** has *loaded state* {loaded_state}
When the GM attempts to open the *Model Browser*
Then the **Model List** *loaded state* determines access to the *Model Browser*

Game Loaded Event (Given):
| scenario                        | publication_state |
| List loaded after event         | published         |
| List not yet loaded             | published         |
| Load failed (file missing)      | published         |

Model List (Given/Then):
| scenario                        | loaded_state |
| List loaded after event         | loaded       |
| List not yet loaded             | not loaded   |
| Load failed (file missing)      | not loaded   |

Then for *List loaded after event*: the *Model Browser* open action is available; any **Model Identity** assignment validates against this loaded list
Then for *List not yet loaded*: the *Model Browser* open action is disabled with "model list not ready" indicator
Then for *Load failed (file missing)*: the *Model Browser* remains unavailable for the session; error surfaced with guidance

#### Scenario: Model list cleared on session end

Given the **Model List** has *loaded state* *loaded*
When the session ends and a new session begins
Then the **Model List** is cleared from memory
And **Models.txt** is re-read on the next **Game Loaded Event** to reflect the current COH installation
