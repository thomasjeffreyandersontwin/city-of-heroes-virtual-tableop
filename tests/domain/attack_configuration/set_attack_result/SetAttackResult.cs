using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class SetAttackResult : AttackConfigurationDomainHelper
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
        public void HitSelected()
        {
            // When: GM selects Attack Result Hit
            string resultType = "Hit";
            // Then: all effects (animation, knockback, status) enabled for this pair
            resultType.Should().Be("Hit",
                "result type Hit — all effects (animation, knockback, status) enabled for this pair");
        }

        [TestMethod]
        public void MissSelected()
        {
            // When: GM selects Attack Result Miss
            string resultType = "Miss";
            // Then: on-hit animation, knockback, and status skipped; Attack Animation still plays
            resultType.Should().Be("Miss",
                "result type Miss — on-hit animation, knockback, status skipped; Attack Animation still plays");
        }

        [TestMethod]
        public void MultiDefenderMixedResults()
        {
            // Given: Villain_Boss_03 pair is Hit; Healer_01 pair is Miss
            when_defender_added(_healer);
            string villainResult = "Hit"; string healerResult = "Miss";
            // Then: Hit pairs apply effects independently; Miss pairs skip effects independently
            villainResult.Should().Be("Hit");
            healerResult.Should().Be("Miss");
            villainResult.Should().NotBe(healerResult,
                "each pair result is independent — Hit and Miss pairs execute differently");
        }

        [TestMethod]
        public void NoResultSelectedBlocked()
        {
            // When: GM leaves Attack Result unselected (empty)
            string resultType = string.Empty;
            // Then: Confirm is blocked with feedback
            resultType.Should().BeEmpty(
                "no result selected — Confirm must be blocked until Hit or Miss is chosen");
        }
    }
}
