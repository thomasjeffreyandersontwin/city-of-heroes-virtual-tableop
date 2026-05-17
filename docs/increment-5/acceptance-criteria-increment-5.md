# Acceptance Criteria — Increment 5: Roster and Desktop Interaction

> Scope: all 33 stories for the Roster and Desktop Interaction increment. Domain terms are sourced from `docs/domain/ubiquitous-language-increment-5.md` and prior increment ULs.

---

## Story: Query Hovered NPC Info from Game

**Domain terms** (vocabulary for this story's AC):
- *Game State Query* — the DLL-backed observation service that reads live game data
- *Hovered NPC Info* — NPC name and identity data returned when the mouse hovers over an NPC in the game viewport
- *Game Bridge* — the DLL bridge used to call the HookCostume DLL

1. **WHEN** the GM's mouse pointer hovers over a visible NPC entity in the COH game viewport  
   **THEN** the application invokes the *Game State Query* via the *Game Bridge* DLL  
   **AND** the *Hovered NPC Info* (NPC name and identity) is returned and made available to the caller

2. **WHEN** the GM's mouse is not hovering over any NPC entity in the game viewport  
   **THEN** the *Game State Query* returns an empty *Hovered NPC Info* result  
   **BUT** no error is raised and the calling service receives the empty signal without failure

3. **WHEN** the *Game Bridge* is not initialized or the COH game client is not running  
   **THEN** the *Game State Query* returns an unavailable signal to the caller  
   **BUT** no NPC name or identity data is fabricated or returned as valid

4. **WHEN** the GM moves the mouse from one NPC to another in the game viewport  
   **THEN** the *Hovered NPC Info* updates to reflect the NPC now under the cursor  
   **AND** the previous NPC's data is discarded; only the current hover target is returned

5. **WHEN** the *Hovered NPC Info* is queried in rapid succession by the application  
   **THEN** each query returns the NPC under the cursor at the time of that query call  
   **BUT** no query blocks subsequent queries; each returns independently and promptly

---

## Story: Query Mouse XYZ Position in Game World

**Domain terms**:
- *Mouse XYZ Position* — three-dimensional world-space coordinates of the GM's mouse cursor in the COH game world
- *Game State Query* — the DLL-backed observation service
- *Game Bridge* — the DLL bridge

1. **WHEN** the application requests the *Mouse XYZ Position*  
   **THEN** the *Game State Query* calls the *Game Bridge* DLL and returns the world-space X/Y/Z coordinate triple of the mouse cursor's current position in the COH game world  
   **AND** the returned coordinates are immediately available for use in placement operations

2. **WHEN** the COH game window does not have input focus at the time of the query  
   **THEN** the *Game State Query* returns the *Mouse XYZ Position* but marks it as potentially stale  
   **BUT** no coordinates are silently treated as authoritative when focus is absent

3. **WHEN** the *Game Bridge* is unavailable  
   **THEN** the *Mouse XYZ Position* query returns an unavailable signal  
   **BUT** no zero-coordinate result is returned as if it were a valid world position

4. **WHEN** the GM positions the mouse at different in-game locations  
   **THEN** each *Mouse XYZ Position* query returns distinct coordinates corresponding to each mouse placement  
   **AND** the coordinates reflect the full three dimensions of the game world at that cursor point

---

## Story: Check Game Done State

**Domain terms**:
- *Game Done State* — Boolean flag indicating whether the COH game session has ended
- *Game State Query* — the DLL-backed observation service
- *Roster Entry* — a character's session record in the roster
- *Spawned State* — per-roster-entry flag for in-game NPC presence
- *Desktop Overlay* — visual interaction layer rendered atop the COH game view

1. **WHEN** the application polls the *Game Done State* and the COH session is still active  
   **THEN** the *Game State Query* returns false  
   **AND** no *Roster Entry* is affected and the *Desktop Overlay* remains unchanged

2. **WHEN** the *Game Done State* becomes true (map unload, disconnect, or client shutdown)  
   **THEN** the *Game State Query* returns true  
   **AND** all *Roster Entries* have their *Spawned State* set to false  
   **AND** all *Character Overlays* are removed from the *Desktop Overlay*

3. **WHEN** *Game Done State* is true  
   **THEN** no spawn, move, or other game command is issued until a new game session is established  
   **BUT** the *Roster* entries themselves are preserved; only *Spawned State* is cleared

4. **WHEN** a new game session is established after *Game Done State* was true  
   **THEN** the *Game Done State* flag is reset to false  
   **AND** the GM may begin spawning characters from the *Roster* again

5. **WHEN** the application cannot reach the *Game Bridge* to poll *Game Done State*  
   **THEN** the application treats the state as indeterminate and suspends game commands  
   **BUT** the *Roster* and its *Roster Entries* are not cleared

---

## Story: Split Oversized Command Chains for Execution

**Domain terms**:
- *Command Chain* — ordered sequence of game commands for delivery to the *Game Bridge*
- *Oversized Command Chain* — a *Command Chain* exceeding the COH engine's per-execution limit
- *Game Bridge* — the DLL bridge for command delivery

1. **WHEN** the application assembles a *Command Chain* for delivery  
   **THEN** the payload size and command count are measured against the known COH engine limit before delivery is attempted

2. **WHEN** the *Command Chain* is within the COH limit  
   **THEN** it is delivered to the *Game Bridge* as a single batch  
   **AND** the COH engine processes all commands in the stated order

3. **WHEN** the *Command Chain* constitutes an *Oversized Command Chain*  
   **THEN** it is split into two or more sub-chains, each within the COH limit  
   **AND** each sub-chain is delivered to the *Game Bridge* in sequence, completing one before the next begins  
   **AND** no command from the original chain is omitted or reordered

4. **WHEN** splitting is required and one sub-chain delivery fails  
   **THEN** the application surfaces a delivery-error signal and does not attempt the remaining sub-chains  
   **BUT** sub-chains already delivered are not reversed; commands already executed remain in effect

5. **WHEN** splitting would produce a sub-chain of zero commands  
   **THEN** that sub-chain is not delivered  
   **BUT** subsequent non-empty sub-chains are still delivered in order

---

## Story: Close Game Bridge on Shutdown

**Domain terms**:
- *Game Bridge* — the DLL bridge managing COH game communication
- *Spawned NPC* — in-game character entity produced by the game bridge
- *Game State Query* — the DLL-backed observation service

1. **WHEN** the GM closes the application or triggers application shutdown  
   **THEN** the *Game Bridge* close sequence is initiated before the process exits  
   **AND** all active DLL handles and HookCostume connections are released  
   **AND** the *Game State Query* poll loop is stopped

2. **WHEN** the *Game Bridge* close sequence completes  
   **THEN** no further game commands are issued  
   **AND** the application process exits cleanly without leaving orphaned DLL resources

3. **WHEN** the *Game Bridge* is already in an uninitialized state at shutdown  
   **THEN** the close sequence completes without error  
   **BUT** no attempt is made to release handles that were never acquired

4. **WHEN** the application crashes unexpectedly without a normal shutdown  
   **THEN** COH-side *Spawned NPCs* remain in game (no implicit despawn on abnormal exit)  
   **BUT** the HookCostume DLL is unloaded by the OS process cleanup

---

## Story: Execute Load Map Command

**Domain terms**:
- *Game Bridge* — the DLL bridge for game command delivery
- *Game Done State* — session-end flag monitored after map transitions

1. **WHEN** the GM triggers a Load Map action for a designated COH map  
   **THEN** the *Game Bridge* issues the load-map slash command with the specified map identifier  
   **AND** the COH client begins transitioning to the designated map

2. **WHEN** the map transition completes in the COH client  
   **THEN** the *Game Done State* is polled to confirm the new session is active  
   **AND** the application proceeds with the new session state

3. **WHEN** the specified map identifier is invalid or unavailable in COH  
   **THEN** the *Game Bridge* issues the command and the COH engine rejects it  
   **AND** the application receives an error signal and surfaces feedback to the GM  
   **BUT** no application state is modified as a result of the failed map load

4. **WHEN** the *Game Bridge* is not initialized when the Load Map action is triggered  
   **THEN** the command is not issued  
   **AND** the GM sees that the map load cannot proceed without an active game session

---

## Story: Write Pop-Up Menu Files to COH Menus Directory

**Domain terms**:
- *Pop-Up Menu* — COH-native menu definition file
- *COH Menus Directory* — file-system path where COH reads pop-up menu files
- *COH Game Directory* — validated installation root

1. **WHEN** the application writes a *Pop-Up Menu*  
   **THEN** the menu definition text file is written to the *COH Menus Directory*  
   **AND** the file is immediately available on disk for a subsequent load command

2. **WHEN** a *Pop-Up Menu* file already exists at the target path in the *COH Menus Directory*  
   **THEN** it is overwritten with the new content  
   **AND** the most recently written version is the one the game will load on the next load command

3. **WHEN** the *COH Menus Directory* does not exist or is not writable  
   **THEN** the write fails and the application surfaces an error to the GM  
   **BUT** no partial file is left in the directory

4. **WHEN** the write succeeds  
   **THEN** the written file path is confirmed and the application is ready to issue a load-pop-up-menu command  
   **BUT** the COH game client does not automatically pick up the file until a load command is explicitly issued

---

## Story: Load Pop-Up Menu in Game

**Domain terms**:
- *Pop-Up Menu* — COH-native menu definition file written to the *COH Menus Directory*
- *Game Bridge* — the DLL bridge for game command delivery

1. **WHEN** the application issues the load-pop-up-menu command for a named *Pop-Up Menu*  
   **THEN** the *Game Bridge* delivers the command to the COH client  
   **AND** the COH client loads the menu definition and makes its entries accessible from the in-game HUD

2. **WHEN** the load command is issued before the *Pop-Up Menu* file has been written to the *COH Menus Directory*  
   **THEN** the COH client attempts the load and fails silently or produces a COH-side error  
   **AND** the application surfaces a warning that the menu file must be written before loading

3. **WHEN** the COH client is not running when the load command is issued  
   **THEN** the *Game Bridge* reports an unavailable signal  
   **BUT** no menu state in the application is changed

4. **WHEN** the same *Pop-Up Menu* is loaded a second time after an update  
   **THEN** the COH client replaces the previously loaded menu version with the updated one  
   **AND** the new entries are immediately active in the game HUD

---

## Story: Deploy Area Attack Pop-Up Menu

**Domain terms**:
- *Area Attack Pop-Up Menu* — the specific pop-up menu for area attack target designation
- *Pop-Up Menu* — COH-native menu definition file
- *COH Menus Directory* — file-system path for COH menu files
- *Game Bridge* — the DLL bridge

1. **WHEN** a game session is initialized  
   **THEN** the *Area Attack Pop-Up Menu* file is written to the *COH Menus Directory*  
   **AND** the load-pop-up-menu command is issued via the *Game Bridge* to activate it in the COH client  
   **AND** the area attack target designation entries become accessible from the COH HUD

2. **WHEN** the write or load step fails during session initialization  
   **THEN** the application surfaces a warning that the *Area Attack Pop-Up Menu* is unavailable  
   **AND** the rest of the session initialization continues; area attack designation from the HUD is simply unavailable until the menu is deployed

3. **WHEN** the *Area Attack Pop-Up Menu* is already deployed from a prior session start and the file is unchanged  
   **THEN** the application writes the file again and reloads it to ensure the current session has the active version  
   **BUT** the overwrite and reload do not cause errors if the menu content is identical

4. **WHEN** the GM designates an area attack center target from within the COH game HUD  
   **THEN** the *Area Attack Pop-Up Menu* entries are present and respond as defined  
   **AND** the attack configuration panel in the application receives the center target designation

---

## Story: Add Character to Roster

**Domain terms**:
- *Roster* — session-scope ordered list of characters in play
- *Roster Entry* — a character's session record with name, *Spawned State*, active, and gang indicators
- *Character* — the named data entity from the crowd library
- *Spawned State* — per-roster-entry Boolean for in-game NPC presence
- *Roster Panel* — the roster list region in the desktop screen

1. **WHEN** the GM adds a *Character* to the *Roster* via the Add action in the *Roster Panel*  
   **THEN** a new *Roster Entry* is created for that character with *Spawned State* false and no active or gang indicators  
   **AND** the entry appears at the bottom of the *Roster Panel* with the character's name, spawned indicator hidden, and active indicator hidden

2. **WHEN** the GM adds a *Character* that is already present in the *Roster*  
   **THEN** the *Roster* rejects the addition with user feedback explaining the character is already on the roster  
   **BUT** no duplicate *Roster Entry* is created

3. **WHEN** the *Roster* is empty before the add  
   **THEN** the empty-roster placeholder is replaced by the new *Roster Entry*

4. **WHEN** the GM adds multiple *Characters* in sequence  
   **THEN** each appears as a separate *Roster Entry* in the *Roster Panel* in the order added  
   **AND** all entries have *Spawned State* false until explicitly spawned

5. **WHEN** the added *Character* has no identity or ability configuration  
   **THEN** the *Roster Entry* is created normally; identity and ability configuration are not required for roster membership

---

## Story: Add Crowd to Roster

**Domain terms**:
- *Roster* — session-scope character list
- *Roster Entry* — a character's session record
- *Crowd* — a named hierarchical container of characters
- *Character* — the named data entity from the crowd library
- *Spawned State* — per-roster-entry Boolean for NPC presence

1. **WHEN** the GM adds a *Crowd* to the *Roster* via the Add Crowd action  
   **THEN** each leaf *Character* in the *Crowd* (including those in nested crowds, expanded recursively) is added as a separate *Roster Entry* with *Spawned State* false  
   **AND** all new entries appear in the *Roster Panel* in crowd-member order

2. **WHEN** a *Character* in the *Crowd* is already present in the *Roster*  
   **THEN** that character is skipped with per-character feedback in the result summary  
   **AND** the remaining characters in the crowd are still added  
   **BUT** no duplicate *Roster Entries* are created

3. **WHEN** the *Crowd* contains no *Characters* (empty crowd)  
   **THEN** the Add Crowd action completes with feedback that no entries were added  
   **BUT** no error is raised

4. **WHEN** the *Crowd* contains nested *Crowds*  
   **THEN** leaf *Characters* from all nesting levels are added as *Roster Entries*  
   **AND** the nested crowd structure itself does not appear in the *Roster*; only individual characters are listed

5. **WHEN** all *Characters* in the *Crowd* are already on the *Roster*  
   **THEN** the Add Crowd action completes with feedback that all members were already present  
   **BUT** the *Roster* is unchanged

---

## Story: Spawn Character to Desktop from Roster

**Domain terms**:
- *Roster Entry* — a character's session record
- *Spawned State* — per-roster-entry Boolean for in-game NPC presence
- *Spawned NPC* — the in-game entity created by the spawn command
- *Desktop Overlay* — visual interaction layer showing spawned characters
- *Character Overlay* — per-character visual marker in the desktop overlay
- *Game Bridge* — the DLL bridge for game commands
- *Roster Panel* — the roster list region

1. **WHEN** the GM selects a *Roster Entry* and triggers the Spawn action in the *Roster Panel*  
   **THEN** the *Game Bridge* issues a spawn NPC command for that character  
   **AND** the *Roster Entry's* *Spawned State* is set to true  
   **AND** a *Character Overlay* for that character appears in the *Desktop Overlay* at the character's initial in-game position

2. **WHEN** the character's *Spawned State* is already true when Spawn is triggered  
   **THEN** the spawn action is a no-op with user feedback that the character is already in game  
   **BUT** no duplicate *Spawned NPC* is created

3. **WHEN** the *Game Bridge* fails to complete the spawn command  
   **THEN** the *Roster Entry's* *Spawned State* remains false  
   **AND** no *Character Overlay* is added to the *Desktop Overlay*  
   **AND** the GM sees an error signal in the *Roster Panel*

4. **WHEN** the spawn succeeds  
   **THEN** the spawned indicator is shown on the *Roster Entry* in the *Roster Panel*  
   **AND** the *Character Overlay* shows a spawned status indicator in the *Desktop Overlay*

5. **WHEN** the GM spawns multiple characters in sequence  
   **THEN** each spawns independently; success or failure for one does not affect the others  
   **AND** each successful spawn adds a distinct *Character Overlay* to the *Desktop Overlay*

---

## Story: Remove Character from Roster

**Domain terms**:
- *Roster* — session-scope character list
- *Roster Entry* — a character's session record
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Spawned NPC* — the in-game entity
- *Desktop Overlay* — visual interaction layer
- *Game Bridge* — the DLL bridge

1. **WHEN** the GM removes a *Roster Entry* whose *Spawned State* is true  
   **THEN** the *Game Bridge* issues a despawn command for the character  
   **AND** the *Character Overlay* is removed from the *Desktop Overlay*  
   **AND** the *Roster Entry* is deleted from the *Roster*

2. **WHEN** the GM removes a *Roster Entry* whose *Spawned State* is false  
   **THEN** the *Roster Entry* is deleted without issuing any game command  
   **AND** the *Roster Panel* no longer shows the character

3. **WHEN** the despawn command fails during removal  
   **THEN** the *Roster Entry* is still deleted from the *Roster*  
   **AND** the GM sees a warning that the NPC may remain in game

4. **WHEN** the removed character was part of an active *Gang Mode* group  
   **THEN** the gang is deactivated for all members before the entry is removed  
   **AND** the gang indicators are cleared from all remaining *Roster Entries* in that gang

5. **WHEN** the GM removes the last *Roster Entry*  
   **THEN** the *Roster Panel* shows the empty-roster placeholder  
   **AND** the *Desktop Overlay* shows no *Character Overlays*

---

## Story: Clear Character from Desktop

**Domain terms**:
- *Roster Entry* — a character's session record
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Spawned NPC* — the in-game entity
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Game Bridge* — the DLL bridge

1. **WHEN** the GM triggers Clear on a *Roster Entry* whose *Spawned State* is true  
   **THEN** the *Game Bridge* issues a despawn command for that character's *Spawned NPC*  
   **AND** the *Roster Entry's* *Spawned State* is set to false  
   **AND** the spawned indicator is hidden in the *Roster Panel*  
   **AND** the *Character Overlay* is removed from the *Desktop Overlay*

2. **WHEN** the clear completes  
   **THEN** the *Roster Entry* remains in the *Roster Panel* with *Spawned State* false  
   **BUT** the character is no longer visible in game

3. **WHEN** the GM triggers Clear on a *Roster Entry* whose *Spawned State* is already false  
   **THEN** the action is a no-op with user feedback  
   **BUT** no despawn command is issued

4. **WHEN** the despawn command fails during clear  
   **THEN** the *Spawned State* remains true and the *Character Overlay* remains visible  
   **AND** the GM sees an error signal

5. **WHEN** the cleared character was the *Active Character*  
   **THEN** the active indicator is removed from the *Roster Entry* as part of the clear  
   **AND** no character becomes the *Active Character* automatically

---

## Story: Activate Character (mark as active turn)

**Domain terms**:
- *Active Character* — a *Roster Entry* marked as holding the current turn
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer

1. **WHEN** the GM triggers the Activate action on a *Roster Entry* in the *Roster Panel*  
   **THEN** that entry is marked as the *Active Character*  
   **AND** an active indicator appears on the *Roster Entry* in the *Roster Panel*  
   **AND** a distinct active status indicator appears on the matching *Character Overlay* in the *Desktop Overlay*

2. **WHEN** a different *Roster Entry* is already the *Active Character* when the GM activates a new one  
   **THEN** the previously active entry loses its active indicator  
   **AND** the newly activated entry gains the active indicator  
   **AND** only one *Active Character* exists at any time (unless *Gang Mode* applies)

3. **WHEN** the GM activates a *Roster Entry* whose *Spawned State* is false  
   **THEN** the active indicator is still applied to the *Roster Entry*  
   **AND** no *Character Overlay* status indicator is shown since the character is not in game

4. **WHEN** the *Active Character's* entry is already marked active and the GM activates it again  
   **THEN** the action is a no-op; the indicator remains and no change is made

5. **WHEN** the activated character belongs to an active *Gang Mode* group  
   **THEN** all entries in the gang are also activated collectively, overriding the single-active rule

---

## Story: Deactivate Character

**Domain terms**:
- *Active Character* — a *Roster Entry* marked as holding the current turn
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region
- *Character Overlay* — per-character visual marker

1. **WHEN** the GM triggers the Deactivate action on the *Active Character* entry in the *Roster Panel*  
   **THEN** the active indicator is removed from that *Roster Entry*  
   **AND** the active status indicator is removed from the matching *Character Overlay*  
   **AND** no other *Roster Entry* is automatically activated

2. **WHEN** the GM triggers Deactivate on a *Roster Entry* that is not currently active  
   **THEN** the action is a no-op with no indicator change  
   **BUT** no error is raised

3. **WHEN** the deactivated entry is part of an active *Gang Mode* group  
   **THEN** only that specific entry is deactivated; the other gang members retain their active indicators  
   **AND** the gang mode itself is not automatically ended by deactivating a single member

4. **WHEN** deactivation succeeds  
   **THEN** the *Roster Panel* shows no active indicator on any entry  
   **AND** no *Character Overlay* shows an active status indicator  
   **BUT** the *Roster* entries, *Spawned States*, and gang membership are unchanged

---

## Story: Activate Crowd as Gang with Gang Leader

**Domain terms**:
- *Gang Mode* — collective activation state for a crowd's roster entries
- *Gang Leader* — the designated lead roster entry in a gang
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region
- *Character Overlay* — per-character visual marker
- *Crowd* — named character group in the library

1. **WHEN** the GM triggers Activate Gang, selects a *Crowd*, and designates a *Gang Leader*  
   **THEN** all *Roster Entries* belonging to that *Crowd* are activated simultaneously  
   **AND** each participating entry shows a gang membership indicator in the *Roster Panel*  
   **AND** the *Gang Leader* entry shows a distinct leader indicator  
   **AND** matching *Character Overlays* in the *Desktop Overlay* show gang status indicators

2. **WHEN** the selected *Crowd* has one or more members not present in the *Roster*  
   **THEN** the Activate Gang action is rejected with an error listing the missing members  
   **BUT** no partial gang activation occurs; no entries are activated

3. **WHEN** no *Gang Leader* is designated before confirming gang activation  
   **THEN** the Activate Gang dialog prevents confirmation  
   **BUT** *Roster Entries* are unchanged until a leader is assigned and the action confirmed

4. **WHEN** *Gang Mode* is already active on a different *Crowd*  
   **THEN** the existing gang is deactivated before the new gang is activated  
   **AND** the previous gang's indicators are cleared from all affected entries

5. **WHEN** the gang activation succeeds  
   **THEN** the *Gang Mode* indicator appears on the *Roster Panel* header or status area  
   **AND** all activated entries are treated as collectively active for ability and movement operations

6. **WHEN** a single *Character* is selected as both the crowd and the leader  
   **THEN** *Gang Mode* activates with one member; the single entry shows both gang and leader indicators

---

## Story: Deactivate Gang

**Domain terms**:
- *Gang Mode* — collective activation state
- *Roster Entry* — a character's session record
- *Gang Leader* — the designated lead roster entry
- *Character Overlay* — per-character visual marker

1. **WHEN** the GM triggers Deactivate Gang  
   **THEN** all *Roster Entries* in the current *Gang Mode* group are marked inactive simultaneously  
   **AND** all gang membership indicators are removed from the *Roster Panel*  
   **AND** the *Gang Leader* indicator is removed  
   **AND** all matching *Character Overlays* in the *Desktop Overlay* have their gang status indicators cleared

2. **WHEN** no *Gang Mode* is currently active when Deactivate Gang is triggered  
   **THEN** the action is a no-op with user feedback  
   **BUT** no *Roster Entries* are affected

3. **WHEN** deactivation completes  
   **THEN** the *Roster* returns to single-character activation mode  
   **AND** no entry is automatically activated as *Active Character* after deactivation

4. **WHEN** some gang members have *Spawned State* false at the time of deactivation  
   **THEN** their inactive status is applied to the *Roster Entry* normally  
   **AND** no game command is issued for unspawned members

---

## Story: Select Character on Desktop via Mouse Click

**Domain terms**:
- *Character Overlay* — per-character visual marker in the desktop overlay
- *Desktop Overlay* — visual interaction layer
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region

1. **WHEN** the GM single-clicks a *Character Overlay* in the *Desktop Overlay*  
   **THEN** that *Character Overlay* displays a selection highlight  
   **AND** the matching *Roster Entry* is highlighted in the *Roster Panel*  
   **AND** any previously selected *Character Overlay* loses its selection highlight

2. **WHEN** the GM clicks empty space in the *Desktop Overlay* (not on any overlay)  
   **THEN** all current selections are cleared  
   **AND** no *Roster Entry* remains highlighted

3. **WHEN** the GM clicks a *Character Overlay* that is already selected  
   **THEN** the selection remains; no deselection occurs on repeated single-click  
   **AND** no *Multi-Select* state is entered

4. **WHEN** the GM clicks a *Character Overlay* while *Multi-Select* is active  
   **THEN** all existing multi-selections are cleared and only the clicked overlay is selected  
   **AND** the *Roster Panel* highlights only the matching *Roster Entry*

---

## Story: Multi-Select Characters

**Domain terms**:
- *Multi-Select* — state in which two or more character overlays are simultaneously selected
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region

1. **WHEN** the GM shift-clicks or ctrl-clicks a *Character Overlay* while another is already selected  
   **THEN** that overlay is added to the current selection  
   **AND** both overlays show the multi-select highlight  
   **AND** both matching *Roster Entries* are highlighted in the *Roster Panel*

2. **WHEN** the GM shift-clicks or ctrl-clicks a *Character Overlay* that is already in the multi-selection  
   **THEN** that overlay is removed from the selection  
   **AND** the remaining selected overlays retain their multi-select highlight

3. **WHEN** the GM reduces the selection to one overlay using shift/ctrl-click  
   **THEN** *Multi-Select* ends and the single remaining overlay shows a single-select highlight  
   **BUT** the remaining *Roster Entry* remains highlighted

4. **WHEN** the GM clicks without a shift/ctrl modifier while *Multi-Select* is active  
   **THEN** all multi-selections are cleared  
   **AND** only the clicked overlay becomes selected (if a *Character Overlay* was clicked)

5. **WHEN** two or more overlays are selected via *Multi-Select*  
   **THEN** a *Context Menu* triggered on any selected overlay applies to all selected characters simultaneously

---

## Story: Drag Character to New Position on Desktop

**Domain terms**:
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Movement Execution* — the service that repositions spawned NPCs
- *Saved Character Position* — the stored coordinate for a roster entry

1. **WHEN** the GM drags a *Character Overlay* to a new position in the *Desktop Overlay* and releases  
   **THEN** *Movement Execution* is invoked with the drop-point world-space coordinates as the destination  
   **AND** the *Spawned NPC* is repositioned to the new location in game  
   **AND** the *Character Overlay* moves to reflect the new in-game position

2. **WHEN** the drag releases on a point outside the game world boundary  
   **THEN** the drag is cancelled and the *Character Overlay* returns to its original position  
   **BUT** no movement command is issued for an out-of-bounds destination

3. **WHEN** the dragged character's *Spawned State* is false  
   **THEN** the drag action is not available; the overlay does not drag  
   **BUT** no error is raised

4. **WHEN** the drag completes and *Movement Execution* reports a collision or blocked path  
   **THEN** the *Spawned NPC* stops at the collision point  
   **AND** the *Character Overlay* updates to the collision-halted position

5. **WHEN** the GM drags a *Character Overlay* during an active *Multi-Select*  
   **THEN** all selected characters are moved together with relative positional offsets from the drag origin  
   **AND** each *Spawned NPC* is repositioned independently via *Movement Execution*

---

## Story: Double-Click Character to Activate

**Domain terms**:
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Active Character* — a *Roster Entry* marked as holding the current turn
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region

1. **WHEN** the GM double-clicks a *Character Overlay* in the *Desktop Overlay*  
   **THEN** the matching *Roster Entry* is marked as the *Active Character*  
   **AND** the active indicator appears on the *Roster Entry* in the *Roster Panel*  
   **AND** the active status indicator appears on the *Character Overlay*  
   **AND** any previously active entry loses its active indicator

2. **WHEN** the double-clicked *Character Overlay* already belongs to the *Active Character*  
   **THEN** the action is a no-op; the active indicator remains  
   **AND** no change is applied to any *Roster Entry*

3. **WHEN** *Gang Mode* is active when the GM double-clicks a *Character Overlay*  
   **THEN** only the double-clicked character is activated  
   **AND** the *Gang Mode* collective activation is replaced by this single-character activation

4. **WHEN** the double-click occurs on an overlay belonging to a character with *Spawned State* false  
   **THEN** the double-click is not interpreted as an activate; it has no effect  
   **AND** a *Character Overlay* cannot be double-clicked if *Spawned State* is false

---

## Story: Sync Roster Selection with Game Target

**Domain terms**:
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Memory Interface* — the process-memory read service
- *Hovered NPC Info* — NPC identity data from the game

1. **WHEN** the GM changes the current target in the COH game client  
   **THEN** the *Memory Interface* detects the target-register change  
   **AND** the *Roster Entry* matching the newly targeted character is highlighted in the *Roster Panel*  
   **AND** the matching *Character Overlay* in the *Desktop Overlay* shows a selection highlight

2. **WHEN** the GM targets a character in game that is not present in the *Roster*  
   **THEN** no *Roster Entry* is highlighted  
   **AND** no *Character Overlay* shows a selection highlight  
   **BUT** the application continues monitoring the target register

3. **WHEN** the game target changes back to a character that is in the *Roster*  
   **THEN** the previous *Roster Entry* highlight is cleared and the newly targeted one is highlighted  
   **AND** the *Desktop Overlay* selection updates to match

4. **WHEN** the game target is cleared in COH (no target)  
   **THEN** all *Roster Entry* and *Character Overlay* highlights driven by target sync are cleared  
   **AND** any GM-driven selection in the *Roster Panel* or *Desktop Overlay* is preserved independently

---

## Story: Track Spawned State per Character

**Domain terms**:
- *Spawned State* — per-roster-entry Boolean for in-game NPC presence
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region
- *Character Overlay* — per-character visual marker
- *Game Done State* — session-end Boolean flag

1. **WHEN** a character is spawned from the *Roster Panel* or *Context Menu*  
   **THEN** the *Roster Entry's* *Spawned State* is set to true  
   **AND** a spawned indicator appears on the *Roster Entry* in the *Roster Panel*

2. **WHEN** a character is cleared from the desktop or removed from the *Roster*  
   **THEN** the *Roster Entry's* *Spawned State* is set to false  
   **AND** the spawned indicator is hidden on the *Roster Entry*

3. **WHEN** *Game Done State* becomes true  
   **THEN** all *Roster Entries* have their *Spawned State* set to false simultaneously  
   **AND** all spawned indicators are hidden in the *Roster Panel*

4. **WHEN** *Spawned State* is false for a *Roster Entry*  
   **THEN** the matching *Character Overlay* is not rendered in the *Desktop Overlay*  
   **AND** drag, double-click, and movement interactions are unavailable for that entry

5. **WHEN** multiple characters have *Spawned State* true simultaneously  
   **THEN** each tracks its own flag independently  
   **AND** each shows its own spawned indicator in the *Roster Panel* and *Character Overlay* in the *Desktop Overlay*

---

## Story: Spawn Character via Context Menu

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Spawned NPC* — the in-game entity
- *Character Overlay* — per-character visual marker
- *Desktop Overlay* — visual interaction layer
- *Game Bridge* — the DLL bridge

1. **WHEN** the GM right-clicks a *Character Overlay* whose *Spawned State* is false  
   **THEN** the *Context Menu* shows the Spawn action as an available option

2. **WHEN** the GM selects Spawn from the *Context Menu*  
   **THEN** the *Game Bridge* issues a spawn command for the target character  
   **AND** the *Roster Entry's* *Spawned State* is set to true  
   **AND** the *Character Overlay* updates to show a spawned status indicator

3. **WHEN** the *Context Menu* is opened on a *Character Overlay* whose *Spawned State* is true  
   **THEN** the Spawn action is not shown in the menu  
   **BUT** all other applicable actions remain visible

4. **WHEN** the spawn command via *Context Menu* fails  
   **THEN** the *Roster Entry's* *Spawned State* remains false  
   **AND** the *Character Overlay* does not update  
   **AND** the GM sees an error signal

---

## Story: Place Character at Location

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Mouse XYZ Position* — world-space coordinate of the mouse cursor in the game world
- *Movement Execution* — service that repositions spawned NPCs
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Character Overlay* — per-character visual marker

1. **WHEN** the GM selects Place at Location from the *Context Menu* on a spawned character  
   **THEN** the *Mouse XYZ Position* at the time of the action is read from the *Game State Query*  
   **AND** *Movement Execution* is invoked to move the character to those world-space coordinates  
   **AND** the *Character Overlay* repositions in the *Desktop Overlay* to reflect the new location

2. **WHEN** the *Mouse XYZ Position* is unavailable (game window not focused)  
   **THEN** the Place at Location action surfaces a feedback message indicating the position cannot be determined  
   **BUT** no movement command is issued and the character remains at its current location

3. **WHEN** the target character's *Spawned State* is false  
   **THEN** the Place at Location action is not available in the *Context Menu*

4. **WHEN** *Movement Execution* detects a collision at the destination  
   **THEN** the character is placed at the closest valid position on the approach path  
   **AND** the *Character Overlay* reflects the halted position  
   **AND** the GM sees placement feedback indicating the adjusted destination

---

## Story: Save Character Position

**Domain terms**:
- *Saved Character Position* — stored X/Y/Z world-space coordinate for a roster entry
- *Context Menu* — right-click popup scoped to a target character
- *Memory Interface* — process-memory read service providing character position
- *Spawned State* — per-roster-entry Boolean for NPC presence
- *Roster Entry* — a character's session record

1. **WHEN** the GM selects Save Position from the *Context Menu* on a spawned character  
   **THEN** the *Memory Interface* reads the character's current X/Y/Z position  
   **AND** the coordinates are stored as the *Saved Character Position* on the *Roster Entry*  
   **AND** the GM sees confirmation that the position has been saved

2. **WHEN** a *Saved Character Position* already exists for the character  
   **THEN** it is overwritten by the new position on Save Position  
   **AND** the previously saved coordinates are discarded

3. **WHEN** the *Memory Interface* cannot read the character's position  
   **THEN** the save fails and no *Saved Character Position* is written  
   **AND** the GM sees an error signal  
   **BUT** any prior *Saved Character Position* for the character is not overwritten on failure

4. **WHEN** the target character's *Spawned State* is false  
   **THEN** Save Position is not available in the *Context Menu*  
   **BUT** any previously *Saved Character Position* remains intact

5. **WHEN** the save succeeds  
   **THEN** the *Saved Character Position* is available for subsequent Place at Location or restore operations

---

## Story: Move Camera to Target Character

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Camera Rig* — the virtual camera in the COH game world
- *Character Overlay* — per-character visual marker
- *Spawned State* — per-roster-entry Boolean

1. **WHEN** the GM selects Move Camera to Target from the *Context Menu* on a spawned character  
   **THEN** the *Camera Rig* is directed to move to the target character's current in-game position  
   **AND** the camera view in the COH game world updates to frame the target character

2. **WHEN** the *Camera Rig* is not active (not deployed)  
   **THEN** the Move Camera to Target action surfaces a feedback message that the camera rig is unavailable  
   **BUT** no camera movement command is issued

3. **WHEN** the target character's *Spawned State* is false  
   **THEN** Move Camera to Target is not available in the *Context Menu*

4. **WHEN** the move completes  
   **THEN** the *Camera Rig* is positioned at or near the character  
   **AND** subsequent maneuver or follow operations use the new camera position

---

## Story: Move Target Character to Camera

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Camera Rig* — the virtual camera in the COH game world
- *Movement Execution* — service that repositions spawned NPCs
- *Spawned State* — per-roster-entry Boolean
- *Character Overlay* — per-character visual marker

1. **WHEN** the GM selects Move Target to Camera from the *Context Menu* on a spawned character  
   **THEN** the *Camera Rig's* current world-space position is read  
   **AND** *Movement Execution* is invoked to move the character to the camera position  
   **AND** the *Character Overlay* repositions in the *Desktop Overlay* to reflect the new location

2. **WHEN** the *Camera Rig* is not active  
   **THEN** the Move Target to Camera action surfaces a feedback message  
   **BUT** no movement command is issued and the character's position is unchanged

3. **WHEN** the movement to camera position is blocked by a collision  
   **THEN** the character stops at the collision point  
   **AND** the *Character Overlay* reflects the halted position

4. **WHEN** the target character's *Spawned State* is false  
   **THEN** Move Target to Camera is not available in the *Context Menu*

---

## Story: Reset Character Orientation via Context Menu

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Movement Execution* — service that repositions and reorients spawned NPCs
- *Spawned State* — per-roster-entry Boolean

1. **WHEN** the GM selects Reset Orientation from the *Context Menu* on a spawned character  
   **THEN** *Movement Execution* writes the identity rotation matrix to process memory for that character  
   **AND** the character's facing direction is reset to the default north-facing orientation in the COH game world

2. **WHEN** the reset completes  
   **THEN** the character is facing the default direction and the orientation change is visible in game  
   **AND** no position change occurs; only the facing direction changes

3. **WHEN** the target character's *Spawned State* is false  
   **THEN** Reset Orientation is not available in the *Context Menu*

4. **WHEN** *Movement Execution* fails to write the rotation  
   **THEN** the facing direction is unchanged  
   **AND** the GM sees an error signal in the *Context Menu* action feedback

---

## Story: Maneuver Character with Camera via Context Menu

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Camera Rig* — the virtual camera
- *Movement Execution* — service that repositions spawned NPCs
- *Spawned State* — per-roster-entry Boolean

1. **WHEN** the GM selects Maneuver with Camera from the *Context Menu* on a spawned character  
   **THEN** maneuver-with-camera mode is activated for that character  
   **AND** subsequent movement commands drive the character in the *Camera Rig's* current facing direction

2. **WHEN** the *Camera Rig* is not active  
   **THEN** the Maneuver with Camera action surfaces a feedback message and is not applied  
   **BUT** no error state is entered

3. **WHEN** maneuver-with-camera mode is already active for the character  
   **THEN** selecting the action again deactivates the mode  
   **AND** movement commands revert to fixed-destination mode

4. **WHEN** the target character's *Spawned State* is false  
   **THEN** Maneuver with Camera is not available in the *Context Menu*

5. **WHEN** maneuver-with-camera mode is active and the GM moves the character  
   **THEN** *Movement Execution* computes the destination from the *Camera Rig's* facing direction at the time of each movement command

---

## Story: Activate Character Option via Context Menu

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Active Character* — a *Roster Entry* marked as holding the current turn
- *Roster Entry* — a character's session record
- *Roster Panel* — the roster list region

1. **WHEN** the GM selects Activate Option from the *Context Menu* on a *Roster Entry*  
   **THEN** that *Roster Entry* is marked as the *Active Character*  
   **AND** the active indicator appears on the *Roster Entry* in the *Roster Panel*  
   **AND** any previously active entry loses its indicator

2. **WHEN** the character targeted by the *Context Menu* is already the *Active Character*  
   **THEN** the Activate Option action is a no-op; the active indicator remains

3. **WHEN** *Gang Mode* is active when Activate Option is selected  
   **THEN** the targeted character is activated individually  
   **AND** the *Gang Mode* collective activation is replaced by this single-character activation

4. **WHEN** the *Context Menu* Activate Option succeeds  
   **THEN** the outcome is identical to clicking Activate in the *Roster Panel* for the same character  
   **AND** any configured default ability for the character may be played automatically if that behavior is configured

---

## Story: Clone and Link Character from Desktop

**Domain terms**:
- *Context Menu* — right-click popup scoped to a target character
- *Roster Entry* — a character's session record
- *Roster* — session-scope character list
- *Character* — the named data entity from the crowd library
- *Crowd* — a named hierarchical container of characters
- *Spawned State* — per-roster-entry Boolean

1. **WHEN** the GM selects Clone-Link from the *Context Menu* on a *Roster Entry*  
   **THEN** a clone-link operation is performed: a new *Character* is created as a linked copy of the original in the crowd library  
   **AND** the new *Character* is added to the same *Crowd* as the original  
   **AND** a new *Roster Entry* for the cloned character is created in the *Roster* with *Spawned State* false

2. **WHEN** the clone-link completes  
   **THEN** the new *Roster Entry* appears in the *Roster Panel* below the original  
   **AND** the new *Character* is visible as a linked member in the crowd library

3. **WHEN** the crowd library is unavailable or saving fails during clone-link  
   **THEN** the new character is not created and no new *Roster Entry* is added  
   **AND** the GM sees an error signal  
   **BUT** the original *Roster Entry* and its *Character* are unchanged

4. **WHEN** the cloned character's name would duplicate an existing name in the target *Crowd*  
   **THEN** the application appends a copy suffix (e.g. "Guard (Copy)") to make the name unique  
   **AND** the new *Roster Entry* reflects the unique name

5. **WHEN** the GM later modifies the original or the linked copy in the crowd library  
   **THEN** the modification is reflected in all crowds where either appears, because they are linked members sharing the same underlying data
