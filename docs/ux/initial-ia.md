# Initial information architecture — Hero Virtual Tabletop (Full Application)

> **Companion to** `docs/ux/initial-ia.tldr` / `initial-ia.svg`. This markdown is the structured spec for the canvas. Author or update **this file first**, then drive the canvas from it. After the canvas is updated, sync any change back into this file so the two never diverge.

## Metadata

| Field | Value |
| --- | --- |
| Scope | Full Application — all increments |
| Story map | `docs/story-map.md` |
| Domain terms | `docs/domain-terms.md` |
| Canvas (`.tldr`) | `docs/ux/initial-ia.tldr` |
| Canvas (`.svg`) | `docs/ux/initial-ia.svg` |
| Last canvas update | 2026-05-16 |

## Description

The Hero Virtual Tabletop is a WPF desktop application with three distinct user-visible surfaces: a startup prompt that validates the game installation path, a pre-session library workspace where the GM builds and organizes character crowds, and a live game overlay where the GM spawns characters, runs combat, and controls movement during play. The IA captures navigation flow between those surfaces, the layout and content of each, and the content types the GM manages throughout.

---

## Navigation

### Site map — screens

Transitions are recorded inside each screen block. Every outgoing entry on one screen has a matching incoming entry on the destination screen.

#### game directory prompt

