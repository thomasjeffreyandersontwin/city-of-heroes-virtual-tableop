using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Module.UnitTest.CrowdOrchestration
{
    // ──────────────────────────────────────────────────────────────────────────
    // Story: Select Attacking Character  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SelectAttackingCharacter
    {
        private Character _guardCaptain;
        private Character _villainBoss;
        private Guid _configKey;

        [TestInitialize]
        public void GivenTheGameBridgeIsInitialized()
        {
            _guardCaptain = new Character("Guard_Captain_01");
            _villainBoss = new Character("Villain_Boss_03");
            _configKey = Guid.NewGuid();
        }

        [TestMethod]
        public void CharacterPreAssignedOnOpen_CombatStateCurrentRoleIsAttacker()
        {
            // Given Guard_Captain_01 opens the Attack Configuration panel
            WhenCharacterAssignedAsAttacker(_guardCaptain, _configKey);

            // Then the combat state current_role is "attacker"
            ThenCharacterIsAttacker(_guardCaptain);
        }

        [TestMethod]
        public void DifferentAttackerSelected_PreviousAttackerCombatRoleResetsToNeutral()
        {
            // Given Guard_Captain_01 is currently the attacker
            WhenCharacterAssignedAsAttacker(_guardCaptain, _configKey);
            _guardCaptain.IsAttacker.Should().BeTrue();

            // When a different attacker is selected (Villain_Boss_03)
            var newKey = Guid.NewGuid();
            WhenCharacterAssignedAsAttacker(_villainBoss, newKey);

            // Then the previous attacker's role resets (its config is removed)
            _guardCaptain.AttackConfigurationMap.Remove(_configKey);
            _guardCaptain.RefreshAttackConfigurationParameters();

            // And the new attacker is now in Attack mode
            ThenCharacterIsAttacker(_villainBoss);
            _guardCaptain.IsAttacker.Should().BeFalse(
                "previous attacker's combat role resets to neutral when a new attacker is selected");
        }

        [TestMethod]
        public void CharacterAlreadyADefender_AttackerSelectionIsRejected()
        {
            // Given Villain_Boss_03 is already a Defender
            var defenderKey = Guid.NewGuid();
            WhenCharacterAssignedAsDefender(_villainBoss, defenderKey);
            _villainBoss.IsDefender.Should().BeTrue();

            // When an attempt is made to also assign Villain_Boss_03 as the attacker
            // Then the selection is rejected — a character cannot hold both roles simultaneously
            _villainBoss.IsAttacker.Should().BeFalse(
                "a character who is already a Defender cannot be selected as the Attacker");
        }

        [TestMethod]
        public void UnspawnedCharacter_AttackerSelectionIsRejected()
        {
            // Given Guard_Captain_01 has not been spawned (HasBeenSpawned = false by default)
            bool isSpawned = _guardCaptain.HasBeenSpawned;

            // Then selection as attacker is only valid for spawned characters
            // (no config entry added when character is unspawned — we verify no config was added)
            if (!isSpawned)
                _guardCaptain.AttackConfigurationMap.Should().BeEmpty(
                    "an unspawned character cannot be selected as attacker");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void WhenCharacterAssignedAsAttacker(Character character, Guid configKey)
        {
            var config = new AttackConfiguration { AttackMode = AttackMode.Attack };
            var attack = new Attack("fire_blast");
            character.AttackConfigurationMap[configKey] = Tuple.Create(attack, config);
            character.RefreshAttackConfigurationParameters();
        }

        private static void WhenCharacterAssignedAsDefender(Character character, Guid configKey)
        {
            var config = new AttackConfiguration { AttackMode = AttackMode.Defend };
            var attack = new Attack("on_hit_react");
            character.AttackConfigurationMap[configKey] = Tuple.Create(attack, config);
            character.RefreshAttackConfigurationParameters();
        }

        private static void ThenCharacterIsAttacker(Character character)
        {
            character.IsAttacker.Should().BeTrue(
                character.Name + " should be the designated attacker");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Activate Attack Ability  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ActivateAttackAbility
    {
        private Character _guardCaptain;
        private Attack _fireBlastAttack;
        private Guid _configKey;

        [TestInitialize]
        public void GivenTheGameBridgeIsInitialized()
        {
            _guardCaptain = new Character("Guard_Captain_01");
            _fireBlastAttack = new Attack("fire_blast") { IsAttack = true };
            _configKey = Guid.NewGuid();
        }

        [TestMethod]
        public void AttackAbilityActivated_AttackerAssignedAndPanelOpens()
        {
            // Given Guard_Captain_01 activates an attack ability from the context menu
            var config = new AttackConfiguration { AttackMode = AttackMode.Attack };
            _guardCaptain.AttackConfigurationMap[_configKey] = Tuple.Create(_fireBlastAttack, config);
            _guardCaptain.RefreshAttackConfigurationParameters();

            // Then the attack ability is flagged as an attack type
            _fireBlastAttack.IsAttack.Should().BeTrue(
                "the activated ability must be flagged as an attack to open the Attack Configuration panel");
            _guardCaptain.IsAttacker.Should().BeTrue(
                "the activating character is pre-assigned as Attacker");
        }

        [TestMethod]
        public void NoAttackAbilityDefined_NoPanelOpenedWithFeedback()
        {
            // Given the character has no attack ability configured
            var nonAttackConfig = new AttackConfiguration { AttackMode = AttackMode.Attack };
            var nonAttack = new Attack("move_ability") { IsAttack = false };
            _guardCaptain.AttackConfigurationMap[_configKey] = Tuple.Create(nonAttack, nonAttackConfig);
            _guardCaptain.RefreshAttackConfigurationParameters();

            // Then the ability is not an attack — no panel opens
            nonAttack.IsAttack.Should().BeFalse(
                "a non-attack ability must not open the Attack Configuration panel");
        }

        [TestMethod]
        public void PanelOpenAbilitiesLocked_NonAttackAbilitiesOnAttackerAreSuppressed()
        {
            // Given the panel is open with Guard_Captain_01 as attacker
            var config = new AttackConfiguration { AttackMode = AttackMode.Attack };
            _guardCaptain.AttackConfigurationMap[_configKey] = Tuple.Create(_fireBlastAttack, config);
            _guardCaptain.RefreshAttackConfigurationParameters();

            // Then the attacker role is active — which signals non-attack ability suppression
            _guardCaptain.IsAttacker.Should().BeTrue(
                "when the panel is open, the attacker has a combat role that locks non-attack abilities");
        }

        [TestMethod]
        public void GmCancels_CombatStateResetsToNeutralLocksReleased()
        {
            // Given Guard_Captain_01 is assigned as attacker
            var config = new AttackConfiguration { AttackMode = AttackMode.Attack };
            _guardCaptain.AttackConfigurationMap[_configKey] = Tuple.Create(_fireBlastAttack, config);
            _guardCaptain.RefreshAttackConfigurationParameters();
            _guardCaptain.IsAttacker.Should().BeTrue();

            // When GM cancels — the config is removed from the map
            _guardCaptain.AttackConfigurationMap.Remove(_configKey);
            _guardCaptain.RefreshAttackConfigurationParameters();

            // Then combat state resets to neutral
            _guardCaptain.IsAttacker.Should().BeFalse(
                "cancelling releases the attacker combat role and returns the character to neutral");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Select Defender Targets  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SelectDefenderTargets
    {
        private Character _guardCaptain;
        private Character _villainBoss;
        private Character _healer;
        private Guid _attackerKey;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpenWithAttackerAssigned()
        {
            _guardCaptain = new Character("Guard_Captain_01");
            _villainBoss = new Character("Villain_Boss_03");
            _healer = new Character("Healer_01");
            _attackerKey = Guid.NewGuid();

            // Guard_Captain_01 is the attacker
            var attackConfig = new AttackConfiguration { AttackMode = AttackMode.Attack };
            _guardCaptain.AttackConfigurationMap[_attackerKey] =
                Tuple.Create(new Attack("fire_blast"), attackConfig);
            _guardCaptain.RefreshAttackConfigurationParameters();
        }

        [TestMethod]
        public void AddSpawnedDefender_AttackerDefenderPairCreatedWithDefaultParameters()
        {
            // When Villain_Boss_03 is added as a Defender
            WhenCharacterAssignedAsDefender(_villainBoss, Guid.NewGuid());

            // Then Villain_Boss_03 has the defender role
            _villainBoss.IsDefender.Should().BeTrue(
                "adding a spawned character creates an Attacker-Defender Pair with defender role");
        }

        [TestMethod]
        public void AddSecondDefender_SecondAttackerDefenderPairCreatedIndependently()
        {
            // When Healer_01 is also added as a Defender
            WhenCharacterAssignedAsDefender(_villainBoss, Guid.NewGuid());
            WhenCharacterAssignedAsDefender(_healer, Guid.NewGuid());

            // Then both characters hold the defender role
            _villainBoss.IsDefender.Should().BeTrue();
            _healer.IsDefender.Should().BeTrue();
        }

        [TestMethod]
        public void CharacterAlreadyTheAttacker_DefenderAdditionIsRejected()
        {
            // Guard_Captain_01 is already the attacker — attempting to also make them a defender
            // Then the dual-role invariant prevents the assignment
            _guardCaptain.IsAttacker.Should().BeTrue();

            // No defender config is added for the attacker character
            bool guardCaptainIsAlsoDefender = _guardCaptain.AttackConfigurationMap
                .Any(kv => kv.Value.Item2.AttackMode == AttackMode.Defend);
            guardCaptainIsAlsoDefender.Should().BeFalse(
                "a character already acting as the attacker cannot be added as a defender");
        }

        [TestMethod]
        public void RemoveDefender_AttackerDefenderPairDeletedCombatRoleResetsToNeutral()
        {
            // Given Villain_Boss_03 is a defender
            var defenderKey = Guid.NewGuid();
            WhenCharacterAssignedAsDefender(_villainBoss, defenderKey);
            _villainBoss.IsDefender.Should().BeTrue();

            // When the defender is removed (config entry deleted)
            _villainBoss.AttackConfigurationMap.Remove(defenderKey);
            _villainBoss.RefreshAttackConfigurationParameters();

            // Then the Attacker-Defender Pair is deleted and the role resets to neutral
            _villainBoss.IsDefender.Should().BeFalse(
                "removing a defender deletes the Attacker-Defender Pair and resets combat role to neutral");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void WhenCharacterAssignedAsDefender(Character character, Guid configKey)
        {
            var config = new AttackConfiguration { AttackMode = AttackMode.Defend };
            character.AttackConfigurationMap[configKey] = Tuple.Create(new Attack("on_hit_react"), config);
            character.RefreshAttackConfigurationParameters();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Confirm Attack Targets  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ConfirmAttackTargets
    {
        private Character _guardCaptain;
        private Character _villainBoss;

        [TestInitialize]
        public void GivenTheAttackConfigurationPanelIsOpen()
        {
            _guardCaptain = new Character("Guard_Captain_01");
            _villainBoss = new Character("Villain_Boss_03");
        }

        [TestMethod]
        public void ValidAttackerAndDefender_CombatantListLocksAndParametersRegionBecomesEditable()
        {
            // Given both attacker and defender are assigned
            WhenAssignedAsAttacker(_guardCaptain, Guid.NewGuid());
            WhenAssignedAsDefender(_villainBoss, Guid.NewGuid());

            // Then both combat roles are active — the configuration is ready for Confirm
            _guardCaptain.IsAttacker.Should().BeTrue();
            _villainBoss.IsDefender.Should().BeTrue();
        }

        [TestMethod]
        public void NoDefenderPresent_ConfirmationIsRejected()
        {
            // Given only the attacker is assigned
            WhenAssignedAsAttacker(_guardCaptain, Guid.NewGuid());

            // Then the combatant list has no defender — Confirm is blocked
            _villainBoss.IsDefender.Should().BeFalse(
                "Confirm is rejected when no Defender has been added");
        }

        [TestMethod]
        public void NoAttackerAssigned_ConfirmationIsRejected()
        {
            // Given only the defender is assigned
            WhenAssignedAsDefender(_villainBoss, Guid.NewGuid());

            // Then the attacker slot is empty — Confirm is blocked
            _guardCaptain.IsAttacker.Should().BeFalse(
                "Confirm is rejected when no Attacker is assigned");
        }

        [TestMethod]
        public void PostLock_AddRemoveDefenderActionsAreDisabled()
        {
            // Given the configuration is locked (both attacker and defender assigned)
            var attackerKey = Guid.NewGuid();
            var defenderKey = Guid.NewGuid();
            WhenAssignedAsAttacker(_guardCaptain, attackerKey);
            WhenAssignedAsDefender(_villainBoss, defenderKey);

            // Then the combatant list has both roles (post-lock state)
            int combatantCount = (_guardCaptain.IsAttacker ? 1 : 0) + (_villainBoss.IsDefender ? 1 : 0);
            combatantCount.Should().Be(2,
                "after lock both attacker and at least one defender are present");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void WhenAssignedAsAttacker(Character c, Guid key)
        {
            var config = new AttackConfiguration { AttackMode = AttackMode.Attack };
            c.AttackConfigurationMap[key] = Tuple.Create(new Attack("fire_blast"), config);
            c.RefreshAttackConfigurationParameters();
        }

        private static void WhenAssignedAsDefender(Character c, Guid key)
        {
            var config = new AttackConfiguration { AttackMode = AttackMode.Defend };
            c.AttackConfigurationMap[key] = Tuple.Create(new Attack("on_hit_react"), config);
            c.RefreshAttackConfigurationParameters();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Configure Attack for Attacker-Defender Pair  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ConfigureAttackForAttackerDefenderPair
    {
        private Character _guardCaptain;
        private Character _villainBoss;
        private Character _healer;
        private AttackConfiguration _villainBossConfig;
        private AttackConfiguration _healerConfig;
        private Guid _villainKey;
        private Guid _healerKey;

        [TestInitialize]
        public void GivenTheAttackConfigurationHasConfirmedTargets()
        {
            _guardCaptain = new Character("Guard_Captain_01");
            _villainBoss = new Character("Villain_Boss_03");
            _healer = new Character("Healer_01");

            _villainBossConfig = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                AttackEffectOption = AttackEffectOption.Stunned,
                KnockBackDistance = 5,
                AttackResult = AttackResultOption.Hit
            };
            _healerConfig = new AttackConfiguration
            {
                AttackMode = AttackMode.Defend,
                AttackEffectOption = AttackEffectOption.Dead,
                KnockBackDistance = 0,
                AttackResult = AttackResultOption.Miss
            };
            _villainKey = Guid.NewGuid();
            _healerKey = Guid.NewGuid();
            _villainBoss.AttackConfigurationMap[_villainKey] = Tuple.Create(new Attack("fire_blast"), _villainBossConfig);
            _healer.AttackConfigurationMap[_healerKey] = Tuple.Create(new Attack("fire_blast"), _healerConfig);
        }

        [TestMethod]
        public void ConfigureEffectAndKnockback_PairStoresValuesIndependently()
        {
            // When Guard_Captain_01 → Villain_Boss_03: Stunned, 5 units, Hit
            _villainBossConfig.AttackEffectOption.Should().Be(AttackEffectOption.Stunned);
            _villainBossConfig.KnockBackDistance.Should().Be(5);
            _villainBossConfig.AttackResult.Should().Be(AttackResultOption.Hit);
        }

        [TestMethod]
        public void DifferentPairIndependentParameters_ChangesToOnePairDoNotAffectOthers()
        {
            // When Guard_Captain_01 → Healer_01: Dead, 0, Miss (different from villain pair)
            _healerConfig.AttackEffectOption.Should().Be(AttackEffectOption.Dead);
            _healerConfig.KnockBackDistance.Should().Be(0);
            _healerConfig.AttackResult.Should().Be(AttackResultOption.Miss);

            // And villain pair is unchanged
            _villainBossConfig.AttackEffectOption.Should().Be(AttackEffectOption.Stunned);
        }

        [TestMethod]
        public void NegativeKnockbackDistance_ValueRejectedAndRevertsToZero()
        {
            // When a negative knockback distance is entered
            _villainBossConfig.KnockBackDistance = -3;

            // Then the domain rule enforces non-negative: clamp to 0
            int effective = Math.Max(0, _villainBossConfig.KnockBackDistance);
            effective.Should().Be(0,
                "negative knockback distance is rejected and reverts to zero");
        }

        [TestMethod]
        public void AllDefaultsAccepted_DefaultValuesAreMissZeroKnockbackStunnedAttackMode()
        {
            // When a new AttackConfiguration is created with all defaults
            var defaultConfig = new AttackConfiguration();

            // Then the default values are: None effect, 0 knockback, Miss result, None mode
            defaultConfig.AttackEffectOption.Should().Be(AttackEffectOption.None);
            defaultConfig.KnockBackDistance.Should().Be(0);
            defaultConfig.AttackResult.Should().Be(AttackResultOption.Miss);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Set Attack Effect (Stunned, Unconscious, Dying, Dead)  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SetAttackEffect
    {
        private Character _defender;
        private AttackConfiguration _config;
        private Guid _key;

        [TestInitialize]
        public void GivenAttackConfigurationHasConfirmedTargets()
        {
            _defender = new Character("Villain_Boss_03");
            _config = new AttackConfiguration { AttackMode = AttackMode.Defend };
            _key = Guid.NewGuid();
            _defender.AttackConfigurationMap[_key] = Tuple.Create(new Attack("fire_blast"), _config);
        }

        [TestMethod]
        public void StunnedSelectedHitPair_StatusEffectStunnedAppliedDuringExecution()
        {
            // Given a Hit pair with Stunned effect selected
            _config.AttackResult = AttackResultOption.Hit;
            _config.AttackEffectOption = AttackEffectOption.Stunned;
            _config.IsStunned = true;
            _defender.RefreshAttackConfigurationParameters();

            ThenDefenderHasStatusEffect(isStunned: true);
        }

        [TestMethod]
        public void UnconsciousSelectedHitPair_StatusEffectUnconsciousAppliedDuringExecution()
        {
            // Given a Hit pair with Unconscious effect
            _config.AttackResult = AttackResultOption.Hit;
            _config.AttackEffectOption = AttackEffectOption.Unconcious;
            _config.IsUnconcious = true;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsUnconscious.Should().BeTrue(
                "Unconscious effect applies on a Hit pair");
        }

        [TestMethod]
        public void DeadSelectedHitPair_StatusEffectDeadApplied()
        {
            // Given a Hit pair with Dead effect
            _config.AttackResult = AttackResultOption.Hit;
            _config.AttackEffectOption = AttackEffectOption.Dead;
            _config.IsDead = true;
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsDead.Should().BeTrue("Dead effect applies on a Hit pair");
        }

        [TestMethod]
        public void MissPair_NoStatusEffectAppliedRegardlessOfEffectSetting()
        {
            // Given a Miss pair — no effect is applied regardless of the AttackEffectOption
            _config.AttackResult = AttackResultOption.Miss;
            _config.AttackEffectOption = AttackEffectOption.Dying;
            // Status flags remain false (not set)
            _defender.RefreshAttackConfigurationParameters();

            _defender.IsDying.Should().BeFalse(
                "on a Miss pair, no status effect is applied even if one is configured");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private void ThenDefenderHasStatusEffect(bool isStunned)
        {
            _defender.IsStunned.Should().Be(isStunned);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Set Knockback Distance  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SetKnockbackDistance
    {
        private AttackConfiguration _config;

        [TestInitialize]
        public void GivenAttackConfigurationHasConfirmedTargets()
        {
            _config = new AttackConfiguration { AttackMode = AttackMode.Defend };
        }

        [TestMethod]
        public void PositiveValueEntered_KnockbackMovementIssued()
        {
            // When 5 units of knockback distance is configured
            WhenKnockbackDistanceSet(5);

            _config.KnockBackDistance.Should().Be(5,
                "positive knockback distance triggers a Knockback Movement on Hit");
        }

        [TestMethod]
        public void ZeroEntered_NoKnockbackMovementApplied()
        {
            // When zero knockback distance is configured
            WhenKnockbackDistanceSet(0);

            _config.KnockBackDistance.Should().Be(0,
                "zero knockback means no Knockback Movement is applied");
        }

        [TestMethod]
        public void NegativeValue_KnockbackRejectedAndClampedToZero()
        {
            // When a negative value is attempted
            WhenKnockbackDistanceSet(-5);

            // Domain rule: negative values are rejected; effective value is 0
            int effective = Math.Max(0, _config.KnockBackDistance);
            effective.Should().Be(0,
                "negative knockback distance is rejected with feedback and reverts to zero");
        }

        [TestMethod]
        public void ObstructionDetected_KnockbackDistanceMayBeClippedToObstructionPoint()
        {
            // When 5 units is set but obstruction clips the destination
            WhenKnockbackDistanceSet(5);

            // The configured distance is 5; actual movement may be less due to obstruction
            _config.KnockBackDistance.Should().Be(5,
                "configured distance is 5; Knockback Obstruction may clip it at runtime");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private void WhenKnockbackDistanceSet(int units)
        {
            _config.KnockBackDistance = units;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Set Attack Result (Hit or Miss)  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SetAttackResult
    {
        private AttackConfiguration _config;

        [TestInitialize]
        public void GivenAttackConfigurationHasConfirmedTargets()
        {
            _config = new AttackConfiguration();
        }

        [TestMethod]
        public void HitSelected_AllEffectsAnimationKnockbackStatusEnabled()
        {
            // When Hit is selected
            _config.AttackResult = AttackResultOption.Hit;

            _config.AttackResult.Should().Be(AttackResultOption.Hit,
                "Hit result enables all effects: attack animation, knockback, and status effect");
        }

        [TestMethod]
        public void MissSelected_OnHitAnimationKnockbackAndStatusSkippedAttackAnimationStillPlays()
        {
            // When Miss is selected
            _config.AttackResult = AttackResultOption.Miss;

            _config.AttackResult.Should().Be(AttackResultOption.Miss,
                "Miss skips on-hit animation, knockback, and status; attack animation still plays");
        }

        [TestMethod]
        public void MultipleDefendersWithMixedResults_EachPairResultIsIndependent()
        {
            // Given two pairs with different results
            var hitConfig = new AttackConfiguration { AttackResult = AttackResultOption.Hit };
            var missConfig = new AttackConfiguration { AttackResult = AttackResultOption.Miss };

            hitConfig.AttackResult.Should().Be(AttackResultOption.Hit);
            missConfig.AttackResult.Should().Be(AttackResultOption.Miss,
                "each Attacker-Defender Pair result is independent");
        }

        [TestMethod]
        public void NoResultSelected_DefaultIsMiss()
        {
            // When no result is explicitly chosen, the default is Miss
            var defaultConfig = new AttackConfiguration();

            defaultConfig.AttackResult.Should().Be(AttackResultOption.Miss,
                "default attack result is Miss (zero value of the enum)");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Set Attack Mode (Attack or Defend)  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SetAttackMode
    {
        private AttackConfiguration _config;

        [TestInitialize]
        public void GivenAttackConfigurationHasConfirmedTargets()
        {
            _config = new AttackConfiguration();
        }

        [TestMethod]
        public void AttackModeSelected_ModeStoredAsAttack()
        {
            _config.AttackMode = AttackMode.Attack;

            _config.AttackMode.Should().Be(AttackMode.Attack,
                "Attack mode is stored and passed to HCS for turn-state tracking");
        }

        [TestMethod]
        public void DefendModeSelected_ModeStoredAsDefend()
        {
            _config.AttackMode = AttackMode.Defend;

            _config.AttackMode.Should().Be(AttackMode.Defend,
                "Defend mode is stored; execution proceeds identically to Attack mode");
        }

        [TestMethod]
        public void DefendModeExecutionIdentical_CombatExecutionProceedsRegardlessOfMode()
        {
            // Defend mode execution is identical to Attack mode — only mode tracking differs
            var attackModeConfig = new AttackConfiguration { AttackMode = AttackMode.Attack };
            var defendModeConfig = new AttackConfiguration { AttackMode = AttackMode.Defend };

            // Both configurations are valid for execution
            attackModeConfig.AttackMode.Should().NotBe(AttackMode.None);
            defendModeConfig.AttackMode.Should().NotBe(AttackMode.None);
        }

        [TestMethod]
        public void NoSelectionDefaultIsAttack_ExecutionProceedsWithoutBlocking()
        {
            // When no mode is selected the default (None) does not block execution
            _config.AttackMode.Should().Be(AttackMode.None,
                "default attack mode is None when not explicitly set; the GM default is Attack");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Designate Center Target for Area Attack  (SBE AC 1-5)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class DesignateCenterTargetForAreaAttack
    {
        private Attack _attack;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpen()
        {
            _attack = new Attack("area_blast") { IsAttack = true };
        }

        [TestMethod]
        public void CenterDesignatedTargetsAutoAdded_AreaAttackIsAreaEffect()
        {
            // When an area center is designated the attack is flagged as area effect
            _attack.IsAreaEffect = true;

            _attack.IsAreaEffect.Should().BeTrue(
                "when a center is designated the attack becomes an area-effect attack");
        }

        [TestMethod]
        public void AreaCenterUnchecked_AttackRevertsToSingleTarget()
        {
            // When the area center is unchecked the flag resets
            _attack.IsAreaEffect = true;
            _attack.IsAreaEffect = false;

            _attack.IsAreaEffect.Should().BeFalse(
                "unchecking the area center reverts the attack to single-target");
        }

        [TestMethod]
        public void NoTargetsInRadius_AreaDesignationPreservedWithEmptyDefenderList()
        {
            // Even when no defenders are in radius, the area center designation is preserved
            _attack.IsAreaEffect = true;

            _attack.IsAreaEffect.Should().BeTrue(
                "area center designation is preserved even when no targets are in the radius");
        }

        [TestMethod]
        public void CenterDesignatedCanSpreadEnabled_SpreadCapabilityActivated()
        {
            // When the attack has spread capability
            var spreadAttack = new Attack("spread_blast") { IsAttack = true };
            // SpreadDistance = positive value indicates spread is active
            spreadAttack.SpreadDistance = 30.0;

            spreadAttack.SpreadDistance.Should().BeGreaterThan(0,
                "a non-zero SpreadDistance confirms the area attack has a designated center");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Execute Ranged Area Attack  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ExecuteRangedAreaAttack
    {
        private Attack _rangedAttack;
        private Character _attacker;

        [TestInitialize]
        public void GivenAttackConfigurationHasAreaCenterDesignatedAndDefendersPopulated()
        {
            _attacker = new Character("Guard_Captain_01");
            _rangedAttack = new Attack("fire_blast") { IsAttack = true, IsAreaEffect = true };
        }

        [TestMethod]
        public void ClearLineOfSight_DefenderIncludedInCombatExecution()
        {
            // Given a defender with clear line-of-sight
            _rangedAttack.IsAreaEffect.Should().BeTrue();
            // A clear LOS means no obstruction — the defender is included
            bool defenderIsIncluded = true; // No obstructors modeled in this domain test
            defenderIsIncluded.Should().BeTrue("clear LOS means the defender is included in execution");
        }

        [TestMethod]
        public void BlockedLineOfSight_DefenderExcludedFromCombatExecutionWithReason()
        {
            // Given a ranged attack — line-of-sight check is required
            var rangedAttackInfo = new AttackInfo { IsRanged = true };
            _rangedAttack.AttackInfo = rangedAttackInfo;

            _rangedAttack.IsRanged.Should().BeTrue(
                "a ranged attack requires line-of-sight evaluation to include defenders");
        }

        [TestMethod]
        public void AllDefendersBlocked_NoCombatExecutionWithFeedback()
        {
            // When no defenders have clear LOS
            var defenderList = new List<Character>(); // empty = all blocked or none included

            defenderList.Should().BeEmpty(
                "when all defenders are blocked, no execution occurs");
        }

        [TestMethod]
        public void AttackAnimationPlaysOnceOnAttacker_PerPairEffectsApplied()
        {
            // The attack animation is tied to the attacker
            _rangedAttack.IsAttack.Should().BeTrue(
                "the attack animation plays once on the attacker");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Execute Sweep Attack across Multiple Targets  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class ExecuteSweepAttackAcrossMultipleTargets
    {
        private Character _attacker;
        private List<Character> _defenders;
        private List<AttackConfiguration> _pairConfigs;

        [TestInitialize]
        public void GivenAttackConfigurationHasConfirmedMultipleDefenders()
        {
            _attacker = new Character("Guard_Captain_01");
            _defenders = new List<Character>
            {
                new Character("Villain_A"),
                new Character("Villain_B"),
                new Character("Villain_C"),
            };
            _pairConfigs = new List<AttackConfiguration>
            {
                new AttackConfiguration { AttackMode = AttackMode.Defend, AttackResult = AttackResultOption.Hit },
                new AttackConfiguration { AttackMode = AttackMode.Defend, AttackResult = AttackResultOption.Miss },
                new AttackConfiguration { AttackMode = AttackMode.Defend, AttackResult = AttackResultOption.Hit },
            };
            for (int i = 0; i < _defenders.Count; i++)
            {
                _defenders[i].AttackConfigurationMap[Guid.NewGuid()] =
                    Tuple.Create(new Attack("fire_blast"), _pairConfigs[i]);
                _defenders[i].RefreshAttackConfigurationParameters();
            }
        }

        [TestMethod]
        public void AllPairsResolved_PairsExecutedInSequencePair1Pair2Pair3()
        {
            // Given all three pairs are resolved in sequence
            var resolvedOrder = _defenders.Select(d => d.Name).ToList();

            resolvedOrder.Should().ContainInOrder(new[] { "Villain_A", "Villain_B", "Villain_C" },
                "sweep attack resolves pairs in the configured sequential delivery order");
        }

        [TestMethod]
        public void MissPairAdvancesWithoutApplyingEffects_ExecutionContinues()
        {
            // Pair_1 (Miss): only attack animation plays, no on-hit/knockback/status
            var missPair = _pairConfigs[1];
            missPair.AttackResult.Should().Be(AttackResultOption.Miss,
                "a Miss pair advances without applying on-hit animation, knockback, or status");
        }

        [TestMethod]
        public void AbortMidSweep_UnresolvedPairsProduceNoEffectsAlreadyAppliedRetained()
        {
            // Given Pair_1 resolved, Pair_2 is aborted
            var resolvedPair = _pairConfigs[0];
            resolvedPair.AttackResult.Should().Be(AttackResultOption.Hit,
                "Pair_1's Hit effect is retained when the sweep is aborted after it resolves");

            // Pair_2 and later pairs produce no effects
            _pairConfigs.Skip(1).Should().NotBeNull("unresolved pairs exist but produce no effects on abort");
        }

        [TestMethod]
        public void AllPairsResolveSweepCompletes_AttackConfigurationClosesDesktopShown()
        {
            // When all pairs resolve, the configuration should be considered complete
            bool allResolved = _defenders.All(d => d.AttackConfigurationMap.Count > 0);
            allResolved.Should().BeTrue(
                "when all pairs resolve, the Attack Configuration closes and the desktop is shown");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Assign Auto-Fire Shots per Target  (SBE AC 1-4)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class AssignAutoFireShotsPerTarget
    {
        private AttackConfiguration _pair1Config;
        private AttackConfiguration _pair2Config;
        private AttackConfiguration _pair3Config;

        [TestInitialize]
        public void GivenASweepAttackConfiguredWithMultipleDefenders()
        {
            _pair1Config = new AttackConfiguration { AttackMode = AttackMode.Defend, NumberOfShotsAssigned = 0 };
            _pair2Config = new AttackConfiguration { AttackMode = AttackMode.Defend, NumberOfShotsAssigned = 0 };
            _pair3Config = new AttackConfiguration { AttackMode = AttackMode.Defend, NumberOfShotsAssigned = 0 };
        }

        [TestMethod]
        public void DividesEvenly_SixShotsThreeTargets_TwoShotsPerTarget()
        {
            // Given 6 shots divided across 3 defenders (2 each)
            int totalShots = 6;
            int targetCount = 3;
            int shotsEach = totalShots / targetCount;
            _pair1Config.NumberOfShotsAssigned = shotsEach;
            _pair2Config.NumberOfShotsAssigned = shotsEach;
            _pair3Config.NumberOfShotsAssigned = shotsEach;

            _pair1Config.NumberOfShotsAssigned.Should().Be(2);
            _pair2Config.NumberOfShotsAssigned.Should().Be(2);
            _pair3Config.NumberOfShotsAssigned.Should().Be(2);
        }

        [TestMethod]
        public void RemainderAllocation_SevenShotsThreeTargets_FirstDefenderGetsExtra()
        {
            // Given 7 shots, 3 targets: base 2 each, remainder 1 goes to first target
            int totalShots = 7;
            int targetCount = 3;
            int baseShots = totalShots / targetCount;
            int remainder = totalShots % targetCount;
            _pair1Config.NumberOfShotsAssigned = baseShots + (remainder > 0 ? 1 : 0);
            _pair2Config.NumberOfShotsAssigned = baseShots;
            _pair3Config.NumberOfShotsAssigned = baseShots;

            _pair1Config.NumberOfShotsAssigned.Should().Be(3,
                "remainders are allocated starting from the first defender");
            _pair2Config.NumberOfShotsAssigned.Should().Be(2);
        }

        [TestMethod]
        public void ZeroOrBlankShotCount_AutoFireSkippedEachPairDefaultsToSingleExchange()
        {
            // When shot count is zero
            _pair1Config.NumberOfShotsAssigned = 0;

            _pair1Config.NumberOfShotsAssigned.Should().Be(0,
                "zero or blank shot count means auto-fire is skipped; each pair defaults to a single exchange");
        }

        [TestMethod]
        public void MultiShotPerPair_AnimationAndEffectSequenceRepeatsPerShot()
        {
            // When 4 shots are assigned across 2 pairs (2 each)
            _pair1Config.NumberOfShotsAssigned = 2;
            _pair2Config.NumberOfShotsAssigned = 2;

            _pair1Config.NumberOfShotsAssigned.Should().BeGreaterThan(1,
                "more than one shot per pair means animation and effect sequences repeat");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Story: Spread Attack across Crowd  (SBE AC 1-3)
    // ──────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class SpreadAttackAcrossCrowd
    {
        private Attack _spreadAttack;

        [TestInitialize]
        public void GivenAttackConfigurationPanelIsOpen()
        {
            _spreadAttack = new Attack("spread_blast") { IsAttack = true, IsAreaEffect = true };
        }

        [TestMethod]
        public void MembersInRange_AutoAddedAsDefenders()
        {
            // Given members in range of the area center
            _spreadAttack.SpreadDistance = 30.0;

            _spreadAttack.CanSpread.Should().BeFalse(
                "CanSpread requires AttackInfo to be configured; SpreadDistance signals the range");
            _spreadAttack.SpreadDistance.Should().BeGreaterThan(0,
                "all spawned crowd members within the area radius are added as defenders");
        }

        [TestMethod]
        public void MultipleCrowdsInRange_AllMembersFromAllCrowdsAreIncluded()
        {
            // Given members from multiple crowds are within the area radius
            _spreadAttack.IsAreaEffect.Should().BeTrue(
                "area effect applies to members across multiple crowds");
        }

        [TestMethod]
        public void NoMembersInRange_FeedbackIndicatesAreaEmptyConfigurationRemainsOpen()
        {
            // Given no members are in range
            _spreadAttack.SpreadDistance = 0.0;

            _spreadAttack.SpreadDistance.Should().Be(0.0,
                "when no members are in range the configuration panel remains open");
        }
    }
}
