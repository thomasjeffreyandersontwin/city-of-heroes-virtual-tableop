using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CameraRig
{
    [TestClass]
    public class FollowCharacterWithGameCamera : CameraRigHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void CharacterMovesCameraTracks()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");
            GivenCharacterPosition("300.0", "0.0", "-50.0");

            // When
            WhenSpawnedNpcMoves("Guard_Captain_01", "300.0", "0.0", "-50.0");

            // Then
            ThenCameraTracksCharacter();
        }

        [TestMethod]
        public void MovementCommandWhileFollowActive()
        {
            // Given
            GivenGameBridgeInitialized();
            GivenCameraRigState("active");
            GivenCameraFollowState("active", "Guard_Captain_01");
            GivenCharacterPosition("350.0", "5.0", "-75.0");

            // When
            WhenSpawnedNpcMoves("Guard_Captain_01", "350.0", "5.0", "-75.0");

            // Then
            ThenCameraTracksCharacter();
        }
    }
}
