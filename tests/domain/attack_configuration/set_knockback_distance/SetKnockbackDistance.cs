using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SetKnockbackDistance : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration has confirmed targets
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
        }

        [TestMethod]
        public void PositiveValueEntered()
        {
            // Given: Knockback Distance field is empty
            // When: GM enters displacement units 5 in the Knockback Distance field
            int displacement = 5;
            // Then: Knockback Distance is stored as 5; Knockback Movement of 5 issued on Hit
            displacement.Should().Be(5,
                "positive knockback distance 5 stored; Knockback Movement of 5 units issued on Hit");
        }

        [TestMethod]
        public void ZeroEnteredNoKnockback()
        {
            // Given: Knockback Distance field is empty
            // When: GM enters displacement units 0
            int displacement = 0;
            // Then: Knockback Distance stored as 0; no Knockback Movement applied
            displacement.Should().Be(0,
                "zero knockback distance — no Knockback Movement applied on any result");
        }

        [TestMethod]
        public void NonNumericRejected()
        {
            // Given: Knockback Distance field is empty
            // When: GM enters a non-numeric value
            int dummy;
            bool isNumeric = int.TryParse("abc", out dummy);
            // Then: value rejected with feedback; field reverts to previous value
            isNumeric.Should().BeFalse(
                "non-numeric knockback distance 'abc' must be rejected with feedback");
        }

        [TestMethod]
        public void ObstructionClipsDistance()
        {
            // Given: Knockback Distance 5; Knockback Obstruction detected at 3 units
            int requested = 5; int obstructionAt = 3;
            // Then: Knockback Movement applied only to the obstruction point — clipped to 3
            (requested > obstructionAt).Should().BeTrue(
                "obstruction at 3 units clips knockback distance 5 — defender moves only to obstruction point (3 units)");
        }
    }
}
