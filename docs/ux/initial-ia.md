# Initial information architecture — Hero Virtual Tabletop

> This markdown is the structured spec. Author or update **this file first**, then drive the diagram from it. After the diagram is updated, sync any change back into this file.

## Metadata

| Field | Value |
| --- | --- |
| Scope | Full Application — all 9 epics |
| Story map | `docs/stories/story-map.md` |
| Ubiquitous language | `docs/domain/domain-terms.md` |
| Diagram | `docs/ux/initial-ia.drawio` |
| Last updated | 2026-05-16 |

## Description

The Hero Virtual Tabletop is a WPF desktop application with two operating contexts. **Pre-session:** a startup validation modal gates entry; the Crowd Manager workspace (three tab-state screens — Identities, Abilities, Movements) lets the GM build and configure the full character library; an Ability Editor screen handles element composition and element management (add FX/MOV/sound/reference/sequence/pause/identity, reorder, remove); a Movement Editor screen handles movement parameter editing; and a Model Browser screen lets the GM generate crowds from COH model lists. **In-session:** a Desktop screen hosts the roster panel, the live game overlay, and the context menu; an Attack Configuration panel appears contextually when combat is active. The IA gives the team an agreed screen inventory and content model for scope reasoning and precise acceptance criteria before lo-fi work begins.

---

## Navigation

### Screens

#### 1. game directory prompt

- **Layout:** `modal` — centered dialog, single column
- **Context:** pre-session — appears on startup if the COH game directory is absent or invalid

```
[game directory prompt]
┌───────────────────────────────────────┐
│ ╔═══════════════════════════════════╗ │
│ ║  directory entry form             ║ │
│ ║   COH game directory              ║ │
│ ║   [path input]       [browse]     ║ │
│ ║   validation feedback             ║ │
│ ╠═══════════════════════════════════╣ │
│ ║   continue (enabled when valid)   ║ │
│ ╚═══════════════════════════════════╝ │
└───────────────────────────────────────┘
```

**Chrome regions:** *(none — modal has no persistent chrome)*

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| directory entry form | form | COH game directory · directory path · browse · validation feedback | continue *(enabled when path valid)* |

**Stories:**
- *(system)* Validate City of Heroes Game Directory
- *(system)* Prompt for Game Directory if Invalid

**Domain terms:** COH game directory

**Transitions out:**
- → crowd manager — identities : *submits valid path*

---

#### 2. crowd manager — identities

- **Layout:** `sidebar` — crowd tree (panel slot, left 35%) · tab content (body slot, right 65%)
- **Context:** pre-session — primary workspace, default tab on open

```
[crowd manager — identities]
┌─────────────────────┬────────────────────────────┐
│ crowd tree          │ [Identities] Abilities Mvt │
│  crowd name (n)     ├────────────────────────────┤
│    > char name      │ identity list              │
│    > type · spawned │  name · type · active      │
│    > active         │  default                   │
│  crowd name (n)     ├────────────────────────────┤
│    > ...            │  add · remove · set-active │
└─────────────────────┴────────────────────────────┘
```

**Chrome regions:** *(none with IA-level content — toolbar and status bar omitted)*

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| crowd tree | tree | crowd name · member count · *(expanded)* char name · type · spawned · active | create crowd · rename crowd · delete crowd · nest crowd · create character · rename character · delete character · clone · cut · link · clone-link · flatten-copy · clone memberships · drag-drop · filter · browse concept · browse coh-structure |
| identity list | list | name · type (model/costume) · active · default | add · remove · set-default · set-active · add ghost · assign-surface · set-type |

*Inactive tab labels (greyed):* Abilities · Movements

