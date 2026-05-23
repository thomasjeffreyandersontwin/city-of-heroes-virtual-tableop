using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class ExecuteFollowCommand : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void FollowOnNewTarget()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("inactive", "none");
            GivenSpawnedNpc("Guard_Captain_01");

            // When
            WhenGmTriggersFollow("Guard_Captain_01");

            // Then
            ThenCameraFollowState("active", "Guard_Captain_01");
        }

        [TestMethod]
        public void SwitchFollowToSecondCharacter()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");
            GivenSpawnedNpc("Villain_Boss_03");

            // When
            WhenGmTriggersFollow("Villain_Boss_03");

            // Then
            ThenCameraFollowState("active", "Villain_Boss_03");
        }

        [TestMethod]
        public void FollowedNpcDespawnedAutoDetach()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");

            // When
            WhenFollowedNpcDespawned("Guard_Captain_01");

            // Then
            ThenCameraFollowState("inactive", "none");
        }

        [TestMethod]
        public void RigNotActiveFollowRejected()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("inactive");
            GivenSpawnedNpc("Guard_Captain_01");

            // When
            WhenGmTriggersFollow("Guard_Captain_01");

            // Then
            ThenFollowRejected();
        }
    }
}
