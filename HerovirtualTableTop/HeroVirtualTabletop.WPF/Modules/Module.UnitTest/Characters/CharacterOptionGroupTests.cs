using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;

namespace Module.UnitTest.Characters
{
    // ========================================================================
    // STORY: Create Character in Crowd — Option Group invariants
    //
    // CRC invariant: exactly three canonical option groups must always exist
    // on every character; each is created on first access but never absent.
    // Canonical names: Identities, Powers (Abilities), Movements.
    // ========================================================================

    [TestClass]
    public class TestCharacterOptionGroups
    {
        // ------------------------------------------------------------------
        // Given helpers

        private CrowdMemberModel GivenNewCharacterNamedGuard()
        {
            return new CrowdMemberModel("Guard");
        }

        private CrowdMemberModel GivenBareCrowdMemberModel()
        {
            // Default constructor — no name passed; character is "empty" but still initialises
            return new CrowdMemberModel();
        }

        // ------------------------------------------------------------------
        // Scenario: Create Character adds a node with exactly three Option Groups

        [TestMethod]
        public void GivenNewCharacter_WhenCreated_ThenHasExactlyThreeOptionGroups()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            // Access all three to force lazy creation (constructor calls them, but belt-and-suspenders)
            var id = character.AvailableIdentities;
            var ab = character.AnimatedAbilities;
            var mv = character.Movements;

            int count = character.OptionGroups.Count();
            Assert.AreEqual(3, count,
                "A character must have exactly three option groups. Found: " + count);
        }

        [TestMethod]
        public void GivenNewCharacter_WhenCreated_ThenIdentitiesGroupExists()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            IOptionGroup identities = character.OptionGroups
                .FirstOrDefault(g => g.Name == Constants.IDENTITY_OPTION_GROUP_NAME);

            Assert.IsNotNull(identities,
                "Option group '" + Constants.IDENTITY_OPTION_GROUP_NAME + "' must always exist.");
        }

        [TestMethod]
        public void GivenNewCharacter_WhenCreated_ThenAbilitiesGroupExists()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            IOptionGroup abilities = character.OptionGroups
                .FirstOrDefault(g => g.Name == Constants.ABILITY_OPTION_GROUP_NAME);

            Assert.IsNotNull(abilities,
                "Option group '" + Constants.ABILITY_OPTION_GROUP_NAME + "' must always exist.");
        }

        [TestMethod]
        public void GivenNewCharacter_WhenCreated_ThenMovementsGroupExists()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            IOptionGroup movements = character.OptionGroups
                .FirstOrDefault(g => g.Name == Constants.MOVEMENT_OPTION_GROUP_NAME);

            Assert.IsNotNull(movements,
                "Option group '" + Constants.MOVEMENT_OPTION_GROUP_NAME + "' must always exist.");
        }

        // ------------------------------------------------------------------
        // Scenario: Option groups are never null when accessed — lazy creation invariant

        [TestMethod]
        public void GivenCharacter_WhenAvailableIdentitiesAccessed_ThenNeverNull()
        {
            CrowdMemberModel character = GivenBareCrowdMemberModel();

            var identities = character.AvailableIdentities;

            Assert.IsNotNull(identities, "AvailableIdentities must never return null.");
        }

        [TestMethod]
        public void GivenCharacter_WhenAnimatedAbilitiesAccessed_ThenNeverNull()
        {
            CrowdMemberModel character = GivenBareCrowdMemberModel();

            var abilities = character.AnimatedAbilities;

            Assert.IsNotNull(abilities, "AnimatedAbilities must never return null.");
        }

        [TestMethod]
        public void GivenCharacter_WhenMovementsAccessed_ThenNeverNull()
        {
            CrowdMemberModel character = GivenBareCrowdMemberModel();

            var movements = character.Movements;

            Assert.IsNotNull(movements, "Movements must never return null.");
        }

        // ------------------------------------------------------------------
        // Scenario: Accessing an option group twice returns the same instance
        // (not a second copy added to the collection)

        [TestMethod]
        public void GivenCharacter_WhenAvailableIdentitiesAccessedTwice_ThenCollectionStaysAtThree()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            var first = character.AvailableIdentities;
            var second = character.AvailableIdentities;

            int count = character.OptionGroups.Count();
            Assert.AreEqual(3, count,
                "Accessing AvailableIdentities twice must not add a second group. Count: " + count);
        }

        [TestMethod]
        public void GivenCharacter_WhenAllThreeGroupsAccessedRepeatedly_ThenCountRemainsThree()
        {
            CrowdMemberModel character = GivenNewCharacterNamedGuard();

            for (int i = 0; i < 3; i++)
            {
                var id = character.AvailableIdentities;
                var ab = character.AnimatedAbilities;
                var mv = character.Movements;
            }

            int count = character.OptionGroups.Count();
            Assert.AreEqual(3, count,
                "Repeated access must not create duplicate option groups. Count: " + count);
        }
    }
}
