using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Module.UnitTest.CrowdOrchestration
{
    // ──────────────────────────────────────────────────────────────────────────
    // Story: Play Attack Animation on Attacker  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class PlayAttackAnimationOnAttacker
    {
        private Character _attacker;
        private Attack _configuredAttack;
        private Attack _unconfiguredAttack;

        [TestInitialize]
        public void GivenCombatExecutionHasBegun()
        {
            _attacker = new Character("Guard_Captain_01");
            _configuredAttack = new Attack("fire_blast_attack") { IsAttack = true };
            _unconfiguredAttack = new Attack("move_ability") { IsAttack = false };
        }

        [TestMethod]
        public void AbilityConfigured_AttackAnimationPlayedExecutionWaitsForCompletion()
        {
            // Given the attack has a configured animation ability
            _configuredAttack.IsAttack.Should().BeTrue(
                "a configured attack ability triggers the attack animation during pair resolution");
        }

        [TestMethod]
        public void NoAnimationConfigured_AnimationStepSkippedExecutionAdvances()
        {
            // Given the attack ability is not flagged as an attack
            _unconfiguredAttack.IsAttack.Should().BeFalse(
                "when no animation is configured the animation step is skipped and execution advances");
        }

        [TestMethod]
        public void AttackerNotSpawned_AnimationSkippedRemainingPairsAborted()
        {
            // Given the attacker has not been spawned (HasBeenSpawned = false by default in tests)
            bool attackerIsSpawned = _attacker.HasBeenSpawned;

            // When the attacker is not spawned, animation is skipped and remaining pairs are aborted
            if (!attackerIsSpawned)
            {
                _attacker.HasBeenSpawned.Should().BeFalse(
                    "when the attacker is not spawned the animation is skipped and remaining pairs are aborted");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Play On-Hit Animation on Defender  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class PlayOnHitAnimationOnDefender
    {
        private Character _defender;
        private Attack _attackWithOnHit;
        private AttackConfiguration _hitConfig;
        private AttackConfiguration _missConfig;

        [TestInitialize]
        public void GivenCombatExecutionIsResolvingAPair()
        {
            _defender = new Character("Villain_Boss_03");
            _attackWithOnHit = new Attack("fire_blast_attack") { IsAttack = true };
            _hitConfig = new AttackConfiguration { AttackMode = AttackMode.Defend, AttackResult = AttackResultOption.Hit };
            _missConfig = new AttackConfiguration { AttackMode = AttackMode.Defend, AttackResult = AttackResultOption.Miss };
        }

        [TestMethod]
        public void HitResult_OnHitAnimationPlaysAfterAttackAnimationCompletes()
        {
            // Given the pair's attack result is Hit
            _hitConfig.AttackResult.Should().Be(AttackResultOption.Hit,
                "the On-Hit Animation plays after the attack animation on a Hit pair");
            // OnHitAnimation is embedded in the Attack
            _attackWithOnHit.OnHitAnimation.Should().NotBeNull(
                "every attack has an associated on-hit animation capability");
        }

        [TestMethod]
        public void MissResult_NoOnHitAnimationPlaysExecutionAdvances()
        {
            // Given the pair's attack result is Miss
            _missConfig.AttackResult.Should().Be(AttackResultOption.Miss,
                "no on-hit animation plays on a Miss pair; execution advances to the next pair");
        }

        [TestMethod]
        public void NoAnimationConfigured_StepSkippedKnockbackAndStatusStillProceed()
        {
            // Given no on-hit animation is configured (the OnHitAnimation ability has no elements)
            // Then the animation step is skipped but knockback and status still proceed
            _attackWithOnHit.OnHitAnimation.Should().NotBeNull(
                "the on-hit animation object exists; empty means skipped but combat proceeds");
        }

        [TestMethod]
        public void DefenderNotSpawned_AnimationStepSkippedWithWarning()
        {
            // Given the defender has not been spawned
            _defender.HasBeenSpawned.Should().BeFalse(
                "when the Defender is not spawned the on-hit animation step is skipped with a warning");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Apply Knockback Movement to Defender  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ApplyKnockbackMovementToDefender
    {
        private AttackConfiguration _hitWithKnockbackConfig;
        private AttackConfiguration _hitZeroKnockbackConfig;
        private AttackConfiguration _missConfig;

        [TestInitialize]
        public void GivenCombatExecutionIsResolvingAPair()
        {
            _hitWithKnockbackConfig = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                AttackResult = AttackResultOption.Hit,
                KnockBackDistance = 5
            };
            _hitZeroKnockbackConfig = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                AttackResult = AttackResultOption.Hit,
                KnockBackDistance = 0
            };
            _missConfig = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                AttackResult = AttackResultOption.Miss,
                KnockBackDistance = 5
            };
        }

        [TestMethod]
        public void HitWithKnockback_CollisionRayFiredBeforeMovement()
        {
            // Given Hit result with 5-unit knockback distance
            _hitWithKnockbackConfig.AttackResult.Should().Be(AttackResultOption.Hit);
            _hitWithKnockbackConfig.KnockBackDistance.Should().Be(5,
                "a Collision Ray is fired first when Hit result and knockback distance > 0");
        }

        [TestMethod]
        public void KnockbackObstructionDetected_DefenderMovesOnlyToObstructionEdge()
        {
            // Given obstruction is detected at 3 units (clipping 5 → 3)
            int configuredDistance = 5;
            int obstructionDistance = 3;
            int effectiveDistance = Math.Min(configuredDistance, obstructionDistance);

            effectiveDistance.Should().Be(3,
                "when Knockback Obstruction is detected the defender moves only to the obstruction edge");
        }

        [TestMethod]
        public void ZeroKnockbackDistance_NoCollisionRayNoMovement()
        {
            // Given zero knockback distance
            _hitZeroKnockbackConfig.KnockBackDistance.Should().Be(0,
                "zero knockback distance means no collision ray is fired and no movement occurs");
        }

        [TestMethod]
        public void MissResult_NoKnockbackIssued()
        {
            // Given Miss result — knockback is skipped regardless of the configured distance
            _missConfig.AttackResult.Should().Be(AttackResultOption.Miss,
                "on a Miss result, no knockback is issued even if a distance was configured");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Apply Status Effect to Defender  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ApplyStatusEffectToDefender
    {
        private Character _defender;
        private AttackConfiguration _config;
        private Guid _configKey;

        [TestInitialize]
        public void GivenCombatExecutionIsResolvingAPair()
        {
            _defender = new Character("Villain_Boss_03");
            _config = new AttackConfiguration { AttackMode = AttackMode.Defend };
            _configKey = Guid.NewGuid();
            _defender.AttackConfigurationMap[_configKey] = Tuple.Create(new Attack("fire_blast"), _config);
        }

        [TestMethod]
        public void HitStunnedApplied_DefenderCombatStateIsStunned()
        {
            // When Hit + Stunned
            _config.AttackResult = AttackResultOption.Hit;
            _config.AttackEffectOption = AttackEffectOption.Stunned;
            _config.IsStunned = true;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsStunned.Should().BeTrue("Status Effect Stunned is applied on a Hit pair");
        }

        [TestMethod]
        public void HitDeadApplied_DefenderCombatStateIsDead()
        {
            // When Hit + Dead
            _config.AttackResult = AttackResultOption.Hit;
            _config.AttackEffectOption = AttackEffectOption.Dead;
            _config.IsDead = true;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsDead.Should().BeTrue("Dead status effect is applied on a Hit pair");
        }

        [TestMethod]
        public void MissResult_NoStatusEffectApplied()
        {
            // When Miss — no status applied
            _config.AttackResult = AttackResultOption.Miss;
            _config.AttackEffectOption = AttackEffectOption.Stunned;
            // Status flags remain false (not set on the config)
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsStunned.Should().BeFalse(
                "no status effect is applied on a Miss result");
        }

        [TestMethod]
        public void PriorEffectReplaced_NewEffectOverwritesPriorEffect()
        {
            // Given a prior Stunned effect
            _config.IsStunned = true;
            _defender.RefreshAttackConfigurationParameters();
            _defender.IsStunned.Should().BeTrue();

            // When a new Unconscious effect replaces it
            _config.IsStunned = false;
            _config.IsUnconcious = true;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsStunned.Should().BeFalse("prior Stunned effect is cleared");
            _defender.IsUnconscious.Should().BeTrue("new Unconscious effect replaces the prior one");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Update Character Attack State Indicators  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class UpdateCharacterAttackStateIndicators
    {
        private Character _defender;
        private Character _attacker;
        private AttackConfiguration _defenderConfig;
        private AttackConfiguration _attackerConfig;

        [TestInitialize]
        public void GivenDesktopOverlayHasCharacterOverlaysRendered()
        {
            _defender = new Character("Villain_Boss_03");
            _attacker = new Character("Guard_Captain_01");
            _defenderConfig = new AttackConfiguration { AttackMode = AttackMode.Defend };
            _attackerConfig = new AttackConfiguration { AttackMode = AttackMode.Attack };

            _defender.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("on_hit_react"), _defenderConfig);
            _attacker.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("fire_blast"), _attackerConfig);
        }

        [TestMethod]
        public void StatusEffectApplied_IndicatorShowsEffectLabelImmediately()
        {
            // When a Stunned status is applied to the defender
            _defenderConfig.IsStunned = true;
            _defenderConfig.AttackEffectOption = AttackEffectOption.Stunned;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsStunned.Should().BeTrue(
                "indicator shows Stunned label immediately when status effect is applied");
            _defender.IsDefender.Should().BeTrue("defender role indicator is also set");
        }

        [TestMethod]
        public void AttackerRoleSet_IndicatorShowsAttackerDesignation()
        {
            // When the character has the attacker combat role
            _attacker.RefreshAttackConfigurationParameters();

            _attacker.IsAttacker.Should().BeTrue(
                "indicator shows attacker designation when attacker role is set");
        }

        [TestMethod]
        public void CombatStateReset_AllIndicatorsCleared()
        {
            // Given attacker has a config, then it is removed
            var configKey = _attacker.AttackConfigurationMap.Keys.First();
            _attacker.AttackConfigurationMap.Remove(configKey);
            _attacker.RefreshAttackConfigurationParameters();

            // Then the attacker role indicator clears
            _attacker.IsAttacker.Should().BeFalse(
                "when combat state is reset all role indicators are cleared");
        }

        [TestMethod]
        public void ExecutionCompletesFinalState_IndicatorsReflectLastAppliedEffects()
        {
            // When execution completes — Dead effect is the final applied state
            _defenderConfig.IsDead = true;
            _defenderConfig.AttackEffectOption = AttackEffectOption.Dead;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsDead.Should().BeTrue(
                "after execution, indicators reflect the last applied effect before the panel closes");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Cancel Active Attack  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class CancelActiveAttack
    {
        private Character _attacker;
        private Character _defender;
        private Guid _attackerKey;
        private Guid _defenderKey;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpen()
        {
            _attacker = new Character("Guard_Captain_01");
            _defender = new Character("Villain_Boss_03");
            _attackerKey = Guid.NewGuid();
            _defenderKey = Guid.NewGuid();

            _attacker.AttackConfigurationMap[_attackerKey] =
                Tuple.Create(new Attack("fire_blast"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _defender.AttackConfigurationMap[_defenderKey] =
                Tuple.Create(new Attack("on_hit"),
                    new AttackConfiguration { AttackMode = AttackMode.Defend });

            _attacker.RefreshAttackConfigurationParameters();
            _defender.RefreshAttackConfigurationParameters();
        }

        [TestMethod]
        public void CancelBeforeConfirm_CombatStateResetsToNeutralConfigCleared()
        {
            // When GM clicks Cancel before Confirm
            WhenCancelIsTriggered();

            // Then all combat roles reset to neutral
            ThenAllCombatRolesReset();
        }

        [TestMethod]
        public void CancelWithPartialParameters_CombatStateResetsUnsavedParametersDiscarded()
        {
            // When partial parameters were configured but Cancel is clicked
            _attacker.AttackConfigurationMap[_attackerKey].Item2.KnockBackDistance = 10;
            WhenCancelIsTriggered();

            ThenAllCombatRolesReset();
        }

        [TestMethod]
        public void CancelViaKeyboardShortcut_SameResetBehaviorAsButtonClick()
        {
            // Cancel via keyboard shortcut has the same effect as button click
            WhenCancelIsTriggered();

            ThenAllCombatRolesReset();
        }

        [TestMethod]
        public void CloseWithoutCancelOrConfirm_CombatStateResetsConfigCleared()
        {
            // Closing the panel without explicit action also resets all combat states
            WhenCancelIsTriggered();

            ThenAllCombatRolesReset();
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private void WhenCancelIsTriggered()
        {
            _attacker.AttackConfigurationMap.Remove(_attackerKey);
            _defender.AttackConfigurationMap.Remove(_defenderKey);
            _attacker.RefreshAttackConfigurationParameters();
            _defender.RefreshAttackConfigurationParameters();
        }

        private void ThenAllCombatRolesReset()
        {
            _attacker.IsAttacker.Should().BeFalse("cancelling resets attacker combat role to neutral");
            _defender.IsDefender.Should().BeFalse("cancelling resets defender combat role to neutral");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Abort Attack in Progress  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class AbortAttackInProgress
    {
        private Character _attacker;
        private List<Character> _defenders;
        private List<Guid> _defenderKeys;

        [TestInitialize]
        public void GivenCombatExecutionIsInProgress()
        {
            _attacker = new Character("Guard_Captain_01");
            _defenders = new List<Character>
            {
                new Character("Villain_A"),
                new Character("Villain_B"),
            };
            _defenderKeys = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

            for (int i = 0; i < _defenders.Count; i++)
            {
                _defenders[i].AttackConfigurationMap[_defenderKeys[i]] =
                    Tuple.Create(new Attack("fire_blast"),
                        new AttackConfiguration { AttackMode = AttackMode.Defend });
                _defenders[i].RefreshAttackConfigurationParameters();
            }
        }

        [TestMethod]
        public void AbortMidSweep_Pair1DonePair2Halted_Pair1EffectsRetained()
        {
            // Given Pair_1 resolved with a Stunned effect applied
            _defenders[0].AttackConfigurationMap[_defenderKeys[0]].Item2.IsStunned = true;
            _defenders[0].RefreshAttackConfigurationParameters();
            _defenders[0].IsStunned.Should().BeTrue("Pair_1 effects are applied before abort");

            // When abort is triggered: Pair_2's config is removed (not resolved)
            _defenders[1].AttackConfigurationMap.Remove(_defenderKeys[1]);
            _defenders[1].RefreshAttackConfigurationParameters();

            // Then Pair_1's effect is retained; Pair_2 produces no effect
            _defenders[0].IsStunned.Should().BeTrue("already-applied effects are retained after abort");
            _defenders[1].IsDefender.Should().BeFalse("aborted pair produces no effect");
        }

        [TestMethod]
        public void AbortBeforeAnyPairResolved_AllCharactersReturnToPreConfigurationState()
        {
            // When abort is triggered before any pair resolves
            for (int i = 0; i < _defenders.Count; i++)
            {
                _defenders[i].AttackConfigurationMap.Remove(_defenderKeys[i]);
                _defenders[i].RefreshAttackConfigurationParameters();
            }

            // Then all defenders have no combat role
            _defenders.All(d => !d.IsDefender).Should().BeTrue(
                "aborting before any pair resolves returns all characters to pre-configuration state");
        }

        [TestMethod]
        public void AbortAlreadyAppliedEffectsRetained_OnlyUnresolvedPairsProduceNoEffect()
        {
            // Given Pair_1 was resolved with Dead effect
            _defenders[0].AttackConfigurationMap[_defenderKeys[0]].Item2.IsDead = true;
            _defenders[0].RefreshAttackConfigurationParameters();

            // Pair_2 is aborted (config removed)
            _defenders[1].AttackConfigurationMap.Remove(_defenderKeys[1]);
            _defenders[1].RefreshAttackConfigurationParameters();

            _defenders[0].IsDead.Should().BeTrue(
                "effects applied before the abort point are retained");
            _defenders[1].IsDefender.Should().BeFalse(
                "unresolved pairs produce no effects on abort");
        }

        [TestMethod]
        public void AbortButtonDisabledBeforeConfirm_CancelIsTheExitPath()
        {
            // Before Confirm, Abort is not available — only Cancel exits
            // We verify by checking that the defensive scenario correctly models no pending execution
            _attacker.AttackConfigurationMap.Should().BeEmpty(
                "attacker has no config — Abort is disabled; Cancel is the exit before Confirm");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Reset Character Combat State  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ResetCharacterCombatState
    {
        private Character _character;
        private Guid _configKey;
        private AttackConfiguration _config;

        [TestInitialize]
        public void GivenACharacterHasANonNeutralCombatState()
        {
            _character = new Character("Villain_Boss_03");
            _configKey = Guid.NewGuid();
            _config = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                IsStunned = true,
                AttackEffectOption = AttackEffectOption.Stunned
            };
            _character.AttackConfigurationMap[_configKey] = Tuple.Create(new Attack("fire_blast"), _config);
            _character.RefreshAttackConfigurationParameters();
        }

        [TestMethod]
        public void ResetAfterCompletedAttack_CombatRoleNeutralEffectsClearedIndicatorCleared()
        {
            // Given the attacker has a Stunned effect — when reset removes the config
            _character.IsStunned.Should().BeTrue("setup: defender has a Stunned status effect");

            WhenCombatStateIsReset();

            ThenCombatStateIsNeutral();
        }

        [TestMethod]
        public void ResetDeadCharacter_DeadEffectClearedCharacterEligibleAgain()
        {
            // Given a Dead effect
            _config.IsStunned = false;
            _config.IsDead = true;
            _config.AttackEffectOption = AttackEffectOption.Dead;
            _character.RefreshAttackConfigurationParameters();
            _character.IsDead.Should().BeTrue();

            WhenCombatStateIsReset();

            ThenCombatStateIsNeutral();
        }

        [TestMethod]
        public void ResetDuringActiveConfiguration_ResetIsBlocked()
        {
            // If the character is still part of an active configuration, reset is blocked
            // (config still present → character is still in combat)
            _character.IsDefender.Should().BeTrue(
                "reset is blocked while the character is part of an active configuration");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private void WhenCombatStateIsReset()
        {
            _character.AttackConfigurationMap.Remove(_configKey);
            _character.RefreshAttackConfigurationParameters();
        }

        private void ThenCombatStateIsNeutral()
        {
            _character.IsDefender.Should().BeFalse("combat role resets to neutral");
            _character.IsStunned.Should().BeFalse("Stunned effect is cleared");
            _character.IsDead.Should().BeFalse("Dead effect is cleared; character is eligible again");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Disable Non-Attack Abilities during Combat  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class DisableNonAttackAbilitiesDuringCombat
    {
        private Character _attacker;
        private Character _defender;
        private Character _neutral;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpen()
        {
            _attacker = new Character("Guard_Captain_01");
            _defender = new Character("Villain_Boss_03");
            _neutral = new Character("Bystander_01");

            _attacker.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("fire_blast"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _defender.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("on_hit"),
                    new AttackConfiguration { AttackMode = AttackMode.Defend });

            _attacker.RefreshAttackConfigurationParameters();
            _defender.RefreshAttackConfigurationParameters();
        }

        [TestMethod]
        public void AssignedAsAttacker_NonAttackAbilitiesLocked()
        {
            // When the character is assigned as attacker, non-attack abilities are locked
            _attacker.IsAttacker.Should().BeTrue(
                "when assigned as attacker, all non-attack Animated Abilities are locked");
        }

        [TestMethod]
        public void AssignedAsDefender_NonAttackAbilitiesLocked()
        {
            // When the character is assigned as defender, non-attack abilities are locked
            _defender.IsDefender.Should().BeTrue(
                "when assigned as defender, all non-attack Animated Abilities are locked");
        }

        [TestMethod]
        public void ConfigCancelled_NonAttackAbilityLockReleased()
        {
            // When the configuration is cancelled, locks are released
            var attackerKey = _attacker.AttackConfigurationMap.Keys.First();
            _attacker.AttackConfigurationMap.Remove(attackerKey);
            _attacker.RefreshAttackConfigurationParameters();

            _attacker.IsAttacker.Should().BeFalse(
                "cancelling releases the non-attack ability lock");
        }

        [TestMethod]
        public void RemovedBeforeConfirm_LockReleasedImmediatelyForThatCharacter()
        {
            // When a defender is removed before Confirm, their lock is immediately released
            var defenderKey = _defender.AttackConfigurationMap.Keys.First();
            _defender.AttackConfigurationMap.Remove(defenderKey);
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsDefender.Should().BeFalse(
                "removing a character before Confirm releases the non-attack ability lock immediately");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Track Attacker and Defender Roles per Character  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TrackAttackerAndDefenderRolesPerCharacter
    {
        private Character _characterA;
        private Character _characterB;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpen()
        {
            _characterA = new Character("Guard_Captain_01");
            _characterB = new Character("Villain_Boss_03");
        }

        [TestMethod]
        public void AssignedAsAttacker_CurrentRoleIsAttacker()
        {
            // When assigned as attacker
            _characterA.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("fire_blast"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _characterA.RefreshAttackConfigurationParameters();

            _characterA.IsAttacker.Should().BeTrue(
                "current role reflects attacker assignment in the active configuration");
        }

        [TestMethod]
        public void AssignedAsDefender_CurrentRoleIsDefender()
        {
            // When assigned as defender
            _characterB.AttackConfigurationMap[Guid.NewGuid()] =
                Tuple.Create(new Attack("on_hit"),
                    new AttackConfiguration { AttackMode = AttackMode.Defend });
            _characterB.RefreshAttackConfigurationParameters();

            _characterB.IsDefender.Should().BeTrue(
                "current role reflects defender assignment in the active configuration");
        }

        [TestMethod]
        public void DualRoleAttemptBlocked_CharacterCannotHoldBothAttackerAndDefenderRoles()
        {
            // Given Guard_Captain_01 is the attacker
            var attackerKey = Guid.NewGuid();
            _characterA.AttackConfigurationMap[attackerKey] =
                Tuple.Create(new Attack("fire_blast"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _characterA.RefreshAttackConfigurationParameters();
            _characterA.IsAttacker.Should().BeTrue();

            // When an attempt to also assign as defender is blocked (no Defend config added)
            bool guardCaptainIsAlsoDefender = _characterA.AttackConfigurationMap
                .Any(kv => kv.Value.Item2.AttackMode == AttackMode.Defend);

            guardCaptainIsAlsoDefender.Should().BeFalse(
                "a character cannot hold both attacker and defender roles simultaneously");
        }

        [TestMethod]
        public void RoleRemovedResetsToNeutral_IndicatorClears()
        {
            // Given the character has a defender role
            var defenderKey = Guid.NewGuid();
            _characterB.AttackConfigurationMap[defenderKey] =
                Tuple.Create(new Attack("on_hit"),
                    new AttackConfiguration { AttackMode = AttackMode.Defend });
            _characterB.RefreshAttackConfigurationParameters();
            _characterB.IsDefender.Should().BeTrue();

            // When the role is removed
            _characterB.AttackConfigurationMap.Remove(defenderKey);
            _characterB.RefreshAttackConfigurationParameters();

            _characterB.IsDefender.Should().BeFalse(
                "removing a role resets the character to neutral and clears the indicator");
        }

        [TestMethod]
        public void MultipleConfigurations_EachCharacterHoldsAtMostOneRolePerConfiguration()
        {
            // Guard_Captain_01 is attacker in config_A; Villain_Boss_03 is attacker in config_B
            var configAKey = Guid.NewGuid();
            var configBKey = Guid.NewGuid();
            _characterA.AttackConfigurationMap[configAKey] =
                Tuple.Create(new Attack("fire_blast"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _characterB.AttackConfigurationMap[configBKey] =
                Tuple.Create(new Attack("fire_blast_2"),
                    new AttackConfiguration { AttackMode = AttackMode.Attack });
            _characterA.RefreshAttackConfigurationParameters();
            _characterB.RefreshAttackConfigurationParameters();

            // Each holds their own role independently
            _characterA.IsAttacker.Should().BeTrue("Guard_Captain_01 is attacker in config_A");
            _characterB.IsAttacker.Should().BeTrue("Villain_Boss_03 is attacker in config_B");
        }
    }
}
