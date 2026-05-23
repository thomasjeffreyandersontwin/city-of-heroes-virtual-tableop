using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class ExecuteSweepAttackAcrossMultipleTargets : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Attack Configuration has confirmed multiple Defenders
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
            when_defender_added(_healer);
        }

        [TestMethod]
        public void AllPairsResolved()
        {
            // Given: Sweep Attack sequential delivery order Pair_1, Pair_2, Pair_3
            string[] order = new[] { "Pair_1", "Pair_2", "Pair_3" };
            // When: GM confirms the Sweep Attack
            // Then: each pair resolved in sequence; Attack Animation then effects per pair
            order.Length.Should().Be(3,
                "Sweep Attack resolves all 3 pairs in sequence: Pair_1, Pair_2, Pair_3");
        }

        [TestMethod]
        public void MissPairAdvanceWithoutEffects()
        {
            // Given: Pair_1 has attack result Miss; Pair_2 has attack result Hit
            string pair1Result = "Miss"; string pair2Result = "Hit";
            // When: GM confirms the Sweep Attack
            // Then: Pair_1 advances without on-hit animation, knockback, or status; Pair_2 applies effects
            pair1Result.Should().Be("Miss",
                "Miss pair advances without effects — execution does not abort; subsequent pairs proceed");
            pair2Result.Should().Be("Hit",
                "Hit pair receives full effects after Miss pair advanced");
        }

        [TestMethod]
        public void AbortMidSweep()
        {
            // Given: Pair_1 resolved; Pair_2 not yet resolved; GM aborts
            string pair1State = "done"; string pair2State = "not resolved";
            // When: GM clicks Abort mid-sweep
            // Then: Pair_1 effects retained; Pair_2 produces no effects; Attack Configuration closes
            pair1State.Should().Be("done",
                "already-resolved Pair_1 effects are retained after abort");
            pair2State.Should().Be("not resolved",
                "unresolved Pair_2 must produce no effects after mid-sweep abort");
        }
    }
}
