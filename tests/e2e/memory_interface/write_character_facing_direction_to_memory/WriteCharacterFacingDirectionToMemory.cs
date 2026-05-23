using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.MemoryInterface
{
    [TestClass]
    public class WriteCharacterFacingDirectionToMemory : MemoryInterfaceHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void NewFacingDiffersFromCurrentWriteIssued()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenCharacterFacingVector("0.0", "0.0", "1.0");

            // When
            WhenMovementDeterminesNewFacing("1.0", "0.0", "0.0");

            // Then
            ThenRotationMatrixWritten();
        }

        [TestMethod]
        public void NewFacingIdenticalToCurrentNoOp()
        {
            // Given
            GivenApplicationStarted();
            GivenMemoryInterfaceAttached();
            GivenTargetRegistrationState("confirmed");
            GivenCharacterFacingVector("1.0", "0.0", "0.0");

            // When
            WhenMovementDeterminesNewFacing("1.0", "0.0", "0.0");

            // Then
            ThenNoRotationWriteIssued();
        }
    }
}
