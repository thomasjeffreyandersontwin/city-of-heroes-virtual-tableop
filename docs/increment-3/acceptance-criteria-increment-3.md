# Acceptance Criteria — Increment 3: Animated Abilities

> Domain source: `docs/domain/ubiquitous-language-increment-3.md`
> All domain terms below appear in that file.

---

## Load FX Resource Catalog (FxRepo.data)

**Domain terms** (vocabulary for this story's AC):
- *FX Resource Catalog* — the in-memory resource store for visual-effects entries, loaded from `FxRepo.data`
- *FxRepo.data* — the binary data file in the COH data directory that persists FX resource entries
- *FX Resource* — a named visual-effects entry in the catalog
- *COH Data Directory* — the file-system location where catalog data files are stored
- *Resource Catalog* — base concept: in-memory store, loaded-state guard

1. **WHEN** the application starts and `FxRepo.data` is present in the *COH Data Directory*
   **THEN** the *FX Resource Catalog* is loaded into memory with all *FX Resource* entries from that file
   **AND** the catalog reports a loaded state, enabling the resource picker in the ability editor

2. **WHEN** the *FX Resource Catalog* is loaded
   **THEN** each *FX Resource* entry is accessible by name and COH FX identifier for browsing and assignment

3. **WHEN** `FxRepo.data` is missing or unreadable at startup
   **THEN** the application falls through to the seed-from-embedded-CSV path
   **BUT** the *FX Resource Catalog* is not reported as loaded from file; the embedded seed path must complete before the catalog is considered ready

4. **WHEN** a resource-picker interaction or element-save operation attempts to use the *FX Resource Catalog* before it has finished loading
   **THEN** the system rejects or blocks that operation
   **BUT** no crash or silent failure occurs; the user sees an indication that the catalog is not yet ready

---

## Load Movement Resource Catalog (MoveRepo.data)

**Domain terms**:
- *Movement Resource Catalog* — the in-memory store for movement resource entries, loaded from `MoveRepo.data`
- *MoveRepo.data* — the binary data file in the COH data directory for movement entries
- *Movement Resource* — a named movement entry in the catalog
- *COH Data Directory* — file-system storage location for catalog files

1. **WHEN** the application starts and `MoveRepo.data` is present in the *COH Data Directory*
   **THEN** the *Movement Resource Catalog* is loaded into memory with all *Movement Resource* entries
   **AND** the catalog reports a loaded state, enabling the resource picker for movement elements

2. **WHEN** the *Movement Resource Catalog* is loaded
   **THEN** each *Movement Resource* entry is accessible by name and COH movement identifier

3. **WHEN** `MoveRepo.data` is missing at startup
   **THEN** the application falls through to the embedded-CSV seed path for movement resources
   **BUT** no movement-related resource-picker or element-save proceeds until the seed completes

4. **WHEN** the *Movement Resource Catalog* has loaded from a prior session file
   **THEN** the in-memory collection matches the persisted entries without re-reading the file mid-session

---

## Load Sound Resource Catalog (SoundRepo.data)

**Domain terms**:
- *Sound Resource Catalog* — the in-memory store for sound resource entries, loaded from `SoundRepo.data`
- *SoundRepo.data* — the binary data file for sound entries
- *Sound Resource* — a named audio entry in the catalog
- *COH Data Directory* — file-system storage location

1. **WHEN** the application starts and `SoundRepo.data` is present in the *COH Data Directory*
   **THEN** the *Sound Resource Catalog* is loaded into memory with all *Sound Resource* entries
   **AND** the catalog reports a loaded state, enabling the resource picker for sound elements

2. **WHEN** the *Sound Resource Catalog* is loaded
   **THEN** each *Sound Resource* entry is accessible by name and COH audio identifier

3. **WHEN** `SoundRepo.data` is missing at startup
   **THEN** the application falls through to the embedded-CSV seed path for sound resources
   **BUT** no sound-related resource-picker or element-save proceeds until the seed completes

4. **WHEN** the three catalog load sequences complete (FX, movement, sound) in sequence
   **THEN** all resource pickers in the ability editor are enabled and populated
   **AND** the ability editor is not blocked by pending catalog load operations

---

## Seed Resource Catalogs from Embedded CSV on First Run

**Domain terms**:
- *Embedded CSV* — default resource data bundled in the application assembly for all three catalog types
- *Resource Catalog* — the persistent in-memory store seeded from this embedded data on first run
- *FX Resource Catalog*, *Movement Resource Catalog*, *Sound Resource Catalog* — the three typed catalogs seeded
- *COH Data Directory* — where the resulting binary data files are written after seeding

1. **WHEN** the application starts and no `FxRepo.data` is found in the *COH Data Directory*
   **THEN** the *FX Resource Catalog* is seeded from the *Embedded CSV* for FX resources
   **AND** the resulting data is written to `FxRepo.data` in the *COH Data Directory* for future sessions

2. **WHEN** the application starts and no `MoveRepo.data` or `SoundRepo.data` is found
   **THEN** the corresponding *Resource Catalogs* are seeded from their respective *Embedded CSVs*
   **AND** each resulting data file is written to disk before the catalog is marked loaded

3. **WHEN** a catalog data file already exists on disk
   **THEN** the embedded CSV is not read for that catalog type
   **BUT** the file-based load path is used exclusively

4. **WHEN** the embedded CSV for a catalog type is absent or unreadable in the assembly
   **THEN** the application reports the catalog as unavailable
   **BUT** it does not crash; the remaining catalogs that can be seeded are still loaded

5. **WHEN** all three catalogs have been seeded and written on first run
   **THEN** subsequent application restarts load from the written data files without seeding again

---

## Browse FX Resources for Ability Authoring

**Domain terms**:
- *FX Resource* — a named visual-effects entry available for assignment to a *FX Element*
- *FX Resource Catalog* — the source collection for the resource picker
- *Ability Editor* — the screen where the GM authors *Animated Abilities* and adds elements
- *FX Element* — the animation element type whose resource is selected through this browse flow

1. **WHEN** the GM selects Add FX in the *Ability Editor* element list
   **THEN** a resource picker opens showing all *FX Resource* entries from the *FX Resource Catalog*
   **AND** each entry displays its name and COH FX identifier

2. **WHEN** the GM selects a *FX Resource* in the picker and confirms
   **THEN** a new *FX Element* is added to the ability's element list with that resource reference
   **AND** the element appears at the bottom of the ordered list

3. **WHEN** the GM dismisses the picker without selecting a resource
   **THEN** no element is added to the ability
   **BUT** the ability's existing element list is unchanged

4. **WHEN** the *FX Resource Catalog* contains no entries (empty catalog)
   **THEN** the resource picker displays an empty state message
   **BUT** the Add FX action remains accessible without error

---

## Browse Movement Resources for Ability Authoring

**Domain terms**:
- *Movement Resource* — a named movement entry for assignment to a *Movement Element*
- *Movement Resource Catalog* — source for the movement resource picker
- *Ability Editor* — the screen hosting the element list
- *Movement Element* — the animation element type whose resource is selected here

1. **WHEN** the GM selects Add MOV in the *Ability Editor* element list
   **THEN** a resource picker opens showing all *Movement Resource* entries from the *Movement Resource Catalog*

2. **WHEN** the GM selects a *Movement Resource* and confirms
   **THEN** a new *Movement Element* is added to the ability's element list with that resource reference
   **AND** the element appears at the bottom of the ordered list

3. **WHEN** the GM dismisses the picker without selecting
   **THEN** no element is added
   **BUT** the existing element list is unchanged

4. **WHEN** the *Movement Resource Catalog* is not yet loaded
   **THEN** the Add MOV action is disabled or the picker shows a not-ready state
   **BUT** no crash or data corruption occurs

---

## Browse Sound Resources for Ability Authoring

**Domain terms**:
- *Sound Resource* — a named audio entry for assignment to a *Sound Element*
- *Sound Resource Catalog* — source for the sound resource picker
- *Ability Editor* — the authoring screen
- *Sound Element* — the animation element type configured through this flow

1. **WHEN** the GM selects Add Sound in the *Ability Editor* element list
   **THEN** a resource picker opens showing all *Sound Resource* entries from the *Sound Resource Catalog*

2. **WHEN** the GM selects a *Sound Resource* and confirms
   **THEN** a new *Sound Element* is added to the ability's element list with that resource reference
   **AND** the element appears at the bottom of the ordered list

3. **WHEN** the GM dismisses the picker without selecting
   **THEN** no element is added
   **BUT** the existing element list is unchanged

4. **WHEN** the *Sound Resource Catalog* is empty
   **THEN** the picker displays an empty state
   **BUT** the Add Sound action remains accessible and does not error

---

## Create Animated Ability

**Domain terms**:
- *Animated Ability* — the named composable action sequence created on a character
- *Crowd Manager — Abilities* — the screen where the GM manages the ability list for a selected character
- *Abilities Option Group* — the collection on a character that holds its animated abilities
- *Activation Key* — optional key field on the ability, unset at creation

1. **WHEN** the GM selects Create in the ability list on the *Crowd Manager — Abilities* screen
   **THEN** a new *Animated Ability* is added to the selected character's *Abilities Option Group* with a default name and empty element list
   **AND** the new ability appears in the ability list

2. **WHEN** the new *Animated Ability* is created
   **THEN** its activation key is unset, persistence flag is off, attack flag is off, and default flag is off
   **AND** it has zero *Animation Elements*

3. **WHEN** the GM attempts to create a second *Animated Ability* with the same name as an existing one on the same character
   **THEN** the system rejects the creation
   **AND** an inline error is displayed indicating the name must be unique
   **BUT** no ability is added

4. **WHEN** no character is selected in the crowd tree
   **THEN** the Create action in the ability list is disabled
   **BUT** the ability list remains visible in its empty state

---

## Edit Animated Ability

**Domain terms**:
- *Animated Ability* — the ability being edited
- *Ability Editor* — the screen opened by the edit action; contains ability config form and element list
- *Activation Key* — configurable key field on the ability
- *Animation Element* — ordered items in the element list managed from this screen

1. **WHEN** the GM selects Edit on an *Animated Ability* in the ability list
   **THEN** the *Ability Editor* opens pre-populated with the ability's current name, *Activation Key*, persistence flag, and attack flag
   **AND** the element list shows all existing *Animation Elements* in their current order

2. **WHEN** the GM modifies fields and saves
   **THEN** the *Animated Ability* is updated with the new values
   **AND** the ability list in *Crowd Manager — Abilities* reflects the updated name and key

3. **WHEN** the GM cancels without saving
   **THEN** the *Animated Ability* retains its previous values unchanged
   **AND** the *Ability Editor* closes, returning to *Crowd Manager — Abilities*

4. **WHEN** the GM attempts to save with a name that duplicates another ability on the same character
   **THEN** the save is rejected with an inline validation error
   **BUT** the *Ability Editor* remains open so the GM can correct the name

5. **WHEN** the GM saves successfully
   **THEN** the *Ability Editor* closes and the updated ability is selected in the ability list

---

## Delete Animated Ability

**Domain terms**:
- *Animated Ability* — the ability being removed
- *Crowd Manager — Abilities* — the screen hosting the delete action
- *Animation Elements* — the elements owned by the ability, removed with it

1. **WHEN** the GM selects Delete on an *Animated Ability* in the ability list
   **THEN** the ability and all its *Animation Elements* are permanently removed from the character's *Abilities Option Group*
   **AND** the ability no longer appears in the ability list

2. **WHEN** the deleted ability was the *Default Ability* for the character
   **THEN** no ability carries the default flag after deletion
   **BUT** other abilities on the character are unaffected

3. **WHEN** the deleted ability is currently executing (playing)
   **THEN** execution is stopped before the ability is removed
   **BUT** no error is raised; the stop completes cleanly before deletion

4. **WHEN** a *Reference Element* in another ability refers to the deleted ability by name
   **THEN** the *Reference Element* remains in the referencing ability's element list
   **AND** when the referencing ability is played, the missing reference resolves to a no-op
   **BUT** no cascade deletion of elements in other abilities occurs

---

## Set Ability Activation Key

**Domain terms**:
- *Activation Key* — the keyboard key bound to an *Animated Ability* for keyboard-triggered dispatch
- *Animated Ability* — the ability the key is assigned to
- *Keyboard Hook* — the system hook that listens for this key to dispatch the ability
- *Ability List* — the list in *Crowd Manager — Abilities* showing the assigned key

1. **WHEN** the GM uses the set-key action on an *Animated Ability* in the ability list
   **THEN** the *Activation Key* is updated on that ability and displayed in the key column
   **AND** the *Keyboard Hook* will now dispatch this ability when the new key is pressed

2. **WHEN** the GM assigns an *Activation Key* already used by another ability on the same character
   **THEN** the system rejects the assignment with a validation message
   **BUT** the ability retains its previous key value

3. **WHEN** the GM clears the *Activation Key* (assigns no key)
   **THEN** the ability is no longer keyboard-dispatchable
   **AND** the key column in the ability list displays empty

4. **WHEN** the ability's *Activation Key* is set and the *Keyboard Hook* is active
   **THEN** pressing the key while the character is active dispatches the ability per *Ability Dispatch* rules

---

## Toggle Ability Persistence

**Domain terms**:
- *Persistent Ability* — an *Animated Ability* with the persistence flag set
- *Animated Ability* — the ability whose flag is toggled
- *Active Identity* — the identity whose load triggers persistent ability replay
- *Crowd Manager — Abilities* — the screen where the toggle action is available

1. **WHEN** the GM toggles persistence on an *Animated Ability* that is currently non-persistent
   **THEN** the persistence flag is set and the persistent indicator appears in the ability list row
   **AND** the ability will replay automatically on each subsequent *Identity* load

2. **WHEN** the GM toggles persistence off on a *Persistent Ability*
   **THEN** the persistence flag is cleared and the indicator is removed
   **AND** the ability no longer replays on identity load

3. **WHEN** a *Persistent Ability* is active and the character's *Active Identity* changes
   **THEN** the persistent ability is stopped before the identity switch completes
   **AND** the persistent ability is restarted after the new identity has finished loading

4. **WHEN** a *Persistent Ability* is deactivated (persistence cleared while active)
   **THEN** the *Load Persistent Costume on Deactivation* behavior applies: the *persistent-FX costume variant* is reloaded onto the *Spawned NPC*
   **BUT** no persistent replay occurs on subsequent identity loads after deactivation

---

## Set Default Ability for Character

**Domain terms**:
- *Default Ability* — the *Animated Ability* auto-activated when the character is first spawned
- *Animated Ability* — the ability receiving the default flag
- *Spawned NPC* — the game-world entity activated when the character spawns

1. **WHEN** the GM uses set-default on an *Animated Ability* in the ability list
   **THEN** that ability receives the default flag and the default indicator is shown in its row
   **AND** any previously designated *Default Ability* on the same character has its default flag cleared

2. **WHEN** a character with a *Default Ability* is spawned
   **THEN** the designated *Default Ability* is automatically played on the *Spawned NPC*
   **AND** no manual play action is needed for this initial activation

3. **WHEN** the *Default Ability* is removed from the character
   **THEN** no ability carries the default flag after removal
   **AND** subsequent spawns do not auto-play any ability

4. **WHEN** the GM clears the default designation (set-default toggled off)
   **THEN** no ability on the character has the default flag
   **AND** the ability list shows no default indicator

---

## Add Movement Element to Ability

**Domain terms**:
- *Movement Element* — the animation element that applies a *Movement Resource* to the *Spawned NPC*
- *Animated Ability* — the ability receiving the new element
- *Movement Resource* — the resource selected from the *Movement Resource Catalog*
- *Element List* — the ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add MOV in the *Ability Editor* element list and selects a *Movement Resource*
   **THEN** a new *Movement Element* is added to the ability's element list at the bottom position
   **AND** the element displays its type (MOV), resource name, and order position

2. **WHEN** the *Movement Element* is executed during ability play
   **THEN** the referenced COH movement command is applied to the target *Spawned NPC*
   **AND** execution continues to the next element in the sequence

3. **WHEN** the *Movement Resource* referenced by the element is not found in the *Movement Resource Catalog* at execution time
   **THEN** the element produces a silent no-op
   **BUT** subsequent elements continue to execute; the ability does not halt

4. **WHEN** the GM reorders the new *Movement Element* via drag-drop
   **THEN** its order position updates and all affected elements' positions shift accordingly
   **AND** the new order is persisted when the ability is saved

---

## Add Sound Element to Ability

**Domain terms**:
- *Sound Element* — the animation element that plays a *Sound Resource*
- *Animated Ability* — the ability receiving the element
- *Sound Resource* — the resource selected from the *Sound Resource Catalog*
- *Element List* — the ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add Sound in the *Ability Editor* element list and selects a *Sound Resource*
   **THEN** a new *Sound Element* is added to the ability's element list at the bottom position
   **AND** the element displays its type (Sound), resource name, and order position

2. **WHEN** the *Sound Element* is executed during ability play
   **THEN** the referenced COH audio identifier is played
   **AND** execution continues to the next element

3. **WHEN** the *Sound Resource* is not found at execution time
   **THEN** the element produces a silent no-op
   **BUT** subsequent elements continue

4. **WHEN** multiple *Sound Elements* are present in the ability
   **THEN** each plays in turn according to the element order position
   **AND** no simultaneous collision between sound plays is managed at this increment

---

## Add FX Element to Ability

**Domain terms**:
- *FX Element* — the animation element that plays a *FX Resource* on the *Spawned NPC*
- *Animated Ability* — the ability receiving the element
- *FX Resource* — the resource selected from the *FX Resource Catalog*
- *Element List* — ordered list in the *Ability Editor*
- *Spawned NPC* — the game-world target

1. **WHEN** the GM selects Add FX in the *Ability Editor* element list and selects a *FX Resource*
   **THEN** a new *FX Element* is added to the ability's element list at the bottom position
   **AND** the element displays its type (FX), resource name, and order position

2. **WHEN** the *FX Element* is executed during ability play
   **THEN** the COH FX command for the referenced resource is issued on the target *Spawned NPC*
   **AND** execution continues to the next element

3. **WHEN** the *FX Resource* referenced by the element does not resolve at execution time
   **THEN** the element produces a silent no-op
   **BUT** subsequent elements continue; the ability does not halt

4. **WHEN** the *Spawned NPC* is not present in the game world when the *FX Element* executes
   **THEN** the FX command produces a no-op
   **BUT** no error is raised and subsequent elements continue

---

## Add Reference Element to Another Ability

**Domain terms**:
- *Reference Element* — the animation element that delegates to another *Animated Ability* by name
- *Animated Ability* — both the owning ability and the referenced ability
- *Element List* — ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add Reference in the *Ability Editor* element list and names a target ability on the same character
   **THEN** a new *Reference Element* is added showing the referenced ability name and order position

2. **WHEN** the *Reference Element* is executed during ability play
   **THEN** the referenced ability's full element list is executed inline at that point in the parent sequence
   **AND** execution returns to the parent sequence after the referenced ability completes

3. **WHEN** the GM attempts to create a *Reference Element* that references the owning ability itself (self-reference)
   **THEN** the system rejects the reference with a validation message
   **BUT** the element is not added

4. **WHEN** the referenced ability does not exist on the character at execution time
   **THEN** the *Reference Element* produces a silent no-op
   **AND** subsequent elements in the parent ability continue

5. **WHEN** a circular reference chain would be created (A → B → A)
   **THEN** the second reference that closes the circle is rejected at save time
   **BUT** the existing valid reference structure is preserved

---

## Add Sequence Element (And/Or)

**Domain terms**:
- *Sequence Element* — the animation element that groups children with And or Or execution type
- *Animation Sequence* — And: all children sequentially; Or: one child at random
- *Animated Ability* — the ability receiving the sequence element
- *Element List* — ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add Sequence in the *Ability Editor* element list and chooses And or Or
   **THEN** a new *Sequence Element* of the specified type is added to the element list
   **AND** the element displays its type (And or Or), order position, and zero child elements initially

2. **WHEN** the *Sequence Element* is executed with type And
   **THEN** every child *Animation Element* is executed in ascending order position
   **AND** execution returns to the parent sequence after all children complete

3. **WHEN** the *Sequence Element* is executed with type Or
   **THEN** exactly one child *Animation Element* is selected at random and executed
   **AND** all other sibling children are skipped; execution returns to the parent sequence after the chosen child completes

4. **WHEN** the *Sequence Element* contains no child elements at execution time
   **THEN** the element produces a no-op
   **BUT** execution of the parent sequence continues uninterrupted

5. **WHEN** the GM changes the execution type (And → Or or Or → And) on an existing *Sequence Element*
   **THEN** the type change is saved with the ability
   **AND** child elements are unaffected

---

## Add Pause Element

**Domain terms**:
- *Pause Element* — the animation element that introduces a timed delay
- *Animated Ability* — the ability receiving the element
- *Element List* — ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add Pause in the *Ability Editor* element list and configures a duration
   **THEN** a new *Pause Element* is added to the element list with the specified duration and order position
   **AND** the element displays its type (Pause) and configured duration value

2. **WHEN** the *Pause Element* is executed during ability play
   **THEN** progression to the next element is blocked for the configured duration
   **AND** after the pause completes, the next element begins execution normally

3. **WHEN** the pause duration is set to zero
   **THEN** the *Pause Element* is a no-op delay and execution continues immediately to the next element

4. **WHEN** the ability is stopped mid-execution while a *Pause Element* is active
   **THEN** the pause timer is cancelled and the stop completes immediately
   **BUT** no partial timing effect is applied to subsequent plays

---

## Add Load-Identity Element

**Domain terms**:
- *Load-Identity Element* — the animation element that triggers an identity switch mid-sequence
- *Identity* — the named identity on the same character activated by this element
- *Animated Ability* — the ability receiving the element
- *Element List* — ordered list in the *Ability Editor*

1. **WHEN** the GM selects Add Identity in the *Ability Editor* element list and names a target identity on the same character
   **THEN** a new *Load-Identity Element* is added showing the target identity name and order position

2. **WHEN** the *Load-Identity Element* is executed during ability play
   **THEN** the named identity is set as the *Active Identity* on the character
   **AND** subsequent elements in the sequence execute after the identity switch completes

3. **WHEN** the target identity does not exist on the character at execution time
   **THEN** the element produces a no-op
   **BUT** subsequent elements continue; the ability does not halt

4. **WHEN** the GM saves an ability containing a *Load-Identity Element* referencing a valid identity
   **THEN** the element is saved with the identity name
   **AND** if the identity is later renamed or removed, the element retains the original name (no cascade update)

---

## Reorder Animation Elements via Drag-Drop

**Domain terms**:
- *Animation Element* — the ordered composition unit being reordered
- *Element List* — the ordered display in the *Ability Editor*
- *Animated Ability* — the ability whose element order is being modified

1. **WHEN** the GM drag-drops an *Animation Element* from one position to another in the element list
   **THEN** the element moves to the target position
   **AND** all elements between the old and new positions shift by one position to accommodate

2. **WHEN** the GM drops the element in the same position it started
   **THEN** the element list is unchanged
   **AND** no save is triggered unless other changes were made

3. **WHEN** the GM saves after a reorder
   **THEN** the new element order is persisted on the *Animated Ability*
   **AND** subsequent play executes elements in the updated order

4. **WHEN** the GM cancels after a reorder (without saving)
   **THEN** the element order reverts to the state before the drag-drop
   **AND** the persisted ability retains its previous order

5. **WHEN** multiple reorder actions are performed before save
   **THEN** only the final order at save time is persisted

---

## Play Animated Ability on Character

**Domain terms**:
- *Animated Ability* — the ability being played
- *Spawned NPC* — the game-world entity on which the ability executes
- *Animation Element* — each typed unit executed in order during play
- *Crowd Manager — Abilities* — screen providing the play action

1. **WHEN** the GM selects Play on an *Animated Ability* in the ability list and the character is spawned
   **THEN** the ability begins executing its *Animation Elements* in order on the target *Spawned NPC*
   **AND** an active ability indicator is shown on the ability row

2. **WHEN** the ability's *Animation Elements* all complete
   **THEN** the ability stops and the active indicator is cleared from the row

3. **WHEN** the GM selects Play on an ability whose character is not currently spawned
   **THEN** the play is blocked with a visible indication
   **BUT** no game command is issued and no error is raised

4. **WHEN** another ability on the same character is already executing
   **THEN** the currently executing ability is stopped before the new ability begins
   **AND** the new ability's execution starts from its first element

---

## Stop Active Ability

**Domain terms**:
- *Animated Ability* — the currently executing ability to be stopped
- *Active Ability Indicator* — the visual marker on the executing ability's row
- *Crowd Manager — Abilities* — the screen where the stop action is available

1. **WHEN** the GM selects Stop on an *Animated Ability* that is currently executing
   **THEN** execution is halted immediately; the current element is abandoned
   **AND** the active ability indicator is cleared from the row

2. **WHEN** the GM selects Stop and no ability is currently executing on the character
   **THEN** the stop action is a no-op
   **BUT** no error is raised

3. **WHEN** a *Persistent Ability* is stopped via the stop action
   **THEN** the persistence flag is not cleared; the ability retains its persistence designation
   **AND** it will replay on the next identity load as expected

4. **WHEN** the ability is stopped mid-pause-element
   **THEN** the pause timer is cancelled immediately
   **AND** no subsequent elements execute

---

## Execute Animation Sequence (And: sequential, Or: random)

**Domain terms**:
- *Animation Sequence* — the execution pattern: And (all in order) or Or (one at random)
- *Sequence Element* — the element holding child elements and the And/Or type
- *Animation Element* — the child elements within the sequence

1. **WHEN** a *Sequence Element* with type And executes
   **THEN** every child *Animation Element* is executed one after another in ascending order
   **AND** each child completes before the next begins

2. **WHEN** a *Sequence Element* with type Or executes
   **THEN** exactly one child *Animation Element* is selected at random (uniform distribution)
   **AND** only that child executes; all other siblings are skipped

3. **WHEN** an Or *Sequence Element* contains exactly one child
   **THEN** that single child always executes (deterministic result)
   **AND** no random selection error occurs

4. **WHEN** a *Sequence Element* (And or Or) is nested inside another *Sequence Element*
   **THEN** the inner sequence executes according to its own type before the outer sequence continues
   **AND** nesting to any depth is supported

---

## Maintain Persistent Ability across Identity Changes

**Domain terms**:
- *Persistent Ability* — the ability with the persistence flag set
- *Active Identity* — the identity being changed
- *Spawned NPC* — the game-world entity on which the ability replays

1. **WHEN** a character's *Active Identity* is changed while a *Persistent Ability* is executing
   **THEN** the *Persistent Ability* is stopped before the identity switch begins

2. **WHEN** the new *Active Identity* has finished loading on the *Spawned NPC*
   **THEN** the *Persistent Ability* is automatically replayed from its first element
   **AND** the active indicator returns on the ability row

3. **WHEN** a character has multiple *Persistent Abilities*
   **THEN** all of them are stopped before the identity switch and all replay after
   **AND** each restarts independently

4. **WHEN** the character is despawned while a *Persistent Ability* is active
   **THEN** the persistent ability is stopped
   **BUT** the persistence flag is not cleared; the ability will still replay on the next spawn-and-identity-load

---

## Load Persistent Costume on Deactivation

**Domain terms**:
- *Persistent-FX Costume Variant* — the costume file carrying persistent ability visual layers
- *Persistent Ability* — the ability whose deactivation triggers the costume reload
- *Spawned NPC* — the game-world entity receiving the costume reload
- *Load Costume Command* — the game command that applies the costume file

1. **WHEN** a *Persistent Ability* is deactivated (persistence flag cleared while the ability is active)
   **THEN** the *Persistent-FX Costume Variant* is loaded onto the *Spawned NPC* via the *Load Costume Command*
   **AND** the visual state of the character in the game world reflects the persistent-FX appearance after deactivation

2. **WHEN** the deactivation occurs and the character is not spawned
   **THEN** the *Load Costume Command* is not issued
   **BUT** the persistence flag is still cleared on the ability

3. **WHEN** a character has multiple *Persistent Abilities* and one is deactivated
   **THEN** only the costume variant relevant to that ability is reloaded
   **AND** remaining active *Persistent Abilities* are unaffected

4. **WHEN** the *Persistent-FX Costume Variant* file does not exist at deactivation time
   **THEN** the *Load Costume Command* is not issued
   **AND** the character retains its current in-game appearance without error

---

## Add Default Abilities to Character

**Domain terms**:
- *Default Ability Set* — the standard named abilities attached to new characters: Recovery, Stun Recovery, Pass Turn, Half Phase Action, Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power
- *Animated Ability* — each ability in the default set
- *Abilities Option Group* — the character collection receiving the abilities

1. **WHEN** the Add Default Abilities operation is applied to a character
   **THEN** all 20 named abilities from the *Default Ability Set* are added to the character's *Abilities Option Group*
   **AND** each ability appears in the ability list with its standard name

2. **WHEN** the default abilities are added
   **THEN** each is created with no *Activation Key*, persistence off, attack flag reflecting its combat nature, and no default designation
   **AND** their element lists are pre-populated with the standard element configuration for each named ability

3. **WHEN** Add Default Abilities is applied to a character that already has one or more of the standard abilities by name
   **THEN** duplicate names are not added
   **AND** only the abilities whose names are not already present are created

4. **WHEN** Add Default Abilities is applied to a new character with an empty *Abilities Option Group*
   **THEN** all 20 abilities are added without conflict
   **AND** the ability list shows exactly 20 rows after the operation

---

## Refresh Ability Activation Eligibility

**Domain terms**:
- *Ability Activation Eligibility* — the computed readiness state gating keyboard-triggered dispatch
- *Animated Ability* — the ability whose eligibility is evaluated
- *Keyboard Hook* — the hook that consults eligibility before dispatching

1. **WHEN** the eligibility conditions for an *Animated Ability* change (e.g., character spawned/despawned, ability execution starts/stops, activation key assigned/cleared)
   **THEN** the *Ability Activation Eligibility* is refreshed and reflects the updated state

2. **WHEN** an ability has no *Activation Key* set
   **THEN** its *Ability Activation Eligibility* is ineligible
   **AND** the *Keyboard Hook* does not dispatch it

3. **WHEN** an ability is currently executing
   **THEN** its *Ability Activation Eligibility* is ineligible for re-trigger
   **BUT** the ability continues executing; no interruption occurs

4. **WHEN** the character is not spawned
   **THEN** all of its abilities report ineligible *Ability Activation Eligibility*
   **AND** no keyboard dispatch fires for any of them

---

## Install Low-Level Keyboard Hook

**Domain terms**:
- *Keyboard Hook* — the Windows system-level hook intercepting key events
- *Ability Dispatch* — the dispatch action enabled by the installed hook

1. **WHEN** the application starts and the hook installation is requested
   **THEN** the *Keyboard Hook* is installed as a low-level Windows keyboard hook
   **AND** subsequent key events are intercepted by the application for routing evaluation

2. **WHEN** the hook is successfully installed
   **THEN** the *Keyboard Hook* is in the installed state and *Ability Dispatch* can fire on matching key presses

3. **WHEN** the hook installation fails (e.g., insufficient OS permissions)
   **THEN** the application reports the failure
   **AND** keyboard-triggered *Ability Dispatch* is disabled for the session
   **BUT** direct play actions in the ability list remain fully functional

4. **WHEN** the application shuts down
   **THEN** the *Keyboard Hook* is uninstalled cleanly, releasing the OS hook handle
   **BUT** no key events are intercepted after uninstall

---

## Route Key Events when Game Window is Focused

**Domain terms**:
- *Key Routing* — the *Keyboard Hook* behavior that matches a key press to an ability
- *Game Window Focus* — the OS state where the COH game window is foreground
- *Ability Dispatch* — the triggered action when a key matches an ability's *Activation Key*
- *Active Character* — the character whose abilities are searched for a key match

1. **WHEN** the COH game window is the foreground window and the GM presses a key
   **THEN** the *Keyboard Hook* evaluates *Key Routing* against the *Active Character's* abilities

2. **WHEN** *Key Routing* finds an *Animated Ability* whose *Activation Key* matches the pressed key and eligibility permits
   **THEN** *Ability Dispatch* fires and the ability begins executing on the character

3. **WHEN** no ability on the *Active Character* has an *Activation Key* matching the pressed key
   **THEN** the key event is passed through to the game without dispatch
   **BUT** no error or notification is generated

4. **WHEN** the COH game window loses foreground focus mid-session
   **THEN** subsequent key presses no longer trigger game-window routing
   **AND** dispatch only fires again when the game window regains foreground focus

---

## Route Key Events when Application Window is Focused

**Domain terms**:
- *Application Window Focus* — the OS state where the HVT application window is foreground
- *Key Routing* — the routing evaluation performed by the *Keyboard Hook*
- *Ability Dispatch* — the triggered action when a match is found

1. **WHEN** the HVT application window is the foreground window and the GM presses a key
   **THEN** the *Keyboard Hook* evaluates *Key Routing* using the same logic as *Game Window Focus*
   **AND** *Ability Dispatch* fires on a match with eligible activation eligibility

2. **WHEN** neither the COH game window nor the HVT application window is the foreground window
   **THEN** key events are not routed and *Ability Dispatch* does not fire
   **BUT** the *Keyboard Hook* remains installed and continues to intercept events for future focus events

3. **WHEN** the application window is focused and the GM presses a key that matches an ability
   **THEN** the dispatch fires and the ability executes on the active character's *Spawned NPC*
   **AND** the active indicator appears on the ability row in the *Crowd Manager — Abilities* screen

4. **WHEN** the application window transitions from focused to unfocused
   **THEN** the routing for application-window focus is suspended immediately for subsequent key presses

---

## Dispatch Ability Activation Keys to Characters

**Domain terms**:
- *Ability Dispatch* — the action that executes an *Animated Ability* triggered by a key press
- *Activation Key* — the key on an *Animated Ability* that triggers dispatch
- *Active Character* — the character whose abilities are searched
- *Ability Activation Eligibility* — the guard that must pass before dispatch fires

1. **WHEN** a key press is received through *Key Routing* and the *Active Character* has an *Animated Ability* with a matching *Activation Key*
   **THEN** *Ability Dispatch* retrieves that ability and initiates execution of its element list on the character's *Spawned NPC*

2. **WHEN** *Ability Activation Eligibility* for the matched ability is ineligible at dispatch time
   **THEN** the dispatch is suppressed; the ability does not execute
   **BUT** the key event is still consumed (not passed to the game for other uses)

3. **WHEN** there is no *Active Character* at dispatch time
   **THEN** the key event is passed through without dispatch
   **BUT** no error is raised

4. **WHEN** the *Active Character* has multiple abilities and more than one shares the pressed key (an invariant violation)
   **THEN** the system dispatches the first eligible match and logs the ambiguity
   **AND** the invariant violation should be surfaced as a validation warning in the ability list

5. **WHEN** dispatch fires successfully and the ability completes
   **THEN** the active indicator is cleared on the ability row
   **AND** the *Ability Activation Eligibility* is refreshed to eligible for the next key press
