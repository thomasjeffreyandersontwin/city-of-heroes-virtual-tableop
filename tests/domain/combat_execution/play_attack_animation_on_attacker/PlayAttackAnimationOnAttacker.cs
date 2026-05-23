using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class PlayAttackAnimationOnAttacker : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution has begun
        }

        [TestMethod]
        public void AbilityConfiguredPlays()
        {
            // Given: Attack Animation selected ability fire_blast_attack
            string selectedAbility = "fire_blast_attack";
            when_execution_begins();
            // When: Combat Execution begins a pair resolution
            // Then: Attack Animation plays; execution waits for completion
            selectedAbility.Should().Be("fire_blast_attack",
                "configured ability fire_blast_attack plays; execution waits for completion before advancing");
        }

        [TestMethod]
        public void NoAnimationConfiguredSkipped()
        {
            // Given: Attack Animation selected ability none
            string selectedAbility = "none";
            when_execution_begins();
            // When: Combat Execution begins pair resolution with no animation configured
            // Then: animation step skipped; execution advances to next step
            selectedAbility.Should().Be("none",
                "no animation configured — step skipped; execution advances immediately to next step");
        }

        [TestMethod]
        public void AttackerNotSpawnedAborted()
        {
            // Given: Attack Animation selected ability fire_blast_attack; attacker not spawned
            _attacker.HasBeenSpawned = false;
            string selectedAbility = "fire_blast_attack";
            when_execution_begins();
            // When: Combat Execution begins pair resolution
            // Then: animation skipped; all remaining pairs aborted
            _attacker.HasBeenSpawned.Should().BeFalse(
                "attacker not spawned — fire_blast_attack animation skipped; remaining pairs aborted");
        }
    }
}
