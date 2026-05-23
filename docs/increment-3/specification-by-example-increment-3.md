# Specification by Example — Increment 3: Animated Abilities

> Domain sources: `docs/increment-3/crc-increment-3.md` (CRC model), `docs/increment-3/acceptance-criteria-increment-3.md`, `docs/increment-3/ubiquitous-language-increment-3.md`.
> All table names are CRC class names. All column names are CRC responsibilities (properties). Scenario Outlines are the primary notation; plain Scenarios used only when flows are materially distinct.

---

## Resource Catalog Loading

---

### Story: Load FX Resource Catalog (FxRepo.data)

Background:
  Given the application is starting

#### Scenario Outline: FX catalog load from data file

Given the **FX Resource Catalog** has *data file reference* *FxRepo.data* in the **COH Data Directory**
When the application reads the data file
Then the **Resource Catalog** has *loaded state* {loaded_state}

Resource Catalog (Then):
| scenario                         | loaded_state |
| File present and valid           | loaded       |
| File missing at startup          | not loaded   |

Then for *File present and valid*: all **FX Resource** entries from the file are accessible by name and COH FX identifier; the resource picker in the ability editor is enabled
Then for *File missing at startup*: the application falls through to the seed-from-embedded-CSV path; the catalog is not reported as loaded until the seed completes

#### Scenario: Resource picker blocked before catalog load completes

Given the **FX Resource Catalog** has *loaded state* *not loaded*
When a resource-picker interaction or element-save attempts to use the **FX Resource Catalog**
Then the system rejects or blocks the operation
And the user sees an indication that the catalog is not yet ready

---

### Story: Load Movement Resource Catalog (MoveRepo.data)

Background:
  Given the application is starting

#### Scenario Outline: Movement catalog load from data file

Given the **Movement Resource Catalog** has *data file reference* *MoveRepo.data* in the **COH Data Directory**
When the application reads the data file
Then the **Resource Catalog** has *loaded state* {loaded_state}

Resource Catalog (Then):
| scenario                         | loaded_state |
| File present and valid           | loaded       |
| File missing at startup          | not loaded   |

Then for *File present and valid*: all **Movement Resource** entries are accessible by name and COH movement identifier; the resource picker for movement elements is enabled
Then for *File missing at startup*: the application falls through to the embedded-CSV seed path; no movement resource-picker or element-save proceeds until the seed completes

#### Scenario: Movement catalog held for session duration

Given the **Movement Resource Catalog** has *loaded state* *loaded*
Then the in-memory collection matches the persisted entries
And the catalog is not re-read mid-session

---

### Story: Load Sound Resource Catalog (SoundRepo.data)

Background:
  Given the application is starting

#### Scenario Outline: Sound catalog load from data file

Given the **Sound Resource Catalog** has *data file reference* *SoundRepo.data* in the **COH Data Directory**
When the application reads the data file
Then the **Resource Catalog** has *loaded state* {loaded_state}

Resource Catalog (Then):
| scenario                         | loaded_state |
| File present and valid           | loaded       |
| File missing at startup          | not loaded   |

Then for *File present and valid*: all **Sound Resource** entries are accessible by name and COH audio identifier; the resource picker for sound elements is enabled
Then for *File missing at startup*: the application falls through to the embedded-CSV seed path; no sound resource-picker or element-save proceeds until the seed completes

#### Scenario: All three catalogs loaded enables ability editor

Given the **FX Resource Catalog** has *loaded state* *loaded*
And the **Movement Resource Catalog** has *loaded state* *loaded*
And the **Sound Resource Catalog** has *loaded state* *loaded*
Then all resource pickers in the ability editor are enabled and populated
And the ability editor is not blocked by pending catalog load operations

---

### Story: Seed Resource Catalogs from Embedded CSV on First Run

Background:
  Given the application is starting

#### Scenario Outline: Catalog seeded from embedded CSV when no data file exists

Given no data file exists for the corresponding catalog in the **COH Data Directory**
And the **Embedded CSV** has *bundled resource data* {bundled_resource_data}
When the application seeds the catalog from the embedded data
Then the **Resource Catalog** has *loaded state* *loaded*

Embedded CSV (Given):
| scenario                         | bundled_resource_data |
| FX catalog first run             | FX resource entries   |
| Movement catalog first run       | movement entries      |
| Sound catalog first run          | sound entries         |

Then for each seeded catalog: the resulting data file is written to the **COH Data Directory** for future sessions; subsequent restarts load from the written file without seeding again

#### Scenario: Data file already exists — embedded CSV not read

Given the **FX Resource Catalog** data file *FxRepo.data* already exists in the **COH Data Directory**
When the application starts
Then the **Embedded CSV** is not read for the FX catalog type
And the file-based load path is used exclusively

#### Scenario: Embedded CSV absent or unreadable

Given the **Embedded CSV** for *FX* is absent or unreadable in the application assembly
When the application attempts to seed the **FX Resource Catalog**
Then the application reports the FX catalog as unavailable
And the remaining catalogs that can be seeded are still loaded without crash

---

### Story: Browse FX Resources for Ability Authoring

Background:
  Given the **FX Resource Catalog** has *loaded state* *loaded*
  And the ability editor is open for an **Animated Ability**

#### Scenario Outline: FX resource selection in picker

When the GM selects Add FX in the element list
Then the resource picker shows **FX Resource** with *display name* {display_name} and *COH FX command identifier* {COH_FX_command_identifier}

