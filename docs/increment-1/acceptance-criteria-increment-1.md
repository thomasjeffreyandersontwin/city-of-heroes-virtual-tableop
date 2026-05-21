# Acceptance Criteria — Increment 1: Character and Crowd Library

> Domain terms from `docs/domain/ubiquitous-language-increment-1.md` are italicised throughout.
> Actor alternation: GM → system → GM → system.
> Format: WHEN … THEN … AND … BUT …
> Happy path + error/edge paths per story. 4–9 AC per story.

---

## Launch and Initialize Session

---

## Story: Validate City of Heroes Game Directory

**Story type:** user
### Acceptance criteria

1. **WHEN** the *application shell* starts  
   **THEN** the system reads the stored *COH game directory* path from application configuration  
   **AND** checks that the path refers to an existing, readable directory on the file system

2. **WHEN** the stored *COH game directory* path is present and resolves to a valid COH installation directory  
   **THEN** the system proceeds to load the *Prism module* without displaying the *game directory prompt*

3. **WHEN** the stored *COH game directory* path is absent from application configuration  
   **THEN** the system opens the *game directory prompt* before loading any module  
   **AND** the GM cannot access the *character crowd main workspace* until a valid path is supplied

4. **WHEN** the stored *COH game directory* path is present but does not resolve to a valid COH installation directory  
   **THEN** the system opens the *game directory prompt* with a validation feedback message identifying the problem (e.g. "Directory not found" or "Not a valid COH installation")  
   **BUT** the system does not attempt to load the *Prism module* until validation passes

5. **WHEN** validation passes  
   **THEN** the system stores the confirmed *COH game directory* path to application configuration  
   **AND** derives the *COH data directory* path (`<coh_dir>/data/`) for subsequent *crowd repository* operations

---

## Story: Prompt for Game Directory if Invalid

**Story type:** user
### Acceptance criteria

1. **WHEN** the system determines the *COH game directory* is absent or invalid  
   **THEN** the system displays the *game directory prompt* as a modal dialog blocking all other UI  
   **AND** the Continue button is disabled

2. **WHEN** the GM types or pastes a directory path into the path input field of the *game directory prompt*  
   **THEN** the system validates the entered path in real time  
   **AND** updates the validation feedback label to reflect the current state (blank when valid, error message when invalid)

3. **WHEN** the entered path resolves to a valid COH installation directory  
   **THEN** the system enables the Continue button  
   **AND** clears any error message from the validation feedback label

4. **WHEN** the GM clicks Browse in the *game directory prompt*  
   **THEN** the system opens the operating system folder picker  
   **AND** populates the path input with the selected folder path  
   **AND** re-validates immediately, enabling or disabling Continue accordingly

5. **WHEN** the GM clicks Continue while the Continue button is enabled  
   **THEN** the system dismisses the *game directory prompt*  
   **AND** proceeds with *application shell* startup (loads the *Prism module*, opens the *character crowd main workspace*)

6. **WHEN** the GM clears the path input field entirely  
   **THEN** the system disables the Continue button  
   **AND** displays an appropriate validation message ("Please enter a directory path")

7. **WHEN** the GM clicks Continue while the Continue button is disabled  
   **THEN** the system takes no action (button is non-interactive)

---

## Story: Load Prism Shell and Module

**Story type:** user
### Acceptance criteria

1. **WHEN** the *COH game directory* is confirmed valid  
   **THEN** the system initializes the Prism IoC container  
   **AND** registers the *Prism module* assembly  
   **AND** the *Prism module* registers its views, view-models, and services

2. **WHEN** the *Prism module* finishes loading  
   **THEN** the system navigates to the *character crowd main workspace* as the initial view  
   **AND** the *crowd tree* is visible and ready for interaction

3. **WHEN** the *Prism module* fails to load (assembly missing or registration error)  
   **THEN** the system displays an error dialog describing the failure  
   **AND** does not attempt to open the *character crowd main workspace*

4. **WHEN** the *Prism module* loads successfully  
   **THEN** the system triggers the *crowd repository* load sequence  
   **AND** populates the *crowd tree* before the GM can interact with it

---

## Story: Open Character Crowd Main Workspace

**Story type:** user
### Acceptance criteria

