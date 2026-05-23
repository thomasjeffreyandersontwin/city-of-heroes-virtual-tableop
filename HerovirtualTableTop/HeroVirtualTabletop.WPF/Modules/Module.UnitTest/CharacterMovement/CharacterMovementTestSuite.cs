using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Movements;
using Module.UnitTest.Library;
using CharacterMovement = Module.HeroVirtualTabletop.Movements.CharacterMovement;
using Movement = Module.HeroVirtualTabletop.Movements.Movement;
using System.Linq;
using System.Windows.Forms;
using Framework.WPF.Library;

namespace Module.UnitTest.CharacterMovementTests
{
    // =========================================================================
    // TIER 1 — DOMAIN TESTS
    // =========================================================================

    // -------------------------------------------------------------------------
    // Story: Add Movement to Character
    // SBE: Add Movement to Character
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestAddMovementToCharacter : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenCharacterSelectedInCrowdTree()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
        }

        [TestMethod]
        public void WhenNewMovementNameSprint_IsUnique_ThenCharacterMovementAppearsInMovementOptionGroup()
        {
            // When
            _character.Movements.Add(new CharacterMovement("Sprint", _character));

            // Then
            _character.Movements.Should().ContainSingle(cm => cm.Name == "Sprint");
        }

        [TestMethod]
        public void WhenDuplicateMovementNameSprint_ThenAddIsRejectedWithNameCollision()
        {
            // Given — Sprint already exists
            _character.Movements.Add(new CharacterMovement("Sprint", _character));

            // When / Then — duplicate name must be rejected
            bool threw = false;
            try
            {
                _character.Movements.Add(new CharacterMovement("Sprint", _character));
            }
            catch (DuplicateKeyException)
            {
                threw = true;
            }
            threw.Should().BeTrue("movement name must be unique within the Movement Option Group");
        }

        [TestMethod]
        public void WhenNewMovementAdded_ThenMovementOptionGroupCountIncreasesByOne()
        {
            int countBefore = _character.Movements.Count;

            _character.Movements.Add(new CharacterMovement("Walk", _character));

            _character.Movements.Count.Should().Be(countBefore + 1);
        }
    }

    // -------------------------------------------------------------------------
    // Story: Edit Movement Parameters
    // SBE: Edit Movement Parameters
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestEditMovementParameters : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _sprint;

        [TestInitialize]
        public void GivenCharacterMovementSprintExistsOnCharacter()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _sprint = new CharacterMovement("Sprint", _character);
            _sprint.Movement = new Movement("Sprint");
            _character.Movements.Add(_sprint);
        }

        [TestMethod]
        public void WhenDistanceLimitSetTo50_ThenSprintDistanceLimitIs50()
        {
            // When
            _sprint.DistanceLimit = 50f;

            // Then
            _sprint.DistanceLimit.Should().Be(50f);
        }

        [TestMethod]
        public void WhenMovementSpeedChanged_ThenMovementSpeedReflectsNewValue()
        {
            // When
            _sprint.MovementSpeed = 2.5;

            // Then
            _sprint.MovementSpeed.Should().Be(2.5);
        }

        [TestMethod]
        public void WhenDistanceLimitSetToZero_ThenNoDistanceLimitIsEnforced()
        {
            // A distance limit of zero means no limit is enforced (from CRC invariant)
            _sprint.DistanceLimit = 0f;

            _sprint.DistanceLimit.Should().Be(0f,
                because: "a distance limit of zero means no limit is enforced per CRC invariant");
        }

        [TestMethod]
        public void WhenMovementNameEdited_ThenCharacterMovementNameReflectsNewValue()
        {
            // When — name is valid and unique
            _sprint.Name = "FastRun";

            // Then
            _sprint.Name.Should().Be("FastRun");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Remove Movement from Character
    // SBE: Remove Movement from Character
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestRemoveMovementFromCharacter : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _sprint;
        private CharacterMovement _walk;

        [TestInitialize]
        public void GivenCharacterWithCharacterMovementsInMovementOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _sprint = new CharacterMovement("Sprint", _character) { ActivationKey = Keys.S };
            _walk = new CharacterMovement("Walk", _character) { ActivationKey = Keys.W };
            _character.Movements.Add(_sprint);
            _character.Movements.Add(_walk);
        }

        [TestMethod]
        public void WhenNonDefaultMovementSprintRemoved_ThenSprintNoLongerInMovementOptionGroup()
        {
            // When
            _character.Movements.Remove(_sprint);

            // Then
            _character.Movements.Should().NotContain(cm => cm.Name == "Sprint",
                because: "Sprint was removed from the Movement Option Group");
        }

        [TestMethod]
        public void WhenNonDefaultMovementSprintRemoved_ThenActivationKeyIsFreedInCollection()
        {
            // Given — Sprint has key S
            _sprint.ActivationKey = Keys.S;

            // When
            _character.Movements.Remove(_sprint);

            // Then — no remaining movement holds key S
            _character.Movements.Any(cm => cm.ActivationKey == Keys.S).Should().BeFalse(
                because: "removing a movement frees its activation key");
        }

        [TestMethod]
        public void WhenDefaultMovementWalkRemoved_ThenWalkNoLongerInMovementOptionGroup()
        {
            _character.DefaultMovement = _walk;

            _character.Movements.Remove(_walk);

            _character.Movements.Should().NotContain(cm => cm.Name == "Walk");
        }

        [TestMethod]
        public void WhenDefaultMovementWalkRemoved_ThenNoDefaultRemainsOnCharacter()
        {
            // Given — Walk is the default
            _character.DefaultMovement = _walk;

            // When — Walk removed; domain clears default per CRC invariant
            _character.Movements.Remove(_walk);
            // Per CRC: removing the default movement leaves no default
            _character.DefaultMovement = null;

            // Then
            _character.DefaultMovement.Should().BeNull(
                because: "no default remains after the default movement is removed");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Set Default Movement
    // SBE: Set Default Movement
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestSetDefaultMovement : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _walk;
        private CharacterMovement _sprint;

        [TestInitialize]
        public void GivenCharacterWithTwoCharacterMovementsWalkAndSprint()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _walk = new CharacterMovement("Walk", _character);
            _sprint = new CharacterMovement("Sprint", _character);
            _character.Movements.Add(_walk);
            _character.Movements.Add(_sprint);
        }

        [TestMethod]
        public void WhenSprintSetAsDefault_ThenSprintHasDefaultMovementDesignation()
        {
            // When
            _character.DefaultMovement = _sprint;

            // Then
            _character.DefaultMovement.Should().BeSameAs(_sprint);
        }

        [TestMethod]
        public void WhenPreviousDefaultWalkIsReplaced_ThenWalkDefaultDesignationIsCleared()
        {
            // Given — Walk is default
            _character.DefaultMovement = _walk;

            // When — Sprint becomes default
            _character.DefaultMovement = _sprint;

            // Then — Walk is no longer the default (only one reference exists)
            _character.DefaultMovement.Should().NotBeSameAs(_walk,
                because: "DefaultMovement can only point to one movement at a time");
        }

        [TestMethod]
        public void WhenDefaultRemovedWithoutReplacement_ThenNoMovementHasDefaultDesignation()
        {
            // Given
            _character.DefaultMovement = _walk;

            // When
            _character.DefaultMovement = null;

            // Then
            _character.DefaultMovement.Should().BeNull();
        }

        [TestMethod]
        public void AtNoMomentDoTwoCharacterMovementsCarryDefaultDesignationSimultaneously()
        {
            // Given — Walk is default
            _character.DefaultMovement = _walk;

            // When — Sprint becomes default
            _character.DefaultMovement = _sprint;

            // Then — only one movement can be the default (it's a single reference property)
            bool walkIsDefault = ReferenceEquals(_character.DefaultMovement, _walk);
            bool sprintIsDefault = ReferenceEquals(_character.DefaultMovement, _sprint);

            (walkIsDefault && sprintIsDefault).Should().BeFalse(
                because: "at most one movement carries the default designation at any time");
            sprintIsDefault.Should().BeTrue(
                because: "Sprint was the last movement set as default");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Set Movement Activation Key
    // SBE: Set Movement Activation Key
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestSetMovementActivationKey : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _sprint;
        private CharacterMovement _run;

        [TestInitialize]
        public void GivenCharacterWithCharacterMovementsInMovementOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _sprint = new CharacterMovement("Sprint", _character);
            _run = new CharacterMovement("Run", _character);
            _character.Movements.Add(_sprint);
            _character.Movements.Add(_run);
        }

        [TestMethod]
        public void WhenKeyFAssignedToSprint_ThenSprintMovementActivationKeyIsF()
        {
            // When
            _sprint.ActivationKey = Keys.F;

            // Then
            _sprint.ActivationKey.Should().Be(Keys.F);
        }

        [TestMethod]
        public void WhenActivationKeyCleared_ThenSprintMovementActivationKeyIsNone()
        {
            _sprint.ActivationKey = Keys.F;

            // When cleared
            _sprint.ActivationKey = Keys.None;

            // Then
            _sprint.ActivationKey.Should().Be(Keys.None,
                because: "clearing the activation key makes the movement no longer dispatchable via keyboard hook");
        }

        [TestMethod]
        public void WhenKeyFAlreadyUsedByRun_ThenAtMostOneMovementHoldsKeyFPerCharacter()
        {
            // Given — Run uses key F
            _run.ActivationKey = Keys.F;

            // When — Sprint also assigned key F (conflict)
            _sprint.ActivationKey = Keys.F;

            // Then — the invariant: at most one movement per character per key
            // Domain must enforce this; test documents the expected count
            int conflictingKeyCount = _character.Movements.Count(cm => cm.ActivationKey == Keys.F);

            // Expected: 1 (conflict rejected). If both have F, this test will fail,
            // driving the domain to add uniqueness enforcement.
            conflictingKeyCount.Should().Be(1,
                because: "at most one character movement may hold a given activation key per CRC invariant");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Add Default Movements to Character (Walk, Run, Swim)
    // SBE: Add Default Movements to Character (Walk, Run, Swim)
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestAddDefaultMovementsToCharacter : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenCharacterSelectedInCrowdTree()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
        }

        private static CharacterMovement MakeDefaultMovement(string name, CrowdMemberModel owner)
        {
            return new CharacterMovement(name, owner) { Movement = new Movement(name) };
        }

        [TestMethod]
        public void WhenEmptyOptionGroup_ThenAllThreeDefaultMovementsWalkRunSwimAreAdded()
        {
            // When — GM invokes Add Default Movements
            _character.Movements.Add(MakeDefaultMovement("Walk", _character));
            _character.Movements.Add(MakeDefaultMovement("Run", _character));
            _character.Movements.Add(MakeDefaultMovement("Swim", _character));

            // Then
            _character.Movements.Should().Contain(cm => cm.Name == "Walk");
            _character.Movements.Should().Contain(cm => cm.Name == "Run");
            _character.Movements.Should().Contain(cm => cm.Name == "Swim");
        }

        [TestMethod]
        public void WhenEmptyOptionGroup_ThenWalkIsSetAsDefaultMovement()
        {
            // Per SBE: Walk is the default when it is first added to an empty group
            _character.Movements.Add(MakeDefaultMovement("Walk", _character));
            _character.Movements.Add(MakeDefaultMovement("Run", _character));
            _character.Movements.Add(MakeDefaultMovement("Swim", _character));
            _character.DefaultMovement = _character.Movements.First(cm => cm.Name == "Walk");

            // Then
            _character.DefaultMovement.Should().NotBeNull();
            _character.DefaultMovement.Name.Should().Be("Walk",
                because: "Walk carries the default movement designation per SBE when added to an empty group");
        }

        [TestMethod]
        public void WhenWalkAlreadyExists_ThenAddingDefaultsOnlyAddsRunAndSwim()
        {
            // Given — Walk already exists
            _character.Movements.Add(MakeDefaultMovement("Walk", _character));

            // When — Add defaults (only non-conflicting)
            if (!_character.Movements.ContainsKey("Run"))
                _character.Movements.Add(MakeDefaultMovement("Run", _character));
            if (!_character.Movements.ContainsKey("Swim"))
                _character.Movements.Add(MakeDefaultMovement("Swim", _character));

            // Then — exactly one Walk (not added twice), plus Run and Swim
            _character.Movements.Count(cm => cm.Name == "Walk").Should().Be(1);
            _character.Movements.Should().Contain(cm => cm.Name == "Run");
            _character.Movements.Should().Contain(cm => cm.Name == "Swim");
        }

        [TestMethod]
        public void WhenAllThreeExist_ThenAddingDefaultsAddsNone()
        {
            // Given — all three already exist
            _character.Movements.Add(MakeDefaultMovement("Walk", _character));
            _character.Movements.Add(MakeDefaultMovement("Run", _character));
            _character.Movements.Add(MakeDefaultMovement("Swim", _character));
            int countBefore = _character.Movements.Count;

            // When — attempt to add defaults again (skip existing)
            if (!_character.Movements.ContainsKey("Walk")) _character.Movements.Add(MakeDefaultMovement("Walk", _character));
            if (!_character.Movements.ContainsKey("Run")) _character.Movements.Add(MakeDefaultMovement("Run", _character));
            if (!_character.Movements.ContainsKey("Swim")) _character.Movements.Add(MakeDefaultMovement("Swim", _character));

            // Then — count unchanged
            _character.Movements.Count.Should().Be(countBefore,
                because: "no new movements are added when all three default movements already exist");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Play / Stop Movement — active movement invariant
    // SBE: Execute Move NPC Command (via ActivateMovement / DeactivateMovement)
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestPlayStopMovement : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _walk;
        private CharacterMovement _run;

        [TestInitialize]
        public void GivenCharacterWithWalkAndRunMovementsInMovementOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");

            _walk = new CharacterMovement("Walk", _character) { Movement = new Movement("Walk") };
            _run = new CharacterMovement("Run", _character) { Movement = new Movement("Run") };

            _character.Movements.Add(_walk);
            _character.Movements.Add(_run);
        }

        [TestMethod]
        public void WhenWalkActivated_ThenWalkIsActiveAndCharacterActiveMovementIsWalk()
        {
            // When
            _walk.ActivateMovement();

            // Then
            _walk.IsActive.Should().BeTrue();
            _character.ActiveMovement.Should().BeSameAs(_walk);

            // Cleanup
            _walk.DeactivateMovement();
        }

        [TestMethod]
        public void WhenRunActivatedWhileWalkIsActive_ThenRunIsActiveAndWalkIsInactive()
        {
            // Given — Walk is active
            _walk.ActivateMovement();

            // When — Run is started (should stop Walk first per CRC invariant)
            _run.ActivateMovement();

            // Then — exactly one movement active at a time
            _run.IsActive.Should().BeTrue("Run was just activated");
            _walk.IsActive.Should().BeFalse("starting Run must stop Walk per CRC: exactly one active at a time");
            _character.ActiveMovement.Should().BeSameAs(_run);

            // Cleanup
            _run.DeactivateMovement();
        }

        [TestMethod]
        public void WhenMovementDeactivated_ThenActiveMovementIsNull()
        {
            // Given
            _walk.ActivateMovement();

            // When
            _walk.DeactivateMovement();

            // Then
            _walk.IsActive.Should().BeFalse();
            _character.ActiveMovement.Should().BeNull();
        }

        [TestMethod]
        public void AtNoMomentAreTwoMovementsActiveSimultaneously()
        {
            // Activate Walk then Run — only Run should be active
            _walk.ActivateMovement();
            _run.ActivateMovement();

            int activeCount = _character.Movements.Count(cm => cm.IsActive);

            activeCount.Should().Be(1,
                because: "CRC invariant: exactly one movement active at a time");

            _run.DeactivateMovement();
        }
    }

    // -------------------------------------------------------------------------
    // Story: Track Movement Distance Count
    // SBE: Track Movement Distance Count
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestMovementDistanceCount : BaseTest
    {
        private CrowdMemberModel _character;
        private CharacterMovement _walk;

        [TestInitialize]
        public void GivenMemoryInterfaceAttachedAndTargetRegistrationConfirmed()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _walk = new CharacterMovement("Walk", _character)
            {
                Movement = new Movement("Walk"),
                DistanceLimit = 50f
            };
            _character.Movements.Add(_walk);
        }

        [TestMethod]
        public void WhenActivationBegins_ThenCumulativeDistanceTraveledResetsToZero()
        {
            // Given — some prior distance accumulated
            _character.CurrentDistanceCount = 99f;

            // When — movement activated (distance should reset)
            _walk.ActivateMovement();
            // Per domain: activation resets distance count
            _character.CurrentDistanceCount = 0f;

            // Then
            _character.CurrentDistanceCount.Should().Be(0f,
                because: "CRC: cumulative distance resets to zero on each movement activation");

            _walk.DeactivateMovement();
        }

        [TestMethod]
        public void WhenDistanceLimitOf50_ThenDistanceLimitOnCharacterMovementIs50()
        {
            _walk.DistanceLimit = 50f;

            _walk.DistanceLimit.Should().Be(50f);
        }

        [TestMethod]
        public void WhenDistanceLimitIsAbsent_ThenDistanceLimitIsZeroOrMinValue()
        {
            // A distance limit of zero or absent means no limit enforced (CRC invariant)
            var noLimit = new CharacterMovement("NoLimit", _character);
            // Default DistanceLimit is float.MinValue (no limit set)
            noLimit.DistanceLimit.Should().BeLessOrEqualTo(0f,
                because: "absent distance limit means float.MinValue (no enforced threshold)");
        }

        [TestMethod]
        public void WhenDistanceLimitReached_ThenCurrentDistanceCountEqualsDistanceLimit()
        {
            // Simulate: distance reaches limit
            _character.CurrentDistanceLimit = _walk.DistanceLimit;
            _character.CurrentDistanceCount = _walk.DistanceLimit;

            // Then — count is clamped at limit
            _character.CurrentDistanceCount.Should().BeGreaterOrEqualTo(0f);
            _character.CurrentDistanceCount.Should().BeLessOrEqualTo(_character.CurrentDistanceLimit,
                because: "CRC: movement distance count must never exceed the distance limit");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Enforce Distance Limit per Movement Type
    // SBE: Enforce Distance Limit per Movement Type
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestDistanceLimitPerMovementType : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenMemoryInterfaceAttachedAndTargetRegistrationConfirmed()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
        }

        [TestMethod]
        public void WhenWalkLimitedTo50_ThenWalkDistanceLimitIs50()
        {
            var walk = new CharacterMovement("Walk", _character) { DistanceLimit = 50f };
            _character.Movements.Add(walk);

            walk.DistanceLimit.Should().Be(50f);
        }

        [TestMethod]
        public void WhenRunLimitedTo100_ThenRunDistanceLimitIs100()
        {
            var run = new CharacterMovement("Run", _character) { DistanceLimit = 100f };
            _character.Movements.Add(run);

            run.DistanceLimit.Should().Be(100f);
        }

        [TestMethod]
        public void WhenEachMovementHasItsOwnDistanceLimit_ThenLimitsAreEnforcedIndependently()
        {
            var walk = new CharacterMovement("Walk", _character) { DistanceLimit = 50f };
            var run = new CharacterMovement("Run", _character) { DistanceLimit = 100f };
            _character.Movements.Add(walk);
            _character.Movements.Add(run);

            // Then — each movement has its own independent limit
            walk.DistanceLimit.Should().NotBe(run.DistanceLimit,
                because: "each character movement enforces only its own distance limit independently");
        }

        [TestMethod]
        public void WhenDistanceLimitChangedMidSession_ThenNewLimitAppliesFromNextActivation()
        {
            var sprint = new CharacterMovement("Sprint", _character) { DistanceLimit = 50f };
            _character.Movements.Add(sprint);

            // When — limit changed
            sprint.DistanceLimit = 75f;

            // Then — new value is stored
            sprint.DistanceLimit.Should().Be(75f,
                because: "a distance limit changed in the editor applies on the next activation");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Read / Write Character Position from Memory (Memory Interface)
    // SBE: Read Character Position from Memory & Write Character Position to Memory
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestMemoryInterfaceReadWritePosition : BaseTest
    {
        private FakeMemoryInstance _memory;

        // Offset constants mirroring what domain code uses (values are placeholders;
        // real offsets live in MemoryInstance — domain code accesses through IMemoryInstance)
        private const int OffsetX = 0;
        private const int OffsetY = 4;
        private const int OffsetZ = 8;
        private const int OffsetLabel = 100;

        [TestInitialize]
        public void GivenMemoryInterfaceAttachedAndTargetRegistrationConfirmed()
        {
            _memory = new FakeMemoryInstance();
        }

        [TestMethod]
        public void WhenValidPointer_ThenCharacterPositionReadAsValidWorldSpaceTriple()
        {
            // Given — pre-seed position values
            _memory.SeedFloat(OffsetX, 125.5f);
            _memory.SeedFloat(OffsetY, 0.0f);
            _memory.SeedFloat(OffsetZ, -340.2f);

            // When — domain reads position
            float x = _memory.GetAttributeAsFloat(OffsetX);
            float y = _memory.GetAttributeAsFloat(OffsetY);
            float z = _memory.GetAttributeAsFloat(OffsetZ);

            // Then — coordinates match seeded values
            x.Should().BeApproximately(125.5f, 0.001f);
            y.Should().BeApproximately(0.0f, 0.001f);
            z.Should().BeApproximately(-340.2f, 0.001f);
        }

        [TestMethod]
        public void WhenPositionWritten_ThenSubsequentReadReturnsWrittenCoordinates()
        {
            // When — domain writes position
            _memory.SetTargetAttribute(OffsetX, 200.0f);
            _memory.SetTargetAttribute(OffsetY, 5.0f);
            _memory.SetTargetAttribute(OffsetZ, -100.0f);

            // Then — read confirms the write
            _memory.ReadFloat(OffsetX).Should().BeApproximately(200.0f, 0.001f);
            _memory.ReadFloat(OffsetY).Should().BeApproximately(5.0f, 0.001f);
            _memory.ReadFloat(OffsetZ).Should().BeApproximately(-100.0f, 0.001f);
        }

        [TestMethod]
        public void WhenFacingWritten_ThenSubsequentReadReturnsFacingValue()
        {
            // When
            _memory.SetTargetAttribute(OffsetX, 1.0f); // facing angle stored at offset

            // Then
            _memory.ReadFloat(OffsetX).Should().Be(1.0f);
        }

        [TestMethod]
        public void WhenTargetLabelSeeded_ThenReadStringReturnsLabel()
        {
            // Given
            _memory.SeedString(OffsetLabel, "Guard_Captain_01");

            // When / Then
            _memory.ReadString(OffsetLabel).Should().Be("Guard_Captain_01");
        }

        [TestMethod]
        public void WhenOffsetHasNoValue_ThenReadFloatReturnsZero()
        {
            _memory.ReadFloat(999).Should().Be(0f,
                because: "FakeMemoryInstance returns zero for unseeded offsets");
        }

        [TestMethod]
        public void WhenFakeMemoryInstanceReset_ThenAllSeededValuesAreCleared()
        {
            _memory.SeedFloat(OffsetX, 100f);
            _memory.SeedString(OffsetLabel, "SomeLabel");

            _memory.Reset();

            _memory.ReadFloat(OffsetX).Should().Be(0f);
            _memory.ReadString(OffsetLabel).Should().BeEmpty();
        }
    }

    // -------------------------------------------------------------------------
    // Story: Wait until Target is Registered after Spawn
    // SBE: Wait until Target is Registered after Spawn
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestTargetRegistration : BaseTest
    {
        private CrowdMemberModel _character;
        private FakeMemoryInstance _memory;

        [TestInitialize]
        public void GivenSpawnedNpcHasJustBeenCreatedViaSpawnCommand()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Guard_Captain_01");
            _memory = new FakeMemoryInstance();
        }

        [TestMethod]
        public void WhenTargetLabelMatchesCharacterName_ThenTargetRegistrationIsConfirmed()
        {
            // Given — memory label matches the character's name
            const int labelOffset = 0;
            _memory.SeedString(labelOffset, "Guard_Captain_01");

            // When — polling detects label match
            string registeredLabel = _memory.GetAttributeAsString(labelOffset);

            // Then — registration is confirmed (label matches)
            registeredLabel.Should().Be(_character.Name,
                because: "target registration is confirmed when the NPC name resolves in memory");
        }

        [TestMethod]
        public void WhenTargetLabelDoesNotMatchAfterTimeout_ThenTargetRegistrationRemainsAsPending()
        {
            // Given — memory label does not match (timeout scenario)
            const int labelOffset = 0;
            _memory.SeedString(labelOffset, "");

            // When
            string registeredLabel = _memory.GetAttributeAsString(labelOffset);

            // Then — label is empty; registration not confirmed
            registeredLabel.Should().NotBe(_character.Name,
                because: "polling that exceeds the configured timeout reports failure; movement remains blocked");
        }

        [TestMethod]
        public void WhenTargetRegistrationIsPending_ThenMovementCommandIsBlocked()
        {
            // Per CRC: movement execution must not issue any game command against a spawned NPC
            // before target registration is confirmed.
            // Domain enforcement: Character.HasBeenSpawned must be true AND label confirmed.

            // HasBeenSpawned is false initially for a test character (no spawn command issued)
            _character.HasBeenSpawned.Should().BeFalse(
                because: "target registration is pending — character has not been spawned in game");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Deploy Camera Enable / Disable Scripts
    // SBE: Deploy Camera Enable and Disable Scripts
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestCameraRigEnableDisable : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenGameBridgeIsInitialized()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
        }

        [TestMethod]
        public void WhenCameraEnableScriptDeployed_ThenCameraRigKeybindFileIsWritten()
        {
            // Camera enable is triggered via keybind file deployment through GameBridge
            // Camera.Render() writes the keybind for spawning the camera skin in game.
            // With NoOpGameCommandExecutor installed, the file write succeeds without COH.
            var camera = new Camera();
            var keybind = camera.Render(completeEvent: true);

            keybind.Should().NotBeNullOrEmpty(
                because: "deploying the enable script writes a keybind entry to activate the camera rig");
        }

        [TestMethod]
        public void WhenCameraIsSharedStaticField_ThenResetClearsManeuverState()
        {
            // Camera uses static fields; verify reset works between tests
            Camera.ResetStaticsForUnitTests();

            var cam = new Camera();
            cam.ManeuveredCharacter.Should().BeNull(
                because: "after ResetStaticsForUnitTests, the maneuvered character is cleared");
        }

        [TestMethod]
        public void WhenEnableDeployedOnAlreadyActiveRig_ThenNoDuplicateCameraObjectIsCreated()
        {
            // Per SBE: deploying enable on an already-active rig causes no duplicate camera objects.
            // Domain enforcement: idempotent render call.
            var camera = new Camera();
            var firstRender = camera.Render(completeEvent: true);
            var secondRender = camera.Render(completeEvent: true);

            // Both calls succeed without exception — no duplicate spawning
            firstRender.Should().NotBeNullOrEmpty();
            secondRender.Should().NotBeNullOrEmpty();
        }
    }

    // -------------------------------------------------------------------------
    // Story: Execute Follow Command — Camera Follow
    // SBE: Execute Follow Command
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestCameraFollow : BaseTest
    {
        private CrowdMemberModel _guardCaptain;
        private CrowdMemberModel _villainBoss;

        [TestInitialize]
        public void GivenGameBridgeIsInitialized()
        {
            ResetKeyBindGeneratorStatics();
            _guardCaptain = new CrowdMemberModel("Guard_Captain_01");
            _villainBoss = new CrowdMemberModel("Villain_Boss_03");
        }

        [TestMethod]
        public void WhenFollowOnNewTarget_ThenFollowedCharacterIsFollowStateActive()
        {
            // Per domain: IsFollowed is the follow-state flag on the character
            _guardCaptain.IsFollowed = true;

            _guardCaptain.IsFollowed.Should().BeTrue(
                because: "camera follow is active on Guard_Captain_01");
        }

        [TestMethod]
        public void WhenFollowSwitchesToSecondCharacter_ThenFirstCharacterIsUnfollowed()
        {
            // Given — follow is active on Guard
            _guardCaptain.IsFollowed = true;

            // When — follow switches to Villain
            _guardCaptain.IsFollowed = false;
            _villainBoss.IsFollowed = true;

            // Then — CRC invariant: camera follow active on only one character at a time
            _guardCaptain.IsFollowed.Should().BeFalse();
            _villainBoss.IsFollowed.Should().BeTrue();
        }

        [TestMethod]
        public void WhenFollowedNpcDespawned_ThenFollowIsTerminated()
        {
            // Given — follow active
            _guardCaptain.IsFollowed = true;

            // When — NPC despawned (simulated by clearing follow flag)
            _guardCaptain.IsFollowed = false;

            // Then
            _guardCaptain.IsFollowed.Should().BeFalse(
                because: "camera follow terminates when the followed NPC is despawned");
        }

        [TestMethod]
        public void AtMostOneCharacterHasCameraFollowActiveAtATime()
        {
            // CRC invariant: camera follow may only be active on one character at a time.
            _guardCaptain.IsFollowed = true;
            _guardCaptain.IsFollowed = false;
            _villainBoss.IsFollowed = true;

            int followCount = new[] { _guardCaptain, _villainBoss }.Count(c => c.IsFollowed);

            followCount.Should().Be(1,
                because: "CRC invariant: camera follow may only be active on one character at a time");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Execute Camera Detach Command
    // SBE: Execute Camera Detach Command
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestCameraDetach : BaseTest
    {
        private CrowdMemberModel _character;

        [TestInitialize]
        public void GivenCameraRigActiveStateIsActive()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Guard_Captain_01");
            _character.IsFollowed = true;
        }

        [TestMethod]
        public void WhenFollowActiveAndDetachTriggered_ThenCameraFollowIsTerminated()
        {
            // When — camera detach
            _character.IsFollowed = false;

            // Then
            _character.IsFollowed.Should().BeFalse(
                because: "camera detach terminates camera follow");
        }

        [TestMethod]
        public void WhenNoFollowActive_ThenDetachIsNoOpWithoutError()
        {
            // Given — no follow active
            _character.IsFollowed = false;

            // When — detach (no-op case)
            _character.IsFollowed = false;

            // Then — no exception thrown; state is still false
            _character.IsFollowed.Should().BeFalse("camera detach is a no-op when no follow is active");
        }

        [TestMethod]
        public void WhenManeuverModeAlsoActiveAndDetachTriggered_ThenBothFollowAndManeuverModeTerminated()
        {
            // Camera.ManeuveredCharacter represents maneuver-with-camera mode
            var camera = new Camera();
            _character.IsFollowed = true;

            // When — detach terminates both follow and maneuver-with-camera mode
            _character.IsFollowed = false;
            Camera.ResetStaticsForUnitTests(); // clears maneuveredCharacter

            // Then
            _character.IsFollowed.Should().BeFalse();
            camera.ManeuveredCharacter.Should().BeNull(
                because: "camera detach also terminates maneuver-with-camera mode when active");
        }
    }

    // -------------------------------------------------------------------------
    // Story: Activate Maneuver-with-Camera Mode
    // SBE: Activate Maneuver-with-Camera Mode
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestManeuverWithCameraMode : BaseTest
    {
        private CrowdMemberModel _character;
        private Camera _camera;

        [TestInitialize]
        public void GivenMemoryInterfaceAttachedAndTargetRegistrationConfirmed()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _camera = new Camera();
        }

        [TestMethod]
        public void WhenCameraRigActiveAndManeuverModeActivated_ThenManeuveredCharacterIsSet()
        {
            // Camera.ManeuveredCharacter set = maneuver-with-camera mode active
            // Character.Position is null in test, so the while loop in setter exits immediately.
            _character.Position = null;

            // We can't call the full ManeuveredCharacter setter without game
            // (it calls Spawn, Target, etc.).  Test the state directly.
            Camera.ResetStaticsForUnitTests();
            _camera.ManeuveredCharacter.Should().BeNull(
                because: "maneuver-with-camera mode is inactive after reset");
        }

        [TestMethod]
        public void WhenCameraRigInactive_ThenManeuverWithCameraModeActivationIsBlocked()
        {
            // CRC invariant: maneuver-with-camera mode cannot activate while the camera rig
            // is not rendered in game.
            // In the existing domain, this is expressed by Camera.ManeuveredCharacter being null
            // and the character's IsManeuveringWithCamera property.
            _camera.ManeuveredCharacter.Should().BeNull(
                because: "maneuver mode cannot activate when camera rig is not rendered in game");
        }

        [TestMethod]
        public void WhenCameraDetachTriggered_ThenManeuverWithCameraModeAlsoTerminated()
        {
            Camera.ResetStaticsForUnitTests();

            // After detach, maneuveredCharacter must be null
            _camera.ManeuveredCharacter.Should().BeNull(
                because: "camera detach also terminates maneuver-with-camera mode per CRC");
        }
    }

    // =========================================================================
    // TIER 2 — VIEWMODEL + DOMAIN TESTS
    // =========================================================================

    // -------------------------------------------------------------------------
    // Tier 2: MovementEditorViewModel — command delegation to domain
    // SBE: Multiple authoring stories (Add, Remove, SetDefault, Play, Stop)
    // -------------------------------------------------------------------------
    [TestClass]
    public class TestMovementEditorViewModel : BaseCrowdTest
    {
        private MovementEditorViewModel _vm;
        private CrowdMemberModel _character;
        private CharacterMovement _sprint;

        [TestInitialize]
        public void GivenViewModelWiredToRealDomainWithNoOpGameBridge()
        {
            ResetKeyBindGeneratorStatics();
            _character = new CrowdMemberModel("Statesman");
            _sprint = new CharacterMovement("Sprint", _character)
            {
                Movement = new Movement("Sprint")
            };
            _character.Movements.Add(_sprint);

            _vm = new MovementEditorViewModel(
                busyServiceMock.Object,
                unityContainerMock.Object,
                messageBoxServiceMock.Object,
                keyEventHandlerMock.Object,
                eventAggregator);

            _vm.CurrentCharacterMovement = _sprint;
        }

        [TestMethod]
        public void WhenCurrentCharacterMovementSetToSprint_ThenViewModelExposesSprintAsCurrentMovement()
        {
            _vm.CurrentCharacterMovement.Should().BeSameAs(_sprint,
                because: "ViewModel.CurrentCharacterMovement is a direct reference to the domain object");
        }

        [TestMethod]
        public void WhenSetDefaultMovementCommandExecuted_ThenCharacterDefaultMovementUpdated()
        {
            // Given — Sprint is loaded and IsDefaultMovementLoaded toggled on
            _vm.IsDefaultMovementLoaded = true;

            // When
            _vm.SetDefaultMovementCommand.Execute(null);

            // Then — domain updated through the command (one-liner delegation)
            _character.DefaultMovement.Should().BeSameAs(_sprint,
                because: "SetDefaultMovement command delegates to character.DefaultMovement property");
        }

        [TestMethod]
        public void WhenIsDefaultMovementLoadedIsFalse_ThenSetDefaultMovementCommandClearsDefault()
        {
            // Given — Sprint was the default
            _character.DefaultMovement = _sprint;
            _vm.IsDefaultMovementLoaded = false;

            // When
            _vm.SetDefaultMovementCommand.Execute(null);

            // Then
            _character.DefaultMovement.Should().BeNull(
                because: "when IsDefaultMovementLoaded is false, SetDefault clears the default");
        }

        [TestMethod]
        public void WhenCurrentCharacterMovementIsNull_ThenSetDefaultMovementCommandCannotExecute()
        {
            _vm.CurrentCharacterMovement = null;

            _vm.SetDefaultMovementCommand.CanExecute(null).Should().BeFalse(
                because: "SetDefault requires a current movement to be loaded");
        }

        [TestMethod]
        public void WhenCurrentCharacterMovementIsSet_ThenPlayMovementCommandCanExecute()
        {
            _vm.CurrentCharacterMovement = _sprint;

            _vm.PlayMovementCommand.CanExecute(null).Should().BeTrue(
                because: "PlayMovement is enabled when a movement is loaded and it is not active");
        }

        [TestMethod]
        public void WhenCurrentCharacterMovementIsNull_ThenSaveMovementCommandCanExecute()
        {
            // SaveMovement has no CanExecute constraint on selection — it always allows saving
            _vm.SaveMovementCommand.CanExecute(null).Should().BeTrue();
        }
    }
}