FX Resource (Then):
| scenario                  | display_name | COH_FX_command_identifier |
| First entry available     | Fire Blast   | FX_FireBlast_01           |
| Second entry available    | Ice Shield   | FX_IceShield_02           |

Then for each entry: the display name and identifier are shown for browsing and selection

#### Scenario: GM selects FX resource and confirms

Given the resource picker is showing **FX Resource** entries
When the GM selects **FX Resource** *Fire Blast* and confirms
Then a new **FX Element** is added with *referenced FX resource* *Fire Blast*
And the **Animation Element** has *display order position* at the bottom of the ordered list

#### Scenario: GM dismisses picker without selecting

When the GM dismisses the resource picker without selecting a **FX Resource**
Then no **Animation Element** is added to the ability
And the existing element list is unchanged

#### Scenario: Empty FX resource catalog

Given the **FX Resource Catalog** has *FX resource entries* containing zero entries
When the GM selects Add FX
Then the resource picker displays an empty state message
And the Add FX action remains accessible without error

---

### Story: Browse Movement Resources for Ability Authoring

Background:
  Given the **Movement Resource Catalog** has *loaded state* *loaded*
  And the ability editor is open for an **Animated Ability**

#### Scenario Outline: Movement resource selection in picker

When the GM selects Add MOV in the element list
Then the resource picker shows **Movement Resource** with *display name* {display_name} and *COH movement command identifier* {COH_movement_command_identifier}

Movement Resource (Then):
| scenario                  | display_name | COH_movement_command_identifier |
| First entry available     | Fly          | MOV_Fly_01                      |
| Second entry available    | Super Jump   | MOV_SuperJump_01                |

Then for each entry: the display name and identifier are shown for browsing and selection

#### Scenario: GM selects movement resource and confirms

Given the resource picker is showing **Movement Resource** entries
When the GM selects **Movement Resource** *Fly* and confirms
Then a new **Movement Element** is added with *referenced movement resource* *Fly*
And the **Animation Element** has *display order position* at the bottom of the ordered list

#### Scenario: GM dismisses picker without selecting

When the GM dismisses the resource picker without selecting
Then no element is added and the existing element list is unchanged

#### Scenario: Movement resource catalog not yet loaded

Given the **Movement Resource Catalog** has *loaded state* *not loaded*
When the GM looks at the Add MOV action
Then the Add MOV action is disabled or the picker shows a not-ready state
And no crash or data corruption occurs

---

### Story: Browse Sound Resources for Ability Authoring

Background:
  Given the **Sound Resource Catalog** has *loaded state* *loaded*
  And the ability editor is open for an **Animated Ability**

#### Scenario Outline: Sound resource selection in picker

When the GM selects Add Sound in the element list
Then the resource picker shows **Sound Resource** with *display name* {display_name} and *COH audio identifier* {COH_audio_identifier}

Sound Resource (Then):
| scenario                  | display_name | COH_audio_identifier |
| First entry available     | Thunder Clap | SND_ThunderClap_01   |
| Second entry available    | Wind Gust    | SND_WindGust_01      |

Then for each entry: the display name and identifier are shown for browsing and selection

#### Scenario: GM selects sound resource and confirms

Given the resource picker is showing **Sound Resource** entries
When the GM selects **Sound Resource** *Thunder Clap* and confirms
Then a new **Sound Element** is added with *referenced sound resource* *Thunder Clap*
And the **Animation Element** has *display order position* at the bottom of the ordered list

#### Scenario: GM dismisses picker without selecting

When the GM dismisses the resource picker without selecting
Then no element is added and the existing element list is unchanged

#### Scenario: Empty sound resource catalog

Given the **Sound Resource Catalog** has *sound resource entries* containing zero entries
When the GM selects Add Sound
Then the picker displays an empty state and the Add Sound action remains accessible without error

---

## Animated Ability Management

---

### Story: Create Animated Ability

Background:
  Given a **Character** with *character name* *Guard_Captain* selected in the crowd tree

#### Scenario Outline: Ability creation on character

When the GM selects Create in the ability list on the Crowd Manager — Abilities screen and provides name {ability_name}
Then the **Animated Ability** in the *Ability Option Group* has *ability name* {ability_name} with *activation key* {activation_key} and *persistence designation* {persistence_designation}

Animated Ability (Then):
| scenario                   | ability_name  | activation_key | persistence_designation | attack_designation | execution_state |
| New ability created        | Fire Strike   | (unset)        | non-persistent          | non-attack         | stopped         |

Then for *New ability created*: the ability appears in the ability list with zero **Animation Elements** in its ordered list

#### Scenario: Duplicate name rejected

When the GM attempts to create an **Animated Ability** with *ability name* *Fire Strike* that already exists on **Character** *Guard_Captain*
Then the system rejects the creation with an inline error indicating the name must be unique
And no ability is added to the *Ability Option Group*

#### Scenario: No character selected — action disabled

Given no **Character** is selected in the crowd tree
When the GM looks at the Create action in the ability list
Then the Create action is disabled
And the ability list remains visible in its empty state

---

### Story: Edit Animated Ability

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Fire Strike* in the *Ability Option Group*

#### Scenario: Edit opens pre-populated ability editor

