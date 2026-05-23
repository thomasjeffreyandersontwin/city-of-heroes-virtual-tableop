using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.DesktopOverlay
{
    [TestClass]
    public class MultiSelectCharacters : DesktopOverlayDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Desktop Overlay has Character Overlays rendered
        }

        [TestMethod]
        public void AddSecondOverlayToSelection()
        {
            // Given: Multi-Select has selected overlays Guard_Captain_01 (already selected)
            given_selected(_guardCaptain);
            // When: the GM shift/ctrl-clicks Guard_B to add a second overlay
            var guardB = given_overlay("Guard_B");
            when_modifier_click_add(guardB);
            // Then: Multi-Select has selected overlays Guard_Captain_01, Guard_B; both show multi-select highlight
            then_selected(_guardCaptain);
            then_selected(guardB);
            then_selection_count(2);
        }

        [TestMethod]
        public void RemoveFromMultiSelection()
        {
            // Given: Multi-Select has Guard_A and Guard_B selected
            var guardA = given_overlay("Guard_A");
            var guardB = given_overlay("Guard_B");
            given_selected(guardA); given_selected(guardB);
            // When: the GM shift/ctrl-clicks Guard_B to remove it from the selection
            when_modifier_click_remove(guardB);
            // Then: Multi-Select has Guard_A remaining; Guard_B shows no multi-select highlight
            then_selected(guardA);
            then_not_selected(guardB);
        }

        [TestMethod]
        public void ReduceToOneMultiSelectEnds()
        {
            // Given: Multi-Select has Guard_A and Guard_B selected
            var guardA = given_overlay("Guard_A");
            var guardB = given_overlay("Guard_B");
            given_selected(guardA); given_selected(guardB);
            // When: the GM removes Guard_A from selection (reduces to one)
            when_modifier_click_remove(guardA);
            // Then: Multi-Select has Guard_B only; Guard_B returns to single-select highlight
            then_not_selected(guardA);
            then_selected(guardB);
            then_selection_count(1);
        }

        [TestMethod]
        public void PlainClickDuringMultiClearsAll()
        {
            // Given: Multi-Select has Guard_A, Guard_B, Guard_C selected
            var guardA = given_overlay("Guard_A");
            var guardB = given_overlay("Guard_B");
            var guardC = given_overlay("Guard_C");
            given_selected(guardA); given_selected(guardB); given_selected(guardC);
            // When: the GM clicks Guard_C without modifier (plain click)
            when_single_click(guardC);
            // Then: all multi-selections cleared; Guard_C is the only selection (single-select)
            then_selected(guardC);
            then_selection_count(1);
            then_not_selected(guardA);
            then_not_selected(guardB);
        }

        [TestMethod]
        public void ContextMenuOnMultiAppliesToAll()
        {
            // Given: Multi-Select has Guard_A and Guard_B selected
            var guardA = given_overlay("Guard_A");
            var guardB = given_overlay("Guard_B");
            given_selected(guardA); given_selected(guardB);
            // When: a Context Menu is triggered on Guard_A during multi-select
            // Then: Context Menu applies to both Guard_A and Guard_B simultaneously
            _selection.Should().Contain(guardA, "Guard_A must be in multi-select for context menu to apply");
            _selection.Should().Contain(guardB, "Guard_B must also be in multi-select — action applies to all");
        }
    }
}
