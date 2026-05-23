using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class SetDefaultMovement : CharacterMovementAuthoringDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character has two Character Movements: Walk (default) and Sprint
            var walk = given_movement("Walk");
            given_movement_on_character(_character, walk);
            when_default_movement_set(_character, "Walk");
            var sprint = given_movement("Sprint");
            given_movement_on_character(_character, sprint);
        }

        [TestMethod]
        public void SetSprintAsDefault()
        {
            // Given: Walk is currently the default movement; Sprint has no default
            // When: the GM sets Character Movement Sprint as the default
            when_default_movement_set(_character, "Sprint");
            // Then: Sprint has default movement designation default
            then_default_movement(_character, "Sprint");
        }

        [TestMethod]
        public void PreviousDefaultWalkCleared()
        {
            // Given: Walk has default movement designation; Sprint does not
            // When: the GM sets Sprint as default
            when_default_movement_set(_character, "Sprint");
            // Then: Walk has default movement designation unset; Sprint is the new default
            then_default_movement(_character, "Sprint");
            _character.DefaultMovement.Name.Should().NotBe("Walk",
                "previous default Walk must have its designation cleared to unset");
        }

        [TestMethod]
        public void RemoveDefaultWithoutReplacement()
        {
            // Given: Walk has default movement designation
            // When: the GM removes Walk's default designation without assigning another
            when_default_movement_cleared(_character);
            // Then: no movement has the default designation
            then_no_default_movement(_character);
        }
    }
}