1. **WHEN** the *Prism module* finishes loading  
   **THEN** the system displays the *character crowd main workspace* with the *crowd tree* panel on the left and the tab body on the right  
   **AND** the Identities tab is active  
   **AND** the Abilities and Movements tabs are visible but greyed and non-interactive

2. **WHEN** the *crowd repository* has been loaded  
   **THEN** the *crowd tree* displays the root-level *crowds* in the order they are stored in the *crowd collection*  
   **AND** each crowd node shows the crowd name and member count

3. **WHEN** the *crowd tree* has no entries (first run, empty *crowd collection*)  
   **THEN** the *crowd tree* is empty but displayed correctly with no error state  
   **AND** the GM can immediately create a new *crowd*

---

## Manage Crowd Repository

---

## Story: Load Active Crowd Files on Startup

**Story type:** user
### Acceptance criteria

1. **WHEN** the *character crowd main workspace* opens  
   **THEN** the system locates the *crowd repository* JSON file in the *COH data directory*  
   **AND** reads the file  
   **AND** populates the *crowd tree* with the deserialized *crowd collection*

2. **WHEN** the *crowd repository* JSON file is found and readable  
   **THEN** the system creates a *daily backup* of the file before reading (if no backup exists for today's date)  
   **AND** then proceeds to deserialize the collection

3. **WHEN** the *crowd repository* JSON file does not exist (first run)  
   **THEN** the system deserializes the *default crowd collection* from the embedded resource  
   **AND** populates the *crowd tree* with those defaults  
   **AND** does not attempt to back up a non-existent file

4. **WHEN** the *crowd repository* JSON file is corrupt or unreadable  
   **THEN** the system displays an error notification identifying the problem  
   **AND** presents the GM with the option to load the most recent *daily backup* instead  
   **BUT** does not silently discard data — the GM must acknowledge the error before any state is replaced

5. **WHEN** the *crowd collection* loads successfully  
   **THEN** the *all characters crowd* is present as a protected root entry in the *crowd tree*  
   **AND** reflects every *character* in the collection alphabetically

---

## Story: Deserialize Crowd Collection from JSON

**Story type:** user
### Acceptance criteria

1. **WHEN** the system reads the *crowd repository* JSON file  
   **THEN** the system reconstructs the full *crowd* hierarchy in memory, preserving nesting order and crowd member order within each crowd

2. **WHEN** a *character* appears in multiple *crowds* in the JSON (linked member serialization)  
   **THEN** the system restores that *character* as a single in-memory instance referenced from all relevant *crowds*  
   **AND** the character is a *linked member* in all crowds where it appears

3. **WHEN** a *character* node in the JSON includes *option group* entries  
   **THEN** the system restores those *option groups* as empty or populated structures depending on the stored data  
   **AND** ensures exactly three canonical *option groups* (Identities, Abilities, Movements) exist on every restored *character*

4. **WHEN** the JSON contains a crowd with member names that are not unique  
   **THEN** the system logs a data integrity warning  
   **AND** loads the first occurrence of each duplicate name, discarding subsequent duplicates  
   **BUT** does not crash or refuse to load the file

5. **WHEN** deserialization completes  
   **THEN** the *all characters crowd* is reconstructed to reflect every *character* present in the restored hierarchy  
   **AND** its list is sorted alphabetically

---

## Story: Load Default Crowd Members from Embedded Resource

**Story type:** user
### Acceptance criteria

1. **WHEN** no *crowd repository* JSON file exists in the *COH data directory*  
   **THEN** the system deserializes the *default crowd collection* from the embedded application resource  
   **AND** populates the *crowd tree* with the default *crowds* and *characters*

2. **WHEN** the *default crowd collection* is loaded  
   **THEN** the *crowd tree* shows the default root *crowds* (e.g. Animals, Armed Forces, Civilians, Vehicles)  
   **AND** each default *crowd* contains its pre-defined *characters*

3. **WHEN** the *default crowd collection* is loaded  
   **THEN** the *all characters crowd* is populated with all default *characters* in alphabetical order

4. **WHEN** the GM saves after loading defaults  
   **THEN** the system writes the *crowd repository* JSON file to the *COH data directory*  
   **AND** subsequent launches load from that file rather than the embedded resource

---

## Manage Crowd CRUD

---

## Story: Create Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Create Crowd from the *crowd tree* toolbar or context menu  
   **THEN** the system creates a new *crowd* with a default name (e.g. "New Crowd") as a root-level entry in the *crowd collection*  
   **AND** the new crowd node appears in the *crowd tree* with its name field in inline-edit mode

2. **WHEN** the GM types a name and confirms (Enter or click away)  
   **THEN** the system assigns the typed name to the new *crowd*  
   **AND** collapses the inline edit  
   **AND** the new crowd node remains selected in the *crowd tree*

3. **WHEN** the GM confirms a name that is already used by a sibling *crowd* at the same level  
   **THEN** the system rejects the name  
   **AND** keeps the inline-edit active with a validation message ("A crowd with this name already exists")

4. **WHEN** the GM presses Escape during inline edit of the new crowd name  
   **THEN** the system removes the newly created *crowd* (cancels creation)  
   **AND** restores the prior selection in the *crowd tree*

5. **WHEN** a new *crowd* is created  
   **THEN** the *crowd collection* is marked dirty  
   **AND** the GM can immediately create *characters* within it or nest it inside another *crowd*

---

## Story: Rename Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Rename on a *crowd* node in the *crowd tree* (double-click, context menu, or F2)  
   **THEN** the system places the crowd name into inline-edit mode

2. **WHEN** the GM types a new name and confirms  
   **THEN** the system renames the *crowd*  
   **AND** updates the *crowd tree* node to show the new name  
   **AND** marks the *crowd collection* dirty

3. **WHEN** the GM confirms a new name that already exists among the crowd's siblings  
   **THEN** the system rejects the rename  
   **AND** keeps the inline-edit active with a validation message ("A crowd with this name already exists")

4. **WHEN** the GM presses Escape during rename  
   **THEN** the system cancels the rename and restores the original name  
   **AND** the *crowd collection* is not marked dirty

5. **WHEN** the *all characters crowd* is targeted for rename  
   **THEN** the system ignores the rename attempt — the rename action is unavailable on the *all characters crowd*

---

## Story: Delete Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Delete on a *crowd* node in the *crowd tree*  
   **THEN** the system displays a confirmation prompt: "Delete crowd '[name]' and all its members? This cannot be undone."

2. **WHEN** the GM confirms deletion  
   **THEN** the system removes the *crowd* and all its *crowd members* (characters and nested crowds) from the *crowd collection*  
   **AND** updates the *crowd tree* removing the node  
   **AND** marks the *crowd collection* dirty

3. **WHEN** the deleted *crowd* contains *linked members* that also appear in other *crowds*  
   **THEN** the system removes those *linked member* entries from the deleted *crowd*  
   **BUT** leaves the *character* and its links in the other *crowds* intact

4. **WHEN** the GM cancels the deletion confirmation  
   **THEN** the system takes no action and the *crowd* remains unchanged

5. **WHEN** the GM attempts to delete the *all characters crowd*  
   **THEN** the system ignores the request — the delete action is unavailable on the *all characters crowd*

6. **WHEN** a *crowd* is deleted  
   **THEN** the *all characters crowd* is updated to reflect only the *characters* remaining in the repository

---

## Story: Nest Crowd inside Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM drag-drops a *crowd* node onto another *crowd* node in the *crowd tree*  
   **THEN** the system makes the dragged *crowd* a child of the target *crowd*  
   **AND** removes it from its previous parent (or from the root level)  
   **AND** the *crowd tree* updates to show the dragged crowd as a nested child node

2. **WHEN** the *crowd tree* is refreshed after nesting  
   **THEN** the parent *crowd* node is expanded to show the newly nested child  
   **AND** the child crowd's member count appears correctly

3. **WHEN** the GM attempts to drag a *crowd* onto itself or onto one of its own descendants  
   **THEN** the system rejects the operation (a crowd cannot be its own ancestor)  
   **AND** the *crowd tree* is unchanged

4. **WHEN** a *crowd* is nested  
   **THEN** the *crowd collection* is marked dirty  
   **AND** the nested crowd and its members remain part of the *crowd repository*

5. **WHEN** the GM nests a *crowd* whose name conflicts with an existing sibling in the target parent  
   **THEN** the system rejects the nesting operation  
   **AND** notifies the GM that a crowd with that name already exists at that level

---

## Manage Character CRUD

---

## Story: Create Character in Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Create Character with a *crowd* selected in the *crowd tree*  
   **THEN** the system creates a new *character* within the selected *crowd* with a default name (e.g. "New Character")  
   **AND** the new character node appears in the *crowd tree* with its name in inline-edit mode  
   **AND** the *all characters crowd* is updated to include the new *character*

2. **WHEN** the GM types a name and confirms  
   **THEN** the system assigns the name  
   **AND** the three canonical empty *option groups* (Identities, Abilities, Movements) are created on the *character*  
   **AND** the node collapses to show the character name

3. **WHEN** the GM confirms a name already used by a sibling in the same *crowd*  
   **THEN** the system rejects the name  
   **AND** keeps the inline-edit active with a validation message

4. **WHEN** the GM presses Escape during inline edit of the new character name  
   **THEN** the system cancels creation and removes the provisional character node

5. **WHEN** a new *character* is created  
   **THEN** the *crowd collection* is marked dirty

---

## Story: Rename Character

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Rename on a *character* node (double-click, context menu, or F2)  
   **THEN** the system places the character name into inline-edit mode

2. **WHEN** the GM types a new name and confirms  
   **THEN** the system renames the *character*  
   **AND** the rename propagates to all *crowds* in which this *character* appears (including *linked members*)  
   **AND** the *all characters crowd* is updated to reflect the new name and re-sorted

3. **WHEN** the GM confirms a name that already exists in any *crowd* the *character* belongs to  
   **THEN** the system rejects the rename  
   **AND** keeps inline-edit active with a validation message

4. **WHEN** the GM presses Escape during rename  
   **THEN** the system cancels the rename; the original name is restored everywhere

---

## Story: Delete Character from Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Delete on a *character* node in the *crowd tree*  
   **THEN** the system removes the *character* from the containing *crowd*

2. **WHEN** the deleted *character* is a *linked member* in other *crowds*  
   **THEN** the system removes the *linked member* entry from all *crowds*  
   **AND** removes the *character* from the *all characters crowd*  
   **AND** the *character* no longer exists in the *crowd repository*

3. **WHEN** the deleted *character* exists only in one *crowd* (not linked)  
   **THEN** the system removes the character node from the *crowd tree*  
   **AND** removes the *character* from the *all characters crowd*

4. **WHEN** a *character* is deleted  
   **THEN** the *crowd collection* is marked dirty

5. **WHEN** the GM invokes Delete on a *character* that is the only remaining member of its *crowd*  
   **THEN** the system deletes the *character*  
   **AND** the parent *crowd* node remains in the *crowd tree* as an empty crowd

---

## Manage Clipboard and Structural Operations

---

## Story: Clone Character

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Clone on a *character* node in the *crowd tree*  
   **THEN** the system creates a deep-copy *character* in the same *crowd* as the original  
   **AND** assigns a unique name (original name + " (Copy)" if available, or original name + " 2", " 3", etc.)  
   **AND** the new character node appears in the *crowd tree* immediately below the original

2. **WHEN** the cloned *character* is created  
   **THEN** it is fully independent — no shared state with the original  
   **AND** modifying the clone (rename, add identity) does not affect the original

3. **WHEN** the original *character* is a *linked member* in multiple *crowds*  
   **THEN** the clone is created only in the *crowd* where the Clone action was invoked  
   **AND** the clone is not automatically linked into any other *crowd*

4. **WHEN** a *character* is cloned  
   **THEN** the *all characters crowd* is updated to include the new clone  
   **AND** the *crowd collection* is marked dirty

---

## Story: Cut Character to Clipboard

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Cut on a *character* node in the *crowd tree*  
   **THEN** the system immediately removes the *character* from its current *crowd*  
   **AND** holds it on the *clipboard*  
   **AND** the character node is removed from the *crowd tree* at the source

2. **WHEN** the GM pastes the *clipboard* contents into a target *crowd*  
   **THEN** the system places the cut *character* into the target *crowd*  
   **AND** clears the *clipboard*  
   **AND** the *crowd tree* shows the *character* under its new *crowd*

3. **WHEN** the GM performs a Cut and then performs another Cut before pasting  
   **THEN** the system replaces the *clipboard* contents with the new cut item  
   **AND** the previously cut item is discarded (lost)

4. **WHEN** the GM invokes Cut on a *character* that is a *linked member* in multiple *crowds*  
   **THEN** the system removes the *character* entry from the current *crowd* only  
   **AND** the *character* (and its links in other crowds) remains intact in those other *crowds*  
   **BUT** the *clipboard* holds the entry for the source crowd's membership

5. **WHEN** the *character* is cut  
   **THEN** the *crowd collection* is marked dirty immediately (the cut crowd no longer contains the member)

---

## Story: Link Character across Crowds

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Link on a *character* node and selects a target *crowd*  
   **THEN** the system adds the *character* as a *linked member* in the target *crowd*  
   **AND** the *character* node appears in the target *crowd* in the *crowd tree* with a link indicator

2. **WHEN** the *character* appears in the target *crowd* as a *linked member*  
   **THEN** renaming the *character* from either *crowd* renames it everywhere  
   **AND** both *crowd* nodes in the *crowd tree* update to show the new name

3. **WHEN** the GM attempts to link a *character* into a *crowd* where it already exists  
   **THEN** the system rejects the operation (duplicate member names are not allowed)  
   **AND** notifies the GM

4. **WHEN** a link is created  
   **THEN** the *crowd collection* is marked dirty

---

## Story: Clone-Link Character

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Clone-Link on a *character* node  
   **THEN** the system creates a deep-copy *character* (as per clone) in the same *crowd* as the original  
   **AND** immediately adds the clone as a *linked member* in a specified target *crowd*  
   **AND** both the source and target *crowd* nodes show the clone with a link indicator

2. **WHEN** the clone-link is created  
   **THEN** the original *character* is unaffected (still exists in its *crowd*)  
   **AND** the cloned *character* is independent from the original but linked between the source and target *crowds*

3. **WHEN** a clone-link is created  
   **THEN** the *crowd collection* is marked dirty  
   **AND** the *all characters crowd* is updated to include the new clone

---

## Story: Flatten-Copy Crowd into Numbered Characters

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Flatten-Copy on a *crowd*  
   **THEN** the system replaces each *character*-level member of that *crowd* with an independently numbered deep-copy (e.g. "Guard 1", "Guard 2", "Guard 3")  
   **AND** all resulting copies are fully independent — no shared state or links

2. **WHEN** the flattened *crowd* contains *linked members* referencing *characters* in other *crowds*  
   **THEN** the numbered copies in the flattened crowd break those links  
   **AND** the linked entries in other *crowds* are unaffected

3. **WHEN** the flattened *crowd* contains nested *crowds*  
   **THEN** nested *crowds* are left in place  
   **AND** only the character-level direct members are numbered and replaced

4. **WHEN** flatten-copy completes  
   **THEN** the *crowd tree* refreshes showing the numbered characters  
   **AND** the *all characters crowd* is updated to include the new numbered copies and remove the original members if they were not linked elsewhere  
   **AND** the *crowd collection* is marked dirty

---

## Story: Clone Memberships to Another Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Clone Memberships from a source *crowd* and selects a target *crowd*  
   **THEN** the system adds each direct *crowd member* of the source *crowd* as a *linked member* in the target *crowd*  
   **AND** the source *crowd* is unchanged

2. **WHEN** a *crowd member* from the source *crowd* already exists in the target *crowd*  
   **THEN** the system skips that member (no duplicate)  
   **AND** continues cloning the remaining members

3. **WHEN** clone memberships completes  
   **THEN** the target *crowd* contains its original members plus the newly linked copies  
   **AND** the *crowd tree* refreshes to show the target crowd's updated member list  
   **AND** the *crowd collection* is marked dirty

---

## Story: Drag-Drop Character between Crowds

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM drags a *character* node from one *crowd* and drops it onto another *crowd* in the *crowd tree*  
   **THEN** the system moves the *character* to the target *crowd*  
   **AND** removes the character node from the source *crowd*  
   **AND** the *crowd tree* shows the *character* under its new *crowd*

2. **WHEN** the dragged *character* is a *linked member* in other *crowds*  
   **THEN** the system moves the entry from the source *crowd* into the target *crowd*  
   **AND** the *character's* links in other *crowds* are unaffected

3. **WHEN** the GM drops a *character* onto a *crowd* where a *crowd member* with the same name already exists  
   **THEN** the system rejects the drag-drop  
   **AND** the *character* returns to its original *crowd*  
   **AND** the GM is notified of the name conflict

4. **WHEN** the drag-drop completes successfully  
   **THEN** the *crowd collection* is marked dirty

---

## Browse and Filter

---

## Story: Filter Characters by Name

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM types text in the filter bar above the *crowd tree*  
   **THEN** the system applies a case-insensitive substring match against all *crowd member* names in real time  
   **AND** the *crowd tree* collapses to show only nodes whose names contain the filter text (or who are ancestors of matching nodes)

2. **WHEN** a *crowd* has no members matching the filter  
   **THEN** the *crowd* node is hidden in the filtered view

3. **WHEN** a *crowd* has at least one matching *crowd member*  
   **THEN** the *crowd* node is shown and expanded to expose the matching members

4. **WHEN** the GM clears the filter bar (or clicks the × clear button)  
   **THEN** the *crowd tree* restores all nodes to their prior expand/collapse state

5. **WHEN** the filter matches zero entries across the entire *crowd collection*  
   **THEN** the *crowd tree* shows an empty state message ("No characters match")  
   **AND** the clear button is still available

---

## Story: Browse Crowds by Concept

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM selects the By Concept *browse mode*  
   **THEN** the *crowd tree* reorganizes to show root-level concept category nodes (Animals, Armed Forces, Civilians, Vehicles, Supernatural, etc.)  
   **AND** each category node contains the *crowds* tagged with that concept

2. **WHEN** the GM expands a concept category node  
   **THEN** the *crowds* belonging to that concept are listed as children  
   **AND** each crowd shows its name and member count as normal

3. **WHEN** a *crowd* has no concept tag  
   **THEN** it appears under an "Uncategorized" or "Other" node in the By Concept view

4. **WHEN** the GM switches from By Concept to another *browse mode*  
   **THEN** the *crowd tree* re-renders using the new mode's grouping  
   **AND** the current filter text remains active and is applied to the new view

---

## Story: Browse Crowds by Gangs, Crews, and Squads

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM selects the By Gangs, Crews, and Squads *browse mode*  
   **THEN** the *crowd tree* shows only *crowds* tagged as gangs, crews, or squads  
   **AND** groups them under sub-headings (Gangs, Crews, Squads) as applicable

2. **WHEN** the GM expands a gang/crew/squad group node  
   **THEN** the *crowds* in that group are listed as children

3. **WHEN** no *crowds* are tagged as gang, crew, or squad  
   **THEN** the view shows an empty state message ("No gangs, crews, or squads defined")

---

## Story: Browse Crowds by COH Structure

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM selects the By COH Structure *browse mode*  
   **THEN** the *crowd tree* shows *crowds* organized according to City of Heroes faction/group hierarchy (e.g. Villain Groups, Hero Groups, Neutral)  
   **AND** each top-level faction node contains the *crowds* tagged with that faction

2. **WHEN** the GM expands a faction node  
   **THEN** the *crowds* in that faction are listed as children

3. **WHEN** a *crowd* has no COH structure tag  
   **THEN** it appears under an "Untagged" or "Other" node

---

## Story: Browse All Characters Crowd

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM selects the All Characters *browse mode*  
   **THEN** the *crowd tree* shows the *all characters crowd* as the sole root entry  
   **AND** the *all characters crowd* contains every *character* in the *crowd repository* as a flat alphabetically sorted list

2. **WHEN** the GM expands the *all characters crowd* node  
   **THEN** every *character* in the repository appears as a child node in alphabetical order

3. **WHEN** a new *character* is created in any *crowd* while the All Characters view is active  
   **THEN** the *all characters crowd* immediately reflects the new *character* in its sorted position

4. **WHEN** the GM attempts to delete or rename the *all characters crowd* node  
   **THEN** the system ignores the action — those operations are unavailable on the *all characters crowd*

---

## Save and Persist

---

## Story: Save Crowd Collection to Repository

**Story type:** user
### Acceptance criteria

1. **WHEN** the GM invokes Save (e.g. Ctrl+S or a toolbar Save button)  
   **THEN** the system serializes the current *crowd collection* to JSON  
   **AND** writes the result to the *crowd repository* file in the *COH data directory*  
   **AND** clears the dirty flag  
   **AND** provides a visible confirmation (e.g. status bar message "Saved")

2. **WHEN** the *crowd collection* has no unsaved changes (not dirty)  
   **THEN** the save operation completes silently without writing to disk  
   **AND** no error is shown

3. **WHEN** the save fails (e.g. disk full, permission denied)  
   **THEN** the system displays an error dialog describing the failure  
   **AND** the *crowd collection* remains marked dirty

4. **WHEN** the GM closes the application with unsaved changes  
   **THEN** the system prompts: "You have unsaved changes. Save before closing?"  
   **AND** offers Save, Discard, and Cancel options

---

## Story: Serialize Crowd Collection to JSON

**Story type:** user
### Acceptance criteria

1. **WHEN** the system serializes the *crowd collection*  
   **THEN** the output JSON preserves the full hierarchy: root crowds, nested crowds, and their members in insertion order

2. **WHEN** a *character* is a *linked member* in multiple *crowds*  
   **THEN** the JSON encodes the *character* once and uses a reference (by name or GUID) in each *crowd* where it appears  
   **AND** deserialization restores the *linked member* relationship correctly

3. **WHEN** the serialized JSON is written to the *COH data directory*  
   **THEN** the file is UTF-8 encoded  
   **AND** the file path is `<coh_dir>/data/<repository filename>`

4. **WHEN** the *crowd collection* contains the *all characters crowd*  
   **THEN** the *all characters crowd* is not written to the JSON as a persisted crowd — it is reconstructed on deserialization from the remaining crowds  
   **AND** is never treated as a user-authored *crowd* in the serialized form

---

## Story: Create Daily Backup of Crowd Repository

**Story type:** user
### Acceptance criteria

1. **WHEN** the system saves the *crowd repository* and a backup for today's date does not yet exist  
   **THEN** the system copies the current valid JSON file to a date-stamped backup filename (e.g. `crowds_2026-05-17.json`) in the *COH data directory*  
   **AND** then overwrites the active file with the new serialized data

2. **WHEN** a backup for today's date already exists  
   **THEN** the system does not create another backup for the same date  
   **AND** proceeds directly to overwriting the active file

3. **WHEN** the backup copy fails (permission denied, disk full)  
   **THEN** the system notifies the GM of the backup failure  
   **AND** still attempts to write the active file (save should not be blocked by backup failure)  
   **BUT** warns the GM that the backup was not created

---

## Story: Store Crowd Repository in COH Data Directory

**Story type:** user
### Acceptance criteria

1. **WHEN** the *crowd repository* is saved  
   **THEN** the JSON file is written to `<coh_dir>/data/` where `<coh_dir>` is the confirmed *COH game directory*

2. **WHEN** the *COH data directory* does not exist  
   **THEN** the system creates the directory (and any required parents) before writing the file

3. **WHEN** the *crowd repository* file path is computed  
   **THEN** the path is deterministic — the same path on every save — so that deserialization on next launch reads the same file

---

## Story: Back Up Repository on Load

**Story type:** user
### Acceptance criteria

1. **WHEN** the system opens the *crowd repository* JSON file at startup  
   **THEN** before reading the file, the system creates a *daily backup* (if one does not already exist for today)  
   **AND** then reads and deserializes the file

2. **WHEN** the pre-load backup succeeds  
   **THEN** the GM has a safe recovery point in case the read or deserialization process corrupts or truncates the file

3. **WHEN** the pre-load backup fails  
   **THEN** the system logs the failure  
   **AND** proceeds with loading the file  
   **AND** notifies the GM that the backup was not created before load

4. **WHEN** both backup-on-load and backup-on-save occur on the same calendar day  
   **THEN** only one backup file exists for that day (the first backup written wins; subsequent backup attempts on the same date are skipped)