When the GM selects Edit on **Animated Ability** *Fire Strike*
Then the ability editor opens pre-populated with the ability's current *ability name*, *activation key*, *persistence designation*, and *attack designation*
And the element list shows all existing **Animation Elements** in their current *display order position*

#### Scenario: Save applies changes

When the GM modifies fields in the ability editor and saves
Then the **Animated Ability** is updated with the new values
And the ability list in Crowd Manager — Abilities reflects the updated name and key

#### Scenario: Cancel discards changes

When the GM cancels without saving
Then the **Animated Ability** retains its previous values unchanged
And the ability editor closes, returning to Crowd Manager — Abilities

#### Scenario: Duplicate name on save rejected

When the GM attempts to save with *ability name* that duplicates another ability on the same **Character**
Then the save is rejected with an inline validation error
And the ability editor remains open so the GM can correct the name

#### Scenario: Successful save closes editor

When the GM saves successfully
Then the ability editor closes and the updated ability is selected in the ability list

---

### Story: Delete Animated Ability

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Fire Strike* in the *Ability Option Group*

#### Scenario: Ability and elements permanently removed

When the GM selects Delete on **Animated Ability** *Fire Strike*
Then the **Animated Ability** *Fire Strike* and all its **Animation Elements** are permanently removed from the *Ability Option Group*
And the ability no longer appears in the ability list

#### Scenario: Deleted ability was the default

Given the **Animated Ability** *Fire Strike* has *default designation* *default*
When the GM deletes **Animated Ability** *Fire Strike*
Then no **Animated Ability** on the **Character** carries the *default designation* after deletion

#### Scenario: Deleted ability is currently executing

Given the **Animated Ability** *Fire Strike* has *execution state* *executing*
When the GM deletes **Animated Ability** *Fire Strike*
Then execution is stopped before the ability is removed
And no error is raised; the stop completes cleanly before deletion

#### Scenario: Reference element points to deleted ability

Given another **Animated Ability** *Combo Strike* has a **Reference Element** with *referenced ability name* *Fire Strike*
When the GM deletes **Animated Ability** *Fire Strike*
Then the **Reference Element** remains in *Combo Strike's* element list
And when *Combo Strike* is played, the missing reference resolves to a no-op
And no cascade deletion of elements in other abilities occurs

---

### Story: Set Ability Activation Key

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Fire Strike* in the *Ability Option Group*

#### Scenario Outline: Activation key assignment

When the GM uses the set-key action on **Animated Ability** *Fire Strike* with key {activation_key}
Then the **Animated Ability** *Fire Strike* has *activation key* {activation_key}

Animated Ability (Then):
| scenario                     | ability_name  | activation_key |
| Valid key assigned           | Fire Strike   | F1             |
| Key cleared                  | Fire Strike   | (unset)        |

Then for *Valid key assigned*: the key is displayed in the ability list key column; the **Keyboard Hook** will dispatch this ability when F1 is pressed
Then for *Key cleared*: the ability is no longer keyboard-dispatchable; the key column displays empty

#### Scenario: Duplicate key on same character rejected

Given another **Animated Ability** *Ice Shield* on the same **Character** has *activation key* *F1*
When the GM assigns *activation key* *F1* to **Animated Ability** *Fire Strike*
Then the system rejects the assignment with a validation message
And *Fire Strike* retains its previous *activation key* value

#### Scenario: Key set and keyboard hook active dispatches ability

Given the **Animated Ability** *Fire Strike* has *activation key* *F1*
And the **Keyboard Hook** has *installed state* *installed*
When the GM presses F1 while **Character** *Guard_Captain* is active
Then the **Animated Ability** *Fire Strike* is dispatched per *Ability Dispatch* rules

---

### Story: Toggle Ability Persistence

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Fire Aura* in the *Ability Option Group*

#### Scenario Outline: Persistence flag toggle

When the GM toggles persistence on **Animated Ability** *Fire Aura*
Then the **Animated Ability** *Fire Aura* has *persistence designation* {persistence_designation}

Animated Ability (Then):
| scenario                       | ability_name | persistence_designation |
| Toggle on (was non-persistent) | Fire Aura    | persistent              |
| Toggle off (was persistent)    | Fire Aura    | non-persistent          |

Then for *Toggle on*: the persistent indicator appears in the ability list row; the ability will replay automatically on each subsequent **Identity** load
Then for *Toggle off*: the indicator is removed; the ability no longer replays on identity load

#### Scenario: Persistent ability stops and restarts on identity change

Given the **Animated Ability** *Fire Aura* has *persistence designation* *persistent* and *execution state* *executing*
When the **Character's** active **Identity** changes
Then the **Animated Ability** *Fire Aura* is stopped before the identity switch completes
And the **Animated Ability** *Fire Aura* is restarted after the new **Identity** has finished loading

#### Scenario: Persistent ability deactivated triggers costume reload

Given the **Animated Ability** *Fire Aura* has *persistence designation* *persistent* and *execution state* *executing*
When the GM clears *persistence designation* to *non-persistent* (deactivation)
Then the persistent-FX costume variant is loaded onto the **Spawned NPC** via the **Game Bridge**
And no persistent replay occurs on subsequent identity loads after deactivation

---

### Story: Set Default Ability for Character

Background:
  Given a **Character** with *character name* *Guard_Captain*

#### Scenario Outline: Default designation management

