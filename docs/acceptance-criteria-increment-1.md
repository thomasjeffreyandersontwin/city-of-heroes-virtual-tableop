# Acceptance Criteria — Increment 1: Character and Crowd Library

**Product:** Hero Virtual Tabletop
**Increment:** 1 — Character and Crowd Library
**Outcome:** GM can create, organize, browse, and persist *characters* and *crowds* — the data foundation everything else builds on. No game connection required.

---

## Story: Validate City of Heroes Game Directory

**Story type:** system

### Domain terms

- *COH game directory* — the file-system path to the City of Heroes installation; must be confirmed valid before any *game bridge* operations proceed
- *game bridge* — the exclusive communication channel; cannot initialize without a valid *COH game directory*
- *crowd repository* — the persistent *crowd* store; not loaded until the *COH game directory* is confirmed

### Acceptance criteria

1. **WHEN** the application starts
   **THEN** the system checks that the stored *COH game directory* path exists on disk and contains the expected content
   **AND** proceeds to open the *crowd manager* if the check passes
   **Evidence:** story-map.md — "Validate City of Heroes Game Directory"; KeyBindsGenerator.cs — COH directory usage

2. **WHEN** no *COH game directory* is stored
   **THEN** the check fails immediately
   **AND** the GM is prompted to supply a path before startup continues
   **Evidence:** story-map.md — "Prompt for Game Directory if Invalid"

3. **WHEN** the stored path exists but does not contain the files needed by the *game bridge*
   **THEN** the check fails with a message identifying what is missing
   **BUT** the application does not proceed to load any *crowd repository* data
   **Evidence:** ubiquitous-language.md — *game bridge* requires *HookCostume DLL* at a known path under the *COH game directory*

---

## Story: Prompt for Game Directory if Invalid

**Story type:** user

### Domain terms

- *COH game directory* — the installation path the GM supplies when the stored path is absent or invalid
- *game bridge* — requires a valid *COH game directory* to locate the *HookCostume DLL* and data directories

### Acceptance criteria

1. **WHEN** the *COH game directory* check fails at startup
   **THEN** the GM is shown an input screen to supply or browse for the installation path
   **Evidence:** story-map.md — "Prompt for Game Directory if Invalid"

2. **WHEN** the GM submits a path
   **THEN** the system re-validates it against *game bridge* requirements
   **AND** saves the path and continues startup if validation passes
   **Evidence:** story-map.md — "Validate City of Heroes Game Directory"

3. **WHEN** the submitted path still fails validation
   **THEN** the input screen remains open with a clear error
   **BUT** the saved path is not updated and startup does not continue
   **Evidence:** story-map.md — "Prompt for Game Directory if Invalid"

---

## Story: Load Prism Shell and Module

**Story type:** system

### Domain terms

*(No terms from the ubiquitous language apply — this story is pure application infrastructure.)*

### Acceptance criteria

1. **WHEN** startup begins (after the *COH game directory* is confirmed)
   **THEN** the application shell initializes and all services are registered
   **AND** the application is ready to display the *crowd manager*
   **Evidence:** story-map.md — "Load Prism Shell and Module"

2. **WHEN** a required service cannot be resolved during initialization
   **THEN** the application surfaces a startup error
   **BUT** does not continue with a partially initialized state
   **Evidence:** Prism bootstrapper contract

---

## Story: Open Character Crowd Main Workspace

**Story type:** system

### Domain terms

- *crowd manager* — the pre-session library surface; opens after startup and displays the *crowd* hierarchy
- *crowd repository* — the persistent *crowd* store; loaded when the *crowd manager* opens

### Acceptance criteria

1. **WHEN** the application shell has initialized
   **THEN** the *crowd manager* is displayed to the GM
   **AND** the system begins loading the *crowd repository*
   **Evidence:** story-map.md — "Open Character Crowd Main Workspace"

2. **WHEN** the *crowd repository* load completes
   **THEN** the *crowd* hierarchy is shown in the *crowd manager*
   **Evidence:** CharacterCrowdMainViewModel.cs — repository load on workspace init

---

## Story: Load Crowd Collection from Repository

**Story type:** system

### Domain terms

