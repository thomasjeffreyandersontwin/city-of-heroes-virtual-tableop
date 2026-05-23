using System;
using CrowdManagement.E2ETests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.AnimationElementAuthoring
{
    public class AnimationElementAuthoringHelper
    {
        protected AppDriver Driver;

        // ---------------------------------------------------------------
        // Given helpers
        // ---------------------------------------------------------------

        protected void GivenAbilityOpenInEditor(string abilityName)
        {
            Driver = new AppDriver();
            Driver.LaunchForStateSimulation();
            Driver.EnsureCharacterExists("Guard_Captain");
            Driver.AddAnimatedAbilityToCharacter("Guard_Captain", abilityName);
            Driver.OpenAbilityEditor(abilityName);
        }

        protected void GivenResourceCatalogLoaded(string catalogType)
        {
            Driver.SetResourceCatalogState(catalogType, "loaded");
        }

        protected void GivenResourceCatalogNotLoaded(string catalogType)
        {
            Driver.SetResourceCatalogState(catalogType, "not loaded");
        }

        protected void GivenMovementElement(string resourceName, int position)
        {
            Driver.AddElementToAbility("Movement", resourceName, position);
        }

        protected void GivenSoundElement(string resourceName, int position)
        {
            Driver.AddElementToAbility("Sound", resourceName, position);
        }

        protected void GivenFxElement(string resourceName, int position)
        {
            Driver.AddElementToAbility("FX", resourceName, position);
        }

        protected void GivenReferenceElement(string referencedAbilityName)
        {
            Driver.AddReferenceElementToAbility("current", referencedAbilityName);
        }

        protected void GivenSequenceElement(string executionType, int childCount)
        {
            Driver.AddSequenceElement(executionType, childCount);
        }

        protected void GivenPauseElement(string duration, int position)
        {
            Driver.AddPauseElement(duration, position);
        }

        protected void GivenLoadIdentityElement(string targetIdentityName)
        {
            Driver.AddLoadIdentityElement(targetIdentityName);
        }

        protected void GivenThreeElementsAtPositions()
        {
            Driver.AddElementToAbility("FX", "Elem1", 1);
            Driver.AddElementToAbility("FX", "Elem2", 2);
            Driver.AddElementToAbility("FX", "Elem3", 3);
        }

        protected void GivenSpawnedNpcPresent(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "present");
        }

        protected void GivenNoSpawnedNpc(string characterName)
        {
            Driver.SetSpawnedNpcState(characterName, "absent");
        }

        protected void GivenAnotherAbilityOnCharacter(string abilityName)
        {
            Driver.AddAnimatedAbilityToCharacter("Guard_Captain", abilityName);
        }

        protected void GivenIdentityOnCharacter(string identityName)
        {
            Driver.AddIdentityToCharacter("Guard_Captain", identityName, "inactive", "unset");
        }

        protected void GivenAbilityHasReferenceToSelf(string abilityName, string referencedAbility)
        {
            Driver.AddReferenceElementToAbility(abilityName, referencedAbility);
        }

        // ---------------------------------------------------------------
        // When helpers
        // ---------------------------------------------------------------

        protected void WhenGmAddsMovementElement(string resourceName)
        {
            Driver.InvokeAddElement("Movement", resourceName);
        }

        protected void WhenGmAddsSoundElement(string resourceName)
        {
            Driver.InvokeAddElement("Sound", resourceName);
        }

        protected void WhenGmAddsFxElement(string resourceName)
        {
            Driver.InvokeAddElement("FX", resourceName);
        }

        protected void WhenGmAddsReferenceElement(string referencedAbilityName)
        {
            Driver.InvokeAddReferenceElement(referencedAbilityName);
        }

        protected void WhenGmAddsSequenceElement(string executionType)
        {
            Driver.InvokeAddSequenceElement(executionType);
        }

        protected void WhenGmAddsPauseElement(string duration)
        {
            Driver.InvokeAddPauseElement(duration);
        }

        protected void WhenGmAddsLoadIdentityElement(string targetIdentityName)
        {
            Driver.InvokeAddLoadIdentityElement(targetIdentityName);
        }

        protected void WhenAbilityExecutesElement(string elementType, string resourceName)
        {
            Driver.SimulateElementExecution(elementType, resourceName);
        }

        protected void WhenSequenceElementExecutes(string executionType)
        {
            Driver.SimulateSequenceExecution(executionType);
        }

        protected void WhenGmDragDropsElement(int fromPosition, int toPosition)
        {
            Driver.InvokeDragDropElement(fromPosition, toPosition);
        }

        protected void WhenGmSaves()
        {
            Driver.InvokeSaveAbilityEditor();
        }

        protected void WhenGmCancels()
        {
            Driver.InvokeCancelAbilityEditor();
        }

        protected void WhenGmChangesSequenceType(string newType)
        {
            Driver.InvokeChangeSequenceType(newType);
        }

        protected void WhenGmStopsAbility()
        {
            Driver.InvokeStopAbility();
        }

        // ---------------------------------------------------------------
        // Then helpers
        // ---------------------------------------------------------------

        protected void ThenElementExists(string elementType, string resourceName)
        {
            Assert.IsTrue(Driver.IsElementInList(elementType, resourceName),
                string.Format("Expected element '{0}: {1}'", elementType, resourceName));
        }

        protected void ThenElementAtPosition(string resourceName, int expectedPosition)
        {
            int actual = Driver.GetElementPosition(resourceName);
            Assert.AreEqual(expectedPosition, actual,
                string.Format("Element '{0}' position: expected {1} got {2}",
                    resourceName, expectedPosition, actual));
        }

        protected void ThenElementAtBottom()
        {
            Assert.IsTrue(Driver.IsLastAddedElementAtBottom(),
                "New element should be at bottom of list");
        }

        protected void ThenNoElementAdded()
        {
            Assert.IsFalse(Driver.WasElementAddedSinceLastCheck(), "No element should be added");
        }

        protected void ThenGameCommandIssued(string commandType, string target)
        {
            Assert.IsTrue(Driver.WasGameCommandIssued(commandType, target),
                string.Format("Expected game command '{0}' for '{1}'", commandType, target));
        }

        protected void ThenElementProducesNoOp()
        {
            Assert.IsTrue(Driver.LastElementWasNoOp(), "Element should produce no-op");
        }

        protected void ThenSubsequentElementsContinue()
        {
            Assert.IsTrue(Driver.DidSubsequentElementsExecute(),
                "Subsequent elements should continue");
        }

        protected void ThenSequenceElementHasType(string expectedType)
        {
            string actual = Driver.GetLastSequenceElementType();
            Assert.AreEqual(expectedType, actual,
                string.Format("Sequence type: expected '{0}' got '{1}'", expectedType, actual));
        }

        protected void ThenPauseBlocksFor(string duration)
        {
            Assert.IsTrue(Driver.WasPauseApplied(duration),
                string.Format("Expected pause of {0}", duration));
        }

        protected void ThenIdentitySwitched(string identityName)
        {
            Assert.IsTrue(Driver.WasIdentitySwitchedTo(identityName),
                string.Format("Expected identity switch to '{0}'", identityName));
        }

        protected void ThenValidationRejected(string reason)
        {
            string msg = Driver.GetLastValidationMessage();
            Assert.IsNotNull(msg, "Expected validation rejection");
            Assert.IsTrue(msg.Contains(reason),
                string.Format("Rejection should contain '{0}'", reason));
        }

        protected void ThenElementListUnchanged()
        {
            Assert.IsTrue(Driver.IsElementListUnchangedFromLastSnapshot(),
                "Element list should be unchanged");
        }

        protected void ThenAllChildrenExecutedInOrder()
        {
            Assert.IsTrue(Driver.WereAllChildrenExecutedInOrder(),
                "All children should execute in order");
        }

        protected void ThenExactlyOneChildExecuted()
        {
            Assert.AreEqual(1, Driver.GetExecutedChildCount(),
                "Exactly one child should execute");
        }

        protected void ThenStopCompletesImmediately()
        {
            Assert.IsTrue(Driver.DidStopCompleteImmediately(),
                "Stop should complete immediately");
        }
    }
}
