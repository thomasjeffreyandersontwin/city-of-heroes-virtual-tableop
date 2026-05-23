# Refactoring Thin Slicing — Hero Virtual Tabletop

## Product / context

**Product:** Hero Virtual Tabletop (HVT) — WPF desktop application driving City of Heroes / Titan Icon.

**Purpose of this document:** Map the refactoring pipeline — strangler-style extraction of domain logic from fat ViewModels — onto vertical, test-first delivery slices. This document is **separate** from the feature-delivery thin slicing (`docs/stories/thin-slicing.md`). It governs the internal quality track, not new GM-facing features.

**Hard gate (locked, non-negotiable):** Tier 1 (domain) and Tier 2 (ViewModel binding) tests must be written from SBE, follow `abd-acceptance-test-driven-development` + `abd-clean-code`, and `dotnet test` must be GREEN — **before any production refactor line is touched** in that increment's scope.

**Folder alignment rule:** `tests/domain/` story folders use the **same sub-epic names** as `tests/e2e/` — this is intentional. Traceability from SBE scenario → Tier 1 class/method → Tier 3 class/method is one-to-one.

**Slicing strategy:** COH seam fakes first (unlock testability), then crowd persistence (Inc 1 foundation), then OptionGroup pattern + identity/ability (Inc 2/3 typed subclasses), then movement extraction (Inc 4), then roster/desktop domain (Inc 5), then combat/orchestration (Inc 6). Each slice is end-to-end: tests RED → GREEN → production extracted.

---

## Refactoring Increments

### Refactoring Slice R0: COH Seam Scaffold

**Outcome:** Test infrastructure in place — domain test project compiles, COH boundary fakes exist, composition root wiring pattern is established. No behavior changed; no tests written yet.

**SBE source:** N/A — cross-cutting infrastructure; no feature SBE drives this slice.

**Slicing notes:** This is the prerequisite for all subsequent slices. Without `FakeMemoryInstance` and `NoOpGameCommandExecutor`, Tier 1 domain tests cannot compile (they would need live COH DLLs). This slice is deliberately non-behavioral: it proves the seam boundary, wires the Unity composition root for test injection, and sets up `GameCommandTestAssemblyHooks` for test isolation. Equivalent to "put the scaffolding up before painting."

**Domain tests (Tier 1 + Tier 2) — to create, not test bodies:**

- `tests/domain/Support/`
  - `FakeMemoryInstance.cs` — implements `IMemoryInstance`; returns configurable canned values
  - `NoOpGameCommandExecutor.cs` — implements `IGameCommandExecutor`; records calls for assertion
  - `GameCommandTestAssemblyHooks.cs` — MSTest `[AssemblyInitialize]` / `[AssemblyCleanup]`; no live game required

**Test project scaffold:**
- `tests/domain/CrowdManagement.DomainTests.csproj` — references production assemblies; wires MSTest + fakes
- No test class bodies in this slice

**Production changes:** None. Zero production lines changed.

**Gate:** `dotnet build` on `CrowdManagement.DomainTests.csproj` returns zero errors.

---

### Refactoring Slice R1: Increment 1 — Crowd Persistence and Tree

**Outcome:** Tier 1 + Tier 2 domain tests for Increment 1 crowd persistence are GREEN; `CrowdTree` and `CrowdRepository` extracted from `CharacterExplorerViewModel` (2,660 LOC) into `src/Crowds/`.

**SBE source:** `docs/increment-1/specification-by-example-increment-1.md`

**Slicing notes:** Increment 1 is the lowest-dependency slice — no game connection required. The SBE covers file load/save, crowd CRUD, and clipboard operations, all of which live today in the fat `CharacterExplorerViewModel`. This is the first concrete end-to-end: SBE scenario → Tier 1 test RED → domain API written → Tier 1 GREEN → ViewModel slimmed to one-liners → E2E stays green. `manage_crowd_repository` folder name matches the existing `tests/e2e/manage_crowd_repository/` exactly — same class and method names across all three tiers.

