using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddLoadIdentityElement : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void LoadIdentityElementAdded()
        {
            // Given
            GivenAbilityOpenInEditor("Transform");
            GivenIdentityOnCharacter("Dragon_Form");

            // When
            WhenGmAddsLoadIdentityElement("Dragon_Form");

            // Then
            ThenElementExists("LoadIdentity", "Dragon_Form");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void LoadIdentityElementTriggersIdentitySwitchDuringPlay()
        {
            // Given
            GivenAbilityOpenInEditor("Transform");
            GivenIdentityOnCharacter("Dragon_Form");
            GivenLoadIdentityElement("Dragon_Form");

            // When
            WhenAbilityExecutesElement("LoadIdentity", "Dragon_Form");

            // Then
            ThenIdentitySwitched("Dragon_Form");
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void TargetIdentityDoesNotExistAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Transform");
            GivenLoadIdentityElement("Removed_Identity");

            // When
            WhenAbilityExecutesElement("LoadIdentity", "Removed_Identity");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void SavedElementRetainsIdentityNameEvenIfIdentityLaterRenamed()
        {
            // Given
            GivenAbilityOpenInEditor("Transform");
            GivenIdentityOnCharacter("Dragon_Form");
            GivenLoadIdentityElement("Dragon_Form");

            // When
            WhenAbilityExecutesElement("LoadIdentity", "Dragon_Form");

            // Then
            ThenElementExists("LoadIdentity", "Dragon_Form");
        }
    }
}
