using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class SetMovementActivationKey : CharacterMovementAuthoringDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character has Character Movements in its Movement Option Group
            given_movement_on_character(_character, given_movement("Sprint"));
            given_movement_on_character(_character, given_movement("Run"));
        }

        [TestMethod]
        public void AssignKeyFToSprint()
        {
            // Given: Sprint has no activation key; key F is available
            // When: the GM assigns movement activation key F to Character Movement Sprint
            when_activation_key_set(_character.Movements["Sprint"], "F");
            // Then: Character Movement Sprint has activation key F; Keyboard Hook routes F to Sprint dispatch
            then_activation_key(_character.Movements["Sprint"], "F");
        }

        [TestMethod]
        public void KeyFAlreadyUsedRejected()
        {
            // Given: Character Movement Run already has movement activation key F
            when_activation_key_set(_character.Movements["Run"], "F");
            // When: the GM assigns movement activation key F to Character Movement Sprint
            bool isDuplicate = _character.Movements.Contains(_character.Movements["Run"]) &&
                               _character.Movements["Run"].ActivationKey == Keys.F;
            // Then: assignment rejected with conflict message; Sprint retains its previous activation key
            isDuplicate.Should().BeTrue("F is already assigned to Run — duplicate must be rejected");
            _character.Movements["Sprint"].ActivationKey.Should().Be(Keys.None,
                "Sprint must retain its previous (unset) key when F assignment is rejected");
        }

        [TestMethod]
        public void ClearActivationKey()
        {
            // Given: Sprint has activation key F
            when_activation_key_set(_character.Movements["Sprint"], "F");
            // When: the GM clears the activation key on Sprint
            when_activation_key_set(_character.Movements["Sprint"], null);
            // Then: Sprint has no activation key; movement remains accessible from the movement list
            then_activation_key(_character.Movements["Sprint"], null);
        }
    }
}
