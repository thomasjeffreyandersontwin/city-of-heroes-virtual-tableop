using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    [TestClass]
    public class AddDefaultMovementsToCharacter : CharacterMovementAuthoringHelper
    {
        [TestCleanup]
        public void Cleanup()
        {
            if (Driver != null) { Driver.Close(); Driver = null; }
        }

        [TestMethod]
        public void EmptyOptionGroupAllThreeAdded()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");

            // When
            WhenGmInvokesAddDefaultMovements();

            // Then
            ThenDefaultMovementsPresent();
            ThenMovementHasDefault("Walk", "default");
            ThenMovementCount(3);
        }

        [TestMethod]
        public void WalkExistsOnlyRunAndSwimAdded()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Walk", "Walk");

            // When
            WhenGmInvokesAddDefaultMovements();

            // Then
            ThenDefaultMovementsPresent();
            ThenMovementCount(3);
        }

        [TestMethod]
        public void AllThreeExistNoneAdded()
        {
            // Given
            GivenCharacterSelected("Guard_Captain");
            GivenCharacterMovementExists("Walk", "Walk");
            GivenCharacterMovementExists("Run", "Run");
            GivenCharacterMovementExists("Swim", "Swim");

            // When
            WhenGmInvokesAddDefaultMovements();

            // Then
            ThenMovementCount(3);
        }
    }
}
