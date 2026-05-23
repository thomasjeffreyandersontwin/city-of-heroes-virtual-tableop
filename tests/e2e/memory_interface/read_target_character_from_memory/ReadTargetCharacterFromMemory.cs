using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class ReadTargetCharacterFromMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void CharacterSelectedInCoh()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();

            // When
            WhenGmSelectsCharacterInGame("Guard_Captain_01");

            // Then
            ThenCurrentTarget("Guard_Captain_01");
        }

        [TestMethod]
        public void NoCharacterSelected()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();

            // When
            WhenGmSelectsCharacterInGame("empty");

            // Then
            ThenCurrentTarget("empty");
            ThenMovementCommandsBlocked();
        }

        [TestMethod]
        public void GmChangesSelectedCharacter()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenCurrentTarget("Guard_Captain_01");

            // When
            WhenGmSelectsCharacterInGame("Villain_Boss_03");

            // Then
            ThenCurrentTarget("Villain_Boss_03");
            ThenMovementExecutionNotified();
        }
    }
}
