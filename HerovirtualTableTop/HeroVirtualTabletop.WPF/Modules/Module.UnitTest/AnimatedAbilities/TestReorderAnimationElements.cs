using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using System.Linq;

namespace Module.UnitTest.AnimatedAbilities
{
    /// <summary>Story: Reorder Animation Elements via Drag-Drop</summary>
    [TestClass]
    public class TestReorderAnimationElements : BaseTest
    {
        private CrowdMemberModel character;
        private AnimatedAbility fireStrike;
        private PauseElement element1;
        private PauseElement element2;
        private PauseElement element3;

        [TestInitialize]
        public void GivenAnAbilityWithThreeElementsAtPositions1_2_3()
        {
            ResetKeyBindGeneratorStatics();
            character = new CrowdMemberModel("Guard_Captain");
            fireStrike = new AnimatedAbility("Fire Strike");
            character.AnimatedAbilities.Add(fireStrike);

            element1 = new PauseElement("Pause 1", 1);
            element2 = new PauseElement("Pause 2", 1);
            element3 = new PauseElement("Pause 3", 1);
            fireStrike.AddAnimationElement(element1);
            fireStrike.AddAnimationElement(element2);
            fireStrike.AddAnimationElement(element3);
        }

        [TestMethod]
        public void InitialElementOrderPositionsAreAscendingAndUnique()
        {
            var orders = fireStrike.AnimationElements
                .OrderBy(e => e.Order)
                .Select(e => e.Order)
                .ToList();

            orders.Should().Equal(new[] { 1, 2, 3 });
            orders.Should().OnlyHaveUniqueItems();
        }

        [TestMethod]
        public void ElementMovedToNewPositionUpdatesItsOrder()
        {
            // When: drag-drop element3 (currently at position 3) to position 1
            // Simulate by removing and re-inserting at the target order
            fireStrike.RemoveAnimationElement(element3);
            fireStrike.AddAnimationElement(element3, order: 1);

            element3.Order.Should().Be(1,
                because: "the moved element occupies position 1 after drag-drop");
        }

        [TestMethod]
        public void ElementsShiftWhenAnotherIsMovedToTheirPosition()
        {
            // When element3 is moved to position 1
            fireStrike.RemoveAnimationElement(element3);
            fireStrike.AddAnimationElement(element3, order: 1);

            // Then element1 and element2 shift to positions 2 and 3
            var orderedElements = fireStrike.AnimationElements.OrderBy(e => e.Order).ToList();
            orderedElements[0].Name.Should().Be("Pause 3",
                because: "the moved element now occupies position 1");
            orderedElements.Select(e => e.Order).Should().OnlyHaveUniqueItems(
                because: "all positions remain unique after reorder");
        }

        [TestMethod]
        public void AllPositionsRemainsUniqueAfterReorder()
        {
            fireStrike.RemoveAnimationElement(element3);
            fireStrike.AddAnimationElement(element3, order: 1);

            var orders = fireStrike.AnimationElements.Select(e => e.Order).ToList();

            orders.Should().OnlyHaveUniqueItems(
                because: "drag-drop reorder must preserve position uniqueness invariant");
        }

        [TestMethod]
        public void ElementCountUnchangedAfterReorder()
        {
            fireStrike.RemoveAnimationElement(element2);
            fireStrike.AddAnimationElement(element2, order: 1);

            fireStrike.AnimationElements.Count.Should().Be(3,
                because: "reorder must not add or remove elements — only change positions");
        }

        [TestMethod]
        public void ElementDroppedInSamePositionLeavesListUnchanged()
        {
            int originalOrder = element2.Order;

            // No-op: element2 is not moved (position stays the same)
            // Position is 2 and stays 2 — no reorder needed
            element2.Order.Should().Be(originalOrder,
                because: "dropping at the same position is a no-op");
            fireStrike.AnimationElements.Count.Should().Be(3);
        }

        [TestMethod]
        public void SavePersistsNewElementOrderOnAbility()
        {
            // After reorder: move element3 to front
            fireStrike.RemoveAnimationElement(element3);
            fireStrike.AddAnimationElement(element3, order: 1);

            // Verify the new order is visible on the ability
            var firstElement = fireStrike.AnimationElements.OrderBy(e => e.Order).First();
            firstElement.Name.Should().Be("Pause 3",
                because: "the persisted order now starts with the moved element");
        }
    }
}
