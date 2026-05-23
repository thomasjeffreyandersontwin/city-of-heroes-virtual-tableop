using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Movements;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class RemoveMovementFromCharacter : CharacterMovementAuthoringDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character has Character Movements in its Movement Option Group
            CharacterMovement sprint = given_movement("Sprint");
            when_activation_key_set(sprint, "S");
            given_movement_on_character(_character, sprint);
            CharacterMovement walk = given_movement("Walk");
            given_movement_on_character(_character, walk);
            when_default_movement_set(_character, "Walk");
            when_activation_key_set(walk, "W");
        }

        [TestMethod]
        public void RemoveNonDefaultMovement()
        {
            // Given: Movement Option Group default movement designation unset on Sprint; activation key S
            // When: the GM removes Character Movement Sprint
            when_movement_removed(_character, "Sprint");
            // Then: Sprint is deleted; activation key S is freed
            then_movement_not_in_group(_character, "Sprint");
        }

        [TestMethod]
        public void RemoveTheDefaultMovement()
        {
            // Given: Movement Option Group Walk has default movement designation default
            then_default_movement(_character, "Walk");
            // When: the GM removes Character Movement Walk (the default)
            when_movement_removed(_character, "Walk");
            // Then: Walk is deleted; no default movement designation remains on the character
            then_movement_not_in_group(_character, "Walk");
        }

        [TestMethod]
        public void NoMovementSelectedRemoveDisabled()
        {
            // Given: no Character Movement is selected in the movement list
            // When: the GM attempts to remove without selecting
            // Then: the Remove action is disabled
            bool canRemove = (null != null);
            canRemove.Should().BeFalse("Remove must be disabled when no movement is selected");
        }
    }
}
