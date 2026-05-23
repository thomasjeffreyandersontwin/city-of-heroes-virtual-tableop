# Hero Virtual Tabletop — Story Map

> A WPF desktop application that turns City of Heroes (Titan Icon) into a virtual tabletop for superhero RPG sessions. The GM orchestrates characters, crowds, combat, and movement inside the 3D game world.

## Personas

- **GM (Game Master)**: Runs the session — spawns characters, arranges scenes, executes combat, controls movement. Primary power-user of every feature.
- **Player** (future): Observes the 3D scene and may eventually control their own character; currently the GM acts on their behalf.

---

(E) Launch and Initialize Session
    (E) Start Application
        (S) System --> Validate City of Heroes Game Directory
        (S) System --> Prompt for Game Directory if Invalid
        (S) System --> Load Prism Shell and Module
        (S) System --> Open Character Crowd Main Workspace
    (E) Initialize Game Connection
        (S) System --> Load HookCostume DLL from Game Directory
        (S) System --> Initialize Game Bridge (InitGame)
        (S) System --> Poll until Game Client is Loaded
        (S) System --> Inject Required KeyBinds into Game
        (S) System --> Deploy Camera Enable and Disable Scripts
        (S) System --> Deploy Area Attack Pop-Up Menu
        (S) System --> Extract Costume Pack on First Run
        (S) System --> Load Models List for Crowd Creation
        (S) System --> Render Camera Rig in Game
        (S) System --> Publish Game Loaded Event

(E) Manage Characters and Crowds
    (E) Manage Crowd Repository
        (S) GM --> Browse and Activate Crowd Files
        (S) System --> Load Active Crowd Files on Startup
        (S) GM --> Create Crowd
        (S) GM --> Rename Crowd
        (S) GM --> Delete Crowd
        (S) GM --> Nest Crowd inside Crowd
        (S) System --> Track Source File per Crowd
        (S) System --> Save Crowd Collection to Repository
        (S) GM --> Save Dirty Crowds to Source Files
        (S) GM --> Save Crowd to New File
        (S) System --> Back Up Repository on Load
        (S) System --> Load Default Crowd Members from Embedded Resource
    (E) Manage Characters within Crowds
        (S) GM --> Create Character in Crowd
        (S) GM --> Rename Character
        (S) GM --> Delete Character from Crowd
        (S) GM --> Clone Character
        (S) GM --> Cut Character to Clipboard
        (S) GM --> Link Character across Crowds
        (S) GM --> Clone-Link Character
        (S) GM --> Flatten-Copy Crowd into Numbered Characters
        (S) GM --> Clone Memberships to Another Crowd
        (S) GM --> Drag-Drop Character between Crowds
        (S) GM --> Filter Characters by Name
    (E) Organize Crowd Collections
        (S) GM --> Browse Crowds by Concept
        (S) GM --> Browse Crowds by Gangs, Crews, and Squads
        (S) GM --> Browse Crowds by COH Structure
        (S) System --> Browse All Characters Crowd
    (E) Build Crowds from Game Models
        (S) GM --> Create Crowd from COH Model List
        (S) System --> Load Available Models from Models.txt
        (S) GM --> Select Models to Include in Crowd
        (S) System --> Generate Characters with Model Identities

(E) Manage Game Roster
    (E) Populate Roster
        (S) GM --> Add Character to Roster
        (S) GM --> Add Crowd to Roster
        (S) GM --> Spawn Character to Desktop from Roster
        (S) GM --> Remove Character from Roster
        (S) GM --> Clear Character from Desktop
    (E) Activate Characters for Play
        (S) GM --> Activate Character (mark as active turn)
        (S) GM --> Deactivate Character
        (S) GM --> Activate Crowd as Gang with Gang Leader
        (S) GM --> Deactivate Gang
    (E) Interact with Roster on Desktop
        (S) GM --> Select Character on Desktop via Mouse Click
        (S) GM --> Multi-Select Characters
        (S) GM --> Drag Character to New Position on Desktop
        (S) GM --> Double-Click Character to Activate
        (S) System --> Sync Roster Selection with Game Target
        (S) System --> Track Spawned State per Character
    (E) Desktop Context Menu Actions
        (S) GM --> Spawn Character via Context Menu
        (S) GM --> Place Character at Location
        (S) GM --> Save Character Position
        (S) GM --> Move Camera to Target Character
        (S) GM --> Move Target Character to Camera
        (S) GM --> Reset Character Orientation via Context Menu
        (S) GM --> Maneuver Character with Camera via Context Menu
        (S) GM --> Activate Character Option via Context Menu
        (S) GM --> Clone and Link Character from Desktop

