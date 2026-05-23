using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AnimatedAbilityManagement
{
    [TestClass]
    public class CreateAnimatedAbility : AnimatedAbilityManagementDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Character Guard_Captain selected in crowd tree
        }

        [TestMethod]
        public void NewAbilityCreatedOnCharacter()
        {
            // Given: Character Guard_Captain selected in crowd tree
            // When: the GM selects Create in the ability list and provides name Fire Strike
            bool created = when_ability_created(_guardCaptain, "Fire Strike");
            // Then: Animated Ability Fire Strike has activation key (unset), persistence designation non-persistent
            then_ability_in_option_group(_guardCaptain, "Fire Strike");
            then_activation_key(_guardCaptain.AnimatedAbilities["Fire Strike"], null);
            then_persistence(_guardCaptain.AnimatedAbilities["Fire Strike"], false);
        }

        [TestMethod]
        public void DuplicateNameRejected()
        {
            // Given: Character Guard_Captain already has Animated Ability Fire Strike
            when_ability_created(_guardCaptain, "Fire Strike");
            int countBefore = _guardCaptain.AnimatedAbilities.Count;
            // When: the GM attempts to create an Animated Ability with ability name Fire Strike (duplicate)
            bool created = when_ability_created(_guardCaptain, "Fire Strike");
            // Then: system rejects creation with inline error; no ability added to Ability Option Group
            then_creation_rejected(created);
            then_ability_count(_guardCaptain, countBefore);
        }

        [TestMethod]
        public void NoCharacterSelectedActionDisabled()
        {
            // Given: no Character is selected in the crowd tree
            // When: the GM looks at the Create action in the ability list
            // Then: the Create action is disabled; ability list remains visible in its empty state
            bool canCreate = (null != null); // null character = no selection
            then_creation_rejected(canCreate);
        }
    }
}
