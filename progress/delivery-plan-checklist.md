# Refactoring Pipeline Checklist

**Gate:** Tier 1 + Tier 2 domain tests GREEN before any production extraction. See `docs/refactoring-plan.md` and `docs/refactoring-thin-slicing.md`.

---

## Run R0 — Scaffold `tests/domain/` + COH seam fakes

- [x] Engineer: create `HeroVTT.DomainTests.csproj` under `tests/domain/`
- [x] Engineer: create `tests/domain/Support/FakeMemoryInstance.cs`
- [x] Engineer: create `tests/domain/Support/NoOpGameCommandExecutor.cs`
- [x] Engineer: create `tests/domain/Support/GameCommandTestAssemblyHooks.cs`
- [x] Build gate: `dotnet build` on domain test project — zero errors

## Run R1 — Increment 1: Crowd Persistence and Tree

- [x] Engineer: Tier 1+2 tests — `tests/domain/manage_crowd_repository/` (5 story files)
  - [x] `load_active_crowd_files_on_startup/LoadActiveCrowdFilesOnStartup.cs`
  - [x] `browse_and_activate_crowd_files/BrowseAndActivateCrowdFiles.cs`
  - [x] `save_dirty_to_source_files/SaveDirtyToSourceFiles.cs`
  - [x] `save_crowd_to_new_file/SaveCrowdToNewFile.cs`
  - [x] `track_source_file_per_crowd/TrackSourceFilePerCrowd.cs`
- [ ] Reviewer: test alignment check (ATDD structure, orchestrator, domain language)
- [ ] Run tests — `dotnet test` domain project: all GREEN (gate)
- [ ] Engineer: extract `CrowdTree`, `CrowdRepository`, `Crowd`, `CrowdMember` to `src/Crowds/`
- [ ] Engineer: slim `CharacterExplorerViewModel` crowd commands to ≤ 3 lines
- [ ] Run tests — domain + E2E `manage_crowd_repository/` green

## Run R2 — Increments 2 & 3: OptionGroup Pattern + Identity + Animated Ability

- [x] Engineer: Tier 1+2 tests — `tests/domain/identity_management/` (6 story files)
  - [x] `add_identity_to_character/AddIdentityToCharacter.cs`
  - [x] `assign_costume_surface_to_identity/AssignCostumeSurfaceToIdentity.cs`
  - [x] `remove_identity_from_character/RemoveIdentityFromCharacter.cs`
  - [x] `set_active_identity/SetActiveIdentity.cs`
  - [x] `set_default_identity/SetDefaultIdentity.cs`
  - [x] `set_identity_type/SetIdentityType.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/animated_ability_management/` (6 story files)
  - [x] `create_animated_ability/CreateAnimatedAbility.cs`
  - [x] `delete_animated_ability/DeleteAnimatedAbility.cs`
  - [x] `edit_animated_ability/EditAnimatedAbility.cs`
  - [x] `set_ability_activation_key/SetAbilityActivationKey.cs`
  - [x] `set_default_ability_for_character/SetDefaultAbilityForCharacter.cs`
  - [x] `toggle_ability_persistence/ToggleAbilityPersistence.cs`
- [ ] Reviewer: test alignment check (ATDD, selection/active invariants, mock boundaries)
- [ ] Run tests — `dotnet test` domain project: all GREEN (gate)
- [ ] Engineer: implement `OptionGroup` + `IdentityOptionGroup`, `AbilityOptionGroup`, `MovementOptionGroup` in `src/`
- [ ] Engineer: slim `OptionGroupViewModel` ≤ 200 LOC
- [ ] Run tests — domain + E2E `identity_management/` and `animated_ability_management/` green

## Run R3 — Increment 4: Movement