**Domain tests (Tier 1 + Tier 2) — `tests/domain/manage_crowd_repository/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `load_active_crowd_files_on_startup/` | `LoadActiveCrowdFilesOnStartup.cs` | Tier 1 (+ optional Tier 2 VM) |
| `browse_and_activate_crowd_files/` | `BrowseAndActivateCrowdFiles.cs` | Tier 1 |
| `save_dirty_to_source_files/` | `SaveDirtyToSourceFiles.cs` | Tier 1 |
| `save_crowd_to_new_file/` | `SaveCrowdToNewFile.cs` | Tier 1 |
| `track_source_file_per_crowd/` | `TrackSourceFilePerCrowd.cs` | Tier 1 |

Sub-epic helper: `tests/domain/manage_crowd_repository/ManageCrowdRepositoryHelper.cs` — mirrors `tests/e2e/manage_crowd_repository/ManageCrowdRepositoryHelper.cs` in shape; helpers call domain (`CrowdTree`, `CrowdRepository`) instead of `AppDriver`.

**Test structure rule (ATDD):** Class = story (exact PascalCase match to SBE), method = scenario outcome. Given/When/Then methods on `ManageCrowdRepositoryHelper`; test bodies ≤ 20 lines.

**Gate:** All 5 story files compile and `dotnet test` passes.

**Production extraction (only after gate):**

- `src/Crowds/CrowdTree.cs` — crowd display/orchestration surface; VM binds to this
- `src/Crowds/CrowdRepository.cs` — registry and JSON persistence
- `src/Crowds/Crowd.cs` — crowd aggregate
- `src/Crowds/CrowdMember.cs` — fix `CrowdMember : Character` inheritance → compose `Character` inside `CrowdMember`
- `src/Crowds/Clipboard.cs` — cut/paste character cross-crowd
- Slim `CharacterExplorerViewModel` crowd persistence commands to one-liners (≤ 3 lines each)

**Traceability:** `manage_crowd_repository` → `tests/domain/manage_crowd_repository/` → `tests/e2e/manage_crowd_repository/`

---

### Refactoring Slice R2: Increments 2 & 3 — OptionGroup Pattern, Identity, and Animated Ability

**Outcome:** Tier 1 + Tier 2 domain tests for identity management and animated ability management are GREEN; `OptionGroup` abstract base and three typed subclasses extracted; `OptionGroupViewModel` slimmed below 200 LOC.

**SBE source:**
- `docs/increment-2/specification-by-example-increment-2.md` — identity management stories
- `docs/increment-3/specification-by-example-increment-3.md` — animated ability management stories

**Slicing notes:** Identity and ability share the same `OptionGroup` selection pattern — choosing which identity/ability is active/default, respecting "one active at a time" invariants. Writing both together forces the `OptionGroup` abstraction to emerge from two concrete cases, not from a single case. The OptionGroup abstract base is in scope here because the SBE for both increments exercises selection and active-state semantics. Game bridge initialization, keybind, costume, ghost, and model browser stories are **not in this slice** — they require COH integration testing and belong in a separate integration track.

**Domain tests (Tier 1 + Tier 2) — `tests/domain/identity_management/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `add_identity_to_character/` | `AddIdentityToCharacter.cs` | Tier 1 |
| `assign_costume_surface_to_identity/` | `AssignCostumeSurfaceToIdentity.cs` | Tier 1 |
| `remove_identity_from_character/` | `RemoveIdentityFromCharacter.cs` | Tier 1 |
| `set_active_identity/` | `SetActiveIdentity.cs` | Tier 1 |
| `set_default_identity/` | `SetDefaultIdentity.cs` | Tier 1 |
| `set_identity_type/` | `SetIdentityType.cs` | Tier 1 |