Given an **Animated Ability** with *ability name* {ability_name} in the *Ability Option Group*
When the GM uses set-default on **Animated Ability** {ability_name}
Then the **Ability Option Group** designates **Animated Ability** {ability_name} as *default ability* with state {default_designation}

Animated Ability (Then):
| scenario                    | ability_name  | default_ability_state |
| Set new default             | Recovery      | default             |

Then for *Set new default*: the default indicator is shown in the row; any previously designated default ability has its *default designation* cleared to *unset*

#### Scenario: Default ability auto-plays on spawn

Given the **Animated Ability** *Recovery* has *default designation* *default*
When **Character** *Guard_Captain* is spawned (a **Spawned NPC** becomes present)
Then the **Animated Ability** *Recovery* is automatically played on the **Spawned NPC**
And no manual play action is needed

#### Scenario: Default ability removed from character

Given the **Animated Ability** *Recovery* has *default designation* *default*
When **Animated Ability** *Recovery* is removed from the **Character**
Then no **Animated Ability** on the **Character** carries the *default designation*
And subsequent spawns do not auto-play any ability

#### Scenario: Clear default designation

When the GM toggles off the default designation on the current default ability
Then no **Animated Ability** on the **Character** has *default designation* *default*
And the ability list shows no default indicator

---

## Animation Element Authoring

---

### Story: Add Movement Element to Ability

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor
  And the **Movement Resource Catalog** has *loaded state* *loaded*

#### Scenario Outline: Movement element added to ability

When the GM selects Add MOV and selects **Movement Resource** {referenced_movement_resource}
Then a new **Movement Element** has *referenced movement resource* {referenced_movement_resource}
And the **Animation Element** has *display order position* {display_order_position}

Movement Element (Then):
| scenario                | referenced_movement_resource |
| Valid resource selected | Fly                          |

Animation Element (Then):
| scenario                | display_order_position |
| Valid resource selected | 3 (bottom)             |

Then for *Valid resource selected*: the element displays type MOV, resource name, and order position in the element list

#### Scenario: Movement element executed during ability play

Given a **Movement Element** with *referenced movement resource* *Fly*
When the **Animated Ability** executes this element
Then the referenced COH movement command is applied to the target **Spawned NPC** via the **Game Bridge**
And execution continues to the next **Animation Element**

#### Scenario: Movement resource not found at execution time

Given a **Movement Element** with *referenced movement resource* *Deleted_Move*
And the **Movement Resource Catalog** does not contain *Deleted_Move*
When the element executes
Then the element produces a silent no-op
And subsequent elements continue; the ability does not halt

#### Scenario: Reorder movement element via drag-drop

Given a **Movement Element** at *display order position* *3*
When the GM drag-drops it to position *1*
Then the **Animation Element** has *display order position* *1*
And all affected elements' positions shift accordingly

---

### Story: Add Sound Element to Ability

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor
  And the **Sound Resource Catalog** has *loaded state* *loaded*

#### Scenario Outline: Sound element added to ability

When the GM selects Add Sound and selects **Sound Resource** {referenced_sound_resource}
Then a new **Sound Element** has *referenced sound resource* {referenced_sound_resource}
And the **Animation Element** has *display order position* at the bottom

Sound Element (Then):
| scenario                | referenced_sound_resource |
| Valid resource selected | Thunder Clap              |

Then for *Valid resource selected*: the element displays type Sound, resource name, and order position

#### Scenario: Sound element executed during ability play

Given a **Sound Element** with *referenced sound resource* *Thunder Clap*
When the **Animated Ability** executes this element
Then the referenced COH audio identifier is played via the **Game Bridge**
And execution continues to the next element

#### Scenario: Sound resource not found at execution time

Given a **Sound Element** with *referenced sound resource* *Missing_Sound*
And the **Sound Resource Catalog** does not contain *Missing_Sound*
When the element executes
Then the element produces a silent no-op and subsequent elements continue

#### Scenario: Multiple sound elements play in sequence

Given an **Animated Ability** with two **Sound Elements** at *display order position* *1* and *2*
When the ability plays
Then each sound plays in turn according to the element order position

---

### Story: Add FX Element to Ability

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor
  And the **FX Resource Catalog** has *loaded state* *loaded*

#### Scenario Outline: FX element added to ability

When the GM selects Add FX and selects **FX Resource** {referenced_FX_resource}
Then a new **FX Element** has *referenced FX resource* {referenced_FX_resource}
And the **Animation Element** has *display order position* at the bottom

FX Element (Then):
| scenario                | referenced_FX_resource |
| Valid resource selected | Fire Blast             |

Then for *Valid resource selected*: the element displays type FX, resource name, and order position

#### Scenario: FX element executed during ability play

Given a **FX Element** with *referenced FX resource* *Fire Blast*
And a **Spawned NPC** is present in the game world
When the **Animated Ability** executes this element
Then the COH FX command for *Fire Blast* is issued on the target **Spawned NPC** via the **Game Bridge**
And execution continues to the next element

#### Scenario: FX resource not found at execution time

Given a **FX Element** with *referenced FX resource* *Deleted_FX*
And the **FX Resource Catalog** does not contain *Deleted_FX*
When the element executes
Then the element produces a silent no-op and subsequent elements continue

#### Scenario: Spawned NPC not present when FX element executes

Given a **FX Element** with *referenced FX resource* *Fire Blast*
And no **Spawned NPC** is present in the game world for the character
When the element executes
Then the FX command produces a no-op
And no error is raised; subsequent elements continue

