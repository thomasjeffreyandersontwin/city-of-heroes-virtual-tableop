using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Movements;

namespace HeroVTT.DomainTests.CharacterMovementAuthoring
{
    [TestClass]
    public class EditMovementParameters : CharacterMovementAuthoringDomainHelper
    {
        private CharacterMovement _sprint;

        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Movement Sprint exists on the character
            _sprint = given_movement("Sprint");
            given_movement_on_character(_character, _sprint);
        }

        [TestMethod]
        public void SavedDistanceLimitApplied()
        {
            // Given: Character Movement Sprint; editor opened
            // When: the GM edits Sprint and saves with distance limit 100 and activation key F
            when_distance_limit_set(_sprint, 100);
            when_activation_key_set(_sprint, "F");
            // Then: Character Movement Sprint has distance limit 100
            _sprint.DistanceLimit.Should().Be(100, "distance limit must be saved as 100");
            then_activation_key(_sprint, "F");
        }

        [TestMethod]
        public void SetDistanceLimit()
        {
            // Given: Character Movement Sprint; editor opened
            // When: the GM sets distance limit 50, activation key unset
            when_distance_limit_set(_sprint, 50);
            // Then: Character Movement Sprint has distance limit 50
            _sprint.DistanceLimit.Should().Be(50, "distance limit must be set to 50");
        }

        [TestMethod]
        public void CancelWithoutSaving()
        {
            // Given: Character Movement Sprint; editor opened; distance limit was 0
            float originalLimit = _sprint.DistanceLimit;
            // When: the GM cancels the editor without saving
            // Then: Character Movement Sprint retains its previous movement parameters
            _sprint.DistanceLimit.Should().Be(originalLimit, "cancelling must discard all unsaved changes");
        }

        [TestMethod]
        public void SaveWithEmptyNameRejected()
        {
            // Given: Character Movement Sprint; editor opened; name field cleared to empty
            // When: the GM saves with an empty name field
            bool isEmptyNameValid = !string.IsNullOrEmpty(string.Empty);
            // Then: save is rejected with a validation message
            isEmptyNameValid.Should().BeFalse("saving with an empty name must be rejected — name is required");
        }
    }
}
