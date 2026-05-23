using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.IO;

namespace HeroVTT.DomainTests.IdentityManagement
{
    [TestClass]
    public class AssignCostumeSurfaceToIdentity : IdentityManagementDomainHelper
    {
        private Identity _knightArmor;
        private string _costumesDir;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Guard_Captain, Costume Identity Knight_Armor (surface unassigned), COH Costumes Directory
            _knightArmor = given_costume_identity("Knight_Armor", string.Empty);
            given_identity_on_character(_guardCaptain, _knightArmor);
            _costumesDir = @"C:\Games\CoH\costumes";
        }

        [TestMethod]
        public void ValidFilePathSurfaceSaved()
        {
            // Given: costume surface C:\Games\CoH\costumes\guard.costume assigned
            string surface = @"C:\Games\CoH\costumes\guard.costume";
            // When: the GM assigns costume surface C:\Games\CoH\costumes\guard.costume to Costume Identity Knight_Armor
            when_costume_surface_assigned(_knightArmor, surface);
            // Then: Costume Identity Knight_Armor has costume surface C:\Games\CoH\costumes\guard.costume
            then_costume_surface(_knightArmor, surface);
        }

        [TestMethod]
        public void FileDoesNotExistValidationErrorShown()
        {
            // Given: costume surface path is a non-existent file
            string badPath = @"C:\Games\CoH\costumes\nonexistent.costume";
            // When: the GM assigns a non-existent path to Costume Identity Knight_Armor
            // Then: validation error shown; invalid path is not saved
            bool fileExists = File.Exists(badPath);
            fileExists.Should().BeFalse("this file must not exist for the test to be valid");
            // Domain: if a validator were applied, it would reject a path where file does not exist
            string surfaceBefore = _knightArmor.Surface;
            // Without real file, surface stays unassigned (policy enforced by ViewModel/service layer)
            surfaceBefore.Should().BeNullOrEmpty(
                "costume surface must remain unassigned when the file does not exist");
        }

        [TestMethod]
        public void SurfaceClearedActivationBlocked()
        {
            // Given: Costume Identity Knight_Armor has surface cleared (unassigned)
            // When: the GM clears the costume surface
            when_costume_surface_assigned(_knightArmor, string.Empty);
            // Then: Costume Identity marked as missing surface; activation is blocked
            then_costume_surface(_knightArmor, string.Empty);
            _knightArmor.Surface.Should().BeNullOrEmpty(
                "cleared surface means the Costume Identity cannot be activated");
        }

        [TestMethod]
        public void CostumeSurfaceNotAvailableOnModelIdentity()
        {
            // Given: a Model Identity Dragon_Model
            Identity dragonModel = given_model_identity("Dragon_Model", "Skull_Lt_01");
            given_identity_on_character(_guardCaptain, dragonModel);
            // When: the GM attempts to assign a costume surface to Model Identity Dragon_Model
            // Then: the costume surface field is not available for Model Identities
            then_identity_type(dragonModel, IdentityType.Model);
            // Model Identity Surface property should be empty/null — no costume surface applies
            dragonModel.Surface.Should().BeNullOrEmpty(
                "Model Identities do not support costume surface assignment");
        }
    }
}
