# Thin Slicing — Hero Virtual Tabletop

## Product / context

**Product:** Hero Virtual Tabletop (HVT) — WPF desktop application driving City of Heroes / Titan Icon as a virtual tabletop for superhero RPG sessions.

**Slicing strategy:** Behavioral epics, bottom-up. Each increment validates one domain capability end-to-end before the next builds on it. Infrastructure stories (game engine communication, data persistence, startup) are pulled into whichever increment first needs them. Primary goal is **testability** — each increment produces a verifiable behavioral surface.

**Spine vs optional:** The spine follows the dependency chain: *Characters* → *Identities* → *Abilities* → *Movement* → *Roster/Desktop* → *Crowd Orchestration + Combat*. Crowd-level operations (gang movement, area attacks) and all combat are **deferred** until single-character abilities and movement are proven. Migration stories are **killed** (out of scope).

---

## Increments

### Increment 1: Character and Crowd Library

**Outcome:** GM can create, organize, browse, and persist *characters* and *crowds* — the data foundation everything else builds on.

**Slicing notes:** No game connection required. Tests validate CRUD, clipboard operations, filtering, and JSON persistence without launching COH. Pulls in startup (app shell, workspace) and data persistence stories. Build Crowds from Game Models deferred to Increment 2 (needs game connection for Models.txt).

**Stories in this increment:**

- Validate City of Heroes Game Directory
- Prompt for Game Directory if Invalid
- Load Prism Shell and Module
- Open Character Crowd Main Workspace
- Load Crowd Collection from Repository
- Deserialize Crowd Collection from JSON
- Load Default Crowd Members from Embedded Resource
- Create Crowd
- Rename Crowd
- Delete Crowd
- Nest Crowd inside Crowd
- Create Character in Crowd
- Rename Character
- Delete Character from Crowd
- Clone Character
- Cut Character to Clipboard
- Link Character across Crowds
- Clone-Link Character
- Flatten-Copy Crowd into Numbered Characters
- Clone Memberships to Another Crowd
- Drag-Drop Character between Crowds
- Filter Characters by Name
- Browse Crowds by Concept (Animals, Armed Forces, Civilians, Vehicles, etc.)
- Browse Crowds by Gangs, Crews, and Squads
- Browse Crowds by COH Structure
- Browse All Characters Crowd
- Save Crowd Collection to Repository
- Serialize Crowd Collection to JSON
- Create Daily Backup of Crowd Repository
- Store Crowd Repository in COH Data Directory
- Back Up Repository on Load

---

### Increment 2: Character Identities

**Outcome:** GM can assign *identities* (Model or Costume) to characters and see them rendered in the game world — characters become visible 3D entities.

**Slicing notes:** First increment that touches the live game. Pulls in game bridge initialization, keybind generation, and costume file management. Ghost shadows are optional depth but included because they exercise the full identity rendering pipeline. Build Crowds from Game Models fits here (requires Models.txt from initialized game).

**Stories in this increment:**

- Load HookCostume DLL from Game Directory
- Initialize Game Bridge (InitGame)
- Poll until Game Client is Loaded
- Inject Required KeyBinds into Game
- Extract Costume Pack on First Run
- Publish Game Loaded Event
- Initialize Native Game Bridge
- Execute Slash Command via DLL
- Generate KeyBind File for Game Event
- Execute Spawn NPC Command
- Execute Target by Name Command
- Execute Load Costume Command
- Execute Delete NPC Command
- Store Costume Files in COH Costumes Directory
- Create Original-Backup Costume Files
- Write Custom KeyBind Files to COH Data Directory
- Load KeyBind File into Game
- Add Identity to Character
- Set Identity Type (Model or Costume)
- Assign Costume Surface to Identity
- Set Default Identity
- Set Active Identity
- Remove Identity from Character
- Load Costume File for Active Identity
- Spawn Character with Model Identity
- Switch Active Identity on Spawned Character
- Play Animation on Identity Load
- Stop Persistent Abilities on Identity Switch
- Superimpose Ghost on Model Character
- Create Ghost Costume File from Original
- Align Ghost Position and Orientation with Character
- Remove Ghost from Desktop
- Create Persistent-FX Costume Variants
- Create Ghost Costume Files
- Load Available Models from Models.txt
- Create Crowd from COH Model List
- Select Models to Include in Crowd
- Generate Characters with Model Identities
- Load Models List for Crowd Creation

