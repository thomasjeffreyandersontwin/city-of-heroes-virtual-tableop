using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CharacterMovementAuthoring
{
    public class CharacterMovementAuthoringHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenCharacterSelected(string characterName)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.EnsureCharacterExists(characterName);
            Driver.SelectCharacterInCrowdTree(characterName);
        }

        protected void GivenNoCharacterSelected()
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.ClearCharacterSelection();
        }

        protected void GivenCharacterMovementExists(string movementName, string movementType)
        {
            Driver.AddCharacterMovement(movementName, movementType);
        }

        protected void GivenMovementWithDefaultDesignation(string movementName, string designation)
        {
            Driver.SetMovementDefaultDesignation(movementName, designation);
        }

        protected void GivenMovementWithActivationKey(string movementName, string key)
        {
            Driver.SetMovementActivationKey(movementName, key);
        }

        protected void GivenTwoMovements(string name1, string name2)
        {
            Driver.AddCharacterMovement(name1, "Walk");
            Driver.AddCharacterMovement(name2, "Run");
        }

        protected void GivenMovementWithDistanceLimit(string movementName, string limit)
        {
            Driver.SetMovementDistanceLimit(movementName, limit);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmAddsMovement(string movementName)
        {
            Driver.InvokeAddCharacterMovement(movementName);
        }

        protected void WhenGmEditsMovementAndSaves(string movementName, string newType,
            string distanceLimit, string activationKey)
        {
            Driver.InvokeEditCharacterMovement(movementName, newType, distanceLimit, activationKey);
        }

        protected void WhenGmCancelsMovementEditor()
        {
            Driver.InvokeCancelMovementEditor();
        }

        protected void WhenGmRemovesMovement(string movementName)
        {
            Driver.InvokeRemoveCharacterMovement(movementName);
        }

        protected void WhenGmSetsDefaultMovement(string movementName)
        {
            Driver.InvokeSetDefaultMovement(movementName);
        }

        protected void WhenGmAssignsMovementKey(string movementName, string key)
        {
            Driver.InvokeSetMovementActivationKey(movementName, key);
        }

        protected void WhenGmInvokesAddDefaultMovements()
        {
            Driver.InvokeAddDefaultMovements();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenMovementExists(string movementName, string expectedType)
        {
            Assert.IsTrue(Driver.CharacterMovementExists(movementName),
                string.Format("Movement '{0}' should exist", movementName));
            string actual = Driver.GetCharacterMovementType(movementName);
            Assert.AreEqual(expectedType, actual,
                string.Format("Movement '{0}' type: expected '{1}' got '{2}'",
                    movementName, expectedType, actual));
        }

        protected void ThenMovementRejected()
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected rejection message");
        }

        protected void ThenAddActionDisabled()
        {
            Assert.IsFalse(Driver.IsAddMovementEnabled(), "Add movement should be disabled");
        }

        protected void ThenRemoveActionDisabled()
        {
            Assert.IsFalse(Driver.IsRemoveMovementEnabled(), "Remove movement should be disabled");
        }

        protected void ThenMovementNotInList(string movementName)
        {
            Assert.IsFalse(Driver.CharacterMovementExists(movementName),
                string.Format("Movement '{0}' should not exist", movementName));
        }

        protected void ThenMovementHasDefault(string movementName, string expected)
        {
            string actual = Driver.GetMovementDefaultDesignation(movementName);
            Assert.AreEqual(expected, actual,
                string.Format("Movement '{0}' default: expected '{1}' got '{2}'",
                    movementName, expected, actual));
        }

        protected void ThenMovementHasKey(string movementName, string expected)
        {
            string actual = Driver.GetMovementActivationKey(movementName);
            Assert.AreEqual(expected, actual,
                string.Format("Movement '{0}' key: expected '{1}' got '{2}'",
                    movementName, expected, actual));
        }

        protected void ThenMovementKeyFreed(string key)
        {
            Assert.IsFalse(Driver.IsMovementKeyInUse(key),
                string.Format("Key '{0}' should be freed", key));
        }

        protected void ThenValidationErrorShown(string fragment)
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected validation error");
            Assert.IsTrue(msg.Contains(fragment),
                string.Format("Message should contain '{0}'", fragment));
        }

        protected void ThenMovementCount(int expected)
        {
            int actual = Driver.GetCharacterMovementCount();
            Assert.AreEqual(expected, actual,
                string.Format("Expected {0} movements, got {1}", expected, actual));
        }

        protected void ThenDefaultMovementsPresent()
        {
            Assert.IsTrue(Driver.CharacterMovementExists("Walk"), "Walk should exist");
            Assert.IsTrue(Driver.CharacterMovementExists("Run"), "Run should exist");
            Assert.IsTrue(Driver.CharacterMovementExists("Swim"), "Swim should exist");
        }
    }
}