- [x] Engineer: Tier 1+2 tests — `tests/domain/character_movement_authoring/` (6 story files)
  - [x] `add_movement_to_character/AddMovementToCharacter.cs`
  - [x] `edit_movement_parameters/EditMovementParameters.cs`
  - [x] `remove_movement_from_character/RemoveMovementFromCharacter.cs`
  - [x] `set_default_movement/SetDefaultMovement.cs`
  - [x] `set_movement_activation_key/SetMovementActivationKey.cs`
  - [x] `add_default_movements_to_character/AddDefaultMovementsToCharacter.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/movement_execution/` (10 story files)
  - [x] `animate_movement/AnimateMovement.cs`
  - [x] `detect_floor_and_wall_collisions/DetectFloorAndWallCollisions.cs`
  - [x] `enforce_distance_limit_per_movement_type/EnforceDistanceLimitPerMovementType.cs`
  - [x] `execute_move_npc_command/ExecuteMoveNpcCommand.cs`
  - [x] `move_character_to_camera_position/MoveCharacterToCameraPosition.cs`
  - [x] `move_character_to_location/MoveCharacterToLocation.cs`
  - [x] `reset_character_orientation/ResetCharacterOrientation.cs`
  - [x] `teleport_character_to_camera/TeleportCharacterToCamera.cs`
  - [x] `track_movement_distance_count/TrackMovementDistanceCount.cs`
  - [x] `turn_character_towards_target/TurnCharacterTowardsTarget.cs`
- [ ] Reviewer: test alignment check (ATDD, `FakeMemoryInstance` boundary, domain language)
- [ ] Run tests — `dotnet test` domain project: all GREEN (gate)
- [ ] Engineer: split `Movement.cs` → `MovementExecution.cs` + `CharacterMovement.cs`; inject `IMemoryInstance`
- [ ] Engineer: slim `MovementEditorViewModel` from 717 LOC to one-liner commands
- [ ] Run tests — domain + E2E `character_movement_authoring/` and `movement_execution/` green

## Run R4 — Increment 5: Roster and Desktop

- [x] Engineer: Tier 1+2 tests — `tests/domain/roster/` (9 story files)
  - [x] `add_character_to_roster/AddCharacterToRoster.cs`
  - [x] `add_crowd_to_roster/AddCrowdToRoster.cs`
  - [x] `spawn_character_to_desktop_from_roster/SpawnCharacterToDesktopFromRoster.cs`
  - [x] `remove_character_from_roster/RemoveCharacterFromRoster.cs`
  - [x] `clear_character_from_desktop/ClearCharacterFromDesktop.cs`
  - [x] `activate_character/ActivateCharacter.cs`
  - [x] `deactivate_character/DeactivateCharacter.cs`
  - [x] `activate_crowd_as_gang_with_gang_leader/ActivateCrowdAsGangWithGangLeader.cs`
  - [x] `deactivate_gang/DeactivateGang.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/desktop_overlay/` (6 story files)
  - [x] `select_character_on_desktop_via_mouse_click/SelectCharacterOnDesktopViaMouseClick.cs`
  - [x] `multi_select_characters/MultiSelectCharacters.cs`
  - [x] `drag_character_to_new_position_on_desktop/DragCharacterToNewPositionOnDesktop.cs`
  - [x] `double_click_character_to_activate/DoubleClickCharacterToActivate.cs`
  - [x] `sync_roster_selection_with_game_target/SyncRosterSelectionWithGameTarget.cs`
  - [x] `track_spawned_state_per_character/TrackSpawnedStatePerCharacter.cs`
- [ ] Reviewer: test alignment check (ATDD, no EventAggregator in domain, mock boundaries)
- [ ] Run tests — `dotnet test` domain project: all GREEN (gate)
- [ ] Engineer: create `Roster`, `RosterEntry`, `ActiveCharacter`, `GangMode` in `src/Roster/`
- [ ] Engineer: replace EventAggregator roster sync with domain subscriptions
- [ ] Engineer: slim `RosterExplorerViewModel` (from 3,701 LOC — measurable reduction)
- [ ] Run tests — domain + E2E `roster/` and `desktop_overlay/` green

## Run R5 — Increment 6: Combat and Orchestration