Sub-epic helper: `tests/domain/identity_management/IdentityManagementHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/animated_ability_management/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `create_animated_ability/` | `CreateAnimatedAbility.cs` | Tier 1 |
| `delete_animated_ability/` | `DeleteAnimatedAbility.cs` | Tier 1 |
| `edit_animated_ability/` | `EditAnimatedAbility.cs` | Tier 1 |
| `set_ability_activation_key/` | `SetAbilityActivationKey.cs` | Tier 1 |
| `set_default_ability_for_character/` | `SetDefaultAbilityForCharacter.cs` | Tier 1 |
| `toggle_ability_persistence/` | `ToggleAbilityPersistence.cs` | Tier 1 |

Sub-epic helper: `tests/domain/animated_ability_management/AnimatedAbilityManagementHelper.cs`

**Gate:** All 12 story files compile and `dotnet test` passes for both sub-epics.

**Production extraction (only after gate):**

- `src/OptionGroups/OptionGroup.cs` — abstract base; selection/active invariants ("one active at a time")
- `src/Identities/IdentityOptionGroup.cs` — typed identity selection
- `src/AnimatedAbilities/AbilityOptionGroup.cs` — typed ability selection
- `src/Movements/MovementOptionGroup.cs` — typed movement selection (pre-wired for R3)
- Slim `OptionGroupViewModel` from 973 LOC to ≤ 200 LOC (selection commands become one-liners)

**Traceability:**
- `identity_management` → `tests/domain/identity_management/` → `tests/e2e/identity_management/`
- `animated_ability_management` → `tests/domain/animated_ability_management/` → `tests/e2e/animated_ability_management/`

---

### Refactoring Slice R3: Increment 4 — Single Character Movement

**Outcome:** Tier 1 + Tier 2 domain tests for character movement authoring and movement execution are GREEN; `Movement.cs` (2,834 LOC) split into domain-level movement classes; `MovementEditorViewModel` slimmed to one-liners.

**SBE source:** `docs/increment-4/specification-by-example-increment-4.md`

**Slicing notes:** Movement is the largest single-file violation (2,834 LOC). The SBE provides two natural sub-epic cuts: *authoring* (add/edit/remove movement types on a character, set keys and defaults) and *execution* (move/teleport/turn/follow, enforce distance, detect collision). Tier 1 tests for movement execution use `FakeMemoryInstance` at the memory read/write boundary — they do not require a live game process. Camera rig and memory interface stories are **not in this slice** — they cross the COH seam in ways that belong in a dedicated integration track.

**Domain tests (Tier 1 + Tier 2) — `tests/domain/character_movement_authoring/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `add_movement_to_character/` | `AddMovementToCharacter.cs` | Tier 1 |
| `edit_movement_parameters/` | `EditMovementParameters.cs` | Tier 1 |
| `remove_movement_from_character/` | `RemoveMovementFromCharacter.cs` | Tier 1 |
| `set_default_movement/` | `SetDefaultMovement.cs` | Tier 1 |
| `set_movement_activation_key/` | `SetMovementActivationKey.cs` | Tier 1 |
| `add_default_movements_to_character/` | `AddDefaultMovementsToCharacter.cs` | Tier 1 |

Sub-epic helper: `tests/domain/character_movement_authoring/CharacterMovementAuthoringHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/movement_execution/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `animate_movement/` | `AnimateMovement.cs` | Tier 1 (uses `FakeMemoryInstance`) |
| `detect_floor_and_wall_collisions/` | `DetectFloorAndWallCollisions.cs` | Tier 1 |
| `enforce_distance_limit_per_movement_type/` | `EnforceDistanceLimitPerMovementType.cs` | Tier 1 |
| `execute_move_npc_command/` | `ExecuteMoveNpcCommand.cs` | Tier 1 (uses `NoOpGameCommandExecutor`) |
| `move_character_to_camera_position/` | `MoveCharacterToCameraPosition.cs` | Tier 1 |
| `move_character_to_location/` | `MoveCharacterToLocation.cs` | Tier 1 |
| `reset_character_orientation/` | `ResetCharacterOrientation.cs` | Tier 1 |
| `teleport_character_to_camera/` | `TeleportCharacterToCamera.cs` | Tier 1 |
| `track_movement_distance_count/` | `TrackMovementDistanceCount.cs` | Tier 1 |
| `turn_character_towards_target/` | `TurnCharacterTowardsTarget.cs` | Tier 1 |

Sub-epic helper: `tests/domain/movement_execution/MovementExecutionHelper.cs`

**Gate:** All 16 story files compile and `dotnet test` passes for both sub-epics.

**Production extraction (only after gate):**

- Split `src/Movements/Movement.cs` (2,834 LOC) into focused classes:
  - `MovementExecution.cs` — move/teleport/turn/follow execution logic
  - `CharacterMovement.cs` — movement authoring (type, key, default, constraints)
- Inject `IMemoryInstance` via constructor; remove `new MemoryElement()` sites
- Slim `MovementEditorViewModel` from 717 LOC to one-liner command handlers

**Traceability:**
- `character_movement_authoring` → `tests/domain/character_movement_authoring/` → `tests/e2e/character_movement_authoring/`
- `movement_execution` → `tests/domain/movement_execution/` → `tests/e2e/movement_execution/`

---

### Refactoring Slice R4: Increment 5 — Roster and Desktop

**Outcome:** Tier 1 + Tier 2 domain tests for roster and desktop overlay are GREEN; `Roster`, `RosterEntry`, `ActiveCharacter`, and `GangMode` extracted from `RosterExplorerViewModel` (3,701 LOC); EventAggregator roster sync replaced with domain subscriptions; `RosterExplorerViewModel` reduced toward ≤ 300 LOC.

**SBE source:** `docs/increment-5/specification-by-example-increment-5.md`

**Slicing notes:** Roster is the largest ViewModel (3,701 LOC) and the most complex domain gap — `Roster`, `ActiveCharacter`, and `GangMode` types do not exist; all logic lives on the ViewModel. This slice targets the pure domain behavior (who is in the roster, who is active, gang state) and desktop overlay interaction (select, multi-select, drag, spawn state). Game state query stories and pop-up menu stories are scoped out of the domain test pass — they cross the COH seam; those stories are in the E2E suite already.

**Domain tests (Tier 1 + Tier 2) — `tests/domain/roster/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `add_character_to_roster/` | `AddCharacterToRoster.cs` | Tier 1 |
| `add_crowd_to_roster/` | `AddCrowdToRoster.cs` | Tier 1 |
| `spawn_character_to_desktop_from_roster/` | `SpawnCharacterToDesktopFromRoster.cs` | Tier 1 (uses `NoOpGameCommandExecutor`) |
| `remove_character_from_roster/` | `RemoveCharacterFromRoster.cs` | Tier 1 |
| `clear_character_from_desktop/` | `ClearCharacterFromDesktop.cs` | Tier 1 |
| `activate_character/` | `ActivateCharacter.cs` | Tier 1 |
| `deactivate_character/` | `DeactivateCharacter.cs` | Tier 1 |
| `activate_crowd_as_gang_with_gang_leader/` | `ActivateCrowdAsGangWithGangLeader.cs` | Tier 1 |
| `deactivate_gang/` | `DeactivateGang.cs` | Tier 1 |

