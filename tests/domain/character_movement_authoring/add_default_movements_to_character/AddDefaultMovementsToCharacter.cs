using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class AddDefaultMovementsToCharacter : CharacterMovementAuthoringDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a Character is selected in the crowd tree
        }

        [TestMethod]
        public void EmptyOptionGroupAllThreeAdded()
        {
            // Given: Character's Movement Option Group is empty
            // When: the GM invokes Add Default Movements
            when_default_movements_added(_character);
            // Then: Walk (default), Run, Swim are all created
            then_movement_in_group(_character, "Walk");
            then_movement_in_group(_character, "Run");
            then_movement_in_group(_character, "Swim");
        }

        [TestMethod]
        public void WalkExistsOnlyRunAndSwimAdded()
        {
            // Given: Walk already exists in the Movement Option Group
            given_movement_on_character(_character, given_movement("Walk"));
            int countBefore = _character.Movements.Count;
            // When: the GM invokes Add Default Movements
            when_default_movements_added(_character);
            // Then: only Run and Swim are added; Walk is not duplicated
            then_movement_in_group(_character, "Run");
            then_movement_in_group(_character, "Swim");
            _character.Movements.Count.Should().Be(countBefore + 2,
                "only Run and Swim must be added when Walk already exists");
        }

        [TestMethod]
        public void AllThreeExistNoneAdded()
        {
            // Given: Walk, Run, and Swim all exist in the Movement Option Group
            given_movement_on_character(_character, given_movement("Walk"));
            given_movement_on_character(_character, given_movement("Run"));
            given_movement_on_character(_character, given_movement("Swim"));
            int countBefore = _character.Movements.Count;
            // When: the GM invokes Add Default Movements
            when_default_movements_added(_character);
            // Then: no movements are added; a message indicates which were skipped
            then_movement_count(_character, countBefore);
        }
    }
}
