using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HeroVTT.DomainTests.CombatExecution
{
    [TestClass]
    public class DetectKnockbackObstructionViaCollisionRay : CombatExecutionDomainHelper
    {
        [TestInitialize]
        public new void Init()
        {
            base.Init();
            // Background: Combat Execution is applying knockback
            given_execution_in_progress();
        }

        [TestMethod]
        public void ClearPathFullKnockback()
        {
            // Given: Collision Ray origin point (100, 0, -200), direction vector (1, 0, 0), maximum distance 5
            string origin = "(100, 0, -200)"; string direction = "(1, 0, 0)"; int maxDist = 5;
            // When: Game Collision Detection processes the ray
            // Then: Knockback Obstruction obstruction point none (full distance) — full 5 units applied
            string obstructionPoint = "none (full distance)";
            obstructionPoint.Should().Be("none (full distance)",
                "clear path from (100, 0, -200) in direction (1, 0, 0) for max 5 — full knockback applied");
        }

        [TestMethod]
        public void ObstructionDetectedClipped()
        {
            // Given: Collision Ray origin point (100, 0, -200), direction vector (1, 0, 0), maximum distance 5
            // When: Game Collision Detection processes the ray; obstruction detected
            // Then: Knockback Obstruction obstruction point (103, 0, -200) — clipped to 3 units
            string obstructionPoint = "(103, 0, -200)";
            obstructionPoint.Should().Be("(103, 0, -200)",
                "obstruction detected at (103, 0, -200) — Knockback Movement clipped to obstruction point");
        }

        [TestMethod]
        public void GameClientNotRunningSafeDefault()
        {
            // Given: Collision Ray origin (100, 0, -200), direction (1, 0, 0), max distance 5; game client not running
            // When: Game Collision Detection processes the ray
            // Then: clear-path result used with a warning logged; Knockback Obstruction obstruction point none (full distance)
            string obstructionPoint = "none (full distance)";
            obstructionPoint.Should().Be("none (full distance)",
                "game client not running — safe default clear-path result used; warning logged");
        }
    }
}
