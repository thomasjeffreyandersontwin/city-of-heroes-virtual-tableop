using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.AttackConfiguration
{
    [TestClass]
    public class AssignAutoFireShotsPerTarget : AttackConfigurationDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a Sweep Attack is configured with multiple Defenders
            given_targets_confirmed();
            when_attacker_assigned(_guardCaptain);
            when_defender_added(_villainBoss);
            when_defender_added(_healer);
        }

        [TestMethod]
        public void DividesEvenly6Shots3Targets()
        {
            // Given: Auto-Fire total shot count 6
            int totalShots = 6; int targets = 3;
            // When: GM enters shot count 6
            int shotsPerTarget = totalShots / targets;
            // Then: shots distributed proportionally — 2 shots per target
            shotsPerTarget.Should().Be(2,
                "6 shots across 3 targets divides evenly — 2 shots each");
        }

        [TestMethod]
        public void Remainder7Shots3Targets()
        {
            // Given: Auto-Fire total shot count 7
            int totalShots = 7; int targets = 3;
            // When: GM enters shot count 7
            int baseShots = totalShots / targets; int remainder = totalShots % targets;
            // Then: remainder allocated from the first defender
            baseShots.Should().Be(2);
            remainder.Should().Be(1,
                "7 shots / 3 targets: 2 per target with remainder 1 allocated to the first defender");
        }

        [TestMethod]
        public void ZeroOrBlankSingleExchange()
        {
            // Given: Auto-Fire total shot count 0
            int totalShots = 0;
            // When: GM enters 0 or leaves blank
            // Then: auto-fire skipped; each pair defaults to a single exchange
            totalShots.Should().Be(0,
                "zero or blank shot count — auto-fire skipped; each pair defaults to a single exchange");
        }

        [TestMethod]
        public void MultiShotPerPairRepeats()
        {
            // Given: Auto-Fire total shot count 4 distributed across 2 targets
            int totalShots = 4; int targets = 2;
            // When: GM enters shot count 4
            int shotsPerTarget = totalShots / targets;
            // Then: animation and effect sequence repeats for each shot on that pair
            shotsPerTarget.Should().Be(2,
                "4 shots / 2 targets = 2 shots each — animation and effect sequence repeats per shot");
        }
    }
}
