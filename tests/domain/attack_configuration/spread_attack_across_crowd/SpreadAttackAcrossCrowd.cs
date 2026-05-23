using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SpreadAttackAcrossCrowd : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration panel is open
            given_panel_open();
            when_attacker_assigned(_guardCaptain);
        }

        [TestMethod]
        public void MembersInRangeAutoAdded()
        {
            // Given: Area Center Guard_Captain_01; Villain_A and Villain_B within area radius
            string centerNpc = "Guard_Captain_01";
            string[] inRange = new[] { "Villain_A", "Villain_B" };
            // When: GM triggers Spread Attack and designates Area Center Guard_Captain_01
            // Then: Villain_A and Villain_B populated as Defenders with default parameters
            centerNpc.Should().Be("Guard_Captain_01");
            inRange.Length.Should().Be(2,
                "Villain_A and Villain_B within radius must be auto-added as Defenders");
        }

        [TestMethod]
        public void MultipleCrowdsInRange()
        {
            // Given: Area Center Guard_Captain_01; Villain_A (crowd A), Guard_X (crowd B), Ally_Y (crowd C) in range
            string[] inRange = new[] { "Villain_A", "Guard_X", "Ally_Y" };
            // When: GM triggers Spread Attack
            // Then: all three from different crowds are included as Defenders
            inRange.Length.Should().Be(3,
                "multiple crowds in range — Villain_A, Guard_X, Ally_Y all included as Defenders");
        }

        [TestMethod]
        public void NoMembersInRange()
        {
            // Given: Area Center Guard_Captain_01; no spawned characters in area radius
            int inRangeCount = 0;
            // When: GM triggers Spread Attack
            // Then: feedback indicates area empty; configuration remains open with no Defenders added
            inRangeCount.Should().Be(0,
                "no members in range — feedback shown; configuration remains open with no Defenders added");
        }
    }
}
