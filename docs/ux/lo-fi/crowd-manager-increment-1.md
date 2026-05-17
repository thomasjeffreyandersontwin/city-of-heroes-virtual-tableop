# Lo-fi wireframe — Crowd Manager (Increment 1)

**Scope:** Increment 1 — Character and Crowd Library  
**Wireframe:** `crowd-manager-increment-1.drawio`  
**State file:** `crowd-manager-increment-1-state.json`  
**AC source:** `docs/stories/acceptance-criteria-increment-1.md`  
**Domain terms:** `docs/domain/domain-terms.md`

---

## Screens in this pass

### game directory prompt

**Layout:** modal  
**AC stories:** Validate City of Heroes Game Directory · Prompt for Game Directory if Invalid

| Region | Type | Controls | Interaction decisions |
| --- | --- | --- | --- |
| directory entry form | form | COH game directory path (text) · validation error (text, conditional) · Browse... (secondary button) · Continue (primary button, disabled until valid) | Continue is disabled until validation passes; error message appears inline on failed submit |

**Conditional states:**
- Default: path field empty, Continue disabled
- Error: validation error text visible ("path not found — missing HookCostume.dll")
- Valid: error cleared, Continue enabled

---

### crowd manager

**Layout:** sidebar (crowd tree 33% · tab content 67%)  
**AC stories:** Create Crowd · Rename Crowd · Delete Crowd · Nest Crowd · Create Character in Crowd · Rename Character · Delete Character · Clone Character · Cut Character to Clipboard · Link Character across Crowds · Clone-Link Character · Flatten-Copy Crowd · Clone Memberships · Drag-Drop Character · Filter Characters by Name · Browse Crowds by Concept · Browse Crowds by Gangs · Browse Crowds by COH Structure · Browse All Characters Crowd · Save Crowd Collection

| Region | Slot | Type | Controls | Interaction decisions |
| --- | --- | --- | --- | --- |
| filter by name | panel | form | filter text input · Clear button | Live filter — updates tree on every keystroke; Clear resets to full tree |
| crowd tree | panel | list | crowd / character · gang mode column | Context menu on right-click or selection: New Crowd, New Character, Rename, Delete, Clone, Cut, Link, Clone-Link, Flatten-Copy, Clone Memberships; Drag-drop to reorder or move between crowds |
| tab bar | body | nav-tabs | Identities (active) · Abilities · Movements | Tab switches active panel; crowd tree stays visible and interactive across all tabs |
| identity list | body | list | name · type · active · default — Add · Remove · Set Default · Set Active | Increment 1 scope: identity management is future work; panel present but not detailed in this pass |

**Conditional states (crowd tree):**
- Empty crowd: shows "no members" placeholder + New Character / New Crowd actions
- All Characters crowd selected: Delete is disabled
- After Cut: character dimmed in tree until pasted
- Filter active: non-matching nodes hidden; matching ancestors remain visible and expanded

---

## Affordance trace

| Affordance | AC story | AC clause |
| --- | --- | --- |
| COH game directory path input | Prompt for Game Directory if Invalid | AC 1 — GM shown input screen to supply or browse for path |
| Browse... button | Prompt for Game Directory if Invalid | AC 1 — browse for installation path |
| Validation error text | Prompt for Game Directory if Invalid | AC 3 — input screen remains open with a clear error |
| Continue button (primary) | Prompt for Game Directory if Invalid | AC 2 — saves path and continues startup if validation passes |
| filter text input | Filter Characters by Name | AC 1 — each crowd member and crowd name tested against filter pattern |
| Clear button | Filter Characters by Name | AC 3 — all crowds and crowd members shown when filter cleared |
| crowd tree rows | Browse Crowds by Concept / Browse Crowds by COH Structure / Browse All Characters Crowd | AC 1 per story |
| New Crowd action | Create Crowd | AC 1 — new crowd with unique default name added, selected, ready to rename |
| New Character action | Create Character in Crowd | AC 1 — new crowd member with unique default name added under selected crowd |
| Rename action | Rename Crowd · Rename Character | AC 1 per story |
| Delete action (disabled for All Characters) | Delete Crowd · Delete Character | AC 3 — deletion of all characters crowd blocked |
| Clone action | Clone Character | AC 1 — independent copy with same configuration added to same crowd |
| Cut action | Cut Character to Clipboard | AC 1 — character placed on clipboard and removed from crowd |
| Link action | Link Character across Crowds | AC 1 — same character instance added as crowd member of second crowd |
| Clone-Link action | Clone-Link Character | AC 1 — new crowd member entry pointing to same character in target crowd |
| Flatten-Copy action | Flatten-Copy Crowd into Numbered Characters | AC 1 — each member deep-cloned with numeric suffix, replaces originals |
| Clone Memberships action | Clone Memberships to Another Crowd | AC 1 — new crowd created with same hierarchical membership structure (shared refs) |
| Drag-drop gesture | Drag-Drop Character between Crowds | AC 1 — crowd member removed from source, added to destination |
| gang mode column | Browse Crowds by Gangs | AC 1 — gang mode status visible in crowd manager |