**Stories (GM):**
- *(crowd tree — crowd management)* Create Crowd · Rename Crowd · Delete Crowd · Nest Crowd inside Crowd
- *(crowd tree — character management)* Create Character in Crowd · Rename Character · Delete Character from Crowd · Clone Character · Cut Character to Clipboard · Link Character across Crowds · Clone-Link Character · Flatten-Copy Crowd into Numbered Characters · Clone Memberships to Another Crowd · Drag-Drop Character between Crowds · Filter Characters by Name
- *(crowd tree — browse)* Browse Crowds by Concept · Browse Crowds by COH Structure
- *(identity list)* Add Identity to Character · Set Identity Type (Model or Costume) · Assign Costume Surface to Identity · Set Default Identity · Set Active Identity · Remove Identity from Character · Superimpose Ghost on Model Character

**Domain terms:** identity · model identity · costume identity · active identity · default identity · crowd · crowd member

**Transitions out:**
- → crowd manager — abilities : *selects Abilities tab* *(dashed)*
- → crowd manager — movements : *selects Movements tab* *(dashed)*
- → model browser : *opens model browser*
- → desktop : *starts game session*

---

#### 3. crowd manager — abilities

- **Layout:** `sidebar` — same template as crowd manager — identities; crowd tree repeated in panel slot (`--dimmed`), abilities tab in body slot

```
[crowd manager — abilities]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬────────────────────────────┐
╎ crowd tree           ╎ Identities [Abilities] Mvt │
╎  crowd name (n)      ├────────────────────────────┤
╎    > char name       │ ability list               │
╎    > type · spawned  │  name · activation key     │
╎    > active          │  persistent · attack flag  │
╎  (dimmed)            ├────────────────────────────┤
╎                      │  create · delete · play    │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴────────────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| ability list | list | name · activation key · persistent · attack flag | create · delete · set-key · toggle-persistence · set-default · play · stop · edit |

*edit → opens ability editor*

*Inactive tab labels (greyed):* Identities · Movements

**Stories (GM):**
- Create Animated Ability
- Delete Animated Ability
- Set Ability Activation Key
- Toggle Ability Persistence
- Set Default Ability for Character
- Play Animated Ability on Character
- Stop Active Ability

**Domain terms:** animated ability · activation key · persistent

**Transitions out:**
- → crowd manager — identities : *selects Identities tab* *(dashed)*
- → crowd manager — movements : *selects Movements tab* *(dashed)*
- → ability editor : *opens ability editor*

---

#### 4. crowd manager — movements

- **Layout:** `sidebar` — same template as crowd manager — identities; crowd tree repeated in panel slot (`--dimmed`), movements tab in body slot

```
[crowd manager — movements]
┌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┬────────────────────────────┐
╎ crowd tree           ╎ Identities Abilities [Mvt] │
╎  crowd name (n)      ├────────────────────────────┤
╎    > char name       │ movement list              │
╎    > type · spawned  │  name · activation key     │
╎    > active          │  default · type            │
╎  (dimmed)            ├────────────────────────────┤
╎                      │  add · remove · edit       │
└╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌╌┴────────────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| movement list | list | name · activation key · default · type | add · remove · set-default · set-key · edit |

*Inactive tab labels (greyed):* Identities · Abilities

**Stories (GM):**
- Add Movement to Character
- Remove Movement from Character
- Set Default Movement
- Set Movement Activation Key
- Edit Movement Parameters

**Domain terms:** character movement · activation key · movement instruction

**Transitions out:**
- → crowd manager — identities : *selects Identities tab* *(dashed)*
- → crowd manager — abilities : *selects Abilities tab* *(dashed)*
- → movement editor : *selects edit on movement row*

---

#### 5. movement editor

- **Layout:** `form` — single column; movement config form
- **Context:** pre-session — opened from crowd manager — movements via edit action