(E) Manage Character Identities
    (E) Configure Identity
        (S) GM --> Add Identity to Character
        (S) GM --> Set Identity Type (Model or Costume)
        (S) GM --> Assign Costume Surface to Identity
        (S) GM --> Set Default Identity
        (S) GM --> Set Active Identity
        (S) GM --> Remove Identity from Character
    (E) Render Identity in Game
        (S) System --> Load Costume File for Active Identity
        (S) System --> Spawn Character with Model Identity
        (S) System --> Switch Active Identity on Spawned Character
        (S) System --> Play Animation on Identity Load
        (S) System --> Stop Persistent Abilities on Identity Switch
    (E) Manage Ghost Shadows
        (S) GM --> Superimpose Ghost on Model Character
        (S) System --> Create Ghost Costume File from Original
        (S) System --> Align Ghost Position and Orientation with Character
        (S) System --> Remove Ghost from Desktop

(E) Manage Animated Abilities
    (E) Configure Abilities
        (S) GM --> Create Animated Ability
        (S) GM --> Edit Animated Ability
        (S) GM --> Delete Animated Ability
        (S) GM --> Set Ability Activation Key
        (S) GM --> Toggle Ability Persistence
        (S) GM --> Set Default Ability for Character
    (E) Compose Animation Elements
        (S) GM --> Add Movement Element to Ability
        (S) GM --> Add Sound Element to Ability
        (S) GM --> Add FX Element to Ability
        (S) GM --> Add Reference Element to Another Ability
        (S) GM --> Add Sequence Element (And/Or)
        (S) GM --> Add Pause Element
        (S) GM --> Add Load-Identity Element
        (S) GM --> Reorder Animation Elements via Drag-Drop
    (E) Play Abilities in Game
        (S) GM --> Play Animated Ability on Character
        (S) GM --> Stop Active Ability
        (S) System --> Execute Animation Sequence (And: sequential, Or: random)
        (S) System --> Maintain Persistent Ability across Identity Changes
        (S) System --> Load Persistent Costume on Deactivation
    (E) Manage Default Abilities
        (S) System --> Add Default Abilities to Character (Recovery, Stun Recovery, Pass Turn, Half Phase Action, Hold Action, Draw A Weapon, Dodge, Strike, Haymaker, Prone, Move By, Move Through, Grab, Disarm, Block, Set, Sweep, Rapid Fire, Off Ground, Generic Damage/Power)
        (S) System --> Refresh Ability Activation Eligibility

(E) Execute Combat
    (E) Initiate Attack
        (S) GM --> Select Attacking Character
        (S) GM --> Activate Attack Ability
        (S) GM --> Select Defender Targets
        (S) GM --> Confirm Attack Targets
        (S) System --> Configure Attack for Attacker-Defender Pair
    (E) Configure Attack Parameters
        (S) GM --> Set Attack Effect (Stunned, Unconscious, Dying, Dead)
        (S) GM --> Set Knockback Distance
        (S) GM --> Set Attack Result (Hit or Miss)
        (S) GM --> Set Attack Mode (Attack or Defend)
        (S) GM --> Designate Center Target for Area Attack
    (E) Execute Area and Sweep Attacks
        (S) GM --> Execute Ranged Area Attack
        (S) GM --> Execute Sweep Attack across Multiple Targets
        (S) GM --> Assign Auto-Fire Shots per Target
        (S) GM --> Spread Attack across Crowd
    (E) Resolve Attack Outcome
        (S) System --> Play Attack Animation on Attacker
        (S) System --> Play On-Hit Animation on Defender
        (S) System --> Apply Knockback Movement to Defender
        (S) System --> Apply Status Effect to Defender (Stunned, Unconscious, Dying, Dead)
        (S) System --> Update Character Attack State Indicators
    (E) Manage Combat State
        (S) GM --> Cancel Active Attack
        (S) GM --> Abort Attack in Progress
        (S) GM --> Reset Character Combat State
        (S) System --> Disable Non-Attack Abilities during Combat
        (S) System --> Track Attacker and Defender Roles per Character
    (E) Integrate Hero Combat System (HCS)
        (S) System --> Start HCS File Watcher Integration
        (S) System --> Read On-Deck Combatants from Info File
        (S) System --> Read Eligible Combatants from Info File
        (S) System --> Read Active Character from Info File
        (S) System --> Read Chronometer Turn State from Info File
        (S) System --> Process Attack Result Events from HCS
        (S) System --> Process Simple Ability Events from HCS
        (S) System --> Resolve Held Character State from HCS
        (S) System --> Execute Sweep Results from HCS
        (S) System --> Stop HCS Integration
    (E) Resolve Collisions during Combat
        (S) System --> Detect Knockback Obstruction via Collision Ray
        (S) System --> Calculate Line-of-Sight for Ranged Attack
        (S) System --> Query Game Collision Detection via HookCostume DLL