---

### Story: Add Reference Element to Another Ability

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Combo Strike* open in the ability editor
  And an **Animated Ability** with *ability name* *Fire Strike* on the same **Character**

#### Scenario: Reference element added

When the GM selects Add Reference and names target ability *Fire Strike*
Then a new **Reference Element** has *referenced ability name* *Fire Strike*
And the **Animation Element** has *display order position* at the bottom of the ordered list

#### Scenario: Reference element executed inline

Given a **Reference Element** with *referenced ability name* *Fire Strike*
When the parent **Animated Ability** *Combo Strike* executes this element
Then the **Animated Ability** *Fire Strike's* full element list is executed inline at that point
And execution returns to *Combo Strike's* sequence after *Fire Strike* completes

#### Scenario: Self-reference rejected

When the GM attempts to create a **Reference Element** with *referenced ability name* *Combo Strike* (the owning ability itself)
Then the system rejects the reference with a validation message
And no element is added

#### Scenario: Referenced ability does not exist at execution time

Given a **Reference Element** with *referenced ability name* *Deleted_Ability*
And no **Animated Ability** with *ability name* *Deleted_Ability* exists on the **Character**
When the element executes
Then the **Reference Element** produces a silent no-op and subsequent elements continue

#### Scenario: Circular reference chain rejected at save

Given **Animated Ability** *Fire Strike* has a **Reference Element** with *referenced ability name* *Combo Strike*
When the GM attempts to add a **Reference Element** with *referenced ability name* *Fire Strike* to *Combo Strike* (creating A→B→A)
Then the reference that closes the circle is rejected at save time
And the existing valid reference structure is preserved

---

### Story: Add Sequence Element (And/Or)

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor

#### Scenario Outline: Sequence element creation

When the GM selects Add Sequence with type {execution_type}
Then a new **Sequence Element** has *execution type* {execution_type} and *child animation elements* {child_animation_elements}
And the **Animation Element** has *display order position* at the bottom

Sequence Element (Then):
| scenario            | execution_type | child_animation_elements |
| Add And sequence    | And            | (zero children)          |
| Add Or sequence     | Or             | (zero children)          |

Then for each: the element displays its type (And or Or), order position, and zero child elements initially

#### Scenario: And sequence executes all children in order

Given a **Sequence Element** with *execution type* *And* and three **Animation Elements** as children
When the **Sequence Element** executes
Then every child **Animation Element** is executed in ascending *display order position*
And each child completes before the next begins
And execution returns to the parent sequence after all children complete

#### Scenario: Or sequence executes one child at random

Given a **Sequence Element** with *execution type* *Or* and three **Animation Elements** as children
When the **Sequence Element** executes
Then exactly one child **Animation Element** is selected at random and executed
And all other sibling children are skipped
And execution returns to the parent sequence after the chosen child completes

#### Scenario: Empty sequence element at execution time

Given a **Sequence Element** with *execution type* *And* and *child animation elements* containing zero children
When the **Sequence Element** executes
Then the element produces a no-op and execution of the parent sequence continues

#### Scenario: Execution type changed on existing element

Given a **Sequence Element** with *execution type* *And*
When the GM changes the execution type to *Or*
Then the **Sequence Element** has *execution type* *Or*
And child elements are unaffected; the type change is saved with the ability

---

### Story: Add Pause Element

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor

#### Scenario Outline: Pause element creation

When the GM selects Add Pause and configures duration {pause_duration}
Then a new **Pause Element** has *pause duration* {pause_duration}
And the **Animation Element** has *display order position* at the bottom

Pause Element (Then):
| scenario             | pause_duration |
| Normal pause         | 2 seconds      |
| Zero duration        | 0 seconds      |

Then for *Normal pause*: the element displays type Pause and its configured duration value

#### Scenario: Pause element blocks progression during play

Given a **Pause Element** with *pause duration* *2 seconds*
When the **Animated Ability** executes this element
Then progression to the next element is blocked for 2 seconds
And after the pause completes, the next element begins execution normally

#### Scenario: Zero duration pause is a no-op

Given a **Pause Element** with *pause duration* *0 seconds*
When the element executes
Then execution continues immediately to the next element without delay

#### Scenario: Ability stopped mid-pause

Given a **Pause Element** with *pause duration* *5 seconds* is currently active during play
When the GM stops the **Animated Ability**
Then the pause timer is cancelled and the stop completes immediately
And no subsequent elements execute

---

### Story: Add Load-Identity Element

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Transform* open in the ability editor
  And an **Identity** *Dragon_Form* on the same **Character**

#### Scenario: Load-identity element added

When the GM selects Add Identity and names target identity *Dragon_Form*
Then a new **Load-Identity Element** has *target identity name* *Dragon_Form*
And the **Animation Element** has *display order position* at the bottom

#### Scenario: Load-identity element triggers identity switch during play

Given a **Load-Identity Element** with *target identity name* *Dragon_Form*
When the **Animated Ability** executes this element
Then the named **Identity** *Dragon_Form* is set as the active identity on the **Character**
And subsequent elements in the sequence execute after the identity switch completes

#### Scenario: Target identity does not exist at execution time

Given a **Load-Identity Element** with *target identity name* *Removed_Identity*
And no **Identity** *Removed_Identity* exists on the **Character**
When the element executes
Then the element produces a no-op and subsequent elements continue