```
[movement editor]
┌────────────────────────────────────────┐
│ movement config                        │
│  name · activation key                 │
│  default · type                        │
├────────────────────────────────────────┤
│  save · cancel                         │
└────────────────────────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| movement config | form | name · activation key · default · type | save · cancel |

**Stories (GM):**
- Edit Movement Parameters

**Domain terms:** character movement · activation key

**Transitions out:**
- → crowd manager — movements : *saves / cancels*

---

#### 6. ability editor

- **Layout:** `form` — single column; ability config form stacked above element list
- **Context:** pre-session — opened from crowd manager — abilities via edit action

```
[ability editor]
┌────────────────────────────────────────┐
│ ability config                         │
│  name · activation key                 │
│  persistent · attack flag              │
├────────────────────────────────────────┤
│  save · cancel                         │
├────────────────────────────────────────┤
│ element list                           │
│  type · resource · order · persistent  │
├────────────────────────────────────────┤
│  add FX · add MOV · add sound          │
└────────────────────────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| ability config | form | name · activation key · persistent · attack flag | save · cancel |
| element list | list | type · resource · order · persistent flag | add FX · add MOV · add sound · add reference · add sequence · add pause · add identity · reorder · remove |

**Stories (GM):**
- Edit Animated Ability
- Add FX Element to Ability
- Add MOV Element to Ability
- Add Sound Element to Ability
- Add Reference Element to Another Ability
- Add Sequence Element (And/Or)
- Add Pause Element
- Add Load-Identity Element
- Reorder Animation Elements via Drag-Drop
- Remove Animation Element
- Browse FX / Movement / Sound Resources for Ability Authoring

**Domain terms:** animated ability · animation element · FX effect element · MOV element · sound element · animation resource

**Transitions out:**
- → crowd manager — abilities : *saves / cancels*

---

#### 6. model browser

- **Layout:** `modal` — centered floating panel, single column
- **Context:** pre-session — opened from crowd manager to build a crowd from COH model list

```
[model browser]
┌───────────────────────────────────────┐
│ ╔═══════════════════════════════════╗ │
│ ║  model list                       ║ │
│ ║   model name · type               ║ │
│ ║   Skull_Lt_01                     ║ │
│ ║   Clockwork_Gear_01               ║ │
│ ╠═══════════════════════════════════╣ │
│ ║   select · deselect · create crowd║ │
│ ╚═══════════════════════════════════╝ │
└───────────────────────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| model list | list | model name · type | select · deselect · create crowd from selection |

**Stories (GM):**
- Create Crowd from COH Model List
- Select Models to Include in Crowd

**Domain terms:** crowd · character

**Transitions out:**
- → crowd manager — identities : *creates crowd / cancels*

---

#### 7. desktop

- **Layout:** `split-screen` — roster panel (left slot, 50%) · game overlay + context menu stacked (right slot, 50%)
- **Context:** in-session — active once game session begins

```
[desktop]
┌────────────────────┬────────────────────┐
│ roster panel       │ game overlay       │
│  name · spawned    │  character overlay │
│  active · status   │  status indicator  │
├────────────────────┼────────────────────┤
│  add · spawn · act │  select · drag     │
│                    ├────────────────────┤
│                    │ context menu       │
│                    │  target character  │
│                    ├────────────────────┤
│                    │  spawn · place     │
└────────────────────┴────────────────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| roster panel | list | character name · spawned · active · status | add · add-crowd · spawn · remove · clear · activate · deactivate · activate-gang · deactivate-gang |
| game overlay | list | character overlay · status indicator | select · multi-select · drag to position · double-click to activate |
| context menu | list | target character | spawn · place at location · save-position · move-camera-to-target · move-target-to-camera · move-to-location · teleport-to-camera · move-crowd-relative · move-crowd-spread · turn-to-target · align-with-gang-leader · follow · unfollow · maneuver-with-camera · reset-orientation · activate-option · clone-link |

