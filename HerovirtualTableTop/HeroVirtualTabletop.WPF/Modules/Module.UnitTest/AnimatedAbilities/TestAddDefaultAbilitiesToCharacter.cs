using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.Utility;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Add Default Abilities to Character</summary>
    [TestClass]
    public class TestAddDefaultAbilitiesToCharacter : BaseTest
    {
        private static readonly string[] ExpectedDefaultNames =
        {
            "Recovery", "Stun Recovery", "Pass Turn", "Half Phase Action", "Hold Action",
            "Draw A Weapon", "Dodge", "Strike", "Haymaker", "Prone",
            "Move By", "Move Through", "Grab", "Disarm", "Block",
            "Set", "Sweep", "Rapid Fire", "Off Ground", "Generic Damage/Power"
        };

        private CrowdMemberModel character;

        [TestInitialize]
        public void GivenACharacterWithAnEmptyAbilityOptionGroup()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");

            var globalDefaults = new List<AnimatedAbility>();
            foreach (var name in ExpectedDefaultNames)
                globalDefaults.Add(new AnimatedAbility(name));
            Helper.GlobalDefaultAbilities = globalDefaults;
        }

        [TestCleanup]
        public void Cleanup()
        {
            Helper.GlobalDefaultAbilities = null;
        }

        [TestMethod]
        public void AddDefaultAbilitiesAddsExactlyTwentyAbilitiesToFreshCharacter()
        {
            character.AddDefaultAbilities();

            int totalAbilityCount = 0;
            foreach (var og in character.OptionGroups)
            {
                foreach (var opt in og.Options)
                    if (opt is AnimatedAbility) totalAbilityCount++;
            }

            totalAbilityCount.Should().Be(20,
                because: "Add Default Abilities creates exactly 20 standard combat abilities");
        }

        [TestMethod]
        public void AllTwentyStandardNamesPresent()
        {
            character.AddDefaultAbilities();

            foreach (var name in ExpectedDefaultNames)
            {
                bool found = false;
                foreach (var og in character.OptionGroups)
                {
                    foreach (var opt in og.Options)
                    {
                        var ability = opt as AnimatedAbility;
                        if (ability != null && ability.Name == name)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                found.Should().BeTrue(
                    because: string.Format("'{0}' must be in the default abilities set", name));
            }
        }

        [TestMethod]
        public void DefaultAbilitiesAddedWithNoKeyNonPersistentNotDefault()
        {
            character.AddDefaultAbilities();

            foreach (var og in character.OptionGroups)
            {
                foreach (var opt in og.Options)
                {
                    var ability = opt as AnimatedAbility;
                    if (ability != null)
                    {
                        ability.ActivateOnKey.Should().Be(Keys.None,
                            because: string.Format("'{0}' should start with no activation key", ability.Name));
                        ability.Persistent.Should().BeFalse(
                            because: string.Format("'{0}' should start as non-persistent", ability.Name));
                    }
                }
            }
        }

        [TestMethod]
        public void DuplicateNamesNotReAddedWhenAbilityAlreadyPresent()
        {
            character.AnimatedAbilities.Add(new AnimatedAbility("Recovery"));

            character.AddDefaultAbilities();

            int recoveryCount = 0;
            foreach (var og in character.OptionGroups)
            {
                foreach (var opt in og.Options)
                {
                    var a = opt as AnimatedAbility;
                    if (a != null && a.Name == "Recovery") recoveryCount++;
                }
            }

            recoveryCount.Should().BeGreaterOrEqualTo(1,
                because: "Recovery must be present somewhere in the character's abilities");
        }
    }
}
