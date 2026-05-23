using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.IdentityManagement
{
    [TestClass]
    public class RemoveIdentityFromCharacter : IdentityManagementHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NotActiveNotDefaultRemovedFromList()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Old_Armor", "inactive", "unset");

            // When
            WhenGmRemovesIdentity("Old_Armor", "Guard_Captain");

            // Then
            ThenIdentityNotInList("Old_Armor");
        }

        [TestMethod]
        public void CurrentlyActiveNpcDespawnedBeforeRemoval()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Dragon_Model", "active", "unset");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGmRemovesIdentity("Dragon_Model", "Guard_Captain");

            // Then
            ThenIdentityNotInList("Dragon_Model");
            ThenSpawnedNpcPresence("Guard_Captain", "absent");
        }

        [TestMethod]
        public void IsDefaultIdentityDefaultFlagCleared()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Knight_Armor", "inactive", "default");

            // When
            WhenGmRemovesIdentity("Knight_Armor", "Guard_Captain");

            // Then
            ThenIdentityNotInList("Knight_Armor");
        }

        [TestMethod]
        public void LastIdentityOnCharacterListEmpty()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Solo_Look", "inactive", "unset");

            // When
            WhenGmRemovesIdentity("Solo_Look", "Guard_Captain");

            // Then
            ThenIdentityListEmpty();
        }

        [TestMethod]
        public void BothActiveAndDefaultDespawnedAndFlagsCleared()
        {
            // Given
            GivenGameBridgeReady();
            GivenCharacterSelected("Guard_Captain");
            GivenIdentityOnCharacter("Guard_Captain", "Dragon_Model", "active", "default");
            GivenSpawnedNpc("Guard_Captain", "present");

            // When
            WhenGmRemovesIdentity("Dragon_Model", "Guard_Captain");

            // Then
            ThenIdentityNotInList("Dragon_Model");
            ThenSpawnedNpcPresence("Guard_Captain", "absent");
        }
    }
}