Sub-epic helper: `tests/domain/roster/RosterHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/desktop_overlay/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `select_character_on_desktop_via_mouse_click/` | `SelectCharacterOnDesktopViaMouseClick.cs` | Tier 1 |
| `multi_select_characters/` | `MultiSelectCharacters.cs` | Tier 1 |
| `drag_character_to_new_position_on_desktop/` | `DragCharacterToNewPositionOnDesktop.cs` | Tier 1 |
| `double_click_character_to_activate/` | `DoubleClickCharacterToActivate.cs` | Tier 1 |
| `sync_roster_selection_with_game_target/` | `SyncRosterSelectionWithGameTarget.cs` | Tier 1 (uses `FakeMemoryInstance`) |
| `track_spawned_state_per_character/` | `TrackSpawnedStatePerCharacter.cs` | Tier 1 |

Sub-epic helper: `tests/domain/desktop_overlay/DesktopOverlayHelper.cs`

**Gate:** All 15 story files compile and `dotnet test` passes for both sub-epics.

**Production extraction (only after gate):**

- `src/Roster/Roster.cs` — aggregate owning roster entries
- `src/Roster/RosterEntry.cs` — character reference + spawned state
- `src/Roster/ActiveCharacter.cs` — active turn tracking
- `src/Roster/GangMode.cs` — gang activation state + leader reference
- Replace EventAggregator roster sync with domain observable subscriptions
- Slim `RosterExplorerViewModel` from 3,701 LOC toward ≤ 300 LOC (one-liner command handlers)

**Traceability:**
- `roster` → `tests/domain/roster/` → `tests/e2e/roster/`
- `desktop_overlay` → `tests/domain/desktop_overlay/` → `tests/e2e/desktop_overlay/`

---

### Refactoring Slice R5: Increment 6 — Crowd Orchestration and Combat

**Outcome:** Tier 1 + Tier 2 domain tests for crowd move, attack configuration, combat execution, and HCS integration are GREEN; `CombatExecution`, `CrowdMove`, and `HCSIntegrator` extracted; `RosterExplorerViewModel` at or below 300 LOC; `HCSIntegrator.cs` (1,542 LOC) split.

**SBE source:** `docs/increment-6/specification-by-example-increment-6.md`

**Slicing notes:** This is the highest-dependency slice — requires R1–R4 domain types to be in place. Combat domain tests use `NoOpGameCommandExecutor` for attack commands and `FakeMemoryInstance` for knockback position writes. HCS file-watcher stories are exercised via a fake file-watcher seam (no live HCS process). Combat geometry stories (line-of-sight, collision) require the COH collision DLL seam — those are covered at Tier 3 E2E only; domain tests stub the geometry boundary.

**Domain tests (Tier 1 + Tier 2) — `tests/domain/crowd_move/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `move_crowd_with_relative_positioning/` | `MoveCrowdWithRelativePositioning.cs` | Tier 1 |
| `move_crowd_with_optimal_spread_positioning/` | `MoveCrowdWithOptimalSpreadPositioning.cs` | Tier 1 |
| `maintain_group_formation_during_crowd_move/` | `MaintainGroupFormationDuringCrowdMove.cs` | Tier 1 |
| `turn_characters_to_face_destination/` | `TurnCharactersToFaceDestination.cs` | Tier 1 |
| `align_character_facing_with_gang_leader/` | `AlignCharacterFacingWithGangLeader.cs` | Tier 1 |