- *crowd repository* — the persistent JSON store; read from the COH data directory on session start
- *crowd* — named hierarchical container displayed in the *crowd manager*
- *crowd member* — character or nested crowd restored from the repository
- *crowd manager* — the surface that displays the restored hierarchy

### Acceptance criteria

1. **WHEN** the *crowd manager* opens
   **THEN** the system reads the *crowd repository* from the COH data directory
   **AND** restores the full *crowd* and *crowd member* hierarchy
   **AND** displays the hierarchy in the *crowd manager*
   **Evidence:** story-map.md — "Load Crowd Collection from Repository"; CrowdRepository.cs — GetCrowdCollection

2. **WHEN** no *crowd repository* file exists (first run)
   **THEN** the system loads the embedded default *crowd members* instead
   **Evidence:** CrowdRepository.cs — seeds from embedded resource when file absent

3. **WHEN** the *crowd repository* file is unreadable or malformed
   **THEN** the *crowd manager* opens with an empty *crowd* hierarchy
   **BUT** the application does not crash; the GM is informed
   **Evidence:** CrowdRepository.cs — null guard after deserialization

---

## Story: Deserialize Crowd Collection from JSON

**Story type:** technical

### Domain terms

- *crowd repository* — source file for deserialization
- *crowd* — top-level container restored from JSON
- *crowd member* — member entry restored with its membership state
- *option group* — per-character collection (Identities, Abilities, Movements) restored with its ordered members

### Acceptance criteria

1. **WHEN** the system reads the *crowd repository* file
   **THEN** the JSON is deserialized into the full *crowd* hierarchy preserving names, nesting, and order
   **AND** each *crowd member's* three canonical *option groups* are restored with their persisted data
   **Evidence:** CrowdRepository.cs — Helper.GetDeserializedJSONFromFile; Character.cs — AfterDeserialized

2. **WHEN** a *crowd member* in the JSON has no *option groups* stored
   **THEN** the system reconstructs the three canonical *option groups* (Identities, Abilities, Movements) with defaults
   **Evidence:** Character.cs — `[OnDeserialized] AfterDeserialized`

3. **WHEN** deserialization returns null (file empty or schema mismatch)
   **THEN** the system returns an empty *crowd* list as fallback
   **BUT** no exception propagates to the *crowd manager*
   **Evidence:** CrowdRepository.cs — null guard before returning collection

---

## Story: Load Default Crowd Members from Embedded Resource

**Story type:** system

### Domain terms

- *crowd repository* — persistent store; absent or empty on first run triggers the embedded fallback
- *crowd member* — the pre-defined members seeded from the embedded resource

### Acceptance criteria

1. **WHEN** the *crowd repository* file is absent or returns an empty collection
   **THEN** the system reads the embedded default *crowd member* resource
   **AND** presents those *crowd members* in the *crowd manager* so the GM has example content to start with
   **Evidence:** CrowdRepository.cs — LoadDefaultCrowdMembers; story-map.md — "Load Default Crowd Members from Embedded Resource"

2. **WHEN** the defaults are loaded
   **THEN** the GM can immediately create, rename, or delete *crowd members* as normal
   **Evidence:** story-map.md — "Load Default Crowd Members from Embedded Resource"

---

## Story: Create Crowd

**Story type:** user

### Domain terms

- *crowd* — named hierarchical container the GM creates in the *crowd manager*
- *gang mode* — boolean flag on *crowd*; defaults to true on creation
- *crowd manager* — the surface where the GM triggers crowd creation

### Acceptance criteria

1. **WHEN** the GM creates a *crowd*
   **THEN** a new *crowd* with a unique default name is added to the hierarchy
   **AND** the new *crowd* appears in the *crowd manager* selected and ready to rename
   **AND** *gang mode* is set to true on the new *crowd*
   **Evidence:** story-map.md — "Create Crowd"; Crowd.cs — constructor sets IsGangMode = true

2. **WHEN** a *crowd* with the same name already exists at the same level
   **THEN** the new *crowd* receives a disambiguated name
   **BUT** no existing *crowd* is modified
   **Evidence:** Crowd.cs — unique name enforcement via HashedObservableCollection

---

## Story: Rename Crowd

**Story type:** user

### Domain terms

- *crowd* — container being renamed

### Acceptance criteria