#### Scenario: Saved element retains identity name even if identity later renamed

Given a **Load-Identity Element** with *target identity name* *Dragon_Form* saved on the ability
When the **Identity** *Dragon_Form* is later renamed or removed
Then the element retains the original *target identity name* *Dragon_Form* (no cascade update)
And if executed, the now-missing reference produces a no-op

---

### Story: Reorder Animation Elements via Drag-Drop

Background:
  Given an **Animated Ability** with *ability name* *Fire Strike* open in the ability editor
  And three **Animation Elements** at *display order position* *1*, *2*, and *3*

#### Scenario: Element moved to new position

When the GM drag-drops the **Animation Element** at *display order position* *3* to position *1*
Then the moved element has *display order position* *1*
And the elements previously at positions *1* and *2* shift to *2* and *3*

#### Scenario: Element dropped in same position

When the GM drops the **Animation Element** at *display order position* *2* back in position *2*
Then the element list is unchanged
And no save is triggered unless other changes were made

#### Scenario: Save persists new order

When the GM saves after a reorder
Then the new element order is persisted on the **Animated Ability**
And subsequent play executes elements in the updated *display order position*

#### Scenario: Cancel reverts reorder

When the GM cancels after a reorder (without saving)
Then the element *display order position* reverts to the state before the drag-drop
And the persisted ability retains its previous order

#### Scenario: Multiple reorders before save

When the GM performs multiple reorder actions before saving
Then only the final order at save time is persisted

---

## Ability Execution

---

### Story: Play Animated Ability on Character

Background:
  Given the **Game Bridge** is ready
  And a **Character** with *character name* *Guard_Captain*
  And a **Spawned NPC** is present in the game world for *Guard_Captain*

#### Scenario Outline: Playing an ability on a spawned character

Given an **Animated Ability** with *ability name* {ability_name} and *execution state* *stopped*
When the GM selects Play on **Animated Ability** {ability_name}
Then the **Animated Ability** has *execution state* {execution_state}

Animated Ability (Then):
| scenario                        | ability_name | execution_state |
| Character spawned, play begins  | Fire Strike  | executing       |

Then for *Character spawned, play begins*: the ability begins executing its **Animation Elements** in order on the target **Spawned NPC**; an active ability indicator is shown on the ability row

#### Scenario: All elements complete — ability stops

Given the **Animated Ability** *Fire Strike* has *execution state* *executing*
When all **Animation Elements** in the ordered list complete
Then the **Animated Ability** *Fire Strike* has *execution state* *stopped*
And the active indicator is cleared from the row

#### Scenario: Play blocked when character not spawned

Given no **Spawned NPC** is present for **Character** *Guard_Captain*
When the GM selects Play on an **Animated Ability**
Then the play is blocked with a visible indication
And no game command is issued

#### Scenario: Another ability already executing — stops first

Given an **Animated Ability** *Ice Shield* has *execution state* *executing* on **Character** *Guard_Captain*
When the GM selects Play on **Animated Ability** *Fire Strike*
Then the **Animated Ability** *Ice Shield* has *execution state* *stopped*
And the **Animated Ability** *Fire Strike* has *execution state* *executing* starting from its first element

---

### Story: Stop Active Ability

Background:
  Given a **Character** with *character name* *Guard_Captain*

#### Scenario: Stop halts executing ability

Given the **Animated Ability** *Fire Strike* has *execution state* *executing*
When the GM selects Stop on **Animated Ability** *Fire Strike*
Then the **Animated Ability** *Fire Strike* has *execution state* *stopped*
And the current element is abandoned; the active ability indicator is cleared

#### Scenario: Stop when nothing executing

When the GM selects Stop and no **Animated Ability** is currently executing on the **Character**
Then the stop action is a no-op and no error is raised

#### Scenario: Stop persistent ability does not clear persistence

Given the **Animated Ability** *Fire Aura* has *persistence designation* *persistent* and *execution state* *executing*
When the GM selects Stop on **Animated Ability** *Fire Aura*
Then the **Animated Ability** *Fire Aura* has *execution state* *stopped* and *persistence designation* *persistent*
And the ability will still replay on the next identity load

#### Scenario: Stop mid-pause-element

Given a **Pause Element** with *pause duration* *5 seconds* is active during the execution of **Animated Ability** *Fire Strike*
When the GM stops the ability
Then the pause timer is cancelled immediately and no subsequent elements execute

---

### Story: Execute Animation Sequence (And: sequential, Or: random)

Background:
  Given an **Animated Ability** is executing

#### Scenario: And sequence — all children execute in order

Given a **Sequence Element** with *execution type* *And* containing three child **Animation Elements**
When the **Sequence Element** executes
Then every child is executed one after another in ascending *display order position*
And each child completes before the next begins

#### Scenario: Or sequence — one child at random

Given a **Sequence Element** with *execution type* *Or* containing three child **Animation Elements**
When the **Sequence Element** executes
Then exactly one child is selected at random (uniform distribution) and executed
And all other siblings are skipped

#### Scenario: Or sequence with exactly one child

Given a **Sequence Element** with *execution type* *Or* containing exactly one child **Animation Element**
When the **Sequence Element** executes
Then that single child always executes (deterministic result)
And no random selection error occurs

#### Scenario: Nested sequence elements