**Stories (GM):**
- *(roster panel)* Add Character to Roster · Add Crowd to Roster · Spawn Character to Desktop · Remove Character from Roster · Clear Character from Desktop
- *(roster panel — activation)* Activate Character · Deactivate Character · Activate Crowd as Gang with Gang Leader · Deactivate Gang
- *(game overlay)* Select Character on Desktop via Mouse Click · Multi-Select Characters · Drag Character to New Position · Double-Click Character to Activate
- *(context menu — spawn/place)* Spawn Character via Context Menu · Place Character at Location · Save Character Position · Clone and Link Character from Desktop · Activate Character Option via Context Menu
- *(context menu — camera)* Move Camera to Target Character · Move Target Character to Camera
- *(context menu — movement)* Move Character to Location · Move Character to Camera Position · Teleport Character to Camera · Move Crowd with Relative Positioning · Move Crowd with Optimal Spread Positioning
- *(context menu — orientation)* Turn Character towards Target · Reset Character Orientation · Align Character Facing with Gang Leader · Maneuver Character with Camera via Context Menu
- *(context menu — follow)* Follow Character with Game Camera · Unfollow Character

**Domain terms:** roster · desktop · active character · gang mode · gang leader · spawned character

**Transitions out:**
- → attack configuration : *activates attack ability*

---

#### 8. attack configuration

- **Layout:** `flyout` — combatant selectors + attack parameters stacked in body slot (65%); panel slot unused (no secondary content at IA level)
- **Context:** in-session — active during attack workflow

```
[attack configuration]
┌─────────────────────────┬───────────┐
│ combatant selectors     │           │
│  name · role            │  (panel   │
│  attacker / defender    │   slot:   │
├─────────────────────────┤   no IA   │
│  select · confirm       │   content)│
├─────────────────────────┤           │
│ attack parameters       │           │
│  effect · knockback     │           │
│  result · mode · center │           │
├─────────────────────────┤           │
│  confirm · cancel       │           │
└─────────────────────────┴───────────┘
```

**Content regions:**

| Region | Type | Visible fields | Actions |
| --- | --- | --- | --- |
| combatant selectors | list | character name · role (attacker/defender) | select attacker · add defender · remove defender · confirm targets |
| attack parameters | form | attack effect · knockback distance · attack result · attack mode · area center · sweep targets · auto-fire shots per target | confirm · cancel · abort |

**Stories (GM):**
- Select Attacking Character
- Activate Attack Ability
- Select Defender Targets
- Confirm Attack Targets
- Set Attack Effect (Stunned / Unconscious / Dying / Dead)
- Set Knockback Distance
- Set Attack Result (Hit or Miss)
- Set Attack Mode (Attack or Defend)
- Designate Center Target for Area Attack
- Execute Ranged Area Attack
- Execute Sweep Attack across Multiple Targets
- Assign Auto-Fire Shots per Target
- Spread Attack across Crowd
- Cancel Active Attack
- Abort Attack in Progress
- Reset Character Combat State

**Domain terms:** attack · attack configuration · attack effect

**Transitions out:**
- → desktop : *confirms / cancels attack*

---

### Transitions summary

| From | To | Trigger |
| --- | --- | --- |
| game directory prompt | crowd manager — identities | submits valid path |
| crowd manager — identities | crowd manager — abilities | selects Abilities tab |
| crowd manager — identities | crowd manager — movements | selects Movements tab |
| crowd manager — abilities | crowd manager — identities | selects Identities tab |
| crowd manager — abilities | crowd manager — movements | selects Movements tab |
| crowd manager — movements | crowd manager — identities | selects Identities tab |
| crowd manager — movements | crowd manager — abilities | selects Abilities tab |
| crowd manager — abilities | ability editor | opens ability editor |
| ability editor | crowd manager — abilities | saves / cancels |
| crowd manager — movements | movement editor | selects edit on movement row |
| movement editor | crowd manager — movements | saves / cancels |
| crowd manager — identities | model browser | opens model browser |
| model browser | crowd manager — identities | creates crowd / cancels |
| crowd manager — identities | desktop | starts game session |
| desktop | attack configuration | activates attack ability |
| attack configuration | desktop | confirms / cancels attack |

### Navigational components

