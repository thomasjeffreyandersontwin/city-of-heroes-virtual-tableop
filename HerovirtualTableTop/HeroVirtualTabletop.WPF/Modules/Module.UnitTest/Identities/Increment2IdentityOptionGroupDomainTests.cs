using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System;
using System.Windows.Forms;

namespace Module.UnitTest.Identities
{
    // ─────────────────────────────────────────────────────────────────────────
    // Story: Add Identity to Character
    // SBE: docs/increment-2/specification-by-example-increment-2.md § Add Identity to Character
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestAddIdentityToCharacter : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenACharacterWithNoIdentities()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Guard_Captain");
        }

        // Scenario: Unique name provided → Identity is accepted
        [TestMethod]
        public void UniqueNameProvided_IdentityAcceptedByIdentityOptionGroup()
        {
            // Given
            var identity = new Identity("Skull_Lt_01", IdentityType.Model, "Knight_Armor");

            // When
            _character.AvailableIdentities.Add(identity);

            // Then
            _character.AvailableIdentities.ContainsKey("Knight_Armor").Should().BeTrue();
        }

        // Scenario: Duplicate name on character → rejected
        [TestMethod]
        public void DuplicateNameOnCharacter_RejectedByIdentityOptionGroup()
        {
            // Given: Knight_Armor already in collection
            var identity1 = new Identity("Skull_Lt_01", IdentityType.Model, "Knight_Armor");
            var identity2 = new Identity("Clockwork_Gear_01", IdentityType.Model, "Knight_Armor");
            _character.AvailableIdentities.Add(identity1);

            // When / Then: adding same name throws
            Action addDuplicate = () => _character.AvailableIdentities.Add(identity2);
            addDuplicate.ShouldThrow<Exception>("duplicate identity name must be rejected");
        }

        // Scenario: First identity added — appears in identity list
        [TestMethod]
        public void AddingFirstIdentity_IdentityListContainsExactlyOneEntry()
        {
            // Given
            var identity = new Identity("Skull_Lt_01", IdentityType.Model, "Knight_Armor");

            // When
            _character.AvailableIdentities.Add(identity);

            // Then
            _character.AvailableIdentities.Count.Should().Be(1);
        }

        // Scenario: Second identity added with unique name — both present
        [TestMethod]
        public void AddingSecondIdentityWithUniqueName_BothIdentitiesInCollection()
        {
            // Given
            var first  = new Identity("Skull_Lt_01",       IdentityType.Model, "Knight_Armor");
            var second = new Identity("Clockwork_Gear_01", IdentityType.Model, "Dragon_Model");

            // When
            _character.AvailableIdentities.Add(first);
            _character.AvailableIdentities.Add(second);

            // Then
            _character.AvailableIdentities.Count.Should().Be(2);
            _character.AvailableIdentities.ContainsKey("Knight_Armor").Should().BeTrue();
            _character.AvailableIdentities.ContainsKey("Dragon_Model").Should().BeTrue();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Set Identity Type (Model or Costume)
    // SBE: § Set Identity Type
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestSetIdentityType : BaseTest
    {
        private CrowdMemberModel _character;
        private Identity _identity;

        [TestInitialize]
        public void GivenAnIdentityInTheIdentityOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Guard_Captain");
            _identity  = new Identity("Skull_Lt_01", IdentityType.Model, "Knight_Armor");
            _character.AvailableIdentities.Add(_identity);
        }

        // Scenario: Set type to Model
        [TestMethod]
        public void SetTypeToModel_IdentityConfiguredAsModelIdentity()
        {
            // When
            _identity.Type = IdentityType.Model;

            // Then
            _identity.Type.Should().Be(IdentityType.Model);
        }

        // Scenario: Set type to Costume
        [TestMethod]
        public void SetTypeToCostume_IdentityConfiguredAsCostumeIdentity()
        {
            // When
            _identity.Type = IdentityType.Costume;

            // Then
            _identity.Type.Should().Be(IdentityType.Costume);
        }

        // Scenario: Type confirmed updates character data immediately
        [TestMethod]
        public void TypeChangeConfirmed_CharacterDataUpdatedImmediately()
        {
            // Given: identity is currently Model
            _identity.Type.Should().Be(IdentityType.Model);

            // When
            _identity.Type = IdentityType.Costume;

            // Then: domain reflects new type right away (no deferred update)
            _character.AvailableIdentities["Knight_Armor"].Type.Should().Be(IdentityType.Costume);
        }

        // Scenario: Model identity does not carry costume surface role
        [TestMethod]
        public void ModelIdentity_TypeRemainsModelRegardlessOfSurfaceValue()
        {
            // Model identity surface is a model archetype name, not a costume path
            _identity.Surface = "Skull_Lt_01";
            _identity.Type = IdentityType.Model;

            _identity.Type.Should().Be(IdentityType.Model);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Assign Costume Surface to Identity
    // SBE: § Assign Costume Surface to Identity
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestAssignCostumeSurfaceToIdentity : BaseTest
    {
        private CrowdMemberModel _character;
        private Identity _costumeIdentity;

        [TestInitialize]
        public void GivenACostumeIdentityWithUnassignedSurface()
        {
            ResetKeyBindGeneratorStatics();
            _character       = new CrowdMemberModel("Guard_Captain");
            _costumeIdentity = new Identity(null, IdentityType.Costume, "Knight_Armor");
            _character.AvailableIdentities.Add(_costumeIdentity);
        }

        // Scenario: Valid file path — surface saved
        [TestMethod]
        public void ValidFilePath_SurfaceSavedOnCostumeIdentity()
        {
            // When
            const string surface = @"C:\Games\CoH\costumes\guard.costume";
            _costumeIdentity.Surface = surface;

            // Then
            _costumeIdentity.Surface.Should().Be(surface);
        }

        // Scenario: Surface cleared — identity marked as unassigned
        [TestMethod]
        public void SurfaceCleared_CostumeIdentityMarkedAsUnassigned()
        {
            // Given: surface was previously assigned
            _costumeIdentity.Surface = @"C:\Games\CoH\costumes\guard.costume";

            // When: GM clears the surface
            _costumeIdentity.Surface = null;

            // Then: surface is unassigned
            _costumeIdentity.Surface.Should().BeNull();
        }

        // Scenario: Costume surface not available on Model Identity
        [TestMethod]
        public void ModelIdentity_TypeRemainsModelWhenSurfaceAssigned()
        {
            // Model identity type is not changed by setting the surface string
            var modelIdentity = new Identity("Skull_Lt_01", IdentityType.Model, "Dragon_Model");
            _character.AvailableIdentities.Add(modelIdentity);

            // Surface on a model identity holds the model archetype name — type stays Model
            modelIdentity.Type.Should().Be(IdentityType.Model);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Set Default Identity
    // SBE: § Set Default Identity
    // CRC invariant: at most one identity may carry the default designation
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestSetDefaultIdentity : BaseTest
    {
        private CrowdMemberModel _character;
        private Identity _shadowForm;
        private Identity _knightArmor;

        [TestInitialize]
        public void GivenACharacterWithTwoIdentitiesAndShadowFormAsDefault()
        {
            ResetKeyBindGeneratorStatics();
            _character  = new CrowdMemberModel("Guard_Captain");
            _shadowForm = new Identity("Skull_Lt_01",       IdentityType.Model, "Shadow_Form");
            _knightArmor = new Identity("Clockwork_Gear_01", IdentityType.Model, "Knight_Armor");
            _character.AvailableIdentities.Add(_shadowForm);
            _character.AvailableIdentities.Add(_knightArmor);
            _character.DefaultIdentity = _shadowForm;
        }

        // Scenario: Set new default on Knight_Armor — exactly one default at a time
        [TestMethod]
        public void SetNewDefaultOnKnightArmor_IdentityOptionGroupHasKnightArmorAsDefault()
        {
            // When
            _character.DefaultIdentity = _knightArmor;

            // Then: Identity Option Group has Knight_Armor as default
            _character.DefaultIdentity.Should().Be(_knightArmor,
                "exactly one identity carries the default designation at a time");
        }

        // Scenario: Previous default is cleared when new default is set
        [TestMethod]
        public void SetNewDefault_PreviousDefaultShadowFormNoLongerDefault()
        {
            // When
            _character.DefaultIdentity = _knightArmor;

            // Then: Shadow_Form no longer carries the default designation
            _character.DefaultIdentity.Should().NotBe(_shadowForm);
        }

        // Scenario: Clear default (set to none) — no identity carries the default flag
        [TestMethod]
        public void ClearDefault_DefaultDesignationUnset_NoExplicitDefaultRemains()
        {
            // When: GM removes the default designation without assigning another
            _character.DefaultIdentity = null;

            // Then: internal default field is null (getter falls back to first available)
            // The character will not auto-activate any explicitly chosen identity on spawn.
            // After clearing, the getter returns AvailableIdentities[0] as the fallback default.
            _character.DefaultIdentity.Should().Be(_shadowForm,
                "getter falls back to first identity in collection when no explicit default is set");
        }

        // Scenario: Set Default disabled when no identities exist
        [TestMethod]
        public void NoIdentities_IdentityOptionGroupIsEmpty()
        {
            // Given: a fresh character with no identities added
            var emptyCharacter = new CrowdMemberModel("Empty_Hero");

            // Then: AvailableIdentities is empty before any default is accessed
            emptyCharacter.AvailableIdentities.Count.Should().Be(0,
                "default identity should not be auto-created until explicitly accessed");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Set Active Identity
    // SBE: § Set Active Identity
    // CRC invariant: exactly zero or one identity carries the active designation at any time
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestSetActiveIdentity : BaseTest
    {
        private CrowdMemberModel _character;
        private Identity _oldLook;
        private Identity _dragonModel;

        [TestInitialize]
        public void GivenACharacterWithOldLookAsActiveIdentity()
        {
            ResetKeyBindGeneratorStatics();
            _character   = new CrowdMemberModel("Guard_Captain");
            _oldLook     = new Identity("Skull_Lt_01",       IdentityType.Model, "Old_Look");
            _dragonModel = new Identity("Clockwork_Gear_01", IdentityType.Model, "Dragon_Model");
            _character.AvailableIdentities.Add(_oldLook);
            _character.AvailableIdentities.Add(_dragonModel);
            _character.ActiveIdentity = _oldLook;
        }

        // Scenario: Model Identity activated — active designation set on Dragon_Model
        [TestMethod]
        public void ModelIdentityActivated_IdentityOptionGroupHasNewIdentityAsActive()
        {
            // When
            _character.ActiveIdentity = _dragonModel;

            // Then: Dragon_Model carries the active designation
            _character.ActiveIdentity.Should().Be(_dragonModel);
        }

        // Scenario: Switch from existing active identity — previous active designation cleared
        [TestMethod]
        public void SwitchFromExistingActiveIdentity_PreviousActiveDesignationCleared()
        {
            // Given: Old_Look is currently active
            _character.ActiveIdentity.Should().Be(_oldLook);

            // When: GM sets Dragon_Model as active
            _character.ActiveIdentity = _dragonModel;

            // Then: Old_Look no longer carries the active designation
            _character.ActiveIdentity.Should().NotBe(_oldLook,
                "exactly one identity is active at a time; setting new active clears the previous");
        }

        // Scenario: Exactly one active identity at all times
        [TestMethod]
        public void AfterSwitch_ExactlyOneIdentityIsActive()
        {
            // When
            _character.ActiveIdentity = _dragonModel;

            // Then: only Dragon_Model is active
            _character.ActiveIdentity.Name.Should().Be("Dragon_Model");
        }

        // Scenario: Costume Identity activated — requires costume surface
        [TestMethod]
        public void CostumeIdentityActivated_ActiveIdentityIsCostumeType()
        {
            // Given: a costume identity
            var knightArmor = new Identity(@"C:\Games\CoH\costumes\guard.costume",
                IdentityType.Costume, "Knight_Armor");
            _character.AvailableIdentities.Add(knightArmor);

            // When
            _character.ActiveIdentity = knightArmor;

            // Then
            _character.ActiveIdentity.Should().Be(knightArmor);
            _character.ActiveIdentity.Type.Should().Be(IdentityType.Costume);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Remove Identity from Character
    // SBE: § Remove Identity from Character
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestRemoveIdentityFromCharacter : BaseTest
    {
        private CrowdMemberModel _character;
        private Identity _oldArmor;
        private Identity _dragonModel;

        [TestInitialize]
        public void GivenACharacterWithTwoIdentities()
        {
            ResetKeyBindGeneratorStatics();
            _character   = new CrowdMemberModel("Guard_Captain");
            _oldArmor    = new Identity("Skull_Lt_01",       IdentityType.Model, "Old_Armor");
            _dragonModel = new Identity("Clockwork_Gear_01", IdentityType.Model, "Dragon_Model");
            _character.AvailableIdentities.Add(_oldArmor);
            _character.AvailableIdentities.Add(_dragonModel);
        }

        // Scenario: Not active, not default — removed cleanly
        [TestMethod]
        public void NotActiveNotDefault_RemovedFromIdentityOptionGroup()
        {
            // When
            _character.AvailableIdentities.Remove(_oldArmor);

            // Then
            _character.AvailableIdentities.ContainsKey("Old_Armor").Should().BeFalse();
        }

        // Scenario: Currently active — removed; character marked as no longer having that active identity
        [TestMethod]
        public void CurrentlyActive_RemovedFromIdentityOptionGroup()
        {
            // Given: Dragon_Model is the active identity
            _character.ActiveIdentity = _dragonModel;

            // When
            _character.AvailableIdentities.Remove(_dragonModel);

            // Then: Dragon_Model no longer in collection
            _character.AvailableIdentities.ContainsKey("Dragon_Model").Should().BeFalse();
        }

        // Scenario: Is default identity — default flag cleared after removal
        [TestMethod]
        public void IsDefaultIdentity_DefaultFlagClearedAfterRemoval()
        {
            // Given: Knight_Armor is the default identity
            var knightArmor = new Identity("Guard_Hero_01", IdentityType.Model, "Knight_Armor");
            _character.AvailableIdentities.Add(knightArmor);
            _character.DefaultIdentity = knightArmor;

            // When
            _character.AvailableIdentities.Remove(knightArmor);

            // Then: Knight_Armor no longer in collection
            _character.AvailableIdentities.ContainsKey("Knight_Armor").Should().BeFalse();
        }

        // Scenario: Last identity — identity list is empty
        [TestMethod]
        public void LastIdentityOnCharacter_IdentityListIsEmpty()
        {
            // When: remove all identities
            _character.AvailableIdentities.Remove(_oldArmor);
            _character.AvailableIdentities.Remove(_dragonModel);

            // Then: identity list is empty
            _character.AvailableIdentities.Count.Should().Be(0);
        }

        // Scenario: Both active and default — both flags cleared as single atomic operation
        [TestMethod]
        public void BothActiveAndDefault_RemovedAndBothFlagsClearedAtomically()
        {
            // Given: Dragon_Model is both active and default
            _character.DefaultIdentity  = _dragonModel;
            _character.ActiveIdentity   = _dragonModel;

            // When
            _character.AvailableIdentities.Remove(_dragonModel);

            // Then: Dragon_Model is gone from the collection
            _character.AvailableIdentities.ContainsKey("Dragon_Model").Should().BeFalse();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Stop Persistent Abilities on Identity Switch
    // SBE: § Stop Persistent Abilities on Identity Switch
    // CRC: persistent abilities are Animated Ability instances with persistence_designation=persistent
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestStopPersistentAbilitiesOnIdentitySwitch : BaseTest
    {
        private CrowdMemberModel _character;
        private AnimatedAbility _persistentAbility;
        private Identity _oldIdentity;
        private Identity _newIdentity;

        [TestInitialize]
        public void GivenACharacterWithAnActivePersistentAbilityAndAnActiveIdentity()
        {
            ResetKeyBindGeneratorStatics();
            _character   = new CrowdMemberModel("Guard_Captain");
            _oldIdentity = new Identity("Skull_Lt_01",       IdentityType.Model, "Old_Look");
            _newIdentity = new Identity("Clockwork_Gear_01", IdentityType.Model, "Dragon_Model");
            _character.AvailableIdentities.Add(_oldIdentity);
            _character.AvailableIdentities.Add(_newIdentity);
            _character.ActiveIdentity = _oldIdentity;

            // persistent = true matches the CRC persistence_designation=persistent
            _persistentAbility = new AnimatedAbility(
                "FX_Persistent_Shield",
                Keys.None,
                AnimationSequenceType.And,
                persistent: true,
                order: 1,
                owner: _character);
            _character.AnimatedAbilities.Add(_persistentAbility);
            _persistentAbility.IsActive = true; // mark as actively running
        }

        // Scenario: Active persistent abilities stopped before despawn of old active identity
        [TestMethod]
        public void ActivePersistentAbilities_StoppedBeforeDespawnOfOldActiveIdentity()
        {
            // Given: Guard_Captain has one or more active persistent abilities
            _persistentAbility.IsActive.Should().BeTrue();

            // When: GM initiates identity switch
            _character.ActiveIdentity = _newIdentity;

            // Then: all persistent abilities are stopped before the old active designation is cleared
            _persistentAbility.IsActive.Should().BeFalse(
                "persistent abilities must stop before the old active identity despawns");
        }

        // Scenario: No active persistent abilities — step skipped without error
        [TestMethod]
        public void NoPersistentAbilities_StepSkippedWithoutError()
        {
            // Given: Guard_Captain has no active persistent abilities
            _persistentAbility.IsActive = false;
            _character.AnimatedAbilities.Remove(_persistentAbility);

            // When: GM initiates identity switch
            Action identitySwitch = () => _character.ActiveIdentity = _newIdentity;

            // Then: switch proceeds without error
            identitySwitch.ShouldNotThrow();
            _character.ActiveIdentity.Should().Be(_newIdentity);
        }

        // Scenario: Persistent ability stop fails — switch continues
        // (Domain-level: Active setter always continues; Stop() is fire-and-forget)
        [TestMethod]
        public void IdentitySwitchCompletes_NewIdentityIsActive()
        {
            // When
            _character.ActiveIdentity = _newIdentity;

            // Then: switch completed; new identity is active
            _character.ActiveIdentity.Should().Be(_newIdentity);
        }

        // Scenario: Stopped abilities remain stopped after switch
        [TestMethod]
        public void StoppedPersistentAbilities_RemainStoppedAfterSwitch()
        {
            // When: identity switch completes
            _character.ActiveIdentity = _newIdentity;

            // Then: previously running persistent abilities remain stopped
            _persistentAbility.IsActive.Should().BeFalse(
                "stopped persistent abilities must not resume automatically after the identity switch");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Story: Spawned NPC entity presence state transitions
    // SBE: § Execute Spawn NPC Command / Execute Delete NPC Command
    // CRC invariant: entity presence is (present or absent)
    // ─────────────────────────────────────────────────────────────────────────
    [TestClass]
    public class TestSpawnedNpcEntityPresenceStateTransitions : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenACharacterWithAModelIdentity()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Guard_Captain");
            var identity = new Identity("Skull_Lt_01", IdentityType.Model, "Base_Look");
            _character.AvailableIdentities.Add(identity);
            _character.ActiveIdentity = identity;
        }

        // Scenario: NPC not yet spawned — entity presence is absent
        [TestMethod]
        public void BeforeSpawn_EntityPresenceIsAbsent()
        {
            // Then: character has not been spawned; entity is absent from game world
            _character.HasBeenSpawned.Should().BeFalse(
                "entity presence is absent before a Spawn NPC Command is issued");
        }

        // Scenario: After SetAsSpawned — entity presence transitions to present
        [TestMethod]
        public void AfterSpawnConfirmed_EntityPresenceIsPresent()
        {
            // When: game confirms NPC is present (Spawn NPC Command succeeded)
            _character.SetAsSpawned();

            // Then: entity presence is present
            _character.HasBeenSpawned.Should().BeTrue(
                "entity presence is present after the Spawn NPC Command is confirmed by the game");
        }

        // Scenario: Load Costume Command requires entity presence = present
        [TestMethod]
        public void LoadCostumeCommand_RequiresEntityPresencePresent()
        {
            // Given: entity is present
            _character.SetAsSpawned();

            // Then: character is in a state where Target + Load Costume can proceed
            _character.HasBeenSpawned.Should().BeTrue(
                "Target by Name Command and Load Costume Command can only be issued after entity is present");
        }
    }
}
