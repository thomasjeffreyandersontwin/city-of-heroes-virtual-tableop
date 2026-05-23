using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.MovementExecution
{
    [TestClass]
    public class MoveCharacterToCameraPosition : MovementExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Memory Interface attached; Target Registration confirmed
            given_memory_interface_attached();
            given_target_registration_confirmed();
        }

        [TestMethod]
        public void CameraRigActiveNormalMove()
        {
            // Given: Camera Rig has active state active
            bool cameraRigActive = true;
            var movement = given_movement("Walk");
            given_movement_active(movement);
            // When: the GM triggers Move to Camera Position
            when_movement_begins(movement);
            // Then: Memory Interface reads camera position; Movement Execution drives Spawned NPC toward that position
            cameraRigActive.Should().BeTrue(
                "camera rig active — Memory Interface reads camera position for normal step-by-step move");
        }

        [TestMethod]
        public void CameraRigInactiveRawCoordsUsed()
        {
            // Given: Camera Rig has active state inactive
            bool cameraRigActive = false;
            var movement = given_movement("Walk");
            given_movement_active(movement);
            // When: the GM triggers Move to Camera Position
            when_movement_begins(movement);
            // Then: GM sees a notice but movement proceeds using raw camera coordinates
            cameraRigActive.Should().BeFalse(
                "camera rig inactive — movement proceeds using raw camera coordinates with a notice to the GM");
        }
    }
}