---

### Increment 3: Animated Abilities

**Outcome:** GM can author *animated abilities* from composable elements (FX, sound, movement, sequences) and play them on spawned characters — characters perform visible actions in the 3D world.

**Slicing notes:** Depends on characters being spawnable (Increment 2). Pulls in resource catalog loading and keyboard hook for activation keys. Default abilities validate the bulk-attach pattern. No attacks yet — only standard non-combat abilities.

**Stories in this increment:**

- Load FX Resource Catalog (FxRepo.data)
- Load Movement Resource Catalog (MoveRepo.data)
- Load Sound Resource Catalog (SoundRepo.data)
- Seed Resource Catalogs from Embedded CSV on First Run
- Browse FX Resources for Ability Authoring
- Browse Movement Resources for Ability Authoring
- Browse Sound Resources for Ability Authoring
- Create Animated Ability
- Edit Animated Ability
- Delete Animated Ability
- Set Ability Activation Key
- Toggle Ability Persistence
- Set Default Ability for Character
- Add Movement Element to Ability
- Add Sound Element to Ability
- Add FX Element to Ability
- Add Reference Element to Another Ability
- Add Sequence Element (And/Or)
- Add Pause Element
- Add Load-Identity Element
- Reorder Animation Elements via Drag-Drop
- Play Animated Ability on Character
- Stop Active Ability
- Execute Animation Sequence (And: sequential, Or: random)
- Maintain Persistent Ability across Identity Changes
- Load Persistent Costume on Deactivation
- Add Default Abilities to Character (Recovery, Stun Recovery, Pass Turn, Half Phase Action, Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power)
- Refresh Ability Activation Eligibility
- Install Low-Level Keyboard Hook
- Route Key Events when Game Window is Focused
- Route Key Events when Application Window is Focused
- Dispatch Ability Activation Keys to Characters

---

### Increment 4: Single Character Movement

**Outcome:** GM can move individual characters through the 3D world using configured *movement types* — walk, run, teleport, turn, follow with camera.

**Slicing notes:** Depends on spawned characters (Increment 2). Pulls in memory read/write for position, facing, and camera. Crowd movement deferred to Increment 6. Only single-character movement and orientation here.

**Stories in this increment:**

- Read Target Character from Memory
- Read Character Position (X, Y, Z) from Memory
- Write Character Position to Memory
- Read Character Model Matrix from Memory
- Write Character Rotation Matrix to Memory
- Read Character Facing Vector from Memory
- Write Character Facing Direction to Memory
- Read Camera Position from Memory
- Execute Move NPC Command
- Execute Follow Command
- Execute Camera Detach Command
- Deploy Camera Enable and Disable Scripts
- Render Camera Rig in Game
- Monitor Current Target in Game
- Wait until Target is Registered after Spawn
- Scan and Fix Stale Memory Pointers
- Detect Game Process for Connection
- Add Movement to Character
- Edit Movement Parameters
- Remove Movement from Character
- Set Default Movement
- Set Movement Activation Key
- Add Default Movements to Character (Walk, Run, Swim)
- Move Character to Location
- Move Character to Camera Position
- Teleport Character to Camera
- Animate Walk/Run/Swim/Fly/Jump Movement
- Track Movement Distance Count
- Enforce Distance Limit per Movement Type
- Detect Floor and Wall Collisions
- Turn Character towards Target
- Reset Character Orientation
- Activate Maneuver-with-Camera Mode
- Follow Character with Game Camera
- Unfollow Character

---

### Increment 5: Roster and Desktop Interaction