Sub-epic helper: `tests/domain/crowd_move/CrowdMoveHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/attack_configuration/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `select_attacking_character/` | `SelectAttackingCharacter.cs` | Tier 1 |
| `activate_attack_ability/` | `ActivateAttackAbility.cs` | Tier 1 |
| `select_defender_targets/` | `SelectDefenderTargets.cs` | Tier 1 |
| `confirm_attack_targets/` | `ConfirmAttackTargets.cs` | Tier 1 |
| `configure_attack_for_attacker_defender_pair/` | `ConfigureAttackForAttackerDefenderPair.cs` | Tier 1 |
| `set_attack_effect/` | `SetAttackEffect.cs` | Tier 1 |
| `set_knockback_distance/` | `SetKnockbackDistance.cs` | Tier 1 |
| `set_attack_result/` | `SetAttackResult.cs` | Tier 1 |
| `set_attack_mode/` | `SetAttackMode.cs` | Tier 1 |
| `designate_center_target_for_area_attack/` | `DesignateCenterTargetForAreaAttack.cs` | Tier 1 |
| `execute_ranged_area_attack/` | `ExecuteRangedAreaAttack.cs` | Tier 1 (uses `NoOpGameCommandExecutor`) |
| `execute_sweep_attack_across_multiple_targets/` | `ExecuteSweepAttackAcrossMultipleTargets.cs` | Tier 1 |
| `assign_auto_fire_shots_per_target/` | `AssignAutoFireShotsPerTarget.cs` | Tier 1 |
| `spread_attack_across_crowd/` | `SpreadAttackAcrossCrowd.cs` | Tier 1 |

Sub-epic helper: `tests/domain/attack_configuration/AttackConfigurationHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/combat_execution/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `play_attack_animation_on_attacker/` | `PlayAttackAnimationOnAttacker.cs` | Tier 1 |
| `play_on_hit_animation_on_defender/` | `PlayOnHitAnimationOnDefender.cs` | Tier 1 |
| `apply_knockback_movement_to_defender/` | `ApplyKnockbackMovementToDefender.cs` | Tier 1 |
| `apply_status_effect_to_defender/` | `ApplyStatusEffectToDefender.cs` | Tier 1 |
| `update_character_attack_state_indicators/` | `UpdateCharacterAttackStateIndicators.cs` | Tier 1 |
| `cancel_active_attack/` | `CancelActiveAttack.cs` | Tier 1 |
| `abort_attack_in_progress/` | `AbortAttackInProgress.cs` | Tier 1 |
| `reset_character_combat_state/` | `ResetCharacterCombatState.cs` | Tier 1 |
| `disable_non_attack_abilities_during_combat/` | `DisableNonAttackAbilitiesDuringCombat.cs` | Tier 1 |
| `track_attacker_and_defender_roles_per_character/` | `TrackAttackerAndDefenderRolesPerCharacter.cs` | Tier 1 |

Sub-epic helper: `tests/domain/combat_execution/CombatExecutionHelper.cs`

**Domain tests (Tier 1 + Tier 2) — `tests/domain/hcs_integration/`:**

