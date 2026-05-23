using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class PlayAttackAnimationOnAttacker : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionBegun();
        }

        [TestMethod]
        public void AbilityConfiguredPlays()
        {
            GivenAttackAnimation("fire_blast_attack");
            GivenSpawnedState("Guard_Captain_01", "true");
            WhenPairResolutionBegins("pair_1");
            ThenAttackAnimationPlayed();
        }

        [TestMethod]
        public void NoAnimationConfiguredSkipped()
        {
            GivenAttackAnimation("none");
            WhenPairResolutionBegins("pair_1");
            ThenAttackAnimationSkipped();
        }

        [TestMethod]
        public void AttackerNotSpawnedAborted()
        {
            GivenAttackAnimation("fire_blast_attack");
            GivenSpawnedState("Guard_Captain_01", "false");
            WhenPairResolutionBegins("pair_1");
            ThenAttackAnimationSkipped();
        }
    }
}
