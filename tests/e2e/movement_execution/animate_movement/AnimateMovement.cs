using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MovementExecution
{
    [TestClass]
    public class AnimateMovement : MovementExecutionHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void WalkMovementBegins()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");

            // When
            WhenMovementBegins("Walk");

            // Then
            ThenMovementAnimationCycle("walk");
        }

        [TestMethod]
        public void RunMovementBegins()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Run", "Run", "100");

            // When
            WhenMovementBegins("Run");

            // Then
            ThenMovementAnimationCycle("run");
        }

        [TestMethod]
        public void SwimMovementBegins()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Swim", "Swim", "100");

            // When
            WhenMovementBegins("Swim");

            // Then
            ThenMovementAnimationCycle("swim");
        }

        [TestMethod]
        public void FlyMovementBegins()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Fly", "Fly", "100");

            // When
            WhenMovementBegins("Fly");

            // Then
            ThenMovementAnimationCycle("fly");
        }

        [TestMethod]
        public void JumpMovementBegins()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Jump", "Jump", "100");

            // When
            WhenMovementBegins("Jump");

            // Then
            ThenMovementAnimationCycle("jump");
        }

        [TestMethod]
        public void MovementHaltsAnimationStops()
        {
            // Given
            GivenMemoryInterfaceAttachedAndRegistered();
            GivenCharacterMovementActive("Walk", "Walk", "100");
            WhenMovementBegins("Walk");

            // When
            WhenMovementHalts();

            // Then
            ThenMovementAnimationCycle("stopped");
        }
    }
}
