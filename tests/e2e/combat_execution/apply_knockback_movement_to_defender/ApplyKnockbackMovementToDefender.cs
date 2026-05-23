using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class ApplyKnockbackMovementToDefender : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionBegun();
        }

        [TestMethod]
        public void HitWithKnockbackFullDistance()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairKnockback("pair_1", "5");
            WhenKnockbackExecutes("pair_1");
            ThenKnockbackDestination("pair_1", "full_5_units");
        }

        [TestMethod]
        public void HitWithObstructionClipped()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairKnockback("pair_1", "5");
            GivenObstructionPresent();
            WhenKnockbackExecutes("pair_1");
            ThenKnockbackDestination("pair_1", "obstruction_point");
        }

        [TestMethod]
        public void ZeroKnockbackNoMovement()
        {
            GivenPairResult("pair_1", "Hit");
            GivenPairKnockback("pair_1", "0");
            WhenKnockbackExecutes("pair_1");
            ThenKnockbackDestination("pair_1", "no_movement");
        }

        [TestMethod]
        public void MissNoKnockback()
        {
            GivenPairResult("pair_1", "Miss");
            GivenPairKnockback("pair_1", "5");
            WhenKnockbackExecutes("pair_1");
            ThenKnockbackDestination("pair_1", "no_movement");
        }
    }
}