- [x] Engineer: Tier 1+2 tests — `tests/domain/crowd_move/` (5 story files)
  - [x] `move_crowd_with_relative_positioning/MoveCrowdWithRelativePositioning.cs`
  - [x] `move_crowd_with_optimal_spread_positioning/MoveCrowdWithOptimalSpreadPositioning.cs`
  - [x] `maintain_group_formation_during_crowd_move/MaintainGroupFormationDuringCrowdMove.cs`
  - [x] `turn_characters_to_face_destination/TurnCharactersToFaceDestination.cs`
  - [x] `align_character_facing_with_gang_leader/AlignCharacterFacingWithGangLeader.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/attack_configuration/` (14 story files)
  - [x] `select_attacking_character/SelectAttackingCharacter.cs`
  - [x] `activate_attack_ability/ActivateAttackAbility.cs`
  - [x] `select_defender_targets/SelectDefenderTargets.cs`
  - [x] `confirm_attack_targets/ConfirmAttackTargets.cs`
  - [x] `configure_attack_for_attacker_defender_pair/ConfigureAttackForAttackerDefenderPair.cs`
  - [x] `set_attack_effect/SetAttackEffect.cs`
  - [x] `set_knockback_distance/SetKnockbackDistance.cs`
  - [x] `set_attack_result/SetAttackResult.cs`
  - [x] `set_attack_mode/SetAttackMode.cs`
  - [x] `designate_center_target_for_area_attack/DesignateCenterTargetForAreaAttack.cs`
  - [x] `execute_ranged_area_attack/ExecuteRangedAreaAttack.cs`
  - [x] `execute_sweep_attack_across_multiple_targets/ExecuteSweepAttackAcrossMultipleTargets.cs`
  - [x] `assign_auto_fire_shots_per_target/AssignAutoFireShotsPerTarget.cs`
  - [x] `spread_attack_across_crowd/SpreadAttackAcrossCrowd.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/combat_execution/` (10 story files)
  - [x] `play_attack_animation_on_attacker/PlayAttackAnimationOnAttacker.cs`
  - [x] `play_on_hit_animation_on_defender/PlayOnHitAnimationOnDefender.cs`
  - [x] `apply_knockback_movement_to_defender/ApplyKnockbackMovementToDefender.cs`
  - [x] `apply_status_effect_to_defender/ApplyStatusEffectToDefender.cs`
  - [x] `update_character_attack_state_indicators/UpdateCharacterAttackStateIndicators.cs`
  - [x] `cancel_active_attack/CancelActiveAttack.cs`
  - [x] `abort_attack_in_progress/AbortAttackInProgress.cs`
  - [x] `reset_character_combat_state/ResetCharacterCombatState.cs`
  - [x] `disable_non_attack_abilities_during_combat/DisableNonAttackAbilitiesDuringCombat.cs`
  - [x] `track_attacker_and_defender_roles_per_character/TrackAttackerAndDefenderRolesPerCharacter.cs`
- [x] Engineer: Tier 1+2 tests — `tests/domain/hcs_integration/` (10 story files)
  - [x] `start_hcs_file_watcher_integration/StartHcsFileWatcherIntegration.cs`
  - [x] `read_on_deck_combatants_from_info_file/ReadOnDeckCombatantsFromInfoFile.cs`
  - [x] `read_eligible_combatants_from_info_file/ReadEligibleCombatantsFromInfoFile.cs`
  - [x] `read_active_character_from_info_file/ReadActiveCharacterFromInfoFile.cs`
  - [x] `read_chronometer_turn_state_from_info_file/ReadChronometerTurnStateFromInfoFile.cs`
  - [x] `process_attack_result_events_from_hcs/ProcessAttackResultEventsFromHcs.cs`
  - [x] `process_simple_ability_events_from_hcs/ProcessSimpleAbilityEventsFromHcs.cs`
  - [x] `resolve_held_character_state_from_hcs/ResolveHeldCharacterStateFromHcs.cs`
  - [x] `execute_sweep_results_from_hcs/ExecuteSweepResultsFromHcs.cs`
  - [x] `stop_hcs_integration/StopHcsIntegration.cs`
- [ ] Reviewer: test alignment check (ATDD, HCS seam boundary, combat domain language)
- [ ] Run tests — `dotnet test` domain project: all GREEN (gate)
- [ ] Engineer: extract `CrowdMove`, `CombatExecution`, `AttackConfiguration` to `src/`
- [ ] Engineer: split `HCSIntegrator.cs` → `HcsFileWatcher.cs` + `HcsCombatEventProcessor.cs`
- [ ] Engineer: `RosterExplorerViewModel` ≤ 300 LOC (from 3,701)
- [ ] Run tests — domain + E2E for all R5 sub-epics green
- [ ] §6.1 baseline scan — verify violation counts at target (Helper.Global → 0, MemoryElement → 0, etc.)