| Story folder | Test class file | Tier |
|---|---|---|
| `start_hcs_file_watcher_integration/` | `StartHcsFileWatcherIntegration.cs` | Tier 1 (fake file watcher) |
| `read_on_deck_combatants_from_info_file/` | `ReadOnDeckCombatantsFromInfoFile.cs` | Tier 1 |
| `read_eligible_combatants_from_info_file/` | `ReadEligibleCombatantsFromInfoFile.cs` | Tier 1 |
| `read_active_character_from_info_file/` | `ReadActiveCharacterFromInfoFile.cs` | Tier 1 |
| `read_chronometer_turn_state_from_info_file/` | `ReadChronometerTurnStateFromInfoFile.cs` | Tier 1 |
| `process_attack_result_events_from_hcs/` | `ProcessAttackResultEventsFromHcs.cs` | Tier 1 |
| `process_simple_ability_events_from_hcs/` | `ProcessSimpleAbilityEventsFromHcs.cs` | Tier 1 |
| `resolve_held_character_state_from_hcs/` | `ResolveHeldCharacterStateFromHcs.cs` | Tier 1 |
| `execute_sweep_results_from_hcs/` | `ExecuteSweepResultsFromHcs.cs` | Tier 1 |
| `stop_hcs_integration/` | `StopHcsIntegration.cs` | Tier 1 |

Sub-epic helper: `tests/domain/hcs_integration/HcsIntegrationHelper.cs`

**Gate:** All 39 story files compile and `dotnet test` passes for all four sub-epics.

**Production extraction (only after gate):**

- `src/CrowdMove/CrowdMove.cs` — formation and positioning logic (extracted from `RosterExplorerViewModel`)
- `src/Combat/CombatExecution.cs` — attack resolution, status effects, knockback
- `src/Combat/AttackConfiguration.cs` — attack target and mode setup
- Split `HCSIntegrator.cs` (1,542 LOC):
  - `HcsFileWatcher.cs` — file watch and parse
  - `HcsCombatEventProcessor.cs` — event dispatch
- `RosterExplorerViewModel` ≤ 300 LOC (from 3,701 — final reduction)

**Traceability:**
- `crowd_move` → `tests/domain/crowd_move/` → `tests/e2e/crowd_move/`
- `attack_configuration` → `tests/domain/attack_configuration/` → `tests/e2e/attack_configuration/`
- `combat_execution` → `tests/domain/combat_execution/` → `tests/e2e/combat_execution/`
- `hcs_integration` → `tests/domain/hcs_integration/` → `tests/e2e/hcs_integration/`

---

## Slice summary

| Slice | SBE source | Domain tests (Tier 1+2) | Sub-epics | Production target |
|-------|-----------|------------------------|-----------|-------------------|
| **R0** | N/A | None — scaffold only | All (cross-cutting) | — (infra only) |
| **R1** | Inc 1 | 5 story files | `manage_crowd_repository` | `CrowdTree`, `CrowdRepository` |
| **R2** | Inc 2 + 3 | 12 story files | `identity_management`, `animated_ability_management` | `OptionGroup` pattern, typed subclasses |
| **R3** | Inc 4 | 16 story files | `character_movement_authoring`, `movement_execution` | `Movement.cs` split |
| **R4** | Inc 5 | 15 story files | `roster`, `desktop_overlay` | `Roster`, `ActiveCharacter`, `GangMode` |
| **R5** | Inc 6 | 39 story files | `crowd_move`, `attack_configuration`, `combat_execution`, `hcs_integration` | `CombatExecution`, `CrowdMove`, `HCSIntegrator` split |

**Total domain test files to produce (R1–R5):** 87 story files across 10 sub-epics.

---

## What is NOT in this refactoring track

The following sub-epics are **E2E only** (require live COH process; no domain-layer extraction):

- `game_bridge_initialization` — native DLL injection, `InitGame`
- `keybind_execution` — write and load `.keybind` files into running game
- `costume_file_management` — write costume files to COH data directory
- `costume_variant_generation` — generate ghost/FX costume variants
- `ghost_shadows` — superimpose ghost on model via game
- `model_browser` — parse `Models.txt` from game directory
- `identity_rendering` — spawn NPC + load costume via game
- `animation_element_authoring` — play animation sequences via game bridge
- `ability_execution` — play animated ability on spawned character
- `resource_catalog_loading` — load FX/movement/sound catalogs from game data
- `keyboard_hook` — low-level keyboard hook routing
- `camera_rig` — camera detach/attach via game commands
- `memory_interface` — read/write memory offsets (integration layer only)
- `game_state_query` — hover NPC / mouse XYZ / game done state
- `context_menu` — pop-up menu via game
- `pop_up_menu` — write `.mnu` files to COH menus directory
- `combat_geometry` — line-of-sight and collision via HookCostume DLL

These stories are already covered in `tests/e2e/` and do **not** need Tier 1 domain extraction — their behavior lives at the COH integration boundary, not in the domain layer.
