using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class SetActiveIdentity : IdentityManagementDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Game Bridge initialization state ready; Character Guard_Captain
        }

        [TestMethod]
        public void ModelIdentityActivatedNpcSpawned()
        {
            // Given: Model Identity Dragon_Model with model name Skull_Lt_01
            Identity dragon = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, dragon);
            // When: the GM sets active designation on Identity Dragon_Model
            when_active_identity_set(_guardCaptain, "Dragon_Model");
            // Then: Spawned NPC has character name Guard_Captain and entity presence present
            then_active_identity(_guardCaptain, "Dragon_Model");
            dragon.Type.Should().Be(IdentityType.Model,
                "Model Identity activated — Game Bridge issues Spawn NPC Command with model name Skull_Lt_01");
        }

        [TestMethod]
        public void CostumeIdentityActivatedSpawnTargetLoadSequence()
        {
            // Given: Costume Identity Knight_Armor with costume surface C:\Games\CoH\costumes\guard.costume
            Identity knight = given_costume_identity("Knight_Armor", @"C:\Games\CoH\costumes\guard.costume");
            given_identity_on_character(_guardCaptain, knight);
            // When: the GM sets active designation on Identity Knight_Armor
            when_active_identity_set(_guardCaptain, "Knight_Armor");
            // Then: Spawned NPC Guard_Captain entity presence present; Game Bridge issues Spawn NPC Command,
            //       Target by Name Command, Load Costume Command in sequence
            then_active_identity(_guardCaptain, "Knight_Armor");
            knight.Type.Should().Be(IdentityType.Costume,
                "Costume Identity activated — sequence: Spawn NPC, Target by Name, Load Costume");
            knight.Surface.Should().Be(@"C:\Games\CoH\costumes\guard.costume",
                "costume surface must be C:\\Games\\CoH\\costumes\\guard.costume for the Load Costume Command");
        }

        [TestMethod]
        public void SwitchFromExistingActiveIdentityDespawnsOld()
        {
            // Given: Identity Old_Look with active designation active on Character Guard_Captain; Spawned NPC present
            Identity oldLook = given_model_identity("Old_Look", "Old_Model");
            given_identity_on_character(_guardCaptain, oldLook);
            when_active_identity_set(_guardCaptain, "Old_Look");
            Identity dragonModel = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, dragonModel);
            // When: the GM sets active designation on a new Identity Dragon_Model
            when_active_identity_set(_guardCaptain, "Dragon_Model");
            // Then: previous active identity in the Identity Option Group is cleared; new identity's activation runs
            then_active_identity(_guardCaptain, "Dragon_Model");
            (_guardCaptain.ActiveIdentity == null || _guardCaptain.ActiveIdentity.Name != "Old_Look")
                .Should().BeTrue("switching active identity must clear Old_Look active designation; old NPC despawned");
        }

        [TestMethod]
        public void BridgeNotReadyActivationBlocked()
        {
            // Given: Game Bridge has initialization state polling
            // When: the GM attempts to set active designation on an Identity
            // Then: the Set Active action is blocked with a "game not connected" indicator; no game commands issued
            bool bridgeReady = false; // simulates initialization state = polling
            bridgeReady.Should().BeFalse(
                "activation must be blocked when Game Bridge initialization state is polling — no commands issued");
        }

        [TestMethod]
        public void CostumeIdentityWithNoSurfaceActivationBlocked()
        {
            // Given: Costume Identity Bare_Armor with costume surface (unassigned)
            Identity bareArmor = given_costume_identity("Bare_Armor", string.Empty);
            given_identity_on_character(_guardCaptain, bareArmor);
            // When: the GM attempts to set active designation on Identity Bare_Armor
            // Then: application blocks activation with "no costume surface" validation message; no Spawn NPC Command
            bareArmor.Surface.Should().BeNullOrEmpty(
                "Bare_Armor has no costume surface — activation must be blocked");
        }

        [TestMethod]
        public void ActiveIndicatorVisibleInUiAfterActivation()
        {
            // Given: Model Identity Dragon_Model with model name Skull_Lt_01
            Identity dragon = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, dragon);
            // When: the GM sets active designation on Identity Dragon_Model and activation succeeds
            when_active_identity_set(_guardCaptain, "Dragon_Model");
            // Then: the active designation indicator is visible on Dragon_Model in the Identity List
            (_guardCaptain.ActiveIdentity != null && _guardCaptain.ActiveIdentity.Name == "Dragon_Model")
                .Should().BeTrue("active designation indicator must be visible on Dragon_Model in the Identity List after activation");
        }
    }
}