| Component | Type | Links to | Diagram note |
| --- | --- | --- | --- |
| toolbar | persistent top chrome | (commands only — no navigation) | omitted from diagram: no IA-level content |
| crowd tree | persistent left panel (`sidebar` panel slot) | all crowd manager tab states | shown on all three crowd manager screens; greyed (`--dimmed`) on sibling tabs |
| tab bar (Identities / Abilities / Movements) | in-body nav | crowd manager — identities · abilities · movements | inactive labels shown greyed in tab bar area of each sibling screen |
| status bar | persistent footer chrome | (status display only) | omitted from diagram: no IA-level content |

---

## Content types

| Content type | Hierarchy / collections | Key actions |
| --- | --- | --- |
| crowd | nested crowd hierarchy; crowd repository | create · rename · delete · nest · filter · save-positions |
| character (crowd member) | member of crowd | create · rename · clone · link · flatten-copy · drag-drop · filter |
| identity | option group on character (Identities) | add · set-default · set-active · remove · assign-surface · set-type |
| animated ability | option group on character (Abilities) | create · edit · delete · play · stop · set-key · toggle-persistence |
| character movement | option group on character (Movements) | add · edit · remove · set-default · set-key |
| animation element | ordered list on animated ability | add (FX/MOV/sound/reference/sequence/pause/identity) · reorder · remove |

---

## Story trace table