- **Description:** Startup modal displayed when the COH game directory path is missing or fails validation; the GM must supply a valid path before the application proceeds.
- **Source:** [`COH game directory`](../domain-terms.md#coh-game-directory)
- **Layout:** `modal dialog (centered, single column)`
- **From (incoming transitions):**
  - (entry point — application launch)
- **To (outgoing transitions):**
  - to `crowd manager` — trigger: GM submits valid COH game directory path
- **Regions and content:**
  - **title area** — slot: `header` — [COH game directory](../domain-terms.md#coh-game-directory) — relationship: subject of validation
    - *Specific to this screen* — labels: "COH game directory"; key actions: validate
  - **path input area** — slot: `body row 1` — [COH game directory](../domain-terms.md#coh-game-directory) — relationship: editable value
    - *Specific to this screen* — labels: directory path; key actions: browse, type path
  - **error message area** — slot: `body row 2` — validation feedback — relationship: conditional (shown when path is invalid)
    - *Specific to this screen* — labels: error message; key actions: (none — display only)
  - **continue area** — slot: `footer` — (no content type) — relationship: action gate
    - *Specific to this screen* — key actions: continue (enabled only when path is valid)
- **In-scope user stories (names + links):**
  - [Validate City of Heroes Game Directory](../story-map.md)
  - [Prompt for Game Directory if Invalid](../story-map.md)
- **Groups system stories (names + links):**
  - [Load Prism Shell and Module](../story-map.md)

---

#### crowd manager

- **Description:** The main pre-session workspace. Opens immediately after the directory prompt (or directly on relaunch when the path is already valid). The GM builds and organizes the full character library here before any game session begins.
- **Source:** [`crowd manager`](../domain-terms.md#crowd-manager)
- **Layout:** `header (toolbar) + left panel (crowd tree) + body (character detail) + footer (status bar)`
- **From (incoming transitions):**
  - from `game directory prompt` — trigger: GM submits valid COH game directory path
  - from `desktop` — trigger: GM ends game session
- **To (outgoing transitions):**
  - to `desktop` — trigger: GM initiates game connection (Initialize Game Connection)
- **Regions and content:**
  - **toolbar** — slot: `header` — [crowd](../domain-terms.md#crowd), [character](../domain-terms.md#character) — relationship: global actions, save/load controls
    - *Specific to this screen* — data shown: crowd repository save state, filter input field; key actions: save crowd repository, initialize game connection (→ desktop), filter characters by name, browse crowds by concept, browse all characters crowd
  - **crowd tree panel** — slot: `left panel` — [crowd](../domain-terms.md#crowd), [crowd member](../domain-terms.md#crowd-member) — relationship: hierarchical collection
    - Sub-regions:
      - **filter bar** — name filter input; key actions: filter characters by name (live regex)
      - **crowd tree** — expandable/collapsible tree of crowds and crowd members; all characters crowd shown as protected root; selected item highlighted; browse categories: by concept (Animals, Armed Forces, Civilians, Vehicles, etc.), by Gangs/Crews/Squads, by COH Structure; key actions: expand/collapse, select, drag-drop character between crowds, context-menu on item (create crowd, rename, delete, nest, clone, cut to clipboard, link, clone-link, flatten-copy)
    - Domain terms: crowd, crowd member, all characters crowd, clipboard, flatten-copy, gang mode, gang, crew, squad, COH structure
  - **character detail panel — Identities tab** — slot: `body (tab: Identities)` — [identity](../domain-terms.md#identity) — relationship: selected character's Identities option group
    - Sub-regions:
      - **identity list** — ordered list of identities on the character; active and default indicators; key actions: add, remove, set as default, set as active, reorder
      - **identity detail** — surface name, identity type (Model / Costume), animation on load assignment; key actions: set type, assign surface, configure animation on load
      - **ghost shadow controls** — shown when selected identity is a model identity; data shown: ghost enabled flag, ghost costume file name; key actions: superimpose ghost on model character, remove ghost from desktop
    - Domain terms: identity, model identity, costume identity, surface, animation on load, costume file, ghost shadow, active identity, default identity
  - **character detail panel — Abilities tab** — slot: `body (tab: Abilities)` — [animated ability](../domain-terms.md#animated-ability) — relationship: selected character's Abilities option group
    - Sub-regions:
      - **ability list** — ordered list of animated abilities; active indicator, persistent flag, attack flag, activation key; key actions: create, delete, play, stop, set activation key, toggle persistence, set as default
      - **ability editor** — tree of animation elements for the selected ability; And/Or sequence type; each element row shows type badge + parameters; key actions per element type:
        - *Add MOV element* — assign animation resource; edit duration
        - *Add FX element* — assign FX resource; edit position, scale
        - *Add Sound element* — assign sound file; edit volume
        - *Add Pause element* — edit pause duration
        - *Add Sequence element (And/Or)* — set sequence type (And: sequential / Or: random); nest child elements
        - *Add Reference element* — link to another animated ability
        - *Add Load-Identity element* — link to a character identity
        - *Reorder elements* — drag-drop to reorder within tree
        - *Edit element parameters* — edit any element's parameters inline
        - *Delete element* — remove element from tree
    - Domain terms: animated ability, animation element, FX effect element, MOV element, sound element, pause element, sequence element, reference ability, identity element, animation resource, attack, on-hit animation, And sequence, Or sequence
  - **character detail panel — Movements tab** — slot: `body (tab: Movements)` — [character movement](../domain-terms.md#character-movement) — relationship: selected character's Movements option group
    - Sub-regions:
      - **movement list** — ordered list of character movements; default indicator, activation key; key actions: add, remove, set as default, set activation key
      - **movement detail** — movement type, distance limit, activation key; key actions: edit parameters
    - Domain terms: character movement, movement instruction
  - **status bar** — slot: `footer` — [crowd repository](../domain-terms.md#crowd-repository) — relationship: persistence and load status
    - *Specific to this screen* — data shown: save state, last save time; key actions: (display only)
- **In-scope user stories (names + links):**
  - [Create Crowd](../story-map.md)
  - [Rename Crowd](../story-map.md)
  - [Delete Crowd](../story-map.md)
  - [Nest Crowd inside Crowd](../story-map.md)
  - [Create Character in Crowd](../story-map.md)
  - [Rename Character](../story-map.md)
  - [Delete Character from Crowd](../story-map.md)
  - [Clone Character](../story-map.md)
  - [Cut Character to Clipboard](../story-map.md)
  - [Link Character across Crowds](../story-map.md)
  - [Clone-Link Character](../story-map.md)
  - [Flatten-Copy Crowd into Numbered Characters](../story-map.md)
  - [Clone Memberships to Another Crowd](../story-map.md)
  - [Drag-Drop Character between Crowds](../story-map.md)
  - [Filter Characters by Name](../story-map.md)
  - [Browse Crowds by Concept](../story-map.md)
  - [Browse Crowds by Gangs, Crews, and Squads](../story-map.md)
  - [Browse Crowds by COH Structure](../story-map.md)
  - [Browse All Characters Crowd](../story-map.md)
  - [Create Crowd from COH Model List](../story-map.md)
  - [Select Models to Include in Crowd](../story-map.md)
  - [Add Identity to Character](../story-map.md)
  - [Set Identity Type (Model or Costume)](../story-map.md)
  - [Assign Costume Surface to Identity](../story-map.md)
  - [Set Default Identity](../story-map.md)
  - [Set Active Identity](../story-map.md)
  - [Remove Identity from Character](../story-map.md)
  - [Superimpose Ghost on Model Character](../story-map.md)
  - [Create Animated Ability](../story-map.md)
  - [Edit Animated Ability](../story-map.md)
  - [Delete Animated Ability](../story-map.md)
  - [Set Ability Activation Key](../story-map.md)
  - [Toggle Ability Persistence](../story-map.md)
  - [Set Default Ability for Character](../story-map.md)
  - [Add Movement Element to Ability](../story-map.md)
  - [Add Sound Element to Ability](../story-map.md)
  - [Add FX Element to Ability](../story-map.md)
  - [Add Reference Element to Another Ability](../story-map.md)
  - [Add Sequence Element (And/Or)](../story-map.md)
  - [Add Pause Element](../story-map.md)
  - [Add Load-Identity Element](../story-map.md)
  - [Reorder Animation Elements via Drag-Drop](../story-map.md)
  - [Add Movement to Character](../story-map.md)
  - [Edit Movement Parameters](../story-map.md)
  - [Remove Movement from Character](../story-map.md)
  - [Set Default Movement](../story-map.md)
  - [Set Movement Activation Key](../story-map.md)
  - [Browse FX Resources for Ability Authoring](../story-map.md)
  - [Browse Movement Resources for Ability Authoring](../story-map.md)
  - [Browse Sound Resources for Ability Authoring](../story-map.md)
- **Groups system stories (names + links):**
  - [Load Crowd Collection from Repository](../story-map.md)
  - [Save Crowd Collection to Repository](../story-map.md)
  - [Back Up Repository on Load](../story-map.md)
  - [Load Default Crowd Members from Embedded Resource](../story-map.md)
  - [Load Available Models from Models.txt](../story-map.md)
  - [Generate Characters with Model Identities](../story-map.md)
  - [Add Default Abilities to Character](../story-map.md)
  - [Add Default Movements to Character](../story-map.md)
  - [Load FX Resource Catalog](../story-map.md)
  - [Load Movement Resource Catalog](../story-map.md)
  - [Load Sound Resource Catalog](../story-map.md)

---

#### desktop

- **Description:** The live game session surface. A transparent overlay on the COH game window with a session toolbar, a roster panel for managing the active session's character list, the game overlay body where spawned characters appear as interactive nodes, and a contextual attack configuration panel that appears during combat.
- **Source:** [`desktop`](../domain-terms.md#desktop)
- **Layout:** `session toolbar (header) + left panel (roster) + body (character overlay) + contextual panel (attack configuration, shown during active attack)`
- **From (incoming transitions):**
  - from `crowd manager` — trigger: GM initiates game connection (game loaded event published)
- **To (outgoing transitions):**
  - to `crowd manager` — trigger: GM ends game session
- **Regions and content:**
  - **session toolbar** — slot: `header` — [roster](../domain-terms.md#roster), [HCS](../domain-terms.md#hcs-hero-combat-system) — relationship: session controls and HCS turn state display
    - *Specific to this screen* — data shown: HCS on-deck combatant name, active character name, chronometer turn state; key actions: add character to roster, add crowd to roster, end game session, start/stop HCS integration
    - Domain terms: roster, HCS, on-deck combatant, chronometer
  - **roster panel** — slot: `left panel` — [roster](../domain-terms.md#roster), [crowd member](../domain-terms.md#crowd-member) — relationship: characters and crowds added to the current session, showing spawned/active state
    - Sub-regions:
      - **roster list** — ordered list of roster entries (characters and crowds); data shown per entry: character/crowd name, spawned badge, active-turn indicator, gang mode indicator, combat state badge (attacker / defender / stunned / unconscious / dying / dead); key actions: spawn to desktop, remove from roster, select
      - **roster actions bar** — key actions: activate character (mark active turn), deactivate character, activate crowd as gang (designate gang leader), deactivate gang, multi-select characters, clear character from desktop
    - Domain terms: roster, crowd member, gang mode, gang leader, active character, spawned state
  - **character overlay area** — slot: `body` — [character](../domain-terms.md#character) — relationship: spawned characters as positioned interactive nodes on the game world
    - *Specific to this screen* — data shown per character node: character name, spawned state badge, active-turn indicator, combat state badge, movement distance progress bar; key actions: click to select, multi-select, drag to new position, double-click to activate, play animated ability (via activation key), stop active ability, move character to location, move character to camera position, teleport character to camera, turn towards target, reset orientation, align facing with gang leader, follow character with camera, unfollow character, move crowd with relative positioning, move crowd with optimal spread positioning
    - Domain terms: character, spawned state, combat state, movement distance, animated ability, character movement, gang mode
  - **attack configuration panel** — slot: `body (contextual — shown when attack ability is active)` — [attack configuration](../domain-terms.md#attack-configuration) — relationship: per-attacker/defender combat parameters for the current attack sequence
    - Sub-regions:
      - **attacker row** — data shown: attacker name, selected attack ability name; key actions: activate attack ability, cancel active attack, abort attack in progress
      - **defender list** — one row per selected defender; data shown per row: defender name, combat state; key actions: confirm attack targets, set attack effect per defender (stunned / unconscious / dying / dead), set knockback distance per defender, set attack result per defender (hit / miss), set attack mode (attack / defend), remove defender from list
      - **area / sweep controls** — data shown: area-attack mode indicator, sweep mode indicator; key actions: designate center target for area attack, execute ranged area attack, execute sweep attack across multiple targets, assign auto-fire shots per target, spread attack across crowd
    - Domain terms: attack configuration, attack, on-hit animation, attacker, defender, knockback, combat state, area attack, sweep attack
  - **context menu** — slot: `body (contextual — right-click on spawned character)` — [crowd member](../domain-terms.md#crowd-member) — relationship: targeted character actions
    - *Specific to this screen* — data shown: targeted character name (header); key actions: spawn, place at location, move to camera, move character to camera, save position, clone-link, activate character option, maneuver with camera, reset orientation
    - Domain terms: crowd member, character, spawned state
- **In-scope user stories (names + links):**
  - [Add Character to Roster](../story-map.md)
  - [Add Crowd to Roster](../story-map.md)
  - [Spawn Character to Desktop from Roster](../story-map.md)
  - [Remove Character from Roster](../story-map.md)
  - [Clear Character from Desktop](../story-map.md)
  - [Activate Character (mark as active turn)](../story-map.md)
  - [Deactivate Character](../story-map.md)
  - [Activate Crowd as Gang with Gang Leader](../story-map.md)
  - [Deactivate Gang](../story-map.md)
  - [Select Character on Desktop via Mouse Click](../story-map.md)
  - [Multi-Select Characters](../story-map.md)
  - [Drag Character to New Position on Desktop](../story-map.md)
  - [Double-Click Character to Activate](../story-map.md)
  - [Spawn Character via Context Menu](../story-map.md)
  - [Place Character at Location](../story-map.md)
  - [Save Character Position](../story-map.md)
  - [Move Camera to Target Character](../story-map.md)
  - [Move Target Character to Camera](../story-map.md)
  - [Reset Character Orientation via Context Menu](../story-map.md)
  - [Maneuver Character with Camera via Context Menu](../story-map.md)
  - [Activate Character Option via Context Menu](../story-map.md)
  - [Clone and Link Character from Desktop](../story-map.md)
  - [Select Attacking Character](../story-map.md)
  - [Activate Attack Ability](../story-map.md)
  - [Select Defender Targets](../story-map.md)
  - [Confirm Attack Targets](../story-map.md)
  - [Set Attack Effect (Stunned, Unconscious, Dying, Dead)](../story-map.md)
  - [Set Knockback Distance](../story-map.md)
  - [Set Attack Result (Hit or Miss)](../story-map.md)
  - [Set Attack Mode (Attack or Defend)](../story-map.md)
  - [Designate Center Target for Area Attack](../story-map.md)
  - [Execute Ranged Area Attack](../story-map.md)
  - [Execute Sweep Attack across Multiple Targets](../story-map.md)
  - [Assign Auto-Fire Shots per Target](../story-map.md)
  - [Spread Attack across Crowd](../story-map.md)
  - [Cancel Active Attack](../story-map.md)
  - [Abort Attack in Progress](../story-map.md)
  - [Reset Character Combat State](../story-map.md)
  - [Move Character to Location](../story-map.md)
  - [Move Character to Camera Position](../story-map.md)
  - [Teleport Character to Camera](../story-map.md)
  - [Move Crowd with Relative Positioning](../story-map.md)
  - [Move Crowd with Optimal Spread Positioning](../story-map.md)
  - [Turn Character towards Target](../story-map.md)
  - [Reset Character Orientation](../story-map.md)
  - [Align Character Facing with Gang Leader](../story-map.md)
  - [Activate Maneuver-with-Camera Mode](../story-map.md)
  - [Follow Character with Game Camera](../story-map.md)
  - [Unfollow Character](../story-map.md)
  - [Play Animated Ability on Character](../story-map.md)
  - [Stop Active Ability](../story-map.md)
- **Groups system stories (names + links):**
  - [Load HookCostume DLL from Game Directory](../story-map.md)
  - [Initialize Game Bridge (InitGame)](../story-map.md)
  - [Poll until Game Client is Loaded](../story-map.md)
  - [Inject Required KeyBinds into Game](../story-map.md)
  - [Deploy Camera Enable and Disable Scripts](../story-map.md)
  - [Deploy Area Attack Pop-Up Menu](../story-map.md)
  - [Render Camera Rig in Game](../story-map.md)
  - [Sync Roster Selection with Game Target](../story-map.md)
  - [Track Spawned State per Character](../story-map.md)
  - [Execute Animation Sequence](../story-map.md)
  - [Maintain Persistent Ability across Identity Changes](../story-map.md)
  - [Play Attack Animation on Attacker](../story-map.md)
  - [Play On-Hit Animation on Defender](../story-map.md)
  - [Apply Knockback Movement to Defender](../story-map.md)
  - [Apply Status Effect to Defender](../story-map.md)
  - [Configure Attack for Attacker-Defender Pair](../story-map.md)
  - [Update Character Attack State Indicators](../story-map.md)
  - [Disable Non-Attack Abilities during Combat](../story-map.md)
  - [Track Attacker and Defender Roles per Character](../story-map.md)
  - [Start HCS File Watcher Integration](../story-map.md)
  - [Read On-Deck Combatants from Info File](../story-map.md)
  - [Read Eligible Combatants from Info File](../story-map.md)
  - [Read Active Character from Info File](../story-map.md)
  - [Read Chronometer Turn State from Info File](../story-map.md)
  - [Process Attack Result Events from HCS](../story-map.md)
  - [Process Simple Ability Events from HCS](../story-map.md)
  - [Resolve Held Character State from HCS](../story-map.md)
  - [Execute Sweep Results from HCS](../story-map.md)
  - [Stop HCS Integration](../story-map.md)
  - [Detect Knockback Obstruction via Collision Ray](../story-map.md)
  - [Calculate Line-of-Sight for Ranged Attack](../story-map.md)
  - [Animate Walk/Run/Swim/Fly/Jump Movement](../story-map.md)
  - [Track Movement Distance Count](../story-map.md)
  - [Enforce Distance Limit per Movement Type](../story-map.md)
  - [Detect Floor and Wall Collisions](../story-map.md)
  - [Maintain Group Formation during Crowd Move](../story-map.md)
  - [Turn Characters to Face Destination](../story-map.md)

---

### Navigational components

#### primary toolbar (toolbar)

- **Appears on:** `crowd manager` (header slot)
- **Links to:** crowd tree panel, character detail panel (tab switching — Identities / Abilities / Movements), initialize game connection (→ desktop)
- **Notes:** Contains save, filter, and session-start controls. No persistent navigation back to game directory prompt after first valid setup.

#### context menu (contextual overlay)

- **Appears on:** `desktop` (right-click on spawned character)
- **Links to:** character overlay area (targeted character actions)
- **Notes:** Not a fixed region — appears at mouse position on right-click. Lists available actions for the targeted crowd member.

---

## Content types (shared across screens)

#### crowd

- **Source:** [`crowd`](../domain-terms.md#crowd)
- **Used on:** `crowd manager` (crowd tree panel), `desktop` (session toolbar — add crowd to roster)
- **Hierarchy / collections:** crowd contains crowd members (characters or nested crowds); all characters crowd is a protected root that aggregates every character
- **Preliminary labels and tags:** crowd name, member count, gang mode indicator
- **Key actions:** create, rename, delete, nest inside crowd, clone, flatten-copy, filter by name, save position, restore position, move crowd (desktop), activate as gang

---

#### character

- **Source:** [`character`](../domain-terms.md#character)
- **Used on:** `crowd manager` (character detail panel, crowd tree as leaf nodes), `desktop` (character overlay area)
- **Hierarchy / collections:** character belongs to crowd (as crowd member); all characters crowd aggregates all characters flat
- **Preliminary labels and tags:** character name, spawned state, active indicator, combat state (attacker / defender / stunned / dying / dead)
- **Key actions:** create, rename, delete, clone, link across crowds, spawn, clear from desktop, activate (mark turn), deactivate, move, turn, maneuver with camera

---

#### identity

- **Source:** [`identity`](../domain-terms.md#identity)
- **Used on:** `crowd manager` (character detail panel — Identities tab)
- **Hierarchy / collections:** identity belongs to character via Identities option group; types: model identity, costume identity
- **Preliminary labels and tags:** identity name, surface name, identity type (Model / Costume), default indicator, active indicator
- **Key actions:** add, set type, assign surface, set as default, set as active, remove

---

#### animated ability

- **Source:** [`animated ability`](../domain-terms.md#animated-ability)
- **Used on:** `crowd manager` (character detail panel — Abilities tab), `desktop` (activate via keyboard shortcut or context menu)
- **Hierarchy / collections:** animated ability belongs to character via Abilities option group; composed of animation elements (FX, MOV, sound, pause, sequence, reference, identity); attack is a subtype
- **Preliminary labels and tags:** ability name, activation key, persistent flag, attack flag, active state
- **Key actions:** create, edit (compose elements), delete, play, stop, set activation key, toggle persistence

---

#### character movement

- **Source:** [`character movement`](../domain-terms.md#character-movement)
- **Used on:** `crowd manager` (character detail panel — Movements tab), `desktop` (activate movement via keyboard shortcut or context menu)
- **Hierarchy / collections:** character movement belongs to character via Movements option group; types: Walk, Run, Swim, Fly, Jump, etc.
- **Preliminary labels and tags:** movement name, activation key, default movement indicator, distance limit
- **Key actions:** add, edit parameters, remove, set as default, activate

---

#### attack

- **Source:** [`attack`](../domain-terms.md#attack-is-a-type-of-animated-ability)
- **Used on:** `desktop` (initiated from character overlay area during combat)
- **Hierarchy / collections:** attack is a subtype of animated ability; carries attack configuration entries per involved character
- **Preliminary labels and tags:** attack name, area-effect indicator, on-hit animation name, combat state flags per target
- **Key actions:** initiate, select defenders, set effect (stunned / unconscious / dying / dead), set knockback, set result (hit / miss), execute area attack, execute sweep, cancel, reset

---

## Change log

| Date | Direction | Summary |
| --- | --- | --- |
| 2026-05-16 | initial | First draft from story-map.md and domain-terms.md — full application scope. |
