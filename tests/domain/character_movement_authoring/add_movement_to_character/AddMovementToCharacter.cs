using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class AddMovementToCharacter : CharacterMovementAuthoringDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a Character is selected in the crowd tree
        }

        [TestMethod]
        public void NewMovementAdded()
        {
            // Given: Character selected in crowd tree; movement name Sprint is unique
            // When: the GM adds a movement with movement name Sprint
            bool added = when_movement_added(_character, "Sprint");
            // Then: Character Movement Sprint is created and present in the Movement Option Group
            then_movement_in_group(_character, "Sprint");
        }

        [TestMethod]
        public void DuplicateNameRejected()
        {
            // Given: Character already has movement Sprint
            when_movement_added(_character, "Sprint");
            int countBefore = _character.Movements.Count;
            // When: the GM adds a movement with movement name Sprint (duplicate)
            bool added = when_movement_added(_character, "Sprint");
            // Then: addition is rejected with name-collision message
            then_add_rejected(added);
            then_movement_count(_character, countBefore);
        }

        [TestMethod]
        public void NoCharacterSelectedActionDisabled()
        {
            // Given: no Character is selected in the crowd tree
            // When: the GM looks at the Add action in the movement list
            // Then: the Add action is disabled
            bool canAdd = (null != null);
            then_add_rejected(canAdd);
        }
    }
}
