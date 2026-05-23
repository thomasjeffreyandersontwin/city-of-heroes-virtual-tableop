using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    [TestClass]
    public class AddSoundElementToAbility : AnimationElementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void ValidResourceSelectedSoundElementAdded()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Sound");

            // When
            WhenGmAddsSoundElement("Thunder Clap");

            // Then
            ThenElementExists("Sound", "Thunder Clap");
            ThenElementAtBottom();
        }

        [TestMethod]
        public void SoundElementExecutedDuringAbilityPlay()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Sound");
            GivenSoundElement("Thunder Clap", 1);

            // When
            WhenAbilityExecutesElement("Sound", "Thunder Clap");

            // Then
            ThenGameCommandIssued("Sound", "Thunder Clap");
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void SoundResourceNotFoundAtExecutionTime()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Sound");
            GivenSoundElement("Missing_Sound", 1);

            // When
            WhenAbilityExecutesElement("Sound", "Missing_Sound");

            // Then
            ThenElementProducesNoOp();
            ThenSubsequentElementsContinue();
        }

        [TestMethod]
        public void MultipleSoundElementsPlayInSequence()
        {
            // Given
            GivenAbilityOpenInEditor("Fire Strike");
            GivenResourceCatalogLoaded("Sound");
            GivenSoundElement("Thunder Clap", 1);
            GivenSoundElement("Wind Gust", 2);

            // When
            WhenAbilityExecutesElement("Sound", "Thunder Clap");

            // Then
            ThenSubsequentElementsContinue();
        }
    }
}