**Outcome:** GM can populate a *roster* for the active session, spawn characters to the desktop, activate them for play, and interact via mouse and context menus — the live session workspace.

**Slicing notes:** Exercises the full loop: pick from crowd library → add to roster → spawn → interact on desktop → save position. Gang activation included as it's roster-level, but gang movement deferred to Increment 6. Pulls in remaining game engine observation and desktop event handling.

**Stories in this increment:**

- Query Hovered NPC Info from Game
- Query Mouse XYZ Position in Game World
- Check Game Done State
- Split Oversized Command Chains for Execution
- Close Game Bridge on Shutdown
- Execute Load Map Command
- Write Pop-Up Menu Files to COH Menus Directory
- Load Pop-Up Menu in Game
- Deploy Area Attack Pop-Up Menu
- Add Character to Roster
- Add Crowd to Roster
- Spawn Character to Desktop from Roster
- Remove Character from Roster
- Clear Character from Desktop
- Activate Character (mark as active turn)
- Deactivate Character
- Activate Crowd as Gang with Gang Leader
- Deactivate Gang
- Select Character on Desktop via Mouse Click
- Multi-Select Characters
- Drag Character to New Position on Desktop
- Double-Click Character to Activate
- Sync Roster Selection with Game Target
- Track Spawned State per Character
- Spawn Character via Context Menu
- Place Character at Location
- Save Character Position
- Move Camera to Target Character
- Move Target Character to Camera
- Reset Character Orientation via Context Menu
- Maneuver Character with Camera via Context Menu
- Activate Character Option via Context Menu
- Clone and Link Character from Desktop

---

### Increment 6: Crowd Orchestration and Combat

**Outcome:** GM can move crowds as formations, execute attacks (single, area, sweep, auto-fire), resolve combat outcomes with status effects and knockback, and integrate with the external *Hero Combat System* — the full tabletop combat experience.

**Slicing notes:** Final increment. Depends on all prior increments: characters with identities, abilities, and movement must work individually before orchestrating them as crowds and resolving combat. Pulls in collision engine and HCS file-watcher integration.

**Stories in this increment:**

- Move Crowd with Relative Positioning
- Move Crowd with Optimal Spread Positioning
- Maintain Group Formation during Crowd Move
- Turn Characters to Face Destination
- Align Character Facing with Gang Leader
- Select Attacking Character
- Activate Attack Ability
- Select Defender Targets
- Confirm Attack Targets
- Configure Attack for Attacker-Defender Pair
- Set Attack Effect (Stunned, Unconscious, Dying, Dead)
- Set Knockback Distance
- Set Attack Result (Hit or Miss)
- Set Attack Mode (Attack or Defend)
- Designate Center Target for Area Attack
- Execute Ranged Area Attack
- Execute Sweep Attack across Multiple Targets
- Assign Auto-Fire Shots per Target
- Spread Attack across Crowd
- Play Attack Animation on Attacker
- Play On-Hit Animation on Defender
- Apply Knockback Movement to Defender
- Apply Status Effect to Defender (Stunned, Unconscious, Dying, Dead)
- Update Character Attack State Indicators
- Cancel Active Attack
- Abort Attack in Progress
- Reset Character Combat State
- Disable Non-Attack Abilities during Combat
- Track Attacker and Defender Roles per Character
- Detect Knockback Obstruction via Collision Ray
- Calculate Line-of-Sight for Ranged Attack
- Query Game Collision Detection via HookCostume DLL
- Start HCS File Watcher Integration
- Read On-Deck Combatants from Info File
- Read Eligible Combatants from Info File
- Read Active Character from Info File
- Read Chronometer Turn State from Info File
- Process Attack Result Events from HCS
- Process Simple Ability Events from HCS
- Resolve Held Character State from HCS
- Execute Sweep Results from HCS
- Stop HCS Integration

---

## Killed (out of scope)

- ~~Migrate Repository to Refactored Format~~
- ~~Convert Legacy CrowdModel Format to Split-File Format~~
- ~~Check Roster Consistency after Migration~~