1. **WHEN** the GM renames a *crowd*
   **THEN** the *crowd's* name is updated and the *crowd manager* reflects it immediately
   **Evidence:** Crowd.cs — Name setter; CharacterExplorerViewModel

2. **WHEN** the new name duplicates a sibling *crowd's* name
   **THEN** the rename is rejected and the *crowd* keeps its prior name
   **BUT** no other *crowd* is affected
   **Evidence:** HashedObservableCollection — unique name constraint

---

## Story: Delete Crowd

**Story type:** user

### Domain terms

- *crowd* — container being removed
- *crowd member* — nested members removed with the deleted *crowd*

### Acceptance criteria

1. **WHEN** the GM deletes a *crowd*
   **THEN** the *crowd* and all its *crowd members* are removed from the hierarchy
   **AND** the *crowd manager* no longer shows the deleted *crowd*
   **Evidence:** story-map.md — "Delete Crowd"; CrowdModel.RemoveAll

2. **WHEN** a deleted *crowd* contains *crowd members* that are linked into other *crowds*
   **THEN** only the membership in the deleted *crowd* is removed
   **AND** the linked *crowd members* remain visible in any other *crowds* that reference them
   **Evidence:** CrowdMember — shared reference pattern; Remove removes the reference, not the object

3. **WHEN** the GM attempts to delete the *all characters crowd*
   **THEN** the deletion is blocked
   **BUT** all other *crowds* may be deleted normally
   **Evidence:** Constants.ALL_CHARACTER_CROWD_NAME — protected special crowd

---

## Story: Nest Crowd inside Crowd

**Story type:** user

### Domain terms

- *crowd* — both the container receiving the nested *crowd* and the *crowd* being nested

### Acceptance criteria

1. **WHEN** the GM nests one *crowd* inside another
   **THEN** the nested *crowd* is added to the parent *crowd's* member collection
   **AND** the *crowd manager* displays the nested *crowd* indented under its parent
   **Evidence:** story-map.md — "Nest Crowd inside Crowd"; CrowdModel.Add

2. **WHEN** the nested *crowd* is added
   **THEN** its back-reference to its parent *crowd* is set
   **Evidence:** CrowdMemberModel — ParentCrowd property

3. **WHEN** the GM attempts to nest a *crowd* inside itself or create a circular hierarchy
   **THEN** the operation is rejected
   **BUT** both *crowds* remain intact
   **Evidence:** Domain invariant — crowd hierarchy must be acyclic

---

## Story: Create Character in Crowd

**Story type:** user

### Domain terms

- *character* — the new entity being created
- *crowd* — the container receiving the new *character*
- *crowd member* — the *character* as it participates in the *crowd*
- *option group* — three canonical groups (Identities, Abilities, Movements) initialized on creation
- *default identity* — auto-set on the new *character* at creation time

### Acceptance criteria

1. **WHEN** the GM creates a *character* in a *crowd*
   **THEN** a new *crowd member* with a unique default name is added to that *crowd*
   **AND** the *character* appears in the *crowd manager* under the selected *crowd*
   **Evidence:** story-map.md — "Create Character in Crowd"; CharacterExplorerViewModel

2. **WHEN** the *character* is created
   **THEN** the system initializes three canonical *option groups* (Identities, Abilities, Movements)
   **AND** the *default identity* is set to a matching costume or a Model identity
   **Evidence:** Character.cs — constructor; SetActiveIdentity

3. **WHEN** a *character* with the same name already exists in that *crowd*
   **THEN** the new *character* receives a disambiguated name
   **BUT** the existing *character* is not modified
   **Evidence:** HashedObservableCollection — unique name per crowd

---

## Story: Rename Character

**Story type:** user

### Domain terms

- *character* — the entity being renamed
- *crowd member* — the membership entry whose keyed name must update with the rename

### Acceptance criteria

1. **WHEN** the GM renames a *character*
   **THEN** the *character's* name is updated
   **AND** the *crowd member* collection re-keys from the old name to the new one
   **AND** the *crowd manager* reflects the new name
   **Evidence:** Character.cs — Name setter; Crowd.cs — Member_PropertyChanged("Name")

2. **WHEN** the new name matches a sibling *crowd member's* name
   **THEN** the rename is rejected and the *character* keeps its prior name
   **BUT** no other *character* is affected
   **Evidence:** HashedObservableCollection — unique name per crowd