Given a **Sequence Element** with *execution type* *And* containing a child **Sequence Element** with *execution type* *Or*
When the outer **Sequence Element** executes
Then the inner **Sequence Element** executes according to its own *execution type* before the outer sequence continues
And nesting to any depth is supported

---

### Story: Maintain Persistent Ability across Identity Changes

Background:
  Given the **Game Bridge** is ready
  And a **Character** with *character name* *Guard_Captain*
  And a **Spawned NPC** is present for *Guard_Captain*

#### Scenario: Persistent ability stopped before identity switch

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent* and *execution state* *executing*
When the **Character's** active **Identity** is changed
Then the **Animated Ability** *Fire Aura* has *execution state* *stopped* before the identity switch begins

#### Scenario: Persistent ability replays after new identity loads

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent*
When the new active **Identity** has finished loading on the **Spawned NPC**
Then the **Animated Ability** *Fire Aura* has *execution state* *executing* (replayed from first element)
And the active indicator returns on the ability row

#### Scenario: Multiple persistent abilities all restart

Given **Animated Abilities** *Fire Aura* and *Ice Shield* both with *persistence designation* *persistent* and *execution state* *executing*
When the **Character's** active **Identity** changes
Then both are stopped before the switch and both replay after the new identity loads
And each restarts independently

#### Scenario: Character despawned while persistent ability active

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent* and *execution state* *executing*
When the **Character** is despawned (no **Spawned NPC** present)
Then the **Animated Ability** *Fire Aura* has *execution state* *stopped*
And the *persistence designation* remains *persistent*; the ability will replay on the next spawn-and-identity-load

---

### Story: Load Persistent Costume on Deactivation

Background:
  Given the **Game Bridge** is ready
  And a **Character** with *character name* *Guard_Captain*
  And a **Spawned NPC** is present for *Guard_Captain*

#### Scenario: Deactivation triggers costume reload

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent* and *execution state* *executing*
When the GM clears *persistence designation* to *non-persistent* (deactivation)
Then the persistent-FX costume variant is loaded onto the **Spawned NPC** via the **Game Bridge**
And the visual state reflects the persistent-FX appearance after deactivation

#### Scenario: Deactivation when character not spawned

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent*
And no **Spawned NPC** is present for the **Character**
When the GM clears *persistence designation* to *non-persistent*
Then no costume load command is issued via the **Game Bridge**
And the *persistence designation* is still cleared on the ability

#### Scenario: One of multiple persistent abilities deactivated

Given **Animated Abilities** *Fire Aura* and *Ice Shield* both with *persistence designation* *persistent*
When the GM clears *persistence designation* on *Fire Aura* only
Then only the costume variant relevant to *Fire Aura* is reloaded
And *Ice Shield* remains with *persistence designation* *persistent* and is unaffected

#### Scenario: Persistent-FX costume variant file missing

Given an **Animated Ability** *Fire Aura* with *persistence designation* *persistent*
And the persistent-FX costume variant file does not exist
When the GM clears *persistence designation* to *non-persistent*
Then no costume load command is issued
And the **Character** retains its current in-game appearance without error

---

### Story: Add Default Abilities to Character

Background:
  Given a **Character** with *character name* *Guard_Captain*

#### Scenario: Default abilities added to empty character

Given the **Character** *Guard_Captain* has an empty *Ability Option Group*
When the Add Default Abilities operation is applied
Then 20 named **Animated Abilities** are added to the *Ability Option Group*: Recovery, Stun Recovery, Pass Turn, Half Phase Action, Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power
And each appears in the ability list with its standard name

#### Scenario: Default abilities configuration

When the default abilities are added
Then each has *activation key* *(unset)*, *persistence designation* *non-persistent*, *default designation* *unset*
And their *ordered animation elements* are pre-populated with the standard element configuration for each named ability

#### Scenario: Duplicate names not re-added

Given the **Character** already has an **Animated Ability** with *ability name* *Recovery*
When Add Default Abilities is applied
Then *Recovery* is not duplicated
And only the abilities whose names are not already present are created

#### Scenario: All 20 added to fresh character

Given the **Character** has zero **Animated Abilities**
When Add Default Abilities is applied
Then the *Ability Option Group* contains exactly 20 **Animated Abilities** after the operation

---

### Story: Refresh Ability Activation Eligibility

Background:
  Given a **Character** with *character name* *Guard_Captain*
  And an **Animated Ability** with *ability name* *Fire Strike* in the *Ability Option Group*

#### Scenario Outline: Eligibility reflects current conditions

Given the conditions for **Animated Ability** *Fire Strike* are as described per scenario
Then the **Ability Activation Eligibility** has *eligible state* {eligible_state}

Ability Activation Eligibility (Then):
| scenario                                  | eligible_state |
| Key assigned, not executing, char spawned | eligible       |
| No activation key assigned                | ineligible     |
| Ability currently executing               | ineligible     |
| Character not spawned                     | ineligible     |

Then for *eligible*: the **Keyboard Hook** will dispatch this ability when its *activation key* is pressed
Then for *ineligible*: the **Keyboard Hook** does not dispatch it even when its key is pressed

#### Scenario: Eligibility refreshes when conditions change

Given the **Animated Ability** *Fire Strike* has *activation key* *F1* and the **Character** is not spawned
When the **Character** is spawned (a **Spawned NPC** becomes present)
Then the **Ability Activation Eligibility** for *Fire Strike* is refreshed to *eligible*