| Story | Screen | Region | Action / trigger |
| --- | --- | --- | --- |
| *(S)* Validate Game Directory | game directory prompt | directory entry form | validation feedback |
| *(S)* Prompt if Invalid | game directory prompt | directory entry form | conditional form display |
| Create Crowd | crowd manager — identities | crowd tree | create crowd |
| Rename Crowd | crowd manager — identities | crowd tree | rename crowd |
| Delete Crowd | crowd manager — identities | crowd tree | delete crowd |
| Nest Crowd inside Crowd | crowd manager — identities | crowd tree | nest crowd |
| Create Character in Crowd | crowd manager — identities | crowd tree | create character |
| Rename Character | crowd manager — identities | crowd tree | rename character |
| Delete Character from Crowd | crowd manager — identities | crowd tree | delete character |
| Clone Character | crowd manager — identities | crowd tree | clone |
| Cut Character to Clipboard | crowd manager — identities | crowd tree | cut |
| Link Character across Crowds | crowd manager — identities | crowd tree | link |
| Clone-Link Character | crowd manager — identities | crowd tree | clone-link |
| Flatten-Copy Crowd into Numbered Characters | crowd manager — identities | crowd tree | flatten-copy |
| Clone Memberships to Another Crowd | crowd manager — identities | crowd tree | clone memberships |
| Drag-Drop Character between Crowds | crowd manager — identities | crowd tree | drag-drop |
| Filter Characters by Name | crowd manager — identities | crowd tree | filter |
| Browse Crowds by Concept | crowd manager — identities | crowd tree | browse concept |
| Browse Crowds by COH Structure | crowd manager — identities | crowd tree | browse coh-structure |
| Add Identity to Character | crowd manager — identities | identity list | add |
| Set Identity Type (Model or Costume) | crowd manager — identities | identity list | set-type |
| Assign Costume Surface to Identity | crowd manager — identities | identity list | assign-surface |
| Set Default Identity | crowd manager — identities | identity list | set-default |
| Set Active Identity | crowd manager — identities | identity list | set-active |
| Remove Identity from Character | crowd manager — identities | identity list | remove |
| Superimpose Ghost on Model Character | crowd manager — identities | identity list | add ghost |
| Create Animated Ability | crowd manager — abilities | ability list | create |
| Delete Animated Ability | crowd manager — abilities | ability list | delete |
| Set Ability Activation Key | crowd manager — abilities | ability list | set-key |
| Toggle Ability Persistence | crowd manager — abilities | ability list | toggle-persistence |
| Set Default Ability for Character | crowd manager — abilities | ability list | set-default |
| Play Animated Ability on Character | crowd manager — abilities | ability list | play |
| Stop Active Ability | crowd manager — abilities | ability list | stop |
| Edit Animated Ability | ability editor | ability config | save |
| Add FX Element to Ability | ability editor | element list | add FX |
| Add Movement Element to Ability (MOV) | ability editor | element list | add MOV |
| Add Sound Element to Ability | ability editor | element list | add sound |
| Add Reference Element to Another Ability | ability editor | element list | add reference |
| Add Sequence Element (And/Or) | ability editor | element list | add sequence |
| Add Pause Element | ability editor | element list | add pause |
| Add Load-Identity Element | ability editor | element list | add identity |
| Reorder Animation Elements | ability editor | element list | reorder |
| Remove Animation Element | ability editor | element list | remove |
| Browse FX Resources | ability editor | element list | add FX (resource picker) |
| Browse Movement Resources | ability editor | element list | add MOV (resource picker) |
| Browse Sound Resources | ability editor | element list | add sound (resource picker) |
| Add Movement to Character | crowd manager — movements | movement list | add |
| Remove Movement from Character | crowd manager — movements | movement list | remove |
| Set Default Movement | crowd manager — movements | movement list | set-default |
| Set Movement Activation Key | crowd manager — movements | movement list | set-key |
| Edit Movement Parameters | crowd manager — movements | movement list | edit → movement editor |
| Edit Movement Parameters (editor) | movement editor | movement config | save |
| Create Crowd from COH Model List | model browser | model list | create crowd from selection |
| Select Models to Include in Crowd | model browser | model list | select · deselect |
| Add Character to Roster | desktop | roster panel | add |
| Add Crowd to Roster | desktop | roster panel | add-crowd |
| Spawn Character to Desktop from Roster | desktop | roster panel | spawn |
| Remove Character from Roster | desktop | roster panel | remove |
| Clear Character from Desktop | desktop | roster panel | clear |
| Activate Character | desktop | roster panel | activate |
| Deactivate Character | desktop | roster panel | deactivate |
| Activate Crowd as Gang with Gang Leader | desktop | roster panel | activate-gang |
| Deactivate Gang | desktop | roster panel | deactivate-gang |
| Select Character on Desktop via Mouse Click | desktop | game overlay | select |
| Multi-Select Characters | desktop | game overlay | multi-select |
| Drag Character to New Position on Desktop | desktop | game overlay | drag to position |
| Double-Click Character to Activate | desktop | game overlay | double-click to activate |
| Spawn Character via Context Menu | desktop | context menu | spawn |
| Place Character at Location | desktop | context menu | place at location |
| Save Character Position | desktop | context menu | save-position |
| Clone and Link Character from Desktop | desktop | context menu | clone-link |
| Activate Character Option via Context Menu | desktop | context menu | activate-option |
| Move Camera to Target Character | desktop | context menu | move-camera-to-target |
| Move Target Character to Camera | desktop | context menu | move-target-to-camera |
| Move Character to Location | desktop | context menu | move-to-location |
| Move Character to Camera Position | desktop | context menu | move-to-location (camera) |
| Teleport Character to Camera | desktop | context menu | teleport-to-camera |
| Move Crowd with Relative Positioning | desktop | context menu | move-crowd-relative |
| Move Crowd with Optimal Spread Positioning | desktop | context menu | move-crowd-spread |
| Turn Character towards Target | desktop | context menu | turn-to-target |
| Reset Character Orientation | desktop | context menu | reset-orientation |
| Align Character Facing with Gang Leader | desktop | context menu | align-with-gang-leader |
| Maneuver Character with Camera via Context Menu (Activate Maneuver-with-Camera Mode) | desktop | context menu | maneuver-with-camera |
| Follow Character with Game Camera | desktop | context menu | follow |
| Unfollow Character | desktop | context menu | unfollow |
| Select Attacking Character | attack configuration | combatant selectors | select attacker |
| Activate Attack Ability | desktop → attack configuration | — | activates attack ability (transition) |
| Select Defender Targets | attack configuration | combatant selectors | add defender |
| Confirm Attack Targets | attack configuration | combatant selectors | confirm targets |
| Set Attack Effect (Stunned / Unconscious / Dying / Dead) | attack configuration | attack parameters | attack effect field |
| Set Knockback Distance | attack configuration | attack parameters | knockback distance field |
| Set Attack Result (Hit or Miss) | attack configuration | attack parameters | attack result field |
| Set Attack Mode (Attack or Defend) | attack configuration | attack parameters | attack mode field |
| Designate Center Target for Area Attack | attack configuration | attack parameters | area center field |
| Execute Ranged Area Attack | attack configuration | attack parameters | confirm (area mode) |
| Execute Sweep Attack across Multiple Targets | attack configuration | attack parameters | sweep targets field |
| Assign Auto-Fire Shots per Target | attack configuration | attack parameters | auto-fire shots field |
| Spread Attack across Crowd | attack configuration | attack parameters | spread attack (crowd target) |
| Cancel Active Attack | attack configuration | attack parameters | cancel |
| Abort Attack in Progress | attack configuration | attack parameters | abort |
| Reset Character Combat State | attack configuration | combatant selectors | reset combat state |