---

## Story: Delete Character from Crowd

**Story type:** user

### Domain terms

- *character* — the *crowd member* being removed
- *crowd* — the container losing the member
- *crowd member* — the membership entry being deleted

### Acceptance criteria

1. **WHEN** the GM deletes a *character* from a *crowd*
   **THEN** the *crowd member* entry is removed from that *crowd*
   **AND** the *crowd manager* no longer shows it under that *crowd*
   **Evidence:** story-map.md — "Delete Character from Crowd"; CrowdModel.Remove

2. **WHEN** the *character* is linked into multiple *crowds* and deleted from one
   **THEN** only the membership in the selected *crowd* is removed
   **AND** the *character* remains in any other *crowds* that reference it
   **Evidence:** CrowdMember — shared reference; Remove removes only the one entry

---

## Story: Clone Character

**Story type:** user

### Domain terms

- *character* — both the original and the independent deep-copy produced by the clone
- *crowd member* — the cloned entry added to the same *crowd* as the original
- *option group* — cloned with all its contents for each capability class

### Acceptance criteria

1. **WHEN** the GM clones a *character*
   **THEN** an independent copy of the *character* is created with the same configuration (name plus suffix, *option groups*, identities, abilities, movements)
   **AND** the clone is added to the same *crowd* as the original
   **AND** both appear as separate entries in the *crowd manager*
   **Evidence:** story-map.md — "Clone Character"; CrowdMemberModel.Clone

2. **WHEN** the clone is created
   **THEN** modifying the clone's name or *option group* contents has no effect on the original
   **AND** modifying the original has no effect on the clone
   **Evidence:** Character.cs — deep-clone via DeepClone; Identity.cs — Identity.Clone

---

## Story: Cut Character to Clipboard

**Story type:** user

### Domain terms

- *character* — the *crowd member* being cut
- *crowd* — the container from which the *character* is removed on cut
- *clipboard* — the transient buffer that holds the cut *crowd member* until paste

### Acceptance criteria

1. **WHEN** the GM cuts a *character*
   **THEN** the *character* is placed on the *clipboard*
   **AND** the *character* is removed from its *crowd*
   **AND** the *crowd manager* no longer shows it under that *crowd*
   **Evidence:** story-map.md — "Cut Character to Clipboard"; CharacterExplorerViewModel clipboard operations

2. **WHEN** the GM pastes into a different *crowd*
   **THEN** the *character* is added to the target *crowd*
   **AND** the *clipboard* is cleared
   **Evidence:** CharacterExplorerViewModel — paste operation

3. **WHEN** the target *crowd* already has a *character* with the same name
   **THEN** the pasted *character* receives a disambiguated name
   **BUT** the existing *character* is not modified
   **Evidence:** HashedObservableCollection — unique name per crowd

---

## Story: Link Character across Crowds

**Story type:** user

### Domain terms

- *character* — the entity shared by reference across multiple *crowds*
- *crowd* — both the *crowd* owning the *character* and the *crowd* receiving the link
- *crowd member* — the shared reference entry added to the second *crowd*

### Acceptance criteria

1. **WHEN** the GM links a *character* into a second *crowd*
   **THEN** the same *character* instance is added as a *crowd member* of the second *crowd*
   **AND** the *character* appears in both *crowds* in the *crowd manager*
   **Evidence:** story-map.md — "Link Character across Crowds"; CrowdMember shared-reference pattern

2. **WHEN** a linked *character's* identities or abilities are updated
   **THEN** the change is reflected in all *crowds* that hold the link
   **AND** there is only one underlying *character* object
   **Evidence:** CrowdMember — same object reference in both collections

3. **WHEN** a link is removed from one *crowd*
   **THEN** only that *crowd's* membership entry is removed; the *character* and all other links remain
   **Evidence:** CrowdModel.Remove — removes the collection entry, not the object

---

## Story: Clone-Link Character

**Story type:** user

### Domain terms

- *character* — the entity referenced by the new membership entry
- *crowd* — the container receiving the new link
- *crowd member* — the new membership entry pointing to the same *character*

### Acceptance criteria

