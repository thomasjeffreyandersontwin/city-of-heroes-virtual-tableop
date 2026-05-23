using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CrowdManagement.E2ETests.CombatExecution
{
    [TestClass]
    public class PlayOnHitAnimationOnDefender : CombatExecutionHelper
    {
        [TestInitialize]
        public void Setup()
        {
            GivenCombatExecutionBegun();
        }

        [TestMethod]
        public void HitOnHitPlays()
        {
            GivenPairResult("pair_1", "Hit");
            GivenOnHitAnimation("stun_hit_react");
            GivenSpawnedState("Villain_Boss_03", "true");
            WhenAttackAnimationCompletes("pair_1");
            ThenOnHitAnimationPlayed();
        }

        [TestMethod]
        public void MissNoOnHit()
        {
            GivenPairResult("pair_1", "Miss");
            GivenOnHitAnimation("stun_hit_react");
            WhenAttackAnimationCompletes("pair_1");
            ThenOnHitAnimationSkipped();
        }

        [TestMethod]
        public void NoAnimationConfiguredSkipped()
        {
            GivenPairResult("pair_1", "Hit");
            GivenOnHitAnimation("none");
            WhenAttackAnimationCompletes("pair_1");
            ThenOnHitAnimationSkipped();
        }

        [TestMethod]
        public void DefenderNotSpawnedSkipped()
        {
            GivenPairResult("pair_1", "Hit");
            GivenOnHitAnimation("stun_hit_react");
            GivenSpawnedState("Villain_Boss_03", "false");
            WhenAttackAnimationCompletes("pair_1");
            ThenOnHitAnimationSkipped();
        }
    }
}