---

## Domain term trace table

| Domain term | Appears as | Screen | Region |
| --- | --- | --- | --- |
| COH game directory | form field label | game directory prompt | directory entry form |
| identity | region name (identity list) | crowd manager — identities | identity list |
| model identity | row type label (type field value) | crowd manager — identities | identity list |
| costume identity | row type label (type field value) | crowd manager — identities | identity list |
| active identity | field label (active) | crowd manager — identities | identity list |
| default identity | field label (default) | crowd manager — identities | identity list |
| crowd | region name (crowd tree) | crowd manager — identities | crowd tree |
| crowd member | row in crowd tree | crowd manager — identities | crowd tree |
| animated ability | region name (ability list) | crowd manager — abilities | ability list |
| activation key | field label | crowd manager — abilities | ability list |
| character movement | region name (movement list) | crowd manager — movements | movement list |
| animation element | row in element list | ability editor | element list |
| FX effect element | row type label (type field value) | ability editor | element list |
| MOV element | row type label (type field value) | ability editor | element list |
| sound element | row type label (type field value) | ability editor | element list |
| animation resource | row field (resource) | ability editor | element list |
| roster | region name (roster panel) | desktop | roster panel |
| desktop | screen name | desktop | — |
| active character | field label (active) | desktop | roster panel |
| gang mode | action label (activate-gang) | desktop | roster panel |
| gang leader | field label in roster row | desktop | roster panel |
| spawned character | field label (spawned) | desktop | roster panel |
| attack | row type in combatant selectors | attack configuration | combatant selectors |
| attack configuration | screen name | attack configuration | — |
| attack effect | form field label | attack configuration | attack parameters |

---

## Change log

| Date | Direction | Summary |
| --- | --- | --- |
| 2026-05-16 | authored | initial spec from story map + domain terms, full application scope |
| 2026-05-16 | md update | layout fields updated to standard template names (modal/sidebar/form/split-screen/flyout); empty chrome (toolbar, status bar) noted as omitted; navigational components table updated with diagram notes |
| 2026-05-16 | md update | added movement editor screen; added → movement editor transition from CM movements; added ability editor stories for add reference / sequence / pause / identity; added add identity action to element list; synced story trace and transition summary tables |
| 2026-05-16 | comprehensive sync | full cross-check against story map: added all missing GM stories across every screen; expanded crowd tree actions (cut, clone memberships, drag-drop, browse concept/COH structure); expanded identity list actions (assign-surface, set-type); added set-default to ability list; added remove-element story to ability editor; expanded desktop context menu with all movement/orientation/follow actions and expanded roster/overlay stories; added area/sweep/spread/cancel/abort/reset stories to attack configuration; expanded story trace table to cover all GM stories |