1. **WHEN** the GM performs a Clone-Link on a *character*
   **THEN** a new *crowd member* entry pointing to the same *character* is created in the target *crowd*
   **AND** the *character* appears in the target *crowd* without duplicating its data
   **Evidence:** story-map.md — "Clone-Link Character"; CharacterExplorerViewModel — CloneLink operation

2. **WHEN** a Clone-Link is created
   **THEN** changes to the *character's* configuration are visible in both *crowds*
   **AND** there is exactly one *character* object shared by both memberships
   **Evidence:** Shared-reference pattern — same CrowdMember in multiple CrowdModels

---

## Story: Flatten-Copy Crowd into Numbered Characters

**Story type:** user

### Domain terms

- *crowd* — the container whose membership is being flattened
- *flatten-copy* — the operation that replaces shared *crowd member* references with independently numbered deep-copy *characters*
- *character* — each independent deep-copy produced by the *flatten-copy* operation
- *crowd member* — the numbered copies that replace the original membership

### Acceptance criteria

1. **WHEN** the GM applies *flatten-copy* to a *crowd*
   **THEN** each *crowd member* is deep-cloned into an independent *character* with a numeric suffix
   **AND** the numbered *characters* replace the original members in the *crowd*
   **Evidence:** story-map.md — "Flatten-Copy Crowd into Numbered Characters"

2. **WHEN** the *flatten-copy* completes
   **THEN** each numbered *character* is independent — modifying one does not affect the others
   **AND** shared *crowd member* references within this *crowd* are broken
   **Evidence:** CrowdMemberModel.Clone — produces independent instances

---

## Story: Clone Memberships to Another Crowd

**Story type:** user

### Domain terms

- *crowd* — both the source *crowd* being replicated and the new *crowd* receiving the membership structure
- *crowd member* — shared reference entries copied into the new *crowd*
- *character* — the underlying entities shared by both *crowds*

### Acceptance criteria

1. **WHEN** the GM clones the memberships of a *crowd* to a new *crowd*
   **THEN** a new *crowd* is created with the same hierarchical membership structure
   **AND** each *crowd member* entry points to the same underlying *character* or nested *crowd* (shared reference)
   **Evidence:** story-map.md — "Clone Memberships to Another Crowd"; CrowdModel.CloneMemberships

2. **WHEN** the clone-memberships operation completes
   **THEN** changes to *characters* are reflected in both *crowds*
   **AND** the *crowd manager* shows both *crowds* as separate entries with the same member structure
   **Evidence:** CrowdModel.CloneMemberships — adds same member references, not deep copies

---

## Story: Drag-Drop Character between Crowds

**Story type:** user

### Domain terms

- *character* — the *crowd member* being repositioned
- *crowd* — both the source and destination containers
- *crowd member* — the membership entry being moved

### Acceptance criteria

1. **WHEN** the GM drags a *character* from one *crowd* and drops it into another
   **THEN** the *crowd member* entry is removed from the source *crowd*
   **AND** the *character* is added to the destination *crowd*
   **AND** the *crowd manager* reflects the new positions immediately
   **Evidence:** story-map.md — "Drag-Drop Character between Crowds"; CharacterExplorerViewModel drag-drop handlers

2. **WHEN** the drop completes
   **THEN** the *crowd member's* *roster crowd* back-reference is updated to the destination *crowd*
   **Evidence:** CrowdMember — RosterCrowd updated on membership change

3. **WHEN** the destination already has a *character* with the same name
   **THEN** the moved *character* receives a disambiguated name
   **BUT** the existing *character* in the destination is not affected
   **Evidence:** HashedObservableCollection — unique name per crowd

---

## Story: Filter Characters by Name

**Story type:** user

### Domain terms

- *crowd* — container filtered and expanded to show matches
- *crowd member* — the entity tested against the filter pattern

### Acceptance criteria

1. **WHEN** the GM types in the filter field
   **THEN** each *crowd member* and *crowd* name is tested against the filter pattern
   **AND** only matching items and their ancestor *crowds* remain visible in the *crowd manager*
   **AND** matching *crowds* expand to show their matching *crowd members*
   **Evidence:** story-map.md — "Filter Characters by Name"; CrowdModel.ApplyFilter

2. **WHEN** a *crowd* does not match but contains at least one matching *crowd member*
   **THEN** the *crowd* is shown so the matching member is reachable
   **Evidence:** CrowdModel.ApplyFilter — parent marked matched if any child matched

