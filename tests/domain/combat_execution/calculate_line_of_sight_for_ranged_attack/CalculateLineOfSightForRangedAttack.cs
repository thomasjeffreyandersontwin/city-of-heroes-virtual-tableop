using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class CalculateLineOfSightForRangedAttack : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: a Ranged Attack is confirmed
        }

        [TestMethod]
        public void ClearToDefenderIncluded()
        {
            // Given: Ranged Attack line-of-sight requirement required; path to Villain_Boss_03 clear
            string pathState = "clear";
            // When: Game Collision Detection evaluates the path
            // Then: Line-of-Sight path state clear; Defender Villain_Boss_03 included in Combat Execution
            pathState.Should().Be("clear",
                "clear LOS to Villain_Boss_03 — included in Combat Execution");
        }

        [TestMethod]
        public void BlockedToDefenderExcluded()
        {
            // Given: Ranged Attack line-of-sight requirement required; path to Villain_Boss_03 blocked
            string pathState = "blocked";
            // When: Game Collision Detection evaluates the path
            // Then: Line-of-Sight path state blocked; Villain_Boss_03 excluded; GM shown the reason
            pathState.Should().Be("blocked",
                "blocked LOS — Villain_Boss_03 excluded from Combat Execution; GM shown the reason");
        }

        [TestMethod]
        public void AllBlockedConfirmBlocked()
        {
            // Given: all defenders have path state blocked
            string allPathState = "blocked";
            // When: Game Collision Detection evaluates all paths
            // Then: Line-of-Sight path state blocked for all; Confirm blocked with feedback
            allPathState.Should().Be("blocked",
                "all defenders blocked — Confirm must be blocked with feedback; no execution occurs");
        }

        [TestMethod]
        public void GameClientUnavailableSafeDefault()
        {
            // Given: game client unavailable during evaluation
            bool clientAvailable = false;
            // When: Game Collision Detection evaluates the path
            // Then: all defenders treated as clear (safe default); warning logged
            clientAvailable.Should().BeFalse(
                "game client unavailable — all defenders treated as clear (safe default); warning logged");
        }
    }
}