---

## Keyboard Hook

---

### Story: Install Low-Level Keyboard Hook

#### Scenario: Successful hook installation at startup

When the application starts and hook installation is requested
Then the **Keyboard Hook** has *installed state* *installed*
And subsequent key events are intercepted for routing evaluation

#### Scenario: Hook installed enables ability dispatch

Given the **Keyboard Hook** has *installed state* *installed*
Then **Ability Dispatch** can fire on matching key presses

#### Scenario: Hook installation fails

When the application starts and hook installation fails (e.g., insufficient OS permissions)
Then the **Keyboard Hook** has *installed state* *not installed*
And keyboard-triggered **Ability Dispatch** is disabled for the session
And direct play actions in the ability list remain fully functional

#### Scenario: Hook uninstalled on application shutdown

Given the **Keyboard Hook** has *installed state* *installed*
When the application shuts down
Then the **Keyboard Hook** has *installed state* *not installed*
And no key events are intercepted after uninstall

---

### Story: Route Key Events when Game Window is Focused

Background:
  Given the **Keyboard Hook** has *installed state* *installed*
  And a **Character** with *character name* *Guard_Captain* is the active character
  And an **Animated Ability** *Fire Strike* with *activation key* *F1* on the active **Character**

#### Scenario Outline: Game window routing

Given the **Game Window Focus** has *focus state* {focus_state}
When the GM presses key *F1*
Then the routing result depends on focus

Game Window Focus (Given):
| scenario                          | focus_state |
| Game window focused               | focused     |
| Game window loses focus           | unfocused   |

Then for *Game window focused*: the **Keyboard Hook** evaluates **Key Routing** against the active **Character's** abilities; **Ability Dispatch** fires and the ability begins executing
Then for *Game window loses focus*: the key press does not trigger game-window routing; dispatch only fires again when the game window regains focus

#### Scenario: No matching activation key — pass through

Given the **Game Window Focus** has *focus state* *focused*
And no **Animated Ability** on the active **Character** has *activation key* matching the pressed key
When the GM presses a key
Then the key event is passed through to the game without dispatch
And no error or notification is generated

---

### Story: Route Key Events when Application Window is Focused

Background:
  Given the **Keyboard Hook** has *installed state* *installed*
  And a **Character** with *character name* *Guard_Captain* is the active character
  And an **Animated Ability** *Fire Strike* with *activation key* *F1* on the active **Character**

#### Scenario Outline: Application window routing

Given the **Application Window Focus** has *focus state* {focus_state}
When the GM presses key *F1*
Then the routing result depends on focus

Application Window Focus (Given):
| scenario                            | focus_state |
| Application window focused          | focused     |
| Neither game nor app window focused | unfocused   |

Then for *Application window focused*: the **Keyboard Hook** evaluates **Key Routing** using the same logic as game window focus; **Ability Dispatch** fires on a match
Then for *Neither focused*: key events are not routed and **Ability Dispatch** does not fire; the **Keyboard Hook** remains installed for future focus events

#### Scenario: Application window dispatch executes ability

Given the **Application Window Focus** has *focus state* *focused*
And the **Ability Activation Eligibility** for *Fire Strike* has *eligible state* *eligible*
When the GM presses *F1*
Then **Ability Dispatch** fires and **Animated Ability** *Fire Strike* has *execution state* *executing*
And the active indicator appears on the ability row in Crowd Manager — Abilities

#### Scenario: Application window loses focus — routing suspended

Given the **Application Window Focus** transitions from *focused* to *unfocused*
When the GM presses a key
Then the routing for application-window focus is suspended for that key press

---

### Story: Dispatch Ability Activation Keys to Characters

Background:
  Given the **Keyboard Hook** has *installed state* *installed*
  And a **Character** with *character name* *Guard_Captain* is the active character

#### Scenario: Dispatch fires on key match with eligible ability

Given an **Animated Ability** *Fire Strike* with *activation key* *F1*
And the **Ability Activation Eligibility** for *Fire Strike* has *eligible state* *eligible*
When a key press *F1* is received through **Key Routing**
Then **Ability Dispatch** retrieves **Animated Ability** *Fire Strike* and initiates execution on the **Spawned NPC**

#### Scenario: Dispatch suppressed when eligibility ineligible

Given an **Animated Ability** *Fire Strike* with *activation key* *F1*
And the **Ability Activation Eligibility** for *Fire Strike* has *eligible state* *ineligible*
When a key press *F1* is received
Then the dispatch is suppressed; the ability does not execute
And the key event is still consumed (not passed to the game)

#### Scenario: No active character — pass through

When a key press is received through **Key Routing** and there is no active **Character**
Then the key event is passed through without dispatch and no error is raised

#### Scenario: Duplicate activation key — first eligible match dispatched

Given two **Animated Abilities** on the **Character** both have *activation key* *F1* (an invariant violation)
When a key press *F1* is received
Then the system dispatches the first eligible match and logs the ambiguity
And a validation warning is surfaced in the ability list

#### Scenario: Dispatch completes — eligibility refreshed

Given **Ability Dispatch** fires and **Animated Ability** *Fire Strike* has *execution state* *executing*
When the ability completes and has *execution state* *stopped*
Then the active indicator is cleared on the ability row
And the **Ability Activation Eligibility** is refreshed to *eligible* for the next key press