3. **WHEN** the filter field is cleared
   **THEN** all *crowds* and *crowd members* are shown in the *crowd manager*
   **Evidence:** CrowdModel.ResetFilter

---

## Story: Browse Crowds by Concept (Animals, Armed Forces, Civilians, Vehicles, etc.)

**Story type:** user

### Domain terms

- *crowd* — root-level container grouping *characters* by thematic type

### Acceptance criteria

1. **WHEN** the GM opens the *crowd manager*
   **THEN** root *crowds* organized by thematic category (Animals, Armed Forces, Civilians, Vehicles, etc.) are shown
   **AND** the GM can expand any category *crowd* to see its *crowd members*
   **Evidence:** story-map.md — "Browse Crowds by Concept"

2. **WHEN** the GM expands a category *crowd*
   **THEN** only the *crowd members* belonging to that category are shown in that branch
   **Evidence:** *crowd* hierarchy — tree structure enforces category scoping

---

## Story: Browse Crowds by Gangs, Crews, and Squads

**Story type:** user

### Domain terms

- *crowd* — root-level grouping containing *crowds* organized as villain groups, hero squads, or tactical teams
- *gang mode* — boolean flag on each *crowd* in this section indicating coordinated activation

### Acceptance criteria

1. **WHEN** the GM browses the Gangs, Crews, and Squads section in the *crowd manager*
   **THEN** all *crowds* in that grouping are shown with their *gang mode* status visible
   **Evidence:** story-map.md — "Browse Crowds by Gangs, Crews, and Squads"

2. **WHEN** the GM expands a gang-type *crowd*
   **THEN** its *crowd members* are listed with any *gang leader* identifiable
   **Evidence:** Crowd.cs — IsGangMode; Character.cs — IsGangLeader

---

## Story: Browse Crowds by COH Structure

**Story type:** user

### Domain terms

- *crowd* — root-level grouping organizing *characters* by COH in-game organizational hierarchy

### Acceptance criteria

1. **WHEN** the GM browses the COH Structure section in the *crowd manager*
   **THEN** *crowds* organized by COH-specific category are shown with their nested structure preserved
   **Evidence:** story-map.md — "Browse Crowds by COH Structure"

2. **WHEN** the GM expands a COH Structure *crowd*
   **THEN** only *crowd members* within that structure are shown; members from other structures are not mixed in
   **Evidence:** *crowd* hierarchy — structural grouping enforced by nesting

---

## Story: Browse All Characters Crowd

**Story type:** user

### Domain terms

- *all characters crowd* — the special protected root *crowd* aggregating every *character* in the *crowd repository* as a flat alphabetical list
- *character* — every *character* in the *crowd repository* appears in the *all characters crowd*
- *crowd repository* — source of all *characters*

### Acceptance criteria

1. **WHEN** the GM browses the *all characters crowd*
   **THEN** every *character* in the *crowd repository* appears as a *crowd member* in alphabetical order
   **Evidence:** story-map.md — "Browse All Characters Crowd"; Constants.ALL_CHARACTER_CROWD_NAME

2. **WHEN** a *character* is added to any *crowd* in the repository
   **THEN** it also appears in the *all characters crowd*
   **Evidence:** CharacterCrowdMainViewModel — all characters crowd maintenance

3. **WHEN** the GM attempts to delete the *all characters crowd*
   **THEN** the deletion is blocked
   **BUT** *characters* within it can be managed normally
   **Evidence:** Constants.ALL_CHARACTER_CROWD_NAME — protected crowd; delete guard

---

## Story: Save Crowd Collection to Repository

**Story type:** system

### Domain terms

- *crowd repository* — the JSON file written to the COH data directory
- *crowd* — hierarchy serialized and persisted
- *crowd member* — each member included in the serialized output

### Acceptance criteria

1. **WHEN** the GM saves
   **THEN** the full *crowd* hierarchy is serialized to JSON and written atomically to the *crowd repository* file
   **AND** no *crowd* or *crowd member* data is lost
   **Evidence:** story-map.md — "Save Crowd Collection to Repository"; CrowdRepository.SaveCrowdCollection