(E) Control Character Movement
    (E) Configure Movement Types
        (S) GM --> Add Movement to Character
        (S) GM --> Edit Movement Parameters
        (S) GM --> Remove Movement from Character
        (S) GM --> Set Default Movement
        (S) GM --> Set Movement Activation Key
        (S) System --> Add Default Movements to Character (Walk, Run, Swim)
    (E) Move Characters in Game
        (S) GM --> Move Character to Location
        (S) GM --> Move Character to Camera Position
        (S) GM --> Teleport Character to Camera
        (S) System --> Animate Walk/Run/Swim/Fly/Jump Movement
        (S) System --> Track Movement Distance Count
        (S) System --> Enforce Distance Limit per Movement Type
        (S) System --> Detect Floor and Wall Collisions
    (E) Move Crowds Together
        (S) GM --> Move Crowd with Relative Positioning
        (S) GM --> Move Crowd with Optimal Spread Positioning
        (S) System --> Maintain Group Formation during Crowd Move
        (S) System --> Turn Characters to Face Destination
    (E) Control Orientation and Facing
        (S) GM --> Turn Character towards Target
        (S) GM --> Reset Character Orientation
        (S) GM --> Align Character Facing with Gang Leader
    (E) Special Movement Modes
        (S) GM --> Activate Maneuver-with-Camera Mode
        (S) GM --> Follow Character with Game Camera
        (S) GM --> Unfollow Character

(E) Communicate with Game Engine
    (E) Bridge via HookCostume DLL
        (S) System --> Initialize Native Game Bridge
        (S) System --> Execute Slash Command via DLL
        (S) System --> Query Hovered NPC Info from Game
        (S) System --> Query Mouse XYZ Position in Game World
        (S) System --> Query Collision Detection via DLL
        (S) System --> Check Game Done State
        (S) System --> Close Game Bridge on Shutdown
    (E) Execute Game Commands via KeyBinds
        (S) System --> Generate KeyBind File for Game Event
        (S) System --> Execute Spawn NPC Command
        (S) System --> Execute Target by Name Command
        (S) System --> Execute Delete NPC Command
        (S) System --> Execute Load Costume Command
        (S) System --> Execute Move NPC Command
        (S) System --> Execute Follow Command
        (S) System --> Execute Camera Detach Command
        (S) System --> Execute Load Map Command
        (S) System --> Split Oversized Command Chains for Execution
    (E) Read and Write Game Memory
        (S) System --> Read Target Character from Memory
        (S) System --> Read Character Position (X, Y, Z) from Memory
        (S) System --> Write Character Position to Memory
        (S) System --> Read Character Model Matrix from Memory
        (S) System --> Write Character Rotation Matrix to Memory
        (S) System --> Read Character Facing Vector from Memory
        (S) System --> Write Character Facing Direction to Memory
        (S) System --> Read Camera Position from Memory
    (E) Observe Game State
        (S) System --> Monitor Current Target in Game
        (S) System --> Wait until Target is Registered after Spawn
        (S) System --> Scan and Fix Stale Memory Pointers
        (S) System --> Detect Game Process for Connection
    (E) Handle Global Keyboard Shortcuts
        (S) System --> Install Low-Level Keyboard Hook
        (S) System --> Route Key Events when Game Window is Focused
        (S) System --> Route Key Events when Application Window is Focused
        (S) System --> Dispatch Ability Activation Keys to Characters

(E) Manage Game Data and Files
    (E) Manage Costume Files
        (S) System --> Store Costume Files in COH Costumes Directory
        (S) System --> Create Original-Backup Costume Files
        (S) System --> Create Persistent-FX Costume Variants
        (S) System --> Create Ghost Costume Files
    (E) Manage KeyBind Files
        (S) System --> Write Custom KeyBind Files to COH Data Directory
        (S) System --> Load KeyBind File into Game
    (E) Manage Pop-Up Menus
        (S) System --> Write Pop-Up Menu Files to COH Menus Directory
        (S) System --> Load Pop-Up Menu in Game
    (E) Manage Crowd Repository Persistence
        (S) System --> Serialize Crowd Collection to JSON
        (S) System --> Deserialize Crowd Collection from JSON
        (S) System --> Create Daily Backup of Crowd Repository
        (S) System --> Store Crowd Repository in COH Data Directory
    (E) Manage Animation Resource Catalogs
        (S) System --> Load FX Resource Catalog (FxRepo.data)
        (S) System --> Load Movement Resource Catalog (MoveRepo.data)
        (S) System --> Load Sound Resource Catalog (SoundRepo.data)
        (S) System --> Seed Resource Catalogs from Embedded CSV on First Run
        (S) GM --> Browse FX Resources for Ability Authoring
        (S) GM --> Browse Movement Resources for Ability Authoring
        (S) GM --> Browse Sound Resources for Ability Authoring