2. **WHEN** a save is triggered while a prior save is still running
   **THEN** the new save waits for the prior one to complete
   **AND** only one save runs at a time
   **Evidence:** CrowdRepository.cs — lock(lockObj)

3. **WHEN** the save fails due to a disk error
   **THEN** the in-memory *crowd* hierarchy is not affected
   **AND** the GM is notified that the save failed
   **Evidence:** CrowdRepository.cs — async error handling contract

---

## Story: Serialize Crowd Collection to JSON

**Story type:** technical

### Domain terms

- *crowd repository* — target file for serialization
- *crowd* — full nested hierarchy written to JSON
- *crowd member* — each member included with its membership state
- *option group* — per-character collection serialized with ordered member data

### Acceptance criteria

1. **WHEN** the system serializes the *crowd* hierarchy
   **THEN** every *crowd*, nested *crowd*, and *crowd member* is represented in the JSON output
   **AND** each *crowd member's* *option groups* (Identities, Abilities, Movements) are included
   **Evidence:** CrowdRepository.cs — Helper.SerializeObjectAsJSONToFile

2. **WHEN** a *crowd member* has runtime-only properties (position, spawned state, active ability)
   **THEN** those properties are excluded from the serialized output
   **AND** the JSON contains only configuration data that should persist
   **Evidence:** Character.cs — `[JsonIgnore]` on position, gamePlayer, HasBeenSpawned

3. **WHEN** the serialized JSON is subsequently deserialized
   **THEN** the restored hierarchy is structurally identical to the original with all names, orders, and *option group* contents preserved
   **Evidence:** Round-trip contract — CrowdRepository serialize/deserialize symmetry

---

## Story: Create Daily Backup of Crowd Repository

**Story type:** system

### Domain terms

- *crowd repository* — the primary JSON file; a backup copy is written before any overwrite

### Acceptance criteria

1. **WHEN** the system loads the *crowd repository* at startup
   **THEN** a date-stamped backup copy is written to the COH data directory before any writes to the primary file
   **Evidence:** story-map.md — "Create Daily Backup of Crowd Repository"; CrowdRepository.cs — TakeBackup

2. **WHEN** a backup for today already exists
   **THEN** the backup step is skipped and the existing backup is not overwritten
   **Evidence:** CrowdRepository.cs — date-based naming; skip if exists

3. **WHEN** the backup write fails
   **THEN** the primary *crowd repository* load continues normally
   **BUT** the failure is logged
   **Evidence:** CrowdRepository.cs — TakeBackup is non-blocking to main load flow

---

## Story: Store Crowd Repository in COH Data Directory

**Story type:** system

### Domain terms

- *crowd repository* — JSON file stored at a fixed path under the COH data directory
- *COH game directory* — the installation root from which the COH data directory path is derived
- *game bridge* — establishes the *COH game directory* path used for all file writes

### Acceptance criteria

1. **WHEN** the system determines the *crowd repository* file path
   **THEN** it constructs the path as `<COH game directory>/data/CharacterCrowdRepository.json`
   **AND** all reads and writes use this path consistently
   **Evidence:** CrowdRepository.cs — crowdRepositoryPath from Settings

2. **WHEN** the COH data directory does not exist
   **THEN** the system creates it before writing
   **Evidence:** KeyBindsGenerator.cs — directory creation pattern

---

## Story: Back Up Repository on Load

**Story type:** system

### Domain terms

- *crowd repository* — persistent store; a backup snapshot is taken after successful load
- *crowd member* — the state captured in the backup is the valid session-start state

### Acceptance criteria

1. **WHEN** the system successfully reads the *crowd repository* at startup
   **THEN** a backup snapshot is created before returning data to the *crowd manager*
   **AND** the backup preserves the valid state at session start
   **Evidence:** CrowdRepository.cs — TakeBackup() called inside GetCrowdCollection after successful read

2. **WHEN** the *crowd repository* read returns an empty or absent file
   **THEN** no backup is attempted
   **AND** default *crowd members* are loaded instead
   **Evidence:** CrowdRepository.cs — null guard before TakeBackup

3. **WHEN** a backup from a prior day already exists
   **THEN** a new backup with today's date is created alongside it
   **AND** earlier backups are not deleted automatically
   **Evidence:** CrowdRepository.cs — date-stamp naming preserves historical backups
